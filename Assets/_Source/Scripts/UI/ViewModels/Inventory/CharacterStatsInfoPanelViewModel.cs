using System.Collections.Generic;
using R3;
using UnityEngine;

public readonly struct CharacterStatRowState
{
    public CharacterStatRowState(CharacterStatType statType, bool isVisible, string text)
    {
        StatType = statType;
        IsVisible = isVisible;
        Text = text ?? string.Empty;
    }

    public CharacterStatType StatType { get; }
    public bool IsVisible { get; }
    public string Text { get; }
}

public sealed class CharacterStatsInfoPanelViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _rootActive = new();
    private readonly ReactiveProperty<IReadOnlyList<CharacterStatRowState>> _rows = new(new List<CharacterStatRowState>());
    private readonly List<CharacterStatRowState> _rowBuffer = new();

    public ReadOnlyReactiveProperty<bool> RootActive => _rootActive;
    public ReadOnlyReactiveProperty<IReadOnlyList<CharacterStatRowState>> Rows => _rows;

    public void RenderItemStats(IReadOnlyList<CharacterStatType> rowStatTypes, ItemTooltipData item, Color currentValueColor, Color fullDurabilityValueColor, bool hideRootWhenEmpty)
    {
        _rowBuffer.Clear();
        bool hasAnyVisibleStat = false;

        for (int i = 0; i < rowStatTypes.Count; i++)
        {
            CharacterStatType statType = rowStatTypes[i];
            bool show = TryGetModifier(item.ItemData, statType, out CharacterStatModifier modifier);
            string text = string.Empty;

            if (show)
            {
                float durabilityPercent = item.HasDurability ? item.DurabilityPercent : 100f;
                float currentValue = CharacterStatUtility.CalculateCurrentValue(modifier, durabilityPercent);
                text = FormatItemValue(statType, currentValue, modifier.ValueAtFullDurability, durabilityPercent, currentValueColor, fullDurabilityValueColor);
                hasAnyVisibleStat = true;
            }

            _rowBuffer.Add(new(statType, show, text));
        }

        PublishRows();
        _rootActive.Value = hasAnyVisibleStat || hideRootWhenEmpty == false;
    }

    public void RenderCharacterStats(IReadOnlyList<CharacterStatType> rowStatTypes, CharacterStatBlock stats, Color currentValueColor, bool hideRootWhenEmpty, bool showAllStats)
    {
        _rowBuffer.Clear();
        bool hasAnyVisibleStat = false;

        for (int i = 0; i < rowStatTypes.Count; i++)
        {
            CharacterStatType statType = rowStatTypes[i];
            float value = stats == null ? 0f : stats.Get(statType);
            bool show = showAllStats || CharacterStatUtility.IsNonZero(value);
            string text = string.Empty;

            if (show)
            {
                Color valueColor = CharacterStatUtility.IsNonZero(value) ? currentValueColor : Color.white;
                text = FormatColoredValue(statType, value, valueColor);
                hasAnyVisibleStat = true;
            }

            _rowBuffer.Add(new(statType, show, text));
        }

        PublishRows();
        _rootActive.Value = showAllStats || hasAnyVisibleStat || hideRootWhenEmpty == false;
    }

    private void PublishRows()
    {
        _rows.Value = new List<CharacterStatRowState>(_rowBuffer);
    }

    private static bool TryGetModifier(ItemData itemData, CharacterStatType statType, out CharacterStatModifier modifier)
    {
        modifier = default;

        if (itemData == null || itemData.StatModifiers == null)
        {
            return false;
        }

        for (int i = 0; i < itemData.StatModifiers.Count; i++)
        {
            CharacterStatModifier candidate = itemData.StatModifiers[i];

            if (candidate.StatType == statType && candidate.HasValue)
            {
                modifier = candidate;
                return true;
            }
        }

        return false;
    }

    private static string FormatItemValue(CharacterStatType statType, float currentValue, float fullDurabilityValue, float durabilityPercent, Color currentValueColor, Color fullDurabilityValueColor)
    {
        string current = FormatColoredValue(statType, currentValue, currentValueColor);
        bool showFullValue = CharacterStatUtility.IsAffectedByDurability(statType) && Mathf.Approximately(durabilityPercent, 100f) == false && Mathf.Approximately(currentValue, fullDurabilityValue) == false;

        if (showFullValue == false)
        {
            return current;
        }

        return $"{current} ({FormatColoredValue(statType, fullDurabilityValue, fullDurabilityValueColor)})";
    }

    private static string FormatColoredValue(CharacterStatType statType, float value, Color color)
    {
        string htmlColor = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{htmlColor}>{FormatSignedValue(statType, value)}</color>";
    }

    private static string FormatSignedValue(CharacterStatType statType, float value)
    {
        if (CharacterStatUtility.IsNonZero(value) == false)
        {
            return FormatStatValue(statType, 0f);
        }

        string sign = value >= 0f ? "+" : string.Empty;
        return $"{sign}{FormatStatValue(statType, value)}";
    }

    private static string FormatStatValue(CharacterStatType statType, float value)
    {
        return statType switch
        {
            CharacterStatType.CarryWeight => $"{FormatNumber(value)} КГ",
            CharacterStatType.ArtifactContainers => $"{FormatNumber(value)} шт.",
            _ => $"{FormatNumber(value)}%",
        };
    }

    private static string FormatNumber(float value)
    {
        float roundedTenths = Mathf.Round(value * 10f) / 10f;

        if (Mathf.Approximately(roundedTenths, Mathf.Round(roundedTenths)))
        {
            return Mathf.RoundToInt(roundedTenths).ToString();
        }

        return roundedTenths.ToString("0.#");
    }

    protected override void DisposeManaged()
    {
        _rootActive.Dispose();
        _rows.Dispose();
    }
}
