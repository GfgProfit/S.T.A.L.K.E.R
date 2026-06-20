using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal static class ItemDataIdValidator
{
    private static readonly Regex _validIdPattern = new("^[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static List<ItemData> LoadAllItems()
    {
        string[] assetGuids = AssetDatabase.FindAssets("t:ItemData");
        List<ItemData> items = new(assetGuids.Length);

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemData != null)
            {
                items.Add(itemData);
            }
        }

        items.Sort((left, right) => string.CompareOrdinal(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right)));
        return items;
    }

    public static ItemDataIdValidationResult Validate(IReadOnlyList<ItemData> items)
    {
        ItemDataIdValidationResult result = new();
        Dictionary<string, List<ItemData>> itemsById = new(StringComparer.Ordinal);

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            string assetPath = item == null ? string.Empty : AssetDatabase.GetAssetPath(item);

            if (item == null || string.IsNullOrEmpty(assetPath) || AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != item)
            {
                result.Errors.Add($"ItemData reference is invalid: {assetPath}");
                continue;
            }

            if (string.IsNullOrEmpty(item.ItemId))
            {
                result.MissingIds.Add(item);
                result.Errors.Add($"Missing ItemId: {assetPath}");
                continue;
            }

            if (IsValidItemId(item.ItemId) == false)
            {
                result.MalformedIds.Add(item);
                result.Errors.Add($"Malformed ItemId '{item.ItemId}': {assetPath}");
                continue;
            }

            if (itemsById.TryGetValue(item.ItemId, out List<ItemData> duplicates) == false)
            {
                duplicates = new List<ItemData>();
                itemsById.Add(item.ItemId, duplicates);
            }

            duplicates.Add(item);
        }

        foreach (KeyValuePair<string, List<ItemData>> pair in itemsById)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }

            result.Duplicates.Add(pair.Key, pair.Value);
            result.Errors.Add($"Duplicate ItemId '{pair.Key}': {string.Join(", ", pair.Value.ConvertAll(AssetDatabase.GetAssetPath))}");
        }

        result.ValidItemCount = items.Count - result.MissingIds.Count - result.MalformedIds.Count;
        return result;
    }

    public static bool IsValidItemId(string itemId) => string.IsNullOrEmpty(itemId) == false && _validIdPattern.IsMatch(itemId);

    public static void AssignItemId(ItemData itemData, string itemId, string undoName)
    {
        if (itemData == null || IsValidItemId(itemId) == false)
        {
            return;
        }

        Undo.RecordObject(itemData, undoName);
        SerializedObject serializedItem = new(itemData);
        serializedItem.FindProperty("_itemId").stringValue = itemId;
        serializedItem.ApplyModifiedProperties();
        EditorUtility.SetDirty(itemData);
    }

    [MenuItem("Tools/Inventory/Item IDs/Validate Item IDs")]
    private static void ValidateMenu()
    {
        List<ItemData> items = LoadAllItems();
        ItemDataIdValidationResult result = Validate(items);

        if (result.Errors.Count == 0)
        {
            Debug.Log($"ItemData validation passed: {items.Count} assets, all ItemId values are unique and valid.");
            return;
        }

        Debug.LogError($"ItemData validation failed with {result.Errors.Count} issue(s):\n{string.Join("\n", result.Errors)}");
    }

    [MenuItem("Tools/Inventory/Item IDs/Assign Missing Item IDs")]
    private static void AssignMissingMenu()
    {
        List<ItemData> items = LoadAllItems();
        ItemDataIdValidationResult result = Validate(items);

        for (int i = 0; i < result.MissingIds.Count; i++)
        {
            ItemData itemData = result.MissingIds[i];
            string assetPath = AssetDatabase.GetAssetPath(itemData);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            if (IsValidItemId(assetGuid))
            {
                AssignItemId(itemData, assetGuid, "Assign Missing Item ID");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Assigned {result.MissingIds.Count} missing ItemId value(s). Existing valid IDs were not changed.");
    }

    [MenuItem("Tools/Inventory/Item IDs/Regenerate Selected Duplicate ID", true)]
    private static bool CanRegenerateSelectedDuplicateId()
    {
        return Selection.activeObject is ItemData;
    }

    [MenuItem("Tools/Inventory/Item IDs/Regenerate Selected Duplicate ID")]
    private static void RegenerateSelectedDuplicateId()
    {
        ItemData selectedItem = Selection.activeObject as ItemData;
        List<ItemData> items = LoadAllItems();
        ItemDataIdValidationResult validation = Validate(items);

        if (selectedItem == null || validation.Duplicates.TryGetValue(selectedItem.ItemId, out List<ItemData> duplicates) == false || duplicates.Count < 2)
        {
            EditorUtility.DisplayDialog("Item IDs", "Selected ItemData does not have a duplicate ItemId.", "OK");
            return;
        }

        string assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedItem));

        if (IsValidItemId(assetGuid) == false)
        {
            EditorUtility.DisplayDialog("Item IDs", "Selected ItemData has no valid asset GUID.", "OK");
            return;
        }

        AssignItemId(selectedItem, assetGuid, "Regenerate Duplicate Item ID");
        AssetDatabase.SaveAssets();
        BakedItemIconCatalog.ResetDefaultCache();
        Debug.Log($"Regenerated ItemId for {AssetDatabase.GetAssetPath(selectedItem)}. Related baked entries are now stale until rebaked.", selectedItem);
    }
}

internal sealed class ItemDataIdValidationResult
{
    public readonly List<string> Errors = new();
    public readonly List<ItemData> MissingIds = new();
    public readonly List<ItemData> MalformedIds = new();
    public readonly Dictionary<string, List<ItemData>> Duplicates = new(StringComparer.Ordinal);
    public int ValidItemCount;
    public bool IsValid => Errors.Count == 0;
}
