using System.Collections.Generic;

internal sealed class SlottedGridLayoutBuildResult
{
    public SlottedGridLayoutBuildResult(List<SlottedGridSlotState> slots, SlottedGridSlotState[,] slotByCell, int gridSizeWidth, int gridSizeHeight, SlottedGridGeometry geometry)
    {
        Slots = slots;
        SlotByCell = slotByCell;
        GridSizeWidth = gridSizeWidth;
        GridSizeHeight = gridSizeHeight;
        Geometry = geometry;
    }

    public List<SlottedGridSlotState> Slots { get; }
    public SlottedGridSlotState[,] SlotByCell { get; }
    public int GridSizeWidth { get; }
    public int GridSizeHeight { get; }
    public SlottedGridGeometry Geometry { get; }
}