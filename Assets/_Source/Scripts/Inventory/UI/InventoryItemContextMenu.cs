using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryItemContextMenu : MonoBehaviour
{
    private static readonly Vector2 _menuOffset = new(12f, -8f);
    private static readonly Vector2 _screenPadding = new(8f, 8f);
    private static readonly Color _textColor = new(0.92f, 0.9f, 0.82f, 1f);
    private static readonly Color _disabledTextColor = new(0.48f, 0.47f, 0.42f, 1f);

    [SerializeField] private RectTransform _panelRectTransform;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Button _dropOneButton;
    [SerializeField] private Button _dropStackButton;
    [SerializeField] private TMP_Text _dropStackButtonText;
    [SerializeField] [Min(0f)] private float _closeRadius = 220f;
    [SerializeField] private bool _showCloseRadiusInEditor = true;
    [SerializeField] private Color _closeRadiusEditorColor = new(1f, 0.65f, 0f, 0.9f);

    private Action _onDropOne;
    private Action _onDropStack;
    private InventoryContextMenuPositioner _positioner;

    public bool IsOpen => gameObject.activeSelf;

    private InventoryContextMenuPositioner Positioner
    {
        get
        {
            _positioner ??= new InventoryContextMenuPositioner(_panelRectTransform, _canvas, transform, _menuOffset, _screenPadding);
            return _positioner;
        }
    }

    private void Awake()
    {
        Hide();
    }

    public void Initialize(Action dropOneAction, Action dropStackAction)
    {
        _onDropOne = dropOneAction;
        _onDropStack = dropStackAction;

        if (_dropOneButton != null)
        {
            _dropOneButton.onClick.RemoveListener(HandleDropOneClicked);
            _dropOneButton.onClick.AddListener(HandleDropOneClicked);
        }

        if (_dropStackButton != null)
        {
            _dropStackButton.onClick.RemoveListener(HandleDropStackClicked);
            _dropStackButton.onClick.AddListener(HandleDropStackClicked);
        }
    }

    public void Show(InventoryItem item, Vector2 screenPosition)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        bool canDropStack = item.IsStackable && item.CurrentAmount > 1;
        SetDropStackButtonEnabled(canDropStack);
        RebuildLayout();
        Positioner.SetPosition(screenPosition);
    }

    public void Hide() => gameObject.SetActive(false);
    public bool ContainsScreenPoint(Vector2 screenPoint) => IsOpen && Positioner.ContainsScreenPoint(screenPoint);

    public bool ShouldCloseForPointer(Vector2 screenPoint)
    {
        if (IsOpen == false || _closeRadius <= 0f)
        {
            return false;
        }

        return Positioner.ShouldCloseForPointer(screenPoint, _closeRadius);
    }

    private void HandleDropOneClicked() => _onDropOne?.Invoke();
    private void HandleDropStackClicked() => _onDropStack?.Invoke();

    private void SetDropStackButtonEnabled(bool enabled)
    {
        if (_dropStackButton != null)
        {
            _dropStackButton.interactable = enabled;
        }

        if (_dropStackButtonText != null)
        {
            _dropStackButtonText.color = enabled ? _textColor : _disabledTextColor;
        }
    }

    private void RebuildLayout()
    {
        if (_panelRectTransform == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_showCloseRadiusInEditor == false || _closeRadius <= 0f)
        {
            return;
        }

        Vector3 center = Positioner.GetPanelCenterWorldPoint();

        Handles.color = _closeRadiusEditorColor;
        Handles.DrawWireDisc(center, _panelRectTransform == null ? Vector3.forward : _panelRectTransform.forward, Positioner.GetCloseRadiusWorldUnits(_closeRadius, center));
    }
#endif
}