using System;
using UnityEngine;

[Serializable]
public struct CharacterStatModifier
{
    [SerializeField] private CharacterStatType _statType;
    [SerializeField] private float _valueAtFullDurability;

    public readonly CharacterStatType StatType => _statType;
    public readonly float ValueAtFullDurability => _valueAtFullDurability;
    public readonly bool HasValue => CharacterStatUtility.IsNonZero(_valueAtFullDurability);

    public CharacterStatModifier(CharacterStatType statType, float valueAtFullDurability)
    {
        _statType = statType;
        _valueAtFullDurability = valueAtFullDurability;
    }
}