using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

public sealed class InventoryItemContextMenuViewModel : ViewModelBase
{
    private readonly Action _useAction;
    private readonly Action _unloadAction;
    private readonly Action _equipPrimaryWeaponAction;
    private readonly Action _equipSecondaryWeaponAction;
    private readonly Action _equipAction;
    private readonly Action _unequipAction;
    private readonly Action _dropOneAction;
    private readonly Action _dropStackAction;
    private readonly ReactiveProperty<bool> _isVisible = new();
    private readonly ReactiveProperty<bool> _canUse = new();
    private readonly ReactiveProperty<bool> _showUnload = new();
    private readonly ReactiveProperty<bool> _canUnload = new();
    private readonly ReactiveProperty<bool> _canEquipPrimaryWeapon = new();
    private readonly ReactiveProperty<bool> _canEquipSecondaryWeapon = new();
    private readonly ReactiveProperty<bool> _canEquip = new();
    private readonly ReactiveProperty<bool> _canUnequip = new();
    private readonly ReactiveProperty<bool> _canDropStack = new();

    public InventoryItemContextMenuViewModel(Action useAction, Action unloadAction, Action equipPrimaryWeaponAction, Action equipSecondaryWeaponAction, Action equipAction, Action unequipAction, Action dropOneAction, Action dropStackAction)
    {
        _useAction = useAction;
        _unloadAction = unloadAction;
        _equipPrimaryWeaponAction = equipPrimaryWeaponAction;
        _equipSecondaryWeaponAction = equipSecondaryWeaponAction;
        _equipAction = equipAction;
        _unequipAction = unequipAction;
        _dropOneAction = dropOneAction;
        _dropStackAction = dropStackAction;
        UseCommand = new(UseAsync, () => _canUse.Value);
        UnloadCommand = new(UnloadAsync, () => _canUnload.Value);
        EquipPrimaryWeaponCommand = new(EquipPrimaryWeaponAsync, () => _canEquipPrimaryWeapon.Value);
        EquipSecondaryWeaponCommand = new(EquipSecondaryWeaponAsync, () => _canEquipSecondaryWeapon.Value);
        EquipCommand = new(EquipAsync, () => _canEquip.Value);
        UnequipCommand = new(UnequipAsync, () => _canUnequip.Value);
        DropOneCommand = new(DropOneAsync);
        DropStackCommand = new(DropStackAsync, () => _canDropStack.Value);
    }

    public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
    public ReadOnlyReactiveProperty<bool> CanUse => _canUse;
    public ReadOnlyReactiveProperty<bool> ShowUnload => _showUnload;
    public ReadOnlyReactiveProperty<bool> CanUnload => _canUnload;
    public ReadOnlyReactiveProperty<bool> CanEquipPrimaryWeapon => _canEquipPrimaryWeapon;
    public ReadOnlyReactiveProperty<bool> CanEquipSecondaryWeapon => _canEquipSecondaryWeapon;
    public ReadOnlyReactiveProperty<bool> CanEquip => _canEquip;
    public ReadOnlyReactiveProperty<bool> CanUnequip => _canUnequip;
    public ReadOnlyReactiveProperty<bool> CanDropStack => _canDropStack;
    public AsyncReactiveCommand UseCommand { get; }
    public AsyncReactiveCommand UnloadCommand { get; }
    public AsyncReactiveCommand EquipPrimaryWeaponCommand { get; }
    public AsyncReactiveCommand EquipSecondaryWeaponCommand { get; }
    public AsyncReactiveCommand EquipCommand { get; }
    public AsyncReactiveCommand UnequipCommand { get; }
    public AsyncReactiveCommand DropOneCommand { get; }
    public AsyncReactiveCommand DropStackCommand { get; }

    public void Show(bool canUse, bool showUnload, bool canUnload, bool canEquipPrimaryWeapon, bool canEquipSecondaryWeapon, bool canEquip, bool canUnequip, bool canDropStack)
    {
        _canUse.Value = canUse;
        _showUnload.Value = showUnload;
        _canUnload.Value = canUnload;
        _canEquipPrimaryWeapon.Value = canEquipPrimaryWeapon;
        _canEquipSecondaryWeapon.Value = canEquipSecondaryWeapon;
        _canEquip.Value = canEquip;
        _canUnequip.Value = canUnequip;
        _canDropStack.Value = canDropStack;
        _isVisible.Value = true;
        UseCommand.RefreshCanExecute();
        UnloadCommand.RefreshCanExecute();
        EquipPrimaryWeaponCommand.RefreshCanExecute();
        EquipSecondaryWeaponCommand.RefreshCanExecute();
        EquipCommand.RefreshCanExecute();
        UnequipCommand.RefreshCanExecute();
        DropStackCommand.RefreshCanExecute();
    }

    public void Hide()
    {
        _isVisible.Value = false;
    }

    private UniTask UseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _useAction?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask UnloadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _unloadAction?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask EquipPrimaryWeaponAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _equipPrimaryWeaponAction?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask EquipSecondaryWeaponAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _equipSecondaryWeaponAction?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask EquipAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _equipAction?.Invoke();
        return UniTask.CompletedTask;
    }

    private UniTask UnequipAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _unequipAction?.Invoke();
        return UniTask.CompletedTask;
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
        UseCommand.Dispose();
        UnloadCommand.Dispose();
        EquipPrimaryWeaponCommand.Dispose();
        EquipSecondaryWeaponCommand.Dispose();
        EquipCommand.Dispose();
        UnequipCommand.Dispose();
        DropOneCommand.Dispose();
        DropStackCommand.Dispose();
        _isVisible.Dispose();
        _canUse.Dispose();
        _showUnload.Dispose();
        _canUnload.Dispose();
        _canEquipPrimaryWeapon.Dispose();
        _canEquipSecondaryWeapon.Dispose();
        _canEquip.Dispose();
        _canUnequip.Dispose();
        _canDropStack.Dispose();
    }
}
