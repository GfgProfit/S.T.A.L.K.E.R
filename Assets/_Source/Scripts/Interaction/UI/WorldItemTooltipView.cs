using System;
using R3;
using TMPro;
using UnityEngine;

public class WorldItemTooltipView : MonoBehaviour, IView<WorldItemTooltipViewModel>
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private string _interactKeyColor = "orange";

    private WorldItemTooltipViewModel _viewModel;
    private IDisposable _isVisibleSubscription;
    private IDisposable _labelTextSubscription;

    private void Awake()
    {
        bool hasViewModel = _viewModel != null;
        EnsureViewModel();

        if (hasViewModel == false)
        {
            Hide();
        }
    }

    public void Show(string itemName, string interactKey)
    {
        EnsureViewModel();
        _viewModel.Show(itemName, interactKey, _interactKeyColor);
    }

    public void Hide()
    {
        EnsureViewModel();
        _viewModel.Hide();
    }

    public void Bind(WorldItemTooltipViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _isVisibleSubscription = _viewModel.IsVisible.Subscribe(SetVisible);
        _labelTextSubscription = _viewModel.LabelText.Subscribe(SetLabelText);
    }

    public void Unbind()
    {
        _isVisibleSubscription?.Dispose();
        _labelTextSubscription?.Dispose();
        _isVisibleSubscription = null;
        _labelTextSubscription = null;
    }

    private void OnDestroy()
    {
        Unbind();
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void EnsureViewModel()
    {
        if (_viewModel != null)
        {
            return;
        }

        Bind(InteractionViewModelFactory.CreateWorldItemTooltip());
    }

    private void SetLabelText(string labelText)
    {
        if (_label == null)
        {
            return;
        }

        _label.richText = true;
        _label.text = labelText;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            return;
        }

        gameObject.SetActive(visible);
    }
}
