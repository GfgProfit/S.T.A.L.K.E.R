using UnityEngine;

public partial class PlayerController
{
    private void SetRawInput()
    {
        Vector2 input = _playerInput.GetMovementInput();
        _rawInput = new Vector3(input.x, 0.0f, input.y);
    }

    private void Move()
    {
        float speed = CalculateTargetSpeed();
        UpdateVerticalMotion();

        Vector3 horizontalMove = (transform.right * _rawInput.x + transform.forward * _rawInput.z).normalized * speed;
        Vector3 move = horizontalMove + Vector3.up * _verticalVelocity;

        _characterController.Move(move * Time.deltaTime);
    }

    private void UpdateVerticalMotion()
    {
        IsGrounded = _characterController.isGrounded;

        if (IsGrounded && _verticalVelocity < 0.0f)
        {
            _verticalVelocity = _groundedGravity;
        }

        if (_controlsEnabled && _movementEnabled && CanJumping && IsGrounded && !IsCrouching && _playerInput.IsJumpPressed())
        {
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2.0f * _gravity);
        }

        _verticalVelocity += _gravity * Time.deltaTime;
    }

    private float CalculateTargetSpeed()
    {
        float targetSpeed;

        if (IsCrouching)
        {
            targetSpeed = _crouchSpeed;
        }
        else
        {
            targetSpeed = IsSprinting ? _sprintSpeed : _walkSpeed;
        }

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, _fieldOfViewSmooth * Time.deltaTime);

        return _currentSpeed;
    }

    private void UpdateMotionState()
    {
        IsWalking = _rawInput.x != 0f || _rawInput.z != 0f;

        IsSprinting = CanSprinting && IsWalking && _playerInput.IsSprintHeld() && _rawInput.z > 0;
    }

    private void ClearMotionInput()
    {
        _rawInput = Vector3.zero;
        IsWalking = false;
        IsSprinting = false;
    }
}
