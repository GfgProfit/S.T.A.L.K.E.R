using System;
using UnityEngine;

internal sealed class InventoryContextMenuController
{
    private readonly InventoryItemContextMenu _contextMenu;
    private readonly IInventoryInput _playerInput;
    private readonly Func<InventoryGrid> _getSelectedGrid;
    private readonly Func<InventoryItem> _getSelectedItem;
    private readonly Func<Vector2Int> _getTileGridPosition;
    private readonly Action _hideItemInfoPanel;
    private readonly Func<InventoryGrid, InventoryItem, bool, bool> _dropItem;
    private InventoryGrid _contextMenuGrid;
    private InventoryItem _contextMenuItem;

    public InventoryContextMenuController(InventoryItemContextMenu contextMenu, IInventoryInput playerInput, Func<InventoryGrid> getSelectedGrid, Func<InventoryItem> getSelectedItem, Func<Vector2Int> getTileGridPosition, Action hideItemInfoPanel, Func<InventoryGrid, InventoryItem, bool, bool> dropItem)
    {
        _contextMenu = contextMenu;
        _playerInput = playerInput;
        _getSelectedGrid = getSelectedGrid;
        _getSelectedItem = getSelectedItem;
        _getTileGridPosition = getTileGridPosition;
        _hideItemInfoPanel = hideItemInfoPanel;
        _dropItem = dropItem;
    }

    public bool IsOpen => _contextMenu != null && _contextMenu.IsOpen;

    public void Initialize() => _contextMenu?.Initialize(DropSingleItem, DropItemStack);

    public bool HandleInput()
    {
        if (_contextMenu != null && _contextMenu.IsOpen)
        {
            if (_playerInput.IsInventoryPrimaryActionPressed())
            {
                if (_contextMenu.ContainsScreenPoint(_playerInput.GetPointerPosition()))
                {
                    return true;
                }

                Hide();

                return true;
            }

            if (_playerInput.IsInventorySecondaryActionPressed() && _contextMenu.ContainsScreenPoint(_playerInput.GetPointerPosition()))
            {
                return true;
            }
        }

        if (_playerInput.IsInventorySecondaryActionPressed())
        {
            OpenAtCursor();

            return true;
        }

        return false;
    }

    public void CloseIfPointerIsOutsideRadius()
    {
        if (_contextMenu == null || _contextMenu.ShouldCloseForPointer(_playerInput.GetPointerPosition()) == false)
        {
            return;
        }

        Hide();
    }

    public void Hide()
    {
        _contextMenu?.Hide();
        _contextMenuGrid = null;
        _contextMenuItem = null;
    }

    private void OpenAtCursor()
    {
        Hide();

        InventoryGrid selectedGrid = _getSelectedGrid();

        if (_getSelectedItem() != null || selectedGrid == null || _contextMenu == null)
        {
            return;
        }

        Vector2Int tileGridPosition = _getTileGridPosition();
        InventoryItem item = selectedGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return;
        }

        _contextMenuGrid = selectedGrid;
        _contextMenuItem = item;

        _hideItemInfoPanel();
        _contextMenu.Show(item, _playerInput.GetPointerPosition());
    }

    private void DropSingleItem() => DropContextMenuItem(false);
    private void DropItemStack() => DropContextMenuItem(true);

    private void DropContextMenuItem(bool wholeStack)
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();

        _dropItem(grid, item, wholeStack);
    }
}