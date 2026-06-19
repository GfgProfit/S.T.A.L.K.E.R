using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public sealed class QuickUseHudSlotViewModel : ViewModelBase
{
    private const int HUD_SLOT_ICON_WIDTH = 1;
    private const int HUD_SLOT_ICON_HEIGHT = 1;

    private readonly ReactiveProperty<bool> _enabled = new();
    private readonly ReactiveProperty<string> _keyText = new(string.Empty);
    private readonly ReactiveProperty<Sprite> _icon = new();
    private readonly ReactiveProperty<bool> _iconVisible = new();
    private readonly ReactiveProperty<string> _countText = new(string.Empty);
    private readonly ReactiveProperty<bool> _countVisible = new();
    private readonly ReactiveProperty<Color> _backgroundColor = new(Color.clear);
    private readonly ReactiveProperty<Color> _borderColor = new(Color.clear);
    private readonly CancellationTokenSource _disposeCancellation = new();
    private int _iconRequestVersion;

    public ReadOnlyReactiveProperty<bool> Enabled => _enabled;
    public ReadOnlyReactiveProperty<string> KeyText => _keyText;
    public ReadOnlyReactiveProperty<Sprite> Icon => _icon;
    public ReadOnlyReactiveProperty<bool> IconVisible => _iconVisible;
    public ReadOnlyReactiveProperty<string> CountText => _countText;
    public ReadOnlyReactiveProperty<bool> CountVisible => _countVisible;
    public ReadOnlyReactiveProperty<Color> BackgroundColor => _backgroundColor;
    public ReadOnlyReactiveProperty<Color> BorderColor => _borderColor;

    public void SetKeyText(string keyText)
    {
        _keyText.Value = keyText ?? string.Empty;
    }

    public void SetItem(InventoryItem item)
    {
        bool hasItem = item != null && item.ItemData != null;

        if (hasItem == false)
        {
            Clear();
            return;
        }

        bool showCount = item.IsStackable && item.CurrentAmount > 1;

        _backgroundColor.Value = item.ItemData.IconBackgroundColor;
        _borderColor.Value = item.ItemData.IconCellGridBorderColor;
        _enabled.Value = true;
        _icon.Value = item.ItemData.GetSlotIcon(HUD_SLOT_ICON_WIDTH, HUD_SLOT_ICON_HEIGHT, item.InstalledModules);
        _iconVisible.Value = _icon.Value != null;
        _countText.Value = showCount ? $"x{item.CurrentAmount}" : string.Empty;
        _countVisible.Value = showCount;

        int requestVersion = ++_iconRequestVersion;
        UpdateIconAsync(item.ItemData, CopyModules(item.InstalledModules), requestVersion).Forget(Debug.LogException);
    }

    private void Clear()
    {
        _iconRequestVersion++;
        _enabled.Value = false;
        _icon.Value = null;
        _iconVisible.Value = false;
        _countText.Value = string.Empty;
        _countVisible.Value = false;
        _backgroundColor.Value = Color.clear;
        _borderColor.Value = Color.clear;
    }

    protected override void DisposeManaged()
    {
        _iconRequestVersion++;
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();
        _enabled.Dispose();
        _keyText.Dispose();
        _icon.Dispose();
        _iconVisible.Dispose();
        _countText.Dispose();
        _countVisible.Dispose();
        _backgroundColor.Dispose();
        _borderColor.Dispose();
    }

    private async UniTask UpdateIconAsync(ItemData itemData, ItemData[] installedModules, int requestVersion)
    {
        (bool isCanceled, Sprite icon) = await itemData
            .GetSlotIconAsync(HUD_SLOT_ICON_WIDTH, HUD_SLOT_ICON_HEIGHT, installedModules, _disposeCancellation.Token)
            .SuppressCancellationThrow();

        if (isCanceled || requestVersion != _iconRequestVersion)
        {
            return;
        }

        _icon.Value = icon;
        _iconVisible.Value = icon != null;
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
