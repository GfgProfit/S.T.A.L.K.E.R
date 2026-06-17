using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    [SerializeField] [Min(1)] private int _magazineCapacity = 1;
    [SerializeField] [Min(1)] private int _durabilityShotsResource = 1000;
    [SerializeField] [Min(1)] private int _fireRateRoundsPerMinute = 600;
    [SerializeField] private WeaponFireMode _fireMode = WeaponFireMode.Semi;
    [SerializeField] [Min(0)] private int _reloadAmmoApplyFrame;
    [SerializeField] [Min(0)] private int _reloadFullAmmoApplyFrame;
    [SerializeField] private List<ItemData> _compatibleAmmo = new();

    [Header("Recoil")]
    [SerializeField] [Min(0f)] private float _recoilX = 2f;
    [SerializeField] [Min(0f)] private float _recoilY = 1f;
    [SerializeField] [Min(0f)] private float _recoilZ = 0.2f;
    [SerializeField] [Min(0f)] private float _recoilReturnSpeed = 8f;
    [SerializeField] [Min(0f)] private float _recoilSnappiness = 16f;

    public int MagazineCapacity => Mathf.Max(1, _magazineCapacity);
    public int DurabilityShotsResource => Mathf.Max(1, _durabilityShotsResource);
    public float DurabilityPercentPerShot => 100f / DurabilityShotsResource;
    public int FireRateRoundsPerMinute => Mathf.Max(1, _fireRateRoundsPerMinute);
    public float SecondsBetweenShots => 60f / FireRateRoundsPerMinute;
    public WeaponFireMode FireMode => _fireMode;
    public IReadOnlyList<ItemData> CompatibleAmmo => _compatibleAmmo;
    public bool HasCompatibleAmmo => _compatibleAmmo != null && _compatibleAmmo.Count > 0;
    public float RecoilX => Mathf.Max(0f, _recoilX);
    public float RecoilY => Mathf.Max(0f, _recoilY);
    public float RecoilZ => Mathf.Max(0f, _recoilZ);
    public float RecoilReturnSpeed => Mathf.Max(0f, _recoilReturnSpeed);
    public float RecoilSnappiness => Mathf.Max(0f, _recoilSnappiness);

    private void OnValidate()
    {
        _magazineCapacity = Mathf.Max(1, _magazineCapacity);
        _durabilityShotsResource = Mathf.Max(1, _durabilityShotsResource);
        _fireRateRoundsPerMinute = Mathf.Max(1, _fireRateRoundsPerMinute);
        _reloadAmmoApplyFrame = Mathf.Max(0, _reloadAmmoApplyFrame);
        _reloadFullAmmoApplyFrame = Mathf.Max(0, _reloadFullAmmoApplyFrame);
        _recoilX = Mathf.Max(0f, _recoilX);
        _recoilY = Mathf.Max(0f, _recoilY);
        _recoilZ = Mathf.Max(0f, _recoilZ);
        _recoilReturnSpeed = Mathf.Max(0f, _recoilReturnSpeed);
        _recoilSnappiness = Mathf.Max(0f, _recoilSnappiness);
    }

    public int GetReloadAmmoApplyFrame(bool fullReload) => fullReload ? Mathf.Max(0, _reloadFullAmmoApplyFrame) : Mathf.Max(0, _reloadAmmoApplyFrame);

    public ItemData GetCompatibleAmmo(int index)
    {
        if (_compatibleAmmo == null || index < 0 || index >= _compatibleAmmo.Count)
        {
            return null;
        }

        return _compatibleAmmo[index];
    }

    public int GetCompatibleAmmoIndex(ItemData ammoData)
    {
        if (ammoData == null || _compatibleAmmo == null)
        {
            return -1;
        }

        for (int i = 0; i < _compatibleAmmo.Count; i++)
        {
            if (_compatibleAmmo[i] == ammoData)
            {
                return i;
            }
        }

        return -1;
    }
}

public enum WeaponFireMode
{
    Semi = 0,
    Auto = 1
}
