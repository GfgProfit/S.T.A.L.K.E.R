using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class ItemIconVariantEnumerator
{
    public const int LARGE_WEAPON_VARIANT_WARNING = 256;

    public static ItemIconVariantAnalysis Enumerate(IReadOnlyList<ItemData> items, ItemIconGeneratorSettings settings)
    {
        ItemIconVariantAnalysis analysis = new();

        for (int i = 0; i < items.Count; i++)
        {
            ItemData itemData = items[i];

            if (itemData == null || itemData.CanGenerateIcon == false || ItemDataIdValidator.IsValidItemId(itemData.ItemId) == false)
            {
                continue;
            }

            int firstVariantIndex = analysis.Variants.Count;
            AddProfileVariants(itemData, Array.Empty<ItemData>(), settings, settings.PrewarmSlotProfiles, analysis.Variants);

            if (IsWeapon(itemData) && itemData.IconPrefab != null)
            {
                AddWeaponModuleVariants(itemData, settings, settings.PrewarmSlotProfiles, analysis.Variants);
            }

            int itemVariantCount = analysis.Variants.Count - firstVariantIndex;
            analysis.VariantCountsByItem.Add(itemData, itemVariantCount);

            if (itemVariantCount > LARGE_WEAPON_VARIANT_WARNING)
            {
                analysis.ExcessiveVariantItems.Add(itemData);
            }
        }

        analysis.Variants.Sort(ItemIconBakeVariant.Compare);
        return analysis;
    }

    public static ItemIconVariantAnalysis Enumerate(ItemData itemData, ItemIconGeneratorSettings settings)
    {
        return Enumerate(itemData == null ? Array.Empty<ItemData>() : new[] { itemData }, settings);
    }

    private static void AddProfileVariants(
        ItemData itemData,
        ItemData[] installedModules,
        ItemIconGeneratorSettings settings,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        ICollection<ItemIconBakeVariant> variants)
    {
        Vector2Int sizeDelta = CalculateSizeDelta(installedModules);
        int width = Mathf.Max(1, itemData.Width + sizeDelta.x);
        int height = Mathf.Max(1, itemData.Height + sizeDelta.y);
        AddUnique(variants, new ItemIconBakeVariant(itemData, installedModules, width, height, ItemIconProfileType.Default, settings.IconRenderScale));

        if (slotProfiles == null)
        {
            return;
        }

        for (int i = 0; i < slotProfiles.Count; i++)
        {
            ItemIconSlotProfile slotProfile = slotProfiles[i];

            if (slotProfile.Accepts(itemData))
            {
                AddUnique(variants, new ItemIconBakeVariant(itemData, installedModules, slotProfile.Width, slotProfile.Height, ItemIconProfileType.Slot, settings.IconRenderScale));
            }
        }
    }

    private static void AddWeaponModuleVariants(
        ItemData itemData,
        ItemIconGeneratorSettings settings,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        ICollection<ItemIconBakeVariant> variants)
    {
        FirstPersonWeaponModule[] iconDefinitions = itemData.IconPrefab.GetComponentsInChildren<FirstPersonWeaponModule>(true);
        Dictionary<string, FirstPersonWeaponModule> uniqueDefinitions = new(StringComparer.Ordinal);

        for (int i = 0; i < iconDefinitions.Length; i++)
        {
            FirstPersonWeaponModule definition = iconDefinitions[i];
            ItemData module = definition == null ? null : definition.ModuleItemData;

            if (module != null && module.ItemType == ItemType.Module && ItemDataIdValidator.IsValidItemId(module.ItemId) && uniqueDefinitions.ContainsKey(module.ItemId) == false)
            {
                uniqueDefinitions.Add(module.ItemId, definition);
            }
        }

        if (uniqueDefinitions.Count == 0)
        {
            return;
        }

        Dictionary<string, FirstPersonWeaponModule> configurationDefinitions = BuildConfigurationDefinitions(itemData, uniqueDefinitions);
        List<ItemData> defaultModules = CollectValidDefaultModules(itemData.DefaultIconModules, uniqueDefinitions);
        HashSet<WeaponModuleSlot> occupiedDefaultSlots = CollectOccupiedSlots(defaultModules);
        SortedDictionary<int, List<FirstPersonWeaponModule>> definitionsBySlot = new();

        foreach (FirstPersonWeaponModule definition in uniqueDefinitions.Values)
        {
            ItemData module = definition.ModuleItemData;
            int slotIndex = (int)module.ModuleSlot;

            if (module.ModuleSlot == WeaponModuleSlot.None || occupiedDefaultSlots.Contains(module.ModuleSlot))
            {
                continue;
            }

            if (definitionsBySlot.TryGetValue(slotIndex, out List<FirstPersonWeaponModule> slotDefinitions) == false)
            {
                slotDefinitions = new List<FirstPersonWeaponModule>();
                definitionsBySlot.Add(slotIndex, slotDefinitions);
            }

            slotDefinitions.Add(definition);
        }

        List<List<FirstPersonWeaponModule>> orderedSlots = new(definitionsBySlot.Values);

        for (int i = 0; i < orderedSlots.Count; i++)
        {
            orderedSlots[i].Sort((left, right) => string.CompareOrdinal(left.ModuleItemData.ItemId, right.ModuleItemData.ItemId));
        }

        List<ItemData> selectedModules = new();
        List<ItemData> effectiveModules = new(defaultModules);
        EnumerateSlot(0, itemData, orderedSlots, configurationDefinitions, settings, slotProfiles, selectedModules, effectiveModules, variants);
    }

    private static void EnumerateSlot(
        int slotIndex,
        ItemData itemData,
        IReadOnlyList<List<FirstPersonWeaponModule>> orderedSlots,
        IReadOnlyDictionary<string, FirstPersonWeaponModule> configurationDefinitions,
        ItemIconGeneratorSettings settings,
        IReadOnlyList<ItemIconSlotProfile> slotProfiles,
        List<ItemData> selectedModules,
        List<ItemData> effectiveModules,
        ICollection<ItemIconBakeVariant> variants)
    {
        if (slotIndex >= orderedSlots.Count)
        {
            if (selectedModules.Count > 0 && ConfigurationSatisfied(effectiveModules, configurationDefinitions))
            {
                ItemData[] modules = selectedModules.ToArray();
                Array.Sort(modules, (left, right) => string.CompareOrdinal(left.ItemId, right.ItemId));
                AddProfileVariants(itemData, modules, settings, slotProfiles, variants);
            }

            return;
        }

        EnumerateSlot(slotIndex + 1, itemData, orderedSlots, configurationDefinitions, settings, slotProfiles, selectedModules, effectiveModules, variants);

        IReadOnlyList<FirstPersonWeaponModule> slotDefinitions = orderedSlots[slotIndex];

        for (int i = 0; i < slotDefinitions.Count; i++)
        {
            ItemData module = slotDefinitions[i].ModuleItemData;
            selectedModules.Add(module);
            effectiveModules.Add(module);
            EnumerateSlot(slotIndex + 1, itemData, orderedSlots, configurationDefinitions, settings, slotProfiles, selectedModules, effectiveModules, variants);
            effectiveModules.RemoveAt(effectiveModules.Count - 1);
            selectedModules.RemoveAt(selectedModules.Count - 1);
        }
    }

    private static bool ConfigurationSatisfied(IReadOnlyList<ItemData> modules, IReadOnlyDictionary<string, FirstPersonWeaponModule> definitions)
    {
        for (int i = 0; i < modules.Count; i++)
        {
            ItemData module = modules[i];

            if (module == null || definitions.TryGetValue(module.ItemId, out FirstPersonWeaponModule definition) == false || definition.ConfigurationSatisfied(modules) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, FirstPersonWeaponModule> BuildConfigurationDefinitions(
        ItemData itemData,
        IReadOnlyDictionary<string, FirstPersonWeaponModule> iconDefinitions)
    {
        Dictionary<string, FirstPersonWeaponModule> result = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, FirstPersonWeaponModule> pair in iconDefinitions)
        {
            result.Add(pair.Key, pair.Value);
        }
        GameObject weaponPrefab = itemData.FirstPersonWeaponPrefab;

        if (weaponPrefab == null)
        {
            return result;
        }

        FirstPersonWeaponModule[] weaponDefinitions = weaponPrefab.GetComponentsInChildren<FirstPersonWeaponModule>(true);

        for (int i = 0; i < weaponDefinitions.Length; i++)
        {
            FirstPersonWeaponModule definition = weaponDefinitions[i];
            ItemData module = definition == null ? null : definition.ModuleItemData;

            if (module != null && result.ContainsKey(module.ItemId))
            {
                result[module.ItemId] = definition;
            }
        }

        return result;
    }

    private static List<ItemData> CollectValidDefaultModules(
        IReadOnlyList<ItemData> configuredModules,
        IReadOnlyDictionary<string, FirstPersonWeaponModule> definitions)
    {
        List<ItemData> modules = new();

        if (configuredModules == null)
        {
            return modules;
        }

        for (int i = 0; i < configuredModules.Count; i++)
        {
            ItemData module = configuredModules[i];

            if (module != null && definitions.ContainsKey(module.ItemId) && modules.Contains(module) == false)
            {
                modules.Add(module);
            }
        }

        return modules;
    }

    private static HashSet<WeaponModuleSlot> CollectOccupiedSlots(IReadOnlyList<ItemData> modules)
    {
        HashSet<WeaponModuleSlot> slots = new();

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i].ModuleSlot != WeaponModuleSlot.None)
            {
                slots.Add(modules[i].ModuleSlot);
            }
        }

        return slots;
    }

    private static Vector2Int CalculateSizeDelta(IReadOnlyList<ItemData> modules)
    {
        Vector2Int delta = Vector2Int.zero;

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] != null)
            {
                delta += modules[i].ModuleInventorySizeDelta;
            }
        }

        return delta;
    }

    private static void AddUnique(ICollection<ItemIconBakeVariant> variants, ItemIconBakeVariant candidate)
    {
        foreach (ItemIconBakeVariant variant in variants)
        {
            if (variant.Equals(candidate))
            {
                return;
            }
        }

        variants.Add(candidate);
    }

    private static bool IsWeapon(ItemData itemData) => itemData.ItemType == ItemType.Weapon || itemData.ItemType == ItemType.Pistol;
}

