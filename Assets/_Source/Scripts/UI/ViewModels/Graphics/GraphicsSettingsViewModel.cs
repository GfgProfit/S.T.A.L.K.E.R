using R3;

public sealed class GraphicsSettingsViewModel : ViewModelBase
{
    private readonly ReactiveProperty<GraphicsData> _currentGraphicsData = new();
    private readonly ReactiveProperty<GraphicsData> _pendingGraphicsData = new();

    public ReadOnlyReactiveProperty<GraphicsData> CurrentGraphicsData => _currentGraphicsData;
    public ReadOnlyReactiveProperty<GraphicsData> PendingGraphicsData => _pendingGraphicsData;

    public GraphicsData Current => _currentGraphicsData.Value;
    public GraphicsData Pending => _pendingGraphicsData.Value;

    public void LoadGraphicsSettings()
    {
        GraphicsData data = GraphicsSettingsStorage.LoadGraphicsSettings();
        GraphicsSettingsApplier.NormalizeUpscalerSettings(data);
        _currentGraphicsData.Value = data.Clone();
        _pendingGraphicsData.Value = data.Clone();
    }

    public void SetPendingGraphicsData(GraphicsData data)
    {
        if (data == null)
        {
            return;
        }

        _pendingGraphicsData.Value = data.Clone();
    }

    public void ApplyPendingGraphicsSettings()
    {
        if (_pendingGraphicsData.Value == null)
        {
            return;
        }

        _currentGraphicsData.Value = _pendingGraphicsData.Value.Clone();
    }

    public void DiscardPendingGraphicsSettings()
    {
        if (_currentGraphicsData.Value == null)
        {
            return;
        }

        _pendingGraphicsData.Value = _currentGraphicsData.Value.Clone();
    }

    protected override void DisposeManaged()
    {
        _currentGraphicsData.Dispose();
        _pendingGraphicsData.Dispose();
    }
}
