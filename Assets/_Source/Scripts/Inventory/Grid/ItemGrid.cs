using UnityEngine;

public class ItemGrid : InventoryGrid
{
    [SerializeField] [Range(1, 20)] private int _gridSizeWidth = 7;
    [SerializeField] [Range(1, 20)] private int _gridSizeHeight = 9;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Canvas _canvas;

    public const float TILE_SIZE_WIDTH = 64;
    public const float TILE_SIZE_HEIGHT = 64;

    private Vector2 _positionOnTheGrid = new();
    private Vector2Int _tileGridPosition = new();
    private readonly ItemGridOccupancy _occupancy = new();
    private Camera _uiCamera;
    private RectTransform _cellGridRoot;

    public override int HighlightSiblingIndex => _cellGridRoot == null ? 0 : 1;
    public override RectTransform RectTransform => _rectTransform;

    private void Awake()
    {
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = _canvas.worldCamera;
        }

        Init(_gridSizeWidth, _gridSizeHeight);
    }

    public override InventoryItem PickUpItem(int x, int y)
    {
        if (PositionCheck(x, y) == false)
        {
            return null;
        }

        InventoryItem toReturn = _occupancy.GetItem(x, y);

        if (toReturn == null)
        {
            return null;
        }

        _occupancy.ClearItem(toReturn);

        return toReturn;
    }

    private void Init(int width, int height)
    {
        _occupancy.Initialize(width, height);
        Vector2 size = new(width * TILE_SIZE_WIDTH, height * TILE_SIZE_HEIGHT);
        _rectTransform.sizeDelta = size;

        _cellGridRoot = ItemGridVisualBuilder.CreateCellGrid(_rectTransform, width, height, size, GameProjectSettings.LoadDefault());
    }

    public override InventoryItem GetItem(int x, int y)
    {
        if (PositionCheck(x, y) == false)
        {
            return null;
        }

        return _occupancy.GetItem(x, y);
    }

    public override bool TryFindStack(ItemData itemData, out InventoryItem stack) => _occupancy.TryFindStack(itemData, _gridSizeWidth, _gridSizeHeight, out stack);

    public override Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, mousePosition, _uiCamera, out Vector2 localPoint);

        _positionOnTheGrid.x = localPoint.x + _rectTransform.pivot.x * _rectTransform.rect.width;
        _positionOnTheGrid.y = (1f - _rectTransform.pivot.y) * _rectTransform.rect.height - localPoint.y;

        if (selectedItem != null)
        {
            _positionOnTheGrid.x -= (selectedItem.Width - 1) * TILE_SIZE_WIDTH / 2f;
            _positionOnTheGrid.y -= (selectedItem.Height - 1) * TILE_SIZE_HEIGHT / 2f;
        }

        _tileGridPosition.x = Mathf.FloorToInt(_positionOnTheGrid.x / TILE_SIZE_WIDTH);
        _tileGridPosition.y = Mathf.FloorToInt(_positionOnTheGrid.y / TILE_SIZE_HEIGHT);

        return _tileGridPosition;
    }

    public override Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        int height = _gridSizeHeight - itemToInsert.Height + 1;
        int width = _gridSizeWidth - itemToInsert.Width + 1;

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
            _occupancy.ClearItem(overlapItem);
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

        return BoundryCheck(posX, posY, inventoryItem.Width, inventoryItem.Height) && CheckAvailableSpace(posX, posY, inventoryItem.Width, inventoryItem.Height);
    }

    public override void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform itemRectTransform = inventoryItem.RectTransform;
        itemRectTransform.SetParent(_rectTransform, false);
        itemRectTransform.localScale = Vector3.one;

        _occupancy.PlaceItem(inventoryItem, posX, posY, inventoryItem.Width, inventoryItem.Height);

        inventoryItem.GridPositionX = posX;
        inventoryItem.GridPositionY = posY;

        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);

        itemRectTransform.localPosition = position;
        inventoryItem.SetCellVisualsVisible(true);
        inventoryItem.SetOverlayTextsVisible(true);
    }

    public override Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new()
        {
            x = posX * TILE_SIZE_WIDTH + TILE_SIZE_WIDTH * inventoryItem.Width / 2,
            y = -(posY * TILE_SIZE_HEIGHT + TILE_SIZE_HEIGHT * inventoryItem.Height / 2)
        };

        return position;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem) => _occupancy.TryGetSingleOverlap(posX, posY, width, height, ref overlapItem);
    private bool CheckAvailableSpace(int posX, int posY, int width, int height) => _occupancy.IsAreaEmpty(posX, posY, width, height);

    private bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0)
        {
            return false;
        }

        if (posX >= _gridSizeWidth || posY >= _gridSizeHeight)
        {
            return false;
        }

        return true;
    }

    public override bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false)
        {
            return false;
        }

        posX += width - 1;
        posY += height - 1;

        if (PositionCheck(posX, posY) == false)
        {
            return false;
        }

        return true;
    }
}