using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class InventoryItemView : IView<InventoryItemViewModel>, IDisposable
{
    private readonly Transform _ownerTransform;
    private readonly RectTransform _rootRectTransform;
    private readonly Image _itemImage;
    private readonly Image _cellBackgroundImage;
    private readonly TMP_Text _countText;
    private readonly RectTransform _countTextRectTransform;
    private readonly TMP_Text _shortNameText;
    private readonly RectTransform _shortNameTextRectTransform;
    private readonly RectTransform _durabilityBackgroundRectTransform;
    private readonly Graphic _durabilityBackgroundGraphic;
    private readonly RectTransform _durabilityFillRectTransform;
    private readonly Graphic _durabilityFillGraphic;
    private readonly Image _statusIconImage;
    private readonly RectTransform _statusIconRectTransform;
    private readonly Func<bool> _isVisuallyRotated;
    private readonly Func<int> _getVisualWidth;
    private readonly Func<int> _getVisualHeight;
    private readonly Func<float> _getCurrentDurabilityPercent;

    private RectTransform _cellGridRoot;
    private IDisposable _iconSubscription;
    private IDisposable _cellBackgroundColorSubscription;
    private IDisposable _cellBackgroundVisibleSubscription;
    private IDisposable _countTextSubscription;
    private IDisposable _countVisibleSubscription;
    private IDisposable _shortNameTextSubscription;
    private IDisposable _shortNameColorSubscription;
    private IDisposable _shortNameVisibleSubscription;
    private IDisposable _durabilityVisibleSubscription;
    private IDisposable _durabilityPercentSubscription;
    private IDisposable _statusIconSubscription;
    private IDisposable _statusIconVisibleSubscription;

    public InventoryItemView(Transform ownerTransform, RectTransform rootRectTransform, Image itemImage, Image cellBackgroundImage, TMP_Text countText, RectTransform countTextRectTransform, TMP_Text shortNameText, RectTransform shortNameTextRectTransform, RectTransform durabilityBackgroundRectTransform, Graphic durabilityBackgroundGraphic, RectTransform durabilityFillRectTransform, Graphic durabilityFillGraphic, Image statusIconImage, RectTransform statusIconRectTransform, Func<bool> isVisuallyRotated, Func<int> getVisualWidth, Func<int> getVisualHeight, Func<float> getCurrentDurabilityPercent)
    {
        _ownerTransform = ownerTransform;
        _rootRectTransform = rootRectTransform;
        _itemImage = itemImage;
        _cellBackgroundImage = cellBackgroundImage;
        _countText = countText;
        _countTextRectTransform = countTextRectTransform;
        _shortNameText = shortNameText;
        _shortNameTextRectTransform = shortNameTextRectTransform;
        _durabilityBackgroundRectTransform = durabilityBackgroundRectTransform;
        _durabilityBackgroundGraphic = durabilityBackgroundGraphic;
        _durabilityFillRectTransform = durabilityFillRectTransform;
        _durabilityFillGraphic = durabilityFillGraphic;
        _statusIconImage = statusIconImage;
        _statusIconRectTransform = statusIconRectTransform;
        _isVisuallyRotated = isVisuallyRotated;
        _getVisualWidth = getVisualWidth;
        _getVisualHeight = getVisualHeight;
        _getCurrentDurabilityPercent = getCurrentDurabilityPercent;
    }

    public void Bind(InventoryItemViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _iconSubscription = viewModel.Icon.Subscribe(SetItemIcon);
        _cellBackgroundColorSubscription = viewModel.CellBackgroundColor.Subscribe(SetCellBackgroundColor);
        _cellBackgroundVisibleSubscription = viewModel.CellBackgroundVisible.Subscribe(SetCellBackgroundVisible);
        _countTextSubscription = viewModel.CountText.Subscribe(SetCountText);
        _countVisibleSubscription = viewModel.CountVisible.Subscribe(SetCountVisible);
        _shortNameTextSubscription = viewModel.ShortNameText.Subscribe(SetShortNameText);
        _shortNameColorSubscription = viewModel.ShortNameColor.Subscribe(SetShortNameColor);
        _shortNameVisibleSubscription = viewModel.ShortNameVisible.Subscribe(SetShortNameVisible);
        _durabilityVisibleSubscription = viewModel.DurabilityVisible.Subscribe(SetDurabilityVisible);
        _durabilityPercentSubscription = viewModel.DurabilityPercent.Subscribe(SetDurabilityPercent);
        _statusIconSubscription = viewModel.StatusIcon.Subscribe(SetStatusIcon);
        _statusIconVisibleSubscription = viewModel.StatusIconVisible.Subscribe(SetStatusIconVisible);
    }

    public void Unbind()
    {
        _iconSubscription?.Dispose();
        _cellBackgroundColorSubscription?.Dispose();
        _cellBackgroundVisibleSubscription?.Dispose();
        _countTextSubscription?.Dispose();
        _countVisibleSubscription?.Dispose();
        _shortNameTextSubscription?.Dispose();
        _shortNameColorSubscription?.Dispose();
        _shortNameVisibleSubscription?.Dispose();
        _durabilityVisibleSubscription?.Dispose();
        _durabilityPercentSubscription?.Dispose();
        _statusIconSubscription?.Dispose();
        _statusIconVisibleSubscription?.Dispose();
        _iconSubscription = null;
        _cellBackgroundColorSubscription = null;
        _cellBackgroundVisibleSubscription = null;
        _countTextSubscription = null;
        _countVisibleSubscription = null;
        _shortNameTextSubscription = null;
        _shortNameColorSubscription = null;
        _shortNameVisibleSubscription = null;
        _durabilityVisibleSubscription = null;
        _durabilityPercentSubscription = null;
        _statusIconSubscription = null;
        _statusIconVisibleSubscription = null;
    }

    public void Dispose()
    {
        Unbind();
    }

    public void ApplySerializedSettings() => InventoryItemOverlayPresenter.ApplySerializedSettings(_cellBackgroundImage, _itemImage, _countText, _countTextRectTransform, _shortNameText, _shortNameTextRectTransform, _durabilityBackgroundGraphic, _durabilityFillGraphic, _statusIconImage, _statusIconRectTransform);

    public void ApplyRootSize()
    {
        ApplySerializedSettings();

        if (_rootRectTransform == null)
        {
            return;
        }

        _rootRectTransform.sizeDelta = new(_getVisualWidth() * ItemGrid.TILE_SIZE_WIDTH, _getVisualHeight() * ItemGrid.TILE_SIZE_HEIGHT);
        ApplyDurabilityLayout();
        ApplyStatusIconLayout();
    }

    public void ApplyRotation()
    {
        ApplySerializedSettings();

        if (_rootRectTransform != null)
        {
            _rootRectTransform.localRotation = Quaternion.Euler(0f, 0f, _isVisuallyRotated() ? -90f : 0f);
        }

        ApplyDurabilityLayout();
        ApplyCountTextLayout();
        ApplyShortNameTextLayout();
        ApplyStatusIconLayout();
    }

    public void SetCellGridVisible(bool visible)
    {
        if (_cellGridRoot != null)
        {
            _cellGridRoot.gameObject.SetActive(visible);
        }
    }

    public void RebuildCellVisuals(ItemData itemData)
    {
        ApplySerializedSettings();
        _cellGridRoot = InventoryItemCellGridBuilder.RebuildCellGrid(_ownerTransform, _cellGridRoot, _cellBackgroundImage, _itemImage, itemData, _getVisualWidth(), _getVisualHeight());
        ApplyDurabilityVisualSettings();
        BringOverlayTextsToFront();
        ApplyStatusIconSettings();
    }

    public void ApplyDurabilityVisualSettings() => InventoryItemOverlayPresenter.ApplyDurabilityVisualSettings(_durabilityBackgroundGraphic, _durabilityFillGraphic);

    public void ApplyDurabilityLayout()
    {
        ApplyDurabilityVisualSettings();
        InventoryItemOverlayLayout.ApplyDurabilityLayout(_durabilityBackgroundRectTransform, _durabilityFillRectTransform, _durabilityFillGraphic, _isVisuallyRotated(), _getCurrentDurabilityPercent());
    }

    public void ApplyCountTextLayout()
    {
        InventoryItemOverlayPresenter.ApplyOverlayTextSettings(_countText, _shortNameText);
        InventoryItemOverlayLayout.ApplyCountTextLayout(_countTextRectTransform, _isVisuallyRotated());
        BringOverlayTextsToFront();
    }

    public void ApplyShortNameTextLayout()
    {
        InventoryItemOverlayPresenter.ApplyOverlayTextSettings(_countText, _shortNameText);
        InventoryItemOverlayLayout.ApplyShortNameTextLayout(_shortNameTextRectTransform, _isVisuallyRotated(), _getVisualWidth());
        BringOverlayTextsToFront();
    }

    public void ApplyStatusIconSettings() => InventoryItemOverlayPresenter.ApplyStatusIconSettings(_statusIconImage);

    public void ApplyStatusIconLayout()
    {
        ApplyStatusIconSettings();
        InventoryItemOverlayLayout.ApplyStatusIconLayout(_statusIconRectTransform, _isVisuallyRotated());
    }

    public void BringOverlayTextsToFront() => InventoryItemOverlayPresenter.BringOverlayTextsToFront(_shortNameTextRectTransform, _countTextRectTransform, _statusIconRectTransform);

    private void ApplyDurabilityFill()
    {
        ApplyDurabilityVisualSettings();
        InventoryItemOverlayLayout.ApplyDurabilityFill(_durabilityFillRectTransform, _durabilityFillGraphic, _isVisuallyRotated(), _getCurrentDurabilityPercent());
    }

    private void SetItemIcon(Sprite icon)
    {
        if (_itemImage == null)
        {
            return;
        }

        _itemImage.sprite = icon;
    }

    private void SetCellBackgroundColor(Color color)
    {
        if (_cellBackgroundImage == null)
        {
            return;
        }

        _cellBackgroundImage.sprite = null;
        _cellBackgroundImage.color = color;
        _cellBackgroundImage.raycastTarget = false;
    }

    private void SetCellBackgroundVisible(bool visible)
    {
        if (_cellBackgroundImage == null)
        {
            return;
        }

        _cellBackgroundImage.enabled = visible;
    }

    private void SetCountText(string text)
    {
        InventoryItemOverlayPresenter.ApplyOverlayTextSettings(_countText, null);

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
            BringOverlayTextsToFront();
        }
    }

    private void SetShortNameText(string text)
    {
        InventoryItemOverlayPresenter.ApplyOverlayTextSettings(null, _shortNameText);

        if (_shortNameText == null)
        {
            return;
        }

        _shortNameText.text = text;
    }

    private void SetShortNameColor(Color color)
    {
        if (_shortNameText == null)
        {
            return;
        }

        _shortNameText.color = color;
    }

    private void SetShortNameVisible(bool visible)
    {
        if (_shortNameText == null)
        {
            return;
        }

        _shortNameText.gameObject.SetActive(visible);

        if (visible)
        {
            BringOverlayTextsToFront();
        }
    }

    private void SetDurabilityVisible(bool visible)
    {
        ApplyDurabilityVisualSettings();

        if (_durabilityBackgroundRectTransform == null)
        {
            return;
        }

        _durabilityBackgroundRectTransform.gameObject.SetActive(visible);

        if (visible == false)
        {
            return;
        }

        ApplyDurabilityLayout();
        ApplyDurabilityFill();
        _durabilityBackgroundRectTransform.SetAsLastSibling();
        BringOverlayTextsToFront();
    }

    private void SetDurabilityPercent(float durabilityPercent)
    {
        if (_durabilityBackgroundRectTransform == null || _durabilityBackgroundRectTransform.gameObject.activeSelf == false)
        {
            return;
        }

        ApplyDurabilityLayout();
        ApplyDurabilityFill();
    }

    private void SetStatusIcon(Sprite icon)
    {
        ApplyStatusIconSettings();

        if (_statusIconImage == null)
        {
            return;
        }

        _statusIconImage.sprite = icon;
    }

    private void SetStatusIconVisible(bool visible)
    {
        ApplyStatusIconSettings();

        if (_statusIconRectTransform == null || _statusIconImage == null)
        {
            return;
        }

        _statusIconImage.enabled = visible;
        _statusIconRectTransform.gameObject.SetActive(visible);

        if (visible)
        {
            InventoryItemOverlayLayout.ApplyStatusIconLayout(_statusIconRectTransform, _isVisuallyRotated());
            _statusIconRectTransform.SetAsLastSibling();
        }
    }
}
