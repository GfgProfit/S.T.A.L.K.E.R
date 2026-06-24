using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryItemFactory
{
    private readonly InventoryItem _itemPrefab;

    public InventoryItemFactory(InventoryItem itemPrefab) => _itemPrefab = itemPrefab;

    public InventoryItem Create(ItemData itemData, int amount, float? durabilityPercent, IReadOnlyList<ItemData> installedModules = null, FirstPersonWeaponMagazineState weaponMagazineState = null)
    {
        InventoryItem inventoryItem = Object.Instantiate(_itemPrefab);
        inventoryItem.Set(itemData, amount, durabilityPercent, installedModules, weaponMagazineState);
        return inventoryItem;
    }
}
