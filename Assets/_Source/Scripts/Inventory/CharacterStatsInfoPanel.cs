using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterStatsInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private List<CharacterStatRow> rows = new List<CharacterStatRow>();

    public void RenderItemStats(
        InventoryItem item,
        Color currentValueColor,
        Color fullDurabilityValueColor,
        bool hideRootWhenEmpty)
    {
        bool hasAnyVisibleStat = false;

        for (int i = 0; i < rows.Count; i++)
        {
            CharacterStatRow row = rows[i];
            CharacterStatModifier modifier;
            bool show = TryGetModifier(item == null ? null : item.itemData, row.StatType, out modifier);

            row.SetActive(show);

            if (show == false)
            {
                continue;
            }

            float durabilityPercent = item != null && item.HasDurability ? item.CurrentDurabilityPercent : 100f;
            float currentValue = CharacterStatUtility.CalculateCurrentValue(modifier, durabilityPercent);
            row.SetText(
                FormatItemValue(
                    row.StatType,
                    currentValue,
                    modifier.ValueAtFullDurability,
                    durabilityPercent,
                    currentValueColor,
                    fullDurabilityValueColor));
            hasAnyVisibleStat = true;
        }

        SetRootActive(hasAnyVisibleStat || hideRootWhenEmpty == false);
    }

    public void RenderCharacterStats(
        CharacterStatBlock stats,
        Color currentValueColor,
        bool hideRootWhenEmpty,
        bool showAllStats)
    {
        bool hasAnyVisibleStat = false;

        for (int i = 0; i < rows.Count; i++)
        {
            CharacterStatRow row = rows[i];
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
        GameObject root = panelRoot == null ? gameObject : panelRoot;
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

    private static string FormatItemValue(
        CharacterStatType statType,
        float currentValue,
        float fullDurabilityValue,
        float durabilityPercent,
        Color currentValueColor,
        Color fullDurabilityValueColor)
    {
        string current = FormatColoredValue(statType, currentValue, currentValueColor);
        bool showFullValue = CharacterStatUtility.IsAffectedByDurability(statType) &&
                             Mathf.Approximately(durabilityPercent, 100f) == false &&
                             Mathf.Approximately(currentValue, fullDurabilityValue) == false;

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
        switch (statType)
        {
            case CharacterStatType.CarryWeight:
                return $"{FormatNumber(value)} КГ";
            case CharacterStatType.ArtifactContainers:
                return $"{FormatNumber(value)} шт.";
            default:
                return $"{FormatNumber(value)}%";
        }
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

[Serializable]
public class CharacterStatRow
{
    [SerializeField] private CharacterStatType statType;
    [SerializeField] private GameObject rowObject;
    [SerializeField] private TMP_Text valueText;

    public CharacterStatType StatType => statType;

    public void SetActive(bool active)
    {
        if (rowObject != null)
        {
            rowObject.SetActive(active);
        }

        if (valueText != null)
        {
            valueText.gameObject.SetActive(active);
        }
    }

    public void SetText(string text)
    {
        if (valueText == null)
        {
            return;
        }

        valueText.richText = true;
        valueText.text = text;
    }
}
