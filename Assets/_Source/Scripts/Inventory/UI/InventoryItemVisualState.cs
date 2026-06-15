using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemVisualState
{
    private bool _hasVisualSizeOverride;
    private int _visualWidthOverride = 1;
    private int _visualHeightOverride = 1;
    private Sprite _iconOverride;

    public bool OverlayTextsVisible { get; private set; } = true;
    public bool CellVisualsVisible { get; private set; } = true;
    public IReadOnlyList<ItemIconPart> RuntimeIconParts { get; private set; }
    public bool HasVisualSizeOverride => _hasVisualSizeOverride;

    public void SetRuntimeIconParts(IReadOnlyList<ItemIconPart> runtimeIconParts) => RuntimeIconParts = runtimeIconParts;

    public void ApplySlotVisual(ItemData itemData, int slotWidth, int slotHeight, bool useGeneratedSlotIcon)
    {
        _hasVisualSizeOverride = true;
        _visualWidthOverride = Mathf.Max(1, slotWidth);
        _visualHeightOverride = Mathf.Max(1, slotHeight);
        _iconOverride = itemData == null ? null : useGeneratedSlotIcon ? itemData.GetSlotIcon(_visualWidthOverride, _visualHeightOverride, RuntimeIconParts) : itemData.GetIcon(RuntimeIconParts);
    }

    public void RestoreDefaultVisual()
    {
        _hasVisualSizeOverride = false;
        _visualWidthOverride = 1;
        _visualHeightOverride = 1;
        _iconOverride = null;
    }

    public void SetCellVisualsVisible(bool visible) => CellVisualsVisible = visible;
    public void SetOverlayTextsVisible(bool visible) => OverlayTextsVisible = visible;
    public Sprite GetIcon(ItemData itemData) => _iconOverride != null ? _iconOverride : itemData == null ? null : itemData.GetIcon(RuntimeIconParts);
    public int GetVisualWidth(int baseWidth) => _hasVisualSizeOverride ? _visualWidthOverride : baseWidth;
    public int GetVisualHeight(int baseHeight) => _hasVisualSizeOverride ? _visualHeightOverride : baseHeight;
}