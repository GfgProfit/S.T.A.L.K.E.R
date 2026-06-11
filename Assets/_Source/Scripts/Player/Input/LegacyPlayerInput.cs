using UnityEngine;

public class LegacyPlayerInput : IPlayerInput
{
    public Vector2 GetMouseDelta()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        return new Vector2(mouseX, mouseY);
    }

    public Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return new Vector2(horizontal, vertical);
    }

    public bool IsEscapePressed() => Input.GetKeyDown(KeyCode.Escape);
    public bool IsInventoryPressed() => Input.GetKeyDown(KeyCode.I);

    public bool IsInteractPressed() => Input.GetKeyDown(KeyCode.F);
    public bool IsInteractHold() => Input.GetKey(KeyCode.F);
    public bool IsInteractUp() => Input.GetKeyUp(KeyCode.F);
    public bool IsInteractDenied() => Input.GetKeyDown(KeyCode.Mouse1);

    public bool IsJumpPressed() => Input.GetKeyDown(KeyCode.Space);
    public bool IsSprintHeld() => Input.GetKey(KeyCode.LeftShift);

    public bool IsCrouchingHold() => Input.GetKey(KeyCode.LeftControl);
}
