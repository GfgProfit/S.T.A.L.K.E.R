using R3;
using UnityEngine;

public sealed class PlayerStaminaViewModel : ViewModelBase
{
    private readonly ReactiveProperty<float> _normalizedStamina = new(1f);
    private readonly ReactiveProperty<Color> _fillColor = new(Color.white);

    public ReadOnlyReactiveProperty<float> NormalizedStamina => _normalizedStamina;
    public ReadOnlyReactiveProperty<Color> FillColor => _fillColor;

    public void SetNormalizedStamina(float normalizedStamina)
    {
        _normalizedStamina.Value = Mathf.Clamp01(normalizedStamina);
    }

    public void SetFillColor(Color fillColor)
    {
        _fillColor.Value = fillColor;
    }

    protected override void DisposeManaged()
    {
        _normalizedStamina.Dispose();
        _fillColor.Dispose();
    }
}
