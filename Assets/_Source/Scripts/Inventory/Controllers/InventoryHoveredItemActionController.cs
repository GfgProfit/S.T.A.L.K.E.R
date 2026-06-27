using System;
using UnityEngine;

internal sealed class InventoryHoveredItemActionController
{
    private readonly IInventoryInput _playerInput;
    private readonly InventoryDragState _dragState;
    private readonly Func<InventoryGrid> _getSelectedItemGrid;
    private readonly Func<Vector2Int> _getTileGridPosition;
    private readonly Func<bool> _isContextMenuOpen;
    private readonly Func<InventoryGrid, InventoryItem, bool, bool> _tryDropItem;
    private readonly InventoryHoverInfoController _hoverInfoController;
    private readonly Action _hideItemTooltip;

    public InventoryHoveredItemActionController(IInventoryInput playerInput, InventoryDragState dragState, Func<InventoryGrid> getSelectedItemGrid, Func<Vector2Int> getTileGridPosition, Func<bool> isContextMenuOpen, Func<InventoryGrid, InventoryItem, bool, bool> tryDropItem, InventoryHoverInfoController hoverInfoController, Action hideItemTooltip)
    {
        _playerInput = playerInput;
        _dragState = dragState;
        _getSelectedItemGrid = getSelectedItemGrid;
        _getTileGridPosition = getTileGridPosition;
        _isContextMenuOpen = isContextMenuOpen;
        _tryDropItem = tryDropItem;
        _hoverInfoController = hoverInfoController;
        _hideItemTooltip = hideItemTooltip;
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
