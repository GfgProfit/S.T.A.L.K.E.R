using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryWeightStateController
{
    private readonly InventoryItemRegistry _itemRegistry;
    private readonly IReadOnlyList<EquipmentSlotGrid> _equipmentSlotGrids;
    private readonly InventoryEquipmentSlotService _equipmentSlotService;
    private readonly Action<float, float, float, float, bool> _setWeightState;
    private readonly Action<CharacterStatBlock, bool, bool> _renderCharacterStats;
    private readonly PlayerController _playerController;
    private readonly PlayerCharacterStats _playerStats;
    private readonly bool _hidePlayerStatsInfoWhenEmpty;
    private readonly float _maxCarryWeight;
    private readonly float _movementBlockExtraWeight;
    private readonly CharacterStatBlock _equippedStats = new();

    public InventoryWeightStateController(InventoryItemRegistry itemRegistry, IReadOnlyList<EquipmentSlotGrid> equipmentSlotGrids, InventoryEquipmentSlotService equipmentSlotService, Action<float, float, float, float, bool> setWeightState, Action<CharacterStatBlock, bool, bool> renderCharacterStats, PlayerController playerController, PlayerCharacterStats playerStats, bool hidePlayerStatsInfoWhenEmpty, float maxCarryWeight, float movementBlockExtraWeight)
    {
        _itemRegistry = itemRegistry;
        _equipmentSlotGrids = equipmentSlotGrids;
        _equipmentSlotService = equipmentSlotService;
        _setWeightState = setWeightState;
        _renderCharacterStats = renderCharacterStats;
        _playerController = playerController;
        _playerStats = playerStats;
        _hidePlayerStatsInfoWhenEmpty = hidePlayerStatsInfoWhenEmpty;
        _maxCarryWeight = maxCarryWeight;
        _movementBlockExtraWeight = movementBlockExtraWeight;
    }

    public float CurrentCarryWeight { get; private set; }
    public float BaseMaxCarryWeight => Mathf.Max(0f, _maxCarryWeight);
    public float MaxCarryWeight => Mathf.Max(0f, BaseMaxCarryWeight + GetCarryWeightBonusKg());
    public float MovementBlockWeight => MaxCarryWeight + Mathf.Max(0f, _movementBlockExtraWeight);
    public bool IsMovementBlockedByWeight => CurrentCarryWeight >= MovementBlockWeight;
    public bool IsSprintBlockedByEquipment => HasEquippedSprintBlockingItem();

    public void Refresh()
    {
        RefreshEquippedStats();
        CurrentCarryWeight = _itemRegistry.CalculateCarryWeight();
        PublishWeightState();
        ApplyMovementState();
        ApplySprintBlockState();
    }

    public void ApplyMovementState()
    {
        if (_playerController == null)
        {
            return;
        }

        _playerController.SetMovementEnabled(IsMovementBlockedByWeight == false);
    }

    private void ApplySprintBlockState()
    {
        if (_playerController == null)
        {
            return;
        }

        _playerController.SetSprintBlocked(PlayerSprintBlockSource.Equipment, IsSprintBlockedByEquipment);
    }

    private void RefreshEquippedStats()
    {
        _equippedStats.Clear();

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = _equipmentSlotGrids[i];
            InventoryItem item = grid == null ? null : grid.EquippedItem;

            if (ShouldApplyEquippedStats(item))
            {
                CharacterStatUtility.AddItemStats(_equippedStats, item.ItemData, item.CurrentDurabilityPercent);
            }
        }

        if (_playerStats != null)
        {
            _playerStats.ApplyEquipmentStats(_equippedStats);
        }

        _equipmentSlotService.RefreshSlotRestrictions();

        _renderCharacterStats?.Invoke(_playerStats == null ? _equippedStats : _playerStats.CurrentStats, _hidePlayerStatsInfoWhenEmpty, true);
    }

    private void PublishWeightState()
    {
        _setWeightState?.Invoke(CurrentCarryWeight, BaseMaxCarryWeight, MaxCarryWeight, MovementBlockWeight, IsMovementBlockedByWeight);
    }

    private float GetCarryWeightBonusKg() => _playerStats == null ? _equippedStats.Get(CharacterStatType.CarryWeight) : _playerStats.GetStat(CharacterStatType.CarryWeight);

    private bool HasEquippedSprintBlockingItem()
    {
        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            InventoryItem item = _equipmentSlotGrids[i] == null ? null : _equipmentSlotGrids[i].EquippedItem;

            if (item != null && item.ItemData != null && item.ItemData.BlocksSprint)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldApplyEquippedStats(InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        return item.ItemData.ItemType == ItemType.Armor || item.ItemData.ItemType == ItemType.Helmet;
    }
}
