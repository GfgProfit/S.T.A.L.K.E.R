using UnityEngine;

internal static class FirstPersonLegsAnimationStateResolver
{
    public static FirstPersonLegsAnimationKey ResolveGrounded(Vector2 movementInput, bool isSprinting, bool isCrouching, float yawInput, float movementDeadZone, float turnDeadZone)
    {
        movementDeadZone = Mathf.Max(0f, movementDeadZone);

        bool forward = movementInput.y > movementDeadZone;
        bool back = movementInput.y < -movementDeadZone;
        bool left = movementInput.x < -movementDeadZone;
        bool right = movementInput.x > movementDeadZone;
        bool isMoving = forward || back || left || right;

        if (isMoving == false)
        {
            return ResolveIdle(isCrouching, yawInput, turnDeadZone);
        }

        if (isCrouching)
        {
            return ResolveCrouchMove(forward, back, left, right);
        }

        if (isSprinting && forward)
        {
            return ResolveSprintMove(left, right);
        }

        return ResolveWalkMove(forward, back, left, right);
    }

    private static FirstPersonLegsAnimationKey ResolveIdle(bool isCrouching, float yawInput, float turnDeadZone)
    {
        if (isCrouching)
        {
            return FirstPersonLegsAnimationKey.CrouchIdle;
        }

        turnDeadZone = Mathf.Max(0f, turnDeadZone);

        if (yawInput < -turnDeadZone)
        {
            return FirstPersonLegsAnimationKey.TurnLeft;
        }

        if (yawInput > turnDeadZone)
        {
            return FirstPersonLegsAnimationKey.TurnRight;
        }

        return FirstPersonLegsAnimationKey.StandIdle;
    }

    private static FirstPersonLegsAnimationKey ResolveWalkMove(bool forward, bool back, bool left, bool right)
    {
        if (forward && left)
        {
            return FirstPersonLegsAnimationKey.WalkForwardLeft;
        }

        if (forward && right)
        {
            return FirstPersonLegsAnimationKey.WalkForwardRight;
        }

        if (back && left)
        {
            return FirstPersonLegsAnimationKey.WalkBackLeft;
        }

        if (back && right)
        {
            return FirstPersonLegsAnimationKey.WalkBackRight;
        }

        if (forward)
        {
            return FirstPersonLegsAnimationKey.WalkForward;
        }

        if (back)
        {
            return FirstPersonLegsAnimationKey.WalkBack;
        }

        return left ? FirstPersonLegsAnimationKey.WalkLeft : FirstPersonLegsAnimationKey.WalkRight;
    }

    private static FirstPersonLegsAnimationKey ResolveCrouchMove(bool forward, bool back, bool left, bool right)
    {
        if (forward && left)
        {
            return FirstPersonLegsAnimationKey.CrouchForwardLeft;
        }

        if (forward && right)
        {
            return FirstPersonLegsAnimationKey.CrouchForwardRight;
        }

        if (back && left)
        {
            return FirstPersonLegsAnimationKey.CrouchBackLeft;
        }

        if (back && right)
        {
            return FirstPersonLegsAnimationKey.CrouchBackRight;
        }

        if (forward)
        {
            return FirstPersonLegsAnimationKey.CrouchForward;
        }

        if (back)
        {
            return FirstPersonLegsAnimationKey.CrouchBack;
        }

        return left ? FirstPersonLegsAnimationKey.CrouchLeft : FirstPersonLegsAnimationKey.CrouchRight;
    }

    private static FirstPersonLegsAnimationKey ResolveSprintMove(bool left, bool right)
    {
        if (left)
        {
            return FirstPersonLegsAnimationKey.SprintForwardLeft;
        }

        if (right)
        {
            return FirstPersonLegsAnimationKey.SprintForwardRight;
        }

        return FirstPersonLegsAnimationKey.SprintForward;
    }
}