using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public sealed class FirstPersonWeaponModule : MonoBehaviour
{
    private const float DEFAULT_SHADER_HORIZONTAL_OFFSET = 0f;
    private const float DEFAULT_SHADER_VERTICAL_OFFSET = 0f;

    private static readonly int _horizontalOffsetPropertyId = Shader.PropertyToID("_OffsetX");
    private static readonly int _verticalOffsetPropertyId = Shader.PropertyToID("_OffsetY");

    [SerializeField] private ItemData _moduleItemData;
    [SerializeField] private ItemData[] _requiredModules = System.Array.Empty<ItemData>();
    [SerializeField] private ItemData[] _incompatibleModules = System.Array.Empty<ItemData>();
    [SerializeField] private Transform _attachPoint;
    [SerializeField] private GameObject[] _enabledWhenInstalled = System.Array.Empty<GameObject>();
    [SerializeField] private GameObject[] _disabledWhenInstalled = System.Array.Empty<GameObject>();
    [SerializeField] [ShowIf(nameof(IsOpticModule))] private Vector3 _aimRootPositionOffset;
    [SerializeField] [ShowIf(nameof(IsOpticModule))] private Vector3 _aimRootRotationOffset;
    [SerializeField] [ShowIf(nameof(IsOpticModule))] private Material _sightMaterial;
    [SerializeField] [ShowIf(nameof(IsOpticModule))] private float _shaderHorizontalOffset = DEFAULT_SHADER_HORIZONTAL_OFFSET;
    [SerializeField] [ShowIf(nameof(IsOpticModule))] private float _shaderVerticalOffset = DEFAULT_SHADER_VERTICAL_OFFSET;
    [SerializeField] [ShowIf(nameof(IsOpticModule))] private AimRootOffsetOverride[] _aimRootOffsetOverrides = System.Array.Empty<AimRootOffsetOverride>();

    private readonly List<SightMaterialBinding> _sightMaterialBindings = new();
    private MaterialPropertyBlock _sightMaterialPropertyBlock;
    private Material _cachedSightMaterial;

    public ItemData ModuleItemData => _moduleItemData;
    internal IReadOnlyList<ItemData> RequiredModules => _requiredModules;
    internal Transform AttachPoint => _attachPoint;

    public bool CanInstall(IReadOnlyList<ItemData> installedModules)
    {
        if (_moduleItemData == null || _moduleItemData.ItemType != ItemType.Module || Contains(installedModules, _moduleItemData))
        {
            return false;
        }

        return ConfigurationSatisfied(installedModules);
    }

    public bool ConfigurationSatisfied(IReadOnlyList<ItemData> installedModules)
    {
        return RequirementsSatisfied(installedModules) && IncompatibilitiesSatisfied(installedModules);
    }

    public bool RequirementsSatisfied(IReadOnlyList<ItemData> installedModules)
    {
        bool hasRequirement = false;

        int requiredModuleCount = _requiredModules?.Length ?? 0;

        for (int i = 0; i < requiredModuleCount; i++)
        {
            ItemData requiredModule = _requiredModules[i];

            if (requiredModule == null)
            {
                continue;
            }

            hasRequirement = true;

            if (Contains(installedModules, requiredModule))
            {
                return true;
            }
        }

        return hasRequirement == false;
    }

    internal bool IncompatibilitiesSatisfied(IReadOnlyList<ItemData> installedModules)
    {
        int incompatibleModuleCount = _incompatibleModules?.Length ?? 0;

        for (int i = 0; i < incompatibleModuleCount; i++)
        {
            ItemData incompatibleModule = _incompatibleModules[i];

            if (incompatibleModule != null && Contains(installedModules, incompatibleModule))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsIncompatibleWith(ItemData moduleItemData)
    {
        return Contains(_incompatibleModules, moduleItemData);
    }

    internal bool IsInstalled(IReadOnlyList<ItemData> installedModules)
    {
        return Contains(installedModules, _moduleItemData);
    }

    internal void ResolveAimRootOffsets(
        IReadOnlyList<ItemData> installedModules,
        out Vector3 positionOffset,
        out Vector3 rotationOffset)
    {
        positionOffset = _aimRootPositionOffset;
        rotationOffset = _aimRootRotationOffset;
        AimRootOffsetOverride offsetOverride = FindAimRootOffsetOverride(installedModules);

        if (offsetOverride != null)
        {
            positionOffset = offsetOverride.PositionOffset;
            rotationOffset = offsetOverride.RotationOffset;
        }
    }

    internal void ApplySightShaderOffsets(IReadOnlyList<ItemData> installedModules)
    {
        float horizontalOffset = _shaderHorizontalOffset;
        float verticalOffset = _shaderVerticalOffset;
        AimRootOffsetOverride offsetOverride = FindAimRootOffsetOverride(installedModules);

        if (offsetOverride != null)
        {
            horizontalOffset = offsetOverride.ShaderHorizontalOffset;
            verticalOffset = offsetOverride.ShaderVerticalOffset;
        }

        ApplySightShaderOffsets(horizontalOffset, verticalOffset);
    }

    internal void ApplyDefaultVisualState()
    {
        SetObjectsActive(_enabledWhenInstalled, false);
        SetObjectsActive(_disabledWhenInstalled, true);
    }

    internal void ApplyInstalledVisualState(Transform attachPoint)
    {
        SetObjectsActive(_enabledWhenInstalled, true);
        SetObjectsActive(_disabledWhenInstalled, false);

        if (attachPoint == null)
        {
            return;
        }

        for (int i = 0; i < _enabledWhenInstalled.Length; i++)
        {
            GameObject moduleVisual = _enabledWhenInstalled[i];

            if (moduleVisual != null)
            {
                moduleVisual.transform.position = attachPoint.position;
            }
        }
    }

    private static bool Contains(IReadOnlyList<ItemData> modules, ItemData module)
    {
        if (modules == null || module == null)
        {
            return false;
        }

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] == module)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetObjectsActive(IReadOnlyList<GameObject> objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            objects[i]?.SetActive(active);
        }
    }

    private AimRootOffsetOverride FindAimRootOffsetOverride(IReadOnlyList<ItemData> installedModules)
    {
        int overrideCount = _aimRootOffsetOverrides?.Length ?? 0;

        for (int i = 0; i < overrideCount; i++)
        {
            AimRootOffsetOverride offsetOverride = _aimRootOffsetOverrides[i];

            if (offsetOverride != null && offsetOverride.Matches(installedModules))
            {
                return offsetOverride;
            }
        }

        return null;
    }

    private void ApplySightShaderOffsets(float horizontalOffset, float verticalOffset)
    {
        if (_sightMaterial == null ||
            _sightMaterial.HasProperty(_horizontalOffsetPropertyId) == false ||
            _sightMaterial.HasProperty(_verticalOffsetPropertyId) == false)
        {
            return;
        }

        EnsureSightMaterialBindings();
        _sightMaterialPropertyBlock ??= new MaterialPropertyBlock();

        for (int i = 0; i < _sightMaterialBindings.Count; i++)
        {
            SightMaterialBinding binding = _sightMaterialBindings[i];

            if (binding.Renderer == null)
            {
                continue;
            }

            _sightMaterialPropertyBlock.Clear();
            binding.Renderer.GetPropertyBlock(_sightMaterialPropertyBlock, binding.MaterialIndex);
            _sightMaterialPropertyBlock.SetFloat(_horizontalOffsetPropertyId, horizontalOffset);
            _sightMaterialPropertyBlock.SetFloat(_verticalOffsetPropertyId, verticalOffset);
            binding.Renderer.SetPropertyBlock(_sightMaterialPropertyBlock, binding.MaterialIndex);
        }
    }

    private void EnsureSightMaterialBindings()
    {
        if (_cachedSightMaterial == _sightMaterial)
        {
            return;
        }

        _cachedSightMaterial = _sightMaterial;
        _sightMaterialBindings.Clear();

        int visualCount = _enabledWhenInstalled?.Length ?? 0;

        for (int visualIndex = 0; visualIndex < visualCount; visualIndex++)
        {
            GameObject moduleVisual = _enabledWhenInstalled[visualIndex];

            if (moduleVisual == null)
            {
                continue;
            }

            Renderer[] renderers = moduleVisual.GetComponentsInChildren<Renderer>(true);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                AddSightMaterialBindings(renderers[rendererIndex]);
            }
        }
    }

    private void AddSightMaterialBindings(Renderer targetRenderer)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Material[] sharedMaterials = targetRenderer.sharedMaterials;

        for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
        {
            if (sharedMaterials[materialIndex] != _sightMaterial || ContainsSightMaterialBinding(targetRenderer, materialIndex))
            {
                continue;
            }

            _sightMaterialBindings.Add(new SightMaterialBinding(targetRenderer, materialIndex));
        }
    }

    private bool ContainsSightMaterialBinding(Renderer targetRenderer, int materialIndex)
    {
        for (int i = 0; i < _sightMaterialBindings.Count; i++)
        {
            SightMaterialBinding binding = _sightMaterialBindings[i];

            if (binding.Renderer == targetRenderer && binding.MaterialIndex == materialIndex)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOpticModule() => _moduleItemData != null && _moduleItemData.ModuleSlot == WeaponModuleSlot.Optic;

    private void OnValidate()
    {
        _cachedSightMaterial = null;
        _sightMaterialBindings.Clear();
    }

    [System.Serializable]
    private sealed class AimRootOffsetOverride
    {
        [SerializeField] private ItemData _requiredModule;
        [SerializeField] private Vector3 _positionOffset;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField] private float _shaderHorizontalOffset = DEFAULT_SHADER_HORIZONTAL_OFFSET;
        [SerializeField] private float _shaderVerticalOffset = DEFAULT_SHADER_VERTICAL_OFFSET;

        public Vector3 PositionOffset => _positionOffset;
        public Vector3 RotationOffset => _rotationOffset;
        public float ShaderHorizontalOffset => _shaderHorizontalOffset;
        public float ShaderVerticalOffset => _shaderVerticalOffset;

        public bool Matches(IReadOnlyList<ItemData> installedModules)
        {
            return Contains(installedModules, _requiredModule);
        }
    }

    private readonly struct SightMaterialBinding
    {
        public SightMaterialBinding(Renderer renderer, int materialIndex)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
        }

        public Renderer Renderer { get; }
        public int MaterialIndex { get; }
    }
}
