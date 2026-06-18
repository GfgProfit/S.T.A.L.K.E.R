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
    [SerializeField] private List<WeaponAmmoBallisticsData> _compatibleAmmo = new();

    [Header("Ballistics")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private LayerMask _ballisticHitMask = Physics.DefaultRaycastLayers;
    [SerializeField] [Min(0f)] private float _ballisticCollisionRadiusMeters;
    [SerializeField] [Min(0.0001f)] private float _ballisticSimulationStepSeconds = 1f / 240f;
    [SerializeField] [Min(1)] private int _ballisticMaxSimulationStepsPerFrame = 64;
    [SerializeField] [Min(0.01f)] private float _ballisticMaxLifetimeSeconds = 10f;
    [SerializeField] [Min(0.01f)] private float _ballisticMaxDistanceMeters = 2000f;
    [SerializeField] [Min(0f)] private float _ballisticGravityScale = 1f;
    [SerializeField] private bool _useAirResistance;
    [SerializeField] [Min(0f)] private float _airDensityKilogramsPerCubicMeter = 1.225f;

    [Header("Ballistics/Ricochet")]
    [SerializeField] private bool _enableRicochet;
    [Tooltip("Angle between the inverse flight direction and the surface normal. 0 is head-on, 90 is grazing.")]
    [SerializeField] [Range(0f, 90f)] private float _ricochetMinimumIncidenceAngleDegrees = 70f;
    [SerializeField] [Range(0f, 1f)] private float _ricochetSpeedRetention = 0.5f;
    [SerializeField] [Min(0f)] private float _ricochetSurfaceOffsetMeters = 0.002f;

    [Header("Ballistics/Debug")]
    [SerializeField] private bool _drawBallisticDebug;
    [SerializeField] [Min(0f)] private float _ballisticDebugDurationSeconds = 2f;

    [Header("Recoil")]
    [SerializeField] [Min(0f)] private float _recoilX = 2f;
    [SerializeField] [Min(0f)] private float _recoilY = 1f;
    [SerializeField] [Min(0f)] private float _recoilZ = 0.2f;
    [SerializeField] [Min(0f)] private float _recoilReturnSpeed = 8f;
    [SerializeField] [Min(0f)] private float _recoilSnappiness = 16f;
    [SerializeField] [Range(0f, 100f)] private float _crouchRecoilReductionPercent = 25f;

    public int MagazineCapacity => Mathf.Max(1, _magazineCapacity);
    public int DurabilityShotsResource => Mathf.Max(1, _durabilityShotsResource);
    public float DurabilityPercentPerShot => 100f / DurabilityShotsResource;
    public int FireRateRoundsPerMinute => Mathf.Max(1, _fireRateRoundsPerMinute);
    public float SecondsBetweenShots => 60f / FireRateRoundsPerMinute;
    public WeaponFireMode FireMode => _fireMode;
    public IReadOnlyList<WeaponAmmoBallisticsData> CompatibleAmmo => _compatibleAmmo;
    public bool HasCompatibleAmmo => _compatibleAmmo != null && _compatibleAmmo.Count > 0;
    public GameObject BulletPrefab => _bulletPrefab;
    public LayerMask BallisticHitMask => _ballisticHitMask;
    public float BallisticCollisionRadiusMeters => Mathf.Max(0f, _ballisticCollisionRadiusMeters);
    public float BallisticSimulationStepSeconds => Mathf.Max(0.0001f, _ballisticSimulationStepSeconds);
    public int BallisticMaxSimulationStepsPerFrame => Mathf.Max(1, _ballisticMaxSimulationStepsPerFrame);
    public float BallisticMaxLifetimeSeconds => Mathf.Max(0.01f, _ballisticMaxLifetimeSeconds);
    public float BallisticMaxDistanceMeters => Mathf.Max(0.01f, _ballisticMaxDistanceMeters);
    public float BallisticGravityScale => Mathf.Max(0f, _ballisticGravityScale);
    public bool UseAirResistance => _useAirResistance;
    public float AirDensityKilogramsPerCubicMeter => Mathf.Max(0f, _airDensityKilogramsPerCubicMeter);
    public bool EnableRicochet => _enableRicochet;
    public float RicochetMinimumIncidenceAngleDegrees => Mathf.Clamp(_ricochetMinimumIncidenceAngleDegrees, 0f, 90f);
    public float RicochetSpeedRetention => Mathf.Clamp01(_ricochetSpeedRetention);
    public float RicochetSurfaceOffsetMeters => Mathf.Max(0f, _ricochetSurfaceOffsetMeters);
    public bool DrawBallisticDebug => _drawBallisticDebug;
    public float BallisticDebugDurationSeconds => Mathf.Max(0f, _ballisticDebugDurationSeconds);
    public float RecoilX => Mathf.Max(0f, _recoilX);
    public float RecoilY => Mathf.Max(0f, _recoilY);
    public float RecoilZ => Mathf.Max(0f, _recoilZ);
    public float RecoilReturnSpeed => Mathf.Max(0f, _recoilReturnSpeed);
    public float RecoilSnappiness => Mathf.Max(0f, _recoilSnappiness);
    public float CrouchRecoilReductionPercent => Mathf.Clamp(_crouchRecoilReductionPercent, 0f, 100f);

    private void OnValidate()
    {
        _magazineCapacity = Mathf.Max(1, _magazineCapacity);
        _durabilityShotsResource = Mathf.Max(1, _durabilityShotsResource);
        _fireRateRoundsPerMinute = Mathf.Max(1, _fireRateRoundsPerMinute);
        _reloadAmmoApplyFrame = Mathf.Max(0, _reloadAmmoApplyFrame);
        _reloadFullAmmoApplyFrame = Mathf.Max(0, _reloadFullAmmoApplyFrame);
        _ballisticCollisionRadiusMeters = Mathf.Max(0f, _ballisticCollisionRadiusMeters);
        _ballisticSimulationStepSeconds = Mathf.Max(0.0001f, _ballisticSimulationStepSeconds);
        _ballisticMaxSimulationStepsPerFrame = Mathf.Max(1, _ballisticMaxSimulationStepsPerFrame);
        _ballisticMaxLifetimeSeconds = Mathf.Max(0.01f, _ballisticMaxLifetimeSeconds);
        _ballisticMaxDistanceMeters = Mathf.Max(0.01f, _ballisticMaxDistanceMeters);
        _ballisticGravityScale = Mathf.Max(0f, _ballisticGravityScale);
        _airDensityKilogramsPerCubicMeter = Mathf.Max(0f, _airDensityKilogramsPerCubicMeter);
        _ricochetMinimumIncidenceAngleDegrees = Mathf.Clamp(_ricochetMinimumIncidenceAngleDegrees, 0f, 90f);
        _ricochetSpeedRetention = Mathf.Clamp01(_ricochetSpeedRetention);
        _ricochetSurfaceOffsetMeters = Mathf.Max(0f, _ricochetSurfaceOffsetMeters);
        _ballisticDebugDurationSeconds = Mathf.Max(0f, _ballisticDebugDurationSeconds);
        _recoilX = Mathf.Max(0f, _recoilX);
        _recoilY = Mathf.Max(0f, _recoilY);
        _recoilZ = Mathf.Max(0f, _recoilZ);
        _recoilReturnSpeed = Mathf.Max(0f, _recoilReturnSpeed);
        _recoilSnappiness = Mathf.Max(0f, _recoilSnappiness);
        _crouchRecoilReductionPercent = Mathf.Clamp(_crouchRecoilReductionPercent, 0f, 100f);
    }

    public int GetReloadAmmoApplyFrame(bool fullReload) => fullReload ? Mathf.Max(0, _reloadFullAmmoApplyFrame) : Mathf.Max(0, _reloadAmmoApplyFrame);

    public ItemData GetCompatibleAmmo(int index)
    {
        if (_compatibleAmmo == null || index < 0 || index >= _compatibleAmmo.Count)
        {
            return null;
        }

        WeaponAmmoBallisticsData ammoBallisticsData = _compatibleAmmo[index];
        return ammoBallisticsData == null ? null : ammoBallisticsData.AmmoData;
    }

    public WeaponAmmoBallisticsData GetCompatibleAmmoBallistics(int index)
    {
        if (_compatibleAmmo == null || index < 0 || index >= _compatibleAmmo.Count)
        {
            return null;
        }

        return _compatibleAmmo[index];
    }

    public float GetBulletVelocityMetersPerSecond(ItemData ammoData)
    {
        int ammoIndex = GetCompatibleAmmoIndex(ammoData);
        WeaponAmmoBallisticsData ammoBallisticsData = GetCompatibleAmmoBallistics(ammoIndex);
        return ammoBallisticsData == null ? 0f : ammoBallisticsData.ResolveBulletVelocityMetersPerSecond();
    }

    public int GetCompatibleAmmoIndex(ItemData ammoData)
    {
        if (ammoData == null || _compatibleAmmo == null)
        {
            return -1;
        }

        for (int i = 0; i < _compatibleAmmo.Count; i++)
        {
            WeaponAmmoBallisticsData ammoBallisticsData = _compatibleAmmo[i];

            if (ammoBallisticsData != null && ammoBallisticsData.AmmoData == ammoData)
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
