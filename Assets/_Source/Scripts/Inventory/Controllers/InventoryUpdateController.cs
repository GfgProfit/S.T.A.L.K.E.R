using System;

internal sealed class InventoryUpdateController
{
    private readonly IInventoryInput _playerInput;
    private readonly Func<bool> _isOpen;
    private readonly Func<InventoryGrid> _selectedGrid;
    private readonly InventoryHoverInfoController _hoverInfoController;
    private readonly InventoryContextMenuController _contextMenuController;
    private readonly Action _toggleInventory;
    private readonly Action _hideItemInfoPanel;
    private readonly Action _hideContextMenu;
    private readonly Action _dragItemIcon;
    private readonly Action _releaseDraggedItem;
    private readonly Action _rotateSelectedItem;
    private readonly Func<bool> _tryHandleHoveredItemDropInput;
    private readonly Action _handleHighlight;
    private readonly Func<bool> _tryHandleQuickItemAction;
    private readonly Action _beginDrag;

    public InventoryUpdateController(IInventoryInput playerInput, Func<bool> isOpen, Func<InventoryGrid> selectedGrid, InventoryHoverInfoController hoverInfoController, InventoryContextMenuController contextMenuController, Action toggleInventory, Action hideItemInfoPanel, Action hideContextMenu, Action dragItemIcon, Action releaseDraggedItem, Action rotateSelectedItem, Func<bool> tryHandleHoveredItemDropInput, Action handleHighlight, Func<bool> tryHandleQuickItemAction, Action beginDrag)
    {
        _playerInput = playerInput;
        _isOpen = isOpen;
        _selectedGrid = selectedGrid;
        _hoverInfoController = hoverInfoController;
        _contextMenuController = contextMenuController;
        _toggleInventory = toggleInventory;
        _hideItemInfoPanel = hideItemInfoPanel;
        _hideContextMenu = hideContextMenu;
        _dragItemIcon = dragItemIcon;
        _releaseDraggedItem = releaseDraggedItem;
        _rotateSelectedItem = rotateSelectedItem;
        _tryHandleHoveredItemDropInput = tryHandleHoveredItemDropInput;
        _handleHighlight = handleHighlight;
        _tryHandleQuickItemAction = tryHandleQuickItemAction;
        _beginDrag = beginDrag;
    }

    public void Tick()
    {
        if (_playerInput.IsInventoryPressed())
        {
            _toggleInventory();
        }

        if (_isOpen() == false)
        {
            HideTransientUi();
            return;
        }

        _dragItemIcon();
        _contextMenuController.CloseIfPointerIsOutsideRadius();

        if (_contextMenuController.HandleInput())
        {
            return;
        }

        if (_playerInput.IsInventoryPrimaryActionReleased())
        {
            _releaseDraggedItem();
        }

        if (_playerInput.IsInventoryRotatePressed())
        {
            _rotateSelectedItem();
        }

        if (_selectedGrid() == null)
        {
            _hoverInfoController.HideHighlight();
            _hideItemInfoPanel();

            return;
        }

        if (_tryHandleHoveredItemDropInput())
        {
            return;
        }

        _handleHighlight();

        if (_playerInput.IsInventoryPrimaryActionPressed() == false)
        {
            return;
        }

        if (_tryHandleQuickItemAction())
        {
            return;
        }

        _beginDrag();
    }

    private void HideTransientUi()
    {
        _hoverInfoController.HideHighlight();
        _hideItemInfoPanel();
        _hideContextMenu();
    }
}
