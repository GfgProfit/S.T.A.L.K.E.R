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

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + itemData.GetInstanceID();
            hash = hash * 31 + (int)itemData.Rarity;
            hash = hash * 31 + GameProjectSettings.LoadDefault().BuildItemRarityVisualHash();
            hash = hash * 31 + Mathf.Max(1, targetWidth);
            hash = hash * 31 + Mathf.Max(1, targetHeight);
            hash = hash * 31 + itemData.IconPixelsPerCell;
            hash = hash * 31 + itemData.IconRenderScale;
            hash = hash * 31 + itemData.IconAntiAliasing;
            hash = hash * 31 + (itemData.IconUseOutline ? 1 : 0);
            hash = hash * 31 + HashColor(itemData.IconOutlineColor);
            hash = hash * 31 + itemData.IconOutlineTextureWidth;
            hash = hash * 31 + (itemData.IconUseShadow ? 1 : 0);
            hash = hash * 31 + HashColor(itemData.IconShadowColor);
            hash = hash * 31 + HashVector(itemData.IconShadowTextureOffset);
            hash = hash * 31 + itemData.IconShadowTextureBlur;
            hash = hash * 31 + Quantize(useSlotIconSettings ? itemData.SlotIconPadding : itemData.IconPadding);
            hash = hash * 31 + HashVector(useSlotIconSettings ? itemData.SlotIconModelEulerAngles : itemData.IconModelEulerAngles);
            hash = hash * 31 + HashVector(useSlotIconSettings ? itemData.SlotIconModelScale : itemData.IconModelScale);
            hash = hash * 31 + HashVector(useSlotIconSettings ? itemData.SlotIconCameraEulerAngles : itemData.IconCameraEulerAngles);
            bool useDirectionalLight = useSlotIconSettings ? itemData.SlotIconUseDirectionalLight : itemData.IconUseDirectionalLight;
            hash = hash * 31 + (useDirectionalLight ? 1 : 0);
            hash = hash * 31 + HashVector(useSlotIconSettings ? itemData.SlotIconLightEulerAngles : itemData.IconLightEulerAngles);
            hash = hash * 31 + Quantize(useSlotIconSettings ? itemData.SlotIconLightIntensity : itemData.IconLightIntensity);
            hash = hash * 31 + (itemData.IconPrefab == null ? 0 : itemData.IconPrefab.GetInstanceID());
            hash = hash * 31 + HashInstalledModules(installedModules);
            return hash;
        }
    }

    private static int HashInstalledModules(IReadOnlyList<ItemData> installedModules)
    {
        if (installedModules == null)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;

            for (int i = 0; i < installedModules.Count; i++)
            {
                hash = hash * 31 + (installedModules[i] == null ? 0 : installedModules[i].GetInstanceID());
            }

            return hash;
        }
    }

    private static int HashVector(Vector3 value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Quantize(value.x);
            hash = hash * 31 + Quantize(value.y);
            hash = hash * 31 + Quantize(value.z);
            return hash;
        }
    }

    private static int HashVector(Vector2 value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Quantize(value.x);
            hash = hash * 31 + Quantize(value.y);
            return hash;
        }
    }

    private static int HashColor(Color value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Quantize(value.r);
            hash = hash * 31 + Quantize(value.g);
            hash = hash * 31 + Quantize(value.b);
            hash = hash * 31 + Quantize(value.a);
            return hash;
        }
    }

    private static int Quantize(float value)
    {
        return Mathf.RoundToInt(value * 1000f);
    }
}
