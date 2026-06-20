using System.Collections.Generic;

internal static class ItemIconStableKeyBuilder
{
    public static IconCacheKey Build(
        ItemData itemData,
        IReadOnlyList<ItemData> installedModules,
        ItemIconGeneratorSettings settings,
        IconRenderProfile renderProfile)
    {
        string itemId = itemData == null ? string.Empty : itemData.ItemId;
        string moduleItemIds = ItemIconModuleKeyCache.GetCanonicalKey(installedModules);
        ulong visualSignature = ItemIconHashBuilder.BuildStableHash(itemData, settings, renderProfile);

        return new IconCacheKey(
            itemId,
            moduleItemIds,
            renderProfile.CellWidth,
            renderProfile.CellHeight,
            renderProfile.ProfileType,
            visualSignature);
    }
}
