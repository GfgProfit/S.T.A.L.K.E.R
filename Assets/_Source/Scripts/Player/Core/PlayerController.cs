using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public partial class PlayerController : MonoBehaviour
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

    private float _xRotation;
    private float _currentSpeed;
    private float _verticalVelocity;
    private Vector3 _rawInput;
    private Vector3 _cameraTargetLocalPos;
    private Vector3 _cameraDefaultLocalPos;

    public bool IsGrounded { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool CanSprinting { get; private set; } = true;
    public bool IsWalking { get; private set; }
    public bool CanJumping { get; private set; } = true;
    public bool IsCrouching { get; private set; }
    public bool CanCrouching { get; private set; } = true;
}
