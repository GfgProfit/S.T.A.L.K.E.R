using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal static class ItemIconModuleKeyCache
{
    private static readonly ConditionalWeakTable<object, CachedModuleKey> _cache = new();

    public static string GetCanonicalKey(IReadOnlyList<ItemData> modules)
    {
        if (modules == null || modules.Count == 0)
        {
            return string.Empty;
        }

        object owner = modules;

        if (_cache.TryGetValue(owner, out CachedModuleKey cached) && cached.Matches(modules))
        {
            return cached.CanonicalKey;
        }

        string[] normalizedIds = BuildNormalizedIds(modules);
        CachedModuleKey replacement = new(normalizedIds);
        _cache.Remove(owner);
        _cache.Add(owner, replacement);
        return replacement.CanonicalKey;
    }

    public static string[] BuildNormalizedIds(IReadOnlyList<ItemData> modules)
    {
        if (modules == null || modules.Count == 0)
        {
            return Array.Empty<string>();
        }

        List<string> ids = new(modules.Count);

        for (int i = 0; i < modules.Count; i++)
        {
            string itemId = modules[i] == null ? string.Empty : modules[i].ItemId;

            if (string.IsNullOrEmpty(itemId) == false && ids.Contains(itemId) == false)
            {
                ids.Add(itemId);
            }
        }

        ids.Sort(StringComparer.Ordinal);
        return ids.ToArray();
    }

    public static string BuildCanonicalKey(IReadOnlyList<string> normalizedIds)
    {
        return normalizedIds == null || normalizedIds.Count == 0
            ? string.Empty
            : string.Join("|", normalizedIds);
    }

    private sealed class CachedModuleKey
    {
        private readonly string[] _normalizedIds;

        public CachedModuleKey(string[] normalizedIds)
        {
            _normalizedIds = normalizedIds;
            CanonicalKey = BuildCanonicalKey(normalizedIds);
        }

        public string CanonicalKey { get; }

        public bool Matches(IReadOnlyList<ItemData> modules)
        {
            if (modules == null)
            {
                return _normalizedIds.Length == 0;
            }

            int uniqueCount = 0;

            for (int i = 0; i < modules.Count; i++)
            {
                string itemId = modules[i] == null ? string.Empty : modules[i].ItemId;

                if (string.IsNullOrEmpty(itemId))
                {
                    continue;
                }

                bool duplicate = false;

                for (int previousIndex = 0; previousIndex < i; previousIndex++)
                {
                    ItemData previous = modules[previousIndex];

                    if (previous != null && string.Equals(previous.ItemId, itemId, StringComparison.Ordinal))
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate)
                {
                    continue;
                }

                uniqueCount++;

                if (Array.BinarySearch(_normalizedIds, itemId, StringComparer.Ordinal) < 0)
                {
                    return false;
                }
            }

            return uniqueCount == _normalizedIds.Length;
        }
    }
}
