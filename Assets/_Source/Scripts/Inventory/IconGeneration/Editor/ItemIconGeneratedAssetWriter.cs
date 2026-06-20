using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class ItemIconGeneratedAssetWriter
{
    public const string CATALOG_ASSET_PATH = "Assets/Resources/BakedItemIconCatalog.asset";
    public const string GENERATED_FOLDER_PATH = "Assets/Resources/Generated/ItemIcons";
    private const string GENERATED_RESOURCE_PREFIX = "Generated/ItemIcons/";

    public static BakedItemIconCatalog LoadOrCreateCatalog()
    {
        EnsureFolder("Assets/Resources");
        BakedItemIconCatalog catalog = AssetDatabase.LoadAssetAtPath<BakedItemIconCatalog>(CATALOG_ASSET_PATH);

        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<BakedItemIconCatalog>();
        AssetDatabase.CreateAsset(catalog, CATALOG_ASSET_PATH);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    public static string WriteTexture(Texture2D texture, BakedItemIconEntry keyEntry, ItemData itemData)
    {
        EnsureFolder(GENERATED_FOLDER_PATH);
        string fileName = $"{keyEntry.ItemId}_{keyEntry.StableKeyHash:x16}.png";
        string assetPath = $"{GENERATED_FOLDER_PATH}/{fileName}";
        string fullPath = Path.GetFullPath(assetPath);
        byte[] pngBytes = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngBytes);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = itemData.IconSpritePixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = GetMaxTextureSize(texture.width, texture.height);
            importer.SaveAndReimport();
        }

        return GENERATED_RESOURCE_PREFIX + Path.GetFileNameWithoutExtension(fileName);
    }

    public static void ClearGeneratedAssets()
    {
        BakedItemIconCatalog catalog = LoadOrCreateCatalog();
        Undo.RecordObject(catalog, "Clear Generated Item Icons");
        catalog.ClearEntries();
        EditorUtility.SetDirty(catalog);

        if (AssetDatabase.IsValidFolder(GENERATED_FOLDER_PATH))
        {
            string[] generatedAssetGuids = AssetDatabase.FindAssets(string.Empty, new[] { GENERATED_FOLDER_PATH });
            List<string> generatedAssetPaths = new(generatedAssetGuids.Length);

            for (int i = 0; i < generatedAssetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(generatedAssetGuids[i]);

                if (string.IsNullOrEmpty(assetPath) == false && string.Equals(assetPath, GENERATED_FOLDER_PATH, StringComparison.Ordinal) == false)
                {
                    generatedAssetPaths.Add(assetPath);
                }
            }

            generatedAssetPaths.Sort((left, right) => right.Length.CompareTo(left.Length));

            for (int i = 0; i < generatedAssetPaths.Count; i++)
            {
                AssetDatabase.DeleteAsset(generatedAssetPaths[i]);
            }
        }

        AssetDatabase.SaveAssets();
        BakedItemIconCatalog.ResetDefaultCache();
        ItemIconCache.Clear();
    }

    public static void DeleteGeneratedAssetsExcept(IReadOnlyList<BakedItemIconEntry> retainedEntries)
    {
        if (AssetDatabase.IsValidFolder(GENERATED_FOLDER_PATH) == false)
        {
            return;
        }

        HashSet<string> retainedPaths = new(StringComparer.Ordinal);

        if (retainedEntries != null)
        {
            for (int i = 0; i < retainedEntries.Count; i++)
            {
                if (retainedEntries[i] != null)
                {
                    retainedPaths.Add(ResourcePathToAssetPath(retainedEntries[i].SpriteResourcePath));
                }
            }
        }

        string[] generatedAssetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { GENERATED_FOLDER_PATH });

        for (int i = 0; i < generatedAssetGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(generatedAssetGuids[i]);

            if (string.IsNullOrEmpty(assetPath) == false && retainedPaths.Contains(assetPath) == false)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }

    public static string ResourcePathToAssetPath(string resourcePath)
    {
        return string.IsNullOrEmpty(resourcePath) ? string.Empty : $"Assets/Resources/{resourcePath}.png";
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalizedPath = folderPath.Replace('\\', '/');
        string[] segments = normalizedPath.Split('/');
        string currentPath = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = $"{currentPath}/{segments[i]}";

            if (AssetDatabase.IsValidFolder(nextPath) == false)
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private static int GetMaxTextureSize(int width, int height)
    {
        int requiredSize = Mathf.NextPowerOfTwo(Mathf.Max(width, height));
        return Mathf.Clamp(requiredSize, 32, 8192);
    }
}
