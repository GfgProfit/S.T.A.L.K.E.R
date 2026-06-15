using UnityEngine;

public interface IPlayerInput :
    IPlayerLookInput,
    IPlayerMovementInput,
    IPlayerInteractionInput,
    IInventoryInput
{
}

public interface IPlayerLookInput
{
    Vector2 GetMouseDelta();
}

public interface IPlayerMovementInput
{
    Vector2 GetMovementInput();

    bool IsJumpPressed();
    bool IsSprintHeld();
    bool IsCrouchingHold();
}

public interface IPlayerInteractionInput
{
    string InteractKeyDisplayName { get; }

    bool IsInteractPressed();
    bool IsInteractHold();
    bool IsInteractUp();
    bool IsInteractDenied();
}

public interface IPlayerPointerInput
{
    Vector2 GetPointerPosition();
}

public interface IInventoryInput : IPlayerPointerInput
{
    bool IsEscapePressed();
    bool IsInventoryPressed();
    bool IsInventoryDropPressed();
    bool IsInventoryDropStackModifierHeld();
    bool IsInventoryQuickEquipModifierHeld();
    bool IsInventoryQuickMoveModifierHeld();
    bool IsInventoryPrimaryActionPressed();
    bool IsInventoryPrimaryActionReleased();
    bool IsInventorySecondaryActionPressed();
    bool IsInventoryRotatePressed();
}