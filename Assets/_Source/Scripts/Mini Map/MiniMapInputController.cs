using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

internal sealed class MiniMapInputController
{
    private readonly IMiniMapInput _input;
    private readonly MiniMapViewModel _viewModel;
    private readonly CancellationToken _cancellationToken;

    public MiniMapInputController(IMiniMapInput input, MiniMapViewModel viewModel, CancellationToken cancellationToken)
    {
        _input = input;
        _viewModel = viewModel;
        _cancellationToken = cancellationToken;
    }

    public void Tick()
    {
        if (_input == null || _viewModel == null)
        {
            return;
        }

        if (_input.IsMiniMapZoomMinusPressed())
        {
            _viewModel.ZoomOutCommand.ExecuteAsync(_cancellationToken).Forget(Debug.LogException);
        }
        else if (_input.IsMiniMapZoomPlusPressed())
        {
            _viewModel.ZoomInCommand.ExecuteAsync(_cancellationToken).Forget(Debug.LogException);
        }
    }
}