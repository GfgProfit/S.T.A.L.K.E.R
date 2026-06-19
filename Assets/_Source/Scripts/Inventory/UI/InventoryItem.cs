using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IView<InventoryItemViewModel>
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
    public bool IsRotated
    {
        get => _state.IsRotated;
        internal set => SetRotated(value);
    }

    private readonly InventoryItemState _state = new();
    private readonly FirstPersonWeaponMagazineState _weaponMagazineState = new();
    private readonly InventoryItemVisualState _visualState = new InventoryItemVisualState();
    private InventoryItemView _itemView;
    private InventoryItemViewModel _viewModel;

    public event Action<InventoryItem> DurabilityChanged;

    public ItemData ItemData => _state.ItemData;
    public int Width => _state.Width;
    public int Height => _state.Height;
    public int CurrentAmount => _state.CurrentAmount;
    public bool IsStackable => _state.IsStackable;
    public bool HasDurability => _state.HasDurability;
    public float CurrentDurabilityPercent => _state.CurrentDurabilityPercent;
    public float UnitWeight => _state.UnitWeight;
    public float TotalWeight => _state.TotalWeight + LoadedMagazineWeight;
    public int BaseWidth => _state.BaseWidth;
    public int BaseHeight => _state.BaseHeight;
    public bool CanRotate => _state.CanRotate;
    public RectTransform RectTransform => _rectTransform;
    public FirstPersonWeaponMagazineState WeaponMagazineState => _weaponMagazineState;
    internal IReadOnlyList<ItemIconPart> RuntimeIconParts => _visualState.RuntimeIconParts;

    private void Awake()
    {
        _state.Initialize(_itemData, _currentAmount, _currentDurabilityPercent, false);
        SyncSerializedStateFromState();
        EnsureItemView();
        EnsureViewModel();
        ApplySerializedVisualSettings();
        RefreshIcon();
        RefreshCountText();
        RefreshShortNameText();
        RefreshCellBackground();
        RefreshDurabilityVisual(_visualState.CellVisualsVisible);
        RefreshStatusIcon();
    }

    private void OnDestroy()
    {
        Unbind();
        _itemView?.Dispose();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    public void Bind(InventoryItemViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        EnsureItemView();
        _itemView.Bind(_viewModel);
    }

    public void Unbind()
    {
        _itemView?.Unbind();
    }

    internal void Set(ItemData itemData) => Set(itemData, 1, null, null);
    internal void Set(ItemData itemData, int amount) => Set(itemData, amount, null, null);
    internal void Set(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts) => Set(itemData, 1, runtimeIconParts, null);
    internal void Set(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts) => Set(itemData, amount, runtimeIconParts, null);

    internal void Set(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts, float? durabilityPercent)
    {
        ApplySerializedVisualSettings();

        ItemData previousItemData = ItemData;
        _state.SetItem(itemData, amount, durabilityPercent ?? InventoryItemState.GetDefaultDurabilityPercent(itemData));

        if (previousItemData != itemData)
        {
            _weaponMagazineState.Clear();
            _visualState.SetCompatibilityHighlight(false, Color.clear);
        }

        SyncSerializedStateFromState();
        _visualState.SetRuntimeIconParts(runtimeIconParts);
        _visualState.RestoreDefaultVisual();
        RefreshCountText();
        RefreshDurabilityVisual(true);
        RefreshShortNameText();

        RefreshIcon();
        RebuildCellVisuals();
        RefreshStatusIcon();

        if (ItemData == null)
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

        _state.AddAmount(amount);
        SyncSerializedStateFromState();
        RefreshCountText();
    }

    public bool CanStackWith(ItemData targetItemData)
    {
        return _state.CanStackWith(targetItemData);
    }

    public void SetDurability(float durabilityPercent) => SetDurabilityInternal(durabilityPercent);

    internal void RefreshIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        EnsureViewModel();
        ApplySerializedVisualSettings();

        if (runtimeIconParts != null)
        {
            _visualState.SetRuntimeIconParts(runtimeIconParts);
        }

        _viewModel.SetIcon(_visualState.GetIcon(ItemData));
    }

    internal void ApplySlotVisual(int slotWidth, int slotHeight, bool useGeneratedSlotIcon)
    {
        ApplySerializedVisualSettings();

        _visualState.ApplySlotVisual(ItemData, slotWidth, slotHeight, useGeneratedSlotIcon);

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
        EnsureViewModel();
        ApplySerializedVisualSettings();
        _visualState.SetCellVisualsVisible(visible);

        RefreshCellBackground();

        _itemView.SetCellGridVisible(visible);

        RefreshDurabilityVisual(visible);
        RefreshStatusIcon();
    }

    internal void SetOverlayTextsVisible(bool visible)
    {
        EnsureViewModel();
        _visualState.SetOverlayTextsVisible(visible);
        RefreshCountText();
        RefreshShortNameText();
    }

    internal void SetCompatibilityHighlight(bool highlighted, Color color)
    {
        if (_visualState.SetCompatibilityHighlight(highlighted, color) == false)
        {
            return;
        }

        RefreshCellBackground();
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
        _state.SetRotated(value);
        SyncSerializedStateFromState();
        ApplyRotation();
    }

    private void OnValidate()
    {
        _currentAmount = InventoryItemState.NormalizeAmount(_itemData, _currentAmount);
        _currentDurabilityPercent = InventoryItemState.NormalizeDurability(_itemData, _currentDurabilityPercent);
    }

    private void ApplyRotation()
    {
        ApplySerializedVisualSettings();
        _itemView.ApplyRotation();
    }

    private void ApplyVisualSize()
    {
        ApplySerializedVisualSettings();
        _itemView.ApplyRootSize();
    }

    private void SetAmountInternal(int amount)
    {
        EnsureViewModel();
        _state.SetAmount(amount);
        SyncSerializedStateFromState();
        RefreshCountText();
    }

    private void SetDurabilityInternal(float durabilityPercent)
    {
        EnsureViewModel();
        bool durabilityChanged = _state.SetDurability(durabilityPercent);
        SyncSerializedStateFromState();
        RefreshDurabilityVisual(true);

        if (durabilityChanged)
        {
            DurabilityChanged?.Invoke(this);
        }
    }

    private void RefreshCellBackground()
    {
        EnsureViewModel();
        _viewModel.SetCellBackground(ItemData, _visualState.CellVisualsVisible, _visualState.IsCompatibilityHighlighted, _visualState.CompatibilityHighlightColor);
    }

    private void RefreshCountText()
    {
        EnsureViewModel();
        _viewModel.SetOverlayTexts(ItemData, CurrentAmount, _visualState.OverlayTextsVisible);
    }

    private void RefreshShortNameText()
    {
        EnsureViewModel();
        _viewModel.SetOverlayTexts(ItemData, CurrentAmount, _visualState.OverlayTextsVisible);
    }

    private void RefreshDurabilityVisual(bool cellVisualsVisible)
    {
        EnsureViewModel();
        _itemView.ApplyDurabilityVisualSettings();
        _viewModel.SetDurability(cellVisualsVisible, HasDurability, CurrentDurabilityPercent);
    }

    private void RefreshStatusIcon()
    {
        EnsureViewModel();
        _itemView.ApplyStatusIconSettings();
        _viewModel.SetStatusIcon(ItemData, _questStatusIcon, _visualState.CellVisualsVisible);
    }

    private void ApplySerializedVisualSettings()
    {
        EnsureItemView();
        _itemView.ApplySerializedSettings();
    }

    private void RebuildCellVisuals()
    {
        ApplySerializedVisualSettings();
        _itemView.RebuildCellVisuals(ItemData);
        RefreshDurabilityVisual(true);
        _itemView.BringOverlayTextsToFront();
        RefreshStatusIcon();
    }

    private void EnsureItemView()
    {
        _itemView ??= new(transform, _rectTransform, _itemImage, _cellBackgroundImage, _countText, _countTextRectTransform, _shortNameText, _shortNameTextRectTransform, _durabilityBackgroundRectTransform, _durabilityBackgroundGraphic, _durabilityFillRectTransform, _durabilityFillGraphic, _statusIconImage, _statusIconRectTransform, () => IsVisuallyRotated, () => VisualWidth, () => VisualHeight, () => CurrentDurabilityPercent);
    }

    private void EnsureViewModel()
    {
        if (_viewModel != null)
        {
            return;
        }

        Bind(InventoryViewModelFactory.CreateInventoryItem());
    }

    private void SyncSerializedStateFromState()
    {
        _itemData = ItemData;
        _currentAmount = CurrentAmount;
        _currentDurabilityPercent = _state.DurabilityPercent;
    }

    private int VisualWidth => _visualState.GetVisualWidth(BaseWidth);
    private int VisualHeight => _visualState.GetVisualHeight(BaseHeight);
    private bool IsVisuallyRotated => _visualState.HasVisualSizeOverride == false && IsRotated;
    private float LoadedMagazineWeight => _weaponMagazineState.LoadedAmmoData == null || _weaponMagazineState.LoadedAmmoAmount <= 0 ? 0f : _weaponMagazineState.LoadedAmmoData.Weight * _weaponMagazineState.LoadedAmmoAmount;

}
