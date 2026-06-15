using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlottedItemGrid : InventoryGrid
{
    [SerializeField] private InventorySlotLayout _slotLayout;
    [SerializeField] private float _slotSpacing = 10f;
    [SerializeField] private Image _slotImageTemplate;
    [SerializeField] private bool _hideSourceImage = true;
    [SerializeField] private bool _centerRows = true;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private InventoryController _inventoryController;

    private readonly List<SlottedGridSlotState> _slots = new();
    private readonly SlottedGridOccupancy _occupancy = new();
    private SlottedGridSlotState[,] _slotByCell;
    private Vector2 _positionOnTheGrid = new();
    private Vector2Int _tileGridPosition = new();
    private Camera _uiCamera;
    private int _gridSizeWidth;
    private int _gridSizeHeight;
    private SlottedGridGeometry _slotGeometry;
    private RectTransform _slotVisualRoot;
    private Image _sourceImage;
    private GameProjectSettings _visualSettings;
    private SlottedGridClosedSlotVisualController _closedSlotVisualController;
    private SlottedGridArtifactSlotController _artifactSlotController;

    public override int HighlightSiblingIndex => _slotVisualRoot == null ? 0 : 1;
    public override RectTransform RectTransform => _rectTransform;

    private SlottedGridArtifactSlotController ArtifactSlotController
    {
        get
        {
            _artifactSlotController ??= new SlottedGridArtifactSlotController(_slots, GetSlotOccupant, ClosedSlotVisualController);
            return _artifactSlotController;
        }
    }

    private SlottedGridClosedSlotVisualController ClosedSlotVisualController
    {
        get
        {
            _closedSlotVisualController ??= new SlottedGridClosedSlotVisualController(GetSlotVisualSize);
            return _closedSlotVisualController;
        }
    }

    private void Awake()
    {
        _visualSettings = GameProjectSettings.LoadDefault();

        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = _canvas.worldCamera;
        }

        _sourceImage = _slotImageTemplate;
        _closedSlotVisualController = new SlottedGridClosedSlotVisualController(GetSlotVisualSize);
        _artifactSlotController = new SlottedGridArtifactSlotController(_slots, GetSlotOccupant, ClosedSlotVisualController);

        InitFromLayout();
    }

    public override InventoryItem PickUpItem(int x, int y)
    {
        if (PositionCheck(x, y) == false)
        {
            return null;
        }

        InventoryItem item = _occupancy.GetItem(x, y);

        if (item == null)
        {
            return null;
        }

        CleanGridReference(item);

        return item;
    }

    public override InventoryItem GetItem(int x, int y)
    {
        if (PositionCheck(x, y) == false)
        {
            return null;
        }

        return _occupancy.GetItem(x, y);
    }

    public override bool TryFindStack(ItemData itemData, out InventoryItem stack) => _occupancy.TryFindStack(itemData, _gridSizeWidth, _gridSizeHeight, CanMergeStackAt, out stack);

    public override Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, mousePosition, _uiCamera, out Vector2 localPoint);

        _positionOnTheGrid.x = localPoint.x + _rectTransform.pivot.x * _rectTransform.rect.width;
        _positionOnTheGrid.y = (1f - _rectTransform.pivot.y) * _rectTransform.rect.height - localPoint.y;
        SlottedGridSlotState hoverSlot = GetSlotAtVisualPosition(_positionOnTheGrid);

        if (selectedItem != null)
        {
            int selectedWidth = GetPlacementWidth(selectedItem, hoverSlot);
            int selectedHeight = GetPlacementHeight(selectedItem, hoverSlot);
            _positionOnTheGrid.x -= (selectedWidth - 1) * ItemGrid.TILE_SIZE_WIDTH / 2f;
            _positionOnTheGrid.y -= (selectedHeight - 1) * ItemGrid.TILE_SIZE_HEIGHT / 2f;
        }

        SlottedGridSlotState slot = GetSlotAtVisualPosition(_positionOnTheGrid);

        if (slot == null)
        {
            return new(-1, -1);
        }

        _tileGridPosition.x = slot.Definition.X + Mathf.FloorToInt((_positionOnTheGrid.x - slot.VisualPosition.x) / ItemGrid.TILE_SIZE_WIDTH);
        _tileGridPosition.y = slot.Definition.Y + Mathf.FloorToInt((_positionOnTheGrid.y - slot.VisualPosition.y) / ItemGrid.TILE_SIZE_HEIGHT);

        return _tileGridPosition;
    }

    public override Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        foreach (SlottedGridSlotState slot in _slots)
        {
            if (slot.IsClosed)
            {
                continue;
            }

            if (slot.Definition.AcceptsItem(itemToInsert) == false)
            {
                continue;
            }

            int placementWidth = GetPlacementWidth(itemToInsert, slot);
            int placementHeight = GetPlacementHeight(itemToInsert, slot);
            int maxX = slot.Definition.X + slot.Definition.Width - placementWidth;
            int maxY = slot.Definition.Y + slot.Definition.Height - placementHeight;

            for (int y = slot.Definition.Y; y <= maxY; y++)
            {
                for (int x = slot.Definition.X; x <= maxX; x++)
                {
                    if (CheckAvailableSpace(itemToInsert, x, y))
                    {
                        return new(x, y);
                    }
                }
            }
        }

        return null;
    }

    public override bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (TryGetPlacementSlot(inventoryItem, posX, posY, out SlottedGridSlotState slot) == false)
        {
            return false;
        }

        int placementWidth = GetPlacementWidth(inventoryItem, slot);
        int placementHeight = GetPlacementHeight(inventoryItem, slot);

        if (BoundryCheck(posX, posY, placementWidth, placementHeight) == false)
        {
            return false;
        }

        if (OverlapCheck(posX, posY, placementWidth, placementHeight, ref overlapItem) == false)
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

        return CheckAvailableSpace(inventoryItem, posX, posY);
    }

    public override void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        if (TryGetPlacementSlot(inventoryItem, posX, posY, out SlottedGridSlotState slot) == false)
        {
            return;
        }

        if (SlottedGridSlotRules.ShouldResetRotation(slot))
        {
            inventoryItem.SetRotated(false);
        }

        int placementWidth = GetPlacementWidth(inventoryItem, slot);
        int placementHeight = GetPlacementHeight(inventoryItem, slot);

        if (BoundryCheck(posX, posY, placementWidth, placementHeight) == false)
        {
            return;
        }

        RectTransform itemRectTransform = inventoryItem.RectTransform;
        itemRectTransform.SetParent(_rectTransform, false);
        itemRectTransform.localScale = Vector3.one;

        _occupancy.PlaceItem(inventoryItem, posX, posY, placementWidth, placementHeight);

        inventoryItem.GridPositionX = posX;
        inventoryItem.GridPositionY = posY;
        itemRectTransform.localPosition = CalculatePositionOnGrid(inventoryItem, posX, posY);
        inventoryItem.SetCellVisualsVisible(true);
        inventoryItem.SetOverlayTextsVisible(SlottedGridSlotRules.ShouldHideOverlayTexts(slot) == false);
    }

    public override Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        SlottedGridSlotState slot = GetSlotAtCell(posX, posY);

        if (slot != null)
        {
            int placementWidth = GetPlacementWidth(inventoryItem, slot);
            int placementHeight = GetPlacementHeight(inventoryItem, slot);

            return new(slot.VisualPosition.x + (posX - slot.Definition.X) * ItemGrid.TILE_SIZE_WIDTH + ItemGrid.TILE_SIZE_WIDTH * placementWidth / 2f, -(slot.VisualPosition.y + (posY - slot.Definition.Y) * ItemGrid.TILE_SIZE_HEIGHT + ItemGrid.TILE_SIZE_HEIGHT * placementHeight / 2f));
        }

        Vector2 position = new()
        {
            x = posX * ItemGrid.TILE_SIZE_WIDTH + ItemGrid.TILE_SIZE_WIDTH * inventoryItem.Width / 2,
            y = -(posY * ItemGrid.TILE_SIZE_HEIGHT + ItemGrid.TILE_SIZE_HEIGHT * inventoryItem.Height / 2)
        };

        return position;
    }

    public override Vector2 GetHighlightSize(InventoryItem inventoryItem, int posX, int posY)
    {
        SlottedGridSlotState slot = GetSlotAtCell(posX, posY);
        return new Vector2(GetPlacementWidth(inventoryItem, slot) * ItemGrid.TILE_SIZE_WIDTH, GetPlacementHeight(inventoryItem, slot) * ItemGrid.TILE_SIZE_HEIGHT);
    }

    public override Vector2 GetHighlightPosition(InventoryItem inventoryItem, int posX, int posY) => CalculatePositionOnGrid(inventoryItem, posX, posY);
    public override bool CanMergeStackAt(int posX, int posY) => SlottedGridSlotRules.IsArtifactSlot(GetSlotAtCell(posX, posY)) == false;

    public bool HasArtifactSlots => ArtifactSlotController.HasArtifactSlots;
    public int ArtifactSlotCount => ArtifactSlotController.ArtifactSlotCount;

    public bool CanSetOpenArtifactSlotCount(int openSlotBudget) => ArtifactSlotController.CanSetOpenArtifactSlotCount(openSlotBudget);
    public int SetOpenArtifactSlotCount(int openSlotBudget, GameObject closedSlotPrefab) => ArtifactSlotController.SetOpenArtifactSlotCount(openSlotBudget, closedSlotPrefab);

    public override bool BoundryCheck(int posX, int posY, int width, int height)
    {
        SlottedGridSlotState slot = GetSlotAtCell(posX, posY);

        if (slot == null)
        {
            return false;
        }

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
        _slots.Clear();

        if (SlottedGridLayoutBuilder.TryBuild(_slotLayout, _slotSpacing, _centerRows, out SlottedGridLayoutBuildResult layout, out string error) == false)
        {
            Debug.LogError(error, _slotLayout == null ? this : _slotLayout);
            return;
        }

        _gridSizeWidth = layout.GridSizeWidth;
        _gridSizeHeight = layout.GridSizeHeight;
        _slotByCell = layout.SlotByCell;
        _slotGeometry = layout.Geometry;
        _slots.AddRange(layout.Slots);
        _occupancy.Initialize(_gridSizeWidth, _gridSizeHeight);

        _rectTransform.sizeDelta = _slotGeometry.GetGridVisualSize(_slots);
        CreateSlotVisuals();
    }

    private void CleanGridReference(InventoryItem item)
    {
        SlottedGridSlotState slot = GetSlotAtCell(item.GridPositionX, item.GridPositionY);
        int placementWidth = GetPlacementWidth(item, slot);
        int placementHeight = GetPlacementHeight(item, slot);

        _occupancy.ClearItem(item, placementWidth, placementHeight);
    }

    private bool CheckAvailableSpace(InventoryItem inventoryItem, int posX, int posY)
    {
        if (TryGetPlacementSlot(inventoryItem, posX, posY, out SlottedGridSlotState slot) == false)
        {
            return false;
        }

        int placementWidth = GetPlacementWidth(inventoryItem, slot);
        int placementHeight = GetPlacementHeight(inventoryItem, slot);

        if (BoundryCheck(posX, posY, placementWidth, placementHeight) == false)
        {
            return false;
        }

        return _occupancy.IsAreaEmpty(posX, posY, placementWidth, placementHeight);
    }

    public bool ShouldSplitStackOnPlace(InventoryItem inventoryItem, int posX, int posY) => inventoryItem != null && inventoryItem.IsStackable && inventoryItem.CurrentAmount > 1 && TryGetPlacementSlot(inventoryItem, posX, posY, out SlottedGridSlotState slot) && SlottedGridSlotRules.IsArtifactSlot(slot);

    private bool TryGetPlacementSlot(InventoryItem inventoryItem, int posX, int posY, out SlottedGridSlotState slot)
    {
        slot = GetSlotAtCell(posX, posY);
        return slot != null && slot.IsClosed == false && slot.Definition.AcceptsItem(inventoryItem);
    }

    private int GetPlacementWidth(InventoryItem inventoryItem, SlottedGridSlotState slot)
    {
        if (inventoryItem == null)
        {
            return 0;
        }

        return SlottedGridSlotRules.ShouldResetRotation(slot) ? inventoryItem.BaseWidth : inventoryItem.Width;
    }

    private int GetPlacementHeight(InventoryItem inventoryItem, SlottedGridSlotState slot)
    {
        if (inventoryItem == null)
        {
            return 0;
        }

        return SlottedGridSlotRules.ShouldResetRotation(slot) ? inventoryItem.BaseHeight : inventoryItem.Height;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem) => _occupancy.TryGetSingleOverlap(posX, posY, width, height, ref overlapItem);

    private SlottedGridSlotState GetSlotAtCell(int x, int y)
    {
        if (_slotByCell == null)
        {
            return null;
        }

        if (PositionCheck(x, y) == false)
        {
            return null;
        }

        return _slotByCell[x, y];
    }

    private bool PositionCheck(int x, int y) => x >= 0 && y >= 0 && x < _gridSizeWidth && y < _gridSizeHeight;

    private SlottedGridSlotState GetSlotAtVisualPosition(Vector2 visualPosition) => _slotGeometry?.GetSlotAtVisualPosition(_slots, visualPosition);

    private void CreateSlotVisuals()
    {
        SlottedItemGridVisualBuilder visualBuilder = new(_inventoryController, this, _rectTransform, _sourceImage, _hideSourceImage, _visualSettings, GetSlotVisualSize);

        _slotVisualRoot = visualBuilder.CreateSlotVisuals(_slots);
    }

    private InventoryItem GetSlotOccupant(SlottedGridSlotState slot)
    {
        if (slot == null || _occupancy.IsInitialized == false)
        {
            return null;
        }

        return _occupancy.GetFirstOccupant(slot.Definition);
    }

    private Vector2 GetSlotVisualSize(InventorySlotDefinition definition) => _slotGeometry == null ? Vector2.zero : _slotGeometry.GetSlotVisualSize(definition);
}