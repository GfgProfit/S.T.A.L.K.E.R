using R3;

public sealed class WorldItemTooltipViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _isVisible = new();
    private readonly ReactiveProperty<string> _labelText = new(string.Empty);

    public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
    public ReadOnlyReactiveProperty<string> LabelText => _labelText;

    public void Show(string itemName, string interactKey, string interactKeyColor)
    {
        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(interactKey))
        {
            Hide();
            return;
        }

        _labelText.Value = $"[<color={interactKeyColor}>{interactKey}</color>] - {itemName}";
        _isVisible.Value = true;
    }

    public void Hide()
    {
        _isVisible.Value = false;
    }

    protected override void DisposeManaged()
    {
        _isVisible.Dispose();
        _labelText.Dispose();
    }
}
