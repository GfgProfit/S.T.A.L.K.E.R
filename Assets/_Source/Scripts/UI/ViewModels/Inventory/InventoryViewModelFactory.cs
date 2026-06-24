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

    public static InventoryItemContextMenuViewModel CreateContextMenu(Action useAction, Action inspectAction, Action unloadAction, Action equipPrimaryWeaponAction, Action equipSecondaryWeaponAction, Action equipAction, Action unequipAction, Action dropOneAction, Action dropStackAction)
    {
        return new(useAction, inspectAction, unloadAction, equipPrimaryWeaponAction, equipSecondaryWeaponAction, equipAction, unequipAction, dropOneAction, dropStackAction);
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

    public static QuickUseHudSlotViewModel CreateQuickUseHudSlot()
    {
        return new();
    }

    public static QuickUseInventorySlotViewModel CreateQuickUseInventorySlot()
    {
        return new();
    }

    public static MiniActionTextViewModel CreateMiniActionText()
    {
        return new();
    }
}
