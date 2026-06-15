using System;
using R3;
using UnityEngine;

public class InventoryHighlight : MonoBehaviour, IView<InventoryHighlightViewModel>
{
    [SerializeField] private RectTransform _highlighter;

    private InventoryHighlightViewModel _viewModel;
    private IDisposable _visibleSubscription;
    private IDisposable _sizeSubscription;
    private IDisposable _positionSubscription;

    public void Show(bool value)
    {
        EnsureViewModel();
        _viewModel.SetVisible(value);
    }

    public void SetSize(Vector2 size)
    {
        EnsureViewModel();
        _viewModel.SetSize(size);
    }

    public void SetParent(RectTransform parent, int siblingIndex)
    {
        if (parent == null)
        {
            return;
        }

        _highlighter.SetParent(parent, false);
        _highlighter.SetSiblingIndex(Mathf.Min(siblingIndex, _highlighter.parent.childCount - 1));
    }

    public void SetPosition(Vector2 position)
    {
        EnsureViewModel();
        _viewModel.SetPosition(position);
    }

    public void Bind(InventoryHighlightViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _visibleSubscription = _viewModel.Visible.Subscribe(SetVisible);
        _sizeSubscription = _viewModel.Size.Subscribe(ApplySize);
        _positionSubscription = _viewModel.Position.Subscribe(ApplyPosition);
    }

    public void Unbind()
    {
        _visibleSubscription?.Dispose();
        _sizeSubscription?.Dispose();
        _positionSubscription?.Dispose();
        _visibleSubscription = null;
        _sizeSubscription = null;
        _positionSubscription = null;
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

        Bind(InventoryViewModelFactory.CreateHighlight());
    }

    private void SetVisible(bool visible)
    {
        _highlighter.gameObject.SetActive(visible);
    }

    private void ApplySize(Vector2 size)
    {
        _highlighter.sizeDelta = size;
    }

    private void ApplyPosition(Vector2 position)
    {
        _highlighter.localPosition = position;
    }
}
