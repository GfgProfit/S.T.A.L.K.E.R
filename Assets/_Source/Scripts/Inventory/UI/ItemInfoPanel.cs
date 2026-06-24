using System;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour, IView<ItemTooltipViewModel>, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private const float MIN_PANEL_WIDTH = 220f;
    private const float MAX_PANEL_WIDTH = 520f;
    private const float DEFAULT_PANEL_HORIZONTAL_PADDING = 32f;
    private const float DESCRIPTION_WIDTH_STEP = 16f;
    private const int MIN_DESCRIPTION_LINE_COUNT = 1;
    private const int MAX_DESCRIPTION_LINE_COUNT = 6;
    private const int TARGET_DESCRIPTION_CHARACTERS_PER_LINE = 70;

    private static readonly char[] _descriptionWordSeparators = { ' ', '\t', '\n', '\r' };

    [SerializeField] private RectTransform _panelRectTransform;
    [SerializeField] private Vector2 _cursorOffset;
    [SerializeField] private Vector2 _screenPadding;
    [SerializeField] private Image _dragImage;
    [SerializeField] private Button _closeButton;
    [SerializeField] private RectTransform _armorClassHoverArea;
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
#pragma warning disable CS0169, CS0414
    // Kept serialized to preserve existing prefab data after switching description wrapping to TMP metrics.
    [SerializeField] [HideInInspector] [Min(1)] private int _descriptionWordsPerLine;
