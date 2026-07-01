using UnityEngine;

public interface IPlayerInput : IPlayerLookInput, IPlayerMovementInput, IPlayerInteractionInput, IInventoryInput, IWeaponSlotInput, IWeaponInput, IMiniMapInput
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

public interface IMiniMapInput
{
    bool IsMiniMapZoomPlusPressed();
    bool IsMiniMapZoomMinusPressed();
}

public interface IInventoryInput : IPlayerPointerInput
{
    bool IsEscapePressed();
    bool IsInventoryPressed();
    int GetInventoryQuickUseSlotIndexPressed();
    string GetInventoryQuickUseSlotDisplayName(int slotIndex);
    bool IsInventoryDropPressed();
    bool IsInventoryDropStackModifierHeld();
    bool IsInventoryCountDragModifierHeld();
    bool IsInventoryPrimaryActionPressed();
    bool IsInventoryPrimaryActionReleased();
    bool IsInventorySecondaryActionPressed();
    bool IsInventoryRotatePressed();
}

public interface IWeaponSlotInput
{
    int GetWeaponSlotIndexPressed();
}

public interface IWeaponInput
{
    string WeaponReloadKeyDisplayName { get; }

    bool IsWeaponShootPressed();
    bool IsWeaponShootHeld();
    bool IsWeaponAimHeld();
    bool IsWeaponReloadPressed();
    bool IsWeaponAmmoTypeChangePressed();
    bool IsWeaponHidePressed();
}
