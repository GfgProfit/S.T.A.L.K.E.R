using UnityEngine;

internal static class InventoryStackService
{
    public static int NormalizeAmount(ItemData itemData, int amount) => itemData != null && itemData.IsStackable ? Mathf.Max(1, amount) : 1;

    public static bool TryAddToExistingStack(ItemData itemData, int amount, InventoryGrid preferredGrid, InventoryGrid fallbackGrid)
    {
        if (itemData == null || itemData.IsStackable == false)
        {
            return false;
        }

        if (TryAddToExistingStackInGrid(preferredGrid, itemData, amount))
        {
            return true;
        }

        return preferredGrid != fallbackGrid && TryAddToExistingStackInGrid(fallbackGrid, itemData, amount);
    }

    public static bool TryAddToExistingStackInGrid(InventoryGrid grid, ItemData itemData, int amount)
    {
        if (grid == null)
        {
            return false;
        }

        if (grid.TryFindStack(itemData, out InventoryItem stack) == false)
        {
            return false;
        }

        stack.AddAmount(amount);
        return true;
    }
}