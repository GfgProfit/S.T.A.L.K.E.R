using System;
using System.Text;
using UnityEngine;

internal static class ItemTooltipTextFormatter
{
    private static readonly char[] _wordSeparators = { ' ', '\t', '\n', '\r' };

    public static string FormatType(ItemData itemData) => itemData == null ? string.Empty : $"Тип: {ItemTypeTextFormatter.ToRussianText(itemData.ItemType)}";

    public static string FormatWeight(int amount, float unitWeight, float totalWeight)
    {
        return amount > 1 ? $"Вес: {FormatWeightValue(totalWeight)} ({FormatWeightValue(unitWeight)})" : $"Вес: {FormatWeightValue(totalWeight)}";
    }

    public static string FormatDurability(float durabilityPercent, Color durabilityColor)
    {
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
