using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-20)]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private InventoryController _inventoryController;
    [SerializeField] private GameObject[] _blockingWindows;
    [SerializeField] private Behaviour[] _disableWhileOpen;
    [SerializeField] private GameObject[] _disableChildrenWhileOpen;
    [SerializeField] private bool _unlockCursorWhileOpen = true;

    [Inject] private IPlayerInput _playerInput = null;

    private readonly List<Behaviour> _disabledBehaviours = new();
    private readonly List<bool> _disabledBehaviourStates = new();
    private readonly List<Behaviour> _childrenBehaviourBuffer = new();
    private IPlayerInput _fallbackPlayerInput;
    private CursorLockMode _previousCursorLockState;
    private bool _previousCursorVisible;
    private bool _hasCursorState;
    private bool _isOpen;

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

    private void Awake()
    {
        if (_pausePanel != null && _pausePanel.activeSelf)
        {
            _isOpen = true;
            ApplyOpenState();
        }
    }

    private void Update()
    {
        SyncExternalPanelState();

        if (PlayerInput.IsEscapePressed() == false)
        {
            return;
        }

        if (_isOpen)
        {
            Close();
            return;
        }

        if (HasBlockingWindowOpen())
        {
            return;
        }

        Open();
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

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(_isOpen == false);

    public void LoadMainMenu()
    {
        SetOpen(false);
        ApplyMainMenuCursorState();
        SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
    }

    private void SetOpen(bool isOpen)
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
            ApplyOpenState();
            return;
        }

        RestoreGameplayBehaviours();
        RestoreCursorState();
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

    private bool HasBlockingWindowOpen()
    {
        if (_inventoryController != null && _inventoryController.IsOpen)
        {
            return true;
        }

        if (_blockingWindows == null)
        {
            return false;
        }

        for (int i = 0; i < _blockingWindows.Length; i++)
        {
            GameObject window = _blockingWindows[i];

            if (window != null && window != _pausePanel && window.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
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
