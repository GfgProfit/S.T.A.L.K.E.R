using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public sealed class FirstPersonWeaponAmmoHudViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _visible = new();
    private readonly ReactiveProperty<string> _ammoText = new("0/0");
    private readonly ReactiveProperty<Sprite> _ammoIcon = new();
    private readonly ReactiveProperty<bool> _ammoIconVisible = new();
    private readonly ReactiveProperty<Vector2> _ammoIconSize = new(Vector2.zero);
    private readonly CancellationTokenSource _disposeCancellation = new();
    private int _iconRequestVersion;

    public ReadOnlyReactiveProperty<bool> Visible => _visible;
    public ReadOnlyReactiveProperty<string> AmmoText => _ammoText;
    public ReadOnlyReactiveProperty<Sprite> AmmoIcon => _ammoIcon;
    public ReadOnlyReactiveProperty<bool> AmmoIconVisible => _ammoIconVisible;
    public ReadOnlyReactiveProperty<Vector2> AmmoIconSize => _ammoIconSize;

    public void SetAmmo(ItemData ammoData, int loadedAmmoAmount, int inventoryAmmoAmount)
    {
        _visible.Value = true;

        if (ammoData == null)
        {
            _ammoText.Value = $"{Mathf.Max(0, loadedAmmoAmount)}/{Mathf.Max(0, inventoryAmmoAmount)}";
            _iconRequestVersion++;
            _ammoIcon.Value = null;
            _ammoIconVisible.Value = false;
            _ammoIconSize.Value = Vector2.zero;
            return;
        }

        _ammoText.Value = $"{Mathf.Max(0, loadedAmmoAmount)}/{Mathf.Max(0, inventoryAmmoAmount)} ({ammoData.ShortName})";
        _ammoIcon.Value = ammoData.GetIcon();
        _ammoIconVisible.Value = _ammoIcon.Value != null;
        _ammoIconSize.Value = new Vector2(ammoData.Width * ItemGrid.TILE_SIZE_WIDTH, ammoData.Height * ItemGrid.TILE_SIZE_HEIGHT);
        int requestVersion = ++_iconRequestVersion;
        UpdateIconAsync(ammoData, requestVersion).Forget(Debug.LogException);
    }

    public void Clear()
    {
        _iconRequestVersion++;
        _visible.Value = false;
        _ammoText.Value = "0/0";
        _ammoIcon.Value = null;
        _ammoIconVisible.Value = false;
        _ammoIconSize.Value = Vector2.zero;
    }

    protected override void DisposeManaged()
    {
        _iconRequestVersion++;
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();
        _visible.Dispose();
        _ammoText.Dispose();
        _ammoIcon.Dispose();
        _ammoIconVisible.Dispose();
        _ammoIconSize.Dispose();
    }

    private async UniTask UpdateIconAsync(ItemData ammoData, int requestVersion)
    {
        (bool isCanceled, Sprite icon) = await ammoData
            .GetIconAsync(cancellationToken: _disposeCancellation.Token)
            .SuppressCancellationThrow();

        if (isCanceled || requestVersion != _iconRequestVersion)
        {
            return;
        }

        _ammoIcon.Value = icon;
        _ammoIconVisible.Value = icon != null;
    }
}
