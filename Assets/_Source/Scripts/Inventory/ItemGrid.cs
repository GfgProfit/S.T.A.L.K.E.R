using System;
using UnityEngine;

public class ItemGrid : InventoryGrid
{
    [SerializeField] [Range(1, 20)] private int gridSizeWidth = 7;
    [SerializeField] [Range(1, 20)] private int gridSizeHeight = 9;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Canvas canvas;

    public const float tileSizeWidth = 64;
    public const float tileSizeHeight = 64;

    private Vector2 positionOnTheGrid = new Vector2();
    private Vector2Int tileGridPosition = new Vector2Int();
    private Camera uiCamera;
    private RectTransform cellGridRoot;
    private GameProjectSettings visualSettings;

    private InventoryItem[,] inventoryItemSlot;

    public override int HighlightSiblingIndex => cellGridRoot == null ? 0 : 1;
    public override RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        visualSettings = GameProjectSettings.LoadDefault();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        Init(gridSizeWidth, gridSizeHeight);
    }

    public override InventoryItem PickUpItem(int x, int y)
    {
        if (PositionCheck(x, y) == false) { return null; }

        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) { return null; }

        CleanGridReference(toReturn);

        return toReturn;
    }

    private void CleanGridReference(InventoryItem item)
    {
        for (int ix = 0; ix < item.Width; ix++)
        {
            for (int iy = 0; iy < item.Height; iy++)
            {
                inventoryItemSlot[item.onGridPositionX + ix, item.onGridPositionY + iy] = null;
            }
        }
    }

    private void Init(int width, int height)
    {
        inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;

        CreateCellGridVisual(width, height, size);
    }

    public override InventoryItem GetItem(int x, int y)
    {
        if (PositionCheck(x, y) == false) { return null; }

        return inventoryItemSlot[x, y];
    }

    public override bool TryFindStack(ItemData itemData, out InventoryItem stack)
    {
        stack = null;

        if (itemData == null || itemData.IsStackable == false || inventoryItemSlot == null)
        {
            return false;
        }

        for (int x = 0; x < gridSizeWidth; x++)
        {
            for (int y = 0; y < gridSizeHeight; y++)
            {
                InventoryItem item = inventoryItemSlot[x, y];
                if (item != null && item.CanStackWith(itemData))
                {
                    stack = item;
                    return true;
                }
            }
        }

        return false;
    }

    public override Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, uiCamera, out Vector2 localPoint);

        positionOnTheGrid.x = localPoint.x + rectTransform.pivot.x * rectTransform.rect.width;
        positionOnTheGrid.y = (1f - rectTransform.pivot.y) * rectTransform.rect.height - localPoint.y;

        if (selectedItem != null)
        {
            positionOnTheGrid.x -= (selectedItem.Width - 1) * tileSizeWidth / 2f;
            positionOnTheGrid.y -= (selectedItem.Height - 1) * tileSizeHeight / 2f;
        }

        tileGridPosition.x = Mathf.FloorToInt(positionOnTheGrid.x / tileSizeWidth);
        tileGridPosition.y = Mathf.FloorToInt(positionOnTheGrid.y / tileSizeHeight);

        return tileGridPosition;
    }

    public override Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        int height = gridSizeHeight - itemToInsert.Height + 1;
        int width = gridSizeWidth - itemToInsert.Width + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (CheckAvailableSpace(x, y, itemToInsert.Width, itemToInsert.Height) == true)
                {
                    return new Vector2Int(x, y);
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

        return BoundryCheck(posX, posY, inventoryItem.Width, inventoryItem.Height) &&
               CheckAvailableSpace(posX, posY, inventoryItem.Width, inventoryItem.Height);
    }

    public override void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform itemRectTransform = inventoryItem.RectTransform;
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

        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);

        itemRectTransform.localPosition = position;
        inventoryItem.SetCellVisualsVisible(true);
        inventoryItem.SetOverlayTextsVisible(true);
    }

    public override Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();

        position.x = posX * tileSizeWidth + tileSizeWidth * inventoryItem.Width / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.Height / 2);

        return position;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    if (overlapItem == null)
                    {
                        overlapItem = inventoryItemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        if (overlapItem != inventoryItemSlot[posX + x, posY + y])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
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

    private bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0)
        {
            return false;
        }

        if (posX >= gridSizeWidth || posY >= gridSizeHeight)
        {
            return false;
        }

        return true;
    }

    public override bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) { return false; }

        posX += width - 1;
        posY += height - 1;

        if (PositionCheck(posX, posY) == false) { return false; }

        return true;
    }

    private void CreateCellGridVisual(int width, int height, Vector2 size)
    {
        if (visualSettings == null)
        {
            visualSettings = GameProjectSettings.LoadDefault();
        }

        if (visualSettings.ShowCellGrid == false) { return; }

        GameObject rootObject = new GameObject("Cell Grid");
        rootObject.transform.SetParent(rectTransform, false);

        cellGridRoot = rootObject.AddComponent<RectTransform>();
        cellGridRoot.anchorMin = new Vector2(0f, 1f);
        cellGridRoot.anchorMax = new Vector2(0f, 1f);
        cellGridRoot.pivot = new Vector2(0f, 1f);
        cellGridRoot.anchoredPosition = Vector2.zero;
        cellGridRoot.sizeDelta = size;
        cellGridRoot.SetAsFirstSibling();

        int firstVerticalLine = visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastVerticalLine = visualSettings.ShowCellGridBorder ? width : width - 1;
        int firstHorizontalLine = visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastHorizontalLine = visualSettings.ShowCellGridBorder ? height : height - 1;

        for (int x = firstVerticalLine; x <= lastVerticalLine; x++)
        {
            bool isBorderLine = x == 0 || x == width;

            CreateGridLine(
                cellGridRoot,
                $"Vertical Grid Line {x}",
                new Vector2(x * tileSizeWidth, 0f),
                new Vector2(visualSettings.GetLineThickness(isBorderLine), size.y),
                new Vector2(0.5f, 1f),
                visualSettings.GetLineColor(isBorderLine));
        }

        for (int y = firstHorizontalLine; y <= lastHorizontalLine; y++)
        {
            bool isBorderLine = y == 0 || y == height;

            CreateGridLine(
                cellGridRoot,
                $"Horizontal Grid Line {y}",
                new Vector2(0f, -y * tileSizeHeight),
                new Vector2(size.x, visualSettings.GetLineThickness(isBorderLine)),
                new Vector2(0f, 0.5f),
                visualSettings.GetLineColor(isBorderLine));
        }
    }

    private void CreateGridLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 pivot, Color color)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform lineRectTransform = lineObject.AddComponent<RectTransform>();
        lineObject.AddComponent<CanvasRenderer>();
        lineRectTransform.anchorMin = new Vector2(0f, 1f);
        lineRectTransform.anchorMax = new Vector2(0f, 1f);
        lineRectTransform.pivot = pivot;
        lineRectTransform.anchoredPosition = anchoredPosition;
        lineRectTransform.sizeDelta = size;

        UnityEngine.UI.Image lineImage = lineObject.AddComponent<UnityEngine.UI.Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }
}
