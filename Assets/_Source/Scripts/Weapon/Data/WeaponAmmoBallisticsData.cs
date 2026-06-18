using System;
using UnityEngine;

[Serializable]
public sealed class WeaponAmmoBallisticsData
{
    [SerializeField] private ItemData _ammoData;
    [SerializeField] [Min(0f)] private float _bulletVelocityMetersPerSecondOverride;

    public ItemData AmmoData => _ammoData;
    public float BulletVelocityMetersPerSecondOverride => Mathf.Max(0f, _bulletVelocityMetersPerSecondOverride);

    public float ResolveBulletVelocityMetersPerSecond()
    {
        if (BulletVelocityMetersPerSecondOverride > 0f)
        {
            return BulletVelocityMetersPerSecondOverride;
        }

        return _ammoData == null ? 0f : _ammoData.AmmoBulletVelocityMetersPerSecondFallback;
    }
}
