using R3;
using UnityEngine;

public sealed class FirstPersonWeaponAmmoHudViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _visible = new();
    private readonly ReactiveProperty<string> _ammoText = new("0/0");
    private readonly ReactiveProperty<Sprite> _ammoIcon = new();
    private readonly ReactiveProperty<bool> _ammoIconVisible = new();
    private readonly ReactiveProperty<Vector2> _ammoIconSize = new(Vector2.zero);

    public ReadOnlyReactiveProperty<bool> Visible => _visible;
    public ReadOnlyReactiveProperty<string> AmmoText => _ammoText;
    public ReadOnlyReactiveProperty<Sprite> AmmoIcon => _ammoIcon;
    public ReadOnlyReactiveProperty<bool> AmmoIconVisible => _ammoIconVisible;
    public ReadOnlyReactiveProperty<Vector2> AmmoIconSize => _ammoIconSize;

    public void SetAmmo(ItemData ammoData, int loadedAmmoAmount, int inventoryAmmoAmount)
    {
        _visible.Value = true;
        _ammoText.Value = $"{Mathf.Max(0, loadedAmmoAmount)}/{Mathf.Max(0, inventoryAmmoAmount)} ({ammoData.ShortName})";

        if (ammoData == null)
        {
            _ammoIcon.Value = null;
            _ammoIconVisible.Value = false;
            _ammoIconSize.Value = Vector2.zero;
            return;
        }

        Sprite icon = ammoData.GetIcon();
        _ammoIcon.Value = icon;
        _ammoIconVisible.Value = icon != null;
        _ammoIconSize.Value = new Vector2(ammoData.Width * ItemGrid.TILE_SIZE_WIDTH, ammoData.Height * ItemGrid.TILE_SIZE_HEIGHT);
    }

    public void Clear()
    {
        _visible.Value = false;
        _ammoText.Value = "0/0";
        _ammoIcon.Value = null;
        _ammoIconVisible.Value = false;
        _ammoIconSize.Value = Vector2.zero;
    }

    protected override void DisposeManaged()
    {
        _visible.Dispose();
        _ammoText.Dispose();
        _ammoIcon.Dispose();
        _ammoIconVisible.Dispose();
        _ammoIconSize.Dispose();
    }
}
