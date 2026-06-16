using UnityEngine;

public sealed class FirstPersonWeaponMotionController : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Transform _weaponBob;
    [SerializeField] private Transform _weaponCrouch;
    [SerializeField] private Transform _weaponHolder;

    [Header("Crouch")]
    [SerializeField] private Vector3 _crouchPositionOffset = new(-0.05f, -0.04f, -0.02f);
    [SerializeField] private Vector3 _crouchRotationOffset = new(0f, -8f, 0f);
    [SerializeField] [Min(0f)] private float _crouchSmoothSpeed = 8f;

    [Header("Jump Bob")]
    [SerializeField] private Vector3 _airbornePositionOffset = new(0f, -0.06f, 0f);
    [SerializeField] private Vector3 _airborneRotationOffset = Vector3.zero;
    [SerializeField] [Min(0f)] private float _airborneSmoothSpeed = 14f;
    [SerializeField] private Vector3 _landingPositionOffset = new(0f, 0.035f, 0f);
    [SerializeField] private Vector3 _landingRotationOffset = Vector3.zero;
    [SerializeField] [Min(0f)] private float _landingKickDuration = 0.08f;
    [SerializeField] [Min(0f)] private float _landingReturnDuration = 0.16f;

    private Vector3 _bobBasePosition;
    private Quaternion _bobBaseRotation;
    private Vector3 _crouchBasePosition;
    private Quaternion _crouchBaseRotation;
    private Vector3 _landingStartPosition;
    private Quaternion _landingStartRotation;
    private bool _hasGroundState;
    private bool _wasGrounded;
    private float _landingElapsed;
    private WeaponJumpBobState _jumpBobState;

    private void Awake() => CacheBaseTransforms();

    private void OnEnable()
    {
        CacheBaseTransforms();
        _hasGroundState = false;
        _jumpBobState = WeaponJumpBobState.Grounded;
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        UpdateCrouch(deltaTime);
        UpdateJumpBob(deltaTime);
    }

    private void OnDisable()
    {
        ResetTransform(_weaponBob, _bobBasePosition, _bobBaseRotation);
        ResetTransform(_weaponCrouch, _crouchBasePosition, _crouchBaseRotation);
    }

    private void CacheBaseTransforms()
    {
        if (_weaponBob != null)
        {
            _bobBasePosition = _weaponBob.localPosition;
            _bobBaseRotation = _weaponBob.localRotation;
        }

        if (_weaponCrouch != null)
        {
            _crouchBasePosition = _weaponCrouch.localPosition;
            _crouchBaseRotation = _weaponCrouch.localRotation;
        }
    }

    private void UpdateCrouch(float deltaTime)
    {
        if (_weaponCrouch == null)
        {
            return;
        }

        bool isCrouching = _playerController != null && _playerController.IsCrouching;
        Vector3 targetPosition = _crouchBasePosition + (isCrouching ? _crouchPositionOffset : Vector3.zero);
        Quaternion targetRotation = _crouchBaseRotation * Quaternion.Euler(isCrouching ? _crouchRotationOffset : Vector3.zero);
        float factor = GetLerpFactor(_crouchSmoothSpeed, deltaTime);

        _weaponCrouch.SetLocalPositionAndRotation(Vector3.Lerp(_weaponCrouch.localPosition, targetPosition, factor), Quaternion.Slerp(_weaponCrouch.localRotation, targetRotation, factor));
    }

    private void UpdateJumpBob(float deltaTime)
    {
        if (_weaponBob == null)
        {
            return;
        }

        bool isGrounded = _playerController == null || _playerController.IsGrounded;

        if (_hasGroundState == false)
        {
            _hasGroundState = true;
            _wasGrounded = isGrounded;
        }
        else if (_wasGrounded && isGrounded == false)
        {
            StartAirborne();
        }
        else if (_wasGrounded == false && isGrounded)
        {
            StartLanding();
        }

        _wasGrounded = isGrounded;

        switch (_jumpBobState)
        {
            case WeaponJumpBobState.Airborne:
                ApplyBobTarget(_airbornePositionOffset, _airborneRotationOffset, GetLerpFactor(_airborneSmoothSpeed, deltaTime));
                break;
            case WeaponJumpBobState.LandingKick:
                UpdateLanding(deltaTime);
                break;
            default:
                ApplyBobTarget(Vector3.zero, Vector3.zero, GetLerpFactor(_airborneSmoothSpeed, deltaTime));
                break;
        }
    }

    private void StartAirborne()
    {
        _jumpBobState = WeaponJumpBobState.Airborne;
        _landingElapsed = 0f;
    }

    private void StartLanding()
    {
        _jumpBobState = WeaponJumpBobState.LandingKick;
        _landingElapsed = 0f;
        _landingStartPosition = _weaponBob.localPosition;
        _landingStartRotation = _weaponBob.localRotation;
    }

    private void UpdateLanding(float deltaTime)
    {
        _landingElapsed += Mathf.Max(0f, deltaTime);

        if (_landingElapsed <= _landingKickDuration)
        {
            float progress = GetNormalizedTime(_landingElapsed, _landingKickDuration);
            SetBobPose(Vector3.Lerp(_landingStartPosition, _bobBasePosition + _landingPositionOffset, EaseOut(progress)), Quaternion.Slerp(_landingStartRotation, _bobBaseRotation * Quaternion.Euler(_landingRotationOffset), EaseOut(progress)));
            return;
        }

        float returnElapsed = _landingElapsed - _landingKickDuration;
        float returnProgress = GetNormalizedTime(returnElapsed, _landingReturnDuration);
        SetBobPose(Vector3.Lerp(_bobBasePosition + _landingPositionOffset, _bobBasePosition, EaseOut(returnProgress)), Quaternion.Slerp(_bobBaseRotation * Quaternion.Euler(_landingRotationOffset), _bobBaseRotation, EaseOut(returnProgress)));

        if (returnProgress >= 1f)
        {
            _jumpBobState = WeaponJumpBobState.Grounded;
        }
    }

    private void ApplyBobTarget(Vector3 positionOffset, Vector3 rotationOffset, float factor) => _weaponBob.SetLocalPositionAndRotation(Vector3.Lerp(_weaponBob.localPosition, _bobBasePosition + positionOffset, factor), Quaternion.Slerp(_weaponBob.localRotation, _bobBaseRotation * Quaternion.Euler(rotationOffset), factor));
    private void SetBobPose(Vector3 localPosition, Quaternion localRotation) => _weaponBob.SetLocalPositionAndRotation(localPosition, localRotation);
    private static float GetLerpFactor(float speed, float deltaTime) => speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * deltaTime);
    private static float GetNormalizedTime(float elapsed, float duration) => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    private static float EaseOut(float time) => 1f - (1f - time) * (1f - time);

    private static void ResetTransform(Transform target, Vector3 localPosition, Quaternion localRotation)
    {
        if (target == null)
        {
            return;
        }

        target.SetLocalPositionAndRotation(localPosition, localRotation);
    }
}