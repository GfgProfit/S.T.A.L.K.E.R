using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private List<ItemData> _items;
    [SerializeField] private InventoryItem _itemPrefab;
    [SerializeField] private Transform _canvasTransform;
    [SerializeField] private GameObject _inventoryRoot;
    [SerializeField] private CanvasGroup _inventoryCanvasGroup;
    [SerializeField] private InventoryGrid _defaultItemGrid;
    [SerializeField] private InventoryHighlight _inventoryHighlight;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerCharacterStats _playerStats;

    [Header("Drop Settings")]
    [SerializeField] private Transform _dropOrigin;
    [SerializeField] private Camera _dropCamera;
    [SerializeField] [Min(0f)] private float _dropForwardDistance = 1.2f;
    [SerializeField] [Min(0f)] private float _dropUpOffset = 0.45f;
    [SerializeField] [Min(0f)] private float _dropGroundProbeHeight = 1.5f;
    [SerializeField] [Min(0f)] private float _dropGroundProbeDistance = 3f;
    [SerializeField] [Min(0f)] private float _dropGroundOffset = 0.08f;
    [SerializeField] [Min(0f)] private float _dropObstacleClearance = 0.2f;
    [SerializeField] [Min(0f)] private float _dropImpulse = 1.2f;
    [SerializeField] private LayerMask _dropGroundLayers = ~0;
    [SerializeField] private LayerMask _dropObstacleLayers = ~0;
    [SerializeField] private TMP_Text _weightText;
    [SerializeField] private ItemInfoPanel _itemInfoPanel;
    [SerializeField] private InventoryItemContextMenu _itemContextMenu;
    [SerializeField] private GameObject _closedSlotPrefab;
    [SerializeField] private List<InventoryItem> _initialInventoryItems = new();
    [SerializeField] private List<InventoryGrid> _quickActionGridReferences = new();

    [Header("Quick Use")]
    [SerializeField] private List<QuickUseSlotBinding> _quickUseSlotBindings = new();
    [SerializeField] private TMP_Text _miniActionText;
    [SerializeField] [Min(0f)] private float _miniActionTextDuration = 2.5f;

    [SerializeField] private List<EquipmentSlotGrid> _equipmentSlotGrids = new();
    [SerializeField] private List<SlottedItemGrid> _slottedItemGrids = new();

    [Header("Stats Info")]
    [SerializeField] private CharacterStatsInfoPanel _playerStatsInfoPanel;
    [SerializeField] private bool _hidePlayerStatsInfoWhenEmpty = true;
    [SerializeField] [Min(0f)] private float _maxCarryWeight = 50f;
    [SerializeField] [Min(0f)] private float _movementBlockExtraWeight = 10f;
    [SerializeField] private bool _openOnStart;
    [SerializeField] private bool _unlockCursorWhileOpen = true;
    [SerializeField] private bool _disablePlayerControlsWhileOpen = true;
    [SerializeField] private bool _prewarmItemIconsOnStart = true;
    [SerializeField] private bool _logIconPrewarmProgress;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private InventoryItemFactory _itemFactory;
    private InventoryEquipmentSlotService _equipmentSlotService;
    private InventoryQuickActionService _quickActionService;
    private InventoryContextMenuController _contextMenuController;
    private InventoryItemDropProcessor _dropProcessor;
    private InventoryWeightStateController _weightStateController;
    private InventoryHoverInfoController _hoverInfoController;
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
    private InventoryQuickActionService CreateQuickActionService() => new(_quickActionGridReferences, _defaultItemGrid, TryMoveItemToGrid, CreateItem, RegisterInventoryItem, RefreshWeightState);
    private InventoryContextMenuController CreateContextMenuController() => new(_itemContextMenu, PlayerInput, () => _selectedItemGrid, () => _dragState.SelectedItem, DragController.GetTileGridPosition, HideItemInfoPanel, TryUseContextMenuItem, TryDropItem);
    private InventoryItemDropProcessor CreateDropProcessor() => new(TrySpawnDroppedWorldItem, TryDetachItemFromGrid, DestroyInventoryItem, RefreshWeightState);
    private InventoryWeightStateController CreateWeightStateController() => new(_itemRegistry, _equipmentSlotGrids, EquipmentSlotService, SetWeightViewModelState, RenderCharacterStatsInfo, _playerController, _playerStats, _hidePlayerStatsInfoWhenEmpty, _maxCarryWeight, _movementBlockExtraWeight);
    private InventoryHoverInfoController CreateHoverInfoController() => new(_inventoryHighlight, _itemInfoPanel, IsContextMenuOpen);
    private InventoryDragPlacementService CreateDragPlacementService() => new(_dragState, ItemFactory, _itemRegistry, CanDetachItemWithSlotRestrictions, TryPrepareSlotRestrictionsForPlacement, RefreshWeightState);
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
        _quickActionService = CreateQuickActionService();
        _contextMenuController = CreateContextMenuController();
        _dropProcessor = CreateDropProcessor();
        _weightStateController = CreateWeightStateController();
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
    public bool TryInsertItem(ItemData itemData, int amount, float? durabilityPercent) => ItemPlacementService.TryInsertItem(itemData, amount, durabilityPercent, IconPrewarmController.IconsReady, GetInsertionGrid(), _defaultItemGrid);
    private bool InsertItem(InventoryItem itemToInsert, InventoryGrid targetGrid) => ItemPlacementService.InsertItem(itemToInsert, targetGrid);
    private bool TryHandleQuickItemAction() => HoveredItemActionController.TryHandleQuickItemAction();
    private bool TryHandleHoveredItemDropInput() => HoveredItemActionController.TryHandleHoveredItemDropInput();
    private bool TryMoveItemToGrid(InventoryGrid sourceGrid, InventoryItem item, InventoryGrid targetGrid, bool allowStackMerge) => ItemPlacementService.TryMoveItemToGrid(sourceGrid, item, targetGrid, allowStackMerge);

    private void HandleHighlight() => HoverInfoController.HandleHighlight(_selectedItemGrid, _dragState.SelectedItem, DragController.GetTileGridPosition(), DragController.TryGetStackMergeTarget);

    private InventoryItem CreateItem(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts) => CreateItem(itemData, amount, runtimeIconParts, null);
    private InventoryItem CreateItem(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts, float? durabilityPercent) => ItemFactory.Create(itemData, amount, runtimeIconParts, durabilityPercent);

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

    private void RefreshWeightState()
    {
        WeightStateController.Refresh();
        RefreshQuickUseSlotsState();
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

    private void ShowMiniActionText(ItemData itemData)
    {
        if (itemData == null || _miniActionTextViewModel == null)
        {
            return;
        }

        CancelMiniActionText();
        _miniActionTextCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ShowMiniActionTextAsync($"\u0418\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u043e: {itemData.ItemName}", _miniActionTextCancellation.Token).Forget(Debug.LogException);
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

    private bool TryDropItem(InventoryGrid grid, InventoryItem item, bool wholeStack) => DropProcessor.TryDropItem(grid, item, wholeStack);
    private bool TrySpawnDroppedWorldItem(ItemData itemData, int amount, float durabilityPercent) => _dropService.TrySpawnDroppedWorldItem(itemData, amount, durabilityPercent, CreateDropContext());
    private InventoryDropContext CreateDropContext() => new(_dropOrigin, _playerController, transform, _dropCamera, _dropForwardDistance, _dropUpOffset, _dropGroundProbeHeight, _dropGroundProbeDistance, _dropGroundOffset, _dropObstacleClearance, _dropImpulse, _dropGroundLayers, _dropObstacleLayers);

    private bool TryDetachItemFromGrid(InventoryGrid grid, InventoryItem item) => DragController.TryDetachItemFromGrid(grid, item);

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
