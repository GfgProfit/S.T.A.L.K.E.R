using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public static class ItemIconCache
{
    private static Dictionary<IconCacheKey, IconCacheEntry> _cache = new();
    private static readonly Dictionary<IconCacheKey, UniTaskCompletionSource<Sprite>> _inFlight = new();
    private static readonly Queue<StandaloneGenerationRequest> _standaloneQueue = new();
    private static CancellationTokenSource _generationCancellation = new();
    private static ItemIconRenderSession _standaloneRenderSession;
    private static ItemIconGeneratorSettings _standaloneRenderSettings;
    private static int _standaloneRenderSettingsHash;
    private static bool _standaloneWorkerRunning;
    private static bool _releaseStandaloneRenderSessionWhenWorkerStops;

    internal static bool IsPrewarmed { get; private set; }

    public static Sprite GetOrCreate(ItemData itemData, IReadOnlyList<ItemData> installedModules = null)
    {
        return itemData == null ? null : GetOrCreate(itemData, itemData.Width, itemData.Height, installedModules);
    }

    internal static bool TryGet(ItemData itemData, IReadOnlyList<ItemData> installedModules, out Sprite icon)
    {
        return TryGet(itemData, itemData == null ? 1 : itemData.Width, itemData == null ? 1 : itemData.Height, installedModules, out icon);
    }

    internal static bool TryGet(ItemData itemData, int width, int height, IReadOnlyList<ItemData> installedModules, out Sprite icon)
    {
        if (itemData == null)
        {
            icon = null;
            return true;
        }

        if (itemData.HasRuntimeIconSource() == false)
        {
            icon = itemData.FallbackIcon;
            return true;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateDefault(itemData, width, height);
        IconCacheKey key = BuildCacheKey(itemData, installedModules, settings, renderProfile);
        return TryGetCachedSprite(key, out icon);
    }

    internal static bool TryGetSlotIcon(ItemData itemData, int slotWidth, int slotHeight, IReadOnlyList<ItemData> installedModules, out Sprite icon)
    {
        if (itemData == null)
        {
            icon = null;
            return true;
        }

        if (itemData.HasRuntimeIconSource() == false)
        {
            icon = itemData.FallbackIcon;
            return true;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateSlot(itemData, slotWidth, slotHeight);
        IconCacheKey key = BuildCacheKey(itemData, installedModules, settings, renderProfile);
        return TryGetCachedSprite(key, out icon);
    }

    public static Sprite GetOrCreate(ItemData itemData, int width, int height, IReadOnlyList<ItemData> installedModules = null)
    {
        if (itemData == null)
        {
            return null;
        }

        if (itemData.HasRuntimeIconSource() == false)
        {
            return itemData.FallbackIcon;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateDefault(itemData, width, height);
        IconCacheKey key = BuildCacheKey(itemData, installedModules, settings, renderProfile);
        return TryGetCachedSprite(key, out Sprite cachedSprite) ? cachedSprite : itemData.FallbackIcon;
    }

    public static Sprite GetOrCreateSlotIcon(ItemData itemData, int slotWidth, int slotHeight, IReadOnlyList<ItemData> installedModules = null)
    {
        if (itemData == null)
        {
            return null;
        }

        if (itemData.HasRuntimeIconSource() == false)
        {
            return itemData.FallbackIcon;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateSlot(itemData, slotWidth, slotHeight);
        IconCacheKey key = BuildCacheKey(itemData, installedModules, settings, renderProfile);
        return TryGetCachedSprite(key, out Sprite cachedSprite) ? cachedSprite : itemData.FallbackIcon;
    }

    public static UniTask<Sprite> GetOrCreateAsync(ItemData itemData, IReadOnlyList<ItemData> installedModules = null, CancellationToken cancellationToken = default)
    {
        return itemData == null
            ? UniTask.FromResult<Sprite>(null)
            : GetOrCreateAsync(itemData, itemData.Width, itemData.Height, installedModules, cancellationToken);
    }

    public static UniTask<Sprite> GetOrCreateAsync(ItemData itemData, int width, int height, IReadOnlyList<ItemData> installedModules = null, CancellationToken cancellationToken = default)
    {
        if (itemData == null)
        {
            return UniTask.FromResult<Sprite>(null);
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateDefault(itemData, width, height);
        return GetOrCreateStandaloneAsync(itemData, installedModules, settings, renderProfile, cancellationToken);
    }

    public static UniTask<Sprite> GetOrCreateSlotIconAsync(ItemData itemData, int slotWidth, int slotHeight, IReadOnlyList<ItemData> installedModules = null, CancellationToken cancellationToken = default)
    {
        if (itemData == null)
        {
            return UniTask.FromResult<Sprite>(null);
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = IconRenderProfile.CreateSlot(itemData, slotWidth, slotHeight);
        return GetOrCreateStandaloneAsync(itemData, installedModules, settings, renderProfile, cancellationToken);
    }

    internal static async UniTask PrewarmAsync(
        IReadOnlyList<ItemData> itemDataList,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        Action<int, int, ItemData> progressCallback,
        CancellationToken cancellationToken)
    {
        if (itemDataList == null)
        {
            return;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        int generatorHash = settings.BuildHash();
        List<PrewarmRequest> requests = BuildPrewarmRequests(BuildUniqueItemList(itemDataList), slotProfiles, generatorHash);
        int total = requests.Count;
        float frameBudgetMilliseconds = settings.PrewarmFrameBudgetMilliseconds;
        int budgetFrame = -1;
        Stopwatch frameTimer = new();

        progressCallback?.Invoke(0, total, null);

        if (total == 0)
        {
            IsPrewarmed = true;
            return;
        }

        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

        while (_standaloneWorkerRunning || _standaloneQueue.Count > 0)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        using ItemIconRenderSession renderSession = ItemIconRenderer.CreateSession(settings);

        for (int i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (budgetFrame != Time.frameCount)
            {
                budgetFrame = Time.frameCount;
                frameTimer.Restart();
            }

            PrewarmRequest request = requests[i];

            try
            {
                await GetOrCreateAsync(request, renderSession, true, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CacheFallbackSprite(request.Key, request.ItemData);
            }

            progressCallback?.Invoke(i + 1, total, request.ItemData);

            if (budgetFrame == Time.frameCount && frameTimer.Elapsed.TotalMilliseconds >= frameBudgetMilliseconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                budgetFrame = Time.frameCount;
                frameTimer.Restart();
            }
        }

        IsPrewarmed = true;
    }

    public static void Clear()
    {
        IsPrewarmed = false;
        CancellationToken canceledGenerationToken = _generationCancellation.Token;
        _generationCancellation.Cancel();
        _generationCancellation.Dispose();
        _generationCancellation = new CancellationTokenSource();

        foreach (UniTaskCompletionSource<Sprite> completionSource in _inFlight.Values)
        {
            completionSource.TrySetCanceled(canceledGenerationToken);
        }

        _inFlight.Clear();
        _standaloneQueue.Clear();

        if (_standaloneWorkerRunning)
        {
            _releaseStandaloneRenderSessionWhenWorkerStops = true;
        }
        else
        {
            ReleaseStandaloneRenderSession();
        }

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

    public static UniTask<Texture2D> RenderPreviewTextureAsync(ItemData itemData, ItemIconGeneratorSettings settings = null, CancellationToken cancellationToken = default)
    {
        return ItemIconRenderer.RenderPreviewTextureAsync(itemData, settings, cancellationToken);
    }

    private static async UniTask<Sprite> GetOrCreateStandaloneAsync(
        ItemData itemData,
        IReadOnlyList<ItemData> installedModules,
        ItemIconGeneratorSettings settings,
        IconRenderProfile renderProfile,
        CancellationToken cancellationToken)
    {
        if (itemData.HasRuntimeIconSource() == false)
        {
            return itemData.FallbackIcon;
        }

        await UniTask.SwitchToMainThread(cancellationToken);

        int settingsHash = settings.BuildHash();
        IconCacheKey key = BuildCacheKey(itemData, installedModules, settingsHash, renderProfile);

        if (TryGetCachedSprite(key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        if (_inFlight.TryGetValue(key, out UniTaskCompletionSource<Sprite> existingGeneration))
        {
            return await existingGeneration.Task.AttachExternalCancellation(cancellationToken);
        }

        ItemData[] modules = CopyModules(installedModules);
        UniTaskCompletionSource<Sprite> completionSource = new();
        PrewarmRequest request = new(itemData, modules, renderProfile, key);
        _inFlight.Add(key, completionSource);
        _standaloneQueue.Enqueue(new StandaloneGenerationRequest(request, settings, settingsHash, completionSource));
        StartStandaloneWorker();
        return await completionSource.Task.AttachExternalCancellation(cancellationToken);
    }

    private static void StartStandaloneWorker()
    {
        if (_standaloneWorkerRunning)
        {
            return;
        }

        _standaloneWorkerRunning = true;
        ProcessStandaloneQueueAsync().Forget(Debug.LogException);
    }

    private static async UniTask ProcessStandaloneQueueAsync()
    {
        CancellationToken generationCancellation = _generationCancellation.Token;

        try
        {
            while (_standaloneQueue.Count > 0)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, generationCancellation);
                generationCancellation.ThrowIfCancellationRequested();

                StandaloneGenerationRequest generationRequest = _standaloneQueue.Dequeue();

                try
                {
                    if (_standaloneRenderSession == null ||
                        _standaloneRenderSettings != generationRequest.Settings ||
                        _standaloneRenderSettingsHash != generationRequest.SettingsHash)
                    {
                        ReleaseStandaloneRenderSession();
                        _standaloneRenderSettings = generationRequest.Settings;
                        _standaloneRenderSettingsHash = generationRequest.SettingsHash;
                        _standaloneRenderSession = ItemIconRenderer.CreateSession(_standaloneRenderSettings);
                    }

                    Sprite sprite = await GetOrCreateAsync(generationRequest.Request, _standaloneRenderSession, false, generationCancellation);
                    generationRequest.CompletionSource.TrySetResult(sprite);
                }
                catch (OperationCanceledException)
                {
                    generationRequest.CompletionSource.TrySetCanceled(generationCancellation);
                    throw;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    if (_standaloneRenderSession == null)
                    {
                        _standaloneRenderSettings = null;
                        _standaloneRenderSettingsHash = 0;
                    }

                    CacheFallbackSprite(generationRequest.Request.Key, generationRequest.Request.ItemData);
                    generationRequest.CompletionSource.TrySetResult(generationRequest.Request.ItemData.FallbackIcon);
                }
                finally
                {
                    if (_inFlight.TryGetValue(generationRequest.Request.Key, out UniTaskCompletionSource<Sprite> current) && ReferenceEquals(current, generationRequest.CompletionSource))
                    {
                        _inFlight.Remove(generationRequest.Request.Key);
                    }
                }

            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_releaseStandaloneRenderSessionWhenWorkerStops)
            {
                ReleaseStandaloneRenderSession();
                _releaseStandaloneRenderSessionWhenWorkerStops = false;
            }

            _standaloneWorkerRunning = false;

            if (_standaloneQueue.Count > 0)
            {
                StartStandaloneWorker();
            }
        }
    }

    private static void ReleaseStandaloneRenderSession()
    {
        _standaloneRenderSession?.Dispose();
        _standaloneRenderSession = null;
        _standaloneRenderSettings = null;
        _standaloneRenderSettingsHash = 0;
    }

    private static async UniTask<Sprite> GetOrCreateAsync(
        PrewarmRequest request,
        ItemIconRenderSession renderSession,
        bool checkInFlight,
        CancellationToken cancellationToken)
    {
        if (TryGetCachedSprite(request.Key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        if (checkInFlight && _inFlight.TryGetValue(request.Key, out UniTaskCompletionSource<Sprite> inFlightGeneration))
        {
            return await inFlightGeneration.Task.AttachExternalCancellation(cancellationToken);
        }

        Texture2D generatedTexture = await ItemIconRenderer.RenderIconTextureAsync(
            request.ItemData,
            request.InstalledModules,
            request.RenderProfile,
            renderSession,
            cancellationToken);

        if (generatedTexture == null)
        {
            CacheFallbackSprite(request.Key, request.ItemData);
            return request.ItemData.FallbackIcon;
        }

        Sprite generatedSprite = ItemIconTextureProcessor.CreateSprite(request.ItemData, generatedTexture);
        _cache[request.Key] = IconCacheEntry.CreateGenerated(generatedSprite, generatedTexture);
        return generatedSprite;
    }

    private static List<ItemData> BuildUniqueItemList(IReadOnlyList<ItemData> itemDataList)
    {
        List<ItemData> uniqueItems = new();
        HashSet<int> seenIds = new();

        for (int i = 0; i < itemDataList.Count; i++)
        {
            ItemData itemData = itemDataList[i];

            if (itemData != null && seenIds.Add(itemData.GetInstanceID()))
            {
                uniqueItems.Add(itemData);
            }
        }

        return uniqueItems;
    }

    private static List<PrewarmRequest> BuildPrewarmRequests(
        IReadOnlyList<ItemData> itemDataList,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        int generatorHash)
    {
        List<PrewarmRequest> requests = new();
        HashSet<IconCacheKey> requestKeys = new();

        for (int i = 0; i < itemDataList.Count; i++)
        {
            ItemData itemData = itemDataList[i];

            if (itemData.HasRuntimeIconSource() == false)
            {
                continue;
            }

            AddVariantRequests(itemData, Array.Empty<ItemData>(), slotProfiles, generatorHash, requests, requestKeys);

            if (IsWeapon(itemData) && itemData.IconPrefab != null)
            {
                AddWeaponModuleVariants(itemData, slotProfiles, generatorHash, requests, requestKeys);
            }
        }

        return requests;
    }

    private static void AddVariantRequests(
        ItemData itemData,
        ItemData[] installedModules,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        int generatorHash,
        ICollection<PrewarmRequest> requests,
        ISet<IconCacheKey> requestKeys)
    {
        Vector2Int moduleSizeDelta = CalculateModuleSizeDelta(installedModules);
        int defaultWidth = Mathf.Max(1, itemData.Width + moduleSizeDelta.x);
        int defaultHeight = Mathf.Max(1, itemData.Height + moduleSizeDelta.y);

        AddRequest(itemData, installedModules, generatorHash, IconRenderProfile.CreateDefault(itemData, defaultWidth, defaultHeight), requests, requestKeys);

        if (slotProfiles == null)
        {
            return;
        }

        for (int i = 0; i < slotProfiles.Count; i++)
        {
            ItemIconSlotProfile slotProfile = slotProfiles[i];

            if (slotProfile.Accepts(itemData) == false)
            {
                continue;
            }

            if (slotProfile.Width == defaultWidth && slotProfile.Height == defaultHeight)
            {
                if (defaultWidth != itemData.Width || defaultHeight != itemData.Height)
                {
                    AddRequest(itemData, installedModules, generatorHash, IconRenderProfile.CreateDefault(itemData), requests, requestKeys);
                }

                continue;
            }

            AddRequest(itemData, installedModules, generatorHash, IconRenderProfile.CreateSlot(itemData, slotProfile.Width, slotProfile.Height), requests, requestKeys);
        }
    }

    private static void AddRequest(
        ItemData itemData,
        ItemData[] installedModules,
        int generatorHash,
        IconRenderProfile renderProfile,
        ICollection<PrewarmRequest> requests,
        ISet<IconCacheKey> requestKeys)
    {
        IconCacheKey key = BuildCacheKey(itemData, installedModules, generatorHash, renderProfile);

        if (requestKeys.Add(key))
        {
            requests.Add(new PrewarmRequest(itemData, installedModules, renderProfile, key));
        }
    }

    private static void AddWeaponModuleVariants(
        ItemData itemData,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        int generatorHash,
        ICollection<PrewarmRequest> requests,
        ISet<IconCacheKey> requestKeys)
    {
        FirstPersonWeaponModule[] definitions = itemData.IconPrefab.GetComponentsInChildren<FirstPersonWeaponModule>(true);
        List<FirstPersonWeaponModule>[] definitionsBySlot = new List<FirstPersonWeaponModule>[(int)WeaponModuleSlot.StockAdapter + 1];
        Dictionary<ItemData, FirstPersonWeaponModule> uniqueDefinitions = new();

        for (int i = 0; i < definitions.Length; i++)
        {
            FirstPersonWeaponModule definition = definitions[i];
            ItemData moduleItemData = definition == null ? null : definition.ModuleItemData;

            if (moduleItemData == null ||
                moduleItemData.ItemType != ItemType.Module ||
                uniqueDefinitions.ContainsKey(moduleItemData))
            {
                continue;
            }

            uniqueDefinitions.Add(moduleItemData, definition);
        }

        if (uniqueDefinitions.Count == 0)
        {
            return;
        }

        Dictionary<ItemData, FirstPersonWeaponModule> configurationDefinitions = BuildConfigurationDefinitions(itemData, uniqueDefinitions);
        List<ItemData> defaultModules = CollectValidDefaultModules(itemData.DefaultIconModules, uniqueDefinitions);
        HashSet<WeaponModuleSlot> defaultSlots = CollectOccupiedSlots(defaultModules);

        foreach (FirstPersonWeaponModule definition in uniqueDefinitions.Values)
        {
            ItemData moduleItemData = definition.ModuleItemData;
            int slotIndex = (int)moduleItemData.ModuleSlot;

            if (slotIndex <= (int)WeaponModuleSlot.None ||
                slotIndex >= definitionsBySlot.Length ||
                defaultSlots.Contains(moduleItemData.ModuleSlot))
            {
                continue;
            }

            definitionsBySlot[slotIndex] ??= new List<FirstPersonWeaponModule>();
            definitionsBySlot[slotIndex].Add(definition);
        }

        List<ItemData> selectedModules = new();
        List<ItemData> configurationModules = new(defaultModules);
        AddWeaponModuleVariants(itemData, definitionsBySlot, configurationDefinitions, slotProfiles, generatorHash, 1, selectedModules, configurationModules, requests, requestKeys);
    }

    private static void AddWeaponModuleVariants(
        ItemData itemData,
        IReadOnlyList<List<FirstPersonWeaponModule>> definitionsBySlot,
        IReadOnlyDictionary<ItemData, FirstPersonWeaponModule> definitions,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        int generatorHash,
        int slotIndex,
        List<ItemData> selectedModules,
        List<ItemData> configurationModules,
        ICollection<PrewarmRequest> requests,
        ISet<IconCacheKey> requestKeys)
    {
        if (slotIndex >= definitionsBySlot.Count)
        {
            if (selectedModules.Count > 0 && AreModuleConfigurationsSatisfied(configurationModules, definitions))
            {
                AddVariantRequests(itemData, selectedModules.ToArray(), slotProfiles, generatorHash, requests, requestKeys);
            }

            return;
        }

        AddWeaponModuleVariants(itemData, definitionsBySlot, definitions, slotProfiles, generatorHash, slotIndex + 1, selectedModules, configurationModules, requests, requestKeys);

        IReadOnlyList<FirstPersonWeaponModule> slotDefinitions = definitionsBySlot[slotIndex];

        if (slotDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < slotDefinitions.Count; i++)
        {
            ItemData moduleItemData = slotDefinitions[i].ModuleItemData;
            selectedModules.Add(moduleItemData);
            configurationModules.Add(moduleItemData);
            AddWeaponModuleVariants(itemData, definitionsBySlot, definitions, slotProfiles, generatorHash, slotIndex + 1, selectedModules, configurationModules, requests, requestKeys);
            configurationModules.RemoveAt(configurationModules.Count - 1);
            selectedModules.RemoveAt(selectedModules.Count - 1);
        }
    }

    private static bool AreModuleConfigurationsSatisfied(IReadOnlyList<ItemData> selectedModules, IReadOnlyDictionary<ItemData, FirstPersonWeaponModule> definitions)
    {
        for (int i = 0; i < selectedModules.Count; i++)
        {
            if (definitions.TryGetValue(selectedModules[i], out FirstPersonWeaponModule definition) == false || definition.ConfigurationSatisfied(selectedModules) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<ItemData, FirstPersonWeaponModule> BuildConfigurationDefinitions(
        ItemData itemData,
        IReadOnlyDictionary<ItemData, FirstPersonWeaponModule> iconDefinitions)
    {
        Dictionary<ItemData, FirstPersonWeaponModule> configurationDefinitions = new(iconDefinitions.Count);

        foreach (KeyValuePair<ItemData, FirstPersonWeaponModule> definition in iconDefinitions)
        {
            configurationDefinitions.Add(definition.Key, definition.Value);
        }

        GameObject weaponPrefab = itemData.FirstPersonWeaponPrefab;

        if (weaponPrefab == null)
        {
            return configurationDefinitions;
        }

        FirstPersonWeaponModule[] weaponDefinitions = weaponPrefab.GetComponentsInChildren<FirstPersonWeaponModule>(true);

        for (int i = 0; i < weaponDefinitions.Length; i++)
        {
            FirstPersonWeaponModule definition = weaponDefinitions[i];
            ItemData moduleItemData = definition == null ? null : definition.ModuleItemData;

            if (moduleItemData != null && configurationDefinitions.ContainsKey(moduleItemData))
            {
                configurationDefinitions[moduleItemData] = definition;
            }
        }

        return configurationDefinitions;
    }

    private static List<ItemData> CollectValidDefaultModules(
        IReadOnlyList<ItemData> configuredDefaultModules,
        IReadOnlyDictionary<ItemData, FirstPersonWeaponModule> definitions)
    {
        List<ItemData> defaultModules = new();

        if (configuredDefaultModules == null)
        {
            return defaultModules;
        }

        for (int i = 0; i < configuredDefaultModules.Count; i++)
        {
            ItemData moduleItemData = configuredDefaultModules[i];

            if (moduleItemData != null &&
                definitions.ContainsKey(moduleItemData) &&
                defaultModules.Contains(moduleItemData) == false)
            {
                defaultModules.Add(moduleItemData);
            }
        }

        return defaultModules;
    }

    private static HashSet<WeaponModuleSlot> CollectOccupiedSlots(IReadOnlyList<ItemData> modules)
    {
        HashSet<WeaponModuleSlot> occupiedSlots = new();

        for (int i = 0; i < modules.Count; i++)
        {
            WeaponModuleSlot moduleSlot = modules[i].ModuleSlot;

            if (moduleSlot != WeaponModuleSlot.None)
            {
                occupiedSlots.Add(moduleSlot);
            }
        }

        return occupiedSlots;
    }

    private static Vector2Int CalculateModuleSizeDelta(IReadOnlyList<ItemData> installedModules)
    {
        Vector2Int sizeDelta = Vector2Int.zero;

        if (installedModules == null)
        {
            return sizeDelta;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            if (installedModules[i] != null)
            {
                sizeDelta += installedModules[i].ModuleInventorySizeDelta;
            }
        }

        return sizeDelta;
    }

    private static ItemData[] CopyModules(IReadOnlyList<ItemData> installedModules)
    {
        if (installedModules == null || installedModules.Count == 0)
        {
            return Array.Empty<ItemData>();
        }

        ItemData[] result = new ItemData[installedModules.Count];

        for (int i = 0; i < installedModules.Count; i++)
        {
            result[i] = installedModules[i];
        }

        return result;
    }

    private static bool IsWeapon(ItemData itemData) => itemData != null && (itemData.ItemType == ItemType.Weapon || itemData.ItemType == ItemType.Pistol);

    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemData> installedModules, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        return BuildCacheKey(itemData, installedModules, settings.BuildHash(), renderProfile);
    }

    private static IconCacheKey BuildCacheKey(ItemData itemData, IReadOnlyList<ItemData> installedModules, int generatorHash, IconRenderProfile renderProfile)
    {
        int iconHash = renderProfile.UseSlotSettings
            ? itemData.BuildSlotIconHash(renderProfile.CellWidth, renderProfile.CellHeight, installedModules)
            : itemData.BuildIconHash(renderProfile.CellWidth, renderProfile.CellHeight, installedModules);

        return new IconCacheKey(itemData.GetInstanceID(), iconHash, generatorHash);
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
        if (itemData.FallbackIcon != null)
        {
            _cache[key] = IconCacheEntry.CreateFallback(itemData.FallbackIcon);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Clear();
        _cache = new Dictionary<IconCacheKey, IconCacheEntry>();
    }

    private readonly struct PrewarmRequest
    {
        public PrewarmRequest(ItemData itemData, ItemData[] installedModules, IconRenderProfile renderProfile, IconCacheKey key)
        {
            ItemData = itemData;
            InstalledModules = installedModules;
            RenderProfile = renderProfile;
            Key = key;
        }

        public ItemData ItemData { get; }
        public ItemData[] InstalledModules { get; }
        public IconRenderProfile RenderProfile { get; }
        public IconCacheKey Key { get; }
    }

    private readonly struct StandaloneGenerationRequest
    {
        public StandaloneGenerationRequest(PrewarmRequest request, ItemIconGeneratorSettings settings, int settingsHash, UniTaskCompletionSource<Sprite> completionSource)
        {
            Request = request;
            Settings = settings;
            SettingsHash = settingsHash;
            CompletionSource = completionSource;
        }

        public PrewarmRequest Request { get; }
        public ItemIconGeneratorSettings Settings { get; }
        public int SettingsHash { get; }
        public UniTaskCompletionSource<Sprite> CompletionSource { get; }
    }
}

[Serializable]
internal struct ItemIconSlotProfile : IEquatable<ItemIconSlotProfile>
{
    [SerializeField] private bool _restrictItemType;
    [SerializeField] private ItemType _acceptedItemType;
    [SerializeField] private int _width;
    [SerializeField] private int _height;

    public ItemIconSlotProfile(bool restrictItemType, ItemType acceptedItemType, int width, int height)
    {
        _restrictItemType = restrictItemType;
        _acceptedItemType = restrictItemType ? acceptedItemType : ItemType.Misc;
        _width = Mathf.Max(1, width);
        _height = Mathf.Max(1, height);
    }

    public int Width => Mathf.Max(1, _width);
    public int Height => Mathf.Max(1, _height);

    public bool Accepts(ItemData itemData) => itemData != null && (_restrictItemType == false || itemData.ItemType == _acceptedItemType);
    public bool Equals(ItemIconSlotProfile other) => _restrictItemType == other._restrictItemType && _acceptedItemType == other._acceptedItemType && Width == other.Width && Height == other.Height;
    public override bool Equals(object obj) => obj is ItemIconSlotProfile other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_restrictItemType, _acceptedItemType, Width, Height);
}
