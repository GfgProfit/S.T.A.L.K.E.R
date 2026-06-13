using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterStats : MonoBehaviour
{
    [SerializeField] private List<CharacterStatModifier> baseStats = new List<CharacterStatModifier>();

    private readonly CharacterStatBlock baseStatBlock = new CharacterStatBlock();
    private readonly CharacterStatBlock equipmentStatBlock = new CharacterStatBlock();
    private readonly CharacterStatBlock currentStats = new CharacterStatBlock();

    public CharacterStatBlock CurrentStats => currentStats;

    private void Awake()
    {
        RebuildCurrentStats();
    }

    private void OnValidate()
    {
        RebuildCurrentStats();
    }

    public float GetStat(CharacterStatType statType)
    {
        return currentStats.Get(statType);
    }

    public void ApplyEquipmentStats(CharacterStatBlock equipmentStats)
    {
        equipmentStatBlock.CopyFrom(equipmentStats);
        RebuildCurrentStats();
    }

    private void RebuildCurrentStats()
    {
        baseStatBlock.Clear();
        CharacterStatUtility.AddModifiers(baseStatBlock, baseStats, 100f);

        currentStats.Clear();
        currentStats.Add(baseStatBlock);
        currentStats.Add(equipmentStatBlock);
    }
}
