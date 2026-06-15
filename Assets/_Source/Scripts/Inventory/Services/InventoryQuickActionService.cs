using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

internal sealed class InventoryQuickActionService
{
    private readonly IReadOnlyList<InventoryGrid> _quickActionGridReferences;
    private readonly InventoryGrid _defaultItemGrid;
    private readonly Func<InventoryGrid, InventoryItem, InventoryGrid, bool, bool> _tryMoveItemToGrid;
    private readonly Func<ItemData, int, IReadOnlyList<ItemIconPart>, InventoryItem> _createItem;
    private readonly Action<InventoryItem> _registerInventoryItem;
    private readonly Action _refreshWeightState;
    private readonly List<InventoryGrid> _quickActionGrids = new();

    public InventoryQuickActionService(IReadOnlyList<InventoryGrid> quickActionGridReferences, InventoryGrid defaultItemGrid, Func<InventoryGrid, InventoryItem, InventoryGrid, bool, bool> tryMoveItemToGrid, Func<ItemData, int, IReadOnlyList<ItemIconPart>, InventoryItem> createItem, Action<InventoryItem> registerInventoryItem, Action refreshWeightState)
    {
        _quickActionGridReferences = quickActionGridReferences;
        _defaultItemGrid = defaultItemGrid;
        _tryMoveItemToGrid = tryMoveItemToGrid;
        _createItem = createItem;
        _registerInventoryItem = registerInventoryItem;
        _refreshWeightState = refreshWeightState;
    }

    public bool TryQuickMoveItemToInventory(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (sourceGrid == null || item == null || _defaultItemGrid == null || sourceGrid == _defaultItemGrid)
        {
            return false;
        }

        return _tryMoveItemToGrid(sourceGrid, item, _defaultItemGrid, true);
    }

    public bool TryQuickEquipItem(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (sourceGrid == null || item == null)
        {
            return false;
        }

        CollectQuickActionGrids();

        for (int i = 0; i < _quickActionGrids.Count; i++)
        {
            InventoryGrid targetGrid = _quickActionGrids[i];

            if (IsQuickEquipTargetGrid(sourceGrid, targetGrid) == false)
            {
                continue;
            }

            if (targetGrid is SlottedItemGrid slottedGrid && TryQuickEquipSingleItemFromStack(sourceGrid, item, slottedGrid))
            {
                return true;
            }

            if (_tryMoveItemToGrid(sourceGrid, item, targetGrid, false))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryQuickEquipSingleItemFromStack(InventoryGrid sourceGrid, InventoryItem item, SlottedItemGrid targetGrid)
    {
        if (sourceGrid == null || item == null || targetGrid == null)
        {
            return false;
        }

        Vector2Int? targetPosition = targetGrid.FindSpaceForObject(item);

        if (targetPosition == null || targetGrid.ShouldSplitStackOnPlace(item, targetPosition.Value.x, targetPosition.Value.y) == false || targetGrid.CanPlaceItem(item, targetPosition.Value.x, targetPosition.Value.y) == false)
        {
            return false;
        }

        InventoryItem singleItem = _createItem(item.ItemData, 1, item.RuntimeIconParts);

        if (targetGrid.CanPlaceItem(singleItem, targetPosition.Value.x, targetPosition.Value.y) == false)
        {
            Object.Destroy(singleItem.gameObject);
            return false;
        }

        item.SetAmount(item.CurrentAmount - 1);
        targetGrid.PlaceItem(singleItem, targetPosition.Value.x, targetPosition.Value.y);

        _registerInventoryItem(singleItem);
        _refreshWeightState();
        return true;
    }

    private void CollectQuickActionGrids()
    {
        _quickActionGrids.Clear();

        for (int i = 0; i < _quickActionGridReferences.Count; i++)
        {
            InventoryGrid grid = _quickActionGridReferences[i];

            if (grid != null && _quickActionGrids.Contains(grid) == false)
            {
                _quickActionGrids.Add(grid);
            }
        }
    }

    private static bool IsQuickEquipTargetGrid(InventoryGrid sourceGrid, InventoryGrid targetGrid)
    {
        if (targetGrid == null || targetGrid == sourceGrid || targetGrid.gameObject.activeInHierarchy == false)
        {
            return false;
        }

        return targetGrid is EquipmentSlotGrid || targetGrid is SlottedItemGrid;
    }
}