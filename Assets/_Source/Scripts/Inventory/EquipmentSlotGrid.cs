using UnityEngine;

public class EquipmentSlotGrid : InventoryGrid
{
    [SerializeField] private bool restrictItemType = true;
    [SerializeField] private ItemType acceptedItemType = ItemType.Misc;
    [SerializeField] private bool useRectTransformSize = true;
    [SerializeField] [Min(1)] private int gridSizeWidth = 1;
    [SerializeField] [Min(1)] private int gridSizeHeight = 1;

    private Vector2 positionOnTheGrid = new Vector2();
    private Vector2Int tileGridPosition = new Vector2Int();
    private RectTransform rectTransform;
    private Camera uiCamera;
    private InventoryItem equippedItem;
    private GameObject closedSlotInstance;

    public InventoryItem EquippedItem => equippedItem;
    public ItemType AcceptedItemType => acceptedItemType;
    public bool IsClosed { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        InitGridSize();
    }

    public override InventoryItem PickUpItem(int x, int y)
    {
        if (PositionCheck(x, y) == false) { return null; }

        InventoryItem item = equippedItem;
        if (item == null) { return null; }

        equippedItem = null;
        item.RestoreDefaultVisual();
        return item;
    }

    public override InventoryItem GetItem(int x, int y)
    {
        if (PositionCheck(x, y) == false) { return null; }

        return equippedItem;
    }

    public override Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, uiCamera, out Vector2 localPoint);

        positionOnTheGrid.x = localPoint.x + rectTransform.pivot.x * rectTransform.rect.width;
        positionOnTheGrid.y = (1f - rectTransform.pivot.y) * rectTransform.rect.height - localPoint.y;

        tileGridPosition.x = Mathf.FloorToInt(positionOnTheGrid.x / ItemGrid.tileSizeWidth);
        tileGridPosition.y = Mathf.FloorToInt(positionOnTheGrid.y / ItemGrid.tileSizeHeight);

        return tileGridPosition;
    }

    public override Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        if (IsClosed || equippedItem != null || CanAcceptItem(itemToInsert) == false)
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

        return equippedItem == null && PositionCheck(posX, posY);
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
        if (CanPlaceItem(inventoryItem, posX, posY) == false) { return; }

        RectTransform itemRectTransform = inventoryItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(rectTransform, false);
        itemRectTransform.localScale = Vector3.one;
        itemRectTransform.SetAsLastSibling();

        equippedItem = inventoryItem;
        inventoryItem.onGridPositionX = 0;
        inventoryItem.onGridPositionY = 0;
        ApplyEquippedVisual(inventoryItem);
        itemRectTransform.localPosition = CalculatePositionOnGrid(inventoryItem, 0, 0);
        inventoryItem.SetCellVisualsVisible(true);
        inventoryItem.SetOverlayTextsVisible(true);
    }

    public override Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        return new Vector2(
            gridSizeWidth * ItemGrid.tileSizeWidth / 2f,
            -gridSizeHeight * ItemGrid.tileSizeHeight / 2f);
    }

    public override bool BoundryCheck(int posX, int posY, int width, int height)
    {
        return PositionCheck(posX, posY);
    }

    public override Vector2 GetHighlightSize(InventoryItem inventoryItem, int posX, int posY)
    {
        return new Vector2(
            gridSizeWidth * ItemGrid.tileSizeWidth,
            gridSizeHeight * ItemGrid.tileSizeHeight);
    }

    public override Vector2 GetHighlightPosition(InventoryItem inventoryItem, int posX, int posY)
    {
        return CalculatePositionOnGrid(inventoryItem, 0, 0);
    }

    public override bool CanMergeStackAt(int posX, int posY)
    {
        return false;
    }

    public bool CanSetClosed(bool closed)
    {
        return closed == false || equippedItem == null;
    }

    public void SetClosed(bool closed, GameObject closedSlotPrefab)
    {
        if (closed && equippedItem != null)
        {
            closed = false;
        }

        IsClosed = closed;

        if (IsClosed)
        {
            EnsureClosedSlotVisual(closedSlotPrefab);
        }
        else
        {
            DestroyClosedSlotVisual();
        }
    }

    private void InitGridSize()
    {
        if (useRectTransformSize == false || rectTransform == null)
        {
            return;
        }

        int width = Mathf.RoundToInt(rectTransform.rect.width / ItemGrid.tileSizeWidth);
        int height = Mathf.RoundToInt(rectTransform.rect.height / ItemGrid.tileSizeHeight);

        gridSizeWidth = Mathf.Max(1, width);
        gridSizeHeight = Mathf.Max(1, height);
    }

    private bool CanAcceptItem(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (restrictItemType == false)
        {
            return true;
        }

        return item.itemData != null && item.itemData.ItemType == acceptedItemType;
    }

    private void EnsureClosedSlotVisual(GameObject closedSlotPrefab)
    {
        if (closedSlotPrefab == null || rectTransform == null)
        {
            return;
        }

        if (closedSlotInstance == null)
        {
            closedSlotInstance = Instantiate(closedSlotPrefab, rectTransform, false);
            closedSlotInstance.name = closedSlotPrefab.name;
        }

        RectTransform closedRectTransform = closedSlotInstance.GetComponent<RectTransform>();
        if (closedRectTransform != null)
        {
            closedRectTransform.anchorMin = new Vector2(0f, 1f);
            closedRectTransform.anchorMax = new Vector2(0f, 1f);
            closedRectTransform.pivot = new Vector2(0f, 1f);
            closedRectTransform.anchoredPosition = Vector2.zero;
            closedRectTransform.sizeDelta = new Vector2(
                gridSizeWidth * ItemGrid.tileSizeWidth,
                gridSizeHeight * ItemGrid.tileSizeHeight);
            closedRectTransform.localRotation = Quaternion.identity;
            closedRectTransform.localScale = Vector3.one;
        }

        closedSlotInstance.transform.SetAsFirstSibling();
        closedSlotInstance.SetActive(true);
    }

    private void DestroyClosedSlotVisual()
    {
        if (closedSlotInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(closedSlotInstance);
        }
        else
        {
            DestroyImmediate(closedSlotInstance);
        }

        closedSlotInstance = null;
    }

    private void ApplyEquippedVisual(InventoryItem item)
    {
        bool useGeneratedSlotIcon = item.BaseWidth != gridSizeWidth || item.BaseHeight != gridSizeHeight;
        item.ApplySlotVisual(gridSizeWidth, gridSizeHeight, useGeneratedSlotIcon);
    }

    private bool PositionCheck(int x, int y)
    {
        return x >= 0 && y >= 0 && x < gridSizeWidth && y < gridSizeHeight;
    }
}
