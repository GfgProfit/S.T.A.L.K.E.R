using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

public sealed class InventoryItemContextMenuViewModel : ViewModelBase
{
    private readonly Action _dropOneAction;
    private readonly Action _dropStackAction;
    private readonly ReactiveProperty<bool> _isVisible = new();
    private readonly ReactiveProperty<bool> _canDropStack = new();

    public InventoryItemContextMenuViewModel(Action dropOneAction, Action dropStackAction)
    {
        _dropOneAction = dropOneAction;
        _dropStackAction = dropStackAction;
        DropOneCommand = new(DropOneAsync);
        DropStackCommand = new(DropStackAsync, () => _canDropStack.Value);
    }

    public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
    public ReadOnlyReactiveProperty<bool> CanDropStack => _canDropStack;
    public AsyncReactiveCommand DropOneCommand { get; }
    public AsyncReactiveCommand DropStackCommand { get; }

    public void Show(bool canDropStack)
    {
        _canDropStack.Value = canDropStack;
        _isVisible.Value = true;
        DropStackCommand.RefreshCanExecute();
    }

    public void Hide()
    {
        _isVisible.Value = false;
    }

    private UniTask DropOneAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dropOneAction?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask DropStackAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dropStackAction?.Invoke();
        return UniTask.CompletedTask;
    }

    protected override void DisposeManaged()
    {
        DropOneCommand.Dispose();
        DropStackCommand.Dispose();
        _isVisible.Dispose();
        _canDropStack.Dispose();
    }
}
