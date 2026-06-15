using System;
using UnityEngine;

internal sealed class InventoryOpenStateController
{
    private readonly PlayerController _playerController;
    private readonly bool _unlockCursorWhileOpen;
    private readonly bool _disablePlayerControlsWhileOpen;
    private readonly Func<bool> _tryStashSelectedItem;
    private readonly Action _applyWeightMovementState;
    private readonly Action _handleClosed;

    public InventoryOpenStateController(PlayerController playerController, bool unlockCursorWhileOpen, bool disablePlayerControlsWhileOpen, Func<bool> tryStashSelectedItem, Action applyWeightMovementState, Action handleClosed)
    {
        _playerController = playerController;
        _unlockCursorWhileOpen = unlockCursorWhileOpen;
        _disablePlayerControlsWhileOpen = disablePlayerControlsWhileOpen;
        _tryStashSelectedItem = tryStashSelectedItem;
        _applyWeightMovementState = applyWeightMovementState;
        _handleClosed = handleClosed;
    }

    public bool IsOpen { get; private set; }

    public void Toggle()
    {
        SetOpen(IsOpen == false, false);
    }

    public void SetOpen(bool isOpen, bool force)
    {
        if (force == false && IsOpen == isOpen)
        {
            return;
        }

        if (isOpen == false && _tryStashSelectedItem() == false)
        {
            return;
        }

        IsOpen = isOpen;

        ApplyCursorState();
        ApplyPlayerControlsState();

        if (IsOpen == false)
        {
            _handleClosed();
        }
    }

    private void ApplyCursorState()
    {
        if (_unlockCursorWhileOpen == false)
        {
            return;
        }

        Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsOpen;
    }

    private void ApplyPlayerControlsState()
    {
        if (_disablePlayerControlsWhileOpen == false || _playerController == null)
        {
            return;
        }

        _playerController.SetControlsEnabled(IsOpen == false);
        _applyWeightMovementState();
    }
}
