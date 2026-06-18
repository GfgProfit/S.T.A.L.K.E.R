using UnityEngine;

public class WeaponCollision : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _weaponRoot;
    [SerializeField] private LayerMask _hitMask;

    [Header("Settings")]
    [SerializeField] private float _checkDistance = 0.8f;
    [SerializeField] private float _minOffset = 0.0f;
    [SerializeField] private float _maxOffset = -0.3f;
    [SerializeField] private float _smooth = 10f;

    private float _currentOffset = 0f;
    private RaycastHit _lastHit;
    private bool _didHit;

    private void Update()
    {
        if (Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out RaycastHit hit, _checkDistance, _hitMask))
        {
            _didHit = true;
            _lastHit = hit;

            float t = 1f - (hit.distance / _checkDistance);
            float targetOffset = Mathf.Lerp(_minOffset, _maxOffset, t);

            _currentOffset = Mathf.Lerp(_currentOffset, targetOffset, Time.deltaTime * _smooth);
        }
        else
        {
            _didHit = false;
            _currentOffset = Mathf.Lerp(_currentOffset, _minOffset, Time.deltaTime * _smooth);
        }

        Vector3 localPos = _weaponRoot.localPosition;
        localPos.z = _currentOffset;
        _weaponRoot.localPosition = localPos;
    }

    public void SetCollisionDistance(float value) => _checkDistance = value;

    private void OnDrawGizmos()
    {
        if (_playerCamera == null)
            return;

        Gizmos.color = Color.yellow;
        Vector3 start = _playerCamera.transform.position;
        Vector3 dir = _playerCamera.transform.forward * _checkDistance;
        Gizmos.DrawLine(start, start + dir);

        if (_didHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastHit.point, 0.03f);
        }

        if (_weaponRoot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_weaponRoot.position, 0.02f);
        }
    }
}