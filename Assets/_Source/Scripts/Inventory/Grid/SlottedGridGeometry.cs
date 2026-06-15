using System.Collections.Generic;
using UnityEngine;

internal sealed class SlottedGridGeometry
{
    private readonly SlottedGridSlotState[,] _slotByCell;
    private readonly int _gridSizeWidth;
    private readonly int _gridSizeHeight;
    private readonly float _slotSpacing;
    private readonly bool _centerRows;
    private readonly float[] _rowVisualPositions;
    private readonly float[] _rowLocalWidths;
    private readonly float _maxRowLocalWidth;

    public SlottedGridGeometry(SlottedGridSlotState[,] slotByCell, int gridSizeWidth, int gridSizeHeight, float slotSpacing, bool centerRows)
    {
        _slotByCell = slotByCell;
        _gridSizeWidth = gridSizeWidth;
        _gridSizeHeight = gridSizeHeight;
        _slotSpacing = slotSpacing;
        _centerRows = centerRows;
        _rowVisualPositions = new float[_gridSizeHeight];
        _rowLocalWidths = new float[_gridSizeHeight];

        for (int y = 0; y < _gridSizeHeight; y++)
        {
            _rowLocalWidths[y] = CalculateRowLocalWidth(y);
            _maxRowLocalWidth = Mathf.Max(_maxRowLocalWidth, _rowLocalWidths[y]);
        }

        for (int y = 1; y < _gridSizeHeight; y++)
        {
            float gap = HasRowBoundary(y - 1) ? _slotSpacing : 0f;
            _rowVisualPositions[y] = _rowVisualPositions[y - 1] + ItemGrid.TILE_SIZE_HEIGHT + gap;
        }
    }

    public Vector2 GetSlotVisualPosition(InventorySlotDefinition definition)
    {
        float x = GetSlotVisualX(definition);
        float y = _rowVisualPositions[definition.Y];

        return new(x, y);
    }

    public Vector2 GetSlotVisualSize(InventorySlotDefinition definition)
    {
        float width = definition.Width * ItemGrid.TILE_SIZE_WIDTH;
        float height = definition.Height * ItemGrid.TILE_SIZE_HEIGHT;

        return new(width, height);
    }

    public Vector2 GetGridVisualSize(IReadOnlyList<SlottedGridSlotState> slots)
    {
        Vector2 size = Vector2.zero;

        for (int i = 0; i < slots.Count; i++)
        {
            SlottedGridSlotState slot = slots[i];
            Vector2 slotSize = GetSlotVisualSize(slot.Definition);
            size.x = Mathf.Max(size.x, slot.VisualPosition.x + slotSize.x);
            size.y = Mathf.Max(size.y, slot.VisualPosition.y + slotSize.y);
        }

        return size;
    }

    public SlottedGridSlotState GetSlotAtVisualPosition(IReadOnlyList<SlottedGridSlotState> slots, Vector2 visualPosition)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            SlottedGridSlotState slot = slots[i];
            Vector2 slotSize = GetSlotVisualSize(slot.Definition);

            if (visualPosition.x >= slot.VisualPosition.x && visualPosition.y >= slot.VisualPosition.y && visualPosition.x < slot.VisualPosition.x + slotSize.x && visualPosition.y < slot.VisualPosition.y + slotSize.y)
            {
                return slot;
            }
        }

        return null;
    }

    private float GetSlotVisualX(InventorySlotDefinition definition)
    {
        float x = 0f;

        for (int y = definition.Y; y < definition.Y + definition.Height; y++)
        {
            x = Mathf.Max(x, CalculateSlotLocalX(definition.X, y) + GetRowOffset(y));
        }

        return x;
    }

    private float GetRowOffset(int y)
    {
        if (_centerRows == false)
        {
            return 0f;
        }

        return (_maxRowLocalWidth - _rowLocalWidths[y]) / 2f;
    }

    private float CalculateRowLocalWidth(int y)
    {
        float width = 0f;
        bool hasVisibleRun = false;
        SlottedGridSlotState previousRun = null;

        for (int x = 0; x < _gridSizeWidth; x++)
        {
            SlottedGridSlotState current = GetSlotAtCell(x, y);

            if (current == null)
            {
                previousRun = null;
                continue;
            }

            if (current == previousRun)
            {
                continue;
            }

            if (hasVisibleRun)
            {
                width += _slotSpacing;
            }

            width += CountRunWidthInRow(x, y, current) * ItemGrid.TILE_SIZE_WIDTH;
            hasVisibleRun = true;
            previousRun = current;
        }

        return width;
    }

    private float CalculateSlotLocalX(int targetX, int y)
    {
        float xPosition = 0f;
        bool hasVisibleRun = false;
        SlottedGridSlotState previousRun = null;

        for (int x = 0; x < _gridSizeWidth; x++)
        {
            SlottedGridSlotState current = GetSlotAtCell(x, y);

            if (current == null)
            {
                previousRun = null;
                continue;
            }

            if (current == previousRun)
            {
                continue;
            }

            if (hasVisibleRun)
            {
                xPosition += _slotSpacing;
            }

            if (x == targetX)
            {
                return xPosition;
            }

            xPosition += CountRunWidthInRow(x, y, current) * ItemGrid.TILE_SIZE_WIDTH;
            hasVisibleRun = true;
            previousRun = current;
        }

        return targetX * ItemGrid.TILE_SIZE_WIDTH;
    }

    private int CountRunWidthInRow(int startX, int y, SlottedGridSlotState slot)
    {
        int width = 0;

        for (int x = startX; x < _gridSizeWidth; x++)
        {
            if (GetSlotAtCell(x, y) != slot)
            {
                break;
            }

            width++;
        }

        return width;
    }

    private bool HasRowBoundary(int topRow)
    {
        for (int x = 0; x < _gridSizeWidth; x++)
        {
            if (HasSlotBoundary(x, topRow, x, topRow + 1))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSlotBoundary(int firstX, int firstY, int secondX, int secondY)
    {
        SlottedGridSlotState first = GetSlotAtCell(firstX, firstY);
        SlottedGridSlotState second = GetSlotAtCell(secondX, secondY);

        return first != second && (first != null || second != null);
    }

    private SlottedGridSlotState GetSlotAtCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _gridSizeWidth || y >= _gridSizeHeight)
        {
            return null;
        }

        return _slotByCell[x, y];
    }
}