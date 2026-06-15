using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryEquipmentSlotService
{
    private readonly IReadOnlyList<EquipmentSlotGrid> _equipmentSlotGrids;
    private readonly IReadOnlyList<SlottedItemGrid> _slottedItemGrids;
    private readonly InventoryGrid _defaultItemGrid;
    private readonly Func<InventoryItem, InventoryGrid, bool> _insertItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryDetachItemFromGrid;
    private readonly Action<InventoryItem> _registerInventoryItem;
    private readonly Action _refreshWeightState;
    private readonly Func<GameObject> _getClosedSlotPrefab;

    public InventoryEquipmentSlotService(IReadOnlyList<EquipmentSlotGrid> equipmentSlotGrids, IReadOnlyList<SlottedItemGrid> slottedItemGrids, InventoryGrid defaultItemGrid, Func<InventoryItem, InventoryGrid, bool> insertItem, Func<InventoryGrid, InventoryItem, bool> tryDetachItemFromGrid, Action<InventoryItem> registerInventoryItem, Action refreshWeightState, Func<GameObject> getClosedSlotPrefab)
    {
        _equipmentSlotGrids = equipmentSlotGrids;
        _slottedItemGrids = slottedItemGrids;
        _defaultItemGrid = defaultItemGrid;
        _insertItem = insertItem;
        _tryDetachItemFromGrid = tryDetachItemFromGrid;
        _registerInventoryItem = registerInventoryItem;
        _refreshWeightState = refreshWeightState;
        _getClosedSlotPrefab = getClosedSlotPrefab;
    }

    public void RefreshSlotRestrictions()
    {
        GameObject resolvedClosedSlotPrefab = _getClosedSlotPrefab();
        InventoryItem equippedArmor = GetFirstEquippedItem(ItemType.Armor);
        bool closeHelmetSlot = equippedArmor != null && equippedArmor.ItemData != null && equippedArmor.ItemData.CanEquipHelmet == false;

        SetEquipmentSlotsClosed(ItemType.Helmet, closeHelmetSlot, resolvedClosedSlotPrefab);
        SetOpenArtifactSlotCount(ArmorArtifactSlotResolver.GetSlotCount(equippedArmor), resolvedClosedSlotPrefab);
    }

    public bool TryPrepareSlotRestrictionsForPlacement(InventoryGrid targetGrid, InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        if (item.ItemData.ItemType != ItemType.Armor || IsEquipmentSlot(targetGrid, ItemType.Armor) == false)
        {
            return true;
        }

        if (CanSetOpenArtifactSlotCount(ArmorArtifactSlotResolver.GetSlotCount(item)) == false)
        {
            return false;
        }

        return item.ItemData.CanEquipHelmet || TryMoveEquippedItemToInventory(ItemType.Helmet);
    }

    public bool CanDetachItemWithSlotRestrictions(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        if (item.ItemData.ItemType != ItemType.Armor || IsEquipmentSlot(sourceGrid, ItemType.Armor) == false)
        {
            return true;
        }

        return CanSetOpenArtifactSlotCount(0);
    }

    private InventoryItem GetFirstEquippedItem(ItemType itemType)
    {
        if (TryGetFirstEquipmentSlot(itemType, out EquipmentSlotGrid slot))
        {
            return slot.EquippedItem;
        }

        return null;
    }

    private bool TryGetFirstEquipmentSlot(ItemType itemType, out EquipmentSlotGrid slot)
    {
        slot = null;

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = _equipmentSlotGrids[i];

            if (grid == null || grid.AcceptedItemType != itemType)
            {
                continue;
            }

            if (grid.EquippedItem != null)
            {
                slot = grid;
                return true;
            }
        }

        return false;
    }

    private bool TryMoveEquippedItemToInventory(ItemType itemType)
    {
        if (_defaultItemGrid == null)
        {
            return false;
        }

        if (TryGetFirstEquipmentSlot(itemType, out EquipmentSlotGrid sourceGrid) == false)
        {
            return true;
        }

        InventoryItem item = sourceGrid.EquippedItem;
        if (item == null)
        {
            return true;
        }

        Vector2Int restorePosition = new(item.GridPositionX, item.GridPositionY);
        bool restoreRotated = item.IsRotated;

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

        item.SetRotated(restoreRotated);
        sourceGrid.PlaceItem(item, restorePosition.x, restorePosition.y);
        _refreshWeightState();
        return false;
    }

    private void SetEquipmentSlotsClosed(ItemType itemType, bool closed, GameObject resolvedClosedSlotPrefab)
    {
        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = _equipmentSlotGrids[i];

            if (grid == null || grid.AcceptedItemType != itemType)
            {
                continue;
            }

            grid.SetClosed(closed, resolvedClosedSlotPrefab);
        }
    }

    private bool CanSetOpenArtifactSlotCount(int openSlotCount)
    {
        int remainingOpenSlots = Mathf.Max(0, openSlotCount);

        for (int i = 0; i < _slottedItemGrids.Count; i++)
        {
            SlottedItemGrid grid = _slottedItemGrids[i];

            if (grid == null || grid.HasArtifactSlots == false)
            {
                continue;
            }

            if (grid.CanSetOpenArtifactSlotCount(remainingOpenSlots) == false)
            {
                return false;
            }

            remainingOpenSlots = Mathf.Max(0, remainingOpenSlots - grid.ArtifactSlotCount);
        }

        return true;
    }

    private void SetOpenArtifactSlotCount(int openSlotCount, GameObject resolvedClosedSlotPrefab)
    {
        int remainingOpenSlots = Mathf.Max(0, openSlotCount);

        for (int i = 0; i < _slottedItemGrids.Count; i++)
        {
            SlottedItemGrid grid = _slottedItemGrids[i];

            if (grid == null || grid.HasArtifactSlots == false)
            {
                continue;
            }

            remainingOpenSlots = grid.SetOpenArtifactSlotCount(remainingOpenSlots, resolvedClosedSlotPrefab);
        }
    }

    private static bool IsEquipmentSlot(InventoryGrid grid, ItemType itemType) => grid is EquipmentSlotGrid equipmentSlot && equipmentSlot.AcceptedItemType == itemType;
}