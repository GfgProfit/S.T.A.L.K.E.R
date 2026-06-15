using System;
using UnityEngine;

[Serializable]
public struct InventorySlotItemTypeRule
{
    [SerializeField] private string _slotId;
    [SerializeField] private ItemType _acceptedItemType;

    public readonly ItemType AcceptedItemType => _acceptedItemType;

    public readonly bool Matches(string targetSlotId) => string.Equals(_slotId?.Trim(), targetSlotId, StringComparison.Ordinal);
}