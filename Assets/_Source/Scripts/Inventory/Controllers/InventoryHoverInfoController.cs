using System;
using UnityEngine;

internal sealed class InventoryHoverInfoController
{
    private readonly InventoryHighlight _inventoryHighlight;
    private readonly ItemTooltip _itemTooltip;
    private readonly Func<bool> _isContextMenuOpen;
    private readonly InventoryItemCompatibilityService _compatibilityService;

    public InventoryHoverInfoController(InventoryHighlight inventoryHighlight, ItemTooltip itemTooltip, Func<bool> isContextMenuOpen, InventoryItemCompatibilityService compatibilityService)
    {
        _inventoryHighlight = inventoryHighlight;
        _itemTooltip = itemTooltip;
        _isContextMenuOpen = isContextMenuOpen;
        _compatibilityService = compatibilityService;
    }

    public InventoryItem HighlightedItem { get; private set; }

    public void HandleHighlight(InventoryGrid selectedItemGrid, InventoryItem selectedItem, Vector2Int positionOnGrid, TryGetStackMergeTargetDelegate tryGetStackMergeTarget, bool useCountDragHighlight, Color countDragHighlightColor)
    {
        if (selectedItemGrid == null)
        {
            HideHighlight();
            HideItemTooltip();

            return;
        }

        if (selectedItem == null)
        {
            _inventoryHighlight.SetDefaultColor();
            HighlightedItem = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            _compatibilityService.ShowCompatibleItems(HighlightedItem);

            if (HighlightedItem != null)
            {
                _inventoryHighlight.Show(true);
                SetHighlightTransform(selectedItemGrid, HighlightedItem, HighlightedItem.GridPositionX, HighlightedItem.GridPositionY);
                ShowItemTooltip(HighlightedItem);
            }
            else
            {
                HideHighlight();
                HideItemTooltip();
            }

            return;
        }

        _compatibilityService.Clear();
        HideItemTooltip();

        if (useCountDragHighlight)
        {
            _inventoryHighlight.SetColor(countDragHighlightColor);
        }
        else
        {
            _inventoryHighlight.SetDefaultColor();
        }

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
        if (_itemTooltip != null && HighlightedItem == item)
        {
            ShowItemTooltip(item);
        }
    }

    public void RefreshHighlightedItem(InventoryItem item)
    {
        if (HighlightedItem != item)
        {
            return;
        }

        _compatibilityService.ShowCompatibleItems(item);
        ShowItemTooltip(item);
    }

    public void HideHighlight()
    {
        _inventoryHighlight.Show(false);
        HighlightedItem = null;
        _compatibilityService.Clear();
    }

    public void HideItemTooltip()
    {
        if (_itemTooltip == null)
        {
            return;
        }

        _itemTooltip.Hide();
    }

    private void ShowItemTooltip(InventoryItem item)
    {
        if (_itemTooltip == null)
        {
            return;
        }

        if (_isContextMenuOpen())
        {
            HideItemTooltip();

            return;
        }

        _itemTooltip.Show(CreateTooltipData(item));
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
