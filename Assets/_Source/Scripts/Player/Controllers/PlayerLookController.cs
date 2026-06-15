using UnityEngine;

public class PlayerLookController
{
    private readonly Transform _playerTransform;
    private readonly Transform _cameraTransform;

    private float _xRotation;

    public PlayerLookController(Transform playerTransform, Transform cameraTransform)
    {
        _playerTransform = playerTransform;
        _cameraTransform = cameraTransform;
    }

    public void Look(IPlayerLookInput playerInput, float mouseSensitivity, Vector2 cameraClampLimit)
    {
        Vector2 mouseDelta = playerInput.GetMouseDelta();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        _xRotation = Mathf.Clamp(_xRotation - mouseY, cameraClampLimit.x, cameraClampLimit.y);

        _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0.0f, 0.0f);
        _playerTransform.Rotate(Vector3.up * mouseX);
    }

    public bool TryGetCameraLocalRotation(out Quaternion localRotation)
    {
        if (_cameraTransform == null)
        {
            localRotation = Quaternion.identity;
            return false;
        }

        localRotation = _cameraTransform.localRotation;
        return true;
    }

    public void RestoreCameraLocalRotation(Quaternion localRotation, Vector2 cameraClampLimit)
    {
        if (_cameraTransform == null)
        {
            return;
        }

        _cameraTransform.localRotation = localRotation;
        _xRotation = Mathf.Clamp(NormalizePitch(localRotation.eulerAngles.x), cameraClampLimit.x, cameraClampLimit.y);
    }

    private static float NormalizePitch(float pitch) => pitch > 180f ? pitch - 360f : pitch;
}