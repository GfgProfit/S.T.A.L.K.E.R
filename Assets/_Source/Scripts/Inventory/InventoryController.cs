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
    [SerializeField] private InventoryItem itemPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private InventoryGrid defaultItemGrid;
    [SerializeField] private InventoryHighlight inventoryHighlight;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCharacterStats playerStats;
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private Camera dropCamera;
    [SerializeField] [Min(0f)] private float dropForwardDistance = 1.2f;
    [SerializeField] [Min(0f)] private float dropUpOffset = 0.45f;
    [SerializeField] [Min(0f)] private float dropGroundProbeHeight = 1.5f;
    [SerializeField] [Min(0f)] private float dropGroundProbeDistance = 3f;
    [SerializeField] [Min(0f)] private float dropGroundOffset = 0.08f;
    [SerializeField] [Min(0f)] private float dropObstacleClearance = 0.2f;
    [SerializeField] [Min(0f)] private float dropImpulse = 1.2f;
    [SerializeField] private LayerMask dropGroundLayers = ~0;
    [SerializeField] private LayerMask dropObstacleLayers = ~0;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private ItemInfoPanel itemInfoPanel;
    [SerializeField] private InventoryItemContextMenu itemContextMenu;
    [SerializeField] private GameObject closedSlotPrefab;
    [SerializeField] private List<InventoryItem> initialInventoryItems = new List<InventoryItem>();
    [SerializeField] private List<InventoryGrid> quickActionGridReferences = new List<InventoryGrid>();
    [SerializeField] private List<EquipmentSlotGrid> equipmentSlotGrids = new List<EquipmentSlotGrid>();
    [SerializeField] private List<SlottedItemGrid> slottedItemGrids = new List<SlottedItemGrid>();
    [Header("Stats Info")]
    [SerializeField] private CharacterStatsInfoPanel playerStatsInfoPanel;
    [SerializeField] private bool hidePlayerStatsInfoWhenEmpty = true;
    [SerializeField] [Min(0f)] private float maxCarryWeight = 50f;
    [SerializeField] [Min(0f)] private float movementBlockExtraWeight = 10f;
    [SerializeField] private bool openOnStart;
    [SerializeField] private bool unlockCursorWhileOpen = true;
    [SerializeField] private bool disablePlayerControlsWhileOpen = true;
    [SerializeField] private bool prewarmItemIconsOnStart = true;
    [SerializeField] private bool logIconPrewarmProgress;

    [Inject] private IPlayerInput playerInput = null;

    private InventoryItem selectedItem;
    private RectTransform rectTransform;
    private InventoryItem itemToHighlight;
    private InventoryGrid dragOriginGrid;
    private InventoryGrid contextMenuGrid;
    private InventoryItem contextMenuItem;
    private Vector2Int dragOriginPosition;
    private bool dragOriginRotated;
    private bool iconsReady;
    private IPlayerInput fallbackPlayerInput;
    private readonly List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private readonly List<InventoryGrid> quickActionGrids = new List<InventoryGrid>();
    private readonly CharacterStatBlock equippedStats = new CharacterStatBlock();
    private float currentCarryWeight;

    public bool IsOpen { get; private set; }
    public float CurrentCarryWeight => currentCarryWeight;
    public float BaseMaxCarryWeight => Mathf.Max(0f, maxCarryWeight);
    public float MaxCarryWeight => Mathf.Max(0f, BaseMaxCarryWeight + GetCarryWeightBonusKg());
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
        iconsReady = prewarmItemIconsOnStart == false;

        itemContextMenu.Initialize(DropSingleContextMenuItem, DropContextMenuItemStack);
        RegisterExistingInventoryItems();
        SetInventoryOpen(openOnStart, true);
        RefreshWeightState();
    }

    private IEnumerator Start()
    {
        RefreshWeightState();

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
            HideContextMenu();
            return;
        }

        if (iconsReady == false)
        {
            inventoryHighlight.Show(false);
            HideItemInfoPanel();
            HideContextMenu();
            return;
        }

        ItemIconDrag();

        CloseContextMenuIfPointerIsOutsideRadius();

        if (HandleContextMenuInput())
        {
            return;
        }

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

        if (TryHandleHoveredItemDropInput())
        {
            return;
        }

        HandleHighlight();

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (TryHandleQuickItemAction())
            {
                return;
            }

            BeginDrag();
        }
    }

    public bool TryInsertItem(ItemData itemData)
    {
        return TryInsertItem(itemData, 1);
    }

    public bool TryInsertItem(ItemData itemData, int amount)
    {
        return TryInsertItem(itemData, amount, null);
    }

    public bool TryInsertItem(ItemData itemData, int amount, float? durabilityPercent)
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

        InventoryItem itemToInsert = CreateItem(itemData, normalizedAmount, null, durabilityPercent);

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
            if (TryPrepareSlotRestrictionsForPlacement(targetGrid, itemToInsert) == false)
            {
                return false;
            }

            targetGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
            return true;
        }

        return false;
    }

    private bool TryHandleQuickItemAction()
    {
        bool quickMoveToInventory = PlayerInput.IsInventoryQuickMoveModifierHeld();
        bool quickEquip = PlayerInput.IsInventoryQuickEquipModifierHeld();

        if (quickMoveToInventory == false && quickEquip == false)
        {
            return false;
        }

        if (selectedItem != null || selectedItemGrid == null)
        {
            return true;
        }

        HideContextMenu();

        Vector2Int tileGridPosition = GetTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return true;
        }

        if (quickEquip)
        {
            TryQuickEquipItem(selectedItemGrid, item);
        }
        else
        {
            TryQuickMoveItemToInventory(selectedItemGrid, item);
        }

        inventoryHighlight.Show(false);
        HideItemInfoPanel();
        return true;
    }

    private bool TryHandleHoveredItemDropInput()
    {
        if (PlayerInput.IsInventoryDropPressed() == false)
        {
            return false;
        }

        if (selectedItem != null || selectedItemGrid == null || IsContextMenuOpen())
        {
            return true;
        }

        Vector2Int tileGridPosition = GetTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return true;
        }

        bool dropWholeStack = PlayerInput.IsInventoryDropStackModifierHeld() && item.IsStackable;
        TryDropItem(selectedItemGrid, item, dropWholeStack);
        inventoryHighlight.Show(false);
        HideItemInfoPanel();
        return true;
    }

    private bool TryQuickMoveItemToInventory(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (sourceGrid == null || item == null || defaultItemGrid == null || sourceGrid == defaultItemGrid)
        {
            return false;
        }

        return TryMoveItemToGrid(sourceGrid, item, defaultItemGrid, true);
    }

    private bool TryQuickEquipItem(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (sourceGrid == null || item == null)
        {
            return false;
        }

        CollectQuickActionGrids();

        for (int i = 0; i < quickActionGrids.Count; i++)
        {
            InventoryGrid targetGrid = quickActionGrids[i];
            if (IsQuickEquipTargetGrid(sourceGrid, targetGrid) == false)
            {
                continue;
            }

            if (targetGrid is SlottedItemGrid slottedGrid &&
                TryQuickEquipSingleItemFromStack(sourceGrid, item, slottedGrid))
            {
                return true;
            }

            if (TryMoveItemToGrid(sourceGrid, item, targetGrid, false))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryQuickEquipSingleItemFromStack(
        InventoryGrid sourceGrid,
        InventoryItem item,
        SlottedItemGrid targetGrid)
    {
        if (sourceGrid == null || item == null || targetGrid == null)
        {
            return false;
        }

        Vector2Int? targetPosition = targetGrid.FindSpaceForObject(item);
        if (targetPosition == null ||
            targetGrid.ShouldSplitStackOnPlace(item, targetPosition.Value.x, targetPosition.Value.y) == false ||
            targetGrid.CanPlaceItem(item, targetPosition.Value.x, targetPosition.Value.y) == false)
        {
            return false;
        }

        InventoryItem singleItem = CreateItem(item.itemData, 1, item.RuntimeIconParts);

        if (targetGrid.CanPlaceItem(singleItem, targetPosition.Value.x, targetPosition.Value.y) == false)
        {
            Destroy(singleItem.gameObject);
            return false;
        }

        item.SetAmount(item.CurrentAmount - 1);
        targetGrid.PlaceItem(singleItem, targetPosition.Value.x, targetPosition.Value.y);

        RegisterInventoryItem(singleItem);
        RefreshWeightState();
        return true;
    }

    private bool TryMoveItemToGrid(
        InventoryGrid sourceGrid,
        InventoryItem item,
        InventoryGrid targetGrid,
        bool allowStackMerge)
    {
        if (sourceGrid == null ||
            item == null ||
            targetGrid == null ||
            sourceGrid == targetGrid)
        {
            return false;
        }

        Vector2Int restorePosition = new Vector2Int(item.onGridPositionX, item.onGridPositionY);
        bool restoreRotated = item.rotated;

        if (TryDetachItemFromGrid(sourceGrid, item) == false)
        {
            return false;
        }

        if (TryPrepareSlotRestrictionsForPlacement(targetGrid, item) == false)
        {
            item.SetRotated(restoreRotated);
            sourceGrid.PlaceItem(item, restorePosition.x, restorePosition.y);
            return false;
        }

        if (allowStackMerge &&
            item.IsStackable &&
            TryAddToExistingStackInGrid(targetGrid, item.itemData, item.CurrentAmount))
        {
            DestroyInventoryItem(item);
            RefreshWeightState();
            return true;
        }

        if (InsertItem(item, targetGrid))
        {
            RegisterInventoryItem(item);
            RefreshWeightState();
            return true;
        }

        item.SetRotated(restoreRotated);
        sourceGrid.PlaceItem(item, restorePosition.x, restorePosition.y);
        return false;
    }

    private void CollectQuickActionGrids()
    {
        quickActionGrids.Clear();

        for (int i = 0; i < quickActionGridReferences.Count; i++)
        {
            InventoryGrid grid = quickActionGridReferences[i];
            if (grid != null && quickActionGrids.Contains(grid) == false)
            {
                quickActionGrids.Add(grid);
            }
        }
    }

    private bool IsQuickEquipTargetGrid(InventoryGrid sourceGrid, InventoryGrid targetGrid)
    {
        if (targetGrid == null ||
            targetGrid == sourceGrid ||
            targetGrid.gameObject.activeInHierarchy == false)
        {
            return false;
        }

        return targetGrid is EquipmentSlotGrid || targetGrid is SlottedItemGrid;
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
        return CreateItem(itemData, amount, null, null);
    }

    private InventoryItem CreateItem(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        return CreateItem(itemData, amount, runtimeIconParts, null);
    }

    private InventoryItem CreateItem(
        ItemData itemData,
        int amount,
        IReadOnlyList<ItemIconPart> runtimeIconParts,
        float? durabilityPercent)
    {
        InventoryItem inventoryItem = Instantiate(itemPrefab);
        inventoryItem.Set(itemData, amount, runtimeIconParts, durabilityPercent);

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
        item.DurabilityChanged += HandleInventoryItemDurabilityChanged;
    }

    private void RegisterExistingInventoryItems()
    {
        for (int i = 0; i < initialInventoryItems.Count; i++)
        {
            RegisterInventoryItem(initialInventoryItems[i]);
        }
    }

    private void HandleInventoryItemDurabilityChanged(InventoryItem item)
    {
        RefreshWeightState();

        if (itemInfoPanel != null && itemToHighlight == item)
        {
            itemInfoPanel.Show(item);
        }
    }

    private void RefreshWeightState()
    {
        RefreshEquippedStats();
        currentCarryWeight = CalculateCurrentCarryWeight();
        RefreshWeightText();
        ApplyWeightMovementState();
    }

    private void RefreshEquippedStats()
    {
        equippedStats.Clear();

        for (int i = 0; i < equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = equipmentSlotGrids[i];
            InventoryItem item = grid == null ? null : grid.EquippedItem;
            if (ShouldApplyEquippedStats(item))
            {
                CharacterStatUtility.AddItemStats(equippedStats, item);
            }
        }

        if (playerStats != null)
        {
            playerStats.ApplyEquipmentStats(equippedStats);
        }

        RefreshSlotRestrictions();

        if (playerStatsInfoPanel != null)
        {
            playerStatsInfoPanel.RenderCharacterStats(
                playerStats == null ? equippedStats : playerStats.CurrentStats,
                GameProjectSettings.LoadDefault().StatCurrentValueColor,
                hidePlayerStatsInfoWhenEmpty,
                true);
        }
    }

    private static bool ShouldApplyEquippedStats(InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            return false;
        }

        return item.itemData.ItemType == ItemType.Armor ||
               item.itemData.ItemType == ItemType.Helmet;
    }

    private float GetCarryWeightBonusKg()
    {
        return playerStats == null
            ? equippedStats.Get(CharacterStatType.CarryWeight)
            : playerStats.GetStat(CharacterStatType.CarryWeight);
    }

    private void RefreshSlotRestrictions()
    {
        GameObject resolvedClosedSlotPrefab = GetClosedSlotPrefab();
        InventoryItem equippedArmor = GetFirstEquippedItem(ItemType.Armor);
        bool closeHelmetSlot = equippedArmor != null &&
                               equippedArmor.itemData != null &&
                               equippedArmor.itemData.CanEquipHelmet == false;

        SetEquipmentSlotsClosed(ItemType.Helmet, closeHelmetSlot, resolvedClosedSlotPrefab);
        SetOpenArtifactSlotCount(GetArmorArtifactSlotCount(equippedArmor), resolvedClosedSlotPrefab);
    }

    private bool TryPrepareSlotRestrictionsForPlacement(InventoryGrid targetGrid, InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            return false;
        }

        if (item.itemData.ItemType != ItemType.Armor || IsEquipmentSlot(targetGrid, ItemType.Armor) == false)
        {
            return true;
        }

        if (CanSetOpenArtifactSlotCount(GetArmorArtifactSlotCount(item)) == false)
        {
            return false;
        }

        return item.itemData.CanEquipHelmet || TryMoveEquippedItemToInventory(ItemType.Helmet);
    }

    private bool CanDetachItemWithSlotRestrictions(InventoryGrid sourceGrid, InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            return false;
        }

        if (item.itemData.ItemType != ItemType.Armor || IsEquipmentSlot(sourceGrid, ItemType.Armor) == false)
        {
            return true;
        }

        return CanSetOpenArtifactSlotCount(0);
    }

    private InventoryItem GetFirstEquippedItem(ItemType itemType)
    {
        if (TryGetFirstEquipmentSlot(itemType, out EquipmentSlotGrid slot))
        {
            return slot.EquippedItem;
        }

        return null;
    }

    private bool TryGetFirstEquipmentSlot(ItemType itemType, out EquipmentSlotGrid slot)
    {
        slot = null;

        for (int i = 0; i < equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = equipmentSlotGrids[i];
            if (grid == null || grid.AcceptedItemType != itemType)
            {
                continue;
            }

            if (grid.EquippedItem != null)
            {
                slot = grid;
                return true;
            }
        }

        return false;
    }

    private bool TryMoveEquippedItemToInventory(ItemType itemType)
    {
        if (defaultItemGrid == null)
        {
            return false;
        }

        if (TryGetFirstEquipmentSlot(itemType, out EquipmentSlotGrid sourceGrid) == false)
        {
            return true;
        }

        InventoryItem item = sourceGrid.EquippedItem;
        if (item == null)
        {
            return true;
        }

        Vector2Int restorePosition = new Vector2Int(item.onGridPositionX, item.onGridPositionY);
        bool restoreRotated = item.rotated;

        if (TryDetachItemFromGrid(sourceGrid, item) == false)
        {
            return false;
        }

        if (InsertItem(item, defaultItemGrid))
        {
            RegisterInventoryItem(item);
            RefreshWeightState();
            return true;
        }

        item.SetRotated(restoreRotated);
        sourceGrid.PlaceItem(item, restorePosition.x, restorePosition.y);
        RefreshWeightState();
        return false;
    }

    private void SetEquipmentSlotsClosed(ItemType itemType, bool closed, GameObject resolvedClosedSlotPrefab)
    {
        for (int i = 0; i < equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = equipmentSlotGrids[i];
            if (grid == null || grid.AcceptedItemType != itemType)
            {
                continue;
            }

            grid.SetClosed(closed, resolvedClosedSlotPrefab);
        }
    }

    private bool CanSetOpenArtifactSlotCount(int openSlotCount)
    {
        int remainingOpenSlots = Mathf.Max(0, openSlotCount);

        for (int i = 0; i < slottedItemGrids.Count; i++)
        {
            SlottedItemGrid grid = slottedItemGrids[i];
            if (grid == null || grid.HasArtifactSlots == false)
            {
                continue;
            }

            if (grid.CanSetOpenArtifactSlotCount(remainingOpenSlots) == false)
            {
                return false;
            }

            remainingOpenSlots = Mathf.Max(0, remainingOpenSlots - grid.ArtifactSlotCount);
        }

        return true;
    }

    private void SetOpenArtifactSlotCount(int openSlotCount, GameObject resolvedClosedSlotPrefab)
    {
        int remainingOpenSlots = Mathf.Max(0, openSlotCount);

        for (int i = 0; i < slottedItemGrids.Count; i++)
        {
            SlottedItemGrid grid = slottedItemGrids[i];
            if (grid == null || grid.HasArtifactSlots == false)
            {
                continue;
            }

            remainingOpenSlots = grid.SetOpenArtifactSlotCount(remainingOpenSlots, resolvedClosedSlotPrefab);
        }
    }

    private int GetArmorArtifactSlotCount(InventoryItem armorItem)
    {
        if (armorItem == null ||
            armorItem.itemData == null ||
            armorItem.itemData.ItemType != ItemType.Armor ||
            armorItem.itemData.StatModifiers == null)
        {
            return 0;
        }

        for (int i = 0; i < armorItem.itemData.StatModifiers.Count; i++)
        {
            CharacterStatModifier modifier = armorItem.itemData.StatModifiers[i];
            if (modifier.StatType != CharacterStatType.ArtifactContainers || modifier.HasValue == false)
            {
                continue;
            }

            float durabilityPercent = armorItem.HasDurability ? armorItem.CurrentDurabilityPercent : 100f;
            float slotCount = CharacterStatUtility.CalculateCurrentValue(modifier, durabilityPercent);
            return Mathf.Max(0, Mathf.RoundToInt(slotCount));
        }

        return 0;
    }

    private static bool IsEquipmentSlot(InventoryGrid grid, ItemType itemType)
    {
        return grid is EquipmentSlotGrid equipmentSlot && equipmentSlot.AcceptedItemType == itemType;
    }

    private GameObject GetClosedSlotPrefab()
    {
        return closedSlotPrefab;
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
        weightText.color = GameProjectSettings.LoadDefault().NormalWeightColor;
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
        GameProjectSettings settings = GameProjectSettings.LoadDefault();

        if (currentCarryWeight >= MovementBlockWeight)
        {
            return settings.MovementBlockedWeightColor;
        }

        if (currentCarryWeight >= MaxCarryWeight)
        {
            return settings.OverweightColor;
        }

        return settings.NormalWeightColor;
    }

    private void ShowItemInfoPanel(InventoryItem item)
    {
        if (itemInfoPanel == null)
        {
            return;
        }

        if (IsContextMenuOpen())
        {
            HideItemInfoPanel();
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

    private bool IsContextMenuOpen()
    {
        return itemContextMenu != null && itemContextMenu.IsOpen;
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

        HideContextMenu();

        Vector2Int tileGridPosition = GetTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return;
        }

        if (CanDetachItemWithSlotRestrictions(selectedItemGrid, item) == false)
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

        if (TryPrepareSlotRestrictionsForPlacement(targetGrid, selectedItem) == false)
        {
            return false;
        }

        targetGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y);
        RefreshWeightState();
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
        item.DurabilityChanged -= HandleInventoryItemDurabilityChanged;
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
        RefreshWeightState();
        FinishDraggingItem();
        return true;
    }

    private void PickupItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);

        if (selectedItem != null)
        {
            StartDraggingItem(selectedItem);
            RefreshWeightState();
        }
    }

    private void StartDraggingItem(InventoryItem item)
    {
        selectedItem = item;
        HideItemInfoPanel();
        selectedItem.SetCellVisualsVisible(false);
        selectedItem.SetOverlayTextsVisible(false);
        rectTransform = selectedItem.RectTransform;
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

    private bool HandleContextMenuInput()
    {
        if (itemContextMenu != null && itemContextMenu.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (itemContextMenu.ContainsScreenPoint(Input.mousePosition))
                {
                    return true;
                }

                HideContextMenu();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Mouse1) &&
                itemContextMenu.ContainsScreenPoint(Input.mousePosition))
            {
                return true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            OpenContextMenuAtCursor();
            return true;
        }

        return false;
    }

    private void CloseContextMenuIfPointerIsOutsideRadius()
    {
        if (itemContextMenu == null ||
            itemContextMenu.ShouldCloseForPointer(Input.mousePosition) == false)
        {
            return;
        }

        HideContextMenu();
    }

    private void OpenContextMenuAtCursor()
    {
        HideContextMenu();

        if (selectedItem != null || selectedItemGrid == null || itemContextMenu == null)
        {
            return;
        }

        Vector2Int tileGridPosition = GetTileGridPosition();
        InventoryItem item = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return;
        }

        contextMenuGrid = selectedItemGrid;
        contextMenuItem = item;

        HideItemInfoPanel();
        itemContextMenu.Show(item, Input.mousePosition);
    }

    private void DropSingleContextMenuItem()
    {
        DropContextMenuItem(false);
    }

    private void DropContextMenuItemStack()
    {
        DropContextMenuItem(true);
    }

    private void DropContextMenuItem(bool wholeStack)
    {
        InventoryItem item = contextMenuItem;
        InventoryGrid grid = contextMenuGrid;
        HideContextMenu();

        TryDropItem(grid, item, wholeStack);
    }

    private bool TryDropItem(InventoryGrid grid, InventoryItem item, bool wholeStack)
    {
        if (item == null || item.itemData == null)
        {
            return false;
        }

        ItemData itemData = item.itemData;
        int amountToDrop = wholeStack ? item.CurrentAmount : 1;
        float durabilityPercent = item.CurrentDurabilityPercent;
        bool removeWholeItem = wholeStack || item.IsStackable == false || item.CurrentAmount <= amountToDrop;

        if (removeWholeItem == false)
        {
            if (TrySpawnDroppedWorldItem(itemData, amountToDrop, durabilityPercent) == false)
            {
                return false;
            }

            item.SetAmount(item.CurrentAmount - amountToDrop);
            RefreshWeightState();
            return true;
        }

        Vector2Int restorePosition = new Vector2Int(item.onGridPositionX, item.onGridPositionY);

        if (TryDetachItemFromGrid(grid, item) == false)
        {
            return false;
        }

        if (TrySpawnDroppedWorldItem(itemData, amountToDrop, durabilityPercent) == false)
        {
            grid.PlaceItem(item, restorePosition.x, restorePosition.y);
            return false;
        }

        DestroyInventoryItem(item);
        RefreshWeightState();
        return true;
    }

    private bool TrySpawnDroppedWorldItem(ItemData itemData, int amount, float durabilityPercent)
    {
        if (itemData == null)
        {
            return false;
        }

        Vector3 dropPosition = CalculateDropPosition();
        Vector3 dropForward = GetDropForwardDirection();
        Quaternion dropRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        WorldItem worldItemPrefab = ResolveWorldItemPrefab(itemData);

        if (worldItemPrefab == null)
        {
            return false;
        }

        WorldItem worldItem = Instantiate(worldItemPrefab, dropPosition, dropRotation);
        worldItem.Initialize(itemData, amount, durabilityPercent);
        ApplyDropImpulse(worldItem.ItemRigidbody, dropForward);
        return true;
    }

    private WorldItem ResolveWorldItemPrefab(ItemData itemData)
    {
        if (itemData == null)
        {
            return null;
        }

        return itemData.WorldItemPrefab;
    }

    private Vector3 CalculateDropPosition()
    {
        Transform origin = GetDropOrigin();
        Vector3 dropForward = GetDropForwardDirection();
        Vector3 rayStart = origin.position + Vector3.up * dropUpOffset;
        Vector3 position = rayStart + dropForward * dropForwardDistance;

        if (Physics.Raycast(
                rayStart,
                dropForward,
                out RaycastHit obstacleHit,
                dropForwardDistance,
                dropObstacleLayers,
                QueryTriggerInteraction.Ignore))
        {
            position = obstacleHit.point - dropForward * dropObstacleClearance;
        }

        rayStart = position + Vector3.up * dropGroundProbeHeight;
        float rayDistance = dropGroundProbeHeight + dropGroundProbeDistance;

        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                dropGroundLayers,
                QueryTriggerInteraction.Ignore))
        {
            position = hit.point + Vector3.up * dropGroundOffset;
        }

        return position;
    }

    private Transform GetDropOrigin()
    {
        if (dropOrigin != null)
        {
            return dropOrigin;
        }

        if (playerController != null)
        {
            return playerController.transform;
        }

        return transform;
    }

    private Vector3 GetDropForwardDirection()
    {
        Transform forwardSource = dropCamera != null
            ? dropCamera.transform
            : dropOrigin != null
                ? dropOrigin
                : playerController != null ? playerController.transform : transform;

        Vector3 forward = forwardSource.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = forwardSource.forward;
        }

        return forward.normalized;
    }

    private void ApplyDropImpulse(Rigidbody rigidbody, Vector3 dropForward)
    {
        if (dropImpulse <= 0f || rigidbody == null)
        {
            return;
        }

        Vector3 impulseDirection = (dropForward + Vector3.up * 0.25f).normalized;
        rigidbody.AddForce(impulseDirection * dropImpulse, ForceMode.VelocityChange);
    }

    private bool TryDetachItemFromGrid(InventoryGrid grid, InventoryItem item)
    {
        if (grid == null || item == null)
        {
            return false;
        }

        if (CanDetachItemWithSlotRestrictions(grid, item) == false)
        {
            return false;
        }

        Vector2Int position = new Vector2Int(item.onGridPositionX, item.onGridPositionY);
        InventoryItem pickedItem = grid.PickUpItem(position.x, position.y);

        if (pickedItem == null)
        {
            return false;
        }

        if (pickedItem == item)
        {
            return true;
        }

        grid.PlaceItem(pickedItem, position.x, position.y);
        return false;
    }

    private void HideContextMenu()
    {
        if (itemContextMenu != null)
        {
            itemContextMenu.Hide();
        }

        contextMenuGrid = null;
        contextMenuItem = null;
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
            HideContextMenu();
        }
    }

    private bool TryStashSelectedItem()
    {
        return ReturnDraggedItemToOrigin();
    }
}
