using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryQuickUseService
{
    private readonly IReadOnlyList<QuickUseSlotBinding> _quickUseSlotBindings;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryDetachItemFromGrid;
    private readonly Action<InventoryItem> _destroyInventoryItem;
    private readonly Action _refreshInventoryState;

    public InventoryQuickUseService(IReadOnlyList<QuickUseSlotBinding> quickUseSlotBindings, Func<InventoryGrid, InventoryItem, bool> tryDetachItemFromGrid, Action<InventoryItem> destroyInventoryItem, Action refreshInventoryState)
    {
        _quickUseSlotBindings = quickUseSlotBindings;
        _tryDetachItemFromGrid = tryDetachItemFromGrid;
        _destroyInventoryItem = destroyInventoryItem;
        _refreshInventoryState = refreshInventoryState;
    }

    public InventoryItem GetSlotItem(int slotIndex)
    {
        InventoryGrid grid = GetSlotGrid(slotIndex);
        return grid == null ? null : grid.GetItem(0, 0);
    }

    public bool TryUseSlot(int slotIndex, out ItemData usedItemData)
    {
        InventoryGrid grid = GetSlotGrid(slotIndex);
        InventoryItem item = grid == null ? null : grid.GetItem(0, 0);

        return TryUseItem(grid, item, out usedItemData);
    }

    public bool TryUseItem(InventoryGrid grid, InventoryItem item, out ItemData usedItemData)
    {
        usedItemData = null;

        if (grid == null || item == null || item.ItemData == null || item.ItemData.ItemType != ItemType.Consumable)
        {
            return false;
        }

        ItemData itemData = item.ItemData;

        if (item.IsStackable && item.CurrentAmount > 1)
        {
            item.SetAmount(item.CurrentAmount - 1);
            usedItemData = itemData;
            _refreshInventoryState();
            return true;
        }

        if (_tryDetachItemFromGrid(grid, item) == false)
        {
            return false;
        }

        _destroyInventoryItem(item);
        usedItemData = itemData;
        _refreshInventoryState();
        return true;
    }

    private InventoryGrid GetSlotGrid(int slotIndex)
    {
        if (_quickUseSlotBindings == null || slotIndex < 0)
        {
            return null;
        }

        for (int i = 0; i < _quickUseSlotBindings.Count; i++)
        {
            QuickUseSlotBinding binding = _quickUseSlotBindings[i];

            if (binding != null && binding.SlotIndex == slotIndex)
            {
                return binding.InventorySlot;
            }
        }

        return null;
    }
}
