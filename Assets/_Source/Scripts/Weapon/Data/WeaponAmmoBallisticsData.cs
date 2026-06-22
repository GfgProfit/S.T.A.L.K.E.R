using System;
using UnityEngine;

[Serializable]
public sealed class WeaponAmmoBallisticsData
{
    [SerializeField] private ItemData _ammoData;

    public ItemData AmmoData => _ammoData;

    public float ResolveBulletVelocityMetersPerSecond() => _ammoData == null ? 0f : _ammoData.AmmoBulletVelocityMetersPerSecondFallback;
}
