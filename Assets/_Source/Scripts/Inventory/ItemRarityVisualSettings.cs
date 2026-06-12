using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = DefaultResourcePath, menuName = "Inventory/Item Rarity Visual Settings")]
public class ItemRarityVisualSettings : ScriptableObject
{
    public const string DefaultResourcePath = "ItemRarityVisualSettings";

    private static ItemRarityVisualSettings defaultSettings;
    private static bool defaultSettingsLoaded;

    [SerializeField] private Color iconOutlineColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color iconShadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private List<ItemRarityColorSet> rarityColors = new List<ItemRarityColorSet>();

    public Color IconOutlineColor => iconOutlineColor;
    public Color IconShadowColor => iconShadowColor;

    public Color GetShortNameColor(ItemRarity rarity)
    {
        return GetColorSet(rarity).ShortNameColor;
    }

    public Color GetIconBackgroundColor(ItemRarity rarity)
    {
        return GetColorSet(rarity).IconBackgroundColor;
    }

    public Color GetIconCellGridBorderColor(ItemRarity rarity)
    {
        return GetColorSet(rarity).IconCellGridBorderColor;
    }

    public int BuildHash()
    {
        EnsureAllRarities();

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + HashColor(iconOutlineColor);
            hash = hash * 31 + HashColor(iconShadowColor);

            for (int i = 0; i < rarityColors.Count; i++)
            {
                hash = hash * 31 + (rarityColors[i] == null ? 0 : rarityColors[i].BuildHash());
            }

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

    public static ItemRarityVisualSettings LoadDefault()
    {
        if (defaultSettingsLoaded)
        {
            return defaultSettings;
        }

        defaultSettingsLoaded = true;
        defaultSettings = Resources.Load<ItemRarityVisualSettings>(DefaultResourcePath);

        if (defaultSettings == null)
        {
            defaultSettings = CreateInstance<ItemRarityVisualSettings>();
            defaultSettings.name = "Runtime ItemRarityVisualSettings";
            Debug.LogWarning($"Item rarity visual settings asset was not found at Resources/{DefaultResourcePath}. A runtime default will be used.");
        }

        defaultSettings.EnsureAllRarities();
        return defaultSettings;
    }

    private ItemRarityColorSet GetColorSet(ItemRarity rarity)
    {
        EnsureAllRarities();

        for (int i = 0; i < rarityColors.Count; i++)
        {
            ItemRarityColorSet colorSet = rarityColors[i];
            if (colorSet != null && colorSet.Rarity == rarity)
            {
                return colorSet;
            }
        }

        return ItemRarityColorSet.CreateDefault(rarity);
    }

    private void EnsureAllRarities()
    {
        rarityColors ??= new List<ItemRarityColorSet>();

        Array values = Enum.GetValues(typeof(ItemRarity));
        for (int i = 0; i < values.Length; i++)
        {
            ItemRarity rarity = (ItemRarity)values.GetValue(i);
            if (HasColorSet(rarity))
            {
                continue;
            }

            rarityColors.Add(ItemRarityColorSet.CreateDefault(rarity));
        }
    }

    private bool HasColorSet(ItemRarity rarity)
    {
        for (int i = 0; i < rarityColors.Count; i++)
        {
            ItemRarityColorSet colorSet = rarityColors[i];
            if (colorSet != null && colorSet.Rarity == rarity)
            {
                return true;
            }
        }

        return false;
    }

    private void Reset()
    {
        EnsureAllRarities();
    }

    private void OnValidate()
    {
        EnsureAllRarities();
    }

    [Serializable]
    public sealed class ItemRarityColorSet
    {
        [SerializeField] private ItemRarity rarity;
        [SerializeField] private Color shortNameColor = Color.white;
        [SerializeField] private Color iconBackgroundColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color iconCellGridBorderColor = new Color(0.745283f, 0.745283f, 0.745283f, 0.11764706f);

        public ItemRarity Rarity => rarity;
        public Color ShortNameColor => shortNameColor;
        public Color IconBackgroundColor => iconBackgroundColor;
        public Color IconCellGridBorderColor => iconCellGridBorderColor;

        public static ItemRarityColorSet CreateDefault(ItemRarity rarity)
        {
            return new ItemRarityColorSet { rarity = rarity };
        }

        public int BuildHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)rarity;
                hash = hash * 31 + HashColor(shortNameColor);
                hash = hash * 31 + HashColor(iconBackgroundColor);
                hash = hash * 31 + HashColor(iconCellGridBorderColor);
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
}
