using System.Collections.Generic;
using UnityEngine;

internal static class ItemIconHashBuilder
{
    public static bool HasRuntimeIconSource(bool generateIconAtRuntime, GameObject iconPrefab) => generateIconAtRuntime && iconPrefab != null;

    public static int BuildHash(ItemData itemData, IReadOnlyList<ItemData> installedModules, int targetWidth, int targetHeight, bool useSlotIconSettings)
    {
        if (itemData == null)
        {
            return 0;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IconRenderProfile renderProfile = useSlotIconSettings
            ? IconRenderProfile.CreateSlot(itemData, targetWidth, targetHeight, settings)
            : IconRenderProfile.CreateDefault(itemData, targetWidth, targetHeight, settings);
        ulong stableHash = BuildStableHash(itemData, settings, renderProfile);
        stableHash = ItemIconStableHash.Add(stableHash, ItemIconModuleKeyCache.GetCanonicalKey(installedModules));
        return unchecked((int)(stableHash ^ (stableHash >> 32)));
    }

    public static ulong BuildStableHash(ItemData itemData, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        if (itemData == null || settings == null)
        {
            return 0UL;
        }

        ulong hash = ItemIconStableHash.Begin();
        hash = ItemIconStableHash.Add(hash, 4);
        hash = ItemIconStableHash.Add(hash, itemData.ItemId);
        hash = ItemIconStableHash.Add(hash, (int)itemData.Rarity);
        hash = ItemIconStableHash.Add(hash, renderProfile.CellWidth);
        hash = ItemIconStableHash.Add(hash, renderProfile.CellHeight);
        hash = ItemIconStableHash.Add(hash, (int)renderProfile.ProfileType);
        hash = ItemIconStableHash.Add(hash, itemData.IconPixelsPerCell);
        hash = ItemIconStableHash.Add(hash, itemData.IconAntiAliasing);
        hash = ItemIconStableHash.Add(hash, itemData.IconUseOutline);
        hash = ItemIconStableHash.Add(hash, itemData.IconOutlineColor);
        hash = ItemIconStableHash.Add(hash, itemData.GetIconOutlineTextureWidth(renderProfile.RenderScale));
        hash = ItemIconStableHash.Add(hash, itemData.IconUseShadow);
        hash = ItemIconStableHash.Add(hash, itemData.IconShadowColor);
        Vector2Int shadowTextureOffset = itemData.GetIconShadowTextureOffset(renderProfile.RenderScale);
        hash = ItemIconStableHash.Add(hash, shadowTextureOffset.x);
        hash = ItemIconStableHash.Add(hash, shadowTextureOffset.y);
        hash = ItemIconStableHash.Add(hash, itemData.GetIconShadowTextureBlur(renderProfile.RenderScale));
        hash = ItemIconStableHash.Add(hash, itemData.IconBackgroundColor);
        hash = ItemIconStableHash.Add(hash, itemData.IconShowCellGrid);
        hash = ItemIconStableHash.Add(hash, itemData.IconShowCellGridBorder);
        hash = ItemIconStableHash.Add(hash, itemData.IconCellGridBorderColor);
        hash = ItemIconStableHash.Add(hash, itemData.IconCellGridBorderLineThickness);
        hash = ItemIconStableHash.Add(hash, renderProfile.Padding);
        hash = ItemIconStableHash.Add(hash, renderProfile.ModelEulerAngles);
        hash = ItemIconStableHash.Add(hash, renderProfile.ModelScale);
        hash = ItemIconStableHash.Add(hash, renderProfile.CameraEulerAngles);
        hash = ItemIconStableHash.Add(hash, renderProfile.UseDirectionalLight);
        hash = ItemIconStableHash.Add(hash, renderProfile.LightIntensity);
        hash = ItemIconStableHash.Add(hash, ItemIconModuleKeyCache.GetCanonicalKey(itemData.DefaultIconModules));
        return ItemIconStableHash.Add(hash, settings.BuildStableHash());
    }
}
