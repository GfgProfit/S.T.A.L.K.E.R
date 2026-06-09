using UnityEngine;

public partial class PlayerController
{
    private void Look()
    {
        Vector2 mouseDelta = _playerInput.GetMouseDelta();
        float mouseX = mouseDelta.x * _mouseSensitivity;
        float mouseY = mouseDelta.y * _mouseSensitivity;

        _xRotation = Mathf.Clamp(_xRotation - mouseY, _cameraClampLimit.x, _cameraClampLimit.y);

        _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0.0f, 0.0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}