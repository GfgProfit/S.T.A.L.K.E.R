using UnityEngine;

public partial class PlayerController
{
    public void SetControlsEnabled(bool controlsEnabled)
    {
        _controlsEnabled = controlsEnabled;

        if (controlsEnabled == false)
        {
            ClearMotionInput();
        }
    }

    public void SetCanCrouching(bool canCrouching)
    {
        CanCrouching = canCrouching;
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

    public void RestoreCameraLocalRotation(Quaternion localRotation)
    {
        if (_cameraTransform == null)
        {
            return;
        }

        _cameraTransform.localRotation = localRotation;
        _xRotation = Mathf.Clamp(NormalizePitch(localRotation.eulerAngles.x), _cameraClampLimit.x, _cameraClampLimit.y);
    }

    private static float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }
}
