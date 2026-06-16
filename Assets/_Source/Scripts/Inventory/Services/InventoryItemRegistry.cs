using System;
using System.Collections.Generic;

internal sealed class InventoryItemRegistry
{
    private readonly List<InventoryItem> _items = new();

    public event Action<InventoryItem> DurabilityChanged;
    public IReadOnlyList<InventoryItem> Items => _items;

    public void Register(InventoryItem item)
    {
        if (item == null || _items.Contains(item))
        {
            return;
        }

        _items.Add(item);
        item.DurabilityChanged += HandleDurabilityChanged;
    }

    public void Unregister(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        if (_items.Remove(item))
        {
            item.DurabilityChanged -= HandleDurabilityChanged;
        }
    }

    public float CalculateCarryWeight() => InventoryCarryWeightCalculator.Calculate(_items);
    private void HandleDurabilityChanged(InventoryItem item) => DurabilityChanged?.Invoke(item);
}
