using System.Collections.Generic;
using TMPro;
using UnityEngine;

internal sealed class InventoryWeightStateController
{
    private readonly InventoryItemRegistry _itemRegistry;
    private readonly IReadOnlyList<EquipmentSlotGrid> _equipmentSlotGrids;
    private readonly InventoryEquipmentSlotService _equipmentSlotService;
    private readonly TMP_Text _weightText;
    private readonly PlayerController _playerController;
    private readonly PlayerCharacterStats _playerStats;
    private readonly CharacterStatsInfoPanel _playerStatsInfoPanel;
    private readonly bool _hidePlayerStatsInfoWhenEmpty;
    private readonly float _maxCarryWeight;
    private readonly float _movementBlockExtraWeight;
    private readonly CharacterStatBlock _equippedStats = new();

    public InventoryWeightStateController(InventoryItemRegistry itemRegistry, IReadOnlyList<EquipmentSlotGrid> equipmentSlotGrids, InventoryEquipmentSlotService equipmentSlotService, TMP_Text weightText, PlayerController playerController, PlayerCharacterStats playerStats, CharacterStatsInfoPanel playerStatsInfoPanel, bool hidePlayerStatsInfoWhenEmpty, float maxCarryWeight, float movementBlockExtraWeight)
    {
        _itemRegistry = itemRegistry;
        _equipmentSlotGrids = equipmentSlotGrids;
        _equipmentSlotService = equipmentSlotService;
        _weightText = weightText;
        _playerController = playerController;
        _playerStats = playerStats;
        _playerStatsInfoPanel = playerStatsInfoPanel;
        _hidePlayerStatsInfoWhenEmpty = hidePlayerStatsInfoWhenEmpty;
        _maxCarryWeight = maxCarryWeight;
        _movementBlockExtraWeight = movementBlockExtraWeight;
    }

    public float CurrentCarryWeight { get; private set; }
    public float BaseMaxCarryWeight => Mathf.Max(0f, _maxCarryWeight);
    public float MaxCarryWeight => Mathf.Max(0f, BaseMaxCarryWeight + GetCarryWeightBonusKg());
    public float MovementBlockWeight => MaxCarryWeight + Mathf.Max(0f, _movementBlockExtraWeight);
    public bool IsMovementBlockedByWeight => CurrentCarryWeight >= MovementBlockWeight;

    public void Refresh()
    {
        RefreshEquippedStats();
        CurrentCarryWeight = _itemRegistry.CalculateCarryWeight();
        RefreshWeightText();
        ApplyMovementState();
    }

    public void ApplyMovementState()
    {
        if (_playerController == null)
        {
            return;
        }

        _playerController.SetMovementEnabled(IsMovementBlockedByWeight == false);
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
                CharacterStatUtility.AddItemStats(_equippedStats, item);
            }
        }

        if (_playerStats != null)
        {
            _playerStats.ApplyEquipmentStats(_equippedStats);
        }

        _equipmentSlotService.RefreshSlotRestrictions();

        if (_playerStatsInfoPanel != null)
        {
            GameProjectSettings settings = GameProjectSettings.LoadDefault();
            _playerStatsInfoPanel.RenderCharacterStats(_playerStats == null ? _equippedStats : _playerStats.CurrentStats, settings.StatCurrentValueColor, _hidePlayerStatsInfoWhenEmpty, true);
        }
    }

    private void RefreshWeightText()
    {
        if (_weightText == null)
        {
            return;
        }

        _weightText.raycastTarget = false;
        _weightText.richText = true;
        GameProjectSettings settings = GameProjectSettings.LoadDefault();
        _weightText.color = settings.NormalWeightColor;
        _weightText.text = InventoryWeightTextFormatter.BuildText(CurrentCarryWeight, MaxCarryWeight, MovementBlockWeight, settings);
    }

    private float GetCarryWeightBonusKg() => _playerStats == null ? _equippedStats.Get(CharacterStatType.CarryWeight) : _playerStats.GetStat(CharacterStatType.CarryWeight);

    private static bool ShouldApplyEquippedStats(InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        return item.ItemData.ItemType == ItemType.Armor || item.ItemData.ItemType == ItemType.Helmet;
    }
}