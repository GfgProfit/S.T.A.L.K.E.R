using System.Collections.Generic;
using UnityEngine;

public class CharacterStatsInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private List<CharacterStatRow> _rows = new();

    public void RenderItemStats(InventoryItem item, Color currentValueColor, Color fullDurabilityValueColor, bool hideRootWhenEmpty)
    {
        bool hasAnyVisibleStat = false;

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterStatRow row = _rows[i];
            bool show = TryGetModifier(item == null ? null : item.ItemData, row.StatType, out CharacterStatModifier modifier);

            row.SetActive(show);

            if (show == false)
            {
                continue;
            }

            float durabilityPercent = item != null && item.HasDurability ? item.CurrentDurabilityPercent : 100f;
            float currentValue = CharacterStatUtility.CalculateCurrentValue(modifier, durabilityPercent);
            row.SetText(FormatItemValue(row.StatType, currentValue, modifier.ValueAtFullDurability, durabilityPercent, currentValueColor, fullDurabilityValueColor));
            hasAnyVisibleStat = true;
        }

        SetRootActive(hasAnyVisibleStat || hideRootWhenEmpty == false);
    }

    public void RenderCharacterStats(CharacterStatBlock stats, Color currentValueColor, bool hideRootWhenEmpty, bool showAllStats)
    {
        bool hasAnyVisibleStat = false;

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterStatRow row = _rows[i];
            float value = stats == null ? 0f : stats.Get(row.StatType);
            bool show = showAllStats || CharacterStatUtility.IsNonZero(value);

            row.SetActive(show);

            if (show == false)
            {
                continue;
            }

            Color valueColor = CharacterStatUtility.IsNonZero(value) ? currentValueColor : Color.white;
            row.SetText(FormatColoredValue(row.StatType, value, valueColor));
            hasAnyVisibleStat = true;
        }

        SetRootActive(showAllStats || hasAnyVisibleStat || hideRootWhenEmpty == false);
    }

    private void SetRootActive(bool active)
    {
        GameObject root = _panelRoot == null ? gameObject : _panelRoot;
        root.SetActive(active);
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
}