using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public static class ItemIconCache
{
    private const int RenderLayer = 31;
    private const int RenderLayerMask = 1 << RenderLayer;
    private const byte VisibleAlphaThreshold = 8;
    private static readonly Vector3 RenderOrigin = Vector3.zero;

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

        IconCacheKey key = BuildCacheKey(itemData, runtimeIconParts);

        if (TryGetCachedSprite(key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite generatedSprite = RenderIcon(itemData, runtimeIconParts, out Texture2D generatedTexture);
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

        IconCacheKey key = BuildCacheKey(itemData, runtimeIconParts);
        if (TryGetCachedSprite(key, out Sprite cachedSprite))
        {
            completedCallback?.Invoke(cachedSprite);
            yield break;
        }

        RawIconRenderResult rawResult = RenderRawIcon(itemData, runtimeIconParts);
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

        Sprite generatedSprite = CreateSprite(itemData, rawResult.Texture, rawResult.Width, rawResult.Height);
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

    private static Sprite RenderIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, out Texture2D texture)
    {
        texture = null;

        RawIconRenderResult rawResult = RenderRawIcon(itemData, runtimeIconParts);
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

            texture = rawResult.Texture;
            return CreateSprite(itemData, texture, rawResult.Width, rawResult.Height);
        }
        catch
        {
            DestroyObject(rawResult.Texture);
            throw;
        }
    }

    private static RawIconRenderResult RenderRawIcon(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        GameObject rootObject = null;
        GameObject cameraObject = null;
        GameObject lightObject = null;
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        RenderTexture previousActiveTexture = RenderTexture.active;

        try
        {
            rootObject = new GameObject($"Runtime Icon Root - {itemData.name}");
            rootObject.hideFlags = HideFlags.HideAndDontSave;
            rootObject.transform.position = RenderOrigin;
            rootObject.transform.rotation = Quaternion.Euler(itemData.IconModelEulerAngles);
            rootObject.transform.localScale = itemData.IconModelScale;

            bool hasRenderablePart = InstantiateSource(itemData.IconPrefab, rootObject.transform, null);
            hasRenderablePart |= InstantiateParts(itemData.IconParts, rootObject.transform);
            hasRenderablePart |= InstantiateParts(runtimeIconParts, rootObject.transform);

            if (hasRenderablePart == false || TryCalculateBounds(rootObject, out Bounds bounds) == false)
            {
                return null;
            }

            SetLayerRecursively(rootObject, RenderLayer);

            int textureWidth = itemData.IconTextureWidth;
            int textureHeight = itemData.IconTextureHeight;

            renderTexture = RenderTexture.GetTemporary(
                textureWidth,
                textureHeight,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default,
                itemData.IconAntiAliasing);
            renderTexture.filterMode = FilterMode.Bilinear;

            cameraObject = new GameObject($"Runtime Icon Camera - {itemData.name}", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;

            Camera renderCamera = cameraObject.GetComponent<Camera>();

            HDAdditionalCameraData hdCameraData = renderCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdCameraData.backgroundColorHDR = new Color(0f, 0f, 0f, 0f);
            hdCameraData.clearDepth = true;

            Quaternion cameraRotation = Quaternion.Euler(itemData.IconCameraEulerAngles);

            float boundsRadius = Mathf.Max(0.1f, bounds.extents.magnitude);

            renderCamera.enabled = false;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = RenderLayerMask;
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = CalculateOrthographicSize(bounds, cameraRotation, (float)textureWidth / textureHeight, itemData.IconPadding);
            renderCamera.nearClipPlane = 0.01f;
            renderCamera.farClipPlane = boundsRadius * 8f + 10f;
            renderCamera.targetTexture = renderTexture;
            renderCamera.transform.rotation = cameraRotation;
            renderCamera.transform.position = bounds.center - renderCamera.transform.forward * (boundsRadius * 3f + 1f);
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = itemData.IconAntiAliasing > 1;

            if (itemData.IconUseDirectionalLight)
            {
                lightObject = new GameObject($"Runtime Icon Light - {itemData.name}", typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                Light renderLight = lightObject.GetComponent<Light>();
                renderLight.gameObject.AddComponent<HDAdditionalLightData>();
                renderLight.type = LightType.Directional;
                renderLight.intensity = itemData.IconLightIntensity;
                renderLight.cullingMask = RenderLayerMask;
                renderLight.transform.rotation = Quaternion.Euler(itemData.IconLightEulerAngles);
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

            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            DestroyObject(lightObject);
            DestroyObject(cameraObject);
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

    private static Sprite CreateSprite(ItemData itemData, Texture2D texture, int textureWidth, int textureHeight)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            itemData.IconSpritePixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        sprite.name = $"{itemData.name} Runtime Icon";

        return sprite;
    }

    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        return new IconCacheKey(itemData.GetInstanceID(), itemData.BuildIconHash(runtimeIconParts));
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

        public IconCacheKey(int itemId, int iconHash)
        {
            this.itemId = itemId;
            this.iconHash = iconHash;
        }

        public bool Equals(IconCacheKey other)
        {
            return itemId == other.itemId && iconHash == other.iconHash;
        }

        public override bool Equals(object obj)
        {
            return obj is IconCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (itemId * 397) ^ iconHash;
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
