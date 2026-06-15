using System;
using UnityEngine;

internal sealed class InventoryHoverInfoController
{
    private readonly InventoryHighlight _inventoryHighlight;
    private readonly ItemInfoPanel _itemInfoPanel;
    private readonly Func<bool> _isContextMenuOpen;

    public InventoryHoverInfoController(InventoryHighlight inventoryHighlight, ItemInfoPanel itemInfoPanel, Func<bool> isContextMenuOpen)
    {
        _inventoryHighlight = inventoryHighlight;
        _itemInfoPanel = itemInfoPanel;
        _isContextMenuOpen = isContextMenuOpen;
    }

    public InventoryItem HighlightedItem { get; private set; }

    public void HandleHighlight(InventoryGrid selectedItemGrid, InventoryItem selectedItem, Vector2Int positionOnGrid, TryGetStackMergeTargetDelegate tryGetStackMergeTarget)
    {
        if (selectedItemGrid == null)
        {
            HideHighlight();
            HideItemInfoPanel();

            return;
        }

        if (selectedItem == null)
        {
            HighlightedItem = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);

            if (HighlightedItem != null)
            {
                _inventoryHighlight.Show(true);
                _inventoryHighlight.SetSize(selectedItemGrid, HighlightedItem);
                _inventoryHighlight.SetPosition(selectedItemGrid, HighlightedItem);
                ShowItemInfoPanel(HighlightedItem);
            }
            else
            {
                HideHighlight();
                HideItemInfoPanel();
            }

            return;
        }

        HideItemInfoPanel();

        bool canMergeStack = tryGetStackMergeTarget(selectedItemGrid, positionOnGrid, out InventoryItem stackMergeTarget);
        bool canPlaceItem = selectedItemGrid.CanPlaceItem(selectedItem, positionOnGrid.x, positionOnGrid.y);

        _inventoryHighlight.Show(canPlaceItem || canMergeStack);

        if (canMergeStack)
        {
            _inventoryHighlight.SetSize(selectedItemGrid, stackMergeTarget);
            _inventoryHighlight.SetPosition(selectedItemGrid, stackMergeTarget);
        }
        else
        {
            _inventoryHighlight.SetSize(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
            _inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }

    public void RefreshHighlightedItemInfo(InventoryItem item)
    {
        if (_itemInfoPanel != null && HighlightedItem == item)
        {
            _itemInfoPanel.Show(item);
        }
    }

    public void HideHighlight()
    {
        _inventoryHighlight.Show(false);
        HighlightedItem = null;
    }

    public void HideItemInfoPanel()
    {
        if (_itemInfoPanel == null)
        {
            return;
        }

        _itemInfoPanel.Hide();
    }

    private void ShowItemInfoPanel(InventoryItem item)
    {
        if (_itemInfoPanel == null)
        {
            return;
        }

        if (_isContextMenuOpen())
        {
            HideItemInfoPanel();

            return;
        }

        _itemInfoPanel.Show(item);
    }
}