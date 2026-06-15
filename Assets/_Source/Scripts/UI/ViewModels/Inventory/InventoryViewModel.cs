using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

public sealed class InventoryViewModel : ViewModelBase
{
    private readonly GameProjectSettings _settings;
    private readonly ReactiveProperty<bool> _isOpen = new();
    private readonly ReactiveProperty<float> _currentCarryWeight = new();
    private readonly ReactiveProperty<float> _baseMaxCarryWeight = new();
    private readonly ReactiveProperty<float> _maxCarryWeight = new();
    private readonly ReactiveProperty<float> _movementBlockWeight = new();
    private readonly ReactiveProperty<bool> _isMovementBlockedByWeight = new();
    private readonly ReactiveProperty<string> _weightText = new(string.Empty);

    public InventoryViewModel(System.Func<CancellationToken, UniTask> toggleOpenAsync, GameProjectSettings settings)
    {
        _settings = settings == null ? GameProjectSettings.LoadDefault() : settings;
        ToggleOpenCommand = new(toggleOpenAsync);
    }

    public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
    public ReadOnlyReactiveProperty<float> CurrentCarryWeight => _currentCarryWeight;
    public ReadOnlyReactiveProperty<float> BaseMaxCarryWeight => _baseMaxCarryWeight;
    public ReadOnlyReactiveProperty<float> MaxCarryWeight => _maxCarryWeight;
    public ReadOnlyReactiveProperty<float> MovementBlockWeight => _movementBlockWeight;
    public ReadOnlyReactiveProperty<bool> IsMovementBlockedByWeight => _isMovementBlockedByWeight;
    public ReadOnlyReactiveProperty<string> WeightText => _weightText;
    public AsyncReactiveCommand ToggleOpenCommand { get; }

    public void SetOpenState(bool isOpen)
    {
        _isOpen.Value = isOpen;
    }

    public void SetWeightState(float currentCarryWeight, float baseMaxCarryWeight, float maxCarryWeight, float movementBlockWeight, bool isMovementBlockedByWeight)
    {
        _currentCarryWeight.Value = currentCarryWeight;
        _baseMaxCarryWeight.Value = baseMaxCarryWeight;
        _maxCarryWeight.Value = maxCarryWeight;
        _movementBlockWeight.Value = movementBlockWeight;
        _isMovementBlockedByWeight.Value = isMovementBlockedByWeight;
        _weightText.Value = InventoryWeightTextFormatter.BuildText(currentCarryWeight, maxCarryWeight, movementBlockWeight, _settings);
    }

    protected override void DisposeManaged()
    {
        ToggleOpenCommand.Dispose();
        _isOpen.Dispose();
        _currentCarryWeight.Dispose();
        _baseMaxCarryWeight.Dispose();
        _maxCarryWeight.Dispose();
        _movementBlockWeight.Dispose();
        _isMovementBlockedByWeight.Dispose();
        _weightText.Dispose();
    }
}
