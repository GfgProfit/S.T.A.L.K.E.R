using UnityEngine;

public class PlayerMovementController
{
    private readonly CharacterController _characterController;
    private readonly Transform _playerTransform;

    private float _currentSpeed;
    private float _verticalVelocity;
    private Vector3 _rawInput;

    public PlayerMovementController(CharacterController characterController, Transform playerTransform, float initialSpeed)
    {
        _characterController = characterController;
        _playerTransform = playerTransform;
        _currentSpeed = initialSpeed;
    }

    public bool IsGrounded { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsWalking { get; private set; }

    public void SetInput(IPlayerMovementInput playerInput, bool canSprint, bool isCrouching)
    {
        Vector2 input = playerInput.GetMovementInput();
        _rawInput = new(input.x, 0.0f, input.y);

        IsWalking = _rawInput.x != 0.0f || _rawInput.z != 0.0f;
        IsSprinting = canSprint && isCrouching == false && IsWalking && playerInput.IsSprintHeld() && _rawInput.z > 0.0f;
    }

    public void ClearInput()
    {
        _rawInput = Vector3.zero;
        IsWalking = false;
        IsSprinting = false;
    }

    public void Move(IPlayerMovementInput playerInput, bool canJump, bool isCrouching, PlayerMovementSettings settings)
    {
        float speed = CalculateTargetSpeed(isCrouching, settings);
        UpdateVerticalMotion(playerInput, canJump, isCrouching, settings);

        Vector3 horizontalMove = (_playerTransform.right * _rawInput.x + _playerTransform.forward * _rawInput.z).normalized * speed;
        Vector3 move = horizontalMove + Vector3.up * _verticalVelocity;

        _characterController.Move(move * Time.deltaTime);
    }

    private void UpdateVerticalMotion(IPlayerMovementInput playerInput, bool canJump, bool isCrouching, PlayerMovementSettings settings)
    {
        IsGrounded = _characterController.isGrounded;

        if (IsGrounded && _verticalVelocity < 0.0f)
        {
            _verticalVelocity = settings.GroundedGravity;
        }

        if (canJump && IsGrounded && !isCrouching && playerInput.IsJumpPressed())
        {
            _verticalVelocity = Mathf.Sqrt(settings.JumpHeight * -2.0f * settings.Gravity);
        }

        _verticalVelocity += settings.Gravity * Time.deltaTime;
    }

    private float CalculateTargetSpeed(bool isCrouching, PlayerMovementSettings settings)
    {
        PlayerLocomotionState locomotionState = PlayerLocomotionStateResolver.Resolve(IsWalking, IsSprinting, isCrouching);
        float targetSpeed = ResolveTargetSpeed(locomotionState, isCrouching, settings);

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, settings.SpeedSmooth * Time.deltaTime);

        return _currentSpeed;
    }

    private static float ResolveTargetSpeed(PlayerLocomotionState locomotionState, bool isCrouching, PlayerMovementSettings settings)
    {
        if (isCrouching)
        {
            return settings.CrouchSpeed;
        }

        return locomotionState == PlayerLocomotionState.Running ? settings.SprintSpeed : settings.WalkSpeed;
    }
}
