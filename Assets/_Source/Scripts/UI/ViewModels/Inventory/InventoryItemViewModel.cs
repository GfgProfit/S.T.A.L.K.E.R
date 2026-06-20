using R3;
using UnityEngine;

public sealed class InventoryItemViewModel : ViewModelBase
{
    private readonly ReactiveProperty<Sprite> _icon = new();
    private readonly ReactiveProperty<bool> _iconLoading = new();
    private readonly ReactiveProperty<Color> _cellBackgroundColor = new(Color.clear);
    private readonly ReactiveProperty<bool> _cellBackgroundVisible = new();
    private readonly ReactiveProperty<string> _countText = new(string.Empty);
    private readonly ReactiveProperty<bool> _countVisible = new();
    private readonly ReactiveProperty<string> _shortNameText = new(string.Empty);
    private readonly ReactiveProperty<Color> _shortNameColor = new(Color.white);
    private readonly ReactiveProperty<bool> _shortNameVisible = new();
    private readonly ReactiveProperty<bool> _durabilityVisible = new();
    private readonly ReactiveProperty<float> _durabilityPercent = new(100f);
    private readonly ReactiveProperty<Sprite> _statusIcon = new();
    private readonly ReactiveProperty<bool> _statusIconVisible = new();

    public ReadOnlyReactiveProperty<Sprite> Icon => _icon;
    public ReadOnlyReactiveProperty<bool> IconLoading => _iconLoading;
    public ReadOnlyReactiveProperty<Color> CellBackgroundColor => _cellBackgroundColor;
    public ReadOnlyReactiveProperty<bool> CellBackgroundVisible => _cellBackgroundVisible;
    public ReadOnlyReactiveProperty<string> CountText => _countText;
    public ReadOnlyReactiveProperty<bool> CountVisible => _countVisible;
    public ReadOnlyReactiveProperty<string> ShortNameText => _shortNameText;
    public ReadOnlyReactiveProperty<Color> ShortNameColor => _shortNameColor;
    public ReadOnlyReactiveProperty<bool> ShortNameVisible => _shortNameVisible;
    public ReadOnlyReactiveProperty<bool> DurabilityVisible => _durabilityVisible;
    public ReadOnlyReactiveProperty<float> DurabilityPercent => _durabilityPercent;
    public ReadOnlyReactiveProperty<Sprite> StatusIcon => _statusIcon;
    public ReadOnlyReactiveProperty<bool> StatusIconVisible => _statusIconVisible;

    public void SetIcon(Sprite icon)
    {
        _icon.Value = icon;
    }

    public void SetIconLoading(bool loading)
    {
        _iconLoading.Value = loading;
    }

    public void SetCellBackground(ItemData itemData, bool cellVisualsVisible, bool compatibilityHighlighted, Color compatibilityHighlightColor)
    {
        Color backgroundColor = itemData == null
            ? Color.clear
            : compatibilityHighlighted ? compatibilityHighlightColor : itemData.IconBackgroundColor;
        _cellBackgroundColor.Value = backgroundColor;
        _cellBackgroundVisible.Value = cellVisualsVisible && itemData != null && backgroundColor.a > 0f;
    }

    public void SetOverlayTexts(ItemData itemData, int currentAmount, bool overlayTextsVisible)
    {
        bool showCount = overlayTextsVisible && itemData != null && itemData.IsStackable && currentAmount > 1;
        string shortName = itemData == null ? string.Empty : itemData.ShortName;
        bool showShortName = overlayTextsVisible && string.IsNullOrWhiteSpace(shortName) == false;

        _countText.Value = showCount ? $"x{currentAmount}" : string.Empty;
        _countVisible.Value = showCount;
        _shortNameText.Value = showShortName ? shortName : string.Empty;
        _shortNameColor.Value = itemData == null ? Color.white : itemData.ShortNameColor;
        _shortNameVisible.Value = showShortName;
    }

    public void SetDurability(bool cellVisualsVisible, bool hasDurability, float durabilityPercent)
    {
        _durabilityVisible.Value = cellVisualsVisible && hasDurability;
        _durabilityPercent.Value = durabilityPercent;
    }

    public void SetStatusIcon(ItemData itemData, Sprite questStatusIcon, bool cellVisualsVisible)
    {
        bool showStatusIcon = cellVisualsVisible && itemData != null && itemData.ItemType == ItemType.Quest && questStatusIcon != null;
        _statusIcon.Value = showStatusIcon ? questStatusIcon : null;
        _statusIconVisible.Value = showStatusIcon;
    }

    protected override void DisposeManaged()
    {
        _icon.Dispose();
        _iconLoading.Dispose();
        _cellBackgroundColor.Dispose();
        _cellBackgroundVisible.Dispose();
        _countText.Dispose();
        _countVisible.Dispose();
        _shortNameText.Dispose();
        _shortNameColor.Dispose();
        _shortNameVisible.Dispose();
        _durabilityVisible.Dispose();
        _durabilityPercent.Dispose();
        _statusIcon.Dispose();
        _statusIconVisible.Dispose();
    }
}
