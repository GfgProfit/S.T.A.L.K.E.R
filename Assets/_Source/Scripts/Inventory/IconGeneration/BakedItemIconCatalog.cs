using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = DEFAULT_RESOURCE_PATH, menuName = "Inventory/Baked Item Icon Catalog")]
public sealed class BakedItemIconCatalog : ScriptableObject
{
    public const string DEFAULT_RESOURCE_PATH = "BakedItemIconCatalog";

    private static BakedItemIconCatalog _defaultCatalog;
    private static bool _defaultCatalogLoaded;

    [SerializeField] private List<BakedItemIconEntry> _entries = new();

    private Dictionary<IconCacheKey, BakedItemIconEntry> _lookup;

    public IReadOnlyList<BakedItemIconEntry> Entries => _entries;

    internal bool TryGetSprite(IconCacheKey key, out Sprite sprite)
    {
        EnsureLookup();

        if (_lookup.TryGetValue(key, out BakedItemIconEntry entry) == false)
        {
            sprite = null;
            return false;
        }

        return entry.TryLoadSprite(out sprite);
    }

    public void ReplaceEntries(IReadOnlyList<BakedItemIconEntry> entries)
    {
        _entries.Clear();

        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                {
                    _entries.Add(entries[i]);
                }
            }
        }

        _lookup = null;
    }

    public void UpsertEntries(IReadOnlyList<BakedItemIconEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        Dictionary<IconCacheKey, int> existingIndices = new();

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null && _entries[i].TryBuildKey(out IconCacheKey key))
            {
                existingIndices[key] = i;
            }
        }

        for (int i = 0; i < entries.Count; i++)
        {
            BakedItemIconEntry entry = entries[i];

            if (entry == null || entry.TryBuildKey(out IconCacheKey key) == false)
            {
                continue;
            }

            if (existingIndices.TryGetValue(key, out int index))
            {
                _entries[index] = entry;
            }
            else
            {
                existingIndices.Add(key, _entries.Count);
                _entries.Add(entry);
            }
        }

        _lookup = null;
    }

    public void ClearEntries()
    {
        _entries.Clear();
        _lookup = null;
    }

    public static BakedItemIconCatalog LoadDefault()
    {
        if (_defaultCatalogLoaded)
        {
            return _defaultCatalog;
        }

        _defaultCatalogLoaded = true;
        _defaultCatalog = Resources.Load<BakedItemIconCatalog>(DEFAULT_RESOURCE_PATH);
        return _defaultCatalog;
    }

    public static void ResetDefaultCache()
    {
        _defaultCatalogLoaded = false;
        _defaultCatalog = null;
    }

    private void OnEnable() => _lookup = null;
    private void OnValidate() => _lookup = null;

    private void EnsureLookup()
    {
        if (_lookup != null)
        {
            return;
        }

        _lookup = new Dictionary<IconCacheKey, BakedItemIconEntry>(_entries.Count);

        for (int i = 0; i < _entries.Count; i++)
        {
            BakedItemIconEntry entry = _entries[i];

            if (entry != null && entry.TryBuildKey(out IconCacheKey key) && _lookup.ContainsKey(key) == false)
            {
                _lookup.Add(key, entry);
            }
        }
    }
}

[Serializable]
public sealed class BakedItemIconEntry
{
    [SerializeField] private string _itemId;
    [SerializeField] private List<string> _moduleItemIds = new();
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private ItemIconProfileType _profileType;
    [SerializeField] private string _visualSignature;
    [SerializeField] private string _spriteResourcePath;
    [SerializeField] private string _bakeSignature;
    [SerializeField] private string _sourceItemAssetGuid;
    [SerializeField] private long _estimatedTextureBytes;

    [NonSerialized] private string _canonicalModuleItemIds;
    [NonSerialized] private Sprite _loadedSprite;

    public BakedItemIconEntry(
        string itemId,
        IReadOnlyList<string> moduleItemIds,
        int width,
        int height,
        ItemIconProfileType profileType,
        ulong visualSignature,
        string spriteResourcePath,
        string bakeSignature,
        string sourceItemAssetGuid,
        long estimatedTextureBytes)
    {
        _itemId = itemId ?? string.Empty;
        _moduleItemIds = moduleItemIds == null ? new List<string>() : new List<string>(moduleItemIds);
        _moduleItemIds.Sort(StringComparer.Ordinal);
        _width = Mathf.Max(1, width);
        _height = Mathf.Max(1, height);
        _profileType = profileType;
        _visualSignature = ItemIconStableHash.ToHex(visualSignature);
        _spriteResourcePath = spriteResourcePath ?? string.Empty;
        _bakeSignature = bakeSignature ?? string.Empty;
        _sourceItemAssetGuid = sourceItemAssetGuid ?? string.Empty;
        _estimatedTextureBytes = Math.Max(0L, estimatedTextureBytes);
    }

    public string ItemId => _itemId ?? string.Empty;
    public IReadOnlyList<string> ModuleItemIds => _moduleItemIds;
    public int Width => Mathf.Max(1, _width);
    public int Height => Mathf.Max(1, _height);
    public ItemIconProfileType ProfileType => _profileType;
    public string VisualSignature => _visualSignature ?? string.Empty;
    public string SpriteResourcePath => _spriteResourcePath ?? string.Empty;
    public string BakeSignature => _bakeSignature ?? string.Empty;
    public string SourceItemAssetGuid => _sourceItemAssetGuid ?? string.Empty;
    public long EstimatedTextureBytes => Math.Max(0L, _estimatedTextureBytes);
    public ulong StableKeyHash => TryBuildKey(out IconCacheKey key) ? key.StableHash : 0UL;

    public static BakedItemIconEntry Create(
        ItemData itemData,
        IReadOnlyList<ItemData> installedModules,
        int width,
        int height,
        ItemIconProfileType profileType,
        ItemIconGeneratorSettings settings,
        string spriteResourcePath,
        string bakeSignature,
        string sourceItemAssetGuid,
        long estimatedTextureBytes)
    {
        IconRenderProfile renderProfile = profileType == ItemIconProfileType.Slot
            ? IconRenderProfile.CreateSlot(itemData, width, height)
            : IconRenderProfile.CreateDefault(itemData, width, height);
        IconCacheKey key = ItemIconStableKeyBuilder.Build(itemData, installedModules, settings, renderProfile);

        return new BakedItemIconEntry(
            key.ItemId,
            ItemIconModuleKeyCache.BuildNormalizedIds(installedModules),
            key.Width,
            key.Height,
            key.ProfileType,
            key.VisualSignature,
            spriteResourcePath,
            bakeSignature,
            sourceItemAssetGuid,
            estimatedTextureBytes);
    }

    internal bool TryBuildKey(out IconCacheKey key)
    {
        if (string.IsNullOrEmpty(ItemId) || ItemIconStableHash.TryParseHex(VisualSignature, out ulong visualSignature) == false)
        {
            key = default;
            return false;
        }

        _canonicalModuleItemIds ??= ItemIconModuleKeyCache.BuildCanonicalKey(_moduleItemIds);
        key = new IconCacheKey(ItemId, _canonicalModuleItemIds, Width, Height, ProfileType, visualSignature);
        return true;
    }

    internal bool TryLoadSprite(out Sprite sprite)
    {
        if (_loadedSprite == null && string.IsNullOrEmpty(SpriteResourcePath) == false)
        {
            _loadedSprite = Resources.Load<Sprite>(SpriteResourcePath);
        }

        sprite = _loadedSprite;
        return sprite != null;
    }
}
