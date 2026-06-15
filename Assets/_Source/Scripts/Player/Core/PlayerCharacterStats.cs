using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterStats : MonoBehaviour
{
    [SerializeField] private List<CharacterStatModifier> _baseStats = new();

    private readonly CharacterStatBlock _baseStatBlock = new();
    private readonly CharacterStatBlock _equipmentStatBlock = new();
    private readonly CharacterStatBlock _currentStats = new();

    public CharacterStatBlock CurrentStats => _currentStats;

    private void Awake()
    {
        RebuildCurrentStats();
    }

    private void OnValidate()
    {
        RebuildCurrentStats();
    }

    public float GetStat(CharacterStatType statType) => _currentStats.Get(statType);

    public void ApplyEquipmentStats(CharacterStatBlock equipmentStats)
    {
        _equipmentStatBlock.CopyFrom(equipmentStats);
        RebuildCurrentStats();
    }

    private void RebuildCurrentStats()
    {
        _baseStatBlock.Clear();
        CharacterStatUtility.AddModifiers(_baseStatBlock, _baseStats, 100f);

        _currentStats.Clear();
        _currentStats.Add(_baseStatBlock);
        _currentStats.Add(_equipmentStatBlock);
    }
}