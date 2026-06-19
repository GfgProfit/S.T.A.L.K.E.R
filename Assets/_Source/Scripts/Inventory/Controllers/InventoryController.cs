using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] [BoxGroup("Items")] private List<ItemData> _items;
    [SerializeField] [BoxGroup("Items")] private InventoryItem _itemPrefab;
    [SerializeField] [BoxGroup("UI")] private Transform _canvasTransform;
    [SerializeField] [BoxGroup("UI")] private GameObject _inventoryRoot;
    [SerializeField] [BoxGroup("UI")] private CanvasGroup _inventoryCanvasGroup;
    [SerializeField] [BoxGroup("Grids")] private InventoryGrid _defaultItemGrid;
    [SerializeField] [BoxGroup("UI")] private InventoryHighlight _inventoryHighlight;
    [SerializeField] [BoxGroup("Player")] private PlayerController _playerController;
    [SerializeField] [BoxGroup("Player")] private PlayerCharacterStats _playerStats;
    [SerializeField] [BoxGroup("First Person")] private FirstPersonLegsController _firstPersonLegsController;
    [SerializeField] [BoxGroup("First Person")] private FirstPersonWeaponHolderController _firstPersonWeaponHolderController;
    [SerializeField] [BoxGroup("Weapon Slots")] [Min(0)] private int _primaryWeaponSlotIndex = 0;
    [SerializeField] [BoxGroup("Weapon Slots")] [Min(0)] private int _secondaryWeaponSlotIndex = 1;
    [SerializeField] [BoxGroup("Weapon Slots")] [Min(0)] private int _pistolWeaponSlotIndex = 2;

    [Header("Drop Settings")]
    [SerializeField] [BoxGroup("Drop Settings")] private Transform _dropOrigin;
    [SerializeField] [BoxGroup("Drop Settings")] private Camera _dropCamera;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropForwardDistance = 1.2f;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropUpOffset = 0.45f;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropGroundProbeHeight = 1.5f;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropGroundProbeDistance = 3f;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropGroundOffset = 0.08f;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropObstacleClearance = 0.2f;
    [SerializeField] [BoxGroup("Drop Settings")] [Min(0f)] private float _dropImpulse = 1.2f;
    [SerializeField] [BoxGroup("Drop Settings")] private LayerMask _dropGroundLayers = ~0;
    [SerializeField] [BoxGroup("Drop Settings")] private LayerMask _dropObstacleLayers = ~0;
    [SerializeField] [BoxGroup("UI")] private TMP_Text _weightText;
    [SerializeField] [BoxGroup("UI")] private ItemInfoPanel _itemInfoPanel;
    [SerializeField] [BoxGroup("UI")] private InventoryItemContextMenu _itemContextMenu;
    [SerializeField] [BoxGroup("Grids")] private GameObject _closedSlotPrefab;
    [SerializeField] [BoxGroup("Items")] private List<InventoryItem> _initialInventoryItems = new();
    [SerializeField] [BoxGroup("Grids")] private List<InventoryGrid> _quickActionGridReferences = new();

    [Header("Quick Use")]
    [SerializeField] [BoxGroup("Quick Use")] private List<QuickUseSlotBinding> _quickUseSlotBindings = new();
    [SerializeField] [BoxGroup("Quick Use")] private TMP_Text _miniActionText;
    [SerializeField] [BoxGroup("Quick Use")] [EnableIf(nameof(UsesMiniActionText))] [Min(0f)] private float _miniActionTextDuration = 2.5f;

    [SerializeField] [BoxGroup("Grids")] private List<EquipmentSlotGrid> _equipmentSlotGrids = new();
    [SerializeField] [BoxGroup("Grids")] private List<SlottedItemGrid> _slottedItemGrids = new();

    [Header("Stats Info")]
    [SerializeField] [BoxGroup("Stats Info")] private CharacterStatsInfoPanel _playerStatsInfoPanel;
    [SerializeField] [BoxGroup("Stats Info")] private bool _hidePlayerStatsInfoWhenEmpty = true;
    [SerializeField] [BoxGroup("Weight")] [Min(0f)] private float _maxCarryWeight = 50f;
    [SerializeField] [BoxGroup("Weight")] [Min(0f)] private float _movementBlockExtraWeight = 10f;
    [SerializeField] [BoxGroup("Open State")] private bool _openOnStart;
    [SerializeField] [BoxGroup("Open State")] private bool _unlockCursorWhileOpen = true;
    [SerializeField] [BoxGroup("Open State")] private bool _disablePlayerControlsWhileOpen = true;
    [SerializeField] [BoxGroup("Icon Prewarm")] private bool _prewarmItemIconsOnStart = true;
    [SerializeField] [BoxGroup("Icon Prewarm")] [EnableIf(nameof(PrewarmsItemIcons))] private bool _logIconPrewarmProgress;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private InventoryItemFactory _itemFactory;
    private InventoryEquipmentSlotService _equipmentSlotService;
    private InventoryEquipmentActionService _equipmentActionService;
    private InventoryQuickActionService _quickActionService;
    private InventoryContextMenuController _contextMenuController;
    private InventoryItemDropProcessor _dropProcessor;
    private InventoryWeightStateController _weightStateController;
    private InventoryHoverInfoController _hoverInfoController;
    private InventoryItemCompatibilityService _itemCompatibilityService;
    private InventoryWeaponModuleService _weaponModuleService;
    private InventoryDragPlacementService _dragPlacementService;
    private InventoryDragController _dragController;
    private InventoryOpenStateController _openStateController;
    private InventoryItemPlacementService _itemPlacementService;
    private InventoryHoveredItemActionController _hoveredItemActionController;
    private InventoryQuickUseService _quickUseService;
    private InventoryUpdateController _updateController;
    private InventoryIconPrewarmController _iconPrewarmController;
    private InventoryRootView _inventoryView;
    private MiniActionTextView _miniActionTextView;
    private InventoryViewModel _viewModel;
    private MiniActionTextViewModel _miniActionTextViewModel;
    private GameProjectSettings _settings;
    private readonly Dictionary<int, QuickUseInventorySlotViewModel> _quickUseInventorySlotViewModels = new();
    private readonly Dictionary<int, QuickUseHudSlotViewModel> _quickUseHudSlotViewModels = new();
    private readonly InventoryDragState _dragState = new();
    private readonly InventoryDropService _dropService = new();
    private readonly InventoryItemRegistry _itemRegistry = new();
    private InventoryGrid _selectedItemGrid;
    private int _activeWeaponSlotIndex = -1;
    private CancellationTokenSource _miniActionTextCancellation;

    public InventoryGrid SelectedItemGrid
    {
        get => _selectedItemGrid;
        set
        {
            _selectedItemGrid = value;

            if (value != null)
            {
                _inventoryHighlight.SetParent(value.RectTransform, value.HighlightSiblingIndex);
            }
        }
    }

    public bool IsOpen => OpenStateController.IsOpen;
    public float CurrentCarryWeight => WeightStateController.CurrentCarryWeight;
    public float BaseMaxCarryWeight => WeightStateController.BaseMaxCarryWeight;
    public float MaxCarryWeight => WeightStateController.MaxCarryWeight;
    public float MovementBlockWeight => WeightStateController.MovementBlockWeight;
    public bool IsMovementBlockedByWeight => WeightStateController.IsMovementBlockedByWeight;
    private bool UsesMiniActionText() => _miniActionText != null;
    private bool PrewarmsItemIcons() => _prewarmItemIconsOnStart;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private InventoryEquipmentSlotService EquipmentSlotService
    {
        get
        {
            _equipmentSlotService ??= CreateEquipmentSlotService();
            return _equipmentSlotService;
        }
    }

    private InventoryItemFactory ItemFactory
    {
        get
        {
            _itemFactory ??= new InventoryItemFactory(_itemPrefab);
            return _itemFactory;
        }
    }

    private InventoryQuickActionService QuickActionService
    {
        get
        {
            _quickActionService ??= CreateQuickActionService();
            return _quickActionService;
        }
    }

    private InventoryContextMenuController ContextMenuController
    {
        get
        {
            _contextMenuController ??= CreateContextMenuController();
            return _contextMenuController;
        }
    }

    private InventoryItemDropProcessor DropProcessor
    {
        get
        {
            _dropProcessor ??= CreateDropProcessor();
            return _dropProcessor;
        }
    }

    private InventoryWeightStateController WeightStateController
    {
        get
        {
            _weightStateController ??= CreateWeightStateController();
            return _weightStateController;
        }
    }

    private InventoryHoverInfoController HoverInfoController
    {
        get
        {
            _hoverInfoController ??= CreateHoverInfoController();
            return _hoverInfoController;
        }
    }

    private InventoryDragPlacementService DragPlacementService
    {
        get
        {
            _dragPlacementService ??= CreateDragPlacementService();
            return _dragPlacementService;
        }
    }

    private InventoryDragController DragController
    {
        get
        {
            _dragController ??= CreateDragController();
            return _dragController;
        }
    }

    private InventoryOpenStateController OpenStateController
    {
        get
        {
            _openStateController ??= CreateOpenStateController();
            return _openStateController;
        }
    }

    private InventoryItemPlacementService ItemPlacementService
    {
        get
        {
            _itemPlacementService ??= CreateItemPlacementService();
            return _itemPlacementService;
        }
    }

    private InventoryHoveredItemActionController HoveredItemActionController
    {
        get
        {
            _hoveredItemActionController ??= CreateHoveredItemActionController();
            return _hoveredItemActionController;
        }
    }

    private InventoryEquipmentActionService EquipmentActionService
    {
        get
        {
            _equipmentActionService ??= CreateEquipmentActionService();
            return _equipmentActionService;
        }
    }

    private InventoryQuickUseService QuickUseService
    {
        get
        {
            _quickUseService ??= CreateQuickUseService();
            return _quickUseService;
        }
    }

    private InventoryIconPrewarmController IconPrewarmController
    {
        get
        {
            _iconPrewarmController ??= CreateIconPrewarmController();
            return _iconPrewarmController;
        }
    }

    private InventoryEquipmentSlotService CreateEquipmentSlotService() => new(_equipmentSlotGrids, _slottedItemGrids, _defaultItemGrid, InsertItem, TryDetachItemFromGrid, RegisterInventoryItem, RefreshWeightState, () => _closedSlotPrefab);
    private InventoryEquipmentActionService CreateEquipmentActionService() => new(_equipmentSlotGrids, _defaultItemGrid, InsertItem, TryDetachItemFromGrid, TryPrepareSlotRestrictionsForPlacement, RegisterInventoryItem, RefreshWeightState);
    private InventoryQuickActionService CreateQuickActionService() => new(_quickActionGridReferences, _defaultItemGrid, TryMoveItemToGrid, CreateItem, RegisterInventoryItem, RefreshWeightState);
    private InventoryContextMenuController CreateContextMenuController() => new(_itemContextMenu, PlayerInput, () => _selectedItemGrid, () => _dragState.SelectedItem, DragController.GetTileGridPosition, HideItemInfoPanel, TryUseContextMenuItem, TryUnloadContextMenuWeapon, CanEquipPrimaryContextMenuWeapon, TryEquipPrimaryContextMenuWeapon, CanEquipSecondaryContextMenuWeapon, TryEquipSecondaryContextMenuWeapon, CanEquipContextMenuItem, TryEquipContextMenuItem, CanUnequipContextMenuItem, TryUnequipContextMenuItem, _equipmentSlotGrids, TryAttachContextMenuModule, TryDetachWeaponModule, TryDropItem);
    private InventoryItemDropProcessor CreateDropProcessor() => new(TrySpawnDroppedWorldItem, TryDetachItemFromGrid, DestroyInventoryItem, RefreshWeightState);
    private InventoryWeightStateController CreateWeightStateController() => new(_itemRegistry, _equipmentSlotGrids, EquipmentSlotService, SetWeightViewModelState, RenderCharacterStatsInfo, _playerController, _playerStats, _hidePlayerStatsInfoWhenEmpty, _maxCarryWeight, _movementBlockExtraWeight);
    private InventoryHoverInfoController CreateHoverInfoController() => new(_inventoryHighlight, _itemInfoPanel, IsContextMenuOpen, _itemCompatibilityService);
    private InventoryItemCompatibilityService CreateItemCompatibilityService() => new(_itemRegistry, new IInventoryItemCompatibilityProvider[] { new WeaponAmmoInventoryCompatibilityProvider(), new WeaponModuleInventoryCompatibilityProvider() }, _settings.CompatibleItemHighlightColor);
    private InventoryWeaponModuleService CreateWeaponModuleService() => new(TryReturnItemToInventoryOrDrop);
    private InventoryDragPlacementService CreateDragPlacementService() => new(_dragState, ItemFactory, _itemRegistry, CanDetachItemWithSlotRestrictions, TryPrepareSlotRestrictionsForPlacement, TryInstallWeaponModule, HandleWeaponModulesChanged, RefreshWeightState);
    private InventoryDragController CreateDragController() => new(_dragState, DragPlacementService, PlayerInput, _canvasTransform, () => _selectedItemGrid, CanDetachItemWithSlotRestrictions, HideContextMenu, HideItemInfoPanel, RefreshWeightState);
    private InventoryOpenStateController CreateOpenStateController() => new(_playerController, _unlockCursorWhileOpen, _disablePlayerControlsWhileOpen, TryStashSelectedItem, ApplyWeightMovementState, HandleInventoryClosed);
    private InventoryItemPlacementService CreateItemPlacementService() => new(ItemFactory, _itemRegistry, TryPrepareSlotRestrictionsForPlacement, TryDetachItemFromGrid, DestroyInventoryItem, RefreshWeightState);
    private InventoryHoveredItemActionController CreateHoveredItemActionController() => new(PlayerInput, _dragState, () => _selectedItemGrid, DragController.GetTileGridPosition, IsContextMenuOpen, QuickActionService, TryDropItem, HoverInfoController, HideItemInfoPanel, HideContextMenu);
    private InventoryQuickUseService CreateQuickUseService() => new(_quickUseSlotBindings, TryDetachItemFromGrid, DestroyInventoryItem, RefreshWeightState);
    private InventoryUpdateController CreateUpdateController() => new(PlayerInput, () => IsOpen, () => IconPrewarmController.IconsReady, () => _selectedItemGrid, HoverInfoController, ContextMenuController, ToggleInventory, HideItemInfoPanel, HideContextMenu, DragController.ItemIconDrag, DragController.ReleaseDraggedItem, DragController.RotateSelectedItem, TryHandleHoveredItemDropInput, HandleHighlight, TryHandleQuickItemAction, DragController.BeginDrag);
    private InventoryIconPrewarmController CreateIconPrewarmController() => new(_items, _prewarmItemIconsOnStart, _logIconPrewarmProgress, this);

    private void Awake()
    {
        _settings = GameProjectSettings.LoadDefault();
        _viewModel = InventoryViewModelFactory.CreateInventory(ToggleInventoryCoreAsync, _settings);
        _miniActionTextViewModel = InventoryViewModelFactory.CreateMiniActionText();
        _inventoryView = new(_inventoryRoot, _inventoryCanvasGroup, _weightText, _settings);
        _miniActionTextView = new(_miniActionText);

        _itemFactory = new InventoryItemFactory(_itemPrefab);
        _equipmentSlotService = CreateEquipmentSlotService();
        _equipmentActionService = CreateEquipmentActionService();
        _quickActionService = CreateQuickActionService();
        _weaponModuleService = CreateWeaponModuleService();
        _contextMenuController = CreateContextMenuController();
        _dropProcessor = CreateDropProcessor();
        _weightStateController = CreateWeightStateController();
        _itemCompatibilityService = CreateItemCompatibilityService();
        _hoverInfoController = CreateHoverInfoController();
        _dragPlacementService = CreateDragPlacementService();
        _dragController = CreateDragController();
        _openStateController = CreateOpenStateController();
        _itemPlacementService = CreateItemPlacementService();
        _hoveredItemActionController = CreateHoveredItemActionController();
        _quickUseService = CreateQuickUseService();
        _iconPrewarmController = CreateIconPrewarmController();
        _updateController = CreateUpdateController();
        _itemRegistry.DurabilityChanged += HandleInventoryItemDurabilityChanged;

        _contextMenuController.Initialize();
        BindQuickUseSlots();
        RegisterExistingInventoryItems();
        SetInventoryOpen(_openOnStart, true);
        _inventoryView.Bind(_viewModel);
        _miniActionTextView.Bind(_miniActionTextViewModel);
        RefreshWeightState();
    }

    private IEnumerator Start()
    {
        RefreshWeightState();
        yield return IconPrewarmController.Prewarm();
    }

    private void Update()
    {
        RefreshQuickUseKeyTexts();
        TryHandleWeaponSlotInput();
        TryHandleQuickUseInput();
        _updateController.Tick();
    }

    private void OnDestroy()
    {
        CancelMiniActionText();
        _itemRegistry.DurabilityChanged -= HandleInventoryItemDurabilityChanged;
        _miniActionTextView?.Dispose();
        DisposeQuickUseSlots();
        _inventoryView?.Dispose();
        _miniActionTextViewModel?.Dispose();
        _viewModel?.Dispose();
    }

    public bool TryInsertItem(ItemData itemData) => TryInsertItem(itemData, 1);
    public bool TryInsertItem(ItemData itemData, int amount) => TryInsertItem(itemData, amount, null);
    public bool TryInsertItem(ItemData itemData, int amount, float? durabilityPercent) => TryInsertItem(itemData, amount, durabilityPercent, null);
    public bool TryInsertItem(ItemData itemData, int amount, float? durabilityPercent, IReadOnlyList<ItemData> installedModules) => ItemPlacementService.TryInsertItem(itemData, amount, durabilityPercent, installedModules, IconPrewarmController.IconsReady, GetInsertionGrid(), _defaultItemGrid);

    public int GetInventoryItemCount(ItemData itemData)
    {
        if (itemData == null)
        {
            return 0;
        }

        int count = 0;
        IReadOnlyList<InventoryItem> items = _itemRegistry.Items;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];

            if (item != null && item.ItemData == itemData)
            {
                count += item.CurrentAmount;
            }
        }

        return count;
    }

    public int ConsumeInventoryItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return 0;
        }

        int remainingAmount = amount;
        List<InventoryItem> items = new(_itemRegistry.Items);

        for (int i = items.Count - 1; i >= 0 && remainingAmount > 0; i--)
        {
            InventoryItem item = items[i];

            if (item == null || item.ItemData != itemData)
            {
                continue;
            }

            int itemAmount = item.CurrentAmount;

            if (itemAmount > remainingAmount)
            {
                item.SetAmount(itemAmount - remainingAmount);
                remainingAmount = 0;
                continue;
            }

            if (TryGetItemGrid(item, out InventoryGrid itemGrid) == false || TryDetachItemFromGrid(itemGrid, item) == false)
            {
                continue;
            }

            DestroyInventoryItem(item);
            remainingAmount -= itemAmount;
        }

        int consumedAmount = amount - remainingAmount;

        if (consumedAmount > 0)
        {
            RefreshWeightState();
        }

        return consumedAmount;
    }

    public bool TryReturnItemToInventoryOrDrop(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return true;
        }

        if (TryInsertItem(itemData, amount))
        {
            return true;
        }

        return TrySpawnDroppedWorldItem(itemData, amount, itemData.DefaultDurabilityPercent);
    }

    public void RefreshInventoryWeightState() => RefreshWeightState();

    private bool InsertItem(InventoryItem itemToInsert, InventoryGrid targetGrid) => ItemPlacementService.InsertItem(itemToInsert, targetGrid);
    private bool TryHandleQuickItemAction() => HoveredItemActionController.TryHandleQuickItemAction();
    private bool TryHandleHoveredItemDropInput() => HoveredItemActionController.TryHandleHoveredItemDropInput();
    private bool TryMoveItemToGrid(InventoryGrid sourceGrid, InventoryItem item, InventoryGrid targetGrid, bool allowStackMerge) => ItemPlacementService.TryMoveItemToGrid(sourceGrid, item, targetGrid, allowStackMerge);

    private void HandleHighlight() => HoverInfoController.HandleHighlight(_selectedItemGrid, _dragState.SelectedItem, DragController.GetTileGridPosition(), DragController.TryGetStackMergeTarget);

    private InventoryItem CreateItem(ItemData itemData, int amount) => ItemFactory.Create(itemData, amount, null);

    private void RegisterInventoryItem(InventoryItem item) => _itemRegistry.Register(item);

    private void RegisterExistingInventoryItems()
    {
        for (int i = 0; i < _initialInventoryItems.Count; i++)
        {
            RegisterInventoryItem(_initialInventoryItems[i]);
        }
    }

    private void HandleInventoryItemDurabilityChanged(InventoryItem item)
    {
        RefreshWeightState();
        HoverInfoController.RefreshHighlightedItemInfo(item);
    }

    private bool TryInstallWeaponModule(InventoryGrid grid, InventoryItem weaponItem, ItemData moduleItemData) => _weaponModuleService != null && _weaponModuleService.TryInstall(grid, weaponItem, moduleItemData);

    private bool TryAttachContextMenuModule(InventoryGrid sourceGrid, InventoryItem moduleItem, EquipmentSlotGrid targetGrid)
    {
        InventoryItem weaponItem = targetGrid == null ? null : targetGrid.EquippedItem;

        if (sourceGrid == null ||
            moduleItem == null ||
            moduleItem.ItemData == null ||
            moduleItem.ItemData.ItemType != ItemType.Module ||
            weaponItem == null ||
            WeaponModuleSupport.CanInstall(weaponItem, moduleItem.ItemData) == false)
        {
            return false;
        }

        Vector2Int sourcePosition = new(moduleItem.GridPositionX, moduleItem.GridPositionY);
        bool sourceRotated = moduleItem.IsRotated;

        if (TryDetachItemFromGrid(sourceGrid, moduleItem) == false)
        {
            return false;
        }

        if (_weaponModuleService != null && _weaponModuleService.TryInstall(targetGrid, weaponItem, moduleItem.ItemData))
        {
            DestroyInventoryItem(moduleItem);
            HandleWeaponModulesChanged(weaponItem);
            return true;
        }

        RestoreContextMenuModuleItem(sourceGrid, moduleItem, sourcePosition, sourceRotated);
        return false;
    }

    private void RestoreContextMenuModuleItem(InventoryGrid sourceGrid, InventoryItem moduleItem, Vector2Int sourcePosition, bool sourceRotated)
    {
        moduleItem.SetRotated(sourceRotated);

        if (sourceGrid.CanPlaceItem(moduleItem, sourcePosition.x, sourcePosition.y))
        {
            sourceGrid.PlaceItem(moduleItem, sourcePosition.x, sourcePosition.y);
            return;
        }

        if (InsertItem(moduleItem, _defaultItemGrid) == false)
        {
            Debug.LogError($"[{nameof(InventoryController)}] Failed to restore {moduleItem.name} after a context-menu module installation rollback.");
        }
    }

    private void HandleWeaponModulesChanged(InventoryItem weaponItem)
    {
        RefreshWeightState();
        HoverInfoController.RefreshHighlightedItem(weaponItem);

        if (_firstPersonWeaponHolderController != null && _firstPersonWeaponHolderController.CurrentWeaponItem == weaponItem)
        {
            _firstPersonWeaponHolderController.SetWeapon(weaponItem);
        }
    }

    private void RefreshWeightState()
    {
        WeightStateController.Refresh();
        RefreshFirstPersonLegs();
        RefreshFirstPersonWeapon();
        RefreshQuickUseSlotsState();
    }

    private void RefreshFirstPersonLegs()
    {
        ItemData equippedArmor = GetFirstEquippedItemData(ItemType.Armor);

        if (_firstPersonLegsController == null)
        {
            _firstPersonWeaponHolderController?.SetEquippedArmor(equippedArmor);
            return;
        }

        _firstPersonLegsController.SetEquippedArmor(equippedArmor);
        _firstPersonWeaponHolderController?.SetEquippedArmor(equippedArmor);
    }

    private void RefreshFirstPersonWeapon()
    {
        if (_firstPersonWeaponHolderController == null)
        {
            return;
        }

        InventoryItem activeWeaponItem = GetActiveWeaponItem();

        if (activeWeaponItem != null)
        {
            _firstPersonWeaponHolderController.SetWeapon(activeWeaponItem);
            return;
        }

        _firstPersonWeaponHolderController.ClearWeapon();
    }

    private void TryHandleWeaponSlotInput()
    {
        int slotIndex = PlayerInput.GetWeaponSlotIndexPressed();

        if (slotIndex < 0)
        {
            return;
        }

        if (_firstPersonWeaponHolderController != null && _firstPersonWeaponHolderController.IsWeaponChangeLocked)
        {
            return;
        }

        if (IsHandledWeaponSlotIndex(slotIndex))
        {
            _activeWeaponSlotIndex = slotIndex;
            RefreshFirstPersonWeapon();
        }
    }

    private void RefreshQuickUseSlotsState()
    {
        for (int i = 0; i < _quickUseSlotBindings.Count; i++)
        {
            QuickUseSlotBinding binding = _quickUseSlotBindings[i];

            if (binding == null)
            {
                continue;
            }

            string keyText = PlayerInput.GetInventoryQuickUseSlotDisplayName(binding.SlotIndex);
            InventoryItem item = QuickUseService.GetSlotItem(binding.SlotIndex);

            if (_quickUseHudSlotViewModels.TryGetValue(binding.SlotIndex, out QuickUseHudSlotViewModel viewModel))
            {
                viewModel.SetKeyText(keyText);
                viewModel.SetItem(item);
            }

            RefreshQuickUseInventorySlot(binding, keyText);
        }
    }

    private void RefreshQuickUseKeyTexts()
    {
        foreach (KeyValuePair<int, QuickUseInventorySlotViewModel> slotViewModel in _quickUseInventorySlotViewModels)
        {
            slotViewModel.Value.SetKeyText(PlayerInput.GetInventoryQuickUseSlotDisplayName(slotViewModel.Key));
        }

        foreach (KeyValuePair<int, QuickUseHudSlotViewModel> slotViewModel in _quickUseHudSlotViewModels)
        {
            slotViewModel.Value.SetKeyText(PlayerInput.GetInventoryQuickUseSlotDisplayName(slotViewModel.Key));
        }
    }

    private void BindQuickUseSlots()
    {
        DisposeQuickUseSlots();

        for (int i = 0; i < _quickUseSlotBindings.Count; i++)
        {
            QuickUseSlotBinding binding = _quickUseSlotBindings[i];

            if (binding == null)
            {
                continue;
            }

            string keyText = PlayerInput.GetInventoryQuickUseSlotDisplayName(binding.SlotIndex);
            BindQuickUseInventorySlot(binding, keyText);
            BindQuickUseHudSlot(binding, keyText);
        }
    }

    private void BindQuickUseInventorySlot(QuickUseSlotBinding binding, string keyText)
    {
        if (binding.InventorySlotView == null || _quickUseInventorySlotViewModels.ContainsKey(binding.SlotIndex))
        {
            return;
        }

        QuickUseInventorySlotViewModel viewModel = InventoryViewModelFactory.CreateQuickUseInventorySlot();
        viewModel.SetKeyText(keyText);
        binding.InventorySlotView.Bind(viewModel);
        binding.InventorySlotView.BringKeyTextToFront();
        _quickUseInventorySlotViewModels.Add(binding.SlotIndex, viewModel);
    }

    private void BindQuickUseHudSlot(QuickUseSlotBinding binding, string keyText)
    {
        if (binding.HudSlotView == null || _quickUseHudSlotViewModels.ContainsKey(binding.SlotIndex))
        {
            return;
        }

        QuickUseHudSlotViewModel viewModel = InventoryViewModelFactory.CreateQuickUseHudSlot();
        viewModel.SetKeyText(keyText);
        binding.HudSlotView.Bind(viewModel);
        _quickUseHudSlotViewModels.Add(binding.SlotIndex, viewModel);
    }

    private void RefreshQuickUseInventorySlot(QuickUseSlotBinding binding, string keyText)
    {
        if (binding.InventorySlotView == null || _quickUseInventorySlotViewModels.TryGetValue(binding.SlotIndex, out QuickUseInventorySlotViewModel viewModel) == false)
        {
            return;
        }

        viewModel.SetKeyText(keyText);
        binding.InventorySlotView.BringKeyTextToFront();
    }

    private void DisposeQuickUseSlots()
    {
        for (int i = 0; i < _quickUseSlotBindings.Count; i++)
        {
            _quickUseSlotBindings[i]?.InventorySlotView?.Unbind();
            _quickUseSlotBindings[i]?.HudSlotView?.Unbind();
        }

        foreach (QuickUseInventorySlotViewModel viewModel in _quickUseInventorySlotViewModels.Values)
        {
            viewModel.Dispose();
        }

        foreach (QuickUseHudSlotViewModel viewModel in _quickUseHudSlotViewModels.Values)
        {
            viewModel.Dispose();
        }

        _quickUseInventorySlotViewModels.Clear();
        _quickUseHudSlotViewModels.Clear();
    }

    private void TryHandleQuickUseInput()
    {
        int slotIndex = PlayerInput.GetInventoryQuickUseSlotIndexPressed();

        if (slotIndex < 0 || _dragState.HasSelectedItem || IconPrewarmController.IconsReady == false)
        {
            return;
        }

        if (QuickUseService.TryUseSlot(slotIndex, out ItemData usedItemData))
        {
            ShowMiniActionText(usedItemData);
        }
    }

    public void ShowMiniActionText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _miniActionTextViewModel == null)
        {
            return;
        }

        CancelMiniActionText();
        _miniActionTextCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ShowMiniActionTextAsync(text, _miniActionTextCancellation.Token).Forget(Debug.LogException);
    }

    private void ShowMiniActionText(ItemData itemData)
    {
        if (itemData == null)
        {
            return;
        }

        ShowMiniActionText($"Использовано: {itemData.ItemName}");
    }

    private async UniTask ShowMiniActionTextAsync(string text, CancellationToken cancellationToken)
    {
        _miniActionTextViewModel.Show(text);

        if (_miniActionTextDuration <= 0f)
        {
            _miniActionTextViewModel.Hide();
            return;
        }

        bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(_miniActionTextDuration), cancellationToken: cancellationToken).SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        _miniActionTextViewModel.Hide();
    }

    private void CancelMiniActionText()
    {
        if (_miniActionTextCancellation == null)
        {
            return;
        }

        _miniActionTextCancellation.Cancel();
        _miniActionTextCancellation.Dispose();
        _miniActionTextCancellation = null;
    }

    private void SetWeightViewModelState(float currentCarryWeight, float baseMaxCarryWeight, float maxCarryWeight, float movementBlockWeight, bool isMovementBlockedByWeight) => _viewModel?.SetWeightState(currentCarryWeight, baseMaxCarryWeight, maxCarryWeight, movementBlockWeight, isMovementBlockedByWeight);
    private void RenderCharacterStatsInfo(CharacterStatBlock stats, bool hideRootWhenEmpty, bool showAllStats)
    {
        if (_playerStatsInfoPanel == null)
        {
            return;
        }

        _playerStatsInfoPanel.RenderCharacterStats(stats, _settings.StatCurrentValueColor, hideRootWhenEmpty, showAllStats);
    }

    private bool TryPrepareSlotRestrictionsForPlacement(InventoryGrid targetGrid, InventoryItem item) => EquipmentSlotService.TryPrepareSlotRestrictionsForPlacement(targetGrid, item);
    private bool CanDetachItemWithSlotRestrictions(InventoryGrid sourceGrid, InventoryItem item) => EquipmentSlotService.CanDetachItemWithSlotRestrictions(sourceGrid, item);
    private void ApplyWeightMovementState() => WeightStateController.ApplyMovementState();
    private void HideItemInfoPanel() => HoverInfoController.HideItemInfoPanel();
    private bool IsContextMenuOpen() => ContextMenuController.IsOpen;
    private InventoryGrid GetInsertionGrid() => _selectedItemGrid != null ? _selectedItemGrid : _defaultItemGrid;
    private void DestroyInventoryItem(InventoryItem item) => DragController.DestroyInventoryItem(item);
    private bool TryUseContextMenuItem(InventoryGrid grid, InventoryItem item)
    {
        if (QuickUseService.TryUseItem(grid, item, out ItemData usedItemData) == false)
        {
            return false;
        }

        ShowMiniActionText(usedItemData);
        return true;
    }

    private bool TryUnloadContextMenuWeapon(InventoryGrid grid, InventoryItem item)
    {
        if (item == null || item.ItemData == null || item.ItemData.WeaponData == null)
        {
            return false;
        }

        FirstPersonWeaponMagazineState magazineState = item.WeaponMagazineState;

        if (magazineState.IsJammed || magazineState.LoadedAmmoData == null || magazineState.LoadedAmmoAmount <= 0)
        {
            return false;
        }

        if (_firstPersonWeaponHolderController != null && _firstPersonWeaponHolderController.CurrentWeaponItem == item)
        {
            bool unloadedCurrentWeapon = _firstPersonWeaponHolderController.TryUnloadCurrentWeapon();

            if (unloadedCurrentWeapon)
            {
                RefreshWeightState();
            }

            return unloadedCurrentWeapon;
        }

        ItemData loadedAmmoData = magazineState.LoadedAmmoData;
        int loadedAmmoAmount = magazineState.LoadedAmmoAmount;
        magazineState.ClearLoadedAmmo();

        bool returnedAmmo = TryReturnItemToInventoryOrDrop(loadedAmmoData, loadedAmmoAmount);
        RefreshWeightState();
        return returnedAmmo;
    }

    private bool TryDetachWeaponModule(InventoryGrid grid, InventoryItem weaponItem, ItemData moduleItemData)
    {
        if (_weaponModuleService == null || _weaponModuleService.TryDetach(grid, weaponItem, moduleItemData) == false)
        {
            return false;
        }

        HandleWeaponModulesChanged(weaponItem);
        return true;
    }

    private bool CanEquipPrimaryContextMenuWeapon(InventoryGrid grid, InventoryItem item)
    {
        return IsContextMenuWeaponItem(item) && EquipmentActionService.CanEquipToSlot(grid, item, ItemType.Weapon, 0);
    }

    private bool TryEquipPrimaryContextMenuWeapon(InventoryGrid grid, InventoryItem item)
    {
        return IsContextMenuWeaponItem(item) && EquipmentActionService.TryEquipToSlot(grid, item, ItemType.Weapon, 0);
    }

    private bool CanEquipSecondaryContextMenuWeapon(InventoryGrid grid, InventoryItem item)
    {
        return IsContextMenuWeaponItem(item) && EquipmentActionService.CanEquipToSlot(grid, item, ItemType.Weapon, 1);
    }

    private bool TryEquipSecondaryContextMenuWeapon(InventoryGrid grid, InventoryItem item)
    {
        return IsContextMenuWeaponItem(item) && EquipmentActionService.TryEquipToSlot(grid, item, ItemType.Weapon, 1);
    }

    private bool CanEquipContextMenuItem(InventoryGrid grid, InventoryItem item)
    {
        return IsGenericEquipContextMenuItem(item) && CanEquipContextMenuItemWithCurrentArmor(item) && EquipmentActionService.CanEquipToSlot(grid, item, item.ItemData.ItemType, 0);
    }

    private bool TryEquipContextMenuItem(InventoryGrid grid, InventoryItem item)
    {
        return IsGenericEquipContextMenuItem(item) && CanEquipContextMenuItemWithCurrentArmor(item) && EquipmentActionService.TryEquipToSlot(grid, item, item.ItemData.ItemType, 0);
    }

    private bool CanUnequipContextMenuItem(InventoryGrid grid, InventoryItem item)
    {
        return EquipmentActionService.CanUnequip(grid, item);
    }

    private bool TryUnequipContextMenuItem(InventoryGrid grid, InventoryItem item)
    {
        return EquipmentActionService.TryUnequip(grid, item);
    }

    private bool TryDropItem(InventoryGrid grid, InventoryItem item, bool wholeStack) => DropProcessor.TryDropItem(grid, item, wholeStack);
    private bool TrySpawnDroppedWorldItem(ItemData itemData, int amount, float durabilityPercent) => TrySpawnDroppedWorldItem(itemData, amount, durabilityPercent, null);
    private bool TrySpawnDroppedWorldItem(ItemData itemData, int amount, float durabilityPercent, IReadOnlyList<ItemData> installedModules) => _dropService.TrySpawnDroppedWorldItem(itemData, amount, durabilityPercent, installedModules, CreateDropContext());
    private InventoryDropContext CreateDropContext() => new(_dropOrigin, _playerController, transform, _dropCamera, _dropForwardDistance, _dropUpOffset, _dropGroundProbeHeight, _dropGroundProbeDistance, _dropGroundOffset, _dropObstacleClearance, _dropImpulse, _dropGroundLayers, _dropObstacleLayers);

    private bool TryDetachItemFromGrid(InventoryGrid grid, InventoryItem item) => DragController.TryDetachItemFromGrid(grid, item);

    private bool TryGetItemGrid(InventoryItem item, out InventoryGrid itemGrid)
    {
        itemGrid = null;

        if (item == null)
        {
            return false;
        }

        if (TryUseItemGrid(_defaultItemGrid, item, out itemGrid))
        {
            return true;
        }

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            if (TryUseItemGrid(_equipmentSlotGrids[i], item, out itemGrid))
            {
                return true;
            }
        }

        for (int i = 0; i < _slottedItemGrids.Count; i++)
        {
            if (TryUseItemGrid(_slottedItemGrids[i], item, out itemGrid))
            {
                return true;
            }
        }

        for (int i = 0; i < _quickActionGridReferences.Count; i++)
        {
            if (TryUseItemGrid(_quickActionGridReferences[i], item, out itemGrid))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryUseItemGrid(InventoryGrid grid, InventoryItem item, out InventoryGrid itemGrid)
    {
        itemGrid = null;

        if (grid == null || item == null)
        {
            return false;
        }

        if (grid.GetItem(item.GridPositionX, item.GridPositionY) != item)
        {
            return false;
        }

        itemGrid = grid;
        return true;
    }

    private ItemData GetFirstEquippedItemData(ItemType itemType)
    {
        InventoryItem item = GetFirstEquippedItem(itemType);
        return item == null ? null : item.ItemData;
    }

    private InventoryItem GetFirstEquippedItem(ItemType itemType)
    {
        return GetEquippedItem(itemType, 0);
    }

    private InventoryItem GetEquippedItem(ItemType itemType, int itemTypeSlotIndex)
    {
        int resolvedSlotIndex = Mathf.Max(0, itemTypeSlotIndex);
        int matchedSlotIndex = 0;

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = _equipmentSlotGrids[i];

            if (grid == null || grid.AcceptedItemType != itemType)
            {
                continue;
            }

            if (matchedSlotIndex == resolvedSlotIndex)
            {
                return grid.EquippedItem;
            }

            matchedSlotIndex++;
        }

        return null;
    }

    private InventoryItem GetActiveWeaponItem()
    {
        if (_activeWeaponSlotIndex == _primaryWeaponSlotIndex)
        {
            return GetEquippedItem(ItemType.Weapon, 0);
        }

        if (_activeWeaponSlotIndex == _secondaryWeaponSlotIndex)
        {
            return GetEquippedItem(ItemType.Weapon, 1);
        }

        if (_activeWeaponSlotIndex == _pistolWeaponSlotIndex)
        {
            return GetFirstEquippedItem(ItemType.Pistol);
        }

        return null;
    }

    private bool IsHandledWeaponSlotIndex(int slotIndex)
    {
        return slotIndex == _primaryWeaponSlotIndex ||
               slotIndex == _secondaryWeaponSlotIndex ||
               slotIndex == _pistolWeaponSlotIndex;
    }

    private static bool IsContextMenuWeaponItem(InventoryItem item)
    {
        return item != null && item.ItemData != null && item.ItemData.ItemType == ItemType.Weapon;
    }

    private static bool IsGenericEquipContextMenuItem(InventoryItem item)
    {
        if (item == null || item.ItemData == null)
        {
            return false;
        }

        return item.ItemData.ItemType == ItemType.Armor ||
               item.ItemData.ItemType == ItemType.Knife ||
               item.ItemData.ItemType == ItemType.Pistol ||
               item.ItemData.ItemType == ItemType.Helmet ||
               item.ItemData.ItemType == ItemType.Detector;
    }

    private bool CanEquipContextMenuItemWithCurrentArmor(InventoryItem item)
    {
        if (item == null || item.ItemData == null || item.ItemData.ItemType != ItemType.Helmet)
        {
            return true;
        }

        ItemData equippedArmor = GetFirstEquippedItemData(ItemType.Armor);
        return equippedArmor == null || equippedArmor.CanEquipHelmet;
    }

    private void HideContextMenu() => ContextMenuController.Hide();

    private void ToggleInventory() => ToggleInventoryAsync(destroyCancellationToken).Forget(Debug.LogException);

    private async UniTask ToggleInventoryAsync(CancellationToken cancellationToken)
    {
        await _viewModel.ToggleOpenCommand.ExecuteAsync(cancellationToken);
    }

    private UniTask ToggleInventoryCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OpenStateController.Toggle();
        _viewModel.SetOpenState(OpenStateController.IsOpen);
        return UniTask.CompletedTask;
    }

    private void SetInventoryOpen(bool isOpen, bool force)
    {
        OpenStateController.SetOpen(isOpen, force);
        _viewModel?.SetOpenState(OpenStateController.IsOpen);
    }

    private void HandleInventoryClosed()
    {
        SelectedItemGrid = null;
        HoverInfoController.HideHighlight();
        HideItemInfoPanel();
        HideContextMenu();
    }

    private bool TryStashSelectedItem() => DragController.ReturnDraggedItemToOrigin();
}
