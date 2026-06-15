using System.Collections.Generic;
using UnityEngine;

public static class CharacterStatUtility
{
    public const float ZERO_TOLERANCE = 0.0001f;

    private static readonly CharacterStatType[] _allStatTypes =
    {
        CharacterStatType.ThermalProtection,
        CharacterStatType.ElectricalProtection,
        CharacterStatType.ChemicalProtection,
        CharacterStatType.RadiationProtection,
        CharacterStatType.PsiProtection,
        CharacterStatType.ShockRelief,
        CharacterStatType.ArmorProtection,
        CharacterStatType.Stamina,
        CharacterStatType.ArtifactContainers,
        CharacterStatType.CarryWeight
    };

    public static IReadOnlyList<CharacterStatType> AllStatTypes => _allStatTypes;
    public static int StatCount => _allStatTypes.Length;

    public static int ToIndex(CharacterStatType statType)
    {
        int index = (int)statType;
        return index >= 0 && index < StatCount ? index : 0;
    }

    public static bool IsNonZero(float value) => Mathf.Abs(value) > ZERO_TOLERANCE;
    public static bool IsAffectedByDurability(CharacterStatType statType) => statType != CharacterStatType.Stamina && statType != CharacterStatType.CarryWeight && statType != CharacterStatType.ArtifactContainers;
    public static float CalculateCurrentValue(CharacterStatModifier modifier, float durabilityPercent) => CalculateCurrentValue(modifier.StatType, modifier.ValueAtFullDurability, durabilityPercent);

    public static float CalculateCurrentValue(CharacterStatType statType, float valueAtFullDurability, float durabilityPercent)
    {
        if (IsAffectedByDurability(statType) == false)
        {
            return valueAtFullDurability;
        }

        return valueAtFullDurability * Mathf.Clamp01(durabilityPercent / 100f);
    }

    public static bool HasAnyModifier(IReadOnlyList<CharacterStatModifier> modifiers)
    {
        if (modifiers == null)
        {
            return false;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].HasValue)
            {
                return true;
            }
        }

        return false;
    }

    public static void AddModifiers(CharacterStatBlock target, IReadOnlyList<CharacterStatModifier> modifiers, float durabilityPercent)
    {
        if (target == null || modifiers == null)
        {
            return;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            CharacterStatModifier modifier = modifiers[i];
            if (modifier.HasValue == false)
            {
                continue;
            }

            target.Add(modifier.StatType, CalculateCurrentValue(modifier, durabilityPercent));
        }
    }

    public static void AddItemStats(CharacterStatBlock target, ItemData itemData, float durabilityPercent)
    {
        if (target == null || itemData == null)
        {
            return;
        }

        float normalizedDurabilityPercent = itemData.HasDurability ? global::ItemData.NormalizeDurability(durabilityPercent) : 100f;
        AddModifiers(target, itemData.StatModifiers, normalizedDurabilityPercent);
    }
}
