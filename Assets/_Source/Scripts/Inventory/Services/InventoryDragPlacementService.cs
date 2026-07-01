using System;
using UnityEngine;

internal sealed class InventoryDragPlacementService
{
    private readonly InventoryDragState _dragState;
    private readonly InventoryItemFactory _itemFactory;
    private readonly InventoryItemRegistry _itemRegistry;
    private readonly Func<InventoryGrid, InventoryItem, bool> _canDetachItemWithSlotRestrictions;
    private readonly Func<InventoryGrid, InventoryItem, bool> _tryPrepareSlotRestrictionsForPlacement;
    private readonly Func<InventoryGrid, InventoryItem, ItemData, bool> _tryInstallWeaponModule;
    private readonly Action<InventoryItem> _weaponModulesChanged;
    private readonly Action _refreshWeightState;

    public InventoryDragPlacementService(InventoryDragState dragState, InventoryItemFactory itemFactory, InventoryItemRegistry itemRegistry, Func<InventoryGrid, InventoryItem, bool> canDetachItemWithSlotRestrictions, Func<InventoryGrid, InventoryItem, bool> tryPrepareSlotRestrictionsForPlacement, Func<InventoryGrid, InventoryItem, ItemData, bool> tryInstallWeaponModule, Action<InventoryItem> weaponModulesChanged, Action refreshWeightState)
    {
        _dragState = dragState;
        _itemFactory = itemFactory;
        _itemRegistry = itemRegistry;
        _canDetachItemWithSlotRestrictions = canDetachItemWithSlotRestrictions;
        _tryPrepareSlotRestrictionsForPlacement = tryPrepareSlotRestrictionsForPlacement;
        _tryInstallWeaponModule = tryInstallWeaponModule;
        _weaponModulesChanged = weaponModulesChanged;
        _refreshWeightState = refreshWeightState;
    }

    public bool TryPlaceDraggedItem(InventoryGrid targetGrid, Vector2Int tileGridPosition)
    {
        InventoryItem selectedItem = _dragState.SelectedItem;

        if (targetGrid == null || selectedItem == null)
        {
            return false;
        }

        SlottedItemGrid slottedGrid = targetGrid as SlottedItemGrid;

        if (TryInstallDraggedWeaponModule(targetGrid, tileGridPosition))
        {
            return true;
        }

        if (TryMergeDraggedItemIntoStack(targetGrid, tileGridPosition))
        {
            return true;
        }

        if (slottedGrid != null && slottedGrid.ShouldSplitStackOnPlace(selectedItem, tileGridPosition.x, tileGridPosition.y))
        {
            return TryPlaceSingleItemFromStack(slottedGrid, tileGridPosition);
        }

        if (targetGrid.CanPlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        if (_tryPrepareSlotRestrictionsForPlacement(targetGrid, selectedItem) == false)
        {
            return false;
        }

        targetGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y);
        _refreshWeightState();
        FinishDraggingItem();
        return true;
    }

