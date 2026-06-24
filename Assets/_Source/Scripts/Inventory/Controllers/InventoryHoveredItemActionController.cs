using System;
using UnityEngine;

internal sealed class InventoryHoveredItemActionController
{
    private readonly IInventoryInput _playerInput;
    private readonly InventoryDragState _dragState;
    private readonly Func<InventoryGrid> _getSelectedItemGrid;
    private readonly Func<Vector2Int> _getTileGridPosition;
    private readonly Func<bool> _isContextMenuOpen;
    private readonly InventoryQuickActionService _quickActionService;
    private readonly Func<InventoryGrid, InventoryItem, bool, bool> _tryDropItem;
    private readonly InventoryHoverInfoController _hoverInfoController;
    private readonly Action _hideItemTooltip;
    private readonly Action _hideContextMenu;

    public InventoryHoveredItemActionController(IInventoryInput playerInput, InventoryDragState dragState, Func<InventoryGrid> getSelectedItemGrid, Func<Vector2Int> getTileGridPosition, Func<bool> isContextMenuOpen, InventoryQuickActionService quickActionService, Func<InventoryGrid, InventoryItem, bool, bool> tryDropItem, InventoryHoverInfoController hoverInfoController, Action hideItemTooltip, Action hideContextMenu)
    {
        _playerInput = playerInput;
        _dragState = dragState;
        _getSelectedItemGrid = getSelectedItemGrid;
        _getTileGridPosition = getTileGridPosition;
        _isContextMenuOpen = isContextMenuOpen;
        _quickActionService = quickActionService;
        _tryDropItem = tryDropItem;
        _hoverInfoController = hoverInfoController;
        _hideItemTooltip = hideItemTooltip;
        _hideContextMenu = hideContextMenu;
    }

    public bool TryHandleQuickItemAction()
    {
        bool quickMoveToInventory = _playerInput.IsInventoryQuickMoveModifierHeld();
        bool quickEquip = _playerInput.IsInventoryQuickEquipModifierHeld();

        if (quickMoveToInventory == false && quickEquip == false)
        {
            return false;
        }

        InventoryGrid selectedItemGrid = _getSelectedItemGrid();

        if (_dragState.HasSelectedItem || selectedItemGrid == null)
        {
            return true;
        }

        _hideContextMenu();

        Vector2Int tileGridPosition = _getTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return true;
        }

        if (quickEquip)
        {
            _quickActionService.TryQuickEquipItem(selectedItemGrid, item);
        }
        else
        {
            _quickActionService.TryQuickMoveItemToInventory(selectedItemGrid, item);
        }

        HideHoverInfo();

        return true;
    }

    public bool TryHandleHoveredItemDropInput()
    {
        if (_playerInput.IsInventoryDropPressed() == false)
        {
            return false;
        }

        InventoryGrid selectedItemGrid = _getSelectedItemGrid();

        if (_dragState.HasSelectedItem || selectedItemGrid == null || _isContextMenuOpen())
        {
            return true;
        }

        Vector2Int tileGridPosition = _getTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return true;
        }

        bool dropWholeStack = _playerInput.IsInventoryDropStackModifierHeld() && item.IsStackable;
        _tryDropItem(selectedItemGrid, item, dropWholeStack);

        HideHoverInfo();

        return true;
    }

    private void HideHoverInfo()
    {
        _hoverInfoController.HideHighlight();
        _hideItemTooltip();
    }
}
