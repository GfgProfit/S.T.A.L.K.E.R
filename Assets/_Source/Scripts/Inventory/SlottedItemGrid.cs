using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlottedItemGrid : InventoryGrid
{
    [SerializeField] private InventorySlotLayout slotLayout;
    [SerializeField] private float slotSpacing = 10f;
    [SerializeField] private Image slotImageTemplate;
    [SerializeField] private bool hideSourceImage = true;
    [SerializeField] private bool centerRows = true;

    private readonly List<SlotState> slots = new List<SlotState>();
    private SlotState[,] slotByCell;
    private InventoryItem[,] inventoryItemSlot;
    private Vector2 positionOnTheGrid = new Vector2();
    private Vector2Int tileGridPosition = new Vector2Int();
    private RectTransform rectTransform;
    private Camera uiCamera;
    private int gridSizeWidth;
    private int gridSizeHeight;
    private float[] rowVisualPositions;
    private float[] rowLocalWidths;
    private float maxRowLocalWidth;
    private RectTransform slotVisualRoot;
    private Image sourceImage;
    private InventoryGridVisualSettings visualSettings;

    public override int HighlightSiblingIndex => slotVisualRoot == null ? 0 : 1;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        visualSettings = InventoryGridVisualSettings.LoadDefault();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        sourceImage = slotImageTemplate != null ? slotImageTemplate : GetComponent<Image>();

        InitFromLayout();
    }

    public override InventoryItem PickUpItem(int x, int y)
    {
        if (PositionCheck(x, y) == false) { return null; }

        InventoryItem item = inventoryItemSlot[x, y];
        if (item == null) { return null; }

        CleanGridReference(item);

        return item;
    }

    public override InventoryItem GetItem(int x, int y)
    {
        if (PositionCheck(x, y) == false) { return null; }

        return inventoryItemSlot[x, y];
    }

    public override Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, uiCamera, out Vector2 localPoint);

        positionOnTheGrid.x = localPoint.x + rectTransform.pivot.x * rectTransform.rect.width;
        positionOnTheGrid.y = (1f - rectTransform.pivot.y) * rectTransform.rect.height - localPoint.y;

        if (selectedItem != null)
        {
            positionOnTheGrid.x -= (selectedItem.Width - 1) * ItemGrid.tileSizeWidth / 2f;
            positionOnTheGrid.y -= (selectedItem.Height - 1) * ItemGrid.tileSizeHeight / 2f;
        }

        SlotState slot = GetSlotAtVisualPosition(positionOnTheGrid);
        if (slot == null) { return new Vector2Int(-1, -1); }

        tileGridPosition.x = slot.Definition.x + Mathf.FloorToInt((positionOnTheGrid.x - slot.VisualPosition.x) / ItemGrid.tileSizeWidth);
        tileGridPosition.y = slot.Definition.y + Mathf.FloorToInt((positionOnTheGrid.y - slot.VisualPosition.y) / ItemGrid.tileSizeHeight);

        return tileGridPosition;
    }

    public override Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        foreach (SlotState slot in slots)
        {
            int maxX = slot.Definition.x + slot.Definition.width - itemToInsert.Width;
            int maxY = slot.Definition.y + slot.Definition.height - itemToInsert.Height;

            for (int y = slot.Definition.y; y <= maxY; y++)
            {
                for (int x = slot.Definition.x; x <= maxX; x++)
                {
                    if (CheckAvailableSpace(x, y, itemToInsert.Width, itemToInsert.Height))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
        }

        return null;
    }

    public override bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (BoundryCheck(posX, posY, inventoryItem.Width, inventoryItem.Height) == false)
        {
            return false;
        }

        if (OverlapCheck(posX, posY, inventoryItem.Width, inventoryItem.Height, ref overlapItem) == false)
        {
            overlapItem = null;
            return false;
        }

        if (overlapItem != null)
        {
            CleanGridReference(overlapItem);
        }

        PlaceItem(inventoryItem, posX, posY);

        return true;
    }

    public override bool CanPlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        if (inventoryItem == null)
        {
            return false;
        }

        return CheckAvailableSpace(posX, posY, inventoryItem.Width, inventoryItem.Height);
    }

    public override void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        if (BoundryCheck(posX, posY, inventoryItem.Width, inventoryItem.Height) == false) { return; }

        RectTransform itemRectTransform = inventoryItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(rectTransform, false);
        itemRectTransform.localScale = Vector3.one;

        for (int x = 0; x < inventoryItem.Width; x++)
        {
            for (int y = 0; y < inventoryItem.Height; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem;
            }
        }

        inventoryItem.onGridPositionX = posX;
        inventoryItem.onGridPositionY = posY;
        itemRectTransform.localPosition = CalculatePositionOnGrid(inventoryItem, posX, posY);
        inventoryItem.SetCellVisualsVisible(true);
    }

    public override Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        SlotState slot = GetSlotAtCell(posX, posY);
        if (slot != null)
        {
            return new Vector2(
                slot.VisualPosition.x + (posX - slot.Definition.x) * ItemGrid.tileSizeWidth + ItemGrid.tileSizeWidth * inventoryItem.Width / 2f,
                -(slot.VisualPosition.y + (posY - slot.Definition.y) * ItemGrid.tileSizeHeight + ItemGrid.tileSizeHeight * inventoryItem.Height / 2f));
        }

        Vector2 position = new Vector2();
        position.x = posX * ItemGrid.tileSizeWidth + ItemGrid.tileSizeWidth * inventoryItem.Width / 2;
        position.y = -(posY * ItemGrid.tileSizeHeight + ItemGrid.tileSizeHeight * inventoryItem.Height / 2);

        return position;
    }

    public override bool BoundryCheck(int posX, int posY, int width, int height)
    {
        SlotState slot = GetSlotAtCell(posX, posY);
        if (slot == null) { return false; }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (GetSlotAtCell(posX + x, posY + y) != slot)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void InitFromLayout()
    {
        slots.Clear();

        if (slotLayout == null)
        {
            Debug.LogError($"{nameof(SlottedItemGrid)} has no slot layout.", this);
            return;
        }

        if (slotLayout.TryBuildSlots(out List<InventorySlotDefinition> definitions, out gridSizeWidth, out gridSizeHeight, out string error) == false)
        {
            Debug.LogError(error, slotLayout);
            return;
        }

        slotByCell = new SlotState[gridSizeWidth, gridSizeHeight];
        inventoryItemSlot = new InventoryItem[gridSizeWidth, gridSizeHeight];

        foreach (InventorySlotDefinition definition in definitions)
        {
            SlotState slot = new SlotState(definition);
            slots.Add(slot);

            for (int x = 0; x < definition.width; x++)
            {
                for (int y = 0; y < definition.height; y++)
                {
                    slotByCell[definition.x + x, definition.y + y] = slot;
                }
            }
        }

        BuildVisualAxes();

        foreach (SlotState slot in slots)
        {
            slot.VisualPosition = GetSlotVisualPosition(slot.Definition);
        }

        rectTransform.sizeDelta = GetGridVisualSize();
        CreateSlotVisuals();
    }

    private void CleanGridReference(InventoryItem item)
    {
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                inventoryItemSlot[item.onGridPositionX + x, item.onGridPositionY + y] = null;
            }
        }
    }

    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        if (BoundryCheck(posX, posY, width, height) == false) { return false; }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventoryItem item = inventoryItemSlot[posX + x, posY + y];
                if (item == null) { continue; }

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

    private SlotState GetSlotAtCell(int x, int y)
    {
        if (slotByCell == null) { return null; }
        if (PositionCheck(x, y) == false) { return null; }

        return slotByCell[x, y];
    }

    private bool PositionCheck(int x, int y)
    {
        return x >= 0 && y >= 0 && x < gridSizeWidth && y < gridSizeHeight;
    }

    private SlotState GetSlotAtVisualPosition(Vector2 visualPosition)
    {
        foreach (SlotState slot in slots)
        {
            Vector2 slotSize = GetSlotVisualSize(slot.Definition);

            if (visualPosition.x >= slot.VisualPosition.x &&
                visualPosition.y >= slot.VisualPosition.y &&
                visualPosition.x < slot.VisualPosition.x + slotSize.x &&
                visualPosition.y < slot.VisualPosition.y + slotSize.y)
            {
                return slot;
            }
        }

        return null;
    }

    private sealed class SlotState
    {
        public readonly InventorySlotDefinition Definition;
        public Vector2 VisualPosition;

        public SlotState(InventorySlotDefinition definition)
        {
            Definition = definition;
        }
    }

    private void CreateSlotVisuals()
    {
        GameObject rootObject = new GameObject("Slot Visuals", typeof(RectTransform));
        rootObject.transform.SetParent(rectTransform, false);

        slotVisualRoot = rootObject.GetComponent<RectTransform>();
        slotVisualRoot.anchorMin = new Vector2(0f, 1f);
        slotVisualRoot.anchorMax = new Vector2(0f, 1f);
        slotVisualRoot.pivot = new Vector2(0f, 1f);
        slotVisualRoot.anchoredPosition = Vector2.zero;
        slotVisualRoot.sizeDelta = rectTransform.sizeDelta;
        slotVisualRoot.SetAsFirstSibling();

        foreach (SlotState slot in slots)
        {
            GameObject slotObject = new GameObject($"Slot {slot.Definition.id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slotObject.transform.SetParent(slotVisualRoot, false);

            RectTransform slotRectTransform = slotObject.GetComponent<RectTransform>();
            slotRectTransform.anchorMin = new Vector2(0f, 1f);
            slotRectTransform.anchorMax = new Vector2(0f, 1f);
            slotRectTransform.pivot = new Vector2(0f, 1f);
            slotRectTransform.anchoredPosition = new Vector2(slot.VisualPosition.x, -slot.VisualPosition.y);
            slotRectTransform.sizeDelta = GetSlotVisualSize(slot.Definition);

            Image slotImage = slotObject.GetComponent<Image>();
            CopyImageSettings(slotImage, sourceImage);
            slotImage.raycastTarget = true;

            CreateCellGridLines(slotRectTransform, slot.Definition);

            slotObject.AddComponent<GridInteract>();
        }

        if (hideSourceImage && sourceImage != null && sourceImage.transform == transform)
        {
            sourceImage.enabled = false;
        }
    }

    private void CopyImageSettings(Image target, Image source)
    {
        if (source == null)
        {
            target.color = Color.white;
            return;
        }

        target.sprite = source.sprite;
        target.color = source.color;
        target.material = source.material;
        target.type = source.type;
        target.preserveAspect = source.preserveAspect;
        target.fillCenter = source.fillCenter;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
    }

    private void CreateCellGridLines(RectTransform slotRectTransform, InventorySlotDefinition definition)
    {
        if (visualSettings == null)
        {
            visualSettings = InventoryGridVisualSettings.LoadDefault();
        }

        if (visualSettings.ShowCellGrid == false) { return; }

        Vector2 slotSize = GetSlotVisualSize(definition);
        int firstVerticalLine = visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastVerticalLine = visualSettings.ShowCellGridBorder ? definition.width : definition.width - 1;
        int firstHorizontalLine = visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastHorizontalLine = visualSettings.ShowCellGridBorder ? definition.height : definition.height - 1;

        for (int x = firstVerticalLine; x <= lastVerticalLine; x++)
        {
            bool isBorderLine = x == 0 || x == definition.width;

            CreateGridLine(
                slotRectTransform,
                $"Vertical Grid Line {x}",
                new Vector2(x * ItemGrid.tileSizeWidth, 0f),
                new Vector2(visualSettings.GetLineThickness(isBorderLine), slotSize.y),
                new Vector2(0.5f, 1f),
                visualSettings.GetLineColor(isBorderLine));
        }

        for (int y = firstHorizontalLine; y <= lastHorizontalLine; y++)
        {
            bool isBorderLine = y == 0 || y == definition.height;

            CreateGridLine(
                slotRectTransform,
                $"Horizontal Grid Line {y}",
                new Vector2(0f, -y * ItemGrid.tileSizeHeight),
                new Vector2(slotSize.x, visualSettings.GetLineThickness(isBorderLine)),
                new Vector2(0f, 0.5f),
                visualSettings.GetLineColor(isBorderLine));
        }
    }

    private void CreateGridLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 pivot, Color color)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        RectTransform lineRectTransform = lineObject.GetComponent<RectTransform>();
        lineRectTransform.anchorMin = new Vector2(0f, 1f);
        lineRectTransform.anchorMax = new Vector2(0f, 1f);
        lineRectTransform.pivot = pivot;
        lineRectTransform.anchoredPosition = anchoredPosition;
        lineRectTransform.sizeDelta = size;

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }

    private Vector2 GetSlotVisualPosition(InventorySlotDefinition definition)
    {
        float x = GetSlotVisualX(definition);
        float y = rowVisualPositions == null ? definition.y * ItemGrid.tileSizeHeight : rowVisualPositions[definition.y];

        return new Vector2(x, y);
    }

    private Vector2 GetSlotVisualSize(InventorySlotDefinition definition)
    {
        float width = definition.width * ItemGrid.tileSizeWidth;
        float height = definition.height * ItemGrid.tileSizeHeight;

        return new Vector2(width, height);
    }

    private Vector2 GetGridVisualSize()
    {
        Vector2 size = Vector2.zero;

        foreach (SlotState slot in slots)
        {
            Vector2 slotSize = GetSlotVisualSize(slot.Definition);
            size.x = Mathf.Max(size.x, slot.VisualPosition.x + slotSize.x);
            size.y = Mathf.Max(size.y, slot.VisualPosition.y + slotSize.y);
        }

        return size;
    }

    private void BuildVisualAxes()
    {
        rowVisualPositions = new float[gridSizeHeight];
        rowLocalWidths = new float[gridSizeHeight];
        maxRowLocalWidth = 0f;

        for (int y = 0; y < gridSizeHeight; y++)
        {
            rowLocalWidths[y] = CalculateRowLocalWidth(y);
            maxRowLocalWidth = Mathf.Max(maxRowLocalWidth, rowLocalWidths[y]);
        }

        for (int y = 1; y < gridSizeHeight; y++)
        {
            float gap = HasRowBoundary(y - 1) ? slotSpacing : 0f;
            rowVisualPositions[y] = rowVisualPositions[y - 1] + ItemGrid.tileSizeHeight + gap;
        }
    }

    private float GetSlotVisualX(InventorySlotDefinition definition)
    {
        float x = 0f;

        for (int y = definition.y; y < definition.y + definition.height; y++)
        {
            x = Mathf.Max(x, CalculateSlotLocalX(definition.x, y) + GetRowOffset(y));
        }

        return x;
    }

    private float GetRowOffset(int y)
    {
        if (centerRows == false || rowLocalWidths == null) { return 0f; }

        return (maxRowLocalWidth - rowLocalWidths[y]) / 2f;
    }

    private float CalculateRowLocalWidth(int y)
    {
        float width = 0f;
        bool hasVisibleRun = false;
        SlotState previousRun = null;

        for (int x = 0; x < gridSizeWidth; x++)
        {
            SlotState current = GetSlotAtCell(x, y);
            if (current == null)
            {
                previousRun = null;
                continue;
            }

            if (current == previousRun) { continue; }

            if (hasVisibleRun)
            {
                width += slotSpacing;
            }

            width += CountRunWidthInRow(x, y, current) * ItemGrid.tileSizeWidth;
            hasVisibleRun = true;
            previousRun = current;
        }

        return width;
    }

    private float CalculateSlotLocalX(int targetX, int y)
    {
        float xPosition = 0f;
        bool hasVisibleRun = false;
        SlotState previousRun = null;

        for (int x = 0; x < gridSizeWidth; x++)
        {
            SlotState current = GetSlotAtCell(x, y);
            if (current == null)
            {
                previousRun = null;
                continue;
            }

            if (current == previousRun) { continue; }

            if (hasVisibleRun)
            {
                xPosition += slotSpacing;
            }

            if (x == targetX)
            {
                return xPosition;
            }

            xPosition += CountRunWidthInRow(x, y, current) * ItemGrid.tileSizeWidth;
            hasVisibleRun = true;
            previousRun = current;
        }

        return targetX * ItemGrid.tileSizeWidth;
    }

    private int CountRunWidthInRow(int startX, int y, SlotState slot)
    {
        int width = 0;

        for (int x = startX; x < gridSizeWidth; x++)
        {
            if (GetSlotAtCell(x, y) != slot) { break; }

            width++;
        }

        return width;
    }

    private bool HasRowBoundary(int topRow)
    {
        for (int x = 0; x < gridSizeWidth; x++)
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
        SlotState first = GetSlotAtCell(firstX, firstY);
        SlotState second = GetSlotAtCell(secondX, secondY);

        return first != second && (first != null || second != null);
    }
}
