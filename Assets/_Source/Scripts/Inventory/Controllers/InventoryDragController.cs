using System;
using UnityEngine;

internal sealed class InventoryDragController
{
    private readonly InventoryDragState _dragState;
    private readonly InventoryDragPlacementService _dragPlacementService;
    private readonly IInventoryInput _playerInput;
    private readonly Transform _canvasTransform;
    private readonly Func<InventoryGrid> _getSelectedItemGrid;
    private readonly Func<InventoryGrid, InventoryItem, bool> _canDetachItemWithSlotRestrictions;
    private readonly Action _hideContextMenu;
    private readonly Action _hideItemInfoPanel;
    private readonly Action _refreshWeightState;

    public InventoryDragController(InventoryDragState dragState, InventoryDragPlacementService dragPlacementService, IInventoryInput playerInput, Transform canvasTransform, Func<InventoryGrid> getSelectedItemGrid, Func<InventoryGrid, InventoryItem, bool> canDetachItemWithSlotRestrictions, Action hideContextMenu, Action hideItemInfoPanel, Action refreshWeightState)
    {
        _dragState = dragState;
        _dragPlacementService = dragPlacementService;
        _playerInput = playerInput;
        _canvasTransform = canvasTransform;
        _getSelectedItemGrid = getSelectedItemGrid;
        _canDetachItemWithSlotRestrictions = canDetachItemWithSlotRestrictions;
        _hideContextMenu = hideContextMenu;
        _hideItemInfoPanel = hideItemInfoPanel;
        _refreshWeightState = refreshWeightState;
    }

    public void BeginDrag()
    {
        if (_dragState.HasSelectedItem)
        {
            return;
        }

        _hideContextMenu();

        InventoryGrid selectedItemGrid = _getSelectedItemGrid();
        Vector2Int tileGridPosition = GetTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return;
        }

        if (_canDetachItemWithSlotRestrictions(selectedItemGrid, item) == false)
        {
            return;
        }

        _dragState.CaptureOrigin(selectedItemGrid, item);
        PickupItem(selectedItemGrid, tileGridPosition);
    }

    public void RotateSelectedItem()
    {
        if (_dragState.SelectedItem == null)
        {
            return;
        }

        _dragState.SelectedItem.Rotate();
    }

    public void ReleaseDraggedItem()
    {
        if (_dragState.SelectedItem == null)
        {
            return;
        }

        InventoryGrid selectedItemGrid = _getSelectedItemGrid();

        if (selectedItemGrid != null)
        {
            Vector2Int tileGridPosition = GetTileGridPosition();

            if (_dragPlacementService.TryPlaceDraggedItem(selectedItemGrid, tileGridPosition))
            {
                return;
            }
        }

        ReturnDraggedItemToOrigin();
    }

    public Vector2Int GetTileGridPosition()
    {
        InventoryGrid selectedItemGrid = _getSelectedItemGrid();
        return selectedItemGrid.GetTileGridPosition(_playerInput.GetPointerPosition(), _dragState.SelectedItem);
    }

    public bool TryGetStackMergeTarget(InventoryGrid targetGrid, Vector2Int tileGridPosition, out InventoryItem targetStack) => _dragPlacementService.TryGetStackMergeTarget(targetGrid, tileGridPosition, out targetStack);
    public void DestroyInventoryItem(InventoryItem item) => _dragPlacementService.DestroyInventoryItem(item);
    public bool ReturnDraggedItemToOrigin() => _dragPlacementService.ReturnDraggedItemToOrigin();
    public void ItemIconDrag() => _dragState.Drag(_playerInput.GetPointerPosition());
    public bool TryDetachItemFromGrid(InventoryGrid grid, InventoryItem item) => _dragPlacementService.TryDetachItemFromGrid(grid, item);

    private void PickupItem(InventoryGrid selectedItemGrid, Vector2Int tileGridPosition)
    {
        InventoryItem selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);

        if (selectedItem != null)
        {
            StartDraggingItem(selectedItem);
            _refreshWeightState();
        }
    }

    private void StartDraggingItem(InventoryItem item)
    {
        _hideItemInfoPanel();
        _dragState.StartDragging(item, _canvasTransform, _playerInput.GetPointerPosition());
    }
}