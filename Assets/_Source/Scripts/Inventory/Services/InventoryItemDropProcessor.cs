using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemDropProcessor
{
    private readonly Func<ItemData, int, float, IReadOnlyList<ItemData>, FirstPersonWeaponMagazineState, bool> _spawnWorldItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _detachItemFromGrid;
    private readonly Action<InventoryItem> _destroyInventoryItem;
    private readonly Action _refreshWeightState;

    public InventoryItemDropProcessor(Func<ItemData, int, float, IReadOnlyList<ItemData>, FirstPersonWeaponMagazineState, bool> spawnWorldItem, Func<InventoryGrid, InventoryItem, bool> detachItemFromGrid, Action<InventoryItem> destroyInventoryItem, Action refreshWeightState)
    {
        _spawnWorldItem = spawnWorldItem;
        _detachItemFromGrid = detachItemFromGrid;
        _destroyInventoryItem = destroyInventoryItem;
        _refreshWeightState = refreshWeightState;
    }

    public bool TryDropItem(InventoryGrid grid, InventoryItem item, bool wholeStack)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        ItemData itemData = item.ItemData;
        int amountToDrop = wholeStack ? item.CurrentAmount : 1;
        float durabilityPercent = item.CurrentDurabilityPercent;
        bool removeWholeItem = wholeStack || item.IsStackable == false || item.CurrentAmount <= amountToDrop;

        if (removeWholeItem == false)
        {
            if (_spawnWorldItem(itemData, amountToDrop, durabilityPercent, null, null) == false)
            {
                return false;
            }

            item.SetAmount(item.CurrentAmount - amountToDrop);
            _refreshWeightState();
            return true;
        }

        Vector2Int restorePosition = new(item.GridPositionX, item.GridPositionY);

        if (_detachItemFromGrid(grid, item) == false)
        {
            return false;
        }

        if (_spawnWorldItem(itemData, amountToDrop, durabilityPercent, item.InstalledModules, item.WeaponMagazineState) == false)
        {
            grid.PlaceItem(item, restorePosition.x, restorePosition.y);
            return false;
        }

        _destroyInventoryItem(item);
        _refreshWeightState();
        return true;
    }
}
