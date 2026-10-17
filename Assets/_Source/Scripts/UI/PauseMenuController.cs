using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-20)]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _pauseMenuPanel;
    [SerializeField] private GameObject _optionsMenuPanel;
    [SerializeField] private GameObject _exitModalWindow;
    [SerializeField] private UIWindowManager _windowManager;
    [SerializeField] private Behaviour[] _disableWhileOpen;
    [SerializeField] private GameObject[] _disableChildrenWhileOpen;
    [SerializeField] private bool _unlockCursorWhileOpen = true;

    private readonly List<Behaviour> _disabledBehaviours = new();
    private readonly List<bool> _disabledBehaviourStates = new();
    private readonly List<Behaviour> _childrenBehaviourBuffer = new();
    private CursorLockMode _previousCursorLockState;
    private bool _previousCursorVisible;
    private bool _hasCursorState;
    private bool _isOpen;
    private IUIWindow _pauseWindow;
    private IUIWindow _optionsWindow;
    private IUIWindow _exitModalWindowHandle;

    private void Awake()
    {
        CreateWindows();

        if (_windowManager != null)
        {
            _windowManager.SetEmptyBackWindow(_pauseWindow);

            if (_pausePanel != null && _pausePanel.activeSelf)
            {
                _windowManager.Open(_pauseWindow);
            }

            return;
        }

        if (_pausePanel != null && _pausePanel.activeSelf)
        {
            _isOpen = true;
            ApplyOpenState();
        }
    }

    private void Update()
    {
        SyncExternalPanelState();
    }

    private void OnDisable()
    {
        if (_isOpen)
        {
            SetOpen(false);
        }
    }

    private void OnDestroy()
    {
        RestoreGameplayBehaviours();
        RestoreCursorState();
    }

    private void CreateWindows()
    {
        _pauseWindow = new DelegateUIWindow("Pause Panel", UIWindowType.Root, () => _isOpen, () => SetOpenDirect(true), () => SetOpenDirect(false), BringPausePanelToFront, null, true, true, true);
        _optionsWindow = new DelegateUIWindow("Options Menu Panel", UIWindowType.SubWindow, () => _optionsMenuPanel != null && _optionsMenuPanel.activeSelf, OpenOptionsMenuDirect, CloseOptionsMenuDirect, BringOptionsMenuPanelToFront, _pauseWindow, true, true, true);
        _exitModalWindowHandle = new DelegateUIWindow("Exit Modal Window", UIWindowType.Modal, () => _exitModalWindow != null && _exitModalWindow.activeSelf, OpenExitModalWindowDirect, CloseExitModalWindowDirect, BringExitModalWindowToFront, _pauseWindow, true, true, true);
    }

    private void OpenOptionsMenuDirect()
    {
        SetExitModalWindowVisible(false);
        SetPauseMenuPanelVisible(false);
        SetOptionsMenuPanelVisible(true);
    }

    private void CloseOptionsMenuDirect()
    {
        SetOptionsMenuPanelVisible(false);
        SetPauseMenuPanelVisible(true);
    }

    private void OpenExitModalWindowDirect()
    {
        SetPauseMenuPanelVisible(true);
        SetExitModalWindowVisible(true);
    }

    private void CloseExitModalWindowDirect()
    {
        SetExitModalWindowVisible(false);
        SetPauseMenuPanelVisible(true);
    }

    private void SetPauseMenuPanelVisible(bool visible)
    {
        if (_pauseMenuPanel != null && _pauseMenuPanel.activeSelf != visible)
        {
            _pauseMenuPanel.SetActive(visible);
        }
    }

    private void SetOptionsMenuPanelVisible(bool visible)
    {
        if (_optionsMenuPanel != null && _optionsMenuPanel.activeSelf != visible)
        {
            _optionsMenuPanel.SetActive(visible);
        }
    }

    private void SetExitModalWindowVisible(bool visible)
    {
        if (_exitModalWindow != null && _exitModalWindow.activeSelf != visible)
        {
            _exitModalWindow.SetActive(visible);
        }
    }

    private void BringPausePanelToFront() => _pausePanel?.transform.SetAsLastSibling();
    private void BringOptionsMenuPanelToFront() => _optionsMenuPanel?.transform.SetAsLastSibling();
    private void BringExitModalWindowToFront() => _exitModalWindow?.transform.SetAsLastSibling();

    public void Open()
    {
        if (_windowManager != null && _pauseWindow != null)
        {
            _windowManager.Open(_pauseWindow);
            return;
        }

        SetOpen(true);
    }

    public void Close()
    {
        if (_windowManager != null && _pauseWindow != null)
        {
            _windowManager.Close(_pauseWindow);
            return;
        }

        SetOpen(false);
    }

    public void Toggle()
    {
        if (_windowManager != null && _pauseWindow != null)
        {
            _windowManager.Toggle(_pauseWindow);
            return;
        }

        SetOpen(_isOpen == false);
    }

    public void OpenOptionsMenu()
    {
        if (_windowManager != null && _optionsWindow != null)
        {
            _windowManager.Open(_optionsWindow);
            return;
        }

        SetPauseMenuPanelVisible(false);
        SetOptionsMenuPanelVisible(true);
    }

    public void CloseOptionsMenu()
    {
        if (_windowManager != null && _optionsWindow != null)
        {
            _windowManager.Close(_optionsWindow);
            return;
        }

        SetOptionsMenuPanelVisible(false);
        SetPauseMenuPanelVisible(true);
    }

    public void OpenExitModalWindow()
    {
        if (_windowManager != null && _exitModalWindowHandle != null)
        {
            _windowManager.Open(_exitModalWindowHandle);
            return;
        }

        SetExitModalWindowVisible(true);
    }

    public void CloseExitModalWindow()
    {
        if (_windowManager != null && _exitModalWindowHandle != null)
        {
            _windowManager.Close(_exitModalWindowHandle);
            return;
        }

        SetExitModalWindowVisible(false);
    }

    public void LoadMainMenu()
    {
        Close();
        ApplyMainMenuCursorState();
        SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
    }

    private void SetOpen(bool isOpen)
    {
        if (_windowManager != null && _pauseWindow != null)
        {
            if (isOpen)
            {
                _windowManager.Open(_pauseWindow);
            }
            else
            {
                _windowManager.Close(_pauseWindow);
            }

            return;
        }

        SetOpenDirect(isOpen);
    }

    private void SetOpenDirect(bool isOpen)
    {
        if (_pausePanel == null || _isOpen == isOpen)
        {
            return;
        }

        _isOpen = isOpen;

        if (_pausePanel.activeSelf != isOpen)
        {
            _pausePanel.SetActive(isOpen);
        }

        if (isOpen)
        {
            SetPauseMenuPanelVisible(true);
            SetOptionsMenuPanelVisible(false);
            SetExitModalWindowVisible(false);

            if (_windowManager == null)
            {
                ApplyOpenState();
            }

            return;
        }

        SetOptionsMenuPanelVisible(false);
        SetExitModalWindowVisible(false);

        if (_windowManager == null)
        {
            RestoreGameplayBehaviours();
            RestoreCursorState();
        }
    }

    private void ApplyOpenState()
    {
        _pausePanel.transform.SetAsLastSibling();
        CacheCursorState();
        DisableGameplayBehaviours();
        ApplyCursorState();
    }

    private void SyncExternalPanelState()
    {
        if (_pausePanel == null || _pausePanel.activeSelf == _isOpen)
        {
            return;
        }

        SetOpen(_pausePanel.activeSelf);
    }

    private void DisableGameplayBehaviours()
    {
        RestoreGameplayBehaviours();
        AddBehaviours(_disableWhileOpen);

        if (_disableChildrenWhileOpen == null)
        {
            return;
        }

        for (int i = 0; i < _disableChildrenWhileOpen.Length; i++)
        {
            GameObject root = _disableChildrenWhileOpen[i];

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
        if (behaviour == null || behaviour == this || IsPausePanelBehaviour(behaviour) || _disabledBehaviours.Contains(behaviour))
        {
            return;
        }

        _disabledBehaviours.Add(behaviour);
        _disabledBehaviourStates.Add(behaviour.enabled);
        behaviour.enabled = false;
    }

    private bool IsPausePanelBehaviour(Behaviour behaviour)
    {
        return _pausePanel != null && behaviour.transform.IsChildOf(_pausePanel.transform);
    }

    private void RestoreGameplayBehaviours()
    {
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

    private void CacheCursorState()
    {
        if (_unlockCursorWhileOpen == false)
        {
            return;
        }

        _previousCursorLockState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        _hasCursorState = true;
    }

    private void ApplyCursorState()
    {
        if (_unlockCursorWhileOpen == false)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ApplyMainMenuCursorState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
}
