using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    [SerializeField] private ItemData _itemData;
    [SerializeField] [Min(1)] private int _currentAmount = 1;
    [SerializeField] [Range(0f, 100f)] private float _currentDurabilityPercent = 100f;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Image _cellBackgroundImage;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private RectTransform _countTextRectTransform;
    [SerializeField] private TMP_Text _shortNameText;
    [SerializeField] private RectTransform _shortNameTextRectTransform;
    [SerializeField] private RectTransform _durabilityBackgroundRectTransform;
    [SerializeField] private Graphic _durabilityBackgroundGraphic;
    [SerializeField] private RectTransform _durabilityFillRectTransform;
    [SerializeField] private Graphic _durabilityFillGraphic;
    [SerializeField] private Sprite _questStatusIcon;
    [SerializeField] private Image _statusIconImage;
    [SerializeField] private RectTransform _statusIconRectTransform;

    public int GridPositionX { get; internal set; }
    public int GridPositionY { get; internal set; }
    public bool IsRotated { get; internal set; }

    private RectTransform _cellGridRoot;
    private readonly InventoryItemVisualState _visualState = new InventoryItemVisualState();

    public event Action<InventoryItem> DurabilityChanged;

    public ItemData ItemData => _itemData;
    public int Width => _itemData == null ? 0 : Mathf.Max(1, IsRotated ? _itemData.Height : _itemData.Width);
    public int Height => _itemData == null ? 0 : Mathf.Max(1, IsRotated ? _itemData.Width : _itemData.Height);
    public int CurrentAmount => Mathf.Max(1, _currentAmount);
    public bool IsStackable => _itemData != null && _itemData.IsStackable;
    public bool HasDurability => _itemData != null && _itemData.HasDurability;
    public float CurrentDurabilityPercent => HasDurability ? ItemData.NormalizeDurability(_currentDurabilityPercent) : 0f;
    public float UnitWeight => _itemData == null ? 0f : _itemData.Weight;
    public float TotalWeight => UnitWeight * CurrentAmount;
    public int BaseWidth => _itemData == null ? 0 : Mathf.Max(1, _itemData.Width);
    public int BaseHeight => _itemData == null ? 0 : Mathf.Max(1, _itemData.Height);
    public bool CanRotate => BaseWidth != BaseHeight;
    public RectTransform RectTransform => _rectTransform;
    internal IReadOnlyList<ItemIconPart> RuntimeIconParts => _visualState.RuntimeIconParts;

    private void Awake()
    {
        ApplySerializedVisualSettings();
        RefreshDurabilityVisual(true);
        RefreshStatusIcon();
    }

    internal void Set(ItemData itemData) => Set(itemData, 1, null, null);
    internal void Set(ItemData itemData, int amount) => Set(itemData, amount, null, null);
    internal void Set(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts) => Set(itemData, 1, runtimeIconParts, null);
    internal void Set(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts) => Set(itemData, amount, runtimeIconParts, null);

    internal void Set(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts, float? durabilityPercent)
    {
        ApplySerializedVisualSettings();

        _itemData = itemData;
        _visualState.SetRuntimeIconParts(runtimeIconParts);
        IsRotated = false;
        _visualState.RestoreDefaultVisual();
        SetAmountInternal(amount);
        SetDurabilityInternal(durabilityPercent ?? GetDefaultDurabilityPercent());
        RefreshShortNameText();

        RefreshIcon();
        RebuildCellVisuals();
        RefreshStatusIcon();

        if (itemData == null)
        {
            return;
        }

        ApplyVisualSize();
        ApplyRotation();
        SetCellVisualsVisible(true);
        RefreshStatusIcon();
    }

    public void SetAmount(int amount) => SetAmountInternal(amount);

    public void AddAmount(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        long totalAmount = (long)CurrentAmount + amount;
        SetAmountInternal(totalAmount > int.MaxValue ? int.MaxValue : (int)totalAmount);
    }

    public bool CanStackWith(ItemData targetItemData)
    {
        return targetItemData != null && _itemData == targetItemData && IsStackable;
    }

    public void SetDurability(float durabilityPercent) => SetDurabilityInternal(durabilityPercent);

    internal void RefreshIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        ApplySerializedVisualSettings();

        if (runtimeIconParts != null)
        {
            _visualState.SetRuntimeIconParts(runtimeIconParts);
        }

        if (_itemImage != null)
        {
            _itemImage.sprite = _visualState.GetIcon(_itemData);
        }
    }

    internal void ApplySlotVisual(int slotWidth, int slotHeight, bool useGeneratedSlotIcon)
    {
        ApplySerializedVisualSettings();

        _visualState.ApplySlotVisual(_itemData, slotWidth, slotHeight, useGeneratedSlotIcon);

        ApplyVisualSize();
        RefreshIcon();
        RebuildCellVisuals();
        ApplyRotation();
        SetCellVisualsVisible(true);
    }

    internal void RestoreDefaultVisual()
    {
        ApplySerializedVisualSettings();
        _visualState.RestoreDefaultVisual();
        ApplyVisualSize();
        RefreshIcon();
        RebuildCellVisuals();
        ApplyRotation();
        SetCellVisualsVisible(true);
    }

    internal void SetCellVisualsVisible(bool visible)
    {
        ApplySerializedVisualSettings();
        _visualState.SetCellVisualsVisible(visible);

        if (_cellBackgroundImage != null)
        {
            _cellBackgroundImage.enabled = visible && _itemData != null && _itemData.IconBackgroundColor.a > 0f;
        }

        if (_cellGridRoot != null)
        {
            _cellGridRoot.gameObject.SetActive(visible);
        }

        RefreshDurabilityVisual(visible);
        RefreshStatusIcon();
    }

    internal void SetOverlayTextsVisible(bool visible)
    {
        _visualState.SetOverlayTextsVisible(visible);
        RefreshCountText();
        RefreshShortNameText();
    }

    public void Rotate()
    {
        if (CanRotate == false)
        {
            SetRotated(false);
            return;
        }

        SetRotated(IsRotated == false);
    }

    internal void SetRotated(bool value)
    {
        bool normalizedValue = value && CanRotate;

        if (IsRotated == normalizedValue)
        {
            ApplyRotation();
            return;
        }

        IsRotated = normalizedValue;
        ApplyRotation();
    }

    private void OnValidate()
    {
        _currentAmount = Mathf.Max(1, _currentAmount);
        _currentDurabilityPercent = ItemData.NormalizeDurability(_currentDurabilityPercent);
    }

    private void ApplyRotation()
    {
        ApplySerializedVisualSettings();
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, IsVisuallyRotated ? -90f : 0f);
        ApplyDurabilityLayout();
        ApplyCountTextLayout();
        ApplyShortNameTextLayout();
        ApplyStatusIconLayout();
    }

    private void ApplyVisualSize()
    {
        ApplySerializedVisualSettings();
        _rectTransform.sizeDelta = new Vector2(VisualWidth * ItemGrid.TILE_SIZE_WIDTH, VisualHeight * ItemGrid.TILE_SIZE_HEIGHT);
        ApplyDurabilityLayout();
        ApplyStatusIconLayout();
    }

    private void SetAmountInternal(int amount)
    {
        _currentAmount = NormalizeAmount(amount);
        RefreshCountText();
    }

    private void SetDurabilityInternal(float durabilityPercent)
    {
        float normalizedDurabilityPercent = ItemData.NormalizeDurability(durabilityPercent);
        bool durabilityChanged = Mathf.Approximately(_currentDurabilityPercent, normalizedDurabilityPercent) == false;

        _currentDurabilityPercent = normalizedDurabilityPercent;
        RefreshDurabilityVisual(true);

        if (durabilityChanged)
        {
            DurabilityChanged?.Invoke(this);
        }
    }

    private float GetDefaultDurabilityPercent() => _itemData == null ? 100f : _itemData.DefaultDurabilityPercent;

    private int NormalizeAmount(int amount)
    {
        if (_itemData == null || _itemData.IsStackable == false)
        {
            return 1;
        }

        return Mathf.Max(1, amount);
    }

    private void RefreshCountText() => InventoryItemOverlayPresenter.RefreshCountText(_countText, _countTextRectTransform, _shortNameTextRectTransform, _statusIconRectTransform, _visualState.OverlayTextsVisible, _itemData, CurrentAmount);
    private void RefreshShortNameText() => InventoryItemOverlayPresenter.RefreshShortNameText(_shortNameText, _shortNameTextRectTransform, _countTextRectTransform, _statusIconRectTransform, _visualState.OverlayTextsVisible, _itemData);

    private void RefreshDurabilityVisual(bool cellVisualsVisible)
    {
        InventoryItemOverlayPresenter.ApplyDurabilityVisualSettings(_durabilityBackgroundGraphic, _durabilityFillGraphic);

        if (_durabilityBackgroundRectTransform == null)
        {
            return;
        }

        bool showDurability = cellVisualsVisible && HasDurability;
        _durabilityBackgroundRectTransform.gameObject.SetActive(showDurability);

        if (showDurability == false)
        {
            return;
        }

        ApplyDurabilityLayout();
        ApplyDurabilityFill();
        _durabilityBackgroundRectTransform.SetAsLastSibling();
        InventoryItemOverlayPresenter.BringOverlayTextsToFront(_shortNameTextRectTransform, _countTextRectTransform, _statusIconRectTransform);
    }

    private void ApplyDurabilityLayout()
    {
        InventoryItemOverlayPresenter.ApplyDurabilityVisualSettings(_durabilityBackgroundGraphic, _durabilityFillGraphic);
        InventoryItemOverlayLayout.ApplyDurabilityLayout(_durabilityBackgroundRectTransform, _durabilityFillRectTransform, _durabilityFillGraphic, IsVisuallyRotated, CurrentDurabilityPercent);
    }

    private void ApplyDurabilityFill()
    {
        InventoryItemOverlayPresenter.ApplyDurabilityVisualSettings(_durabilityBackgroundGraphic, _durabilityFillGraphic);
        InventoryItemOverlayLayout.ApplyDurabilityFill(_durabilityFillRectTransform, _durabilityFillGraphic, IsVisuallyRotated, CurrentDurabilityPercent);
    }

    private void RefreshStatusIcon() => InventoryItemOverlayPresenter.RefreshStatusIcon(_statusIconRectTransform, _statusIconImage, _questStatusIcon, _visualState.CellVisualsVisible, _itemData, IsVisuallyRotated);

    private void ApplyStatusIconLayout()
    {
        InventoryItemOverlayPresenter.ApplyStatusIconSettings(_statusIconImage);
        InventoryItemOverlayLayout.ApplyStatusIconLayout(_statusIconRectTransform, IsVisuallyRotated);
    }

    private void ApplySerializedVisualSettings() => InventoryItemOverlayPresenter.ApplySerializedSettings(_cellBackgroundImage, _itemImage, _countText, _countTextRectTransform, _shortNameText, _shortNameTextRectTransform, _durabilityBackgroundGraphic, _durabilityFillGraphic, _statusIconImage, _statusIconRectTransform);

    private void RebuildCellVisuals()
    {
        ApplySerializedVisualSettings();
        _cellGridRoot = InventoryItemCellGridBuilder.RebuildCellGrid(transform, _cellGridRoot, _cellBackgroundImage, _itemImage, _itemData, VisualWidth, VisualHeight);

        RefreshDurabilityVisual(true);
        InventoryItemOverlayPresenter.BringOverlayTextsToFront(_shortNameTextRectTransform, _countTextRectTransform, _statusIconRectTransform);
        RefreshStatusIcon();
    }

    private void ApplyCountTextLayout()
    {
        InventoryItemOverlayPresenter.ApplyOverlayTextSettings(_countText, _shortNameText);
        InventoryItemOverlayLayout.ApplyCountTextLayout(_countTextRectTransform, IsVisuallyRotated);
        InventoryItemOverlayPresenter.BringOverlayTextsToFront(_shortNameTextRectTransform, _countTextRectTransform, _statusIconRectTransform);
    }

    private void ApplyShortNameTextLayout()
    {
        InventoryItemOverlayPresenter.ApplyOverlayTextSettings(_countText, _shortNameText);
        InventoryItemOverlayLayout.ApplyShortNameTextLayout(_shortNameTextRectTransform, IsVisuallyRotated, VisualWidth);
        InventoryItemOverlayPresenter.BringOverlayTextsToFront(_shortNameTextRectTransform, _countTextRectTransform, _statusIconRectTransform);
    }

    private int VisualWidth => _visualState.GetVisualWidth(BaseWidth);
    private int VisualHeight => _visualState.GetVisualHeight(BaseHeight);
    private bool IsVisuallyRotated => _visualState.HasVisualSizeOverride == false && IsRotated;

}