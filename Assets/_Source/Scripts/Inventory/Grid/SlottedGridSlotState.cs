using UnityEngine;

internal sealed class SlottedGridSlotState
{
    public SlottedGridSlotState(InventorySlotDefinition definition)
    {
        Definition = definition;
    }

    public InventorySlotDefinition Definition { get; }
    public Vector2 VisualPosition { get; set; }
    public RectTransform VisualRoot { get; set; }
    public RectTransform ClosedSlotInstance { get; set; }
    public bool IsClosed { get; set; }
}