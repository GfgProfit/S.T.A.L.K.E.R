using UnityEngine;

public interface IBallisticSurface
{
    bool TryResolvePenetration(BallisticImpactContext impact, out BallisticPenetrationResult result);
}

public readonly struct BallisticImpactContext
{
    public BallisticImpactContext(GameObject projectile, ItemData ammoData, RaycastHit hit, Vector3 incomingVelocity)
    {
        Projectile = projectile;
        AmmoData = ammoData;
        Hit = hit;
        IncomingVelocity = incomingVelocity;
    }

    public GameObject Projectile { get; }
    public ItemData AmmoData { get; }
    public RaycastHit Hit { get; }
    public Vector3 IncomingVelocity { get; }
    public float IncomingSpeedMetersPerSecond => IncomingVelocity.magnitude;
    public Collider Collider => Hit.collider;
    public Object PhysicsMaterial => Hit.collider == null ? null : Hit.collider.sharedMaterial;
}

public readonly struct BallisticPenetrationResult
{
    public BallisticPenetrationResult(bool continueProjectile, Vector3 exitPosition, Vector3 exitVelocity)
    {
        ContinueProjectile = continueProjectile;
        ExitPosition = exitPosition;
        ExitVelocity = exitVelocity;
    }

    public bool ContinueProjectile { get; }
    public Vector3 ExitPosition { get; }
    public Vector3 ExitVelocity { get; }
}
