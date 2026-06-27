using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemPlacementService
{
    private readonly InventoryItemFactory _itemFactory;
    private readonly InventoryItemRegistry _itemRegistry;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryPrepareSlotRestrictionsForPlacement;
    private readonly Action _refreshWeightState;

    public InventoryItemPlacementService(InventoryItemFactory itemFactory, InventoryItemRegistry itemRegistry, Func<InventoryGrid, InventoryItem, bool> tryPrepareSlotRestrictionsForPlacement, Action refreshWeightState)
    {
        _itemFactory = itemFactory;
        _itemRegistry = itemRegistry;
        _tryPrepareSlotRestrictionsForPlacement = tryPrepareSlotRestrictionsForPlacement;
        _refreshWeightState = refreshWeightState;
    }

    public bool TryInsertItem(ItemData itemData, int amount, float? durabilityPercent, IReadOnlyList<ItemData> installedModules, FirstPersonWeaponMagazineState weaponMagazineState, InventoryGrid insertionGrid, InventoryGrid defaultItemGrid)
    {
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

        InventoryItem itemToInsert = _itemFactory.Create(itemData, normalizedAmount, durabilityPercent, installedModules, weaponMagazineState);

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
