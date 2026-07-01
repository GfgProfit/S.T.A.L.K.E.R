using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class InventoryHighlight : MonoBehaviour, IView<InventoryHighlightViewModel>
{
    [SerializeField] private RectTransform _highlighter;
    [SerializeField] private Graphic _highlighterGraphic;

    private InventoryHighlightViewModel _viewModel;
    private IDisposable _visibleSubscription;
    private IDisposable _sizeSubscription;
    private IDisposable _positionSubscription;
    private IDisposable _colorSubscription;
    private Color _defaultColor;
    private bool _hasDefaultColor;

    private void Awake()
    {
        EnsureHighlighterGraphic();
        CaptureDefaultColor();
    }

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

    public void SetColor(Color color)
    {
        EnsureViewModel();
        _viewModel.SetColor(color);
    }

    public void SetDefaultColor()
    {
        EnsureViewModel();
        _viewModel.SetColor(GetDefaultColor());
    }

    public void Bind(InventoryHighlightViewModel viewModel)
    {
        Unbind();
        CaptureDefaultColor();
        _viewModel = viewModel;

        if (_viewModel == null)
        {
            return;
        }

        _viewModel.SetColor(GetDefaultColor());
        _visibleSubscription = _viewModel.Visible.Subscribe(SetVisible);
        _sizeSubscription = _viewModel.Size.Subscribe(ApplySize);
        _positionSubscription = _viewModel.Position.Subscribe(ApplyPosition);
        _colorSubscription = _viewModel.Color.Subscribe(ApplyColor);
    }

    public void Unbind()
    {
        _visibleSubscription?.Dispose();
        _sizeSubscription?.Dispose();
        _positionSubscription?.Dispose();
        _colorSubscription?.Dispose();
        _visibleSubscription = null;
        _sizeSubscription = null;
        _positionSubscription = null;
        _colorSubscription = null;
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

    private void EnsureHighlighterGraphic()
    {
        if (_highlighterGraphic == null && _highlighter != null)
        {
            _highlighterGraphic = _highlighter.GetComponent<Graphic>();
        }
    }

    private void CaptureDefaultColor()
    {
        if (_hasDefaultColor)
        {
            return;
        }

        EnsureHighlighterGraphic();
        _defaultColor = _highlighterGraphic == null ? Color.white : _highlighterGraphic.color;
        _hasDefaultColor = true;
    }

    private Color GetDefaultColor()
    {
        CaptureDefaultColor();
        return _defaultColor;
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

    private void ApplyColor(Color color)
    {
        EnsureHighlighterGraphic();

        if (_highlighterGraphic != null)
        {
            _highlighterGraphic.color = color;
        }
    }
}
