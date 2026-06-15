using System;
using System.Threading;
using Cysharp.Threading.Tasks;

internal static class InventoryViewModelFactory
{
    public static InventoryViewModel CreateInventory(Func<CancellationToken, UniTask> toggleOpenAsync, GameProjectSettings settings)
    {
        return new(toggleOpenAsync, settings);
    }

    public static InventoryItemViewModel CreateInventoryItem()
    {
        return new();
    }

    public static InventoryItemContextMenuViewModel CreateContextMenu(Action dropOneAction, Action dropStackAction)
    {
        return new(dropOneAction, dropStackAction);
    }

    public static InventoryHighlightViewModel CreateHighlight()
    {
        return new();
    }

    public static ItemTooltipViewModel CreateItemTooltip()
    {
        return new();
    }

    public static CharacterStatsInfoPanelViewModel CreateCharacterStatsInfoPanel()
    {
        return new();
    }

    public static CharacterStatRowViewModel CreateCharacterStatRow()
    {
        return new();
    }
}
