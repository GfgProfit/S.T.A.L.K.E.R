using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponCollision : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _weaponRoot;
    [SerializeField] private LayerMask _hitMask;

    [Header("Detection")]
    [SerializeField] [Min(0.01f)] private float _checkDistance = 0.8f;
    [SerializeField] [Min(0f)] private float _collisionRadius;

    [Header("Pose")]
    [SerializeField] private float _minOffset;
    [SerializeField] private float _maxOffset = -0.3f;
    [SerializeField] private Vector3 _maxRotationOffset = new(-12f, 0f, 0f);
    [SerializeField] [Min(0f)] private float _smooth = 14f;
    [SerializeField] [Min(0f)] private float _returnSmooth = 8f;

    [Header("State")]
    [SerializeField] [Range(0f, 1f)] private float _obstructedEnterThreshold = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float _obstructedExitThreshold = 0.3f;

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private RaycastHit _lastHit;
    private bool _didHit;
    private bool _hasBasePose;

    public float CollisionAmount { get; private set; }
    public bool IsObstructed { get; private set; }

    private void Awake()
    {
        if (_weaponRoot != null)
        {
            CacheBasePose();
        }

        if (ValidateLinks() == false)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_weaponRoot == null)
        {
            return;
        }

        CacheBasePose();
        ResetState();
    }

    private void LateUpdate()
    {
        if (_playerCamera == null || _weaponRoot == null)
        {
            return;
        }

        bool didHit = DetectObstacle(out RaycastHit hit);
        float targetAmount = didHit
            ? Mathf.InverseLerp(_checkDistance, 0f, hit.distance)
            : 0f;

        _didHit = didHit;

        if (didHit)
        {
            _lastHit = hit;
        }

        float speed = targetAmount > CollisionAmount ? _smooth : _returnSmooth;
        CollisionAmount = Mathf.Lerp(CollisionAmount, targetAmount, GetLerpFactor(speed, Time.deltaTime));
        UpdateObstructedState();
        ApplyCollisionPose(SmoothStep(CollisionAmount));
    }

    private void OnDisable()
    {
        if (_weaponRoot != null && _hasBasePose)
        {
            _weaponRoot.SetLocalPositionAndRotation(_baseLocalPosition, _baseLocalRotation);
        }

        ResetState();
    }

    public void SetCollisionDistance(float value) => _checkDistance = Mathf.Max(0.01f, value);

    private bool ValidateLinks()
    {
        if (_playerCamera == null)
        {
            Debug.LogError($"{nameof(WeaponCollision)} requires a player camera.", this);
            return false;
        }

        if (_weaponRoot == null)
        {
            Debug.LogError($"{nameof(WeaponCollision)} requires a weapon root.", this);
            return false;
        }

        return true;
    }

    private void CacheBasePose()
    {
        _baseLocalPosition = _weaponRoot.localPosition;
        _baseLocalRotation = _weaponRoot.localRotation;
        _hasBasePose = true;
    }

    private bool DetectObstacle(out RaycastHit hit)
    {
        Transform cameraTransform = _playerCamera.transform;
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;

        if (_collisionRadius <= 0f)
        {
            return Physics.Raycast(origin, direction, out hit, _checkDistance, _hitMask, QueryTriggerInteraction.Ignore);
        }

        return Physics.SphereCast(origin, _collisionRadius, direction, out hit, _checkDistance, _hitMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyCollisionPose(float amount)
    {
        float offset = Mathf.Lerp(_minOffset, _maxOffset, amount);
        Vector3 targetPosition = _baseLocalPosition + Vector3.forward * offset;
        Quaternion targetRotation = _baseLocalRotation * Quaternion.Euler(_maxRotationOffset * amount);
        _weaponRoot.SetLocalPositionAndRotation(targetPosition, targetRotation);
    }

    private void UpdateObstructedState()
    {
        if (IsObstructed)
        {
            IsObstructed = CollisionAmount > _obstructedExitThreshold;
            return;
        }

        IsObstructed = CollisionAmount >= _obstructedEnterThreshold;
    }

    private void ResetState()
    {
        CollisionAmount = 0f;
        IsObstructed = false;
        _didHit = false;
        _lastHit = default;
    }

    private static float GetLerpFactor(float speed, float deltaTime) => speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * deltaTime);
    private static float SmoothStep(float value) => value * value * (3f - 2f * value);

    private void OnValidate()
    {
        _checkDistance = Mathf.Max(0.01f, _checkDistance);
        _collisionRadius = Mathf.Max(0f, _collisionRadius);
        _smooth = Mathf.Max(0f, _smooth);
        _returnSmooth = Mathf.Max(0f, _returnSmooth);
        _obstructedEnterThreshold = Mathf.Clamp01(_obstructedEnterThreshold);
        _obstructedExitThreshold = Mathf.Clamp(_obstructedExitThreshold, 0f, _obstructedEnterThreshold);
    }

    private void OnDrawGizmosSelected()
    {
        if (_playerCamera == null)
        {
            return;
        }

        Transform cameraTransform = _playerCamera.transform;
        Vector3 start = cameraTransform.position;
        Vector3 end = start + cameraTransform.forward * _checkDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);

        if (_collisionRadius > 0f)
        {
            Gizmos.DrawWireSphere(start, _collisionRadius);
            Gizmos.DrawWireSphere(end, _collisionRadius);
        }

        if (_didHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastHit.point, 0.03f);
            Gizmos.DrawRay(_lastHit.point, _lastHit.normal * 0.15f);
        }

        if (_weaponRoot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_weaponRoot.position, 0.02f);
        }
    }
}
