using System.Collections.Generic;

internal static class SlottedGridLayoutBuilder
{
    public static bool TryBuild(InventorySlotLayout slotLayout, float slotSpacing, bool centerRows, out SlottedGridLayoutBuildResult result, out string error)
    {
        result = null;

        if (slotLayout == null)
        {
            error = $"{nameof(SlottedItemGrid)} has no slot layout.";
            return false;
        }

        if (slotLayout.TryBuildSlots(out List<InventorySlotDefinition> definitions, out int gridSizeWidth, out int gridSizeHeight, out error) == false)
        {
            return false;
        }

        SlottedGridSlotState[,] slotByCell = new SlottedGridSlotState[gridSizeWidth, gridSizeHeight];
        List<SlottedGridSlotState> slots = BuildSlots(definitions, slotByCell);
        SlottedGridGeometry geometry = new(slotByCell, gridSizeWidth, gridSizeHeight, slotSpacing, centerRows);

        for (int i = 0; i < slots.Count; i++)
        {
            SlottedGridSlotState slot = slots[i];
            slot.VisualPosition = geometry.GetSlotVisualPosition(slot.Definition);
        }

        result = new(slots, slotByCell, gridSizeWidth, gridSizeHeight, geometry);
        return true;
    }

    private static List<SlottedGridSlotState> BuildSlots(IReadOnlyList<InventorySlotDefinition> definitions, SlottedGridSlotState[,] slotByCell)
    {
        List<SlottedGridSlotState> slots = new(definitions.Count);

        for (int i = 0; i < definitions.Count; i++)
        {
            InventorySlotDefinition definition = definitions[i];
            SlottedGridSlotState slot = new(definition);
            slot.IsClosed = SlottedGridSlotRules.IsArtifactSlot(slot);
            slots.Add(slot);

            FillSlotCells(slotByCell, definition, slot);
        }

        return slots;
    }

    private static void FillSlotCells(SlottedGridSlotState[,] slotByCell, InventorySlotDefinition definition, SlottedGridSlotState slot)
    {
        for (int x = 0; x < definition.Width; x++)
        {
            for (int y = 0; y < definition.Height; y++)
            {
                slotByCell[definition.X + x, definition.Y + y] = slot;
            }
        }
    }
}