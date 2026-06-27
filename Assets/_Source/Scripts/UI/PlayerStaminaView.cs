using System;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

internal sealed class PlayerStaminaView : IView<PlayerStaminaViewModel>, IDisposable
{
    private const float COLOR_EPSILON = 0.002f;

    private readonly Image _fillImage;
    private readonly float _colorTweenDuration;
    private IDisposable _normalizedStaminaSubscription;
    private IDisposable _fillColorSubscription;
    private Tween _fillColorTween;
    private Color _targetFillColor;
    private bool _hasTargetFillColor;

    public PlayerStaminaView(Image fillImage, float colorTweenDuration)
    {
        _fillImage = fillImage;
        _colorTweenDuration = Mathf.Max(0f, colorTweenDuration);
    }

    public void Bind(PlayerStaminaViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _normalizedStaminaSubscription = viewModel.NormalizedStamina.Subscribe(SetNormalizedStamina);
        _fillColorSubscription = viewModel.FillColor.Subscribe(SetFillColor);
    }

    public void Unbind()
    {
        _normalizedStaminaSubscription?.Dispose();
        _normalizedStaminaSubscription = null;
        _fillColorSubscription?.Dispose();
        _fillColorSubscription = null;
        KillFillColorTween();
        _hasTargetFillColor = false;
    }

    public void Dispose()
    {
        Unbind();
    }

    private void SetNormalizedStamina(float normalizedStamina)
    {
        if (_fillImage == null)
        {
            return;
        }

        _fillImage.fillAmount = Mathf.Clamp01(normalizedStamina);
    }

    private void SetFillColor(Color fillColor)
    {
        if (_fillImage == null)
        {
            return;
        }

        if (_hasTargetFillColor && IsColorApproximately(_targetFillColor, fillColor))
        {
            return;
        }

        _targetFillColor = fillColor;
        _hasTargetFillColor = true;
        KillFillColorTween();

        if (_colorTweenDuration <= 0f)
        {
            _fillImage.color = fillColor;
            return;
        }

        _fillColorTween = _fillImage.DOColor(fillColor, _colorTweenDuration).SetEase(Ease.OutSine);
    }

    private void KillFillColorTween()
    {
        if (_fillColorTween != null && _fillColorTween.IsActive())
        {
            _fillColorTween.Kill(false);
        }

        _fillColorTween = null;
    }

    private static bool IsColorApproximately(Color first, Color second)
    {
        return Mathf.Abs(first.r - second.r) <= COLOR_EPSILON && Mathf.Abs(first.g - second.g) <= COLOR_EPSILON && Mathf.Abs(first.b - second.b) <= COLOR_EPSILON && Mathf.Abs(first.a - second.a) <= COLOR_EPSILON;
    }
}
