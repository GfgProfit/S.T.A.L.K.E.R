using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class ItemIconCatalogValidator
{
    public static ItemIconCatalogValidationResult Validate(bool includeMissingVariants)
    {
        ItemIconCatalogValidationResult result = new();
        List<ItemData> items = ItemDataIdValidator.LoadAllItems();
        ItemDataIdValidationResult idValidation = ItemDataIdValidator.Validate(items);
        result.Errors.AddRange(idValidation.Errors);

        Dictionary<string, ItemData> itemsById = new(StringComparer.Ordinal);

        for (int i = 0; i < items.Count; i++)
        {
            if (ItemDataIdValidator.IsValidItemId(items[i].ItemId) && itemsById.ContainsKey(items[i].ItemId) == false)
            {
                itemsById.Add(items[i].ItemId, items[i]);
            }
        }

        BakedItemIconCatalog catalog = AssetDatabase.LoadAssetAtPath<BakedItemIconCatalog>(ItemIconGeneratedAssetWriter.CATALOG_ASSET_PATH);

        if (catalog == null)
        {
            result.Errors.Add($"Baked catalog is missing: {ItemIconGeneratedAssetWriter.CATALOG_ASSET_PATH}");
            return result;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorWindow.LoadOrCreateDefaultSettings();
        Dictionary<string, BakedItemIconEntry> entriesByIdentity = new(StringComparer.Ordinal);
        Dictionary<ulong, string> identitiesByHash = new();
        bool verifiedDefaultLookup = false;
        bool verifiedSlotLookup = false;
        bool verifiedModuleLookup = false;

        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            BakedItemIconEntry entry = catalog.Entries[i];

            if (entry == null)
            {
                result.Errors.Add($"Catalog entry {i} is null.");
                continue;
            }

            string identity = ItemIconBakeSignatureBuilder.BuildIdentity(entry);

            if (entriesByIdentity.ContainsKey(identity))
            {
                result.Errors.Add($"Duplicate catalog entry: {identity}");
            }
            else
            {
                entriesByIdentity.Add(identity, entry);
            }

            if (identitiesByHash.TryGetValue(entry.StableKeyHash, out string existingIdentity) && string.Equals(existingIdentity, identity, StringComparison.Ordinal) == false)
            {
                result.Errors.Add($"Stable key hash collision: {entry.StableKeyHash:x16} maps to both '{existingIdentity}' and '{identity}'.");
            }
            else
            {
                identitiesByHash[entry.StableKeyHash] = identity;
            }

            if (itemsById.TryGetValue(entry.ItemId, out ItemData itemData) == false)
            {
                result.Errors.Add($"Catalog entry references unknown ItemId '{entry.ItemId}'.");
                continue;
            }

            ItemData[] modules = ResolveModules(entry.ModuleItemIds, itemsById, result, identity);

            if (modules == null)
            {
                continue;
            }

            string assetPath = ItemIconGeneratedAssetWriter.ResourcePathToAssetPath(entry.SpriteResourcePath);

            Sprite bakedSprite = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (bakedSprite == null)
            {
                result.Errors.Add($"Generated Sprite is missing for '{identity}': {assetPath}");
            }

            ItemIconBakeVariant variant = new(itemData, modules, entry.Width, entry.Height, entry.ProfileType, settings.IconRenderScale);
            BakedItemIconEntry expectedEntry = BakedItemIconEntry.Create(
                itemData,
                modules,
                entry.Width,
                entry.Height,
                entry.ProfileType,
                settings,
                entry.SpriteResourcePath,
                string.Empty,
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(itemData)),
                variant.EstimatedTextureBytes);

            if (string.Equals(entry.VisualSignature, expectedEntry.VisualSignature, StringComparison.Ordinal) == false)
            {
                result.Errors.Add($"Catalog entry has a stale runtime signature: {identity}");
            }

            string expectedBakeSignature = ItemIconBakeSignatureBuilder.Build(variant, settings);

            if (string.Equals(entry.BakeSignature, expectedBakeSignature, StringComparison.Ordinal) == false)
            {
                result.Errors.Add($"Catalog entry has stale asset dependencies: {identity}");
            }

            string currentGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(itemData));

            if (string.Equals(entry.SourceItemAssetGuid, currentGuid, StringComparison.Ordinal) == false)
            {
                result.Errors.Add($"Catalog entry source reference is stale for ItemId '{entry.ItemId}'.");
            }

            bool shouldVerifyRuntimeLookup = bakedSprite != null &&
                                             ((entry.ModuleItemIds.Count > 0 && verifiedModuleLookup == false) ||
                                              (entry.ModuleItemIds.Count == 0 && entry.ProfileType == ItemIconProfileType.Default && verifiedDefaultLookup == false) ||
                                              (entry.ModuleItemIds.Count == 0 && entry.ProfileType == ItemIconProfileType.Slot && verifiedSlotLookup == false));

            if (shouldVerifyRuntimeLookup)
            {
                Sprite runtimeSprite = entry.ProfileType == ItemIconProfileType.Slot
                    ? itemData.GetSlotIcon(entry.Width, entry.Height, modules)
                    : itemData.GetIcon(entry.Width, entry.Height, modules);

                if (runtimeSprite != bakedSprite)
                {
                    result.Errors.Add($"Runtime baked lookup does not resolve the expected Sprite: {identity}");
                }

                verifiedModuleLookup |= entry.ModuleItemIds.Count > 0;
                verifiedDefaultLookup |= entry.ModuleItemIds.Count == 0 && entry.ProfileType == ItemIconProfileType.Default;
                verifiedSlotLookup |= entry.ModuleItemIds.Count == 0 && entry.ProfileType == ItemIconProfileType.Slot;
            }
        }

        if (includeMissingVariants && idValidation.IsValid)
        {
            ItemIconVariantAnalysis analysis = ItemIconVariantEnumerator.Enumerate(items, settings);

            for (int i = 0; i < analysis.Variants.Count; i++)
            {
                ItemIconBakeVariant variant = analysis.Variants[i];
                BakedItemIconEntry expectedEntry = BakedItemIconEntry.Create(
                    variant.ItemData,
                    variant.InstalledModules,
                    variant.Width,
                    variant.Height,
                    variant.ProfileType,
                    settings,
                    string.Empty,
                    string.Empty,
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variant.ItemData)),
                    variant.EstimatedTextureBytes);
                string identity = ItemIconBakeSignatureBuilder.BuildIdentity(expectedEntry);

                if (entriesByIdentity.ContainsKey(identity) == false)
                {
                    result.Errors.Add($"Missing baked icon variant: {variant.DisplayName}");
                }
            }
        }

        result.EntryCount = catalog.Entries.Count;
        return result;
    }

    [MenuItem("Tools/Inventory/Item Icons/Validate Catalog")]
    private static void ValidateMenu()
    {
        ItemIconCatalogValidationResult result = Validate(true);

        if (result.IsValid)
        {
            Debug.Log($"Baked item icon catalog validation passed: {result.EntryCount} entries.");
        }
        else
        {
            Debug.LogError($"Baked item icon catalog validation failed with {result.Errors.Count} issue(s):\n{string.Join("\n", result.Errors)}");
        }
    }

    private static ItemData[] ResolveModules(
        IReadOnlyList<string> moduleItemIds,
        IReadOnlyDictionary<string, ItemData> itemsById,
        ItemIconCatalogValidationResult result,
        string identity)
    {
        ItemData[] modules = new ItemData[moduleItemIds.Count];

        for (int i = 0; i < moduleItemIds.Count; i++)
        {
            if (itemsById.TryGetValue(moduleItemIds[i], out ItemData module) == false || module.ItemType != ItemType.Module)
            {
                result.Errors.Add($"Catalog entry '{identity}' references unknown module ItemId '{moduleItemIds[i]}'.");
                return null;
            }

            modules[i] = module;
        }

        return modules;
    }
}

internal sealed class ItemIconCatalogValidationResult
{
    public readonly List<string> Errors = new();
    public int EntryCount;
    public bool IsValid => Errors.Count == 0;
}
