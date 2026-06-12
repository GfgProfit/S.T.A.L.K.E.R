using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class ItemIconCache
{
    private const byte VisibleAlphaThreshold = 8;

    private static Dictionary<IconCacheKey, IconCacheEntry> cache = new Dictionary<IconCacheKey, IconCacheEntry>();

    public static Sprite GetOrCreate(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        if (itemData == null)
        {
            return null;
        }

        if (itemData.HasRuntimeIconSource(runtimeIconParts) == false)
        {
            return itemData.FallbackIcon;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconCacheKey key = BuildCacheKey(itemData, runtimeIconParts, settings);

        if (TryGetCachedSprite(key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite generatedSprite = RenderIcon(itemData, runtimeIconParts, settings, out Texture2D generatedTexture);
        if (generatedSprite == null)
        {
            DestroyObject(generatedTexture);
            CacheFallbackSprite(key, itemData);
            return itemData.FallbackIcon;
        }

        cache[key] = new IconCacheEntry(generatedSprite, generatedTexture);
        return generatedSprite;
    }

    public static Sprite GetOrCreateSlotIcon(ItemData itemData, int slotWidth, int slotHeight, IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        if (itemData == null)
        {
            return null;
        }

        if (itemData.HasRuntimeIconSource(runtimeIconParts) == false)
        {
            return itemData.FallbackIcon;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateSlot(itemData, slotWidth, slotHeight);
        IconCacheKey key = BuildCacheKey(itemData, runtimeIconParts, settings, renderProfile);

        if (TryGetCachedSprite(key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite generatedSprite = RenderIcon(itemData, runtimeIconParts, settings, renderProfile, out Texture2D generatedTexture);
        if (generatedSprite == null)
        {
            DestroyObject(generatedTexture);
            CacheFallbackSprite(key, itemData);
            return itemData.FallbackIcon;
        }

        cache[key] = new IconCacheEntry(generatedSprite, generatedTexture);
        return generatedSprite;
    }

    public static IEnumerator PrewarmCoroutine(IReadOnlyList<ItemData> itemDataList, Action<int, int, ItemData> progressCallback = null)
    {
        if (itemDataList == null)
        {
            yield break;
        }

        List<ItemData> uniqueItems = BuildUniqueItemList(itemDataList);
        int total = uniqueItems.Count;

        for (int i = 0; i < total; i++)
        {
            ItemData itemData = uniqueItems[i];

            if (itemData != null && itemData.HasRuntimeIconSource())
            {
                yield return GetOrCreateCoroutine(itemData, null, null);
            }

            progressCallback?.Invoke(i + 1, total, itemData);
            yield return null;
        }
    }

    public static void Clear()
    {
        foreach (IconCacheEntry entry in cache.Values)
        {
            if (entry == null)
            {
                continue;
            }

            DestroyObject(entry.Sprite);
            DestroyObject(entry.Texture);
        }

        cache.Clear();
    }

    private static IEnumerator GetOrCreateCoroutine(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, Action<Sprite> completedCallback)
    {
        if (itemData == null)
        {
            completedCallback?.Invoke(null);
            yield break;
        }

        if (itemData.HasRuntimeIconSource(runtimeIconParts) == false)
        {
            completedCallback?.Invoke(itemData.FallbackIcon);
            yield break;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconCacheKey key = BuildCacheKey(itemData, runtimeIconParts, settings);
        if (TryGetCachedSprite(key, out Sprite cachedSprite))
        {
            completedCallback?.Invoke(cachedSprite);
            yield break;
        }

        RawIconRenderResult rawResult = RenderRawIcon(itemData, runtimeIconParts, settings, IconRenderProfile.CreateDefault(itemData));
        if (rawResult == null)
        {
            CacheFallbackSprite(key, itemData);
            completedCallback?.Invoke(itemData.FallbackIcon);
            yield break;
        }

        IconPostProcessSettings postProcessSettings = CreatePostProcessSettings(itemData);
        Task<Color32[]> postProcessTask = Task.Run(() => CreateProcessedPixels(rawResult.ItemPixels, rawResult.Width, rawResult.Height, postProcessSettings));

        while (postProcessTask.IsCompleted == false)
        {
            yield return null;
        }

        if (postProcessTask.IsFaulted || postProcessTask.IsCanceled)
        {
            Exception exception = postProcessTask.Exception;
            Debug.LogException(exception ?? new InvalidOperationException("Runtime item icon post-processing was canceled."));
            DestroyObject(rawResult.Texture);
            completedCallback?.Invoke(itemData.FallbackIcon);
            yield break;
        }

        rawResult.Texture.SetPixels32(postProcessTask.Result);
        rawResult.Texture.Apply(false, false);

        Sprite generatedSprite = CreateSprite(itemData, rawResult.Texture);
        cache[key] = new IconCacheEntry(generatedSprite, rawResult.Texture);
        completedCallback?.Invoke(generatedSprite);
    }

    private static List<ItemData> BuildUniqueItemList(IReadOnlyList<ItemData> itemDataList)
    {
        List<ItemData> uniqueItems = new List<ItemData>();
        HashSet<int> seenIds = new HashSet<int>();

        for (int i = 0; i < itemDataList.Count; i++)
        {
            ItemData itemData = itemDataList[i];
            if (itemData == null)
            {
                continue;
            }

            if (seenIds.Add(itemData.GetInstanceID()))
            {
                uniqueItems.Add(itemData);
            }
        }

        return uniqueItems;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Clear();
        cache = new Dictionary<IconCacheKey, IconCacheEntry>();
    }

    public static Texture2D RenderPreviewTexture(ItemData itemData, ItemIconGeneratorSettings settings = null)
    {
        if (itemData == null || itemData.HasRuntimeIconSource() == false)
        {
            return null;
        }

        return RenderIconTexture(itemData, null, settings ?? ItemIconGeneratorSettings.LoadDefault(), IconRenderProfile.CreateDefault(itemData));
    }

    private static Sprite RenderIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, out Texture2D texture)
    {
        texture = RenderIconTexture(itemData, runtimeIconParts, settings, IconRenderProfile.CreateDefault(itemData));
        if (texture == null)
        {
            return null;
        }

        return CreateSprite(itemData, texture);
    }

    private static Sprite RenderIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile, out Texture2D texture)
    {
        texture = RenderIconTexture(itemData, runtimeIconParts, settings, renderProfile);
        if (texture == null)
        {
            return null;
        }

        return CreateSprite(itemData, texture);
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
            Color32[] pixels = CreateProcessedPixels(
                rawResult.ItemPixels,
                rawResult.Width,
                rawResult.Height,
                CreatePostProcessSettings(itemData));

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

    private static RawIconRenderResult RenderRawIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        GameObject rootObject = null;
        GameObject cameraObject = null;
        List<GameObject> lightObjects = null;
        GameObject volumeObject = null;
        VolumeProfile volumeProfile = null;
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        RenderTexture previousActiveTexture = RenderTexture.active;
        Light[] sceneLights = null;
        int[] sceneLightCullingMasks = null;

        try
        {
            if (settings.ExcludeIconLayerFromSceneLights)
            {
                sceneLightCullingMasks = ExcludeIconLayerFromSceneLights(settings, out sceneLights);
            }

            rootObject = new GameObject($"Runtime Icon Root - {itemData.name}");
            rootObject.hideFlags = HideFlags.HideAndDontSave;
            rootObject.transform.position = settings.RenderOrigin;
            rootObject.transform.rotation = Quaternion.Euler(renderProfile.ModelEulerAngles);
            rootObject.transform.localScale = renderProfile.ModelScale;

            bool hasRenderablePart = InstantiateSource(itemData.IconPrefab, rootObject.transform, null);
            hasRenderablePart |= InstantiateParts(itemData.IconParts, rootObject.transform);
            hasRenderablePart |= InstantiateParts(runtimeIconParts, rootObject.transform);

            if (hasRenderablePart == false || TryCalculateBounds(rootObject, out Bounds bounds) == false)
            {
                return null;
            }

            SetLayerRecursively(rootObject, settings.RenderLayer);
            SetRendererIsolation(rootObject, settings);

            int textureWidth = renderProfile.TextureWidth;
            int textureHeight = renderProfile.TextureHeight;

            renderTexture = RenderTexture.GetTemporary(
                textureWidth,
                textureHeight,
                24,
                settings.RenderTextureFormat,
                RenderTextureReadWrite.Default,
                itemData.IconAntiAliasing);
            renderTexture.filterMode = FilterMode.Bilinear;

            volumeObject = CreateIsolatedVolume(itemData.name, settings, out volumeProfile);

            cameraObject = new GameObject($"Runtime Icon Camera - {itemData.name}", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;

            Camera renderCamera = cameraObject.GetComponent<Camera>();

            HDAdditionalCameraData hdCameraData = renderCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            ConfigureIsolatedCamera(hdCameraData, settings);

            Quaternion cameraRotation = Quaternion.Euler(renderProfile.CameraEulerAngles);

            float boundsRadius = Mathf.Max(0.1f, bounds.extents.magnitude);

            renderCamera.enabled = false;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = settings.RenderLayerMask;
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = CalculateOrthographicSize(bounds, cameraRotation, (float)textureWidth / textureHeight, renderProfile.Padding);
            renderCamera.nearClipPlane = settings.NearClipPlane;
            renderCamera.farClipPlane = boundsRadius * settings.FarClipBoundsMultiplier + settings.FarClipOffset;
            renderCamera.targetTexture = renderTexture;
            renderCamera.transform.rotation = cameraRotation;
            renderCamera.transform.position = bounds.center - renderCamera.transform.forward * (boundsRadius * settings.CameraDistanceMultiplier + settings.CameraDistanceOffset);
            renderCamera.allowHDR = settings.AllowHdr;
            renderCamera.allowMSAA = settings.UseCameraMsaa && itemData.IconAntiAliasing > 1;

            if (renderProfile.UseDirectionalLight)
            {
                lightObjects = CreateIconLights(itemData, renderCamera.transform, settings, renderProfile);
            }

            renderCamera.Render();

            RenderTexture.active = renderTexture;
            texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.name = $"{itemData.name} Runtime Icon Texture";
            texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            texture.Apply(false, false);

            Color32[] itemPixels = texture.GetPixels32();

            if (HasVisiblePixels(itemPixels) == false)
            {
                DestroyObject(texture);
                return null;
            }

            return new RawIconRenderResult(texture, itemPixels, textureWidth, textureHeight);
        }
        finally
        {
            RenderTexture.active = previousActiveTexture;
            RestoreSceneLightCullingMasks(sceneLights, sceneLightCullingMasks);

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

    private static bool HasVisiblePixels(Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > VisibleAlphaThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private static Color32[] CreateProcessedPixels(Color32[] itemPixels, int width, int height, IconPostProcessSettings settings)
    {
        Color32[] pixels = CreateBackgroundPixels(width, height, new Color32(0, 0, 0, 0));
        TryApplyShadow(pixels, itemPixels, width, height, settings);
        CompositeSourceOver(pixels, itemPixels);
        TryApplyOutline(pixels, itemPixels, width, height, settings);
        return pixels;
    }

    private static IconPostProcessSettings CreatePostProcessSettings(ItemData itemData)
    {
        return new IconPostProcessSettings(
            itemData.IconUseShadow,
            itemData.IconShadowColor,
            itemData.IconShadowTextureOffset,
            itemData.IconShadowTextureBlur,
            itemData.IconUseOutline,
            itemData.IconOutlineColor,
            itemData.IconOutlineTextureWidth);
    }

    private static Sprite CreateSprite(ItemData itemData, Texture2D texture)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            itemData.IconSpritePixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        sprite.name = $"{itemData.name} Runtime Icon";

        return sprite;
    }

    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings)
    {
        return new IconCacheKey(itemData.GetInstanceID(), itemData.BuildIconHash(runtimeIconParts), settings.BuildHash());
    }

    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        int iconHash = renderProfile.UseSlotSettings
            ? itemData.BuildSlotIconHash(renderProfile.CellWidth, renderProfile.CellHeight, runtimeIconParts)
            : itemData.BuildIconHash(runtimeIconParts);

        return new IconCacheKey(itemData.GetInstanceID(), iconHash, settings.BuildHash());
    }

    private static bool TryGetCachedSprite(IconCacheKey key, out Sprite sprite)
    {
        if (cache.TryGetValue(key, out IconCacheEntry entry) && entry.Sprite != null)
        {
            sprite = entry.Sprite;
            return true;
        }

        sprite = null;
        return false;
    }

    private static void CacheFallbackSprite(IconCacheKey key, ItemData itemData)
    {
        if (itemData.FallbackIcon == null)
        {
            return;
        }

        cache[key] = new IconCacheEntry(itemData.FallbackIcon, null);
    }

    private static Color32[] CreateBackgroundPixels(int width, int height, Color32 backgroundColor)
    {
        Color32[] pixels = new Color32[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }

        return pixels;
    }

    private static bool TryApplyShadow(Color32[] pixels, Color32[] sourcePixels, int width, int height, IconPostProcessSettings settings)
    {
        if (settings.UseShadow == false)
        {
            return false;
        }

        Vector2Int offset = settings.ShadowOffset;
        int blurRadius = settings.ShadowBlur;
        Color shadowColor = settings.ShadowColor;
        int blurRadiusSquared = blurRadius * blurRadius;
        bool changed = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float shadowAlpha = GetShadowAlpha(sourcePixels, width, height, x - offset.x, y - offset.y, blurRadius, blurRadiusSquared);
                if (shadowAlpha <= 0f)
                {
                    continue;
                }

                int index = y * width + x;
                Color32 shadowPixel = new Color(
                    shadowColor.r,
                    shadowColor.g,
                    shadowColor.b,
                    shadowColor.a * shadowAlpha);
                pixels[index] = CompositeOver(shadowPixel, pixels[index]);
                changed = true;
            }
        }

        return changed;
    }

    private static float GetShadowAlpha(Color32[] sourcePixels, int width, int height, int centerX, int centerY, int blurRadius, int blurRadiusSquared)
    {
        if (blurRadius <= 0)
        {
            if (centerX < 0 || centerY < 0 || centerX >= width || centerY >= height)
            {
                return 0f;
            }

            return sourcePixels[centerY * width + centerX].a / 255f;
        }

        float alpha = 0f;

        for (int offsetY = -blurRadius; offsetY <= blurRadius; offsetY++)
        {
            int sampleY = centerY + offsetY;
            if (sampleY < 0 || sampleY >= height)
            {
                continue;
            }

            for (int offsetX = -blurRadius; offsetX <= blurRadius; offsetX++)
            {
                int distanceSquared = offsetX * offsetX + offsetY * offsetY;
                if (distanceSquared > blurRadiusSquared)
                {
                    continue;
                }

                int sampleX = centerX + offsetX;
                if (sampleX < 0 || sampleX >= width)
                {
                    continue;
                }

                float sampleAlpha = sourcePixels[sampleY * width + sampleX].a / 255f;
                if (sampleAlpha <= 0f)
                {
                    continue;
                }

                float falloff = 1f - Mathf.Sqrt(distanceSquared) / (blurRadius + 1f);
                alpha = Mathf.Max(alpha, sampleAlpha * falloff);
            }
        }

        return alpha;
    }

    private static void CompositeSourceOver(Color32[] pixels, Color32[] sourcePixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            if (sourcePixels[i].a == 0)
            {
                continue;
            }

            pixels[i] = CompositeOver(sourcePixels[i], pixels[i]);
        }
    }

    private static Color32 CompositeOver(Color32 foreground, Color32 background)
    {
        float foregroundAlpha = foreground.a / 255f;
        float backgroundAlpha = background.a / 255f;
        float outputAlpha = foregroundAlpha + backgroundAlpha * (1f - foregroundAlpha);

        if (outputAlpha <= 0f)
        {
            return new Color32(0, 0, 0, 0);
        }

        byte r = (byte)Mathf.RoundToInt(((foreground.r / 255f) * foregroundAlpha + (background.r / 255f) * backgroundAlpha * (1f - foregroundAlpha)) / outputAlpha * 255f);
        byte g = (byte)Mathf.RoundToInt(((foreground.g / 255f) * foregroundAlpha + (background.g / 255f) * backgroundAlpha * (1f - foregroundAlpha)) / outputAlpha * 255f);
        byte b = (byte)Mathf.RoundToInt(((foreground.b / 255f) * foregroundAlpha + (background.b / 255f) * backgroundAlpha * (1f - foregroundAlpha)) / outputAlpha * 255f);
        byte a = (byte)Mathf.RoundToInt(outputAlpha * 255f);

        return new Color32(r, g, b, a);
    }

    private static bool TryApplyOutline(Color32[] pixels, Color32[] sourcePixels, int width, int height, IconPostProcessSettings settings)
    {
        if (settings.UseOutline == false)
        {
            return false;
        }

        int radius = settings.OutlineWidth;
        if (radius <= 0)
        {
            return false;
        }

        Color32 outlineColor = settings.OutlineColor;
        int radiusSquared = radius * radius;
        bool changed = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (sourcePixels[index].a > VisibleAlphaThreshold)
                {
                    continue;
                }

                bool touchesVisiblePixel = false;

                for (int offsetY = -radius; offsetY <= radius && touchesVisiblePixel == false; offsetY++)
                {
                    int sampleY = y + offsetY;
                    if (sampleY < 0 || sampleY >= height)
                    {
                        continue;
                    }

                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
                        {
                            continue;
                        }

                        int sampleX = x + offsetX;
                        if (sampleX < 0 || sampleX >= width)
                        {
                            continue;
                        }

                        if (sourcePixels[sampleY * width + sampleX].a > VisibleAlphaThreshold)
                        {
                            touchesVisiblePixel = true;
                            break;
                        }
                    }
                }

                if (touchesVisiblePixel)
                {
                    pixels[index] = CompositeOver(outlineColor, pixels[index]);
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static bool InstantiateParts(IReadOnlyList<ItemIconPart> parts, Transform parent)
    {
        if (parts == null)
        {
            return false;
        }

        bool hasRenderablePart = false;

        for (int i = 0; i < parts.Count; i++)
        {
            ItemIconPart part = parts[i];
            if (part == null)
            {
                continue;
            }

            hasRenderablePart |= InstantiateSource(part.Prefab, parent, part);
        }

        return hasRenderablePart;
    }

    private static bool InstantiateSource(GameObject source, Transform parent, ItemIconPart transformOverride)
    {
        if (source == null)
        {
            return false;
        }

        GameObject instance = UnityEngine.Object.Instantiate(source, parent);
        instance.hideFlags = HideFlags.HideAndDontSave;

        if (transformOverride == null)
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }
        else
        {
            transformOverride.ApplyTo(instance.transform);
        }

        return true;
    }

    private static bool TryCalculateBounds(GameObject rootObject, out Bounds bounds)
    {
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(rootObject.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer.enabled == false)
            {
                continue;
            }

            if (hasBounds == false)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void ConfigureIsolatedCamera(HDAdditionalCameraData hdCameraData, ItemIconGeneratorSettings settings)
    {
        hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hdCameraData.backgroundColorHDR = new Color(0f, 0f, 0f, 0f);
        hdCameraData.clearDepth = true;
        hdCameraData.volumeLayerMask = settings.RenderLayerMask;
        hdCameraData.volumeAnchorOverride = null;
        hdCameraData.probeLayerMask = settings.RenderLayerMask;
        hdCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        hdCameraData.dithering = false;
        hdCameraData.stopNaNs = false;
        hdCameraData.xrRendering = false;
        hdCameraData.allowDynamicResolution = false;
        hdCameraData.customRenderingSettings = true;

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.OpaqueObjects, true);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.TransparentObjects, true);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LightLayers, true);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.DirectSpecularLighting, settings.EnableDirectSpecularLighting);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ExposureControl, settings.EnableExposureControl);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.AtmosphericScattering, settings.EnableAtmosphericScattering);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Postprocess, settings.EnablePostProcess);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.CustomPostProcess, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.AfterPostprocess, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Antialiasing, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Bloom, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ColorGrading, settings.EnableColorGrading);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Tonemapping, settings.EnableTonemapping);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Vignette, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.DepthOfField, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.MotionBlur, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LensDistortion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ChromaticAberration, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.FilmGrain, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Dithering, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LensFlareDataDriven, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LensFlareScreenSpace, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Volumetrics, settings.EnableVolumetrics);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ReprojectionForVolumetrics, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.VolumetricClouds, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.FullResolutionCloudsForSky, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SSAO, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SSGI, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SSR, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.TransparentSSR, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ScreenSpaceShadows, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ContactShadows, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Shadowmask, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ShadowMaps, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ReflectionProbe, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.PlanarProbe, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SkyReflection, settings.EnableSkyReflection);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.AdaptiveProbeVolume, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.NormalizeReflectionProbeWithProbeVolume, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Decals, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.DecalLayers, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.CustomPass, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.MotionVectors, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ObjectMotionVectors, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.TransparentsWriteMotionVector, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Refraction, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Distortion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.RoughDistortion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Water, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.WaterDecals, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.WaterExclusion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.RayTracing, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.RaytracingVFX, false);
    }

    private static GameObject CreateIsolatedVolume(string itemName, ItemIconGeneratorSettings settings, out VolumeProfile volumeProfile)
    {
        GameObject volumeObject = new GameObject($"Runtime Icon Volume - {itemName}")
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = settings.RenderLayer
        };

        volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        volumeProfile.hideFlags = HideFlags.HideAndDontSave;
        volumeProfile.name = $"{itemName} Runtime Icon Volume Profile";
        ConfigureIsolatedVolumeProfile(volumeProfile, settings);

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10000f;
        volume.weight = 1f;
        volume.sharedProfile = volumeProfile;

        return volumeObject;
    }

    private static void ConfigureIsolatedVolumeProfile(VolumeProfile volumeProfile, ItemIconGeneratorSettings settings)
    {
        VisualEnvironment visualEnvironment = volumeProfile.Add<VisualEnvironment>(true);
        visualEnvironment.skyType.Override(SkySettings.GetUniqueID<PhysicallyBasedSky>());
        visualEnvironment.cloudType.Override(0);
        visualEnvironment.skyAmbientMode.Override(settings.SkyAmbientMode);
        visualEnvironment.renderingSpace.Override(settings.SkyRenderingSpace);

        PhysicallyBasedSky physicallyBasedSky = volumeProfile.Add<PhysicallyBasedSky>(true);
        physicallyBasedSky.active = true;
        physicallyBasedSky.type.Override(settings.PhysicallyBasedSkyModel);
        physicallyBasedSky.groundTint.Override(settings.PhysicallyBasedSkyGroundTint);

        Fog fog = volumeProfile.Add<Fog>(true);
        fog.enabled.Override(settings.FogEnabled);
        fog.colorMode.Override(settings.FogColorMode);
        fog.maxFogDistance.Override(settings.MaxFogDistance);
        fog.meanFreePath.Override(settings.MeanFreePath);
        fog.enableVolumetricFog.Override(settings.EnableVolumetricFog);
        fog.anisotropy.Override(settings.FogAnisotropy);

        Exposure exposure = volumeProfile.Add<Exposure>(true);
        exposure.mode.Override(settings.ExposureMode);
        exposure.fixedExposure.Override(settings.FixedExposure);
        exposure.compensation.Override(settings.ExposureCompensation);
        exposure.limitMin.Override(settings.ExposureLimitMin);
        exposure.limitMax.Override(settings.ExposureLimitMax);
        exposure.histogramPercentages.Override(settings.HistogramPercentages);

        ColorAdjustments colorAdjustments = volumeProfile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(settings.PostExposure);
        colorAdjustments.contrast.Override(settings.Contrast);
        colorAdjustments.colorFilter.Override(settings.ColorFilter);
        colorAdjustments.hueShift.Override(settings.HueShift);
        colorAdjustments.saturation.Override(settings.Saturation);

        Tonemapping tonemapping = volumeProfile.Add<Tonemapping>(true);
        tonemapping.mode.Override(settings.TonemappingMode);
    }

    private static void SetCustomFrameSetting(HDAdditionalCameraData hdCameraData, FrameSettingsField field, bool enabled)
    {
        hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        hdCameraData.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
    }

    private static List<GameObject> CreateIconLights(ItemData itemData, Transform cameraTransform, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        List<GameObject> lightObjects = new List<GameObject>(4);
        float baseIntensity = renderProfile.LightIntensity;

        AddIconDirectionalLight(
            settings,
            lightObjects,
            $"Runtime Icon Key Light - {itemData.name}",
            baseIntensity,
            Quaternion.Euler(renderProfile.LightEulerAngles));

        if (settings.UseFrontFillLight)
        {
            AddIconDirectionalLight(
                settings,
                lightObjects,
                $"Runtime Icon Front Fill Light - {itemData.name}",
                baseIntensity * settings.FrontFillLightIntensityMultiplier,
                GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.FrontFillLightCameraDirection), cameraTransform.up));
        }

        if (settings.UseSideFillLight)
        {
            AddIconDirectionalLight(
                settings,
                lightObjects,
                $"Runtime Icon Side Fill Light - {itemData.name}",
                baseIntensity * settings.SideFillLightIntensityMultiplier,
                GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.SideFillLightCameraDirection), cameraTransform.up));
        }

        if (settings.UseRimLight)
        {
            AddIconDirectionalLight(
                settings,
                lightObjects,
                $"Runtime Icon Rim Light - {itemData.name}",
                baseIntensity * settings.RimLightIntensityMultiplier,
                GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.RimLightCameraDirection), cameraTransform.up));
        }

        return lightObjects;
    }

    private static void AddIconDirectionalLight(ItemIconGeneratorSettings settings, List<GameObject> lightObjects, string lightName, float intensity, Quaternion rotation)
    {
        GameObject lightObject = new GameObject(lightName, typeof(Light))
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Light renderLight = lightObject.GetComponent<Light>();
        HDAdditionalLightData hdLightData = renderLight.gameObject.AddComponent<HDAdditionalLightData>();
        renderLight.type = LightType.Directional;
        renderLight.intensity = Mathf.Max(0f, intensity);
        renderLight.cullingMask = settings.RenderLayerMask;
        renderLight.renderingLayerMask = (int)settings.RenderingLayerMask;
        renderLight.shadows = settings.GeneratedLightShadows;
        renderLight.transform.rotation = rotation;

        UnityEngine.Rendering.HighDefinition.RenderingLayerMask iconLightLayer =
            (UnityEngine.Rendering.HighDefinition.RenderingLayerMask)settings.RenderingLayerMask;
        hdLightData.SetLightLayer(iconLightLayer, iconLightLayer);

        lightObjects.Add(lightObject);
    }

    private static Quaternion GetDirectionalLightRotation(Vector3 direction, Vector3 up)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        Vector3 normalizedUp = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;
        return Quaternion.LookRotation(direction.normalized, normalizedUp);
    }

    private static int[] ExcludeIconLayerFromSceneLights(ItemIconGeneratorSettings settings, out Light[] sceneLights)
    {
        sceneLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int[] previousCullingMasks = new int[sceneLights.Length];

        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light sceneLight = sceneLights[i];
            if (sceneLight == null)
            {
                continue;
            }

            previousCullingMasks[i] = sceneLight.cullingMask;
            sceneLight.cullingMask &= ~settings.RenderLayerMask;
        }

        return previousCullingMasks;
    }

    private static void RestoreSceneLightCullingMasks(Light[] sceneLights, int[] previousCullingMasks)
    {
        if (sceneLights == null || previousCullingMasks == null)
        {
            return;
        }

        int count = Mathf.Min(sceneLights.Length, previousCullingMasks.Length);
        for (int i = 0; i < count; i++)
        {
            Light sceneLight = sceneLights[i];
            if (sceneLight != null)
            {
                sceneLight.cullingMask = previousCullingMasks[i];
            }
        }
    }

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

    private static float CalculateOrthographicSize(Bounds bounds, Quaternion cameraRotation, float aspect, float padding)
    {
        Vector3[] corners = GetBoundsCorners(bounds);
        Quaternion inverseCameraRotation = Quaternion.Inverse(cameraRotation);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 cameraLocalPoint = inverseCameraRotation * (corners[i] - bounds.center);
            minX = Mathf.Min(minX, cameraLocalPoint.x);
            maxX = Mathf.Max(maxX, cameraLocalPoint.x);
            minY = Mathf.Min(minY, cameraLocalPoint.y);
            maxY = Mathf.Max(maxY, cameraLocalPoint.y);
        }

        float halfWidth = (maxX - minX) / 2f;
        float halfHeight = (maxY - minY) / 2f;
        return Mathf.Max(0.01f, halfHeight, halfWidth / Mathf.Max(0.01f, aspect)) * padding;
    }

    private static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new[]
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z),
        };
    }

    private static void SetLayerRecursively(GameObject rootObject, int layer)
    {
        Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.layer = layer;
        }
    }

    private static void SetRendererIsolation(GameObject rootObject, ItemIconGeneratorSettings settings)
    {
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.renderingLayerMask = settings.RenderingLayerMask;
            renderer.lightProbeUsage = settings.RendererLightProbeUsage;
            renderer.reflectionProbeUsage = settings.RendererReflectionProbeUsage;
            renderer.receiveShadows = settings.RendererReceiveShadows;
        }
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
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

    private readonly struct IconRenderProfile
    {
        public readonly bool UseSlotSettings;
        public readonly int CellWidth;
        public readonly int CellHeight;
        public readonly int TextureWidth;
        public readonly int TextureHeight;
        public readonly Vector3 ModelEulerAngles;
        public readonly Vector3 ModelScale;
        public readonly Vector3 CameraEulerAngles;
        public readonly float Padding;
        public readonly bool UseDirectionalLight;
        public readonly Vector3 LightEulerAngles;
        public readonly float LightIntensity;

        private IconRenderProfile(
            bool useSlotSettings,
            int cellWidth,
            int cellHeight,
            int textureWidth,
            int textureHeight,
            Vector3 modelEulerAngles,
            Vector3 modelScale,
            Vector3 cameraEulerAngles,
            float padding,
            bool useDirectionalLight,
            Vector3 lightEulerAngles,
            float lightIntensity)
        {
            UseSlotSettings = useSlotSettings;
            CellWidth = Mathf.Max(1, cellWidth);
            CellHeight = Mathf.Max(1, cellHeight);
            TextureWidth = Mathf.Max(1, textureWidth);
            TextureHeight = Mathf.Max(1, textureHeight);
            ModelEulerAngles = modelEulerAngles;
            ModelScale = modelScale == Vector3.zero ? Vector3.one : modelScale;
            CameraEulerAngles = cameraEulerAngles;
            Padding = Mathf.Max(1f, padding);
            UseDirectionalLight = useDirectionalLight;
            LightEulerAngles = lightEulerAngles;
            LightIntensity = Mathf.Max(0f, lightIntensity);
        }

        public static IconRenderProfile CreateDefault(ItemData itemData)
        {
            return new IconRenderProfile(
                false,
                Mathf.Max(1, itemData.width),
                Mathf.Max(1, itemData.height),
                itemData.IconTextureWidth,
                itemData.IconTextureHeight,
                itemData.IconModelEulerAngles,
                itemData.IconModelScale,
                itemData.IconCameraEulerAngles,
                itemData.IconPadding,
                itemData.IconUseDirectionalLight,
                itemData.IconLightEulerAngles,
                itemData.IconLightIntensity);
        }

        public static IconRenderProfile CreateSlot(ItemData itemData, int slotWidth, int slotHeight)
        {
            int cellWidth = Mathf.Max(1, slotWidth);
            int cellHeight = Mathf.Max(1, slotHeight);

            return new IconRenderProfile(
                true,
                cellWidth,
                cellHeight,
                cellWidth * itemData.IconPixelsPerCell * itemData.IconRenderScale,
                cellHeight * itemData.IconPixelsPerCell * itemData.IconRenderScale,
                itemData.SlotIconModelEulerAngles,
                itemData.SlotIconModelScale,
                itemData.SlotIconCameraEulerAngles,
                itemData.SlotIconPadding,
                itemData.SlotIconUseDirectionalLight,
                itemData.SlotIconLightEulerAngles,
                itemData.SlotIconLightIntensity);
        }
    }

    private sealed class RawIconRenderResult
    {
        public readonly Texture2D Texture;
        public readonly Color32[] ItemPixels;
        public readonly int Width;
        public readonly int Height;

        public RawIconRenderResult(Texture2D texture, Color32[] itemPixels, int width, int height)
        {
            Texture = texture;
            ItemPixels = itemPixels;
            Width = width;
            Height = height;
        }
    }

    private readonly struct IconPostProcessSettings
    {
        public readonly bool UseShadow;
        public readonly Color ShadowColor;
        public readonly Vector2Int ShadowOffset;
        public readonly int ShadowBlur;
        public readonly bool UseOutline;
        public readonly Color32 OutlineColor;
        public readonly int OutlineWidth;

        public IconPostProcessSettings(
            bool useShadow,
            Color shadowColor,
            Vector2Int shadowOffset,
            int shadowBlur,
            bool useOutline,
            Color outlineColor,
            int outlineWidth)
        {
            UseShadow = useShadow;
            ShadowColor = shadowColor;
            ShadowOffset = shadowOffset;
            ShadowBlur = shadowBlur;
            UseOutline = useOutline;
            OutlineColor = outlineColor;
            OutlineWidth = outlineWidth;
        }
    }

    private struct IconCacheKey : IEquatable<IconCacheKey>
    {
        private readonly int itemId;
        private readonly int iconHash;
        private readonly int generatorHash;

        public IconCacheKey(int itemId, int iconHash, int generatorHash)
        {
            this.itemId = itemId;
            this.iconHash = iconHash;
            this.generatorHash = generatorHash;
        }

        public bool Equals(IconCacheKey other)
        {
            return itemId == other.itemId && iconHash == other.iconHash && generatorHash == other.generatorHash;
        }

        public override bool Equals(object obj)
        {
            return obj is IconCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = itemId;
                hash = (hash * 397) ^ iconHash;
                hash = (hash * 397) ^ generatorHash;
                return hash;
            }
        }
    }

    private sealed class IconCacheEntry
    {
        public readonly Sprite Sprite;
        public readonly Texture2D Texture;

        public IconCacheEntry(Sprite sprite, Texture2D texture)
        {
            Sprite = sprite;
            Texture = texture;
        }
    }
}