#pragma warning restore CS0169, CS0414

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
    private LayoutElement _descriptionLayoutElement;
    private bool _referencesResolved;
    private bool _closeButtonSubscribed;
    private bool _isDragging;
    private bool _hasArmorClassInfo;
    private int _closedByEscapeFrame = -1;
    private Vector2 _dragPointerOffset;

    public bool IsOpen => gameObject.activeSelf;
    public bool WasClosedByEscapeThisFrame => _closedByEscapeFrame == Time.frameCount;

    private IInventoryInput PlayerInput
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
        ResolveOptionalReferences();
        SubscribeCloseButton();
        bool hasViewModel = _viewModel != null;
        EnsureViewModel();

        if (hasViewModel == false)
        {
            Hide();
        }
    }

    private void Update()
    {
        if (PlayerInput.IsEscapePressed())
        {
            _closedByEscapeFrame = Time.frameCount;
            Hide();
            return;
        }

        RefreshArmorClassVisibility();
    }

    private void OnDisable()
    {
        _isDragging = false;
        SetArmorClassRootVisible(false);
    }

    public void Show(ItemTooltipData item)
    {
        ResolveOptionalReferences();
        SubscribeCloseButton();
        EnsureViewModel();

        if (item.IsValid == false)
        {
            Hide();
            return;
        }

        GameProjectSettings settings = GameProjectSettings.LoadDefault();
        _viewModel.Show(item, settings);
        SetAmmoInfo(item.ItemData, settings);
        SetModuleInfo(item, settings);
        SetArmorInfo(item.ItemData, settings);
        SetStatsInfo(item, settings);
        FitDescriptionLayout(_descriptionText != null ? _descriptionText.text : string.Empty);
        RebuildLayout();
        FitDragAreaWidthToWidestPanelChild();
        RebuildLayout();
        CenterOnScreen();
    }

    public void Hide()
    {
        EnsureViewModel();
        _hasArmorClassInfo = false;
        SetArmorClassRootVisible(false);
        _viewModel.Hide();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveOptionalReferences();

        if (CanDragFromEvent(eventData) == false)
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
        _dragPointerOffset = (Vector2)_panelRectTransform.position - eventData.position;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging == false || _panelRectTransform == null)
        {
            return;
        }

        _panelRectTransform.position = eventData.position + _dragPointerOffset;
        ClampToScreen();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ResolveOptionalReferences();

        if (IsCloseButtonScreenPoint(eventData.position, eventData.pressEventCamera))
        {
            Hide();
        }
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
        UnsubscribeCloseButton();
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

    private void ResolveOptionalReferences()
    {
        if (_referencesResolved)
        {
            return;
        }

        if (_panelRectTransform == null)
        {
            _panelRectTransform = transform as RectTransform;
        }

        if (_dragImage == null)
        {
            Transform dragTransform = FindChildRecursive(transform, "Drag Image");

            if (dragTransform != null)
            {
                _dragImage = dragTransform.GetComponent<Image>();
            }
        }

        if (_closeButton == null)
        {
            Transform closeTransform = FindChildRecursive(transform, "Close Button");

            if (closeTransform != null)
            {
                _closeButton = closeTransform.GetComponent<Button>();
            }
        }

        if (_armorClassHoverArea == null)
        {
            Transform hoverAreaTransform = FindChildRecursive(transform, "Armor Class Area");

            if (hoverAreaTransform == null && _panelRectTransform != null)
            {
                Canvas canvas = _panelRectTransform.GetComponentInParent<Canvas>();
                Transform searchRoot = canvas != null ? canvas.transform : transform.root;
                hoverAreaTransform = FindChildRecursive(searchRoot, "Armor Class Area");
            }

            _armorClassHoverArea = hoverAreaTransform as RectTransform;
        }

        _referencesResolved = true;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindChildRecursive(child, childName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void SubscribeCloseButton()
    {
        if (_closeButton == null || _closeButtonSubscribed)
        {
            return;
        }

        _closeButton.onClick.RemoveListener(HandleCloseClicked);
        _closeButton.onClick.AddListener(HandleCloseClicked);
        _closeButtonSubscribed = true;
    }

    private void UnsubscribeCloseButton()
    {
        if (_closeButton == null || _closeButtonSubscribed == false)
        {
            return;
        }

        _closeButton.onClick.RemoveListener(HandleCloseClicked);
        _closeButtonSubscribed = false;
    }

    private void HandleCloseClicked() => Hide();

    private bool CanDragFromEvent(PointerEventData eventData)
    {
        if (_panelRectTransform == null || _dragImage == null || eventData == null)
        {
            return false;
        }

        Camera eventCamera = eventData.pressEventCamera;

        if (IsCloseButtonScreenPoint(eventData.pressPosition, eventCamera))
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(_dragImage.rectTransform, eventData.pressPosition, eventCamera);
    }

    private bool IsCloseButtonScreenPoint(Vector2 screenPoint, Camera eventCamera)
    {
        return _closeButton != null &&
               _closeButton.transform is RectTransform closeRectTransform &&
               RectTransformUtility.RectangleContainsScreenPoint(closeRectTransform, screenPoint, eventCamera);
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

        _hasArmorClassInfo = visible;

        if (_ammoArmorClassTexts != null)
        {
            int valueCount = visible ? Mathf.Min(_ammoArmorClassTexts.Length, ammoInfo.ArmorClassTexts.Length) : 0;

            for (int i = 0; i < _ammoArmorClassTexts.Length; i++)
            {
                bool showArmorClassText = i < valueCount;
                GameObject parent = _ammoArmorClassTextParents != null && i < _ammoArmorClassTextParents.Length ? _ammoArmorClassTextParents[i] : null;
                string value = showArmorClassText ? ammoInfo.ArmorClassTexts[i] : string.Empty;
                SetAmmoText(_ammoArmorClassTexts[i], parent, value, showArmorClassText);
            }
        }

        RefreshArmorClassVisibility();
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

    private void RefreshArmorClassVisibility()
    {
        bool visible = _hasArmorClassInfo && IsArmorClassAreaHovered();
        SetArmorClassRootVisible(visible);

        if (visible)
        {
            UpdateArmorClassPosition();
        }
    }

    private bool IsArmorClassAreaHovered()
    {
        if (_armorClassHoverArea == null || _armorClassHoverArea.gameObject.activeInHierarchy == false)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            _armorClassHoverArea,
            PlayerInput.GetPointerPosition(),
            GetArmorClassAreaCamera());
    }

    private Camera GetArmorClassAreaCamera()
    {
        if (_armorClassHoverArea == null)
        {
            return null;
        }

        Canvas canvas = _armorClassHoverArea.GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void SetArmorClassRootVisible(bool visible)
    {
        if (_ammoArmorClassRoot == null)
        {
            return;
        }

        if (visible)
        {
            _ammoArmorClassRoot.transform.SetAsLastSibling();
        }

        if (_ammoArmorClassRoot.activeSelf != visible)
        {
            _ammoArmorClassRoot.SetActive(visible);
        }
    }

    private void UpdateArmorClassPosition()
    {
        if (_ammoArmorClassRoot == null || _ammoArmorClassRoot.transform is not RectTransform armorClassRectTransform)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(armorClassRectTransform);
        _positioner.UpdatePosition(armorClassRectTransform, PlayerInput.GetPointerPosition(), _cursorOffset, _screenPadding);
    }

    private void SetVisible(bool visible)
    {
        if (visible == false)
        {
            _hasArmorClassInfo = false;
            SetArmorClassRootVisible(false);
        }

        gameObject.SetActive(visible);

        if (visible)
        {
            transform.SetAsLastSibling();
        }
    }

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
        _descriptionText.enableAutoSizing = false;
        _descriptionText.textWrappingMode = TextWrappingModes.Normal;
        _descriptionText.overflowMode = TextOverflowModes.Overflow;
        FitDescriptionLayout(description);
    }

    private void RebuildLayout()
    {
        if (_panelRectTransform == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
    }

    private void FitDragAreaWidthToWidestPanelChild()
    {
        if (_panelRectTransform == null || _dragImage == null)
        {
            return;
        }

        RectTransform dragRectTransform = _dragImage.rectTransform;
        float width = GetWidestPanelChildWidth(dragRectTransform);

        if (width <= 0f)
        {
            return;
        }

        ApplyDragAreaWidth(dragRectTransform, Mathf.Ceil(width));
    }

    private float GetWidestPanelChildWidth(RectTransform excludedChild)
    {
        float width = 0f;

        for (int i = 0; i < _panelRectTransform.childCount; i++)
        {
            if (_panelRectTransform.GetChild(i) is not RectTransform child ||
                child == excludedChild ||
                child.gameObject.activeSelf == false)
            {
                continue;
            }

            width = Mathf.Max(width, GetLayoutChildWidth(child));
        }

        return width;
    }

    private static float GetLayoutChildWidth(RectTransform child)
    {
        float width = child.rect.width;
        float preferredWidth = LayoutUtility.GetPreferredWidth(child);

        if (preferredWidth > 0f)
        {
            width = Mathf.Max(width, preferredWidth);
        }

        return width;
    }

    private void ApplyDragAreaWidth(RectTransform dragRectTransform, float width)
    {
        dragRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        Vector2 anchoredPosition = dragRectTransform.anchoredPosition;
        anchoredPosition.x = GetPanelLeftPadding() + width * dragRectTransform.pivot.x;
        dragRectTransform.anchoredPosition = anchoredPosition;

        if (dragRectTransform.TryGetComponent(out LayoutElement layoutElement) == false)
        {
            layoutElement = dragRectTransform.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        layoutElement.flexibleWidth = 0f;
    }

    private void FitDescriptionLayout(string description)
    {
        if (_panelRectTransform == null || _descriptionText == null)
        {
            return;
        }

        float panelWidth = CalculatePanelWidth(description, out float descriptionWidth);
        LayoutElement panelLayoutElement = _panelRectTransform.GetComponent<LayoutElement>();

        if (panelLayoutElement != null)
        {
            panelLayoutElement.minWidth = panelWidth;
            panelLayoutElement.preferredWidth = panelWidth;
            panelLayoutElement.flexibleWidth = 0f;
        }

        _panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
        SetDescriptionWidth(descriptionWidth);
    }

    private float CalculatePanelWidth(string description, out float descriptionWidth)
    {
        float maxPanelWidth = GetMaxPanelWidth();
        float minPanelWidth = Mathf.Clamp(GetMinimumPanelWidth(), MIN_PANEL_WIDTH, maxPanelWidth);
        float horizontalPadding = GetPanelHorizontalPadding();
        descriptionWidth = Mathf.Max(1f, minPanelWidth - horizontalPadding);

        if (string.IsNullOrWhiteSpace(description))
        {
            return minPanelWidth;
        }

        float minDescriptionWidth = Mathf.Max(1f, minPanelWidth - horizontalPadding);
        float maxDescriptionWidth = Mathf.Max(minDescriptionWidth, maxPanelWidth - horizontalPadding);
        descriptionWidth = CalculateDescriptionWidth(description, minDescriptionWidth, maxDescriptionWidth);

        return Mathf.Clamp(descriptionWidth + horizontalPadding, minPanelWidth, maxPanelWidth);
    }

    private void SetDescriptionWidth(float width)
    {
        _descriptionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        LayoutElement descriptionLayoutElement = GetDescriptionLayoutElement();
        descriptionLayoutElement.minWidth = 0f;
        descriptionLayoutElement.preferredWidth = width;
        descriptionLayoutElement.flexibleWidth = 0f;
    }

    private LayoutElement GetDescriptionLayoutElement()
    {
        if (_descriptionLayoutElement != null)
        {
            return _descriptionLayoutElement;
        }

        if (_descriptionText.TryGetComponent(out _descriptionLayoutElement) == false)
        {
            _descriptionLayoutElement = _descriptionText.gameObject.AddComponent<LayoutElement>();
        }

        return _descriptionLayoutElement;
    }

    private float CalculateDescriptionWidth(string description, float minWidth, float maxWidth)
    {
        float longestWordWidth = GetLongestWordWidth(description);
        float singleLineWidth = _descriptionText.GetPreferredValues(description).x;
        int targetLineCount = Mathf.Clamp(Mathf.CeilToInt(description.Length / (float)TARGET_DESCRIPTION_CHARACTERS_PER_LINE), MIN_DESCRIPTION_LINE_COUNT, MAX_DESCRIPTION_LINE_COUNT);
        float targetWidth = Mathf.Max(singleLineWidth / targetLineCount, longestWordWidth);
        float width = Mathf.Clamp(targetWidth, minWidth, maxWidth);
        float lineHeight = Mathf.Max(1f, _descriptionText.GetPreferredValues("A", maxWidth, 0f).y);
        float targetHeight = lineHeight * targetLineCount;

        while (width < maxWidth && _descriptionText.GetPreferredValues(description, width, 0f).y > targetHeight)
        {
            width = Mathf.Min(width + DESCRIPTION_WIDTH_STEP, maxWidth);
        }

        while (width - DESCRIPTION_WIDTH_STEP >= minWidth && _descriptionText.GetPreferredValues(description, width - DESCRIPTION_WIDTH_STEP, 0f).y <= targetHeight)
        {
            width -= DESCRIPTION_WIDTH_STEP;
        }

        return width;
    }

    private float GetLongestWordWidth(string description)
    {
        float width = 0f;
        string[] words = description.Split(_descriptionWordSeparators, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            width = Mathf.Max(width, _descriptionText.GetPreferredValues(words[i]).x);
        }

        return width;
    }

    private float GetMinimumPanelWidth()
    {
        float width = MIN_PANEL_WIDTH;
        width = Mathf.Max(width, GetPreferredTextWidth(_itemNameText));
        width = Mathf.Max(width, GetPreferredTextWidth(_typeText));
        width = Mathf.Max(width, GetPreferredTextWidth(_weightText));
        width = Mathf.Max(width, GetPreferredTextWidth(_durabilityText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoFleshDamageText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoArmorPenetrationText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoBulletSpeedText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoBulletMassText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoBulletDiameterText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoRicochetChanceText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoRecoilModifierText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ammoDurabilityLossModifierText));
        width = Mathf.Max(width, GetPreferredTextWidth(_moduleMagazineSizeText));
        width = Mathf.Max(width, GetPreferredTextWidth(_moduleAccuracyText));
        width = Mathf.Max(width, GetPreferredTextWidth(_ergonomicsText));

        if (_iconLayoutElement != null)
        {
            width = Mathf.Max(width, _iconLayoutElement.preferredWidth);
        }

        return width + GetPanelHorizontalPadding();
    }

    private static float GetPreferredTextWidth(TMP_Text text)
    {
        if (text == null || text.gameObject.activeInHierarchy == false || string.IsNullOrEmpty(text.text))
        {
            return 0f;
        }

        return text.GetPreferredValues(text.text).x;
    }

    private float GetPanelHorizontalPadding()
    {
        if (_panelRectTransform != null && _panelRectTransform.TryGetComponent(out LayoutGroup layoutGroup))
        {
            return layoutGroup.padding.left + layoutGroup.padding.right;
        }

        return DEFAULT_PANEL_HORIZONTAL_PADDING;
    }

    private float GetPanelLeftPadding()
    {
        if (_panelRectTransform != null && _panelRectTransform.TryGetComponent(out LayoutGroup layoutGroup))
        {
            return layoutGroup.padding.left;
        }

        return DEFAULT_PANEL_HORIZONTAL_PADDING * 0.5f;
    }

    private float GetMaxPanelWidth()
    {
        float screenWidth = Screen.width;
        Canvas canvas = _panelRectTransform.GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        float availableWidth = (screenWidth - _screenPadding.x * 2f) / scaleFactor;

        return Mathf.Max(MIN_PANEL_WIDTH, Mathf.Min(MAX_PANEL_WIDTH, availableWidth));
    }

    private void CenterOnScreen()
    {
        _positioner.CenterOnScreen(_panelRectTransform, _screenPadding);
    }

    private void ClampToScreen()
    {
        _positioner.ClampToScreen(_panelRectTransform, _screenPadding);
    }
}
