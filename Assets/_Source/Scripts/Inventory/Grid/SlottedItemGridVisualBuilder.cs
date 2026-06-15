using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class SlottedItemGridVisualBuilder
{
    private readonly InventoryController _inventoryController;
    private readonly SlottedItemGrid _grid;
    private readonly RectTransform _rectTransform;
    private readonly Image _sourceImage;
    private readonly bool _hideSourceImage;
    private readonly GameProjectSettings _visualSettings;
    private readonly Func<InventorySlotDefinition, Vector2> _getSlotVisualSize;

    public SlottedItemGridVisualBuilder(InventoryController inventoryController, SlottedItemGrid grid, RectTransform rectTransform, Image sourceImage, bool hideSourceImage, GameProjectSettings visualSettings, Func<InventorySlotDefinition, Vector2> getSlotVisualSize)
    {
        _inventoryController = inventoryController;
        _grid = grid;
        _rectTransform = rectTransform;
        _sourceImage = sourceImage;
        _hideSourceImage = hideSourceImage;
        _visualSettings = visualSettings;
        _getSlotVisualSize = getSlotVisualSize;
    }

    public RectTransform CreateSlotVisuals(IReadOnlyList<SlottedGridSlotState> slots)
    {
        GameObject rootObject = new("Slot Visuals");
        rootObject.transform.SetParent(_rectTransform, false);

        RectTransform slotVisualRoot = rootObject.AddComponent<RectTransform>();
        slotVisualRoot.anchorMin = new(0f, 1f);
        slotVisualRoot.anchorMax = new(0f, 1f);
        slotVisualRoot.pivot = new(0f, 1f);
        slotVisualRoot.anchoredPosition = Vector2.zero;
        slotVisualRoot.sizeDelta = _rectTransform.sizeDelta;
        slotVisualRoot.SetAsFirstSibling();

        for (int i = 0; i < slots.Count; i++)
        {
            CreateSlotVisual(slotVisualRoot, slots[i]);
        }

        if (_hideSourceImage && _sourceImage != null && _sourceImage.transform == _rectTransform)
        {
            _sourceImage.enabled = false;
        }

        return slotVisualRoot;
    }

    private void CreateSlotVisual(RectTransform slotVisualRoot, SlottedGridSlotState slot)
    {
        GameObject slotObject = new($"Slot {slot.Definition.Id}");
        slotObject.transform.SetParent(slotVisualRoot, false);

        RectTransform slotRectTransform = slotObject.AddComponent<RectTransform>();
        slotObject.AddComponent<CanvasRenderer>();
        slot.VisualRoot = slotRectTransform;
        slotRectTransform.anchorMin = new(0f, 1f);
        slotRectTransform.anchorMax = new(0f, 1f);
        slotRectTransform.pivot = new(0f, 1f);
        slotRectTransform.anchoredPosition = new(slot.VisualPosition.x, -slot.VisualPosition.y);
        slotRectTransform.sizeDelta = _getSlotVisualSize(slot.Definition);

        Image slotImage = slotObject.AddComponent<Image>();
        CopyImageSettings(slotImage, _sourceImage);
        slotImage.raycastTarget = true;

        CreateCellGridLines(slotRectTransform, slot.Definition);

        GridInteract gridInteract = slotObject.AddComponent<GridInteract>();
        gridInteract.Initialize(_inventoryController, _grid);
    }

    private void CopyImageSettings(Image target, Image source)
    {
        if (source == null)
        {
            target.color = Color.white;
            return;
        }

        target.sprite = source.sprite;
        target.color = source.color;
        target.material = source.material;
        target.type = source.type;
        target.preserveAspect = source.preserveAspect;
        target.fillCenter = source.fillCenter;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
    }

    private void CreateCellGridLines(RectTransform slotRectTransform, InventorySlotDefinition definition)
    {
        if (_visualSettings == null || _visualSettings.ShowCellGrid == false)
        {
            return;
        }

        Vector2 slotSize = _getSlotVisualSize(definition);
        int firstVerticalLine = _visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastVerticalLine = _visualSettings.ShowCellGridBorder ? definition.Width : definition.Width - 1;
        int firstHorizontalLine = _visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastHorizontalLine = _visualSettings.ShowCellGridBorder ? definition.Height : definition.Height - 1;

        for (int x = firstVerticalLine; x <= lastVerticalLine; x++)
        {
            bool isBorderLine = x == 0 || x == definition.Width;

            CreateGridLine(slotRectTransform, $"Vertical Grid Line {x}", new(x * ItemGrid.TILE_SIZE_WIDTH, 0f), new(_visualSettings.GetLineThickness(isBorderLine), slotSize.y), new(0.5f, 1f), _visualSettings.GetLineColor(isBorderLine));
        }

        for (int y = firstHorizontalLine; y <= lastHorizontalLine; y++)
        {
            bool isBorderLine = y == 0 || y == definition.Height;

            CreateGridLine(slotRectTransform, $"Horizontal Grid Line {y}", new(0f, -y * ItemGrid.TILE_SIZE_HEIGHT), new(slotSize.x, _visualSettings.GetLineThickness(isBorderLine)), new(0f, 0.5f), _visualSettings.GetLineColor(isBorderLine));
        }
    }

    private static void CreateGridLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 pivot, Color color)
    {
        GameObject lineObject = new(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform lineRectTransform = lineObject.AddComponent<RectTransform>();
        lineObject.AddComponent<CanvasRenderer>();
        lineRectTransform.anchorMin = new(0f, 1f);
        lineRectTransform.anchorMax = new(0f, 1f);
        lineRectTransform.pivot = pivot;
        lineRectTransform.anchoredPosition = anchoredPosition;
        lineRectTransform.sizeDelta = size;

        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }
}