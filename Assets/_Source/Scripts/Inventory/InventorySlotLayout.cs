using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Slot Layout")]
public class InventorySlotLayout : ScriptableObject
{
    [SerializeField] [TextArea(2, 12)] private string slotMask = "XXA\nCUA";

    public bool TryBuildSlots(out List<InventorySlotDefinition> slots, out int width, out int height, out string error)
    {
        slots = new List<InventorySlotDefinition>();
        width = 0;
        height = 0;
        error = string.Empty;

        string[] rows = slotMask.Replace("\r", string.Empty).Split('\n');
        height = rows.Length;

        Dictionary<char, SlotBounds> boundsById = new Dictionary<char, SlotBounds>();

        for (int y = 0; y < rows.Length; y++)
        {
            width = Mathf.Max(width, rows[y].Length);

            for (int x = 0; x < rows[y].Length; x++)
            {
                char slotId = rows[y][x];
                if (IsEmptyCell(slotId)) { continue; }

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

            slots.Add(new InventorySlotDefinition(bounds.Id.ToString(), bounds.MinX, bounds.MinY, slotWidth, slotHeight));
        }

        slots.Sort((left, right) =>
        {
            int yCompare = left.y.CompareTo(right.y);
            return yCompare != 0 ? yCompare : left.x.CompareTo(right.x);
        });

        return true;
    }

    private static bool IsEmptyCell(char cell)
    {
        return cell == ' ' || cell == '.' || cell == '_' || cell == '-';
    }

    private static char GetCell(string[] rows, int x, int y)
    {
        if (y < 0 || y >= rows.Length) { return '\0'; }
        if (x < 0 || x >= rows[y].Length) { return '\0'; }

        char cell = rows[y][x];
        return IsEmptyCell(cell) ? '\0' : cell;
    }

    private struct SlotBounds
    {
        public readonly char Id;
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
        public int CellCount;

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

[Serializable]
public struct InventorySlotDefinition
{
    public string id;
    public int x;
    public int y;
    public int width;
    public int height;

    public InventorySlotDefinition(string id, int x, int y, int width, int height)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public bool Contains(int cellX, int cellY)
    {
        return cellX >= x && cellY >= y && cellX < x + width && cellY < y + height;
    }
}
