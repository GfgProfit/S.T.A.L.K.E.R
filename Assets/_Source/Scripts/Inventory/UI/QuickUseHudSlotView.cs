using System;
using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuickUseHudSlotView : MonoBehaviour, IView<QuickUseHudSlotViewModel>
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _keyText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private List<Graphic> _slotStateGraphics = new();
    [SerializeField] [Range(0f, 1f)] private float _disabledAlpha = 0.35f;

    private readonly List<Color> _enabledGraphicColors = new();
    private IDisposable _enabledSubscription;
    private IDisposable _keyTextSubscription;
    private IDisposable _iconSubscription;
    private IDisposable _iconVisibleSubscription;
    private IDisposable _countTextSubscription;
    private IDisposable _countVisibleSubscription;
    private IDisposable _backgroundColorSubscription;
    private IDisposable _borderColorSubscription;
    private bool _enabled;
    private Color _backgroundColor = Color.clear;
    private Color _borderColor = Color.clear;

    private void Awake()
    {
        CacheGraphicColors();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(QuickUseHudSlotViewModel viewModel)
    {
        Unbind();
        CacheGraphicColors();

        if (viewModel == null)
        {
            return;
        }

        _enabledSubscription = viewModel.Enabled.Subscribe(SetEnabled);
        _keyTextSubscription = viewModel.KeyText.Subscribe(SetKeyText);
        _iconSubscription = viewModel.Icon.Subscribe(SetIcon);
        _iconVisibleSubscription = viewModel.IconVisible.Subscribe(SetIconVisible);
        _countTextSubscription = viewModel.CountText.Subscribe(SetCountText);
        _countVisibleSubscription = viewModel.CountVisible.Subscribe(SetCountVisible);
        _backgroundColorSubscription = viewModel.BackgroundColor.Subscribe(SetBackgroundColor);
        _borderColorSubscription = viewModel.BorderColor.Subscribe(SetBorderColor);
    }

    public void Unbind()
    {
        _enabledSubscription?.Dispose();
        _keyTextSubscription?.Dispose();
        _iconSubscription?.Dispose();
        _iconVisibleSubscription?.Dispose();
        _countTextSubscription?.Dispose();
        _countVisibleSubscription?.Dispose();
        _backgroundColorSubscription?.Dispose();
        _borderColorSubscription?.Dispose();
        _enabledSubscription = null;
        _keyTextSubscription = null;
        _iconSubscription = null;
        _iconVisibleSubscription = null;
        _countTextSubscription = null;
        _countVisibleSubscription = null;
        _backgroundColorSubscription = null;
        _borderColorSubscription = null;
    }

    private void CacheGraphicColors()
    {
        if (_enabledGraphicColors.Count == _slotStateGraphics.Count)
        {
            return;
        }

        _enabledGraphicColors.Clear();

        for (int i = 0; i < _slotStateGraphics.Count; i++)
        {
            Graphic graphic = _slotStateGraphics[i];
            _enabledGraphicColors.Add(graphic == null ? Color.white : graphic.color);
        }
    }

    private void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        ApplySlotState();
    }

    private void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
        ApplySlotState();
    }

    private void SetBorderColor(Color color)
    {
        _borderColor = color;
        ApplySlotState();
    }

    private void ApplySlotState()
    {
        CacheGraphicColors();

        for (int i = 0; i < _slotStateGraphics.Count; i++)
        {
            Graphic graphic = _slotStateGraphics[i];

            if (graphic == null)
            {
                continue;
            }

            Color enabledColor = i < _enabledGraphicColors.Count ? _enabledGraphicColors[i] : graphic.color;
            Color targetColor = _enabled ? GetEnabledColor(i, enabledColor) : new Color(enabledColor.r, enabledColor.g, enabledColor.b, enabledColor.a * _disabledAlpha);
            graphic.color = targetColor;
        }
    }

    private Color GetEnabledColor(int graphicIndex, Color fallbackColor)
    {
        if (graphicIndex == 0 && _backgroundColor.a > 0f)
        {
            return _backgroundColor;
        }

        if (graphicIndex > 0 && _borderColor.a > 0f)
        {
            return _borderColor;
        }

        return fallbackColor;
    }

    private void SetKeyText(string keyText)
    {
        if (_keyText == null)
        {
            return;
        }

        _keyText.text = keyText ?? string.Empty;
        _keyText.rectTransform.SetAsLastSibling();
    }

    private void SetIcon(Sprite icon)
    {
        if (_icon == null)
        {
            return;
        }

        _icon.sprite = icon;
        _icon.raycastTarget = false;
    }

    private void SetIconVisible(bool visible)
    {
        if (_icon == null)
        {
            return;
        }

        _icon.enabled = visible;
        _icon.gameObject.SetActive(visible);
    }

    private void SetCountText(string text)
    {
        if (_countText == null)
        {
            return;
        }

        _countText.text = text;
    }

    private void SetCountVisible(bool visible)
    {
        if (_countText == null)
        {
            return;
        }

        _countText.gameObject.SetActive(visible);

        if (visible)
        {
            _countText.rectTransform.SetAsLastSibling();
        }
    }
}
