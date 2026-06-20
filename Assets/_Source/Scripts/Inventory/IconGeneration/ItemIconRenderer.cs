using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal static class ItemIconRenderer
{
    public static ItemIconRenderSession CreateSession(ItemIconGeneratorSettings settings)
    {
        return new ItemIconRenderSession(settings ?? ItemIconGeneratorSettings.LoadDefault());
    }

    public static async UniTask<Texture2D> RenderPreviewTextureAsync(
        ItemData itemData,
        ItemIconGeneratorSettings settings = null,
        CancellationToken cancellationToken = default)
    {
        if (itemData == null || itemData.HasRuntimeIconSource() == false)
        {
            return null;
        }

        await UniTask.SwitchToMainThread(cancellationToken);

        ItemIconGeneratorSettings resolvedSettings = settings ?? ItemIconGeneratorSettings.LoadDefault();
        using ItemIconRenderSession renderSession = CreateSession(resolvedSettings);

        return await RenderIconTextureAsync(
            itemData,
            Array.Empty<ItemData>(),
            IconRenderProfile.CreateDefault(itemData),
            renderSession,
            cancellationToken);
    }

    public static async UniTask<Texture2D> RenderIconTextureAsync(
        ItemData itemData,
        IReadOnlyList<ItemData> installedModules,
        IconRenderProfile renderProfile,
        ItemIconRenderSession renderSession,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await UniTask.SwitchToMainThread(cancellationToken);

        RawIconRenderResult rawResult = await renderSession.RenderRawIconAsync(itemData, installedModules, renderProfile, cancellationToken);

        if (rawResult == null)
        {
            return null;
        }

        try
        {
            ItemIconPostProcessSettings postProcessSettings = ItemIconTextureProcessor.CreatePostProcessSettings(itemData);
            Color32[] processedPixels = await UniTask.RunOnThreadPool(
                () => ItemIconTextureProcessor.CreateProcessedPixels(rawResult.ItemPixels, rawResult.Width, rawResult.Height, postProcessSettings),
                true,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            rawResult.Texture.SetPixels32(processedPixels);
            rawResult.Texture.Apply(false, true);
            return rawResult.Texture;
        }
        catch
        {
            await UniTask.SwitchToMainThread();
            DestroyObject(rawResult.Texture);
            throw;
        }
    }

    public static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (target is GameObject gameObject)
        {
            gameObject.SetActive(false);
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}

internal sealed class ItemIconRenderSession : IDisposable
{
    private readonly ItemIconGeneratorSettings _settings;
    private readonly GameObject _cameraObject;
    private readonly Camera _renderCamera;
    private readonly GameObject _volumeObject;
    private readonly VolumeProfile _volumeProfile;
    private readonly Light[] _lights;
    private readonly ItemIconSceneIsolationScope _sceneIsolation;

    private ItemData _currentItemData;
    private GameObject _rootObject;
    private FirstPersonWeaponModule[] _moduleDefinitions = Array.Empty<FirstPersonWeaponModule>();
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private readonly List<ItemData> _effectiveModules = new();
    private bool _requiresExposureWarmup;
    private bool _disposed;

    public ItemIconRenderSession(ItemIconGeneratorSettings settings)
    {
        _settings = settings;
        _sceneIsolation = new ItemIconSceneIsolationScope();

        _volumeObject = ItemIconVolumeFactory.Create("Shared Runtime Item Icons", settings, out _volumeProfile);
        _cameraObject = new GameObject("Shared Runtime Item Icon Camera", typeof(Camera))
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        _renderCamera = _cameraObject.GetComponent<Camera>();
        HDAdditionalCameraData hdCameraData = _cameraObject.AddComponent<HDAdditionalCameraData>();
        ItemIconCameraConfigurator.ConfigureHdCamera(hdCameraData, settings);
        _lights = ItemIconLightFactory.CreateReusable(settings);
    }

    public async UniTask<RawIconRenderResult> RenderRawIconAsync(ItemData itemData, IReadOnlyList<ItemData> installedModules, IconRenderProfile renderProfile, CancellationToken cancellationToken)
    {
        if (_disposed || itemData == null || itemData.HasRuntimeIconSource() == false)
        {
            return null;
        }

        EnsureItemVisual(itemData);

        if (_rootObject == null)
        {
            return null;
        }

        _rootObject.transform.SetPositionAndRotation(_settings.RenderOrigin, Quaternion.Euler(renderProfile.ModelEulerAngles));
        _rootObject.transform.localScale = renderProfile.ModelScale;
        BuildEffectiveModules(itemData, installedModules);
        WeaponModuleSupport.ApplyToVisual(_moduleDefinitions, _effectiveModules);

        if (ItemIconSceneBuilder.TryCalculateBounds(_renderers, _rootObject.transform.position, out Bounds bounds) == false)
        {
            return null;
        }

        RenderTexture renderTexture = null;
        RenderTexture readbackTexture = null;
        bool sceneEnvironmentIsolated = false;
        bool readbackRequested = false;
        AsyncGPUReadbackRequest readbackRequest = default;

        try
        {
            int textureWidth = renderProfile.TextureWidth;
            int textureHeight = renderProfile.TextureHeight;

            renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 24, _settings.RenderTextureFormat, RenderTextureReadWrite.Default, itemData.IconAntiAliasing);
            renderTexture.filterMode = FilterMode.Bilinear;

            if (_requiresExposureWarmup)
            {
                RenderExposureWarmupFrame(itemData, bounds, renderTexture, renderProfile);
                _requiresExposureWarmup = false;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            try
            {
                _sceneIsolation.Apply(_settings.ExcludeIconLayerFromSceneLights);
                sceneEnvironmentIsolated = true;

                ItemIconCameraConfigurator.ConfigureRenderCamera(_renderCamera, bounds, renderTexture, _settings, renderProfile, itemData.IconAntiAliasing);

                if (renderProfile.UseDirectionalLight)
                {
                    ItemIconLightFactory.Configure(_lights, itemData, _renderCamera.transform, _settings, renderProfile);
                }

                RenderCamera();
                readbackTexture = PrepareReadbackTexture(renderTexture, textureWidth, textureHeight);
                readbackRequest = AsyncGPUReadback.Request(readbackTexture, 0, TextureFormat.RGBA32);
                readbackRequested = true;
            }
            finally
            {
                _renderCamera.targetTexture = null;

                if (sceneEnvironmentIsolated)
                {
                    _sceneIsolation.Restore();
                    sceneEnvironmentIsolated = false;
                }

                ItemIconLightFactory.Disable(_lights);
            }

            if (readbackRequested == false)
            {
                return null;
            }

            while (readbackRequest.done == false)
            {
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (readbackRequest.hasError)
            {
                Debug.LogError($"Asynchronous GPU readback failed while generating the icon for '{itemData.name}'.", itemData);
                return null;
            }

            NativeArray<Color32> readbackData = readbackRequest.GetData<Color32>();
            Color32[] itemPixels = readbackData.ToArray();

            if (ItemIconTextureProcessor.HasVisiblePixels(itemPixels) == false)
            {
                return null;
            }

            Texture2D texture = new(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                name = $"{itemData.name} Runtime Icon Texture"
            };

            texture.SetPixels32(itemPixels);
            texture.Apply(false, false);

            return new RawIconRenderResult(texture, itemPixels, textureWidth, textureHeight);
        }
        finally
        {
            if (sceneEnvironmentIsolated)
            {
                _sceneIsolation.Restore();
            }

            if (renderTexture != null)
            {
                _renderCamera.targetTexture = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            if (readbackTexture != null && readbackTexture != renderTexture)
            {
                RenderTexture.ReleaseTemporary(readbackTexture);
            }

            ItemIconLightFactory.Disable(_lights);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseCurrentItemVisual();

        for (int i = 0; i < _lights.Length; i++)
        {
            if (_lights[i] != null)
            {
                ItemIconRenderer.DestroyObject(_lights[i].gameObject);
            }
        }

        ItemIconRenderer.DestroyObject(_cameraObject);
        ItemIconRenderer.DestroyObject(_volumeObject);
        ItemIconRenderer.DestroyObject(_volumeProfile);
    }

    private void EnsureItemVisual(ItemData itemData)
    {
        if (_currentItemData == itemData && _rootObject != null)
        {
            return;
        }

        ReleaseCurrentItemVisual();

        _currentItemData = itemData;
        _requiresExposureWarmup = RequiresExposureWarmup();
        _rootObject = new GameObject($"Runtime Icon Root - {itemData.name}")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        GameObject iconInstance = ItemIconSceneBuilder.InstantiateSource(itemData.IconPrefab, _rootObject.transform);

        if (iconInstance == null)
        {
            ReleaseCurrentItemVisual();
            return;
        }

        _moduleDefinitions = iconInstance.GetComponentsInChildren<FirstPersonWeaponModule>(true);
        _renderers = _rootObject.GetComponentsInChildren<Renderer>(true);
        ItemIconSceneBuilder.SetLayerRecursively(_rootObject, _settings.RenderLayer);
        ItemIconSceneBuilder.SetRendererIsolation(_renderers, _settings);
    }

    private void ReleaseCurrentItemVisual()
    {
        _currentItemData = null;
        _moduleDefinitions = Array.Empty<FirstPersonWeaponModule>();
        _renderers = Array.Empty<Renderer>();

        if (_rootObject == null)
        {
            return;
        }

        ItemIconRenderer.DestroyObject(_rootObject);
        _rootObject = null;
    }

    private void BuildEffectiveModules(ItemData itemData, IReadOnlyList<ItemData> installedModules)
    {
        _effectiveModules.Clear();
        AddUniqueModules(itemData.DefaultIconModules);
        AddUniqueModules(installedModules);
    }

    private void AddUniqueModules(IReadOnlyList<ItemData> modules)
    {
        if (modules == null)
        {
            return;
        }

        for (int i = 0; i < modules.Count; i++)
        {
            ItemData module = modules[i];

            if (module != null && module.ItemType == ItemType.Module && _effectiveModules.Contains(module) == false)
            {
                _effectiveModules.Add(module);
            }
        }
    }

    private void RenderExposureWarmupFrame(ItemData itemData, Bounds bounds, RenderTexture renderTexture, IconRenderProfile renderProfile)
    {
        bool sceneEnvironmentIsolated = false;

        try
        {
            _sceneIsolation.Apply(_settings.ExcludeIconLayerFromSceneLights);
            sceneEnvironmentIsolated = true;
            ItemIconCameraConfigurator.ConfigureRenderCamera(_renderCamera, bounds, renderTexture, _settings, renderProfile, itemData.IconAntiAliasing);

            if (renderProfile.UseDirectionalLight)
            {
                ItemIconLightFactory.Configure(_lights, itemData, _renderCamera.transform, _settings, renderProfile);
            }

            _renderCamera.Render();
        }
        finally
        {
            _renderCamera.targetTexture = null;

            if (sceneEnvironmentIsolated)
            {
                _sceneIsolation.Restore();
            }

            ItemIconLightFactory.Disable(_lights);
        }
    }

    private RenderTexture PrepareReadbackTexture(RenderTexture renderTexture, int width, int height)
    {
        if (renderTexture.antiAliasing <= 1)
        {
            return renderTexture;
        }

        RenderTexture resolvedTexture = RenderTexture.GetTemporary(width, height, 0, _settings.RenderTextureFormat, RenderTextureReadWrite.Default, 1);
        resolvedTexture.filterMode = FilterMode.Bilinear;
        Graphics.Blit(renderTexture, resolvedTexture);
        return resolvedTexture;
    }

    private void RenderCamera()
    {
        _renderCamera.Render();
    }

    private bool RequiresExposureWarmup()
    {
        return _settings.EnableExposureControl &&
               (_settings.ExposureMode == ExposureMode.Automatic ||
                _settings.ExposureMode == ExposureMode.AutomaticHistogram ||
                _settings.ExposureMode == ExposureMode.CurveMapping);
    }

}
