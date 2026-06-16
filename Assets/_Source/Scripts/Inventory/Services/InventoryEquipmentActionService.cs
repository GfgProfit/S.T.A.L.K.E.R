using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryEquipmentActionService
{
    private readonly IReadOnlyList<EquipmentSlotGrid> _equipmentSlotGrids;
    private readonly InventoryGrid _defaultItemGrid;
    private readonly Func<InventoryItem, InventoryGrid, bool> _insertItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryDetachItemFromGrid;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryPrepareSlotRestrictionsForPlacement;
    private readonly Action<InventoryItem> _registerInventoryItem;
    private readonly Action _refreshWeightState;

    public InventoryEquipmentActionService(IReadOnlyList<EquipmentSlotGrid> equipmentSlotGrids, InventoryGrid defaultItemGrid, Func<InventoryItem, InventoryGrid, bool> insertItem, Func<InventoryGrid, InventoryItem, bool> tryDetachItemFromGrid, Func<InventoryGrid, InventoryItem, bool> tryPrepareSlotRestrictionsForPlacement, Action<InventoryItem> registerInventoryItem, Action refreshWeightState)
    {
        _equipmentSlotGrids = equipmentSlotGrids;
        _defaultItemGrid = defaultItemGrid;
        _insertItem = insertItem;
        _tryDetachItemFromGrid = tryDetachItemFromGrid;
        _tryPrepareSlotRestrictionsForPlacement = tryPrepareSlotRestrictionsForPlacement;
        _registerInventoryItem = registerInventoryItem;
        _refreshWeightState = refreshWeightState;
    }

    public bool CanEquipToSlot(InventoryGrid sourceGrid, InventoryItem item, ItemType itemType, int itemTypeSlotIndex)
    {
        if (TryGetEquipmentSlot(itemType, itemTypeSlotIndex, out EquipmentSlotGrid targetSlot) == false)
        {
            return false;
        }

        if (CanStartEquip(sourceGrid, item, targetSlot, itemType) == false)
        {
            return false;
        }

        InventoryItem replacedItem = targetSlot.EquippedItem;
        return replacedItem == null || CanMoveItemToInventoryAfterSourceRemoval(sourceGrid, item, replacedItem);
    }

    public bool TryEquipToSlot(InventoryGrid sourceGrid, InventoryItem item, ItemType itemType, int itemTypeSlotIndex)
    {
        if (TryGetEquipmentSlot(itemType, itemTypeSlotIndex, out EquipmentSlotGrid targetSlot) == false || CanStartEquip(sourceGrid, item, targetSlot, itemType) == false)
        {
            return false;
        }

        Vector2Int sourcePosition = new(item.GridPositionX, item.GridPositionY);
        bool sourceRotated = item.IsRotated;
        InventoryItem replacedItem = targetSlot.EquippedItem;
        bool replacedRotated = replacedItem != null && replacedItem.IsRotated;
        bool replacedMovedToInventory = false;

        if (_tryDetachItemFromGrid(sourceGrid, item) == false)
        {
            return false;
        }

        if (replacedItem != null)
        {
            if (targetSlot.PickUpItem(0, 0) != replacedItem)
            {
                RestoreItem(sourceGrid, item, sourcePosition, sourceRotated);
                return false;
            }

            if (_insertItem(replacedItem, _defaultItemGrid) == false)
            {
                RestoreReplacedItem(targetSlot, replacedItem, replacedRotated, false);
                RestoreItem(sourceGrid, item, sourcePosition, sourceRotated);
                _refreshWeightState();
                return false;
            }

            replacedMovedToInventory = true;
            _registerInventoryItem(replacedItem);
        }

        if (_tryPrepareSlotRestrictionsForPlacement(targetSlot, item) == false || targetSlot.CanPlaceItem(item, 0, 0) == false)
        {
            RestoreReplacedItem(targetSlot, replacedItem, replacedRotated, replacedMovedToInventory);
            RestoreItem(sourceGrid, item, sourcePosition, sourceRotated);
            _refreshWeightState();
            return false;
        }

        targetSlot.PlaceItem(item, 0, 0);
        _registerInventoryItem(item);
        _refreshWeightState();
        return true;
    }

    public bool CanUnequip(InventoryGrid sourceGrid, InventoryItem item)
    {
        return CanStartUnequip(sourceGrid, item) && _defaultItemGrid != null && _defaultItemGrid.FindSpaceForObject(item) != null;
    }

    public bool TryUnequip(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (CanStartUnequip(sourceGrid, item) == false)
        {
            return false;
        }

        Vector2Int sourcePosition = new(item.GridPositionX, item.GridPositionY);
        bool sourceRotated = item.IsRotated;

        if (_tryDetachItemFromGrid(sourceGrid, item) == false)
        {
            return false;
        }

        if (_insertItem(item, _defaultItemGrid))
        {
            _registerInventoryItem(item);
            _refreshWeightState();
            return true;
        }

        RestoreItem(sourceGrid, item, sourcePosition, sourceRotated);
        _refreshWeightState();
        return false;
    }

    private bool TryGetEquipmentSlot(ItemType itemType, int itemTypeSlotIndex, out EquipmentSlotGrid slot)
    {
        slot = null;

        int resolvedSlotIndex = Mathf.Max(0, itemTypeSlotIndex);
        int matchedSlotIndex = 0;

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = _equipmentSlotGrids[i];

            if (grid == null || grid.AcceptedItemType != itemType)
            {
                continue;
            }

            if (matchedSlotIndex == resolvedSlotIndex)
            {
                slot = grid;
                return true;
            }

            matchedSlotIndex++;
        }

        return false;
    }

    private static bool CanStartEquip(InventoryGrid sourceGrid, InventoryItem item, EquipmentSlotGrid targetSlot, ItemType itemType)
    {
        if (sourceGrid == null || item == null || item.ItemData == null || targetSlot == null || sourceGrid == targetSlot || targetSlot.IsClosed)
        {
            return false;
        }

        return item.ItemData.ItemType == itemType && targetSlot.AcceptedItemType == itemType;
    }

    private static bool CanStartUnequip(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (sourceGrid is not EquipmentSlotGrid equipmentSlot || item == null || item.ItemData == null)
        {
            return false;
        }

        if (item.ItemData.ItemType == ItemType.Artifact || item.ItemData.ItemType == ItemType.Consumable)
        {
            return false;
        }

        return equipmentSlot.EquippedItem == item;
    }

    private bool CanMoveItemToInventoryAfterSourceRemoval(InventoryGrid sourceGrid, InventoryItem sourceItem, InventoryItem itemToMove)
    {
        if (_defaultItemGrid == null || itemToMove == null)
        {
            return false;
        }

        if (_defaultItemGrid.FindSpaceForObject(itemToMove) != null)
        {
            return true;
        }

        if (sourceGrid != _defaultItemGrid || sourceItem == null)
        {
            return false;
        }

        Vector2Int sourcePosition = new(sourceItem.GridPositionX, sourceItem.GridPositionY);
        InventoryItem pickedItem = _defaultItemGrid.PickUpItem(sourcePosition.x, sourcePosition.y);

        if (pickedItem != sourceItem)
        {
            if (pickedItem != null)
            {
                _defaultItemGrid.PlaceItem(pickedItem, sourcePosition.x, sourcePosition.y);
            }

            return false;
        }

        bool hasSpace = _defaultItemGrid.FindSpaceForObject(itemToMove) != null;
        _defaultItemGrid.PlaceItem(sourceItem, sourcePosition.x, sourcePosition.y);
        return hasSpace;
    }

    private void RestoreReplacedItem(EquipmentSlotGrid targetSlot, InventoryItem replacedItem, bool replacedRotated, bool detachFromInventory)
    {
        if (targetSlot == null || replacedItem == null)
        {
            return;
        }

        if (detachFromInventory)
        {
            _tryDetachItemFromGrid(_defaultItemGrid, replacedItem);
        }

        replacedItem.SetRotated(replacedRotated);
        targetSlot.PlaceItem(replacedItem, 0, 0);
    }

    private bool RestoreItem(InventoryGrid sourceGrid, InventoryItem item, Vector2Int sourcePosition, bool sourceRotated)
    {
        if (sourceGrid == null || item == null)
        {
            return false;
        }

        item.SetRotated(sourceRotated);

        if (sourceGrid.CanPlaceItem(item, sourcePosition.x, sourcePosition.y))
        {
            sourceGrid.PlaceItem(item, sourcePosition.x, sourcePosition.y);
            return true;
        }

        return _insertItem(item, _defaultItemGrid);
    }
}
