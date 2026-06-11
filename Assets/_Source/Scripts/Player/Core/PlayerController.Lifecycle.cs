using UnityEngine;

public partial class PlayerController
{
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _currentSpeed = _walkSpeed;

        _cameraDefaultLocalPos = _cameraTransform.localPosition;
        _cameraTargetLocalPos = _cameraDefaultLocalPos;
    }

    private void OnValidate()
    {
        _characterController = GetComponent<CharacterController>();
        _mainCamera = Camera.main;
        _cameraTransform = _mainCamera.transform;
    }

    private void Update()
    {
        if (_controlsEnabled)
        {
            Look();
            SetRawInput();
            UpdateMotionState();
            HandleCrouch();
        }
        else
        {
            ClearMotionInput();
        }

        Move();
        UpdateCameraFieldOfView();
    }
}
