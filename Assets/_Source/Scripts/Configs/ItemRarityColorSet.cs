using System;
using UnityEngine;

[Serializable]
public sealed class ItemRarityColorSet
{
    [SerializeField] private ItemRarity _rarity;
    [SerializeField] private Color _shortNameColor = Color.white;
    [SerializeField] private Color _iconBackgroundColor = new(0f, 0f, 0f, 0f);
    [SerializeField] private Color _iconCellGridBorderColor = new(0.745283f, 0.745283f, 0.745283f, 0.11764706f);

    public ItemRarity Rarity => _rarity;
    public Color ShortNameColor => _shortNameColor;
    public Color IconBackgroundColor => _iconBackgroundColor;
    public Color IconCellGridBorderColor => _iconCellGridBorderColor;

    public static ItemRarityColorSet CreateDefault(ItemRarity rarity) => new() { _rarity = rarity };

    public int BuildHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (int)_rarity;
            hash = hash * 31 + HashColor(_shortNameColor);
            hash = hash * 31 + HashColor(_iconBackgroundColor);
            hash = hash * 31 + HashColor(_iconCellGridBorderColor);
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

    private static int Quantize(float value) => Mathf.RoundToInt(value * 1000f);
}