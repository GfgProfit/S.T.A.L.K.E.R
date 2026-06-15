using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour
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
        Hide();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    public void Show(InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        SetIcon(item);
        SetItemName(item.ItemData);
        SetType(item.ItemData);
        SetWeight(item);
        SetDurability(item);
        SetStatsInfo(item);
        SetDescription(item.ItemData);
        RebuildLayout();
        UpdatePosition();
    }

    public void Hide() => gameObject.SetActive(false);

    private void SetStatsInfo(InventoryItem item)
    {
        if (_statsInfoPanel == null)
        {
            return;
        }

        GameProjectSettings settings = GameProjectSettings.LoadDefault();
        _statsInfoPanel.RenderItemStats(item, settings.StatCurrentValueColor, settings.StatFullDurabilityValueColor, _hideStatsInfoWhenEmpty);
    }

    private void SetIcon(InventoryItem item)
    {
        Vector2 iconSize = new(item.BaseWidth * ItemGrid.TILE_SIZE_WIDTH, item.BaseHeight * ItemGrid.TILE_SIZE_HEIGHT);

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

        if (_iconImage == null)
        {
            return;
        }

        Sprite icon = item.ItemData.GetIcon(item.RuntimeIconParts);
        _iconImage.sprite = icon;
        _iconImage.enabled = icon != null;
        _iconImage.rectTransform.localRotation = Quaternion.identity;
        _iconImage.rectTransform.localScale = Vector3.one;
    }

    private void SetItemName(ItemData itemData)
    {
        if (_itemNameText == null)
        {
            return;
        }

        _itemNameText.text = itemData.ItemName;
    }

    private void SetType(ItemData itemData)
    {
        if (_typeText == null)
        {
            return;
        }

        _typeText.text = ItemInfoPanelTextFormatter.FormatType(itemData);
        _typeText.gameObject.SetActive(true);
    }

    private void SetWeight(InventoryItem item)
    {
        if (_weightText == null)
        {
            return;
        }

        _weightText.text = ItemInfoPanelTextFormatter.FormatWeight(item);
    }

    private void SetDurability(InventoryItem item)
    {
        if (_durabilityText == null)
        {
            return;
        }

        bool showDurability = item != null && item.HasDurability;
        _durabilityText.gameObject.SetActive(showDurability);

        if (showDurability == false)
        {
            return;
        }

        float durabilityPercent = item.CurrentDurabilityPercent;
        _durabilityText.richText = true;
        _durabilityText.text = ItemInfoPanelTextFormatter.FormatDurability(item, GameProjectSettings.LoadDefault().GetDurabilityColor(durabilityPercent));
    }

    private void SetDescription(ItemData itemData)
    {
        if (_descriptionText == null)
        {
            return;
        }

        _descriptionText.text = ItemInfoPanelTextFormatter.WrapDescription(itemData.Description, _descriptionWordsPerLine);
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