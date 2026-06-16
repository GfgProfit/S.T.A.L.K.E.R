using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

internal sealed class MiniMapViewModel : ViewModelBase
{
    private readonly ReactiveProperty<float> _targetZoom = new();
    private readonly ReactiveProperty<bool> _canZoomIn = new();
    private readonly ReactiveProperty<bool> _canZoomOut = new();
    private readonly MiniMapZoomSettings _zoomSettings;
    private int _zoomLevel;

    public MiniMapViewModel(float initialZoom, int step, Vector2Int minMaxZoom)
    {
        _zoomSettings = new MiniMapZoomSettings(step, minMaxZoom);
        ZoomInCommand = new(ZoomInAsync, () => _canZoomIn.Value);
        ZoomOutCommand = new(ZoomOutAsync, () => _canZoomOut.Value);
        SetZoomLevel(_zoomSettings.GetNearestLevel(initialZoom));
    }

    public ReadOnlyReactiveProperty<float> TargetZoom => _targetZoom;
    public ReadOnlyReactiveProperty<bool> CanZoomIn => _canZoomIn;
    public ReadOnlyReactiveProperty<bool> CanZoomOut => _canZoomOut;
    public AsyncReactiveCommand ZoomInCommand { get; }
    public AsyncReactiveCommand ZoomOutCommand { get; }

    private UniTask ZoomInAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SetZoomLevel(_zoomLevel - 1);
        return UniTask.CompletedTask;
    }

    private UniTask ZoomOutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SetZoomLevel(_zoomLevel + 1);
        return UniTask.CompletedTask;
    }

    private void SetZoomLevel(int zoomLevel)
    {
        _zoomLevel = _zoomSettings.ClampLevel(zoomLevel);
        _targetZoom.Value = _zoomSettings.GetZoom(_zoomLevel);
        RefreshZoomAvailability();
    }

    private void RefreshZoomAvailability()
    {
        _canZoomIn.Value = _zoomLevel > 0;
        _canZoomOut.Value = _zoomLevel < _zoomSettings.MaxLevel;
        ZoomInCommand.RefreshCanExecute();
        ZoomOutCommand.RefreshCanExecute();
    }

    protected override void DisposeManaged()
    {
        ZoomInCommand.Dispose();
        ZoomOutCommand.Dispose();
        _targetZoom.Dispose();
        _canZoomIn.Dispose();
        _canZoomOut.Dispose();
    }
}