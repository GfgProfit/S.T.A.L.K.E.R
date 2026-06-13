using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = DefaultResourcePath, menuName = "Project/Settings")]
public class GameProjectSettings : ScriptableObject
{
    public const string DefaultResourcePath = "ProjectSettings";

    private static GameProjectSettings defaultSettings;
    private static bool defaultSettingsLoaded;

    [Header("Inventory Grid Visual Settings")]
    [SerializeField] private bool showCellGrid = true;
    [SerializeField] private bool showCellGridBorder = true;
    [SerializeField] private Color cellGridColor = new Color(1f, 1f, 1f, 0.11764706f);
    [SerializeField] private float cellGridLineThickness = 1f;
    [SerializeField] private Color cellGridBorderColor = new Color(0.745283f, 0.745283f, 0.745283f, 0.11764706f);
    [SerializeField] private float cellGridBorderLineThickness = 2f;

    [Header("Item Durability Visual Settings")]
    [SerializeField] private Color highDurabilityColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color mediumDurabilityColor = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color lowDurabilityColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Item Rarity Visual Settings")]
    [SerializeField] private Color iconOutlineColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color iconShadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private List<ItemRarityColorSet> rarityColors = new List<ItemRarityColorSet>();

    [Header("Character Stats Visual Settings")]
    [SerializeField] private Color statCurrentValueColor = new Color(0f, 1f, 0.5568628f, 1f);
    [SerializeField] private Color statFullDurabilityValueColor = new Color(1f, 0.55f, 0f, 1f);

    [Header("Inventory Weight Visual Settings")]
    [SerializeField] private Color normalWeightColor = Color.white;
    [SerializeField] private Color overweightColor = new Color(1f, 0.7033792f, 0f, 1f);
    [SerializeField] private Color movementBlockedWeightColor = new Color(1f, 0f, 0.26052094f, 1f);

    public bool ShowCellGrid => showCellGrid;
    public bool ShowCellGridBorder => showCellGridBorder;
    public Color IconOutlineColor => iconOutlineColor;
    public Color IconShadowColor => iconShadowColor;
    public Color StatCurrentValueColor => statCurrentValueColor;
    public Color StatFullDurabilityValueColor => statFullDurabilityValueColor;
    public Color NormalWeightColor => normalWeightColor;
    public Color OverweightColor => overweightColor;
    public Color MovementBlockedWeightColor => movementBlockedWeightColor;

    public Color GetLineColor(bool isBorderLine)
    {
        return isBorderLine ? cellGridBorderColor : cellGridColor;
    }

    public float GetLineThickness(bool isBorderLine)
    {
        float thickness = isBorderLine ? cellGridBorderLineThickness : cellGridLineThickness;
        return Mathf.Max(1f, thickness);
    }

    public Color GetDurabilityColor(float durabilityPercent)
    {
        float normalizedDurability = ItemData.NormalizeDurability(durabilityPercent);

        if (normalizedDurability > 66f)
        {
            return highDurabilityColor;
        }

        if (normalizedDurability > 33f)
        {
            return mediumDurabilityColor;
        }

        return lowDurabilityColor;
    }

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

    public int BuildItemRarityVisualHash()
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

    public static GameProjectSettings LoadDefault()
    {
        if (defaultSettingsLoaded)
        {
            return defaultSettings;
        }

        defaultSettingsLoaded = true;
        defaultSettings = Resources.Load<GameProjectSettings>(DefaultResourcePath);

        if (defaultSettings == null)
        {
            defaultSettings = CreateInstance<GameProjectSettings>();
            defaultSettings.name = "Runtime GameProjectSettings";
            Debug.LogWarning($"Project settings asset was not found at Resources/{DefaultResourcePath}. A runtime default will be used.");
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