internal sealed class ItemIconVariantAnalysis
{
    public readonly List<ItemIconBakeVariant> Variants = new();
    public readonly Dictionary<ItemData, int> VariantCountsByItem = new();
    public readonly List<ItemData> ExcessiveVariantItems = new();

    public int DefaultVariantCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < Variants.Count; i++)
            {
                count += Variants[i].ProfileType == ItemIconProfileType.Default ? 1 : 0;
            }

            return count;
        }
    }

    public int SlotVariantCount => Variants.Count - DefaultVariantCount;
    public long EstimatedTextureBytes
    {
        get
        {
            long bytes = 0L;

            for (int i = 0; i < Variants.Count; i++)
            {
                bytes += Variants[i].EstimatedTextureBytes;
            }

            return bytes;
        }
    }
}

internal sealed class ItemIconBakeVariant : IEquatable<ItemIconBakeVariant>
{
    public ItemIconBakeVariant(ItemData itemData, ItemData[] installedModules, int width, int height, ItemIconProfileType profileType, int iconRenderScale)
    {
        ItemData = itemData;
        InstalledModules = installedModules ?? Array.Empty<ItemData>();
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        ProfileType = profileType;
        IconRenderScale = Mathf.Clamp(iconRenderScale, 1, 4);
    }

    public ItemData ItemData { get; }
    public ItemData[] InstalledModules { get; }
    public int Width { get; }
    public int Height { get; }
    public ItemIconProfileType ProfileType { get; }
    public int IconRenderScale { get; }
    public long EstimatedTextureBytes => (long)Width * ItemData.IconPixelsPerCell * IconRenderScale * Height * ItemData.IconPixelsPerCell * IconRenderScale * 4L;
    public string DisplayName => $"{ItemData.ItemName} | {ProfileType} {Width}x{Height} | {GetModuleLabel()}";

