using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    private readonly CancellationTokenSource _disposeCancellation = new();
    private int _iconRequestVersion;

    public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
    public ReadOnlyReactiveProperty<Sprite> Icon => _icon;
    public ReadOnlyReactiveProperty<Vector2> IconSize => _iconSize;
    public ReadOnlyReactiveProperty<string> ItemNameText => _itemNameText;
    public ReadOnlyReactiveProperty<string> TypeText => _typeText;
    public ReadOnlyReactiveProperty<string> WeightText => _weightText;
    public ReadOnlyReactiveProperty<string> DurabilityText => _durabilityText;
    public ReadOnlyReactiveProperty<bool> ShowDurability => _showDurability;
    public ReadOnlyReactiveProperty<string> DescriptionText => _descriptionText;

    public void Show(ItemTooltipData item, GameProjectSettings settings)
    {
        if (item.IsValid == false)
        {
            Hide();
            return;
        }

        _icon.Value = item.ItemData.GetIcon(item.BaseWidth, item.BaseHeight, item.InstalledModules);
        _iconSize.Value = new(item.BaseWidth * ItemGrid.TILE_SIZE_WIDTH, item.BaseHeight * ItemGrid.TILE_SIZE_HEIGHT);
        _itemNameText.Value = item.ItemData.ItemName;
        _typeText.Value = ItemTooltipTextFormatter.FormatType(item.ItemData);
        _weightText.Value = ItemTooltipTextFormatter.FormatWeight(item.Amount, item.UnitWeight, item.TotalWeight);
        _showDurability.Value = item.HasDurability;
        _durabilityText.Value = item.HasDurability ? ItemTooltipTextFormatter.FormatDurability(item.DurabilityPercent, settings.GetDurabilityColor(item.DurabilityPercent)) : string.Empty;
        _descriptionText.Value = item.ItemData.Description?.Trim() ?? string.Empty;
        _isVisible.Value = true;

        int requestVersion = ++_iconRequestVersion;
        UpdateIconAsync(item.ItemData, item.BaseWidth, item.BaseHeight, CopyModules(item.InstalledModules), requestVersion).Forget(Debug.LogException);
    }

    public void Hide()
    {
        _iconRequestVersion++;
        _isVisible.Value = false;
    }

    protected override void DisposeManaged()
    {
        _iconRequestVersion++;
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();
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

    private async UniTask UpdateIconAsync(ItemData itemData, int width, int height, ItemData[] installedModules, int requestVersion)
    {
        (bool isCanceled, Sprite icon) = await itemData
            .GetIconAsync(width, height, installedModules, _disposeCancellation.Token)
            .SuppressCancellationThrow();

        if (isCanceled || requestVersion != _iconRequestVersion)
        {
            return;
        }

        _icon.Value = icon;
    }

    private static ItemData[] CopyModules(IReadOnlyList<ItemData> installedModules)
    {
        if (installedModules == null || installedModules.Count == 0)
        {
            return Array.Empty<ItemData>();
        }

        ItemData[] modules = new ItemData[installedModules.Count];

        for (int i = 0; i < installedModules.Count; i++)
        {
            modules[i] = installedModules[i];
        }

        return modules;
    }
}
