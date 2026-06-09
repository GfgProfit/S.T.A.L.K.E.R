public static class PlayerLocomotionStateResolver
{
    public static PlayerLocomotionState Resolve(bool isMoving, bool isRunning, bool isCrouching)
    {
        if (isCrouching)
        {
            return PlayerLocomotionState.Walking;
        }

        if (isRunning)
        {
            return PlayerLocomotionState.Running;
        }

        if (isMoving)
        {
            return PlayerLocomotionState.Walking;
        }

        return PlayerLocomotionState.Idle;
    }
}