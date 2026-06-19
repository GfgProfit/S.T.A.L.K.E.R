using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

internal sealed class InventoryItemVisualState
{
    private bool _hasVisualSizeOverride;
    private bool _useGeneratedSlotIcon;
    private int _visualWidthOverride = 1;
    private int _visualHeightOverride = 1;

    public bool OverlayTextsVisible { get; private set; } = true;
    public bool CellVisualsVisible { get; private set; } = true;
    public bool IsCompatibilityHighlighted { get; private set; }
    public Color CompatibilityHighlightColor { get; private set; } = Color.clear;

    public bool HasVisualSizeOverride => _hasVisualSizeOverride;

    public void ApplySlotVisual(int slotWidth, int slotHeight, bool useGeneratedSlotIcon)
    {
        _hasVisualSizeOverride = true;
        _useGeneratedSlotIcon = useGeneratedSlotIcon;
        _visualWidthOverride = Mathf.Max(1, slotWidth);
        _visualHeightOverride = Mathf.Max(1, slotHeight);
    }

    public void RestoreDefaultVisual()
    {
        _hasVisualSizeOverride = false;
        _useGeneratedSlotIcon = false;
        _visualWidthOverride = 1;
        _visualHeightOverride = 1;
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

    public UniTask<Sprite> GetIconAsync(ItemData itemData, int width, int height, IReadOnlyList<ItemData> installedModules, CancellationToken cancellationToken)
    {
        if (itemData == null)
        {
            return UniTask.FromResult<Sprite>(null);
        }

        if (_hasVisualSizeOverride == false)
        {
            return itemData.GetIconAsync(width, height, installedModules, cancellationToken);
        }

        return _useGeneratedSlotIcon
            ? itemData.GetSlotIconAsync(_visualWidthOverride, _visualHeightOverride, installedModules, cancellationToken)
            : itemData.GetIconAsync(installedModules, cancellationToken);
    }

    public Sprite GetCachedIcon(ItemData itemData, int width, int height, IReadOnlyList<ItemData> installedModules)
    {
        if (itemData == null)
        {
            return null;
        }

        if (_hasVisualSizeOverride == false)
        {
            return itemData.GetIcon(width, height, installedModules);
        }

        return _useGeneratedSlotIcon
            ? itemData.GetSlotIcon(_visualWidthOverride, _visualHeightOverride, installedModules)
            : itemData.GetIcon(installedModules);
    }

    public int GetVisualWidth(int baseWidth) => _hasVisualSizeOverride ? _visualWidthOverride : baseWidth;
    public int GetVisualHeight(int baseHeight) => _hasVisualSizeOverride ? _visualHeightOverride : baseHeight;
}
