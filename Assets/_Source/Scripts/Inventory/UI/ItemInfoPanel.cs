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
    [SerializeField] private GameObject _typeTextParent;
    [SerializeField] private TMP_Text _weightText;
    [SerializeField] private GameObject _weightTextParent;
    [SerializeField] private TMP_Text _durabilityText;
    [SerializeField] private GameObject _durabilityTextParent;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] [Min(1)] private int _descriptionWordsPerLine;

    [Header("Ammo Info")]
    [SerializeField] private TMP_Text _ammoFleshDamageText;
    [SerializeField] private GameObject _ammoFleshDamageParent;
    [SerializeField] private TMP_Text _ammoArmorPenetrationText;
    [SerializeField] private GameObject _ammoArmorPenetrationParent;
    [SerializeField] private GameObject _ammoArmorClassRoot;
    [SerializeField] private TMP_Text[] _ammoArmorClassTexts;
    [SerializeField] private GameObject[] _ammoArmorClassTextParents;
    [SerializeField] private TMP_Text _ammoBulletSpeedText;
    [SerializeField] private GameObject _ammoBulletSpeedParent;
    [SerializeField] private TMP_Text _ammoBulletMassText;
    [SerializeField] private GameObject _ammoBulletMassParent;
    [SerializeField] private TMP_Text _ammoBulletDiameterText;
    [SerializeField] private GameObject _ammoBulletDiameterParent;
    [SerializeField] private TMP_Text _ammoRicochetChanceText;
    [SerializeField] private GameObject _ammoRicochetChanceParent;
    [SerializeField] private TMP_Text _ammoRecoilModifierText;
    [SerializeField] private GameObject _ammoRecoilModifierParent;
    [SerializeField] private TMP_Text _ammoDurabilityLossModifierText;
    [SerializeField] private GameObject _ammoDurabilityLossModifierParent;

    [Header("Module Info")]
    [SerializeField] private TMP_Text _moduleMagazineSizeText;
    [SerializeField] private GameObject _moduleMagazineSizeParent;
    [SerializeField] private TMP_Text _moduleAccuracyText;
    [SerializeField] private GameObject _moduleAccuracyParent;
    [SerializeField] private TMP_Text _ergonomicsText;
    [SerializeField] private GameObject _ergonomicsParent;

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
        SetAmmoInfo(item.ItemData, settings);
        SetModuleInfo(item, settings);
        SetArmorInfo(item.ItemData, settings);
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

    private void SetAmmoInfo(ItemData itemData, GameProjectSettings settings)
    {
        AmmoTooltipTextData ammoInfo = ItemTooltipTextFormatter.FormatAmmoDetails(itemData, settings);
        bool visible = ammoInfo != null;

        SetAmmoText(_ammoFleshDamageText, _ammoFleshDamageParent, visible ? ammoInfo.FleshDamageText : string.Empty, visible);
        SetAmmoText(_ammoArmorPenetrationText, _ammoArmorPenetrationParent, visible ? ammoInfo.ArmorPenetrationText : string.Empty, visible);
        SetAmmoText(_ammoBulletSpeedText, _ammoBulletSpeedParent, visible ? ammoInfo.BulletSpeedText : string.Empty, visible);
        SetAmmoText(_ammoBulletMassText, _ammoBulletMassParent, visible ? ammoInfo.BulletMassText : string.Empty, visible);
        SetAmmoText(_ammoBulletDiameterText, _ammoBulletDiameterParent, visible ? ammoInfo.BulletDiameterText : string.Empty, visible);
        SetAmmoText(_ammoRicochetChanceText, _ammoRicochetChanceParent, visible ? ammoInfo.RicochetChanceText : string.Empty, visible);
        bool showRecoilModifier = visible && string.IsNullOrEmpty(ammoInfo.RecoilModifierText) == false;
        bool showDurabilityModifier = visible && string.IsNullOrEmpty(ammoInfo.DurabilityLossModifierText) == false;
        SetAmmoText(_ammoRecoilModifierText, _ammoRecoilModifierParent, showRecoilModifier ? ammoInfo.RecoilModifierText : string.Empty, showRecoilModifier);
        SetAmmoText(_ammoDurabilityLossModifierText, _ammoDurabilityLossModifierParent, showDurabilityModifier ? ammoInfo.DurabilityLossModifierText : string.Empty, showDurabilityModifier);

        if (_ammoArmorClassRoot != null)
        {
            _ammoArmorClassRoot.SetActive(visible);
        }

        if (_ammoArmorClassTexts == null)
        {
            return;
        }

        int count = visible ? Mathf.Min(_ammoArmorClassTexts.Length, ammoInfo.ArmorClassTexts.Length) : _ammoArmorClassTexts.Length;

        for (int i = 0; i < count; i++)
        {
            GameObject parent = _ammoArmorClassTextParents != null && i < _ammoArmorClassTextParents.Length ? _ammoArmorClassTextParents[i] : null;
            string value = visible ? ammoInfo.ArmorClassTexts[i] : string.Empty;
            SetAmmoText(_ammoArmorClassTexts[i], parent, value, visible);
        }
    }

    private void SetModuleInfo(ItemTooltipData item, GameProjectSettings settings)
    {
        ModuleTooltipTextData moduleInfo = ItemTooltipTextFormatter.FormatModuleDetails(item, settings);

        if (moduleInfo != null)
        {
            bool showRecoilModifier = string.IsNullOrEmpty(moduleInfo.RecoilModifierText) == false;
            bool showDurabilityModifier = string.IsNullOrEmpty(moduleInfo.DurabilityLossModifierText) == false;
            SetAmmoText(_ammoRecoilModifierText, _ammoRecoilModifierParent, moduleInfo.RecoilModifierText, showRecoilModifier);
            SetAmmoText(_ammoDurabilityLossModifierText, _ammoDurabilityLossModifierParent, moduleInfo.DurabilityLossModifierText, showDurabilityModifier);
        }

        bool showMagazineSize = moduleInfo != null && string.IsNullOrEmpty(moduleInfo.MagazineSizeText) == false;
        SetAmmoText(_moduleMagazineSizeText, _moduleMagazineSizeParent, showMagazineSize ? moduleInfo.MagazineSizeText : string.Empty, showMagazineSize);
        bool showAccuracy = moduleInfo != null && string.IsNullOrEmpty(moduleInfo.AccuracyText) == false;
        SetAmmoText(_moduleAccuracyText, _moduleAccuracyParent, showAccuracy ? moduleInfo.AccuracyText : string.Empty, showAccuracy);
        bool showErgonomics = moduleInfo != null && string.IsNullOrEmpty(moduleInfo.ErgonomicsText) == false;
        SetAmmoText(_ergonomicsText, _ergonomicsParent, showErgonomics ? moduleInfo.ErgonomicsText : string.Empty, showErgonomics);
    }

    private void SetArmorInfo(ItemData itemData, GameProjectSettings settings)
    {
        if (itemData == null || itemData.ItemType != ItemType.Armor)
        {
            return;
        }

        string recoilReductionText = ItemTooltipTextFormatter.FormatArmorRecoilReduction(itemData, settings);
        bool visible = string.IsNullOrEmpty(recoilReductionText) == false;
        SetAmmoText(_ammoRecoilModifierText, _ammoRecoilModifierParent, recoilReductionText, visible);
    }

    private static void SetAmmoText(TMP_Text text, GameObject parent, string value, bool visible)
    {
        if (text != null)
        {
            text.richText = true;
            text.text = value;
        }

        parent?.SetActive(visible);
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
        if (_typeText != null)
        {
            _typeText.text = typeText;
        }

        _typeTextParent?.SetActive(true);
    }

    private void SetWeight(string weightText)
    {
        if (_weightText != null)
        {
            _weightText.text = weightText;
        }

        _weightTextParent?.SetActive(true);
    }

    private void SetDurabilityVisible(bool visible) => _durabilityTextParent?.SetActive(visible);

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
