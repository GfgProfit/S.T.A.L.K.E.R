using R3;
using UnityEngine;

public sealed class ItemTooltipViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _isVisible = new();
    private readonly ReactiveProperty<Sprite> _icon = new();
    private readonly ReactiveProperty<Vector2> _iconSize = new();
    private readonly ReactiveProperty<string> _itemNameText = new(string.Empty);
    private readonly ReactiveProperty<string> _typeText = new(string.Empty);
    private readonly ReactiveProperty<string> _weightText = new(string.Empty);
    private readonly ReactiveProperty<string> _durabilityText = new(string.Empty);
    private readonly ReactiveProperty<bool> _showDurability = new();
    private readonly ReactiveProperty<string> _descriptionText = new(string.Empty);

    public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
    public ReadOnlyReactiveProperty<Sprite> Icon => _icon;
    public ReadOnlyReactiveProperty<Vector2> IconSize => _iconSize;
    public ReadOnlyReactiveProperty<string> ItemNameText => _itemNameText;
    public ReadOnlyReactiveProperty<string> TypeText => _typeText;
    public ReadOnlyReactiveProperty<string> WeightText => _weightText;
    public ReadOnlyReactiveProperty<string> DurabilityText => _durabilityText;
    public ReadOnlyReactiveProperty<bool> ShowDurability => _showDurability;
    public ReadOnlyReactiveProperty<string> DescriptionText => _descriptionText;

    public void Show(ItemTooltipData item, int descriptionWordsPerLine, GameProjectSettings settings)
    {
        if (item.IsValid == false)
        {
            Hide();
            return;
        }

        _icon.Value = item.ItemData.GetIcon(item.InstalledModules);
        _iconSize.Value = new(item.BaseWidth * ItemGrid.TILE_SIZE_WIDTH, item.BaseHeight * ItemGrid.TILE_SIZE_HEIGHT);
        _itemNameText.Value = item.ItemData.ItemName;
        _typeText.Value = ItemTooltipTextFormatter.FormatType(item.ItemData);
        _weightText.Value = ItemTooltipTextFormatter.FormatWeight(item.Amount, item.UnitWeight, item.TotalWeight);
        _showDurability.Value = item.HasDurability;
        _durabilityText.Value = item.HasDurability ? ItemTooltipTextFormatter.FormatDurability(item.DurabilityPercent, settings.GetDurabilityColor(item.DurabilityPercent)) : string.Empty;
        _descriptionText.Value = ItemTooltipTextFormatter.WrapDescription(item.ItemData.Description, descriptionWordsPerLine);
        _isVisible.Value = true;
    }

    public void Hide()
    {
        _isVisible.Value = false;
    }

    protected override void DisposeManaged()
    {
        _isVisible.Dispose();
        _icon.Dispose();
        _iconSize.Dispose();
        _itemNameText.Dispose();
        _typeText.Dispose();
        _weightText.Dispose();
        _durabilityText.Dispose();
        _showDurability.Dispose();
        _descriptionText.Dispose();
    }
}
