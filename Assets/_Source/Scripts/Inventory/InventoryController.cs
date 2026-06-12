using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

public class InventoryController : MonoBehaviour
{
    private InventoryGrid selectedItemGrid;

    public InventoryGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);
        }
    }

    [SerializeField] private List<ItemData> items;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private InventoryGrid defaultItemGrid;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private ItemInfoPanel itemInfoPanel;
    [SerializeField] [Min(0f)] private float maxCarryWeight = 50f;
    [SerializeField] [Min(0f)] private float movementBlockExtraWeight = 10f;
    [SerializeField] private Color normalWeightColor = Color.white;
    [SerializeField] private Color overweightColor = new Color(1f, 0.55f, 0f, 1f);
    [SerializeField] private Color movementBlockedWeightColor = Color.red;
    [SerializeField] private bool openOnStart;
    [SerializeField] private bool unlockCursorWhileOpen = true;
    [SerializeField] private bool disablePlayerControlsWhileOpen = true;
    [SerializeField] private bool prewarmItemIconsOnStart = true;
    [SerializeField] private bool logIconPrewarmProgress;

    [Inject] private IPlayerInput playerInput = null;

    private InventoryItem selectedItem;
    private RectTransform rectTransform;
    private InventoryItem itemToHighlight;
    private InventoryHighlight inventoryHighlight;
    private InventoryGrid dragOriginGrid;
    private Vector2Int dragOriginPosition;
    private bool dragOriginRotated;
    private bool iconsReady;
    private IPlayerInput fallbackPlayerInput;
    private readonly List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private float currentCarryWeight;

    public bool IsOpen { get; private set; }
    public float CurrentCarryWeight => currentCarryWeight;
    public float MaxCarryWeight => Mathf.Max(0f, maxCarryWeight);
    public float MovementBlockWeight => MaxCarryWeight + Mathf.Max(0f, movementBlockExtraWeight);
    public bool IsMovementBlockedByWeight => currentCarryWeight >= MovementBlockWeight;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (playerInput != null)
            {
                return playerInput;
            }

            fallbackPlayerInput ??= new LegacyPlayerInput();
            return fallbackPlayerInput;
        }
    }

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
        iconsReady = prewarmItemIconsOnStart == false;

        if (inventoryRoot == null && inventoryCanvasGroup != null)
        {
            inventoryRoot = inventoryCanvasGroup.gameObject;
        }

        if (inventoryCanvasGroup == null && inventoryRoot != null)
        {
            inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();
        }

        if (defaultItemGrid == null)
        {
            defaultItemGrid = FindFirstObjectByType<InventoryGrid>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        RegisterExistingInventoryItems();
        SetInventoryOpen(openOnStart, true);
        RefreshWeightState();
    }

    private IEnumerator Start()
    {
        if (prewarmItemIconsOnStart == false)
        {
            yield break;
        }

        yield return ItemIconCache.PrewarmCoroutine(items, HandleIconPrewarmProgress);
        iconsReady = true;
    }

    private void Update()
    {
        if (PlayerInput.IsInventoryPressed())
        {
            ToggleInventory();
        }

        if (IsOpen == false)
        {
            inventoryHighlight.Show(false);
            HideItemInfoPanel();
            return;
        }

        if (iconsReady == false)
        {
            inventoryHighlight.Show(false);
            HideItemInfoPanel();
            return;
        }

        ItemIconDrag();

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            ReleaseDraggedItem();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateSelectedItem();
        }

        if (selectedItemGrid == null) 
        {
            inventoryHighlight.Show(false);
            HideItemInfoPanel();

            return; 
        }

        HandleHighlight();

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            BeginDrag();
        }
    }

    public bool TryInsertItem(ItemData itemData)
    {
        return TryInsertItem(itemData, 1);
    }

    public bool TryInsertItem(ItemData itemData, int amount)
    {
        if (iconsReady == false) { return false; }
        if (itemData == null) { return false; }

        InventoryGrid insertionGrid = GetInsertionGrid();
        if (insertionGrid == null) { return false; }

        int normalizedAmount = NormalizeItemAmount(itemData, amount);
        if (TryAddToExistingStack(itemData, normalizedAmount, insertionGrid))
        {
            RefreshWeightState();
            return true;
        }

        InventoryItem itemToInsert = CreateItem(itemData, normalizedAmount);

        if (InsertItem(itemToInsert, insertionGrid) ||
            (insertionGrid != defaultItemGrid && InsertItem(itemToInsert, defaultItemGrid)))
        {
            RegisterInventoryItem(itemToInsert);
            RefreshWeightState();
            return true;
        }

        Destroy(itemToInsert.gameObject);
        return false;
    }

    private bool InsertItem(InventoryItem itemToInsert, InventoryGrid targetGrid)
    {
        if (targetGrid == null) { return false; }

        if (TryPlaceItemInFirstAvailableSpace(itemToInsert, targetGrid))
        {
            return true;
        }

        if (itemToInsert.CanRotate == false)
        {
            return false;
        }

        itemToInsert.Rotate();

        if (TryPlaceItemInFirstAvailableSpace(itemToInsert, targetGrid))
        {
            return true;
        }

        itemToInsert.Rotate();
        return false;
    }

    private bool TryPlaceItemInFirstAvailableSpace(InventoryItem itemToInsert, InventoryGrid targetGrid)
    {
        Vector2Int? posOnGrid = targetGrid.FindSpaceForObject(itemToInsert);

        if (posOnGrid != null)
        {
            targetGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
            return true;
        }

        return false;
    }

    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();

        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);

            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(selectedItemGrid, itemToHighlight);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);
                ShowItemInfoPanel(itemToHighlight);
            }
            else
            {
                inventoryHighlight.Show(false);
                HideItemInfoPanel();
            }
        }
        else
        {
            HideItemInfoPanel();
            bool canMergeStack = TryGetStackMergeTarget(selectedItemGrid, positionOnGrid, out InventoryItem stackMergeTarget);
            bool canPlaceItem = selectedItemGrid.CanPlaceItem(selectedItem, positionOnGrid.x, positionOnGrid.y);

            inventoryHighlight.Show(canPlaceItem || canMergeStack);
            if (canMergeStack)
            {
                inventoryHighlight.SetSize(selectedItemGrid, stackMergeTarget);
                inventoryHighlight.SetPosition(selectedItemGrid, stackMergeTarget);
            }
            else
            {
                inventoryHighlight.SetSize(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
                inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
            }
        }
    }

    private InventoryItem CreateItem(ItemData itemData, int amount)
    {
        return CreateItem(itemData, amount, null);
    }

    private InventoryItem CreateItem(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
        inventoryItem.Set(itemData, amount, runtimeIconParts);

        return inventoryItem;
    }

    private bool TryAddToExistingStack(ItemData itemData, int amount, InventoryGrid preferredGrid)
    {
        if (itemData == null || itemData.IsStackable == false)
        {
            return false;
        }

        if (TryAddToExistingStackInGrid(preferredGrid, itemData, amount))
        {
            return true;
        }

        return preferredGrid != defaultItemGrid &&
               TryAddToExistingStackInGrid(defaultItemGrid, itemData, amount);
    }

    private bool TryAddToExistingStackInGrid(InventoryGrid grid, ItemData itemData, int amount)
    {
        if (grid == null)
        {
            return false;
        }

        if (grid.TryFindStack(itemData, out InventoryItem stack) == false)
        {
            return false;
        }

        stack.AddAmount(amount);
        return true;
    }

    private void RegisterInventoryItem(InventoryItem item)
    {
        if (item == null || inventoryItems.Contains(item))
        {
            return;
        }

        inventoryItems.Add(item);
    }

    private void RegisterExistingInventoryItems()
    {
        RegisterInventoryItemsIn(inventoryRoot == null ? null : inventoryRoot.transform);
        RegisterInventoryItemsIn(canvasTransform);
    }

    private void RegisterInventoryItemsIn(Transform root)
    {
        if (root == null)
        {
            return;
        }

        InventoryItem[] existingItems = root.GetComponentsInChildren<InventoryItem>(true);
        for (int i = 0; i < existingItems.Length; i++)
        {
            RegisterInventoryItem(existingItems[i]);
        }
    }

    private void RefreshWeightState()
    {
        currentCarryWeight = CalculateCurrentCarryWeight();
        RefreshWeightText();
        ApplyWeightMovementState();
    }

    private float CalculateCurrentCarryWeight()
    {
        float totalWeight = 0f;

        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = inventoryItems[i];
            if (item == null)
            {
                inventoryItems.RemoveAt(i);
                continue;
            }

            totalWeight += item.TotalWeight;
        }

        return totalWeight;
    }

    private void RefreshWeightText()
    {
        if (weightText == null)
        {
            return;
        }

        weightText.raycastTarget = false;
        weightText.richText = true;
        weightText.color = normalWeightColor;
        weightText.text = $"Вес: <color=#{ColorUtility.ToHtmlStringRGBA(GetWeightTextColor())}>{FormatWeight(currentCarryWeight)}</color> / {FormatWeight(MaxCarryWeight)}";
    }

    private void ApplyWeightMovementState()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.SetMovementEnabled(IsMovementBlockedByWeight == false);
    }

    private string FormatWeight(float weight)
    {
        float normalizedWeight = Mathf.Max(0f, weight);

        if (normalizedWeight < 1f)
        {
            return $"{Mathf.RoundToInt(normalizedWeight * 1000f)} ГР";
        }

        return $"{normalizedWeight:0.#} КГ";
    }

    private Color GetWeightTextColor()
    {
        if (currentCarryWeight >= MovementBlockWeight)
        {
            return movementBlockedWeightColor;
        }

        if (currentCarryWeight >= MaxCarryWeight)
        {
            return overweightColor;
        }

        return normalWeightColor;
    }

    private void ShowItemInfoPanel(InventoryItem item)
    {
        if (itemInfoPanel == null)
        {
            return;
        }

        itemInfoPanel.Show(item);
    }

    private void HideItemInfoPanel()
    {
        if (itemInfoPanel == null)
        {
            return;
        }

        itemInfoPanel.Hide();
    }

    private int NormalizeItemAmount(ItemData itemData, int amount)
    {
        return itemData != null && itemData.IsStackable ? Mathf.Max(1, amount) : 1;
    }

    private InventoryGrid GetInsertionGrid()
    {
        return selectedItemGrid != null ? selectedItemGrid : defaultItemGrid;
    }

    private void BeginDrag()
    {
        if (selectedItem != null)
        {
            return;
        }

        Vector2Int tileGridPosition = GetTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return;
        }

        dragOriginGrid = selectedItemGrid;
        dragOriginPosition = new Vector2Int(item.onGridPositionX, item.onGridPositionY);
        dragOriginRotated = item.rotated;

        PickupItem(tileGridPosition);
    }

    private Vector2Int GetTileGridPosition()
    {
        return selectedItemGrid.GetTileGridPosition(Input.mousePosition, selectedItem);
    }

    private void RotateSelectedItem()
    {
        if (selectedItem == null) { return; }

        selectedItem.Rotate();
    }

    private void ReleaseDraggedItem()
    {
        if (selectedItem == null)
        {
            return;
        }

        if (selectedItemGrid != null)
        {
            Vector2Int tileGridPosition = GetTileGridPosition();

            if (TryPlaceDraggedItem(selectedItemGrid, tileGridPosition))
            {
                return;
            }
        }

        ReturnDraggedItemToOrigin();
    }

    private bool TryPlaceDraggedItem(InventoryGrid targetGrid, Vector2Int tileGridPosition)
    {
        if (targetGrid == null || selectedItem == null)
        {
            return false;
        }

        SlottedItemGrid slottedGrid = targetGrid as SlottedItemGrid;
        if (TryMergeDraggedItemIntoStack(targetGrid, tileGridPosition))
        {
            return true;
        }

        if (slottedGrid != null &&
            slottedGrid.ShouldSplitStackOnPlace(selectedItem, tileGridPosition.x, tileGridPosition.y))
        {
            return TryPlaceSingleItemFromStack(slottedGrid, tileGridPosition);
        }

        if (targetGrid.CanPlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        targetGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y);
        FinishDraggingItem();
        return true;
    }

    private bool TryMergeDraggedItemIntoStack(InventoryGrid targetGrid, Vector2Int tileGridPosition)
    {
        if (TryGetStackMergeTarget(targetGrid, tileGridPosition, out InventoryItem targetStack) == false)
        {
            return false;
        }

        InventoryItem mergedItem = selectedItem;
        targetStack.AddAmount(mergedItem.CurrentAmount);
        DestroyInventoryItem(mergedItem);
        RefreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private bool TryGetStackMergeTarget(InventoryGrid targetGrid, Vector2Int tileGridPosition, out InventoryItem targetStack)
    {
        targetStack = null;

        if (targetGrid == null ||
            selectedItem == null ||
            selectedItem.IsStackable == false ||
            targetGrid.CanMergeStackAt(tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        targetStack = targetGrid.GetItem(tileGridPosition.x, tileGridPosition.y);
        return targetStack != null &&
               targetStack != selectedItem &&
               targetStack.CanStackWith(selectedItem.itemData);
    }

    private bool TryPlaceSingleItemFromStack(SlottedItemGrid slottedGrid, Vector2Int tileGridPosition)
    {
        if (slottedGrid == null ||
            selectedItem == null ||
            dragOriginGrid == null)
        {
            return false;
        }

        if (slottedGrid == dragOriginGrid && tileGridPosition == dragOriginPosition)
        {
            return false;
        }

        if (slottedGrid.CanPlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            return false;
        }

        int originalAmount = selectedItem.CurrentAmount;
        bool originalRotated = selectedItem.rotated;

        selectedItem.SetAmount(originalAmount - 1);
        selectedItem.SetRotated(dragOriginRotated);

        if (dragOriginGrid.CanPlaceItem(selectedItem, dragOriginPosition.x, dragOriginPosition.y) == false)
        {
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        InventoryItem singleItem = CreateItem(selectedItem.itemData, 1, selectedItem.RuntimeIconParts);

        if (slottedGrid.CanPlaceItem(singleItem, tileGridPosition.x, tileGridPosition.y) == false)
        {
            Destroy(singleItem.gameObject);
            selectedItem.SetAmount(originalAmount);
            selectedItem.SetRotated(originalRotated);
            return false;
        }

        slottedGrid.PlaceItem(singleItem, tileGridPosition.x, tileGridPosition.y);
        dragOriginGrid.PlaceItem(selectedItem, dragOriginPosition.x, dragOriginPosition.y);

        RegisterInventoryItem(singleItem);
        RefreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private void DestroyInventoryItem(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        inventoryItems.Remove(item);
        Destroy(item.gameObject);
    }

    private bool ReturnDraggedItemToOrigin()
    {
        if (selectedItem == null)
        {
            return true;
        }

        if (dragOriginGrid == null)
        {
            return false;
        }

        selectedItem.SetRotated(dragOriginRotated);

        dragOriginGrid.PlaceItem(selectedItem, dragOriginPosition.x, dragOriginPosition.y);
        FinishDraggingItem();
        return true;
    }

    private void PickupItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);

        if (selectedItem != null)
        {
            StartDraggingItem(selectedItem);
        }
    }

    private void StartDraggingItem(InventoryItem item)
    {
        selectedItem = item;
        HideItemInfoPanel();
        selectedItem.SetCellVisualsVisible(false);
        selectedItem.SetOverlayTextsVisible(false);
        rectTransform = selectedItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.SetAsLastSibling();
        rectTransform.position = Input.mousePosition;
    }

    private void ItemIconDrag()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    private void FinishDraggingItem()
    {
        selectedItem = null;
        rectTransform = null;
        dragOriginGrid = null;
    }

    private void HandleIconPrewarmProgress(int completedCount, int totalCount, ItemData itemData)
    {
        if (logIconPrewarmProgress == false)
        {
            return;
        }

        string itemName = itemData == null ? "None" : itemData.ItemName;
        Debug.Log($"Item icon bootstrap: {completedCount}/{totalCount} ({itemName})", this);
    }

    private void ToggleInventory()
    {
        SetInventoryOpen(IsOpen == false, false);
    }

    private void SetInventoryOpen(bool isOpen, bool force)
    {
        if (force == false && IsOpen == isOpen)
        {
            return;
        }

        if (isOpen == false && TryStashSelectedItem() == false)
        {
            return;
        }

        IsOpen = isOpen;

        if (inventoryRoot != null && inventoryCanvasGroup == null)
        {
            inventoryRoot.SetActive(IsOpen);
        }
        else if (inventoryRoot != null && inventoryRoot.activeSelf == false)
        {
            inventoryRoot.SetActive(true);
        }

        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.alpha = IsOpen ? 1f : 0f;
            inventoryCanvasGroup.interactable = IsOpen;
            inventoryCanvasGroup.blocksRaycasts = IsOpen;
        }

        if (unlockCursorWhileOpen)
        {
            Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsOpen;
        }

        if (disablePlayerControlsWhileOpen && playerController != null)
        {
            playerController.SetControlsEnabled(IsOpen == false);
            ApplyWeightMovementState();
        }

        if (IsOpen == false)
        {
            SelectedItemGrid = null;
            inventoryHighlight.Show(false);
            HideItemInfoPanel();
        }
    }

    private bool TryStashSelectedItem()
    {
        return ReturnDraggedItemToOrigin();
    }
}
