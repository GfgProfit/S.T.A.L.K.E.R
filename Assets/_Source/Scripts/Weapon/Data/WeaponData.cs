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

    public int MagazineCapacity => Mathf.Max(1, _magazineCapacity);
    public int DurabilityShotsResource => Mathf.Max(1, _durabilityShotsResource);
    public float DurabilityPercentPerShot => 100f / DurabilityShotsResource;
    public int FireRateRoundsPerMinute => Mathf.Max(1, _fireRateRoundsPerMinute);
    public float SecondsBetweenShots => 60f / FireRateRoundsPerMinute;
    public WeaponFireMode FireMode => _fireMode;
    public IReadOnlyList<ItemData> CompatibleAmmo => _compatibleAmmo;
    public bool HasCompatibleAmmo => _compatibleAmmo != null && _compatibleAmmo.Count > 0;

    private void OnValidate()
    {
        _magazineCapacity = Mathf.Max(1, _magazineCapacity);
        _durabilityShotsResource = Mathf.Max(1, _durabilityShotsResource);
        _fireRateRoundsPerMinute = Mathf.Max(1, _fireRateRoundsPerMinute);
        _reloadAmmoApplyFrame = Mathf.Max(0, _reloadAmmoApplyFrame);
        _reloadFullAmmoApplyFrame = Mathf.Max(0, _reloadFullAmmoApplyFrame);
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
