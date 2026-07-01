using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InventoryCountDragWindow : MonoBehaviour, IScrollHandler
{
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _selectedItemsWeightText;
    [SerializeField] private TMP_InputField _countInputField;
    [SerializeField] private Button _minusButton;
    [SerializeField] private Button _plusButton;
    [SerializeField] private Scrollbar _countScrollbar;
    [SerializeField] private TMP_Text _minCountText;
    [SerializeField] private TMP_Text _maxCountText;
    [SerializeField] private Button _halfCountButton;
    [SerializeField] private Button _allCountButton;
    [SerializeField] private Button _cancelCountButton;
    [SerializeField] private Button _applyCountButton;

    private Action<int> _apply;
    private Action _cancel;
    private ItemData _itemData;
    private int _minCount = 1;
    private int _maxCount = 1;
    private int _currentCount = 1;
    private float _unitWeight;
    private bool _initialized;
    private bool _isSettingValue;
    private bool _isShowing;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        EnsureInitialized();

        if (_isShowing == false)
        {
            Hide();
        }
    }

    private void OnDestroy()
    {
        if (_countInputField != null)
        {
            _countInputField.onValueChanged.RemoveListener(HandleInputValueChanged);
            _countInputField.onEndEdit.RemoveListener(HandleInputEndEdit);
        }

        if (_countScrollbar != null)
        {
            _countScrollbar.onValueChanged.RemoveListener(HandleScrollbarValueChanged);
        }

        RemoveButtonListener(_minusButton, HandleMinusClicked);
        RemoveButtonListener(_plusButton, HandlePlusClicked);
        RemoveButtonListener(_halfCountButton, HandleHalfClicked);
        RemoveButtonListener(_allCountButton, HandleAllClicked);
        RemoveButtonListener(_cancelCountButton, HandleCancelClicked);
        RemoveButtonListener(_applyCountButton, HandleApplyClicked);
    }

    public void Show(InventoryItem item, int maxCount, Action<int> apply, Action cancel)
    {
        EnsureInitialized();

        if (item == null || item.ItemData == null || maxCount <= 1 || apply == null)
        {
            cancel?.Invoke();
            return;
        }

        _itemData = item.ItemData;
        _unitWeight = item.UnitWeight;
        _minCount = 1;
        _maxCount = Mathf.Max(_minCount, maxCount);
        _apply = apply;
        _cancel = cancel;

        _isShowing = true;
        gameObject.SetActive(true);
        _isShowing = false;
        transform.SetAsLastSibling();

        SetCount(_minCount);

        if (_countInputField != null)
        {
            _countInputField.Select();
            _countInputField.ActivateInputField();
        }
    }

    public void Cancel()
    {
        Action cancel = _cancel;
        Hide();
        cancel?.Invoke();
    }

    public void Hide()
    {
        _apply = null;
        _cancel = null;
        _itemData = null;
        gameObject.SetActive(false);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData == null || Mathf.Approximately(eventData.scrollDelta.y, 0f))
        {
            return;
        }

        SetCount(_currentCount + (eventData.scrollDelta.y > 0f ? 1 : -1));
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        ResolveReferences();

        if (_countInputField != null)
        {
            _countInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            _countInputField.onValueChanged.AddListener(HandleInputValueChanged);
            _countInputField.onEndEdit.AddListener(HandleInputEndEdit);
        }

        if (_countScrollbar != null)
        {
            _countScrollbar.onValueChanged.AddListener(HandleScrollbarValueChanged);
        }

        AddButtonListener(_minusButton, HandleMinusClicked);
        AddButtonListener(_plusButton, HandlePlusClicked);
        AddButtonListener(_halfCountButton, HandleHalfClicked);
        AddButtonListener(_allCountButton, HandleAllClicked);
        AddButtonListener(_cancelCountButton, HandleCancelClicked);
        AddButtonListener(_applyCountButton, HandleApplyClicked);

        _initialized = true;
    }

    private void ResolveReferences()
    {
        _itemNameText = ResolveComponent(_itemNameText, "Item Name Text");
        _selectedItemsWeightText = ResolveComponent(_selectedItemsWeightText, "Selected Items Weight Text");
        _countInputField = ResolveComponent(_countInputField, "Count Input Field");
        _minusButton = ResolveComponent(_minusButton, "Minus Button");
        _plusButton = ResolveComponent(_plusButton, "Plus Button");
        _countScrollbar = ResolveComponent(_countScrollbar, "Count Scrollbar");
        _minCountText = ResolveComponent(_minCountText, "Min Count Text");
        _maxCountText = ResolveComponent(_maxCountText, "Max Count Text");
        _halfCountButton = ResolveComponent(_halfCountButton, "Half Count Button");
        _allCountButton = ResolveComponent(_allCountButton, "All Count Button");
        _cancelCountButton = ResolveComponent(_cancelCountButton, "Cancel Count Button");
        _applyCountButton = ResolveComponent(_applyCountButton, "Apply Count Button");
    }

    private T ResolveComponent<T>(T currentValue, string childName) where T : Component
    {
        if (currentValue != null)
        {
            return currentValue;
        }

        Transform child = FindChildRecursive(transform, childName);
        return child == null ? null : child.GetComponent<T>();
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

    private void SetCount(int count)
    {
        _currentCount = Mathf.Clamp(count, _minCount, _maxCount);
        RefreshTexts();
        RefreshInput();
        RefreshScrollbar();
    }

    private void RefreshTexts()
    {
        if (_itemNameText != null)
        {
            _itemNameText.text = _itemData == null ? string.Empty : _itemData.ItemName;
        }

        if (_selectedItemsWeightText != null)
        {
            _selectedItemsWeightText.text = $"\u0412\u0435\u0441: {FormatWeight(_unitWeight * _currentCount)}";
        }

        if (_minCountText != null)
        {
            _minCountText.text = $"x{_minCount}";
        }

        if (_maxCountText != null)
        {
            _maxCountText.text = $"x{_maxCount}";
        }
    }

    private void RefreshInput()
    {
        if (_countInputField == null)
        {
            return;
        }

        string countText = _currentCount.ToString();

        if (_countInputField.text == countText)
        {
            return;
        }

        _isSettingValue = true;
        _countInputField.SetTextWithoutNotify(countText);
        _isSettingValue = false;
    }

    private void RefreshScrollbar()
    {
        if (_countScrollbar == null)
        {
            return;
        }

        _isSettingValue = true;
        _countScrollbar.numberOfSteps = Mathf.Max(0, _maxCount - _minCount + 1);
        _countScrollbar.SetValueWithoutNotify(_maxCount <= _minCount ? 0f : Mathf.InverseLerp(_minCount, _maxCount, _currentCount));
        _isSettingValue = false;
    }

    private static string FormatWeight(float weight)
    {
        float normalizedWeight = Mathf.Max(0f, weight);

        if (normalizedWeight < 1f)
        {
            return $"{Mathf.RoundToInt(normalizedWeight * 1000f)} \u0413\u0420";
        }

        return $"{normalizedWeight:0.#} \u041a\u0413";
    }

    private void HandleInputValueChanged(string value)
    {
        if (_isSettingValue || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (int.TryParse(value, out int count))
        {
            SetCount(count);
        }
    }

    private void HandleInputEndEdit(string value)
    {
        if (int.TryParse(value, out int count))
        {
            SetCount(count);
            return;
        }

        SetCount(_currentCount);
    }

    private void HandleScrollbarValueChanged(float value)
    {
        if (_isSettingValue)
        {
            return;
        }

        SetCount(_maxCount <= _minCount ? _minCount : Mathf.RoundToInt(Mathf.Lerp(_minCount, _maxCount, value)));
    }

    private void HandleMinusClicked() => SetCount(_currentCount - 1);
    private void HandlePlusClicked() => SetCount(_currentCount + 1);
    private void HandleHalfClicked() => SetCount(Mathf.Max(_minCount, _maxCount / 2));
    private void HandleAllClicked() => SetCount(_maxCount);
    private void HandleCancelClicked() => Cancel();

    private void HandleApplyClicked()
    {
        Action<int> apply = _apply;
        int count = _currentCount;

        Hide();
        apply?.Invoke(count);
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button != null)
        {
            button.onClick.AddListener(listener);
        }
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(listener);
        }
    }
}
