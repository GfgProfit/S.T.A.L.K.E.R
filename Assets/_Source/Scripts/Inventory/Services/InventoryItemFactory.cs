using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemFactory
{
    private readonly InventoryItem _itemPrefab;

    public InventoryItemFactory(InventoryItem itemPrefab) => _itemPrefab = itemPrefab;

    public InventoryItem Create(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts, float? durabilityPercent)
    {
        InventoryItem inventoryItem = Object.Instantiate(_itemPrefab);
        inventoryItem.Set(itemData, amount, runtimeIconParts, durabilityPercent);
        return inventoryItem;
    }
}