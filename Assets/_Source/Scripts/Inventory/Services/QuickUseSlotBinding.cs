using System;
using UnityEngine;

[Serializable]
public sealed class QuickUseSlotBinding
{
    [SerializeField] [Min(0)] private int _slotIndex;
    [SerializeField] private InventoryGrid _inventorySlot;
    [SerializeField] private QuickUseInventorySlotView _inventorySlotView;
    [SerializeField] private QuickUseHudSlotView _hudSlotView;

    public int SlotIndex => _slotIndex;
    public InventoryGrid InventorySlot => _inventorySlot;
    public QuickUseInventorySlotView InventorySlotView => _inventorySlotView;
    public QuickUseHudSlotView HudSlotView => _hudSlotView;
}
