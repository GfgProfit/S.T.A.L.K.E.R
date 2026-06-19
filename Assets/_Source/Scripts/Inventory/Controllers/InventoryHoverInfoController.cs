using System;
using UnityEngine;

internal sealed class InventoryHoverInfoController
{
    private readonly InventoryHighlight _inventoryHighlight;
    private readonly ItemInfoPanel _itemInfoPanel;
    private readonly Func<bool> _isContextMenuOpen;
    private readonly InventoryItemCompatibilityService _compatibilityService;

    public InventoryHoverInfoController(InventoryHighlight inventoryHighlight, ItemInfoPanel itemInfoPanel, Func<bool> isContextMenuOpen, InventoryItemCompatibilityService compatibilityService)
    {
        _inventoryHighlight = inventoryHighlight;
        _itemInfoPanel = itemInfoPanel;
        _isContextMenuOpen = isContextMenuOpen;
        _compatibilityService = compatibilityService;
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
            _compatibilityService.ShowCompatibleItems(HighlightedItem);

            if (HighlightedItem != null)
            {
                _inventoryHighlight.Show(true);
                SetHighlightTransform(selectedItemGrid, HighlightedItem, HighlightedItem.GridPositionX, HighlightedItem.GridPositionY);
                ShowItemInfoPanel(HighlightedItem);
            }
            else
            {
                HideHighlight();
                HideItemInfoPanel();
            }

            return;
        }

        _compatibilityService.Clear();
        HideItemInfoPanel();

        bool canMergeStack = tryGetStackMergeTarget(selectedItemGrid, positionOnGrid, out InventoryItem stackMergeTarget);
        bool canPlaceItem = selectedItemGrid.CanPlaceItem(selectedItem, positionOnGrid.x, positionOnGrid.y);

        _inventoryHighlight.Show(canPlaceItem || canMergeStack);

        if (canMergeStack)
        {
            SetHighlightTransform(selectedItemGrid, stackMergeTarget, stackMergeTarget.GridPositionX, stackMergeTarget.GridPositionY);
        }
        else
        {
            SetHighlightTransform(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }

    public void RefreshHighlightedItemInfo(InventoryItem item)
    {
        if (_itemInfoPanel != null && HighlightedItem == item)
        {
            _itemInfoPanel.Show(CreateTooltipData(item));
        }
    }

    public void HideHighlight()
    {
        _inventoryHighlight.Show(false);
        HighlightedItem = null;
        _compatibilityService.Clear();
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

        _itemInfoPanel.Show(CreateTooltipData(item));
    }

    private void SetHighlightTransform(InventoryGrid grid, InventoryItem item, int posX, int posY)
    {
        _inventoryHighlight.SetSize(grid.GetHighlightSize(item, posX, posY));
        _inventoryHighlight.SetPosition(grid.GetHighlightPosition(item, posX, posY));
    }

    private static ItemTooltipData CreateTooltipData(InventoryItem item)
    {
        if (item == null)
        {
            return default;
        }

        return new(item.ItemData, item.CurrentAmount, item.UnitWeight, item.TotalWeight, item.HasDurability, item.CurrentDurabilityPercent, item.BaseWidth, item.BaseHeight, item.InstalledModules);
    }
}
