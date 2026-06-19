using UnityEngine;

public class PlayerCameraFieldOfViewController
{
    private readonly Camera _camera;

    public PlayerCameraFieldOfViewController(Camera camera)
    {
        _camera = camera;
    }

    public void Update(
        bool isSprinting,
        bool isCrouching,
        bool isUnarmedAiming,
        float defaultFieldOfView,
        float sprintFieldOfView,
        float unarmedAimFieldOfView,
        float smooth)
    {
        float targetFieldOfView = isUnarmedAiming
            ? unarmedAimFieldOfView
            : isSprinting && !isCrouching
                ? sprintFieldOfView
                : defaultFieldOfView;

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFieldOfView, smooth * Time.deltaTime);
    }
}
