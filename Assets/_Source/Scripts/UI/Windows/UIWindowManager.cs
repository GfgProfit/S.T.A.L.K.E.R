using System.Collections.Generic;
using R3;
using UnityEngine;

[DefaultExecutionOrder(-30)]
public sealed class UIWindowManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool _handleEscapeInput = true;

    [Header("Cursor")]
    [SerializeField] private bool _manageCursor = true;

    [Header("Gameplay Input")]
    [SerializeField] private bool _manageGameplayInput = true;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Behaviour[] _disableWhileGameplayInputBlocked;
    [SerializeField] private GameObject[] _disableChildrenWhileGameplayInputBlocked;

    [Inject] private IPlayerInput _playerInput = null;

    private readonly List<IUIWindow> _stack = new();
    private readonly List<Behaviour> _disabledBehaviours = new();
    private readonly List<bool> _disabledBehaviourStates = new();
    private readonly List<Behaviour> _childrenBehaviourBuffer = new();
    private readonly ReactiveProperty<bool> _hasOpenWindows = new();
    private readonly ReactiveProperty<IUIWindow> _topWindow = new();
    private readonly ReactiveProperty<bool> _requiresCursor = new();
    private readonly ReactiveProperty<bool> _blocksGameplayInput = new();
    private readonly ReactiveProperty<bool> _isGamePaused = new();
    private IPlayerInput _fallbackPlayerInput;
    private IUIWindow _emptyBackWindow;
    private CursorLockMode _previousCursorLockState;
    private bool _previousCursorVisible;
    private bool _hasCursorState;
    private bool _cursorRequired;
    private bool _gameplayInputBlocked;
    private bool _playerControlsDisabled;

    public ReadOnlyReactiveProperty<bool> HasOpenWindows => _hasOpenWindows;
    public ReadOnlyReactiveProperty<IUIWindow> TopWindow => _topWindow;
    public ReadOnlyReactiveProperty<bool> RequiresCursor => _requiresCursor;
    public ReadOnlyReactiveProperty<bool> BlocksGameplayInput => _blocksGameplayInput;
    public ReadOnlyReactiveProperty<bool> IsGamePaused => _isGamePaused;

    private IPlayerInput PlayerInput
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

    private void Update()
    {
        if (_handleEscapeInput && PlayerInput.IsEscapePressed())
        {
            HandleBack();
        }
    }

    private void OnDestroy()
    {
        RestoreGameplayInput();
        RestoreCursorState();
        _hasOpenWindows.Dispose();
        _topWindow.Dispose();
        _requiresCursor.Dispose();
        _blocksGameplayInput.Dispose();
        _isGamePaused.Dispose();
    }

    public void SetEmptyBackWindow(IUIWindow window)
    {
        _emptyBackWindow = window;
    }

    public bool Open(IUIWindow window)
    {
        if (window == null)
        {
            return false;
        }

        if (window.Type == UIWindowType.Root)
        {
            CloseIncompatibleRootWindows(window);
        }

        if (window.Parent != null && IsOpen(window.Parent) == false && Open(window.Parent) == false)
        {
            return false;
        }

        int existingIndex = _stack.IndexOf(window);

        if (existingIndex >= 0)
        {
            CloseAbove(existingIndex);
            window.BringToFront();
            RefreshState();
            return true;
        }

        window.Open();

        if (window.IsOpen == false)
        {
            RefreshState();
            return false;
        }

        _stack.Add(window);
        window.BringToFront();
        RefreshState();
        return true;
    }

    public bool Close(IUIWindow window)
    {
        if (window == null)
        {
            return false;
        }

        int index = _stack.IndexOf(window);

        if (index < 0)
        {
            if (window.IsOpen == false)
            {
                return false;
            }

            window.Close();
            RefreshState();
            return true;
        }

        while (_stack.Count > index)
        {
            IUIWindow closingWindow = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            closingWindow.Close();
        }

        RefreshState();
        return true;
    }

    public bool Toggle(IUIWindow window)
    {
        if (window == null)
        {
            return false;
        }

        return IsOpen(window) ? Close(window) : Open(window);
    }

    public bool HandleBack()
    {
        if (_stack.Count == 0)
        {
            return _emptyBackWindow != null && Open(_emptyBackWindow);
        }

        IUIWindow topWindow = _stack[_stack.Count - 1];

        if (topWindow.CanCloseWithBack == false)
        {
            return false;
        }

        return Close(topWindow);
    }

    public bool IsOpen(IUIWindow window)
    {
        return window != null && _stack.Contains(window);
    }

    public void Forget(IUIWindow window)
    {
        if (window == null)
        {
            return;
        }

        bool removed = false;

        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            if (_stack[i] != window)
            {
                continue;
            }

            _stack.RemoveAt(i);
            removed = true;
        }

        if (removed)
        {
            RefreshState();
        }
    }

    private void CloseIncompatibleRootWindows(IUIWindow window)
    {
        IUIWindow rootWindow = GetOpenRootWindowExcept(window);

        while (rootWindow != null)
        {
            Close(rootWindow);
            rootWindow = GetOpenRootWindowExcept(window);
        }
    }

    private IUIWindow GetOpenRootWindowExcept(IUIWindow window)
    {
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            IUIWindow openWindow = _stack[i];

            if (openWindow != window && openWindow.Type == UIWindowType.Root)
            {
                return openWindow;
            }
        }

        return null;
    }

    private void CloseAbove(int index)
    {
        while (_stack.Count - 1 > index)
        {
            IUIWindow closingWindow = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            closingWindow.Close();
        }
    }

    private void RefreshState()
    {
        IUIWindow topWindow = _stack.Count == 0 ? null : _stack[_stack.Count - 1];
        bool requiresCursor = false;
        bool blocksGameplayInput = false;
        bool isGamePaused = false;

        for (int i = 0; i < _stack.Count; i++)
        {
            IUIWindow window = _stack[i];
            requiresCursor |= window.RequiresCursor;
            blocksGameplayInput |= window.BlocksGameplayInput;
            isGamePaused |= window.PausesGame;
        }

        _hasOpenWindows.Value = _stack.Count > 0;
        _topWindow.Value = topWindow;
        _requiresCursor.Value = requiresCursor;
        _blocksGameplayInput.Value = blocksGameplayInput;
        _isGamePaused.Value = isGamePaused;
        ApplyCursorState(requiresCursor);
        ApplyGameplayInputState(blocksGameplayInput);
    }

    private void ApplyCursorState(bool cursorRequired)
    {
        if (_manageCursor == false || _cursorRequired == cursorRequired)
        {
            return;
        }

        _cursorRequired = cursorRequired;

        if (cursorRequired)
        {
            CacheCursorState();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        RestoreCursorState();
    }

    private void CacheCursorState()
    {
        if (_hasCursorState)
        {
            return;
        }

        _previousCursorLockState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        _hasCursorState = true;
    }

    private void RestoreCursorState()
    {
        if (_hasCursorState == false)
        {
            return;
        }

        Cursor.lockState = _previousCursorLockState;
        Cursor.visible = _previousCursorVisible;
        _hasCursorState = false;
    }

    private void ApplyGameplayInputState(bool blocked)
    {
        if (_manageGameplayInput == false || _gameplayInputBlocked == blocked)
        {
            return;
        }

        _gameplayInputBlocked = blocked;

        if (blocked)
        {
            DisableGameplayInput();
            return;
        }

        RestoreGameplayInput();
    }

    private void DisableGameplayInput()
    {
        RestoreGameplayInput();

        if (_playerController != null)
        {
            _playerController.SetControlsEnabled(false);
            _playerControlsDisabled = true;
        }

        AddBehaviours(_disableWhileGameplayInputBlocked);

        if (_disableChildrenWhileGameplayInputBlocked == null)
        {
            return;
        }

        for (int i = 0; i < _disableChildrenWhileGameplayInputBlocked.Length; i++)
        {
            GameObject root = _disableChildrenWhileGameplayInputBlocked[i];

            if (root == null)
            {
                continue;
            }

            _childrenBehaviourBuffer.Clear();
            root.GetComponentsInChildren(true, _childrenBehaviourBuffer);
            AddBehaviours(_childrenBehaviourBuffer);
        }

        _childrenBehaviourBuffer.Clear();
    }

    private void AddBehaviours(IReadOnlyList<Behaviour> behaviours)
    {
        if (behaviours == null)
        {
            return;
        }

        for (int i = 0; i < behaviours.Count; i++)
        {
            AddBehaviour(behaviours[i]);
        }
    }

    private void AddBehaviour(Behaviour behaviour)
    {
        if (behaviour == null || behaviour == this || _disabledBehaviours.Contains(behaviour))
        {
            return;
        }

        _disabledBehaviours.Add(behaviour);
        _disabledBehaviourStates.Add(behaviour.enabled);
        behaviour.enabled = false;
    }

    private void RestoreGameplayInput()
    {
        if (_playerController != null && _playerControlsDisabled)
        {
            _playerController.SetControlsEnabled(true);
            _playerControlsDisabled = false;
        }

        for (int i = 0; i < _disabledBehaviours.Count; i++)
        {
            Behaviour behaviour = _disabledBehaviours[i];

            if (behaviour != null)
            {
                behaviour.enabled = _disabledBehaviourStates[i];
            }
        }

        _disabledBehaviours.Clear();
        _disabledBehaviourStates.Clear();
    }
}
