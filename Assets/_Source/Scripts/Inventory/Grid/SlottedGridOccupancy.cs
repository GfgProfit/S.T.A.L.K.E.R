using System;

internal sealed class SlottedGridOccupancy
{
    private InventoryItem[,] _items;

    public bool IsInitialized => _items != null;

    public void Initialize(int width, int height) => _items = new InventoryItem[width, height];
    public InventoryItem GetItem(int x, int y) => IsInBounds(x, y) ? _items[x, y] : null;

    public void PlaceItem(InventoryItem item, int posX, int posY, int width, int height)
    {
        if (item == null || _items == null)
        {
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _items[posX + x, posY + y] = item;
            }
        }
    }

    public void ClearItem(InventoryItem item, int width, int height)
    {
        if (item == null || _items == null)
        {
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int slotX = item.GridPositionX + x;
                int slotY = item.GridPositionY + y;

                if (IsInBounds(slotX, slotY) && _items[slotX, slotY] == item)
                {
                    _items[slotX, slotY] = null;
                }
            }
        }
    }

    public bool IsAreaEmpty(int posX, int posY, int width, int height)
    {
        if (_items == null)
        {
            return false;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int slotX = posX + x;
                int slotY = posY + y;

                if (IsInBounds(slotX, slotY) == false || _items[slotX, slotY] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryGetSingleOverlap(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        if (_items == null)
        {
            return false;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int slotX = posX + x;
                int slotY = posY + y;

                if (IsInBounds(slotX, slotY) == false)
                {
                    return false;
                }

                InventoryItem item = _items[slotX, slotY];

                if (item == null)
                {
                    continue;
                }

                if (overlapItem == null)
                {
                    overlapItem = item;
                }
                else if (overlapItem != item)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryFindStack(ItemData itemData, int width, int height, Func<int, int, bool> canMergeAt, out InventoryItem stack)
    {
        stack = null;

        if (itemData == null || itemData.IsStackable == false || _items == null)
        {
            return false;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventoryItem item = _items[x, y];

                if (item != null && canMergeAt(x, y) && item.CanStackWith(itemData))
                {
                    stack = item;
                    return true;
                }
            }
        }

        return false;
    }

    public InventoryItem GetFirstOccupant(InventorySlotDefinition definition)
    {
        if (_items == null)
        {
            return null;
        }

        for (int x = 0; x < definition.Width; x++)
        {
            for (int y = 0; y < definition.Height; y++)
            {
                InventoryItem item = GetItem(definition.X + x, definition.Y + y);

                if (item != null)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private bool IsInBounds(int x, int y) => _items != null && x >= 0 && y >= 0 && x < _items.GetLength(0) && y < _items.GetLength(1);
}