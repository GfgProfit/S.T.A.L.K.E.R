using System;

[Serializable]
public readonly struct InventorySlotDefinition
{
    public string Id { get; }
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public bool RestrictItemType { get; }
    public ItemType AcceptedItemType { get; }

    public InventorySlotDefinition(string id, int x, int y, int width, int height) : this(id, x, y, width, height, false, ItemType.Misc)
    {
    }

    public InventorySlotDefinition(string id, int x, int y, int width, int height, bool restrictItemType, ItemType acceptedItemType)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        RestrictItemType = restrictItemType;
        AcceptedItemType = acceptedItemType;
    }

    public bool Contains(int cellX, int cellY) => cellX >= X && cellY >= Y && cellX < X + Width && cellY < Y + Height;

    public bool AcceptsItem(InventoryItem inventoryItem)
    {
        if (RestrictItemType == false)
        {
            return true;
        }

        return inventoryItem != null && inventoryItem.ItemData != null && inventoryItem.ItemData.ItemType == AcceptedItemType;
    }
}