using System;
using R3;
using TMPro;

internal sealed class MiniActionTextView : IView<MiniActionTextViewModel>, IDisposable
{
    private readonly TMP_Text _text;
    private IDisposable _visibleSubscription;
    private IDisposable _textSubscription;

    public MiniActionTextView(TMP_Text text)
    {
        _text = text;
    }

    public void Bind(MiniActionTextViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _visibleSubscription = viewModel.Visible.Subscribe(SetVisible);
        _textSubscription = viewModel.Text.Subscribe(SetText);
    }

    public void Unbind()
    {
        _visibleSubscription?.Dispose();
        _textSubscription?.Dispose();
        _visibleSubscription = null;
        _textSubscription = null;
    }

    public void Dispose()
    {
        Unbind();
    }

    private void SetVisible(bool visible)
    {
        if (_text != null)
        {
            _text.gameObject.SetActive(visible);
        }
    }

    private void SetText(string text)
    {
        if (_text == null)
        {
            return;
        }

        _text.text = text;
    }
}
