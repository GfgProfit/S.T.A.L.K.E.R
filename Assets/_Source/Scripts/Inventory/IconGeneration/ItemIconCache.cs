using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class ItemIconCache
{
    private static Dictionary<IconCacheKey, IconCacheEntry> _cache = new();

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

        Sprite generatedSprite = ItemIconRenderer.RenderIcon(itemData, runtimeIconParts, settings, out Texture2D generatedTexture);

        if (generatedSprite == null)
        {
            ItemIconRenderer.DestroyObject(generatedTexture);
            CacheFallbackSprite(key, itemData);
            return itemData.FallbackIcon;
        }

        _cache[key] = IconCacheEntry.CreateGenerated(generatedSprite, generatedTexture);
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

        Sprite generatedSprite = ItemIconRenderer.RenderIcon(itemData, runtimeIconParts, settings, renderProfile, out Texture2D generatedTexture);

        if (generatedSprite == null)
        {
            ItemIconRenderer.DestroyObject(generatedTexture);
            CacheFallbackSprite(key, itemData);
            return itemData.FallbackIcon;
        }

        _cache[key] = IconCacheEntry.CreateGenerated(generatedSprite, generatedTexture);
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
        foreach (IconCacheEntry entry in _cache.Values)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.OwnsSprite)
            {
                ItemIconRenderer.DestroyObject(entry.Sprite);
            }

            if (entry.OwnsTexture)
            {
                ItemIconRenderer.DestroyObject(entry.Texture);
            }
        }

        _cache.Clear();
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

        RawIconRenderResult rawResult = ItemIconRenderer.RenderRawIcon(itemData, runtimeIconParts, settings, IconRenderProfile.CreateDefault(itemData));

        if (rawResult == null)
        {
            CacheFallbackSprite(key, itemData);
            completedCallback?.Invoke(itemData.FallbackIcon);
            yield break;
        }

        ItemIconPostProcessSettings postProcessSettings = ItemIconTextureProcessor.CreatePostProcessSettings(itemData);
        Task<Color32[]> postProcessTask = Task.Run(() => ItemIconTextureProcessor.CreateProcessedPixels(rawResult.ItemPixels, rawResult.Width, rawResult.Height, postProcessSettings));

        while (postProcessTask.IsCompleted == false)
        {
            yield return null;
        }

        if (postProcessTask.IsFaulted || postProcessTask.IsCanceled)
        {
            Exception exception = postProcessTask.Exception;
            Debug.LogException(exception ?? new InvalidOperationException("Runtime item icon post-processing was canceled."));
            ItemIconRenderer.DestroyObject(rawResult.Texture);
            completedCallback?.Invoke(itemData.FallbackIcon);
            yield break;
        }

        rawResult.Texture.SetPixels32(postProcessTask.Result);
        rawResult.Texture.Apply(false, false);

        Sprite generatedSprite = ItemIconTextureProcessor.CreateSprite(itemData, rawResult.Texture);
        _cache[key] = IconCacheEntry.CreateGenerated(generatedSprite, rawResult.Texture);
        completedCallback?.Invoke(generatedSprite);
    }

    private static List<ItemData> BuildUniqueItemList(IReadOnlyList<ItemData> itemDataList)
    {
        List<ItemData> uniqueItems = new();
        HashSet<int> seenIds = new();

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
        _cache = new Dictionary<IconCacheKey, IconCacheEntry>();
    }

    public static Texture2D RenderPreviewTexture(ItemData itemData, ItemIconGeneratorSettings settings = null) => ItemIconRenderer.RenderPreviewTexture(itemData, settings);
    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings) => new(itemData.GetInstanceID(), itemData.BuildIconHash(runtimeIconParts), settings.BuildHash());

    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        int iconHash = renderProfile.UseSlotSettings ? itemData.BuildSlotIconHash(renderProfile.CellWidth, renderProfile.CellHeight, runtimeIconParts) : itemData.BuildIconHash(runtimeIconParts);

        return new IconCacheKey(itemData.GetInstanceID(), iconHash, settings.BuildHash());
    }

    private static bool TryGetCachedSprite(IconCacheKey key, out Sprite sprite)
    {
        if (_cache.TryGetValue(key, out IconCacheEntry entry) && entry.Sprite != null)
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

        _cache[key] = IconCacheEntry.CreateFallback(itemData.FallbackIcon);
    }

}