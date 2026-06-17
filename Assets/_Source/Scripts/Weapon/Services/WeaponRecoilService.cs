using UnityEngine;
using Random = UnityEngine.Random;

public sealed class WeaponRecoilService
{
    private readonly Transform _weaponRecoilTransform;
    private readonly Quaternion _baseLocalRotation;
    private Vector3 _targetRotation;
    private Vector3 _currentRotation;

    public WeaponRecoilService(Transform weaponRecoilTransform)
    {
        _weaponRecoilTransform = weaponRecoilTransform;
        _baseLocalRotation = weaponRecoilTransform == null ? Quaternion.identity : weaponRecoilTransform.localRotation;
    }

    public void Tick(float returnSpeed, float snappiness)
    {
        if (_weaponRecoilTransform == null)
        {
            return;
        }

        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, snappiness * Time.deltaTime);
        _weaponRecoilTransform.localRotation = _baseLocalRotation * Quaternion.Euler(_currentRotation);
    }

    public void RecoilShoot(float recoilX, float recoilY, float recoilZ)
    {
        if (_weaponRecoilTransform == null)
        {
            return;
        }

        _targetRotation += new Vector3(-recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }

    public void Reset()
    {
        _targetRotation = Vector3.zero;
        _currentRotation = Vector3.zero;

        if (_weaponRecoilTransform != null)
        {
            _weaponRecoilTransform.localRotation = _baseLocalRotation;
        }
    }
}
