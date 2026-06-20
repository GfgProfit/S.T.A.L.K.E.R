using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

internal static class ItemIconBakeSignatureBuilder
{
    public static string Build(ItemIconBakeVariant variant, ItemIconGeneratorSettings settings)
    {
        StringBuilder source = new(512);
        BakedItemIconEntry keyEntry = BakedItemIconEntry.Create(
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

        source.Append(BuildIdentity(keyEntry));
        AppendDependency(source, variant.ItemData);
        AppendDependency(source, variant.ItemData.IconPrefab);
        AppendDependency(source, variant.ItemData.FirstPersonWeaponPrefab);
        AppendDependency(source, settings);
        AppendDependency(source, GameProjectSettings.LoadDefault());
        AppendModules(source, variant.ItemData.DefaultIconModules);
        AppendModules(source, variant.InstalledModules);
        return Hash128.Compute(source.ToString()).ToString();
    }

    public static string BuildIdentity(BakedItemIconEntry entry)
    {
        StringBuilder identity = new(192);
        identity.Append(entry.ItemId).Append('|');

        for (int i = 0; i < entry.ModuleItemIds.Count; i++)
        {
            identity.Append(entry.ModuleItemIds[i]).Append(',');
        }

        identity.Append('|')
            .Append(entry.Width).Append('x').Append(entry.Height).Append('|')
            .Append((int)entry.ProfileType).Append('|')
            .Append(entry.VisualSignature);
        return identity.ToString();
    }

    private static void AppendModules(StringBuilder source, IReadOnlyList<ItemData> modules)
    {
        if (modules == null)
        {
            return;
        }

        List<ItemData> sortedModules = new();

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] != null && sortedModules.Contains(modules[i]) == false)
            {
                sortedModules.Add(modules[i]);
            }
        }

        sortedModules.Sort((left, right) => string.CompareOrdinal(left.ItemId, right.ItemId));

        for (int i = 0; i < sortedModules.Count; i++)
        {
            AppendDependency(source, sortedModules[i]);
            AppendDependency(source, sortedModules[i].IconPrefab);
        }
    }

    private static void AppendDependency(StringBuilder source, Object asset)
    {
        if (asset == null)
        {
            source.Append("null;");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(asset);
        source.Append(assetPath).Append(':');

        if (string.IsNullOrEmpty(assetPath))
        {
            source.Append("unsaved;");
            return;
        }

        source.Append(AssetDatabase.GetAssetDependencyHash(assetPath)).Append(';');
    }
}
