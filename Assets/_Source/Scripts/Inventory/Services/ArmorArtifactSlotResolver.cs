using UnityEngine;

internal static class ArmorArtifactSlotResolver
{
    public static int GetSlotCount(InventoryItem armorItem)
    {
        if (armorItem == null || armorItem.ItemData == null || armorItem.ItemData.ItemType != ItemType.Armor || armorItem.ItemData.StatModifiers == null)
        {
            return 0;
        }

        for (int i = 0; i < armorItem.ItemData.StatModifiers.Count; i++)
        {
            CharacterStatModifier modifier = armorItem.ItemData.StatModifiers[i];

            if (modifier.StatType != CharacterStatType.ArtifactContainers || modifier.HasValue == false)
            {
                continue;
            }

            float durabilityPercent = armorItem.HasDurability ? armorItem.CurrentDurabilityPercent : 100.0f;
            float slotCount = CharacterStatUtility.CalculateCurrentValue(modifier, durabilityPercent);
            return Mathf.Max(0, Mathf.RoundToInt(slotCount));
        }

        return 0;
    }
}