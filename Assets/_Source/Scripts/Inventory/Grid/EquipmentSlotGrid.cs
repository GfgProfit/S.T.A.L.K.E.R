using UnityEngine;

public class EquipmentSlotGrid : InventoryGrid
{
    [SerializeField] private bool _restrictItemType = true;
    [SerializeField] private ItemType _acceptedItemType = ItemType.Misc;
    [SerializeField] private bool _useRectTransformSize = true;
    [SerializeField] [Min(1)] private int _gridSizeWidth = 1;
    [SerializeField] [Min(1)] private int _gridSizeHeight = 1;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Canvas _canvas;

    private Vector2 _positionOnTheGrid = new();
    private Vector2Int _tileGridPosition = new();
    private Camera _uiCamera;
    private InventoryItem _equippedItem;
    private EquipmentSlotClosedVisualController _closedSlotVisualController;

    public InventoryItem EquippedItem => _equippedItem;
    public ItemType AcceptedItemType => _acceptedItemType;
    internal bool RestrictsItemType => _restrictItemType;
    internal int GridWidth => Mathf.Max(1, _gridSizeWidth);
    internal int GridHeight => Mathf.Max(1, _gridSizeHeight);
    public bool IsClosed { get; private set; }
    public override RectTransform RectTransform => _rectTransform;

    private void Awake()
    {
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = _canvas.worldCamera;
        }

        InitGridSize();
        _closedSlotVisualController = new EquipmentSlotClosedVisualController(_rectTransform, GetSlotVisualSize);
    }

    public override InventoryItem PickUpItem(int x, int y)
    {
        if (PositionCheck(x, y) == false)
        {
            return null;
        }

        InventoryItem item = _equippedItem;

        if (item == null)
        {
            return null;
        }

        _equippedItem = null;
        item.RestoreDefaultVisual();
        return item;
    }

    public override InventoryItem GetItem(int x, int y)
    {
        if (PositionCheck(x, y) == false)
        { 
           return null;
        }

        return _equippedItem;
    }

    public override Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, mousePosition, _uiCamera, out Vector2 localPoint);

        _positionOnTheGrid.x = localPoint.x + _rectTransform.pivot.x * _rectTransform.rect.width;
        _positionOnTheGrid.y = (1f - _rectTransform.pivot.y) * _rectTransform.rect.height - localPoint.y;

        _tileGridPosition.x = Mathf.FloorToInt(_positionOnTheGrid.x / ItemGrid.TILE_SIZE_WIDTH);
        _tileGridPosition.y = Mathf.FloorToInt(_positionOnTheGrid.y / ItemGrid.TILE_SIZE_HEIGHT);

        return _tileGridPosition;
    }

    public override Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        if (IsClosed || _equippedItem != null || CanAcceptItem(itemToInsert) == false)
        {
            return null;
        }

        return Vector2Int.zero;
    }

    public override bool CanPlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        if (IsClosed)
        {
            return false;
        }

        if (CanAcceptItem(inventoryItem) == false)
        {
            return false;
        }

        return _equippedItem == null && PositionCheck(posX, posY);
    }

    public override bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (CanPlaceItem(inventoryItem, posX, posY) == false)
        {
            return false;
        }

        overlapItem = null;
        PlaceItem(inventoryItem, posX, posY);
        return true;
    }

    public override void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        if (CanPlaceItem(inventoryItem, posX, posY) == false)
        {
            return;
        }

        RectTransform itemRectTransform = inventoryItem.RectTransform;
        itemRectTransform.SetParent(_rectTransform, false);
        itemRectTransform.localScale = Vector3.one;
        itemRectTransform.SetAsLastSibling();

        _equippedItem = inventoryItem;
        inventoryItem.GridPositionX = 0;
        inventoryItem.GridPositionY = 0;
        ApplyEquippedVisual(inventoryItem);
        itemRectTransform.localPosition = CalculatePositionOnGrid(inventoryItem, 0, 0);
        inventoryItem.SetCellVisualsVisible(true);
        inventoryItem.SetOverlayTextsVisible(true);
    }

    public override Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY) => new(_gridSizeWidth * ItemGrid.TILE_SIZE_WIDTH / 2f, -_gridSizeHeight * ItemGrid.TILE_SIZE_HEIGHT / 2f);
    public override bool BoundryCheck(int posX, int posY, int width, int height) => PositionCheck(posX, posY);
    public override Vector2 GetHighlightSize(InventoryItem inventoryItem, int posX, int posY) => new(_gridSizeWidth * ItemGrid.TILE_SIZE_WIDTH, _gridSizeHeight * ItemGrid.TILE_SIZE_HEIGHT);
    public override Vector2 GetHighlightPosition(InventoryItem inventoryItem, int posX, int posY) => CalculatePositionOnGrid(inventoryItem, 0, 0);
    public override bool CanMergeStackAt(int posX, int posY) => false;
    public bool CanSetClosed(bool closed) => closed == false || _equippedItem == null;

    public void SetClosed(bool closed, GameObject closedSlotPrefab)
    {
        if (closed && _equippedItem != null)
        {
            closed = false;
        }

        IsClosed = closed;

        if (IsClosed)
        {
            _closedSlotVisualController.Show(closedSlotPrefab);
        }
        else
        {
            _closedSlotVisualController.Hide();
        }
    }

    private void InitGridSize()
    {
        if (_useRectTransformSize == false || _rectTransform == null)
        {
            return;
        }

        int width = Mathf.RoundToInt(_rectTransform.rect.width / ItemGrid.TILE_SIZE_WIDTH);
        int height = Mathf.RoundToInt(_rectTransform.rect.height / ItemGrid.TILE_SIZE_HEIGHT);

        _gridSizeWidth = Mathf.Max(1, width);
        _gridSizeHeight = Mathf.Max(1, height);
    }

    private bool CanAcceptItem(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (_restrictItemType == false)
        {
            return true;
        }

        return item.ItemData != null && item.ItemData.ItemType == _acceptedItemType;
    }

    private void ApplyEquippedVisual(InventoryItem item)
    {
        bool useGeneratedSlotIcon = item.BaseWidth != _gridSizeWidth || item.BaseHeight != _gridSizeHeight;
        item.ApplySlotVisual(_gridSizeWidth, _gridSizeHeight, useGeneratedSlotIcon);
    }

    private bool PositionCheck(int x, int y) => x >= 0 && y >= 0 && x < _gridSizeWidth && y < _gridSizeHeight;
    private Vector2 GetSlotVisualSize() => new(_gridSizeWidth * ItemGrid.TILE_SIZE_WIDTH, _gridSizeHeight * ItemGrid.TILE_SIZE_HEIGHT);
}
