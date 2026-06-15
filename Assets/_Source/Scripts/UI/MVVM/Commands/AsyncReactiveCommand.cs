using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

public sealed class AsyncReactiveCommand : IDisposable
{
    private readonly Func<CancellationToken, UniTask> _executeAsync;
    private readonly Func<bool> _canExecute;
    private readonly bool _allowConcurrentExecution;
    private readonly ReactiveProperty<bool> _isRunning = new(false);
    private readonly ReactiveProperty<bool> _canExecuteState = new(true);
    private bool _isDisposed;

    public AsyncReactiveCommand(Func<CancellationToken, UniTask> executeAsync, Func<bool> canExecute = null, bool allowConcurrentExecution = false)
    {
        _executeAsync = executeAsync ?? throw new(nameof(executeAsync));
        _canExecute = canExecute;
        _allowConcurrentExecution = allowConcurrentExecution;
        RefreshCanExecute();
    }

    public ReadOnlyReactiveProperty<bool> IsRunning => _isRunning;
    public ReadOnlyReactiveProperty<bool> CanExecuteState => _canExecuteState;
    public bool CanExecute => _canExecuteState.Value;

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        if (CanExecute == false || _isDisposed)
        {
            return;
        }

        if (_allowConcurrentExecution == false && _isRunning.Value)
        {
            return;
        }

        _isRunning.Value = true;
        RefreshCanExecute();

        try
        {
            await _executeAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            _isRunning.Value = false;
            RefreshCanExecute();
        }
    }

    public void RefreshCanExecute()
    {
        bool canExecute = _isDisposed == false && (_canExecute == null || _canExecute());

        if (_allowConcurrentExecution == false && _isRunning.Value)
        {
            canExecute = false;
        }

        _canExecuteState.Value = canExecute;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isRunning.Dispose();
        _canExecuteState.Dispose();
    }
}
