using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterStatType
{
    ThermalProtection = 0,
    ElectricalProtection = 1,
    ChemicalProtection = 2,
    RadiationProtection = 3,
    PsiProtection = 4,
    ShockRelief = 5,
    ArmorProtection = 6,
    Stamina = 7,
    ArtifactContainers = 8,
    CarryWeight = 9
}

[Serializable]
public struct CharacterStatModifier
{
    [SerializeField] private CharacterStatType statType;
    [SerializeField] private float valueAtFullDurability;

    public CharacterStatType StatType => statType;
    public float ValueAtFullDurability => valueAtFullDurability;
    public bool HasValue => CharacterStatUtility.IsNonZero(valueAtFullDurability);

    public CharacterStatModifier(CharacterStatType statType, float valueAtFullDurability)
    {
        this.statType = statType;
        this.valueAtFullDurability = valueAtFullDurability;
    }
}

public sealed class CharacterStatBlock
{
    private readonly float[] values = new float[CharacterStatUtility.StatCount];

    public float Get(CharacterStatType statType)
    {
        return values[CharacterStatUtility.ToIndex(statType)];
    }

    public void Set(CharacterStatType statType, float value)
    {
        values[CharacterStatUtility.ToIndex(statType)] = value;
    }

    public void Add(CharacterStatType statType, float value)
    {
        values[CharacterStatUtility.ToIndex(statType)] += value;
    }

    public void Add(CharacterStatBlock source)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            values[i] += source.values[i];
        }
    }

    public void CopyFrom(CharacterStatBlock source)
    {
        Clear();

        if (source == null)
        {
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = source.values[i];
        }
    }

    public void Clear()
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = 0f;
        }
    }

    public bool HasAnyNonZeroValue()
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (CharacterStatUtility.IsNonZero(values[i]))
            {
                return true;
            }
        }

        return false;
    }
}

public static class CharacterStatUtility
{
    public const float ZeroTolerance = 0.0001f;

    private static readonly CharacterStatType[] allStatTypes =
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

    public static IReadOnlyList<CharacterStatType> AllStatTypes => allStatTypes;
    public static int StatCount => allStatTypes.Length;

    public static int ToIndex(CharacterStatType statType)
    {
        int index = (int)statType;
        return index >= 0 && index < StatCount ? index : 0;
    }

    public static bool IsNonZero(float value)
    {
        return Mathf.Abs(value) > ZeroTolerance;
    }

    public static bool IsAffectedByDurability(CharacterStatType statType)
    {
        return statType != CharacterStatType.Stamina &&
               statType != CharacterStatType.CarryWeight &&
               statType != CharacterStatType.ArtifactContainers;
    }

    public static float CalculateCurrentValue(CharacterStatModifier modifier, float durabilityPercent)
    {
        return CalculateCurrentValue(modifier.StatType, modifier.ValueAtFullDurability, durabilityPercent);
    }

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

    public static void AddModifiers(
        CharacterStatBlock target,
        IReadOnlyList<CharacterStatModifier> modifiers,
        float durabilityPercent)
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

    public static void AddItemStats(CharacterStatBlock target, InventoryItem item)
    {
        if (target == null || item == null || item.itemData == null)
        {
            return;
        }

        float durabilityPercent = item.HasDurability ? item.CurrentDurabilityPercent : 100f;
        AddModifiers(target, item.itemData.StatModifiers, durabilityPercent);
    }
}
