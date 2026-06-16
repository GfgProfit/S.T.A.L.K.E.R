using R3;

public sealed class QuickUseInventorySlotViewModel : ViewModelBase
{
    private readonly ReactiveProperty<string> _keyText = new(string.Empty);

    public ReadOnlyReactiveProperty<string> KeyText => _keyText;

    public void SetKeyText(string keyText)
    {
        _keyText.Value = keyText ?? string.Empty;
    }

    protected override void DisposeManaged()
    {
        _keyText.Dispose();
    }
}
