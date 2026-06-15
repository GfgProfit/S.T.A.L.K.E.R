using UnityEngine;

internal static class InventoryWeightTextFormatter
{
    public static string BuildText(float currentWeight, float maxWeight, float movementBlockWeight, GameProjectSettings settings)
    {
        Color valueColor = GetWeightTextColor(currentWeight, maxWeight, movementBlockWeight, settings);
        string valueColorHtml = ColorUtility.ToHtmlStringRGBA(valueColor);
        return $"Вес: <color=#{valueColorHtml}>{FormatWeight(currentWeight)}</color> / {FormatWeight(maxWeight)}";
    }

    private static string FormatWeight(float weight)
    {
        float normalizedWeight = Mathf.Max(0f, weight);

        if (normalizedWeight < 1f)
        {
            return $"{Mathf.RoundToInt(normalizedWeight * 1000f)} ГР";
        }

        return $"{normalizedWeight:0.#} КГ";
    }

    private static Color GetWeightTextColor(float currentWeight, float maxWeight, float movementBlockWeight, GameProjectSettings settings)
    {
        if (currentWeight >= movementBlockWeight)
        {
            return settings.MovementBlockedWeightColor;
        }

        if (currentWeight >= maxWeight)
        {
            return settings.OverweightColor;
        }

        return settings.NormalWeightColor;
    }
}
