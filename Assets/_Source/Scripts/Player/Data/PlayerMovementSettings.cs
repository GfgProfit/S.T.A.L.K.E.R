public readonly struct PlayerMovementSettings
{
    public PlayerMovementSettings(float walkSpeed, float sprintSpeed, float crouchSpeed, float jumpHeight, float gravity, float groundedGravity, float speedSmooth)
    {
        WalkSpeed = walkSpeed;
        SprintSpeed = sprintSpeed;
        CrouchSpeed = crouchSpeed;
        JumpHeight = jumpHeight;
        Gravity = gravity;
        GroundedGravity = groundedGravity;
        SpeedSmooth = speedSmooth;
    }

    public float WalkSpeed { get; }
    public float SprintSpeed { get; }
    public float CrouchSpeed { get; }
    public float JumpHeight { get; }
    public float Gravity { get; }
    public float GroundedGravity { get; }
    public float SpeedSmooth { get; }
}