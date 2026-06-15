using System;
using System.Text;
using UnityEngine;

internal static class ItemInfoPanelTextFormatter
{
    private static readonly char[] _wordSeparators = { ' ', '\t', '\n', '\r' };

    public static string FormatType(ItemData itemData) => itemData == null ? string.Empty : $"Тип: {ItemTypeFormatter.ToRussianText(itemData.ItemType)}";

    public static string FormatWeight(InventoryItem item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        return item.CurrentAmount > 1 ? $"Вес: {FormatWeightValue(item.TotalWeight)} ({FormatWeightValue(item.UnitWeight)})" : $"Вес: {FormatWeightValue(item.TotalWeight)}";
    }

    public static string FormatDurability(InventoryItem item, Color durabilityColor)
    {
        if (item == null || item.HasDurability == false)
        {
            return string.Empty;
        }

        float durabilityPercent = item.CurrentDurabilityPercent;
        string percentColor = ColorUtility.ToHtmlStringRGBA(durabilityColor);
        return $"Прочность: <color=#{percentColor}>{durabilityPercent:00.0}%</color>";
    }

    public static string WrapDescription(string text, int wordsPerLine)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (wordsPerLine <= 0)
        {
            return text.Trim();
        }

        string[] words = text.Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new(text.Length);

        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % wordsPerLine == 0 ? '\n' : ' ');
            }

            builder.Append(words[i]);
        }

        return builder.ToString();
    }

    private static string FormatWeightValue(float weight)
    {
        float normalizedWeight = Mathf.Max(0f, weight);

        if (normalizedWeight < 1f)
        {
            return $"<color=orange>{Mathf.RoundToInt(normalizedWeight * 1000f)}</color> г";
        }

        return $"<color=orange>{normalizedWeight:0.#}</color> кг";
    }
}