    public bool CanOpenCountDragWindow(InventoryGrid targetGrid, Vector2Int tileGridPosition)
    {
        InventoryItem selectedItem = _dragState.SelectedItem;

        if (targetGrid == null || selectedItem == null || selectedItem.IsStackable == false || selectedItem.CurrentAmount <= 1)
        {
            return false;
        }

        if (targetGrid == _dragState.OriginGrid && tileGridPosition == _dragState.OriginPosition)
        {
            return false;
        }

        return TryGetStackMergeTarget(targetGrid, tileGridPosition, out _) || targetGrid.CanPlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y);
    }

    public bool TryPlaceDraggedItemAmount(InventoryGrid targetGrid, Vector2Int tileGridPosition, int amount)
    {
        InventoryItem selectedItem = _dragState.SelectedItem;

        if (targetGrid == null || selectedItem == null)
        {
            return false;
        }

        int normalizedAmount = Mathf.Clamp(amount, 1, selectedItem.CurrentAmount);

        if (normalizedAmount >= selectedItem.CurrentAmount)
        {
            return TryPlaceDraggedItem(targetGrid, tileGridPosition);
        }

        if (TryMergeDraggedItemAmountIntoStack(targetGrid, tileGridPosition, normalizedAmount))
        {
            return true;
        }

        return TryPlaceSplitItemFromStack(targetGrid, tileGridPosition, normalizedAmount);
    }

    public bool TryGetStackMergeTarget(InventoryGrid targetGrid, Vector2Int tileGridPosition, out InventoryItem targetStack)
    {
        targetStack = null;

        if (targetGrid == null || _dragState.SelectedItem == null || _dragState.SelectedItem.IsStackable == false || targetGrid.CanMergeStackAt(tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        targetStack = targetGrid.GetItem(tileGridPosition.x, tileGridPosition.y);
        return targetStack != null && targetStack != _dragState.SelectedItem && targetStack.CanStackWith(_dragState.SelectedItem.ItemData);
    }

    public void DestroyInventoryItem(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        _itemRegistry.Unregister(item);
        UnityEngine.Object.Destroy(item.gameObject);
    }

    public bool ReturnDraggedItemToOrigin()
    {
        if (_dragState.SelectedItem == null)
        {
            return true;
        }

        if (_dragState.OriginGrid == null)
        {
            return false;
        }

        if (_dragState.ReturnToOrigin() == false)
        {
            return false;
        }

        _refreshWeightState();
        return true;
    }

    public bool TryDetachItemFromGrid(InventoryGrid grid, InventoryItem item)
    {
        if (grid == null || item == null)
        {
            return false;
        }

        if (_canDetachItemWithSlotRestrictions(grid, item) == false)
        {
            return false;
        }

        Vector2Int position = new Vector2Int(item.GridPositionX, item.GridPositionY);
        InventoryItem pickedItem = grid.PickUpItem(position.x, position.y);

        if (pickedItem == null)
        {
            return false;
        }

        if (pickedItem == item)
        {
            return true;
        }

        grid.PlaceItem(pickedItem, position.x, position.y);
        return false;
    }

    private bool TryMergeDraggedItemIntoStack(InventoryGrid targetGrid, Vector2Int tileGridPosition)
    {
        if (TryGetStackMergeTarget(targetGrid, tileGridPosition, out InventoryItem targetStack) == false)
        {
            return false;
        }

        InventoryItem mergedItem = _dragState.SelectedItem;
        targetStack.AddAmount(mergedItem.CurrentAmount);
        DestroyInventoryItem(mergedItem);
        _refreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private bool TryMergeDraggedItemAmountIntoStack(InventoryGrid targetGrid, Vector2Int tileGridPosition, int amount)
    {
        if (TryGetStackMergeTarget(targetGrid, tileGridPosition, out InventoryItem targetStack) == false)
        {
            return false;
        }

        InventoryItem selectedItem = _dragState.SelectedItem;

        if (selectedItem == null || _dragState.OriginGrid == null || amount <= 0 || amount >= selectedItem.CurrentAmount)
        {
            return false;
        }

        int originalAmount = selectedItem.CurrentAmount;
        bool originalRotated = selectedItem.IsRotated;

        selectedItem.SetAmount(originalAmount - amount);
        selectedItem.SetRotated(_dragState.OriginRotated);

        if (_dragState.OriginGrid.CanPlaceItem(selectedItem, _dragState.OriginPosition.x, _dragState.OriginPosition.y) == false)
        {
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        targetStack.AddAmount(amount);
        _dragState.OriginGrid.PlaceItem(selectedItem, _dragState.OriginPosition.x, _dragState.OriginPosition.y);

        _refreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private bool TryInstallDraggedWeaponModule(InventoryGrid targetGrid, Vector2Int tileGridPosition)
    {
        InventoryItem moduleItem = _dragState.SelectedItem;

        if (moduleItem == null || moduleItem.ItemData == null || moduleItem.ItemData.ItemType != ItemType.Module || _tryInstallWeaponModule == null)
        {
            return false;
        }

        InventoryItem weaponItem = targetGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (weaponItem == null || weaponItem == moduleItem || _tryInstallWeaponModule(targetGrid, weaponItem, moduleItem.ItemData) == false)
        {
            return false;
        }

        DestroyInventoryItem(moduleItem);
        FinishDraggingItem();

        if (_weaponModulesChanged != null)
        {
            _weaponModulesChanged(weaponItem);
        }
        else
        {
            _refreshWeightState();
        }

        return true;
    }

    private bool TryPlaceSingleItemFromStack(SlottedItemGrid slottedGrid, Vector2Int tileGridPosition)
    {
        if (slottedGrid == null || _dragState.SelectedItem == null || _dragState.OriginGrid == null)
        {
            return false;
        }

        InventoryItem selectedItem = _dragState.SelectedItem;

        if (slottedGrid == _dragState.OriginGrid && tileGridPosition == _dragState.OriginPosition)
        {
            return false;
        }

        if (slottedGrid.CanPlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        int originalAmount = selectedItem.CurrentAmount;
        bool originalRotated = selectedItem.IsRotated;

        selectedItem.SetAmount(originalAmount - 1);
        selectedItem.SetRotated(_dragState.OriginRotated);

        if (_dragState.OriginGrid.CanPlaceItem(selectedItem, _dragState.OriginPosition.x, _dragState.OriginPosition.y) == false)
        {
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        InventoryItem singleItem = _itemFactory.Create(selectedItem.ItemData, 1, null);

        if (slottedGrid.CanPlaceItem(singleItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            UnityEngine.Object.Destroy(singleItem.gameObject);
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        slottedGrid.PlaceItem(singleItem, tileGridPosition.x, tileGridPosition.y);
        _dragState.OriginGrid.PlaceItem(selectedItem, _dragState.OriginPosition.x, _dragState.OriginPosition.y);

        _itemRegistry.Register(singleItem);
        _refreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private bool TryPlaceSplitItemFromStack(InventoryGrid targetGrid, Vector2Int tileGridPosition, int amount)
    {
        if (targetGrid == null || _dragState.SelectedItem == null || _dragState.OriginGrid == null)
        {
            return false;
        }

        InventoryItem selectedItem = _dragState.SelectedItem;

        if (targetGrid == _dragState.OriginGrid && tileGridPosition == _dragState.OriginPosition)
        {
            return false;
        }

        if (targetGrid.CanPlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        int originalAmount = selectedItem.CurrentAmount;
        bool originalRotated = selectedItem.IsRotated;

        if (amount <= 0 || amount >= originalAmount)
        {
            return false;
        }

        selectedItem.SetAmount(originalAmount - amount);
        selectedItem.SetRotated(_dragState.OriginRotated);

        if (_dragState.OriginGrid.CanPlaceItem(selectedItem, _dragState.OriginPosition.x, _dragState.OriginPosition.y) == false)
        {
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        InventoryItem splitItem = _itemFactory.Create(selectedItem.ItemData, amount, selectedItem.CurrentDurabilityPercent, null);
        splitItem.SetRotated(originalRotated);

        if (targetGrid.CanPlaceItem(splitItem, tileGridPosition.x, tileGridPosition.y) == false || IsOverlappingOriginPlacement(targetGrid, selectedItem, splitItem, tileGridPosition) || _tryPrepareSlotRestrictionsForPlacement(targetGrid, splitItem) == false)
        {
            UnityEngine.Object.Destroy(splitItem.gameObject);
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        targetGrid.PlaceItem(splitItem, tileGridPosition.x, tileGridPosition.y);
        _dragState.OriginGrid.PlaceItem(selectedItem, _dragState.OriginPosition.x, _dragState.OriginPosition.y);

        _itemRegistry.Register(splitItem);
        _refreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private bool IsOverlappingOriginPlacement(InventoryGrid targetGrid, InventoryItem originItem, InventoryItem targetItem, Vector2Int targetPosition)
    {
        if (targetGrid != _dragState.OriginGrid || originItem == null || targetItem == null)
        {
            return false;
        }

        RectInt originRect = new(_dragState.OriginPosition.x, _dragState.OriginPosition.y, originItem.Width, originItem.Height);
        RectInt targetRect = new(targetPosition.x, targetPosition.y, targetItem.Width, targetItem.Height);
        return originRect.xMin < targetRect.xMax && originRect.xMax > targetRect.xMin && originRect.yMin < targetRect.yMax && originRect.yMax > targetRect.yMin;
    }

    private void FinishDraggingItem() => _dragState.Finish();
}
