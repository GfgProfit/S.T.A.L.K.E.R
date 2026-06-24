using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform _panelRectTransform;
    [SerializeField] private Vector2 _cursorOffset = new(24f, -24f);
    [SerializeField] private Vector2 _screenPadding = new(16f, 16f);
    [SerializeField] private TMP_Text _itemNameText;

    [Inject] private IPlayerInput _playerInput = null;

    private readonly ItemInfoPanelPositioner _positioner = new();
    private IPlayerInput _fallbackPlayerInput;
    private bool _referencesResolved;
    private bool _isActivatingForShow;

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
        ResolveReferences();

        if (_isActivatingForShow == false)
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
        if (item.IsValid == false)
        {
            Hide();
            return;
        }

        Show(item.ItemData.ItemName);
    }

    public void Show(string itemName)
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(itemName))
        {
            Hide();
            return;
        }

        if (_itemNameText != null)
        {
            _itemNameText.text = itemName;
        }

        _isActivatingForShow = true;
        gameObject.SetActive(true);
        _isActivatingForShow = false;
        RebuildLayout();
        UpdatePosition();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetPlayerInput(IPlayerInput playerInput)
    {
        _playerInput = playerInput;
    }

    private void ResolveReferences()
    {
        if (_referencesResolved)
        {
            return;
        }

        if (_panelRectTransform == null)
        {
            _panelRectTransform = transform as RectTransform;
        }

        if (_itemNameText == null)
        {
            Transform nameTransform = FindChildRecursive(transform, "Item Tooltip Name Text");

            if (nameTransform != null)
            {
                _itemNameText = nameTransform.GetComponent<TMP_Text>();
            }
        }

        if (_itemNameText == null)
        {
            _itemNameText = GetComponentInChildren<TMP_Text>(true);
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

    private void RebuildLayout()
    {
        if (_panelRectTransform == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
    }

    private void UpdatePosition()
    {
        if (gameObject.activeInHierarchy == false)
        {
            return;
        }

        _positioner.UpdatePosition(_panelRectTransform, PlayerInput.GetPointerPosition(), _cursorOffset, _screenPadding);
    }
}
