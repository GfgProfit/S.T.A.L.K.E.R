using System.Collections.Generic;
using UnityEngine;

public sealed class BallisticProjectile : MonoBehaviour
{
    private const int HIT_BUFFER_SIZE = 64;
    private const float MIN_SPEED_SQUARED = 0.0001f;
    private const float MUZZLE_DEBUG_SIZE = 0.025f;
    private const float IMPACT_DEBUG_SIZE = 0.035f;

    private readonly HashSet<Collider> _ignoredColliders = new();
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HIT_BUFFER_SIZE];

    private WeaponData _weaponData;
    private ItemData _ammoData;
    private Vector3 _position;
    private Vector3 _velocity;
    private float _simulationAccumulator;
    private float _elapsedLifetime;
    private float _distanceTravelled;
    private BallisticTracerView _tracerView;
    private bool _initialized;

    public Vector3 Velocity => _velocity;
    public float SpeedMetersPerSecond => _velocity.magnitude;
    public float DistanceTravelledMeters => _distanceTravelled;

    public void Initialize(WeaponData weaponData, ItemData ammoData, Vector3 position, Vector3 direction, float speedMetersPerSecond, Transform ownerRoot, Transform weaponRoot)
    {
        _weaponData = weaponData;
        _ammoData = ammoData;
        _position = position;
        _velocity = direction.normalized * Mathf.Max(0f, speedMetersPerSecond);
        _simulationAccumulator = 0f;
        _elapsedLifetime = 0f;
        _distanceTravelled = 0f;

        AddIgnoredColliders(ownerRoot);
        AddIgnoredColliders(weaponRoot);
        DisableAndIgnoreProjectileColliders();
        DisableProjectileRigidbodyPhysics();

        transform.SetPositionAndRotation(_position, ResolveRotation(_velocity, transform.rotation));
        _initialized = _weaponData != null && _ammoData != null && _velocity.sqrMagnitude > MIN_SPEED_SQUARED;

        if (_initialized == false)
        {
            Destroy(gameObject);
            return;
        }

        _tracerView = BallisticTracerView.Create(_ammoData, _position);

        if (_weaponData.DrawBallisticDebug)
        {
            DrawMarker(_position, MUZZLE_DEBUG_SIZE, Color.cyan);
            Debug.DrawLine(_position, _position + direction.normalized * 0.25f, Color.cyan, _weaponData.BallisticDebugDurationSeconds);
        }
    }

    private void Update()
    {
        if (_initialized == false)
        {
            return;
        }

        float simulationStep = _weaponData.BallisticSimulationStepSeconds;
        int maxSteps = _weaponData.BallisticMaxSimulationStepsPerFrame;
        _simulationAccumulator = Mathf.Min(_simulationAccumulator + Time.deltaTime, simulationStep * maxSteps);

        int simulatedSteps = 0;

        while (_simulationAccumulator >= simulationStep && simulatedSteps < maxSteps)
        {
            _simulationAccumulator -= simulationStep;
            simulatedSteps++;

            if (SimulateStep(simulationStep) == false)
            {
                return;
            }
        }
    }

    private bool SimulateStep(float deltaTime)
    {
        _elapsedLifetime += deltaTime;

        if (_elapsedLifetime >= _weaponData.BallisticMaxLifetimeSeconds || _distanceTravelled >= _weaponData.BallisticMaxDistanceMeters)
        {
            CompleteTracer();
            Destroy(gameObject);
            return false;
        }

        IntegrateRungeKutta4(_position, _velocity, deltaTime, out Vector3 nextPosition, out Vector3 nextVelocity);

        if (IsFinite(nextPosition) == false || IsFinite(nextVelocity) == false)
        {
            Debug.LogError($"[{nameof(BallisticProjectile)}] Simulation produced a non-finite trajectory. Check ballistic mass, diameter, drag and velocity settings.", this);
            CompleteTracer();
            Destroy(gameObject);
            return false;
        }

        Vector3 displacement = nextPosition - _position;
        float travelDistance = displacement.magnitude;

        if (travelDistance <= Mathf.Epsilon)
        {
            _velocity = nextVelocity;
            return true;
        }

        if (TryGetClosestHit(_position, displacement / travelDistance, travelDistance, out RaycastHit hit))
        {
            float impactFraction = Mathf.Clamp01(hit.distance / travelDistance);
            Vector3 impactVelocity = Vector3.Lerp(_velocity, nextVelocity, impactFraction);
            _distanceTravelled += hit.distance;
            MoveTracer(hit.point);

            if (_weaponData.DrawBallisticDebug)
            {
                Debug.DrawLine(_position, hit.point, Color.yellow, _weaponData.BallisticDebugDurationSeconds);
            }

            return ResolveImpact(hit, impactVelocity);
        }

        if (_weaponData.DrawBallisticDebug)
        {
            Debug.DrawLine(_position, nextPosition, Color.green, _weaponData.BallisticDebugDurationSeconds);
        }

        _distanceTravelled += travelDistance;
        _position = nextPosition;
        _velocity = nextVelocity;
        transform.SetPositionAndRotation(_position, ResolveRotation(_velocity, transform.rotation));
        MoveTracer(_position);
        return true;
    }

    private bool ResolveImpact(RaycastHit hit, Vector3 impactVelocity)
    {
        BallisticImpactContext impact = new(gameObject, _ammoData, hit, impactVelocity);
        IBallisticSurface ballisticSurface = hit.collider == null ? null : hit.collider.GetComponentInParent<IBallisticSurface>();

        if (ballisticSurface != null && ballisticSurface.TryResolvePenetration(impact, out BallisticPenetrationResult penetrationResult) &&
            penetrationResult.ContinueProjectile && penetrationResult.ExitVelocity.sqrMagnitude > MIN_SPEED_SQUARED)
        {
            Vector3 exitDirection = penetrationResult.ExitVelocity.normalized;
            float offset = Mathf.Max(_weaponData.RicochetSurfaceOffsetMeters, 0.0001f);
            _position = penetrationResult.ExitPosition + exitDirection * offset;
            _velocity = penetrationResult.ExitVelocity;
            transform.SetPositionAndRotation(_position, ResolveRotation(_velocity, transform.rotation));
            MoveTracer(_position);

            if (_weaponData.DrawBallisticDebug)
            {
                DrawMarker(_position, IMPACT_DEBUG_SIZE, Color.blue);
            }

            return true;
        }

        if (TryRicochet(hit, impactVelocity))
        {
            return true;
        }

        _position = hit.point;
        transform.position = _position;

        if (_weaponData.DrawBallisticDebug)
        {
            DrawMarker(_position, IMPACT_DEBUG_SIZE, Color.red);
        }

        CompleteTracer();
        Destroy(gameObject);
        return false;
    }

    private bool TryRicochet(RaycastHit hit, Vector3 impactVelocity)
    {
        if (_weaponData.EnableRicochet == false || _ammoData.AmmoRicochetChance <= 0f || impactVelocity.sqrMagnitude <= MIN_SPEED_SQUARED)
        {
            return false;
        }

        float incidenceAngle = Vector3.Angle(-impactVelocity.normalized, hit.normal);

        if (incidenceAngle < _weaponData.RicochetMinimumIncidenceAngleDegrees || Random.value > _ammoData.AmmoRicochetChance)
        {
            return false;
        }

        Vector3 reflectedVelocity = Vector3.Reflect(impactVelocity, hit.normal) * _weaponData.RicochetSpeedRetention;

        if (reflectedVelocity.sqrMagnitude <= MIN_SPEED_SQUARED)
        {
            return false;
        }

        _position = hit.point + hit.normal * Mathf.Max(_weaponData.RicochetSurfaceOffsetMeters, 0.0001f);
        _velocity = reflectedVelocity;
        transform.SetPositionAndRotation(_position, ResolveRotation(_velocity, transform.rotation));
        MoveTracer(_position);

        if (_weaponData.DrawBallisticDebug)
        {
            DrawMarker(hit.point, IMPACT_DEBUG_SIZE, Color.magenta);
            Debug.DrawLine(hit.point, hit.point + _velocity.normalized * 0.25f, Color.magenta, _weaponData.BallisticDebugDurationSeconds);
        }

        return true;
    }

    private void IntegrateRungeKutta4(Vector3 position, Vector3 velocity, float deltaTime, out Vector3 nextPosition, out Vector3 nextVelocity)
    {
        Vector3 positionK1 = velocity;
        Vector3 velocityK1 = CalculateAcceleration(velocity);

        Vector3 positionK2 = velocity + velocityK1 * (deltaTime * 0.5f);
        Vector3 velocityK2 = CalculateAcceleration(positionK2);

        Vector3 positionK3 = velocity + velocityK2 * (deltaTime * 0.5f);
        Vector3 velocityK3 = CalculateAcceleration(positionK3);

        Vector3 positionK4 = velocity + velocityK3 * deltaTime;
        Vector3 velocityK4 = CalculateAcceleration(positionK4);

        nextPosition = position + deltaTime / 6f * (positionK1 + 2f * positionK2 + 2f * positionK3 + positionK4);
        nextVelocity = velocity + deltaTime / 6f * (velocityK1 + 2f * velocityK2 + 2f * velocityK3 + velocityK4);
    }

    private Vector3 CalculateAcceleration(Vector3 velocity)
    {
        Vector3 acceleration = Physics.gravity * _weaponData.BallisticGravityScale;

        if (_weaponData.UseAirResistance == false)
        {
            return acceleration;
        }

        float massKilograms = _ammoData.AmmoBulletMassKilograms;
        float diameterMeters = _ammoData.AmmoBulletDiameterMeters;
        float dragCoefficient = _ammoData.AmmoBulletDragCoefficient;
        float airDensity = _weaponData.AirDensityKilogramsPerCubicMeter;
        float speed = velocity.magnitude;

        if (massKilograms <= 0f || diameterMeters <= 0f || dragCoefficient <= 0f || airDensity <= 0f || speed <= Mathf.Epsilon)
        {
            return acceleration;
        }

        float crossSectionArea = Mathf.PI * diameterMeters * diameterMeters * 0.25f;
        float dragAccelerationFactor = 0.5f * airDensity * dragCoefficient * crossSectionArea / massKilograms;
        return acceleration - velocity * (dragAccelerationFactor * speed);
    }

    private bool TryGetClosestHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit closestHit)
    {
        int hitCount = _weaponData.BallisticCollisionRadiusMeters > 0f
            ? Physics.SphereCastNonAlloc(origin, _weaponData.BallisticCollisionRadiusMeters, direction, _hitBuffer, distance, _weaponData.BallisticHitMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(origin, direction, _hitBuffer, distance, _weaponData.BallisticHitMask, QueryTriggerInteraction.Ignore);

        if (hitCount < HIT_BUFFER_SIZE)
        {
            return TrySelectClosestHit(_hitBuffer, hitCount, out closestHit);
        }

        RaycastHit[] allHits = _weaponData.BallisticCollisionRadiusMeters > 0f
            ? Physics.SphereCastAll(origin, _weaponData.BallisticCollisionRadiusMeters, direction, distance, _weaponData.BallisticHitMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(origin, direction, distance, _weaponData.BallisticHitMask, QueryTriggerInteraction.Ignore);
        return TrySelectClosestHit(allHits, allHits.Length, out closestHit);
    }

    private bool TrySelectClosestHit(RaycastHit[] hits, int hitCount, out RaycastHit closestHit)
    {
        closestHit = default;
        float closestDistance = float.PositiveInfinity;
        bool hasHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null || _ignoredColliders.Contains(hit.collider) || hit.distance >= closestDistance)
            {
                continue;
            }

            closestHit = hit;
            closestDistance = hit.distance;
            hasHit = true;
        }

        return hasHit;
    }

    private void AddIgnoredColliders(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            _ignoredColliders.Add(colliders[i]);
        }
    }

    private void DisableAndIgnoreProjectileColliders()
    {
        Collider[] projectileColliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider projectileCollider = projectileColliders[i];
            _ignoredColliders.Add(projectileCollider);
            projectileCollider.enabled = false;
        }
    }

    private void DisableProjectileRigidbodyPhysics()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].useGravity = false;
            rigidbodies[i].isKinematic = true;
        }
    }

    private void OnDestroy()
    {
        CompleteTracer();
    }

    private void MoveTracer(Vector3 position)
    {
        // The tracer consumes resolved ballistic positions and never participates in collision queries.
        _tracerView?.MoveTo(position, _distanceTravelled, _elapsedLifetime);
    }

    private void CompleteTracer()
    {
        if (_tracerView == null)
        {
            return;
        }

        _tracerView.Complete();
        _tracerView = null;
    }

    private void DrawMarker(Vector3 position, float size, Color color)
    {
        float duration = _weaponData.BallisticDebugDurationSeconds;
        Debug.DrawLine(position - Vector3.right * size, position + Vector3.right * size, color, duration);
        Debug.DrawLine(position - Vector3.up * size, position + Vector3.up * size, color, duration);
        Debug.DrawLine(position - Vector3.forward * size, position + Vector3.forward * size, color, duration);
    }

    private static Quaternion ResolveRotation(Vector3 velocity, Quaternion fallback)
    {
        return velocity.sqrMagnitude <= MIN_SPEED_SQUARED ? fallback : Quaternion.LookRotation(velocity.normalized, Vector3.up);
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsNaN(value.x) == false && float.IsInfinity(value.x) == false &&
               float.IsNaN(value.y) == false && float.IsInfinity(value.y) == false &&
               float.IsNaN(value.z) == false && float.IsInfinity(value.z) == false;
    }
}
