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
    [SerializeField] private Button _useButton;
    [SerializeField] private TMP_Text _useButtonText;
    [SerializeField] private Button _unloadButton;
    [SerializeField] private TMP_Text _unloadButtonText;
    [SerializeField] private Button _equipPrimaryWeaponButton;
    [SerializeField] private TMP_Text _equipPrimaryWeaponButtonText;
    [SerializeField] private Button _equipSecondaryWeaponButton;
    [SerializeField] private TMP_Text _equipSecondaryWeaponButtonText;
    [SerializeField] private Button _equipButton;
    [SerializeField] private TMP_Text _equipButtonText;
    [SerializeField] private Button _unequipButton;
    [SerializeField] private TMP_Text _unequipButtonText;
    [SerializeField] private string _unloadButtonLabel = "\u0420\u0430\u0437\u0440\u044f\u0434\u0438\u0442\u044c";
    [SerializeField] private string _equipPrimaryWeaponButtonLabel = "\u042d\u043a\u0438\u043f\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u0432 \u043e\u0441\u043d\u043e\u0432\u043d\u043e\u0439 \u0441\u043b\u043e\u0442";
    [SerializeField] private string _equipSecondaryWeaponButtonLabel = "\u042d\u043a\u0438\u043f\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u0432\u043e \u0432\u0442\u043e\u0440\u0438\u0447\u043d\u044b\u0439 \u0441\u043b\u043e\u0442";
    [SerializeField] private string _equipButtonLabel = "\u042d\u043a\u0438\u043f\u0438\u0440\u043e\u0432\u0430\u0442\u044c";
    [SerializeField] private string _unequipButtonLabel = "\u0421\u043d\u044f\u0442\u044c";
    [SerializeField] private Button _dropOneButton;
    [SerializeField] private Button _dropStackButton;
    [SerializeField] private TMP_Text _dropStackButtonText;
    [SerializeField] [Min(0f)] private float _closeRadius = 220f;
    [SerializeField] private bool _showCloseRadiusInEditor = true;
    [SerializeField] private Color _closeRadiusEditorColor = new(1f, 0.65f, 0f, 0.9f);

    private Action _onUse;
    private Action _onUnload;
    private Action _onEquipPrimaryWeapon;
    private Action _onEquipSecondaryWeapon;
    private Action _onEquip;
    private Action _onUnequip;
    private Action _onDropOne;
    private Action _onDropStack;
    private InventoryContextMenuPositioner _positioner;
    private InventoryItemContextMenuViewModel _viewModel;
    private IDisposable _isVisibleSubscription;
    private IDisposable _canUseSubscription;
    private IDisposable _showUnloadSubscription;
    private IDisposable _canUnloadSubscription;
    private IDisposable _canEquipPrimaryWeaponSubscription;
    private IDisposable _canEquipSecondaryWeaponSubscription;
    private IDisposable _canEquipSubscription;
    private IDisposable _canUnequipSubscription;
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

    public void Initialize(Action useAction, Action unloadAction, Action equipPrimaryWeaponAction, Action equipSecondaryWeaponAction, Action equipAction, Action unequipAction, Action dropOneAction, Action dropStackAction)
    {
        _onUse = useAction;
        _onUnload = unloadAction;
        _onEquipPrimaryWeapon = equipPrimaryWeaponAction;
        _onEquipSecondaryWeapon = equipSecondaryWeaponAction;
        _onEquip = equipAction;
        _onUnequip = unequipAction;
        _onDropOne = dropOneAction;
        _onDropStack = dropStackAction;
        EnsureContextButtons();
        Bind(InventoryViewModelFactory.CreateContextMenu(_onUse, _onUnload, _onEquipPrimaryWeapon, _onEquipSecondaryWeapon, _onEquip, _onUnequip, _onDropOne, _onDropStack));

        if (_useButton != null)
        {
            _useButton.onClick.RemoveListener(HandleUseClicked);
            _useButton.onClick.AddListener(HandleUseClicked);
        }

        if (_dropOneButton != null)
        {
            _dropOneButton.onClick.RemoveListener(HandleDropOneClicked);
            _dropOneButton.onClick.AddListener(HandleDropOneClicked);
        }

        if (_unloadButton != null)
        {
            _unloadButton.onClick.RemoveListener(HandleUnloadClicked);
            _unloadButton.onClick.AddListener(HandleUnloadClicked);
        }

        if (_equipPrimaryWeaponButton != null)
        {
            _equipPrimaryWeaponButton.onClick.RemoveListener(HandleEquipPrimaryWeaponClicked);
            _equipPrimaryWeaponButton.onClick.AddListener(HandleEquipPrimaryWeaponClicked);
        }

        if (_equipSecondaryWeaponButton != null)
        {
            _equipSecondaryWeaponButton.onClick.RemoveListener(HandleEquipSecondaryWeaponClicked);
            _equipSecondaryWeaponButton.onClick.AddListener(HandleEquipSecondaryWeaponClicked);
        }

        if (_equipButton != null)
        {
            _equipButton.onClick.RemoveListener(HandleEquipClicked);
            _equipButton.onClick.AddListener(HandleEquipClicked);
        }

        if (_unequipButton != null)
        {
            _unequipButton.onClick.RemoveListener(HandleUnequipClicked);
            _unequipButton.onClick.AddListener(HandleUnequipClicked);
        }

        if (_dropStackButton != null)
        {
            _dropStackButton.onClick.RemoveListener(HandleDropStackClicked);
            _dropStackButton.onClick.AddListener(HandleDropStackClicked);
        }
    }

    public void Show(bool canUse, bool showUnload, bool canUnload, bool canEquipPrimaryWeapon, bool canEquipSecondaryWeapon, bool canEquip, bool canUnequip, bool canDropStack, Vector2 screenPosition)
    {
        if (_viewModel == null)
        {
            Hide();
            return;
        }

        _viewModel.Show(canUse, showUnload, canUnload, canEquipPrimaryWeapon, canEquipSecondaryWeapon, canEquip, canUnequip, canDropStack);
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
        _canUseSubscription = _viewModel.CanUse.Subscribe(SetUseButtonVisible);
        _showUnloadSubscription = _viewModel.ShowUnload.Subscribe(SetUnloadButtonVisible);
        _canUnloadSubscription = _viewModel.CanUnload.Subscribe(SetUnloadButtonVisible);
        _canEquipPrimaryWeaponSubscription = _viewModel.CanEquipPrimaryWeapon.Subscribe(SetEquipPrimaryWeaponButtonVisible);
        _canEquipSecondaryWeaponSubscription = _viewModel.CanEquipSecondaryWeapon.Subscribe(SetEquipSecondaryWeaponButtonVisible);
        _canEquipSubscription = _viewModel.CanEquip.Subscribe(SetEquipButtonVisible);
        _canUnequipSubscription = _viewModel.CanUnequip.Subscribe(SetUnequipButtonVisible);
        _canDropStackSubscription = _viewModel.CanDropStack.Subscribe(SetDropStackButtonVisible);
    }

    public void Unbind()
    {
        _isVisibleSubscription?.Dispose();
        _canUseSubscription?.Dispose();
        _showUnloadSubscription?.Dispose();
        _canUnloadSubscription?.Dispose();
        _canEquipPrimaryWeaponSubscription?.Dispose();
        _canEquipSecondaryWeaponSubscription?.Dispose();
        _canEquipSubscription?.Dispose();
        _canUnequipSubscription?.Dispose();
        _canDropStackSubscription?.Dispose();
        _isVisibleSubscription = null;
        _canUseSubscription = null;
        _showUnloadSubscription = null;
        _canUnloadSubscription = null;
        _canEquipPrimaryWeaponSubscription = null;
        _canEquipSecondaryWeaponSubscription = null;
        _canEquipSubscription = null;
        _canUnequipSubscription = null;
        _canDropStackSubscription = null;
    }

    private void OnDestroy()
    {
        UnsubscribeButtons();
        Unbind();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void HandleUseClicked() => ExecuteCommandAsync(_viewModel?.UseCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleUnloadClicked() => ExecuteCommandAsync(_viewModel?.UnloadCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleEquipPrimaryWeaponClicked() => ExecuteCommandAsync(_viewModel?.EquipPrimaryWeaponCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleEquipSecondaryWeaponClicked() => ExecuteCommandAsync(_viewModel?.EquipSecondaryWeaponCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleEquipClicked() => ExecuteCommandAsync(_viewModel?.EquipCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleUnequipClicked() => ExecuteCommandAsync(_viewModel?.UnequipCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleDropOneClicked() => ExecuteCommandAsync(_viewModel?.DropOneCommand, destroyCancellationToken).Forget(Debug.LogException);
    private void HandleDropStackClicked() => ExecuteCommandAsync(_viewModel?.DropStackCommand, destroyCancellationToken).Forget(Debug.LogException);

    private void UnsubscribeButtons()
    {
        if (_useButton != null)
        {
            _useButton.onClick.RemoveListener(HandleUseClicked);
        }

        if (_unloadButton != null)
        {
            _unloadButton.onClick.RemoveListener(HandleUnloadClicked);
        }

        if (_equipPrimaryWeaponButton != null)
        {
            _equipPrimaryWeaponButton.onClick.RemoveListener(HandleEquipPrimaryWeaponClicked);
        }

        if (_equipSecondaryWeaponButton != null)
        {
            _equipSecondaryWeaponButton.onClick.RemoveListener(HandleEquipSecondaryWeaponClicked);
        }

        if (_equipButton != null)
        {
            _equipButton.onClick.RemoveListener(HandleEquipClicked);
        }

        if (_unequipButton != null)
        {
            _unequipButton.onClick.RemoveListener(HandleUnequipClicked);
        }

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

    private void SetUseButtonVisible(bool visible) => SetButtonVisible(_useButton, _useButtonText, visible);
    private void SetUnloadButtonVisible(bool visible) => SetButtonVisible(_unloadButton, _unloadButtonText, visible);
    private void SetEquipPrimaryWeaponButtonVisible(bool visible) => SetButtonVisible(_equipPrimaryWeaponButton, _equipPrimaryWeaponButtonText, visible);
    private void SetEquipSecondaryWeaponButtonVisible(bool visible) => SetButtonVisible(_equipSecondaryWeaponButton, _equipSecondaryWeaponButtonText, visible);
    private void SetEquipButtonVisible(bool visible) => SetButtonVisible(_equipButton, _equipButtonText, visible);
    private void SetUnequipButtonVisible(bool visible) => SetButtonVisible(_unequipButton, _unequipButtonText, visible);
    private void SetDropStackButtonVisible(bool visible) => SetButtonVisible(_dropStackButton, _dropStackButtonText, visible);

    private static void SetButtonVisible(Button button, TMP_Text buttonText, bool visible)
    {
        if (button != null)
        {
            button.interactable = visible;
            button.gameObject.SetActive(visible);
        }

        if (buttonText != null)
        {
            buttonText.color = visible ? _textColor : _disabledTextColor;
        }
    }

    private void EnsureContextButtons()
    {
        int nextSiblingIndex = GetNextButtonSiblingIndex(_useButton);
        EnsureActionButton(ref _equipPrimaryWeaponButton, ref _equipPrimaryWeaponButtonText, "Equip Primary Weapon Button", _equipPrimaryWeaponButtonLabel, nextSiblingIndex++);
        EnsureActionButton(ref _equipSecondaryWeaponButton, ref _equipSecondaryWeaponButtonText, "Equip Secondary Weapon Button", _equipSecondaryWeaponButtonLabel, nextSiblingIndex++);
        EnsureActionButton(ref _equipButton, ref _equipButtonText, "Equip Button", _equipButtonLabel, nextSiblingIndex++);
        EnsureActionButton(ref _unequipButton, ref _unequipButtonText, "Unequip Button", _unequipButtonLabel, nextSiblingIndex++);
        EnsureActionButton(ref _unloadButton, ref _unloadButtonText, "Unload Button", _unloadButtonLabel, nextSiblingIndex);
    }

    private void EnsureActionButton(ref Button button, ref TMP_Text buttonText, string objectName, string label, int siblingIndex)
    {
        if (button != null)
        {
            SetButtonText(buttonText, label);
            return;
        }

        Button template = _useButton != null ? _useButton : _dropOneButton;

        if (template == null || template.transform.parent == null)
        {
            return;
        }

        GameObject buttonObject = Instantiate(template.gameObject, template.transform.parent);
        buttonObject.name = objectName;
        buttonObject.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, template.transform.parent.childCount - 1));
        buttonObject.SetActive(false);

        button = buttonObject.GetComponent<Button>();
        buttonText = buttonObject.GetComponentInChildren<TMP_Text>(true);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }

        SetButtonText(buttonText, label);
    }

    private static int GetNextButtonSiblingIndex(Button button) => button == null ? 0 : button.transform.GetSiblingIndex() + 1;

    private static void SetButtonText(TMP_Text buttonText, string label)
    {
        if (buttonText != null)
        {
            buttonText.text = label;
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
