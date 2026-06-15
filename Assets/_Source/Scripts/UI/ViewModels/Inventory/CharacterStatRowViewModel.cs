using R3;

public sealed class CharacterStatRowViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _active = new();
    private readonly ReactiveProperty<string> _text = new(string.Empty);

    public ReadOnlyReactiveProperty<bool> Active => _active;
    public ReadOnlyReactiveProperty<string> Text => _text;

    public void Render(CharacterStatRowState state)
    {
        _text.Value = state.IsVisible ? state.Text : string.Empty;
        _active.Value = state.IsVisible;
    }

    public void Hide()
    {
        _text.Value = string.Empty;
        _active.Value = false;
    }

    protected override void DisposeManaged()
    {
        _active.Dispose();
        _text.Dispose();
    }
}
