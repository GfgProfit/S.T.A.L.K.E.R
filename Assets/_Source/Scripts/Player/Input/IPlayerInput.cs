using UnityEngine;

public interface IPlayerInput
{
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
}