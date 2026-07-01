using System;

internal sealed class InventoryUpdateController
{
    private readonly IInventoryInput _playerInput;
    private readonly Func<bool> _isOpen;
    private readonly Func<bool> _isItemInfoPanelOpen;
    private readonly Func<InventoryGrid> _selectedGrid;
    private readonly InventoryHoverInfoController _hoverInfoController;
    private readonly InventoryContextMenuController _contextMenuController;
    private readonly Action _toggleInventory;
    private readonly Action _closeInventory;
    private readonly Action _hideItemTooltip;
    private readonly Action _hideItemInfoPanel;
    private readonly Action _hideContextMenu;
    private readonly Func<bool> _isCountDragWindowOpen;
    private readonly Action _cancelCountDragWindow;
    private readonly Action _dragItemIcon;
    private readonly Action _releaseDraggedItem;
    private readonly Action _rotateSelectedItem;
    private readonly Func<bool> _tryHandleHoveredItemDropInput;
    private readonly Action _handleHighlight;
    private readonly Action _beginDrag;

    public InventoryUpdateController(IInventoryInput playerInput, Func<bool> isOpen, Func<bool> isItemInfoPanelOpen, Func<InventoryGrid> selectedGrid, InventoryHoverInfoController hoverInfoController, InventoryContextMenuController contextMenuController, Action toggleInventory, Action closeInventory, Action hideItemTooltip, Action hideItemInfoPanel, Action hideContextMenu, Func<bool> isCountDragWindowOpen, Action cancelCountDragWindow, Action dragItemIcon, Action releaseDraggedItem, Action rotateSelectedItem, Func<bool> tryHandleHoveredItemDropInput, Action handleHighlight, Action beginDrag)
    {
        _playerInput = playerInput;
        _isOpen = isOpen;
        _isItemInfoPanelOpen = isItemInfoPanelOpen;
        _selectedGrid = selectedGrid;
        _hoverInfoController = hoverInfoController;
        _contextMenuController = contextMenuController;
        _toggleInventory = toggleInventory;
        _closeInventory = closeInventory;
        _hideItemTooltip = hideItemTooltip;
        _hideItemInfoPanel = hideItemInfoPanel;
        _hideContextMenu = hideContextMenu;
        _isCountDragWindowOpen = isCountDragWindowOpen;
        _cancelCountDragWindow = cancelCountDragWindow;
        _dragItemIcon = dragItemIcon;
        _releaseDraggedItem = releaseDraggedItem;
        _rotateSelectedItem = rotateSelectedItem;
        _tryHandleHoveredItemDropInput = tryHandleHoveredItemDropInput;
        _handleHighlight = handleHighlight;
        _beginDrag = beginDrag;
    }

    public void Tick()
    {
        if (_isOpen() && _playerInput.IsEscapePressed())
        {
            if (_isCountDragWindowOpen())
            {
                _cancelCountDragWindow();
                return;
            }

            if (_isItemInfoPanelOpen())
            {
                _hideItemInfoPanel();
                return;
            }

            _closeInventory();
            return;
        }

        if (_playerInput.IsInventoryPressed())
        {
            _toggleInventory();
        }

        if (_isOpen() == false)
        {
            HideTransientUi();
            return;
        }

        if (_isCountDragWindowOpen())
        {
            return;
        }

        if (_isItemInfoPanelOpen())
        {
            HideItemInteractionUi();
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

            if (_isCountDragWindowOpen())
            {
                return;
            }
        }

        if (_playerInput.IsInventoryRotatePressed())
        {
            _rotateSelectedItem();
        }

        if (_selectedGrid() == null)
        {
            _hoverInfoController.HideHighlight();
            _hideItemTooltip();

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

        _beginDrag();
    }

    private void HideTransientUi()
    {
        HideItemInteractionUi();
        _hideItemInfoPanel();
        _hideContextMenu();
    }

    private void HideItemInteractionUi()
    {
        _hoverInfoController.HideHighlight();
        _hideItemTooltip();
        _hideContextMenu();
    }
}
