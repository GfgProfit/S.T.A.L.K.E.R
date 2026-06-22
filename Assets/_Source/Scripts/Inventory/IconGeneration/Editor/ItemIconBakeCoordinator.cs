using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

internal static class ItemIconBakeCoordinator
{
    public static async UniTask<int> BakeAsync(
        IReadOnlyList<ItemIconBakeVariant> variants,
        ItemIconGeneratorSettings settings,
        bool replaceCatalog,
        Action<int, int, ItemIconBakeVariant> progress,
        CancellationToken cancellationToken)
    {
        if (variants == null || variants.Count == 0)
        {
            return 0;
        }

        BakedItemIconCatalog catalog = ItemIconGeneratedAssetWriter.LoadOrCreateCatalog();
        List<BakedItemIconEntry> bakedEntries = new(variants.Count);
        bool completed = false;

        try
        {
            using ItemIconBatchRenderSession renderSession = new(settings);

            for (int i = 0; i < variants.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ItemIconBakeVariant variant = variants[i];
                progress?.Invoke(i, variants.Count, variant);
                string itemAssetPath = AssetDatabase.GetAssetPath(variant.ItemData);
                string sourceGuid = AssetDatabase.AssetPathToGUID(itemAssetPath);
                string bakeSignature = ItemIconBakeSignatureBuilder.Build(variant, settings);
                BakedItemIconEntry keyEntry = BakedItemIconEntry.Create(
                    variant.ItemData,
                    variant.InstalledModules,
                    variant.Width,
                    variant.Height,
                    variant.ProfileType,
                    settings,
                    string.Empty,
                    bakeSignature,
                    sourceGuid,
                    variant.EstimatedTextureBytes);
                Texture2D texture = await renderSession.RenderAsync(
                    variant.ItemData,
                    variant.InstalledModules,
                    variant.Width,
                    variant.Height,
                    variant.ProfileType,
                    cancellationToken);

                if (texture == null)
                {
                    throw new InvalidOperationException($"Icon renderer returned no texture for {variant.DisplayName}.");
                }

                try
                {
                    string resourcePath = ItemIconGeneratedAssetWriter.WriteTexture(texture, keyEntry, variant.ItemData, settings);
                    bakedEntries.Add(BakedItemIconEntry.Create(
                        variant.ItemData,
                        variant.InstalledModules,
                        variant.Width,
                        variant.Height,
                        variant.ProfileType,
                        settings,
                        resourcePath,
                        bakeSignature,
                        sourceGuid,
                        variant.EstimatedTextureBytes));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            completed = true;
            return bakedEntries.Count;
        }
        finally
        {
            if (bakedEntries.Count > 0)
            {
                Undo.RecordObject(catalog, replaceCatalog && completed ? "Bake All Item Icons" : "Bake Item Icon Variants");

                if (replaceCatalog && completed)
                {
                    catalog.ReplaceEntries(bakedEntries);
                    ItemIconGeneratedAssetWriter.DeleteGeneratedAssetsExcept(bakedEntries);
                }
                else
                {
                    catalog.UpsertEntries(bakedEntries);
                }

                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                BakedItemIconCatalog.ResetDefaultCache();
                ItemIconCache.Clear();
            }

            progress?.Invoke(variants.Count, variants.Count, null);
        }
    }
}
