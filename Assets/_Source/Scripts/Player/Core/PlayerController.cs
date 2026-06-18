using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _walkSpeed = 3.0f;
    [SerializeField] private float _sprintSpeed = 6.0f;

    [SerializeField] private Vector2 _cameraClampLimit = new(-90.0f, 90.0f);

    [Space]
    [SerializeField] private float _defaultCameraFieldOfView = 60.0f;
    [SerializeField] private float _sprintCameraFieldOfView = 70.0f;
    [SerializeField] private float _fieldOfViewSmooth = 5.0f;

    [Space]
    [SerializeField] private float _mouseSensitivity = 2.0f;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _gravity = -19.62f;
    [SerializeField] private float _groundedGravity = -2.0f;

    [Header("Crouch")]
    [SerializeField] private float _crouchHeight = 1.0f;
    [SerializeField] private float _standHeight = 2.0f;
    [SerializeField] private float _crouchSpeed = 1.5f;
    [SerializeField] private float _crouchTransitionSpeed = 6.0f;
    [SerializeField] private Vector3 _crouchCameraOffset = new(0, -0.5f, 0);

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private VignetteController _vignetteController;

    [Inject] private IPlayerInput _playerInput = null;

    private PlayerLookController _lookController;
    private PlayerMovementController _movementController;
    private PlayerCrouchController _crouchController;
    private PlayerCameraFieldOfViewController _cameraFieldOfViewController;
    private bool _controlsEnabled = true;
    private bool _movementEnabled = true;
    private bool _canCrouching = true;

    public bool IsGrounded => _movementController != null && _movementController.IsGrounded;
    public bool IsSprinting => _movementController != null && _movementController.IsSprinting;
    public bool CanSprinting { get; private set; } = true;
    public bool IsWalking => _movementController != null && _movementController.IsWalking;
    public bool CanJumping { get; private set; } = true;
    public bool IsCrouching => _crouchController != null && _crouchController.IsCrouching;
    public bool CanCrouching => _canCrouching;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _lookController = new(transform, _cameraTransform);
        _movementController = new(_characterController, transform, _walkSpeed);
        _crouchController = new(_characterController, _cameraTransform, _vignetteController, _standHeight, _crouchHeight, _crouchCameraOffset);
        _cameraFieldOfViewController = new(_mainCamera);
    }

    private void Update()
    {
        if (_controlsEnabled)
        {
            _lookController.Look(_playerInput, _mouseSensitivity, _cameraClampLimit);

            if (_movementEnabled)
            {
                _movementController.SetInput(_playerInput, CanSprinting);
                _crouchController.Tick(_playerInput, _canCrouching, _crouchTransitionSpeed);
            }
            else
            {
                ClearMotionInput();
            }
        }
        else
        {
            ClearMotionInput();
        }

        _movementController.Move(_playerInput, _controlsEnabled && _movementEnabled && CanJumping, IsCrouching, CreateMovementSettings());

        _cameraFieldOfViewController.Update(IsSprinting, IsCrouching, _defaultCameraFieldOfView, _sprintCameraFieldOfView, _fieldOfViewSmooth);
    }

    public void SetControlsEnabled(bool controlsEnabled)
    {
        _controlsEnabled = controlsEnabled;

        if (controlsEnabled == false)
        {
            ClearMotionInput();
        }
    }

    public void SetMovementEnabled(bool movementEnabled)
    {
        _movementEnabled = movementEnabled;

        if (movementEnabled == false)
        {
            ClearMotionInput();
        }
    }

    public void SetCanSprinting(bool canSprinting)
    {
        CanSprinting = canSprinting;

        if (canSprinting == false && IsSprinting)
        {
            _movementController.SetInput(_playerInput, false);
        }
    }

    public void SetCanCrouching(bool canCrouching) => _canCrouching = canCrouching;
    public bool TryGetCameraLocalRotation(out Quaternion localRotation) => _lookController.TryGetCameraLocalRotation(out localRotation);
    public void RestoreCameraLocalRotation(Quaternion localRotation) => _lookController.RestoreCameraLocalRotation(localRotation, _cameraClampLimit);
    private void ClearMotionInput() => _movementController.ClearInput();
    private PlayerMovementSettings CreateMovementSettings() => new(_walkSpeed, _sprintSpeed, _crouchSpeed, _jumpHeight, _gravity, _groundedGravity, _fieldOfViewSmooth);
}
