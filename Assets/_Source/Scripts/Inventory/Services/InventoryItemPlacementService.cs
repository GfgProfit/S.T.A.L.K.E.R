using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemPlacementService
{
    private readonly InventoryItemFactory _itemFactory;
    private readonly InventoryItemRegistry _itemRegistry;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryPrepareSlotRestrictionsForPlacement;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryDetachItemFromGrid;
    private readonly Action<InventoryItem> _destroyInventoryItem;
    private readonly Action _refreshWeightState;

    public InventoryItemPlacementService(InventoryItemFactory itemFactory, InventoryItemRegistry itemRegistry, Func<InventoryGrid, InventoryItem, bool> tryPrepareSlotRestrictionsForPlacement, Func<InventoryGrid, InventoryItem, bool> tryDetachItemFromGrid, Action<InventoryItem> destroyInventoryItem, Action refreshWeightState)
    {
        _itemFactory = itemFactory;
        _itemRegistry = itemRegistry;
        _tryPrepareSlotRestrictionsForPlacement = tryPrepareSlotRestrictionsForPlacement;
        _tryDetachItemFromGrid = tryDetachItemFromGrid;
        _destroyInventoryItem = destroyInventoryItem;
        _refreshWeightState = refreshWeightState;
    }

    public bool TryInsertItem(ItemData itemData, int amount, float? durabilityPercent, IReadOnlyList<ItemData> installedModules, bool iconsReady, InventoryGrid insertionGrid, InventoryGrid defaultItemGrid)
    {
        if (iconsReady == false)
        {
            return false;
        }

        if (itemData == null)
        {
            return false;
        }

        if (insertionGrid == null)
        {
            return false;
        }

        int normalizedAmount = InventoryStackService.NormalizeAmount(itemData, amount);

        if (InventoryStackService.TryAddToExistingStack(itemData, normalizedAmount, insertionGrid, defaultItemGrid))
        {
            _refreshWeightState();
            return true;
        }

        InventoryItem itemToInsert = _itemFactory.Create(itemData, normalizedAmount, durabilityPercent, installedModules);

        if (InsertItem(itemToInsert, insertionGrid) || (insertionGrid != defaultItemGrid && InsertItem(itemToInsert, defaultItemGrid)))
        {
            _itemRegistry.Register(itemToInsert);
            _refreshWeightState();
            return true;
        }

        UnityEngine.Object.Destroy(itemToInsert.gameObject);
        return false;
    }

    public bool InsertItem(InventoryItem itemToInsert, InventoryGrid targetGrid)
    {
        if (targetGrid == null)
        {
            return false;
        }

        if (TryPlaceItemInFirstAvailableSpace(itemToInsert, targetGrid))
        {
            return true;
        }

        if (itemToInsert.CanRotate == false)
        {
            return false;
        }

        itemToInsert.Rotate();

        if (TryPlaceItemInFirstAvailableSpace(itemToInsert, targetGrid))
        {
            return true;
        }

        itemToInsert.Rotate();
        return false;
    }

    public bool TryMoveItemToGrid(InventoryGrid sourceGrid, InventoryItem item, InventoryGrid targetGrid, bool allowStackMerge)
    {
        if (sourceGrid == null || item == null || targetGrid == null || sourceGrid == targetGrid)
        {
            return false;
        }

        Vector2Int restorePosition = new(item.GridPositionX, item.GridPositionY);
        bool restoreRotated = item.IsRotated;

        if (_tryDetachItemFromGrid(sourceGrid, item) == false)
        {
            return false;
        }

        if (_tryPrepareSlotRestrictionsForPlacement(targetGrid, item) == false)
        {
            item.SetRotated(restoreRotated);
            sourceGrid.PlaceItem(item, restorePosition.x, restorePosition.y);
            return false;
        }

        if (allowStackMerge && item.IsStackable && InventoryStackService.TryAddToExistingStackInGrid(targetGrid, item.ItemData, item.CurrentAmount))
        {
            _destroyInventoryItem(item);
            _refreshWeightState();
            return true;
        }

        if (InsertItem(item, targetGrid))
        {
            _itemRegistry.Register(item);
            _refreshWeightState();
            return true;
        }

        item.SetRotated(restoreRotated);
        sourceGrid.PlaceItem(item, restorePosition.x, restorePosition.y);
        return false;
    }

    private bool TryPlaceItemInFirstAvailableSpace(InventoryItem itemToInsert, InventoryGrid targetGrid)
    {
        Vector2Int? posOnGrid = targetGrid.FindSpaceForObject(itemToInsert);

        if (posOnGrid == null)
        {
            return false;
        }

        if (_tryPrepareSlotRestrictionsForPlacement(targetGrid, itemToInsert) == false)
        {
            return false;
        }

        targetGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
        return true;
    }
}
