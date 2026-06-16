using UnityEngine;

public class LegacyPlayerInput : IPlayerInput
{
    private const KeyCode INTERACT_KEY = KeyCode.F;
    private const KeyCode SPRINT_KEY = KeyCode.LeftShift;
    private const KeyCode CROUCH_KEY = KeyCode.LeftControl;
    private const KeyCode INVENTORY_DROP_KEY = KeyCode.Z;
    private const KeyCode INVENTORY_DROP_STACK_MODIFIER_KEY = KeyCode.LeftShift;
    private const KeyCode INVENTORY_QUICK_EQUIP_MODIFIER_KEY = KeyCode.LeftAlt;
    private const KeyCode INVENTORY_QUICK_MOVE_MODIFIER_KEY = KeyCode.LeftControl;
    private static readonly KeyCode[] INVENTORY_QUICK_USE_KEYS =
    {
        KeyCode.F1,
        KeyCode.F2,
        KeyCode.F3,
        KeyCode.F4,
        KeyCode.F5,
        KeyCode.F6
    };

    public string InteractKeyDisplayName => INTERACT_KEY.ToString();

    public Vector2 GetMouseDelta()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        return new Vector2(mouseX, mouseY);
    }

    public Vector2 GetPointerPosition()
    {
        return Input.mousePosition;
    }

    public Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return new Vector2(horizontal, vertical);
    }

    public bool IsEscapePressed() => Input.GetKeyDown(KeyCode.Escape);
    public bool IsInventoryPressed() => Input.GetKeyDown(KeyCode.I);
    public int GetInventoryQuickUseSlotIndexPressed()
    {
        for (int i = 0; i < INVENTORY_QUICK_USE_KEYS.Length; i++)
        {
            if (Input.GetKeyDown(INVENTORY_QUICK_USE_KEYS[i]))
            {
                return i;
            }
        }

        return -1;
    }

    public string GetInventoryQuickUseSlotDisplayName(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= INVENTORY_QUICK_USE_KEYS.Length)
        {
            return string.Empty;
        }

        return INVENTORY_QUICK_USE_KEYS[slotIndex].ToString();
    }

    public bool IsInventoryDropPressed() => Input.GetKeyDown(INVENTORY_DROP_KEY);
    public bool IsInventoryDropStackModifierHeld() => Input.GetKey(INVENTORY_DROP_STACK_MODIFIER_KEY);
    public bool IsInventoryQuickEquipModifierHeld() => Input.GetKey(INVENTORY_QUICK_EQUIP_MODIFIER_KEY);
    public bool IsInventoryQuickMoveModifierHeld() => Input.GetKey(INVENTORY_QUICK_MOVE_MODIFIER_KEY);
    public bool IsInventoryPrimaryActionPressed() => Input.GetKeyDown(KeyCode.Mouse0);
    public bool IsInventoryPrimaryActionReleased() => Input.GetKeyUp(KeyCode.Mouse0);
    public bool IsInventorySecondaryActionPressed() => Input.GetKeyDown(KeyCode.Mouse1);
    public bool IsInventoryRotatePressed() => Input.GetKeyDown(KeyCode.R);

    public bool IsInteractPressed() => Input.GetKeyDown(INTERACT_KEY);
    public bool IsInteractHold() => Input.GetKey(INTERACT_KEY);
    public bool IsInteractUp() => Input.GetKeyUp(INTERACT_KEY);
    public bool IsInteractDenied() => Input.GetKeyDown(KeyCode.Mouse1);

    public bool IsJumpPressed() => Input.GetKeyDown(KeyCode.Space);
    public bool IsSprintHeld() => Input.GetKey(SPRINT_KEY);

    public bool IsCrouchingHold() => Input.GetKey(CROUCH_KEY);

    public bool IsMiniMapZoomPlusPressed() => Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus);
    public bool IsMiniMapZoomMinusPressed() => Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
}
