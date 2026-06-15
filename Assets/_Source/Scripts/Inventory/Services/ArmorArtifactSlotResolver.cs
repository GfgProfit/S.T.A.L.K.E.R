using UnityEngine;

internal static class ArmorArtifactSlotResolver
{
    public static int GetSlotCount(ItemData armorData, float durabilityPercent)
    {
        if (armorData == null || armorData.ItemType != ItemType.Armor || armorData.StatModifiers == null)
        {
            return 0;
        }

        for (int i = 0; i < armorData.StatModifiers.Count; i++)
        {
            CharacterStatModifier modifier = armorData.StatModifiers[i];

            if (modifier.StatType != CharacterStatType.ArtifactContainers || modifier.HasValue == false)
            {
                continue;
            }

            float normalizedDurabilityPercent = armorData.HasDurability ? global::ItemData.NormalizeDurability(durabilityPercent) : 100.0f;
            float slotCount = CharacterStatUtility.CalculateCurrentValue(modifier, normalizedDurabilityPercent);
            return Mathf.Max(0, Mathf.RoundToInt(slotCount));
        }

        return 0;
    }
}
