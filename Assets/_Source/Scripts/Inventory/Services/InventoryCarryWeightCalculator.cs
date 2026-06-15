using System.Collections.Generic;

internal static class InventoryCarryWeightCalculator
{
    public static float Calculate(IList<InventoryItem> inventoryItems)
    {
        float totalWeight = 0f;

        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = inventoryItems[i];

            if (item == null)
            {
                inventoryItems.RemoveAt(i);
                continue;
            }

            totalWeight += item.TotalWeight;
        }

        return totalWeight;
    }
}