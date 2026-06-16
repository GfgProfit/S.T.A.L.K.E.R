using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;

internal sealed class MiniMapCameraView : IView<MiniMapViewModel>, IDisposable
{
    private readonly Camera _camera;
    private readonly float _zoomDuration;
    private readonly Ease _zoomEase;
    private readonly CancellationToken _lifetimeCancellationToken;
    private IDisposable _targetZoomSubscription;
    private CancellationTokenSource _animationCancellation;
    private Tween _zoomTween;

    public MiniMapCameraView(Camera camera, float zoomDuration, Ease zoomEase, CancellationToken lifetimeCancellationToken)
    {
        _camera = camera;
        _zoomDuration = Mathf.Max(0f, zoomDuration);
        _zoomEase = zoomEase;
        _lifetimeCancellationToken = lifetimeCancellationToken;
    }

    public void Bind(MiniMapViewModel viewModel)
    {
        Unbind();

        if (viewModel == null)
        {
            return;
        }

        _targetZoomSubscription = viewModel.TargetZoom.Subscribe(SetTargetZoom);
    }

    public void Unbind()
    {
        _targetZoomSubscription?.Dispose();
        _targetZoomSubscription = null;
        CancelActiveAnimation();
    }

    public void Dispose() => Unbind();

    private void SetTargetZoom(float targetZoom)
    {
        SetTargetZoomAsync(targetZoom).Forget(Debug.LogException);
    }

    private async UniTask SetTargetZoomAsync(float targetZoom)
    {
        CancelActiveAnimation();

        if (_camera == null)
        {
            return;
        }

        if (_zoomDuration <= 0f || Mathf.Approximately(_camera.orthographicSize, targetZoom))
        {
            _camera.orthographicSize = targetZoom;
            return;
        }

        CancellationTokenSource animationCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellationToken);
        Tween zoomTween = _camera.DOOrthoSize(targetZoom, _zoomDuration).SetEase(_zoomEase);
        _animationCancellation = animationCancellation;
        _zoomTween = zoomTween;

        bool isCanceled = await AwaitTweenAsync(zoomTween, animationCancellation.Token).SuppressCancellationThrow();

        if (ReferenceEquals(_zoomTween, zoomTween))
        {
            _zoomTween = null;
            _animationCancellation = null;
        }

        animationCancellation.Dispose();

        if (isCanceled || _camera == null)
        {
            return;
        }

        _camera.orthographicSize = targetZoom;
    }

    private void CancelActiveAnimation()
    {
        _animationCancellation?.Cancel();
        _animationCancellation = null;

        if (_zoomTween != null && _zoomTween.IsActive())
        {
            _zoomTween.Kill(false);
        }

        _zoomTween = null;
    }

    private static async UniTask AwaitTweenAsync(Tween tween, CancellationToken cancellationToken)
    {
        if (tween == null || tween.IsActive() == false)
        {
            return;
        }

        UniTaskCompletionSource completionSource = new();
        tween.OnComplete(() => completionSource.TrySetResult());
        tween.OnKill(() => completionSource.TrySetResult());

        using (cancellationToken.Register(() =>
        {
            completionSource.TrySetCanceled(cancellationToken);

            if (tween.IsActive())
            {
                tween.Kill(false);
            }
        }))
        {
            await completionSource.Task;
        }
    }
}