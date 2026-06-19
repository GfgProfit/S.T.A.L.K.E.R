using System.Collections.Generic;
using UnityEngine;

internal interface IInventoryItemCompatibilityProvider
{
    void CollectCompatibleItemData(InventoryItem sourceItem, ISet<ItemData> compatibleItemData);
}

internal sealed class WeaponAmmoInventoryCompatibilityProvider : IInventoryItemCompatibilityProvider
{
    public void CollectCompatibleItemData(InventoryItem sourceItem, ISet<ItemData> compatibleItemData)
    {
        ItemData sourceItemData = sourceItem == null ? null : sourceItem.ItemData;
        WeaponData weaponData = sourceItemData == null ? null : sourceItemData.WeaponData;

        if (weaponData == null || compatibleItemData == null)
        {
            return;
        }

        for (int i = 0; i < weaponData.CompatibleAmmo.Count; i++)
        {
            ItemData ammoData = weaponData.GetCompatibleAmmo(i);

            if (ammoData != null)
            {
                compatibleItemData.Add(ammoData);
            }
        }
    }
}

internal sealed class WeaponModuleInventoryCompatibilityProvider : IInventoryItemCompatibilityProvider
{
    public void CollectCompatibleItemData(InventoryItem sourceItem, ISet<ItemData> compatibleItemData)
    {
        WeaponModuleSupport.CollectInstallableModules(sourceItem, compatibleItemData);
    }
}

internal sealed class InventoryItemCompatibilityService
{
    private readonly InventoryItemRegistry _itemRegistry;
    private readonly IReadOnlyList<IInventoryItemCompatibilityProvider> _providers;
    private readonly Color _highlightColor;
    private readonly HashSet<ItemData> _compatibleItemData = new();

    public InventoryItemCompatibilityService(InventoryItemRegistry itemRegistry, IReadOnlyList<IInventoryItemCompatibilityProvider> providers, Color highlightColor)
    {
        _itemRegistry = itemRegistry;
        _providers = providers;
        _highlightColor = highlightColor;
    }

    public void ShowCompatibleItems(InventoryItem sourceItem)
    {
        _compatibleItemData.Clear();

        if (sourceItem != null && _providers != null)
        {
            for (int i = 0; i < _providers.Count; i++)
            {
                _providers[i]?.CollectCompatibleItemData(sourceItem, _compatibleItemData);
            }
        }

        IReadOnlyList<InventoryItem> items = _itemRegistry.Items;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];

            if (item != null)
            {
                item.SetCompatibilityHighlight(_compatibleItemData.Contains(item.ItemData), _highlightColor);
            }
        }
    }

    public void Clear() => ShowCompatibleItems(null);
}
