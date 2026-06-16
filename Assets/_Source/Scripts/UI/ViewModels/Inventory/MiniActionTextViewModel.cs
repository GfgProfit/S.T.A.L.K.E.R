using R3;

public sealed class MiniActionTextViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _visible = new();
    private readonly ReactiveProperty<string> _text = new(string.Empty);

    public ReadOnlyReactiveProperty<bool> Visible => _visible;
    public ReadOnlyReactiveProperty<string> Text => _text;

    public void Show(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Hide();
            return;
        }

        _text.Value = text;
        _visible.Value = true;
    }

    public void Hide()
    {
        _visible.Value = false;
    }

    protected override void DisposeManaged()
    {
        _visible.Dispose();
        _text.Dispose();
    }
}
