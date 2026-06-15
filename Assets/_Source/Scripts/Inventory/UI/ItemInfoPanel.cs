using System;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour, IView<ItemTooltipViewModel>
{
    [SerializeField] private RectTransform _panelRectTransform;
    [SerializeField] private Vector2 _cursorOffset;
    [SerializeField] private Vector2 _screenPadding;
    [SerializeField] private Image _iconImage;
    [SerializeField] private RectTransform _iconRectTransform;
    [SerializeField] private LayoutElement _iconLayoutElement;
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _weightText;
    [SerializeField] private TMP_Text _durabilityText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] [Min(1)] private int _descriptionWordsPerLine;

    [Header("Stats Info")]
    [SerializeField] private CharacterStatsInfoPanel _statsInfoPanel;
    [SerializeField] private bool _hideStatsInfoWhenEmpty = true;

    [Inject] private IPlayerInput _playerInput = null;

    private readonly ItemInfoPanelPositioner _positioner = new ItemInfoPanelPositioner();
    private IPlayerInput _fallbackPlayerInput;
    private ItemTooltipViewModel _viewModel;
    private IDisposable _isVisibleSubscription;
    private IDisposable _iconSubscription;
    private IDisposable _iconSizeSubscription;
    private IDisposable _itemNameSubscription;
    private IDisposable _typeSubscription;
    private IDisposable _weightSubscription;
    private IDisposable _durabilitySubscription;
    private IDisposable _showDurabilitySubscription;
    private IDisposable _descriptionSubscription;

    private IPlayerPointerInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private void Awake()
    {
        bool hasViewModel = _viewModel != null;
        EnsureViewModel();

        if (hasViewModel == false)
        {
            Hide();
        }
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    public void Show(ItemTooltipData item)
    {
        EnsureViewModel();

        if (item.IsValid == false)
        {
            Hide();
            return;
        }

        GameProjectSettings settings = GameProjectSettings.LoadDefault();
        _viewModel.Show(item, _descriptionWordsPerLine, settings);
        SetStatsInfo(item, settings);
        RebuildLayout();
        UpdatePosition();
    }

    public void Hide()
    {
        EnsureViewModel();
        _viewModel.Hide();
    }

    public void Bind(ItemTooltipViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _isVisibleSubscription = _viewModel.IsVisible.Subscribe(SetVisible);
        _iconSubscription = _viewModel.Icon.Subscribe(SetIcon);
        _iconSizeSubscription = _viewModel.IconSize.Subscribe(SetIconSize);
        _itemNameSubscription = _viewModel.ItemNameText.Subscribe(SetItemName);
        _typeSubscription = _viewModel.TypeText.Subscribe(SetType);
        _weightSubscription = _viewModel.WeightText.Subscribe(SetWeight);
        _durabilitySubscription = _viewModel.DurabilityText.Subscribe(SetDurability);
        _showDurabilitySubscription = _viewModel.ShowDurability.Subscribe(SetDurabilityVisible);
        _descriptionSubscription = _viewModel.DescriptionText.Subscribe(SetDescription);
    }

    public void Unbind()
    {
        _isVisibleSubscription?.Dispose();
        _iconSubscription?.Dispose();
        _iconSizeSubscription?.Dispose();
        _itemNameSubscription?.Dispose();
        _typeSubscription?.Dispose();
        _weightSubscription?.Dispose();
        _durabilitySubscription?.Dispose();
        _showDurabilitySubscription?.Dispose();
        _descriptionSubscription?.Dispose();
        _isVisibleSubscription = null;
        _iconSubscription = null;
        _iconSizeSubscription = null;
        _itemNameSubscription = null;
        _typeSubscription = null;
        _weightSubscription = null;
        _durabilitySubscription = null;
        _showDurabilitySubscription = null;
        _descriptionSubscription = null;
    }

    private void OnDestroy()
    {
        Unbind();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void EnsureViewModel()
    {
        if (_viewModel != null)
        {
            return;
        }

        Bind(InventoryViewModelFactory.CreateItemTooltip());
    }

    private void SetStatsInfo(ItemTooltipData item, GameProjectSettings settings)
    {
        if (_statsInfoPanel == null)
        {
            return;
        }

        _statsInfoPanel.RenderItemStats(item, settings.StatCurrentValueColor, settings.StatFullDurabilityValueColor, _hideStatsInfoWhenEmpty);
    }

    private void SetVisible(bool visible) => gameObject.SetActive(visible);

    private void SetIconSize(Vector2 iconSize)
    {
        if (_iconRectTransform != null)
        {
            _iconRectTransform.sizeDelta = iconSize;
            _iconRectTransform.localRotation = Quaternion.identity;
            _iconRectTransform.localScale = Vector3.one;
        }

        if (_iconLayoutElement != null)
        {
            _iconLayoutElement.minWidth = iconSize.x;
            _iconLayoutElement.minHeight = iconSize.y;
            _iconLayoutElement.preferredWidth = iconSize.x;
            _iconLayoutElement.preferredHeight = iconSize.y;
            _iconLayoutElement.flexibleWidth = 0f;
            _iconLayoutElement.flexibleHeight = 0f;
        }
    }

    private void SetIcon(Sprite icon)
    {
        if (_iconImage == null)
        {
            return;
        }

        _iconImage.sprite = icon;
        _iconImage.enabled = icon != null;
        _iconImage.rectTransform.localRotation = Quaternion.identity;
        _iconImage.rectTransform.localScale = Vector3.one;
    }

    private void SetItemName(string itemName)
    {
        if (_itemNameText == null)
        {
            return;
        }

        _itemNameText.text = itemName;
    }

    private void SetType(string typeText)
    {
        if (_typeText == null)
        {
            return;
        }

        _typeText.text = typeText;
        _typeText.gameObject.SetActive(true);
    }

    private void SetWeight(string weightText)
    {
        if (_weightText == null)
        {
            return;
        }

        _weightText.text = weightText;
    }

    private void SetDurabilityVisible(bool visible)
    {
        if (_durabilityText == null)
        {
            return;
        }

        _durabilityText.gameObject.SetActive(visible);
    }

    private void SetDurability(string durabilityText)
    {
        if (_durabilityText == null)
        {
            return;
        }

        _durabilityText.richText = true;
        _durabilityText.text = durabilityText;
    }

    private void SetDescription(string description)
    {
        if (_descriptionText == null)
        {
            return;
        }

        _descriptionText.text = description;
    }

    private void RebuildLayout()
    {
        if (_panelRectTransform == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
    }

    private void UpdatePosition() => _positioner.UpdatePosition(_panelRectTransform, PlayerInput.GetPointerPosition(), _cursorOffset, _screenPadding);
}
