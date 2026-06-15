using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal static class ItemIconRenderer
{
    public static Texture2D RenderPreviewTexture(ItemData itemData, ItemIconGeneratorSettings settings = null)
    {
        if (itemData == null || itemData.HasRuntimeIconSource() == false)
        {
            return null;
        }

        return RenderIconTexture(itemData, null, settings ?? ItemIconGeneratorSettings.LoadDefault(), IconRenderProfile.CreateDefault(itemData));
    }

    public static Sprite RenderIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, out Texture2D texture)
    {
        texture = RenderIconTexture(itemData, runtimeIconParts, settings, IconRenderProfile.CreateDefault(itemData));

        if (texture == null)
        {
            return null;
        }

        return ItemIconTextureProcessor.CreateSprite(itemData, texture);
    }

    public static Sprite RenderIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile, out Texture2D texture)
    {
        texture = RenderIconTexture(itemData, runtimeIconParts, settings, renderProfile);

        if (texture == null)
        {
            return null;
        }

        return ItemIconTextureProcessor.CreateSprite(itemData, texture);
    }

    public static RawIconRenderResult RenderRawIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        GameObject rootObject = null;
        GameObject cameraObject = null;
        List<GameObject> lightObjects = null;
        GameObject volumeObject = null;
        VolumeProfile volumeProfile = null;
        RenderTexture renderTexture = null;
        RenderTexture previousActiveTexture = RenderTexture.active;
        Light[] sceneLights = null;
        int[] sceneLightCullingMasks = null;

        try
        {
            if (settings.ExcludeIconLayerFromSceneLights)
            {
                sceneLightCullingMasks = ItemIconSceneLightIsolation.ExcludeIconLayerFromSceneLights(settings, out sceneLights);
            }

            rootObject = new($"Runtime Icon Root - {itemData.name}")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            rootObject.transform.SetPositionAndRotation(settings.RenderOrigin, Quaternion.Euler(renderProfile.ModelEulerAngles));
            rootObject.transform.localScale = renderProfile.ModelScale;

            bool hasRenderablePart = ItemIconSceneBuilder.InstantiateSource(itemData.IconPrefab, rootObject.transform, null);
            hasRenderablePart |= ItemIconSceneBuilder.InstantiateParts(itemData.IconParts, rootObject.transform);
            hasRenderablePart |= ItemIconSceneBuilder.InstantiateParts(runtimeIconParts, rootObject.transform);

            if (hasRenderablePart == false || ItemIconSceneBuilder.TryCalculateBounds(rootObject, out Bounds bounds) == false)
            {
                return null;
            }

            ItemIconSceneBuilder.SetLayerRecursively(rootObject, settings.RenderLayer);
            ItemIconSceneBuilder.SetRendererIsolation(rootObject, settings);

            int textureWidth = renderProfile.TextureWidth;
            int textureHeight = renderProfile.TextureHeight;

            renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 24, settings.RenderTextureFormat, RenderTextureReadWrite.Default, itemData.IconAntiAliasing);
            renderTexture.filterMode = FilterMode.Bilinear;

            volumeObject = ItemIconVolumeFactory.Create(itemData.name, settings, out volumeProfile);

            cameraObject = new($"Runtime Icon Camera - {itemData.name}", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Camera renderCamera = cameraObject.GetComponent<Camera>();

            HDAdditionalCameraData hdCameraData = renderCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            ItemIconCameraConfigurator.ConfigureHdCamera(hdCameraData, settings);
            ItemIconCameraConfigurator.ConfigureRenderCamera(renderCamera, bounds, renderTexture, settings, renderProfile, itemData.IconAntiAliasing);

            if (renderProfile.UseDirectionalLight)
            {
                lightObjects = ItemIconLightFactory.Create(itemData, renderCamera.transform, settings, renderProfile);
            }

            RenderCamera(renderCamera, settings);

            RenderTexture.active = renderTexture;

            Texture2D texture = new(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                name = $"{itemData.name} Runtime Icon Texture"
            };

            texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            texture.Apply(false, false);

            Color32[] itemPixels = texture.GetPixels32();

            if (ItemIconTextureProcessor.HasVisiblePixels(itemPixels) == false)
            {
                DestroyObject(texture);
                return null;
            }

            return new RawIconRenderResult(texture, itemPixels, textureWidth, textureHeight);
        }
        finally
        {
            RenderTexture.active = previousActiveTexture;
            ItemIconSceneLightIsolation.RestoreSceneLightCullingMasks(sceneLights, sceneLightCullingMasks);

            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            DestroyObjects(lightObjects);
            DestroyObject(cameraObject);
            DestroyObject(volumeObject);
            DestroyObject(volumeProfile);
            DestroyObject(rootObject);
        }
    }

    public static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    private static Texture2D RenderIconTexture(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        RawIconRenderResult rawResult = RenderRawIcon(itemData, runtimeIconParts, settings, renderProfile);
        if (rawResult == null)
        {
            return null;
        }

        try
        {
            Color32[] pixels = ItemIconTextureProcessor.CreateProcessedPixels(rawResult.ItemPixels, rawResult.Width, rawResult.Height, ItemIconTextureProcessor.CreatePostProcessSettings(itemData));

            rawResult.Texture.SetPixels32(pixels);
            rawResult.Texture.Apply(false, false);

            return rawResult.Texture;
        }
        catch
        {
            DestroyObject(rawResult.Texture);
            throw;
        }
    }

    private static void RenderCamera(Camera renderCamera, ItemIconGeneratorSettings settings)
    {
        if (RequiresExposureWarmup(settings))
        {
            renderCamera.Render();
        }

        renderCamera.Render();
    }

    private static bool RequiresExposureWarmup(ItemIconGeneratorSettings settings) => settings.EnableExposureControl && (settings.ExposureMode == ExposureMode.Automatic || settings.ExposureMode == ExposureMode.AutomaticHistogram || settings.ExposureMode == ExposureMode.CurveMapping);

    private static void DestroyObjects(IReadOnlyList<GameObject> targets)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            DestroyObject(targets[i]);
        }
    }
}