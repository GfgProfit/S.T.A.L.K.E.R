using UnityEngine;

public partial class PlayerController
{
    private void HandleCrouch()
    {
        if (!CanCrouching)
        {
            return;
        }

        bool crouchPressed = _playerInput.IsCrouchingHold();

        if (crouchPressed && !IsCrouching)
        {
            StartCrouch();
        }
        else if (!crouchPressed && IsCrouching)
        {
            StopCrouch();
        }

        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, _cameraTargetLocalPos, Time.deltaTime * _crouchTransitionSpeed);
    }

    private void StartCrouch()
    {
        IsCrouching = true;
        _characterController.height = _crouchHeight;
        _characterController.center = new Vector3(0, _crouchHeight / 2f, 0);
        _cameraTargetLocalPos = _cameraDefaultLocalPos + _crouchCameraOffset;

        _vignetteController.AnimateIntensityCrouch();
    }

    private void StopCrouch()
    {
        IsCrouching = false;
        _characterController.height = _standHeight;
        _characterController.center = new Vector3(0, _standHeight / 2f, 0);
        _cameraTargetLocalPos = _cameraDefaultLocalPos;

        _vignetteController.AnimateIntensityBase();
    }
}