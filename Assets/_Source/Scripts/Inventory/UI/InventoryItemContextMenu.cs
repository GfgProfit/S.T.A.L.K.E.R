using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryItemContextMenu : MonoBehaviour, IView<InventoryItemContextMenuViewModel>
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
    private InventoryItemContextMenuViewModel _viewModel;
    private IDisposable _isVisibleSubscription;
    private IDisposable _canDropStackSubscription;

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
        if (_viewModel == null)
        {
            Hide();
        }
    }

    public void Initialize(Action dropOneAction, Action dropStackAction)
    {
        _onDropOne = dropOneAction;
        _onDropStack = dropStackAction;
        Bind(InventoryViewModelFactory.CreateContextMenu(_onDropOne, _onDropStack));

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

    public void Show(bool canDropStack, Vector2 screenPosition)
    {
        if (_viewModel == null)
        {
            Hide();
            return;
        }

        _viewModel.Show(canDropStack);
        transform.SetAsLastSibling();

        RebuildLayout();
        Positioner.SetPosition(screenPosition);
    }

    public void Hide()
    {
        if (_viewModel == null)
        {
            SetVisible(false);
            return;
        }

        _viewModel.Hide();
    }

    public bool ContainsScreenPoint(Vector2 screenPoint) => IsOpen && Positioner.ContainsScreenPoint(screenPoint);

    public bool ShouldCloseForPointer(Vector2 screenPoint)
    {
        if (IsOpen == false || _closeRadius <= 0f)
        {
            return false;
        }

        return Positioner.ShouldCloseForPointer(screenPoint, _closeRadius);
    }

    public void Bind(InventoryItemContextMenuViewModel viewModel)
    {
        Unbind();
        _viewModel?.Dispose();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _isVisibleSubscription = _viewModel.IsVisible.Subscribe(SetVisible);
        _canDropStackSubscription = _viewModel.CanDropStack.Subscribe(SetDropStackButtonEnabled);
    }

    public void Unbind()
    {
        _isVisibleSubscription?.Dispose();
        _canDropStackSubscription?.Dispose();
        _isVisibleSubscription = null;
        _canDropStackSubscription = null;
    }

    private void OnDestroy()
    {
        UnsubscribeButtons();
        Unbind();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void HandleDropOneClicked() => ExecuteCommandAsync(_viewModel?.DropOneCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleDropStackClicked() => ExecuteCommandAsync(_viewModel?.DropStackCommand, destroyCancellationToken).Forget(Debug.LogException);

    private void UnsubscribeButtons()
    {
        if (_dropOneButton != null)
        {
            _dropOneButton.onClick.RemoveListener(HandleDropOneClicked);
        }

        if (_dropStackButton != null)
        {
            _dropStackButton.onClick.RemoveListener(HandleDropStackClicked);
        }
    }

    private static async UniTask ExecuteCommandAsync(AsyncReactiveCommand command, CancellationToken cancellationToken)
    {
        if (command == null)
        {
            return;
        }

        await command.ExecuteAsync(cancellationToken);
    }

    private void SetVisible(bool visible) => gameObject.SetActive(visible);

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
