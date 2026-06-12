using UnityEngine;

public class LegacyPlayerInput : IPlayerInput
{
    private const KeyCode InteractKey = KeyCode.F;
    private const KeyCode SprintKey = KeyCode.LeftShift;
    private const KeyCode CrouchKey = KeyCode.LeftControl;
    private const KeyCode InventoryDropKey = KeyCode.Z;
    private const KeyCode InventoryDropStackModifierKey = SprintKey;
    private const KeyCode InventoryQuickEquipModifierKey = KeyCode.LeftAlt;
    private const KeyCode InventoryQuickMoveModifierKey = CrouchKey;

    public string InteractKeyDisplayName => InteractKey.ToString();

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
    public bool IsInventoryDropPressed() => Input.GetKeyDown(InventoryDropKey);
    public bool IsInventoryDropStackModifierHeld() => Input.GetKey(InventoryDropStackModifierKey);
    public bool IsInventoryQuickEquipModifierHeld() => Input.GetKey(InventoryQuickEquipModifierKey);
    public bool IsInventoryQuickMoveModifierHeld() => Input.GetKey(InventoryQuickMoveModifierKey);

    public bool IsInteractPressed() => Input.GetKeyDown(InteractKey);
    public bool IsInteractHold() => Input.GetKey(InteractKey);
    public bool IsInteractUp() => Input.GetKeyUp(InteractKey);
    public bool IsInteractDenied() => Input.GetKeyDown(KeyCode.Mouse1);

    public bool IsJumpPressed() => Input.GetKeyDown(KeyCode.Space);
    public bool IsSprintHeld() => Input.GetKey(SprintKey);

    public bool IsCrouchingHold() => Input.GetKey(CrouchKey);
}
