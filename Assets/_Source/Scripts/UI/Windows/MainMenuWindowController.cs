using UnityEngine;

[DefaultExecutionOrder(-10)]
public sealed class MainMenuWindowController : MonoBehaviour
{
    [SerializeField] private UIWindowManager _windowManager;
    [SerializeField] private GameObject _background;
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private bool _openMainMenuOnAwake = true;

    private IUIWindow _mainMenuWindow;
    private IUIWindow _optionsWindow;

    private void Awake()
    {
        CreateWindows();

        if (_openMainMenuOnAwake)
        {
            OpenMainMenu();
        }
    }

    public void OpenMainMenu()
    {
        if (_windowManager != null && _mainMenuWindow != null)
        {
            _windowManager.Open(_mainMenuWindow);
            return;
        }

        OpenMainMenuDirect();
    }

    public void OpenOptionsPanel()
    {
        if (_windowManager != null && _optionsWindow != null)
        {
            _windowManager.Open(_optionsWindow);
            return;
        }

        OpenOptionsPanelDirect();
    }

    public void CloseOptionsPanel()
    {
        if (_windowManager != null && _optionsWindow != null)
        {
            _windowManager.Close(_optionsWindow);
            return;
        }

        CloseOptionsPanelDirect();
    }

    private void CreateWindows()
    {
        _mainMenuWindow = new DelegateUIWindow("Main Menu Panel", UIWindowType.Root, IsMainMenuRootOpen, OpenMainMenuDirect, CloseMainMenuDirect, BringBackgroundToFront, null, true, false, false, false);
        _optionsWindow = new DelegateUIWindow("Options Panel", UIWindowType.SubWindow, () => _optionsPanel != null && _optionsPanel.activeSelf, OpenOptionsPanelDirect, CloseOptionsPanelDirect, BringOptionsPanelToFront, _mainMenuWindow, true, false);
    }

    private bool IsMainMenuRootOpen()
    {
        if (_background != null)
        {
            return _background.activeSelf;
        }

        return _mainMenuPanel != null && _mainMenuPanel.activeSelf;
    }

    private void OpenMainMenuDirect()
    {
        SetBackgroundVisible(true);
        SetMainMenuPanelVisible(true);
        SetOptionsPanelVisible(false);
    }

    private void CloseMainMenuDirect()
    {
        SetOptionsPanelVisible(false);
        SetMainMenuPanelVisible(false);
        SetBackgroundVisible(false);
    }

    private void OpenOptionsPanelDirect()
    {
        SetBackgroundVisible(true);
        SetMainMenuPanelVisible(false);
        SetOptionsPanelVisible(true);
    }

    private void CloseOptionsPanelDirect()
    {
        SetOptionsPanelVisible(false);
        SetMainMenuPanelVisible(true);
    }

    private void SetBackgroundVisible(bool visible)
    {
        if (_background != null && _background.activeSelf != visible)
        {
            _background.SetActive(visible);
        }
    }

    private void SetMainMenuPanelVisible(bool visible)
    {
        if (_mainMenuPanel != null && _mainMenuPanel.activeSelf != visible)
        {
            _mainMenuPanel.SetActive(visible);
        }
    }

    private void SetOptionsPanelVisible(bool visible)
    {
        if (_optionsPanel != null && _optionsPanel.activeSelf != visible)
        {
            _optionsPanel.SetActive(visible);
        }
    }

    private void BringBackgroundToFront() => _background?.transform.SetAsLastSibling();
    private void BringOptionsPanelToFront() => _optionsPanel?.transform.SetAsLastSibling();
}
