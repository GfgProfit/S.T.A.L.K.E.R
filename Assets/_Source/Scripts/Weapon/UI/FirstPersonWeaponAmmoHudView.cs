using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FirstPersonWeaponAmmoHudView : MonoBehaviour, IView<FirstPersonWeaponAmmoHudViewModel>
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private Image _ammoIcon;
    [SerializeField] private RectTransform _ammoIconRectTransform;

    private IDisposable _visibleSubscription;
    private IDisposable _ammoTextSubscription;
    private IDisposable _ammoIconSubscription;
    private IDisposable _ammoIconVisibleSubscription;
    private IDisposable _ammoIconSizeSubscription;

    private void Reset()
    {
        _root = gameObject;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(FirstPersonWeaponAmmoHudViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _visibleSubscription = viewModel.Visible.Subscribe(SetVisible);
        _ammoTextSubscription = viewModel.AmmoText.Subscribe(SetAmmoText);
        _ammoIconSubscription = viewModel.AmmoIcon.Subscribe(SetAmmoIcon);
        _ammoIconVisibleSubscription = viewModel.AmmoIconVisible.Subscribe(SetAmmoIconVisible);
        _ammoIconSizeSubscription = viewModel.AmmoIconSize.Subscribe(SetAmmoIconSize);
    }

    public void Unbind()
    {
        _visibleSubscription?.Dispose();
        _ammoTextSubscription?.Dispose();
        _ammoIconSubscription?.Dispose();
        _ammoIconVisibleSubscription?.Dispose();
        _ammoIconSizeSubscription?.Dispose();
        _visibleSubscription = null;
        _ammoTextSubscription = null;
        _ammoIconSubscription = null;
        _ammoIconVisibleSubscription = null;
        _ammoIconSizeSubscription = null;
    }

    private void SetVisible(bool visible)
    {
        GameObject targetRoot = _root != null ? _root : gameObject;
        targetRoot.SetActive(visible);
    }

    private void SetAmmoText(string text)
    {
        if (_ammoText == null)
        {
            return;
        }

        _ammoText.text = text ?? string.Empty;
    }

    private void SetAmmoIcon(Sprite icon)
    {
        if (_ammoIcon == null)
        {
            return;
        }

        _ammoIcon.sprite = icon;
        _ammoIcon.preserveAspect = true;
        _ammoIcon.raycastTarget = false;
    }

    private void SetAmmoIconVisible(bool visible)
    {
        if (_ammoIcon == null)
        {
            return;
        }

        _ammoIcon.enabled = visible;
        _ammoIcon.gameObject.SetActive(visible);
    }

    private void SetAmmoIconSize(Vector2 size)
    {
        if (_ammoIconRectTransform == null || size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        _ammoIconRectTransform.sizeDelta = size;
    }
}
