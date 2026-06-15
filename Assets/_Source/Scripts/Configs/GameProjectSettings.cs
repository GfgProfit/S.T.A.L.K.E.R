using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = DEFAULT_RESOURCE_PATH, menuName = "Project/Settings")]
public class GameProjectSettings : ScriptableObject
{
    public const string DEFAULT_RESOURCE_PATH = "ProjectSettings";

    private static GameProjectSettings _defaultSettings;
    private static bool _defaultSettingsLoaded;

    [Header("Inventory Grid Visual Settings")]
    [SerializeField] private bool _showCellGrid = true;
    [SerializeField] private bool _showCellGridBorder = true;
    [SerializeField] private Color _cellGridColor = new Color(1f, 1f, 1f, 0.11764706f);
    [SerializeField] private float _cellGridLineThickness = 1f;
    [SerializeField] private Color _cellGridBorderColor = new Color(0.745283f, 0.745283f, 0.745283f, 0.11764706f);
    [SerializeField] private float _cellGridBorderLineThickness = 2f;

    [Header("Item Durability Visual Settings")]
    [SerializeField] private Color _highDurabilityColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color _mediumDurabilityColor = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color _lowDurabilityColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Item Rarity Visual Settings")]
    [SerializeField] private Color _iconOutlineColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color _iconShadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private List<ItemRarityColorSet> _rarityColors = new List<ItemRarityColorSet>();

    [Header("Character Stats Visual Settings")]
    [SerializeField] private Color _statCurrentValueColor = new Color(0f, 1f, 0.5568628f, 1f);
    [SerializeField] private Color _statFullDurabilityValueColor = new Color(1f, 0.55f, 0f, 1f);

    [Header("Inventory Weight Visual Settings")]
    [SerializeField] private Color _normalWeightColor = Color.white;
    [SerializeField] private Color _overweightColor = new Color(1f, 0.7033792f, 0f, 1f);
    [SerializeField] private Color _movementBlockedWeightColor = new Color(1f, 0f, 0.26052094f, 1f);

    public bool ShowCellGrid => _showCellGrid;
    public bool ShowCellGridBorder => _showCellGridBorder;
    public Color IconOutlineColor => _iconOutlineColor;
    public Color IconShadowColor => _iconShadowColor;
    public Color StatCurrentValueColor => _statCurrentValueColor;
    public Color StatFullDurabilityValueColor => _statFullDurabilityValueColor;
    public Color NormalWeightColor => _normalWeightColor;
    public Color OverweightColor => _overweightColor;
    public Color MovementBlockedWeightColor => _movementBlockedWeightColor;

    public Color GetLineColor(bool isBorderLine) => isBorderLine ? _cellGridBorderColor : _cellGridColor;

    public float GetLineThickness(bool isBorderLine)
    {
        float thickness = isBorderLine ? _cellGridBorderLineThickness : _cellGridLineThickness;
        return Mathf.Max(1f, thickness);
    }

    public Color GetDurabilityColor(float durabilityPercent)
    {
        float normalizedDurability = ItemData.NormalizeDurability(durabilityPercent);

        if (normalizedDurability > 66f)
        {
            return _highDurabilityColor;
        }

        if (normalizedDurability > 33f)
        {
            return _mediumDurabilityColor;
        }

        return _lowDurabilityColor;
    }

    public Color GetShortNameColor(ItemRarity rarity) => GetColorSet(rarity).ShortNameColor;
    public Color GetIconBackgroundColor(ItemRarity rarity) => GetColorSet(rarity).IconBackgroundColor;
    public Color GetIconCellGridBorderColor(ItemRarity rarity) => GetColorSet(rarity).IconCellGridBorderColor;

    public int BuildItemRarityVisualHash()
    {
        EnsureAllRarities();

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + HashColor(_iconOutlineColor);
            hash = hash * 31 + HashColor(_iconShadowColor);

            for (int i = 0; i < _rarityColors.Count; i++)
            {
                hash = hash * 31 + (_rarityColors[i] == null ? 0 : _rarityColors[i].BuildHash());
            }

            return hash;
        }
    }

    public static GameProjectSettings LoadDefault()
    {
        if (_defaultSettingsLoaded)
        {
            return _defaultSettings;
        }

        _defaultSettingsLoaded = true;
        _defaultSettings = Resources.Load<GameProjectSettings>(DEFAULT_RESOURCE_PATH);

        if (_defaultSettings == null)
        {
            _defaultSettings = CreateInstance<GameProjectSettings>();
            _defaultSettings.name = "Runtime GameProjectSettings";
            Debug.LogWarning($"Project settings asset was not found at Resources/{DEFAULT_RESOURCE_PATH}. A runtime default will be used.");
        }

        _defaultSettings.EnsureAllRarities();

        return _defaultSettings;
    }

    private ItemRarityColorSet GetColorSet(ItemRarity rarity)
    {
        EnsureAllRarities();

        for (int i = 0; i < _rarityColors.Count; i++)
        {
            ItemRarityColorSet colorSet = _rarityColors[i];

            if (colorSet != null && colorSet.Rarity == rarity)
            {
                return colorSet;
            }
        }

        return ItemRarityColorSet.CreateDefault(rarity);
    }

    private void EnsureAllRarities()
    {
        _rarityColors ??= new List<ItemRarityColorSet>();

        Array values = Enum.GetValues(typeof(ItemRarity));

        for (int i = 0; i < values.Length; i++)
        {
            ItemRarity rarity = (ItemRarity)values.GetValue(i);

            if (HasColorSet(rarity))
            {
                continue;
            }

            _rarityColors.Add(ItemRarityColorSet.CreateDefault(rarity));
        }
    }

    private bool HasColorSet(ItemRarity rarity)
    {
        for (int i = 0; i < _rarityColors.Count; i++)
        {
            ItemRarityColorSet colorSet = _rarityColors[i];

            if (colorSet != null && colorSet.Rarity == rarity)
            {
                return true;
            }
        }

        return false;
    }

    private void Reset() => EnsureAllRarities();
    private void OnValidate() => EnsureAllRarities();

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