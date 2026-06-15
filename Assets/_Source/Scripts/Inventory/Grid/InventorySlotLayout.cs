using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Slot Layout")]
public class InventorySlotLayout : ScriptableObject
{
    [SerializeField] [TextArea(2, 12)] private string _slotMask = "XXA\nCUA";
    [SerializeField] private List<InventorySlotItemTypeRule> _itemTypeRules = new();

    public bool TryBuildSlots(out List<InventorySlotDefinition> slots, out int width, out int height, out string error)
    {
        slots = new();
        width = 0;
        height = 0;
        error = string.Empty;

        string[] rows = _slotMask.Replace("\r", string.Empty).Split('\n');
        height = rows.Length;

        Dictionary<char, SlotBounds> boundsById = new Dictionary<char, SlotBounds>();

        for (int y = 0; y < rows.Length; y++)
        {
            width = Mathf.Max(width, rows[y].Length);

            for (int x = 0; x < rows[y].Length; x++)
            {
                char slotId = rows[y][x];

                if (IsEmptyCell(slotId))
                {
                    continue;
                }

                if (boundsById.TryGetValue(slotId, out SlotBounds bounds))
                {
                    bounds.Add(x, y);
                    boundsById[slotId] = bounds;
                }
                else
                {
                    boundsById.Add(slotId, new SlotBounds(slotId, x, y));
                }
            }
        }

        if (boundsById.Count == 0)
        {
            error = "Slot layout has no slots.";
            return false;
        }

        foreach (SlotBounds bounds in boundsById.Values)
        {
            int slotWidth = bounds.MaxX - bounds.MinX + 1;
            int slotHeight = bounds.MaxY - bounds.MinY + 1;

            if (slotWidth * slotHeight != bounds.CellCount)
            {
                error = $"Slot '{bounds.Id}' must be a filled rectangle.";
                return false;
            }

            for (int y = bounds.MinY; y <= bounds.MaxY; y++)
            {
                for (int x = bounds.MinX; x <= bounds.MaxX; x++)
                {
                    if (GetCell(rows, x, y) != bounds.Id)
                    {
                        error = $"Slot '{bounds.Id}' must not contain holes or other slot ids.";
                        return false;
                    }
                }
            }

            string slotId = bounds.Id.ToString();
            bool restrictItemType = TryGetAcceptedItemType(slotId, out ItemType acceptedItemType);
            slots.Add(new InventorySlotDefinition(slotId, bounds.MinX, bounds.MinY, slotWidth, slotHeight, restrictItemType, acceptedItemType));
        }

        slots.Sort((left, right) =>
        {
            int yCompare = left.Y.CompareTo(right.Y);
            return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
        });

        return true;
    }

    private static bool IsEmptyCell(char cell) => cell == ' ' || cell == '.' || cell == '_' || cell == '-';

    private static char GetCell(string[] rows, int x, int y)
    {
        if (y < 0 || y >= rows.Length)
        {
            return '\0';
        }

        if (x < 0 || x >= rows[y].Length)
        {
            return '\0';
        }

        char cell = rows[y][x];
        return IsEmptyCell(cell) ? '\0' : cell;
    }

    private bool TryGetAcceptedItemType(string slotId, out ItemType acceptedItemType)
    {
        acceptedItemType = ItemType.Misc;

        if (_itemTypeRules == null)
        {
            return false;
        }

        for (int i = 0; i < _itemTypeRules.Count; i++)
        {
            InventorySlotItemTypeRule rule = _itemTypeRules[i];

            if (rule.Matches(slotId) == false)
            {
                continue;
            }

            acceptedItemType = rule.AcceptedItemType;
            return true;
        }

        return false;
    }

    private struct SlotBounds
    {
        public readonly char Id;
        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }
        public int CellCount { get; private set; }

        public SlotBounds(char id, int x, int y)
        {
            Id = id;
            MinX = x;
            MaxX = x;
            MinY = y;
            MaxY = y;
            CellCount = 1;
        }

        public void Add(int x, int y)
        {
            MinX = Mathf.Min(MinX, x);
            MaxX = Mathf.Max(MaxX, x);
            MinY = Mathf.Min(MinY, y);
            MaxY = Mathf.Max(MaxY, y);
            CellCount++;
        }
    }
}