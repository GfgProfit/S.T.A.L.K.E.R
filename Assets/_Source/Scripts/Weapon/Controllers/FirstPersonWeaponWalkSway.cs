using UnityEngine;

public sealed class FirstPersonWeaponWalkSway : MonoBehaviour
{
    private const float STRAFE_INPUT_THRESHOLD = 0.01f;

    [SerializeField] private Transform _swayRoot;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] [Min(0f)] private float _rollAngle = 4f;
    [SerializeField] [Min(0f)] private float _returnSmoothTime = 0.08f;
    [SerializeField] [Min(0f)] private float _swaySmoothTime = 0.08f;
    [SerializeField] private bool _invertDirection;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private Quaternion _initialLocalRotation;
    private float _currentRoll;
    private float _rollVelocity;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private void Awake()
    {
        _swayRoot ??= transform;
        _playerController ??= GetComponentInParent<PlayerController>();
        _initialLocalRotation = _swayRoot.localRotation;
    }

    private void OnEnable()
    {
        if (_swayRoot == null)
        {
            return;
        }

        _initialLocalRotation = _swayRoot.localRotation;
        _currentRoll = 0f;
        _rollVelocity = 0f;
    }

    private void OnDisable()
    {
        if (_swayRoot != null)
        {
            _swayRoot.localRotation = _initialLocalRotation;
        }
    }

    private void LateUpdate()
    {
        if (_swayRoot == null)
        {
            return;
        }

        Vector2 movementInput = PlayerInput.GetMovementInput();
        bool shouldTilt = ShouldApplyTilt(movementInput);
        float targetRoll = shouldTilt ? GetTargetRoll(movementInput.x) : 0f;
        float smoothTime = shouldTilt ? _swaySmoothTime : _returnSmoothTime;

        _currentRoll = Mathf.SmoothDampAngle(_currentRoll, targetRoll, ref _rollVelocity, smoothTime);
        _swayRoot.localRotation = _initialLocalRotation * Quaternion.Euler(0f, 0f, _currentRoll);
    }

    private void OnValidate()
    {
        _rollAngle = Mathf.Max(0f, _rollAngle);
        _returnSmoothTime = Mathf.Max(0f, _returnSmoothTime);
        _swaySmoothTime = Mathf.Max(0f, _swaySmoothTime);
    }

    private bool ShouldApplyTilt(Vector2 movementInput)
    {
        if (Mathf.Abs(movementInput.x) <= STRAFE_INPUT_THRESHOLD)
        {
            return false;
        }

        if (_playerController != null)
        {
            return _playerController.IsWalking && _playerController.IsCrouching == false && _playerController.IsSprinting == false;
        }

        return PlayerInput.IsCrouchingHold() == false && PlayerInput.IsSprintHeld() == false;
    }

    private float GetTargetRoll(float strafeInput)
    {
        float direction = _invertDirection ? strafeInput : -strafeInput;
        return Mathf.Clamp(direction, -1f, 1f) * _rollAngle;
    }
}
