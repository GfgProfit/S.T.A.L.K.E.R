using System;
using UnityEngine;

public sealed class WeaponShellEjector : MonoBehaviour
{
    [SerializeField] private GameObject _shellPrefab;
    [SerializeField] private Vector3 _shellSpawnScale = Vector3.one;
    [SerializeField] private Vector3 _minimumLocalEjectionVelocity = new(1.5f, 1.25f, -0.35f);
    [SerializeField] private Vector3 _maximumLocalEjectionVelocity = new(2.25f, 2f, 0.35f);
    [SerializeField] private Vector3 _minimumLocalAngularVelocity = new(-30f, -30f, -30f);
    [SerializeField] private Vector3 _maximumLocalAngularVelocity = new(30f, 30f, 30f);
    [SerializeField] [Min(0.1f)] private float _shellLifetimeSeconds = 12f;
    [SerializeField] [Min(0f)] private float _maximumAngularVelocity = 50f;
    [SerializeField] private bool _inheritOwnerVelocity = true;
    [SerializeField] private bool _ignoreOwnerCollisions = true;

    private CharacterController _ownerCharacterController;
    private Collider[] _ownerColliders = Array.Empty<Collider>();
    private bool _configurationWarningLogged;

    private void Awake()
    {
        CacheOwnerPhysics();
    }

    public void Eject(Material shellMaterial)
    {
        if (_shellPrefab == null)
        {
            LogConfigurationWarning("Shell prefab is not assigned.");
            return;
        }

        GameObject shellObject = Instantiate(_shellPrefab, transform.position, transform.rotation);
        shellObject.transform.localScale = _shellSpawnScale;
        ApplyShellMaterial(shellObject, shellMaterial);
        Rigidbody shellRigidbody = shellObject.GetComponent<Rigidbody>();

        if (shellRigidbody == null)
        {
            shellRigidbody = shellObject.GetComponentInChildren<Rigidbody>();
        }

        if (shellRigidbody == null)
        {
            LogConfigurationWarning($"{_shellPrefab.name} requires a Rigidbody.");
            Destroy(shellObject);
            return;
        }

        Vector3 ejectionVelocity = transform.TransformDirection(GetRandomVector(_minimumLocalEjectionVelocity, _maximumLocalEjectionVelocity));

        if (_inheritOwnerVelocity && _ownerCharacterController != null)
        {
            ejectionVelocity += GetOwnerVelocity();
        }

        shellRigidbody.maxAngularVelocity = Mathf.Max(0f, _maximumAngularVelocity);
        shellRigidbody.AddForce(ejectionVelocity, ForceMode.VelocityChange);
        shellRigidbody.AddTorque(
            transform.TransformDirection(GetRandomVector(_minimumLocalAngularVelocity, _maximumLocalAngularVelocity)),
            ForceMode.VelocityChange);

        if (_ignoreOwnerCollisions)
        {
            IgnoreOwnerCollisions(shellObject);
        }

        Destroy(shellObject, Mathf.Max(0.1f, _shellLifetimeSeconds));
    }

    private static void ApplyShellMaterial(GameObject shellObject, Material shellMaterial)
    {
        if (shellMaterial == null)
        {
            return;
        }

        Renderer[] shellRenderers = shellObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < shellRenderers.Length; i++)
        {
            shellRenderers[i].sharedMaterial = shellMaterial;
        }
    }

    private void CacheOwnerPhysics()
    {
        PlayerController owner = GetComponentInParent<PlayerController>();

        if (owner == null)
        {
            _ownerCharacterController = null;
            _ownerColliders = Array.Empty<Collider>();
            return;
        }

        _ownerCharacterController = owner.GetComponent<CharacterController>();
        _ownerColliders = owner.GetComponentsInChildren<Collider>(true);
    }

    private Vector3 GetOwnerVelocity()
    {
        Vector3 ownerVelocity = _ownerCharacterController.velocity;

        if (_ownerCharacterController.isGrounded && ownerVelocity.y < 0f)
        {
            ownerVelocity.y = 0f;
        }

        return ownerVelocity;
    }

    private void IgnoreOwnerCollisions(GameObject shellObject)
    {
        if (_ownerColliders.Length == 0)
        {
            CacheOwnerPhysics();
        }

        Collider[] shellColliders = shellObject.GetComponentsInChildren<Collider>(true);

        for (int shellIndex = 0; shellIndex < shellColliders.Length; shellIndex++)
        {
            Collider shellCollider = shellColliders[shellIndex];

            for (int ownerIndex = 0; ownerIndex < _ownerColliders.Length; ownerIndex++)
            {
                Collider ownerCollider = _ownerColliders[ownerIndex];

                if (shellCollider != null && ownerCollider != null)
                {
                    Physics.IgnoreCollision(shellCollider, ownerCollider);
                }
            }
        }
    }

    private static Vector3 GetRandomVector(Vector3 minimum, Vector3 maximum)
    {
        return new Vector3(
            UnityEngine.Random.Range(Mathf.Min(minimum.x, maximum.x), Mathf.Max(minimum.x, maximum.x)),
            UnityEngine.Random.Range(Mathf.Min(minimum.y, maximum.y), Mathf.Max(minimum.y, maximum.y)),
            UnityEngine.Random.Range(Mathf.Min(minimum.z, maximum.z), Mathf.Max(minimum.z, maximum.z)));
    }

    private void LogConfigurationWarning(string message)
    {
        if (_configurationWarningLogged)
        {
            return;
        }

        Debug.LogWarning($"[{nameof(WeaponShellEjector)}] {message}", this);
        _configurationWarningLogged = true;
    }

    private void OnValidate()
    {
        _shellLifetimeSeconds = Mathf.Max(0.1f, _shellLifetimeSeconds);
        _maximumAngularVelocity = Mathf.Max(0f, _maximumAngularVelocity);
    }
}
