using UnityEngine;

internal sealed class InventoryItemVisualState
{
    private bool _hasVisualSizeOverride;
    private int _visualWidthOverride = 1;
    private int _visualHeightOverride = 1;
    private Sprite _iconOverride;

    public bool OverlayTextsVisible { get; private set; } = true;
    public bool CellVisualsVisible { get; private set; } = true;
    public bool IsCompatibilityHighlighted { get; private set; }
    public Color CompatibilityHighlightColor { get; private set; } = Color.clear;

    public bool HasVisualSizeOverride => _hasVisualSizeOverride;

    public void ApplySlotVisual(ItemData itemData, int slotWidth, int slotHeight, bool useGeneratedSlotIcon, System.Collections.Generic.IReadOnlyList<ItemData> installedModules)
    {
        _hasVisualSizeOverride = true;
        _visualWidthOverride = Mathf.Max(1, slotWidth);
        _visualHeightOverride = Mathf.Max(1, slotHeight);
        _iconOverride = itemData == null ? null : useGeneratedSlotIcon ? itemData.GetSlotIcon(_visualWidthOverride, _visualHeightOverride, installedModules) : itemData.GetIcon(installedModules);
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

    public bool SetCompatibilityHighlight(bool highlighted, Color color)
    {
        if (IsCompatibilityHighlighted == highlighted && (highlighted == false || CompatibilityHighlightColor == color))
        {
            return false;
        }

        IsCompatibilityHighlighted = highlighted;
        CompatibilityHighlightColor = color;
        return true;
    }

    public Sprite GetIcon(ItemData itemData, int width, int height, System.Collections.Generic.IReadOnlyList<ItemData> installedModules) => _iconOverride != null ? _iconOverride : itemData == null ? null : itemData.GetIcon(width, height, installedModules);
    public int GetVisualWidth(int baseWidth) => _hasVisualSizeOverride ? _visualWidthOverride : baseWidth;
    public int GetVisualHeight(int baseHeight) => _hasVisualSizeOverride ? _visualHeightOverride : baseHeight;
}
