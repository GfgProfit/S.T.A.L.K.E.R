using UnityEngine;

public class PlayerCrouchController
{
    private readonly CharacterController _characterController;
    private readonly Transform _cameraTransform;
    private readonly VignetteController _vignetteController;
    private readonly float _standHeight;
    private readonly float _crouchHeight;
    private readonly Vector3 _crouchCameraOffset;
    private readonly Vector3 _cameraDefaultLocalPosition;

    private Vector3 _cameraTargetLocalPosition;

    public PlayerCrouchController(CharacterController characterController, Transform cameraTransform, VignetteController vignetteController, float standHeight, float crouchHeight, Vector3 crouchCameraOffset)
    {
        _characterController = characterController;
        _cameraTransform = cameraTransform;
        _vignetteController = vignetteController;
        _standHeight = standHeight;
        _crouchHeight = crouchHeight;
        _crouchCameraOffset = crouchCameraOffset;
        _cameraDefaultLocalPosition = cameraTransform.localPosition;
        _cameraTargetLocalPosition = _cameraDefaultLocalPosition;
    }

    public bool IsCrouching { get; private set; }

    public void Tick(IPlayerMovementInput playerInput, bool canCrouching, float transitionSpeed)
    {
        if (!canCrouching)
        {
            return;
        }

        bool crouchPressed = playerInput.IsCrouchingHold();

        if (crouchPressed && !IsCrouching)
        {
            StartCrouch();
        }
        else if (!crouchPressed && IsCrouching)
        {
            StopCrouch();
        }

        _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, _cameraTargetLocalPosition, Time.deltaTime * transitionSpeed);
    }

    private void StartCrouch()
    {
        IsCrouching = true;
        _characterController.height = _crouchHeight;
        _characterController.center = new(0.0f, _crouchHeight / 2.0f, 0.0f);
        _cameraTargetLocalPosition = _cameraDefaultLocalPosition + _crouchCameraOffset;

        _vignetteController.AnimateIntensityCrouch();
    }

    private void StopCrouch()
    {
        IsCrouching = false;
        _characterController.height = _standHeight;
        _characterController.center = new(0.0f, _standHeight / 2.0f, 0.0f);
        _cameraTargetLocalPosition = _cameraDefaultLocalPosition;

        _vignetteController.AnimateIntensityBase();
    }
}