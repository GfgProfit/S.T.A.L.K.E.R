using UnityEngine;

public class PlayerCameraFieldOfViewController
{
    private readonly Camera _camera;

    public PlayerCameraFieldOfViewController(Camera camera)
    {
        _camera = camera;
    }

    public void Update(bool isSprinting, bool isCrouching, float defaultFieldOfView, float sprintFieldOfView, float smooth)
    {
        float targetFieldOfView = isSprinting && !isCrouching ? sprintFieldOfView : defaultFieldOfView;

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFieldOfView, smooth * Time.deltaTime);
    }
}