    public bool Equals(ItemIconBakeVariant other)
    {
        if (other == null || ItemData != other.ItemData || Width != other.Width || Height != other.Height || ProfileType != other.ProfileType || InstalledModules.Length != other.InstalledModules.Length)
        {
            return false;
        }

        for (int i = 0; i < InstalledModules.Length; i++)
        {
            if (string.Equals(InstalledModules[i].ItemId, other.InstalledModules[i].ItemId, StringComparison.Ordinal) == false)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj) => Equals(obj as ItemIconBakeVariant);

    public override int GetHashCode()
    {
        BakedItemIconEntry entry = BakedItemIconEntry.Create(ItemData, InstalledModules, Width, Height, ProfileType, ItemIconGeneratorSettings.LoadDefault(), string.Empty, string.Empty, string.Empty, 0L);
        return unchecked((int)(entry.StableKeyHash ^ (entry.StableKeyHash >> 32)));
    }

    public static int Compare(ItemIconBakeVariant left, ItemIconBakeVariant right)
    {
        int itemComparison = string.CompareOrdinal(AssetDatabase.GetAssetPath(left.ItemData), AssetDatabase.GetAssetPath(right.ItemData));

        if (itemComparison != 0)
        {
            return itemComparison;
        }

        int profileComparison = left.ProfileType.CompareTo(right.ProfileType);

        if (profileComparison != 0)
        {
            return profileComparison;
        }

        int widthComparison = left.Width.CompareTo(right.Width);
        return widthComparison != 0 ? widthComparison : left.Height.CompareTo(right.Height);
    }

    private string GetModuleLabel()
    {
        if (InstalledModules.Length == 0)
        {
            return "base";
        }

        string[] names = new string[InstalledModules.Length];

        for (int i = 0; i < InstalledModules.Length; i++)
        {
            names[i] = InstalledModules[i].ShortName.Length > 0 ? InstalledModules[i].ShortName : InstalledModules[i].ItemName;
        }

        return string.Join(" + ", names);
    }
}
