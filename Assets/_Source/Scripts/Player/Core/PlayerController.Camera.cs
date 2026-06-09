using UnityEngine;

public partial class PlayerController
{
    private void UpdateCameraFieldOfView()
    {
        float targetFOV = IsSprinting && !IsCrouching ? _sprintCameraFieldOfView : _defaultCameraFieldOfView;
        _mainCamera.fieldOfView = Mathf.Lerp(_mainCamera.fieldOfView, targetFOV, _fieldOfViewSmooth * Time.deltaTime);
    }
}