using UnityEngine;

internal sealed class FirstPersonWeaponFollower
{
    private readonly Transform _source;
    private readonly Transform _target;
    private readonly Vector3 _positionOffset;
    private readonly Quaternion _rotationOffset;

    private Vector3 _additionalPositionOffset;
    private Quaternion _additionalRotationOffset = Quaternion.identity;
    private bool _isEnabled;

    public FirstPersonWeaponFollower(Transform source, Transform target)
    {
        _source = source;
        _target = target;

        if (_source == null || _target == null)
        {
            return;
        }

        _positionOffset = _source.InverseTransformPoint(_target.position);
        _rotationOffset = Quaternion.Inverse(_source.rotation) * _target.rotation;
    }

    public void SetEnabled(bool isEnabled)
    {
        _isEnabled = isEnabled && _source != null && _target != null;
    }

    public void SetAdditionalOffset(Vector3 positionOffset, Vector3 rotationOffset)
    {
        _additionalPositionOffset = positionOffset;
        _additionalRotationOffset = Quaternion.Euler(rotationOffset);
    }

    public void Tick()
    {
        if (_isEnabled == false)
        {
            return;
        }

        _target.SetPositionAndRotation(_source.TransformPoint(_positionOffset + _additionalPositionOffset), _source.rotation * _rotationOffset * _additionalRotationOffset);
    }
}
