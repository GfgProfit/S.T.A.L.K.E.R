using UnityEngine;

public interface IPlayerInput
{
    string InteractKeyDisplayName { get; }

    Vector2 GetMovementInput();
    Vector2 GetMouseDelta();

    bool IsJumpPressed();
    bool IsSprintHeld();
    bool IsCrouchingHold();

    bool IsInteractPressed();
    bool IsInteractHold();
    bool IsInteractUp();
    bool IsInteractDenied();

    bool IsEscapePressed();
    bool IsInventoryPressed();
    bool IsInventoryDropPressed();
    bool IsInventoryDropStackModifierHeld();
    bool IsInventoryQuickEquipModifierHeld();
    bool IsInventoryQuickMoveModifierHeld();
}
