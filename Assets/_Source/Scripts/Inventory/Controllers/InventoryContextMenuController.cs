using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryContextMenuController
{
    private const string DETACH_MODULE_LABEL_FORMAT = "\u041e\u0442\u0441\u043e\u0435\u0434\u0438\u043d\u0438\u0442\u044c: {0}";
    private const string ATTACH_MODULE_LABEL_FORMAT = "\u041f\u0440\u0438\u043a\u0440\u0435\u043f\u0438\u0442\u044c \u043a: {0}";

    private readonly InventoryItemContextMenu _contextMenu;
    private readonly IInventoryInput _playerInput;
    private readonly Func<InventoryGrid> _getSelectedGrid;
    private readonly Func<InventoryItem> _getSelectedItem;
    private readonly Func<Vector2Int> _getTileGridPosition;
    private readonly Action _hideItemTooltip;
    private readonly Action<InventoryItem> _showItemInfoPanel;
    private readonly Func<InventoryGrid, InventoryItem, bool> _useItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _unloadWeapon;
    private readonly Func<InventoryGrid, InventoryItem, bool> _canEquipPrimaryWeapon;
    private readonly Func<InventoryGrid, InventoryItem, bool> _equipPrimaryWeapon;
    private readonly Func<InventoryGrid, InventoryItem, bool> _canEquipSecondaryWeapon;
    private readonly Func<InventoryGrid, InventoryItem, bool> _equipSecondaryWeapon;
    private readonly Func<InventoryGrid, InventoryItem, bool> _canEquipItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _equipItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _canUnequipItem;
    private readonly Func<InventoryGrid, InventoryItem, bool> _unequipItem;
    private readonly IReadOnlyList<EquipmentSlotGrid> _equipmentSlotGrids;
    private readonly Func<InventoryGrid, InventoryItem, EquipmentSlotGrid, bool> _attachWeaponModule;
    private readonly Func<InventoryGrid, InventoryItem, ItemData, bool> _detachWeaponModule;
    private readonly Func<InventoryGrid, InventoryItem, bool, bool> _dropItem;
    private InventoryGrid _contextMenuGrid;
    private InventoryItem _contextMenuItem;

    public InventoryContextMenuController(InventoryItemContextMenu contextMenu, IInventoryInput playerInput, Func<InventoryGrid> getSelectedGrid, Func<InventoryItem> getSelectedItem, Func<Vector2Int> getTileGridPosition, Action hideItemTooltip, Action<InventoryItem> showItemInfoPanel, Func<InventoryGrid, InventoryItem, bool> useItem, Func<InventoryGrid, InventoryItem, bool> unloadWeapon, Func<InventoryGrid, InventoryItem, bool> canEquipPrimaryWeapon, Func<InventoryGrid, InventoryItem, bool> equipPrimaryWeapon, Func<InventoryGrid, InventoryItem, bool> canEquipSecondaryWeapon, Func<InventoryGrid, InventoryItem, bool> equipSecondaryWeapon, Func<InventoryGrid, InventoryItem, bool> canEquipItem, Func<InventoryGrid, InventoryItem, bool> equipItem, Func<InventoryGrid, InventoryItem, bool> canUnequipItem, Func<InventoryGrid, InventoryItem, bool> unequipItem, IReadOnlyList<EquipmentSlotGrid> equipmentSlotGrids, Func<InventoryGrid, InventoryItem, EquipmentSlotGrid, bool> attachWeaponModule, Func<InventoryGrid, InventoryItem, ItemData, bool> detachWeaponModule, Func<InventoryGrid, InventoryItem, bool, bool> dropItem)
    {
        _contextMenu = contextMenu;
        _playerInput = playerInput;
        _getSelectedGrid = getSelectedGrid;
        _getSelectedItem = getSelectedItem;
        _getTileGridPosition = getTileGridPosition;
        _hideItemTooltip = hideItemTooltip;
        _showItemInfoPanel = showItemInfoPanel;
        _useItem = useItem;
        _unloadWeapon = unloadWeapon;
        _canEquipPrimaryWeapon = canEquipPrimaryWeapon;
        _equipPrimaryWeapon = equipPrimaryWeapon;
        _canEquipSecondaryWeapon = canEquipSecondaryWeapon;
        _equipSecondaryWeapon = equipSecondaryWeapon;
        _canEquipItem = canEquipItem;
        _equipItem = equipItem;
        _canUnequipItem = canUnequipItem;
        _unequipItem = unequipItem;
        _equipmentSlotGrids = equipmentSlotGrids;
        _attachWeaponModule = attachWeaponModule;
        _detachWeaponModule = detachWeaponModule;
        _dropItem = dropItem;
    }

    public bool IsOpen => _contextMenu != null && _contextMenu.IsOpen;

    public void Initialize() => _contextMenu?.Initialize(UseContextMenuItem, InspectContextMenuItem, UnloadContextMenuWeapon, EquipContextMenuPrimaryWeapon, EquipContextMenuSecondaryWeapon, EquipContextMenuItem, UnequipContextMenuItem, DropSingleItem, DropItemStack);

    public bool HandleInput()
    {
        if (_contextMenu != null && _contextMenu.IsOpen)
        {
            if (_playerInput.IsInventoryPrimaryActionPressed())
            {
                if (_contextMenu.ContainsScreenPoint(_playerInput.GetPointerPosition()))
                {
                    return true;
                }

                Hide();

                return true;
            }

            if (_playerInput.IsInventorySecondaryActionPressed() && _contextMenu.ContainsScreenPoint(_playerInput.GetPointerPosition()))
            {
                return true;
            }
        }

        if (_playerInput.IsInventorySecondaryActionPressed())
        {
            OpenAtCursor();

            return true;
        }

        return false;
    }

    public void CloseIfPointerIsOutsideRadius()
    {
        if (_contextMenu == null || _contextMenu.ShouldCloseForPointer(_playerInput.GetPointerPosition()) == false)
        {
            return;
        }

        Hide();
    }

    public void Hide()
    {
        _contextMenu?.Hide();
        _contextMenuGrid = null;
        _contextMenuItem = null;
    }

    private void OpenAtCursor()
    {
        Hide();

        InventoryGrid selectedGrid = _getSelectedGrid();

        if (_getSelectedItem() != null || selectedGrid == null || _contextMenu == null)
        {
            return;
        }

        Vector2Int tileGridPosition = _getTileGridPosition();
        InventoryItem item = selectedGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (item == null)
        {
            return;
        }

        _contextMenuGrid = selectedGrid;
        _contextMenuItem = item;

        _hideItemTooltip();
        bool canUnloadWeapon = CanUnloadWeapon(item);
        _contextMenu.Show(CanUseItem(item), canUnloadWeapon, canUnloadWeapon, CanEquipPrimaryWeapon(selectedGrid, item), CanEquipSecondaryWeapon(selectedGrid, item), CanEquipItem(selectedGrid, item), CanUnequipItem(selectedGrid, item), CanDropStack(item), BuildModuleActions(item), _playerInput.GetPointerPosition());
    }

    private void UseContextMenuItem()
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();

        _useItem(grid, item);
    }

    private void InspectContextMenuItem()
    {
        InventoryItem item = _contextMenuItem;
        Hide();

        _showItemInfoPanel?.Invoke(item);
    }

    private void UnloadContextMenuWeapon()
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();

        _unloadWeapon(grid, item);
    }

    private void EquipContextMenuPrimaryWeapon() => EquipContextMenuItem(_equipPrimaryWeapon);
    private void EquipContextMenuSecondaryWeapon() => EquipContextMenuItem(_equipSecondaryWeapon);
    private void EquipContextMenuItem() => EquipContextMenuItem(_equipItem);
    private void UnequipContextMenuItem() => EquipContextMenuItem(_unequipItem);
    private void DropSingleItem() => DropContextMenuItem(false);
    private void DropItemStack() => DropContextMenuItem(true);
    private static bool CanUseItem(InventoryItem item) => item != null && item.ItemData != null && item.ItemData.ItemType == ItemType.Consumable;
    private static bool CanUnloadWeapon(InventoryItem item) => IsWeapon(item) &&
                                                               item.WeaponMagazineState.IsJammed == false &&
                                                               item.WeaponMagazineState.LoadedAmmoData != null &&
                                                               item.WeaponMagazineState.LoadedAmmoAmount > 0;
    private static bool CanDropStack(InventoryItem item) => item != null && item.IsStackable && item.CurrentAmount > 1;
    private static bool IsWeapon(InventoryItem item) => item != null && item.ItemData != null && item.ItemData.WeaponData != null;

    private bool CanEquipPrimaryWeapon(InventoryGrid grid, InventoryItem item) => _canEquipPrimaryWeapon != null && _canEquipPrimaryWeapon(grid, item);
    private bool CanEquipSecondaryWeapon(InventoryGrid grid, InventoryItem item) => _canEquipSecondaryWeapon != null && _canEquipSecondaryWeapon(grid, item);
    private bool CanEquipItem(InventoryGrid grid, InventoryItem item) => _canEquipItem != null && _canEquipItem(grid, item);
    private bool CanUnequipItem(InventoryGrid grid, InventoryItem item) => _canUnequipItem != null && _canUnequipItem(grid, item);

    private void EquipContextMenuItem(Func<InventoryGrid, InventoryItem, bool> equipAction)
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();

        equipAction?.Invoke(grid, item);
    }

    private void DropContextMenuItem(bool wholeStack)
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();

        _dropItem(grid, item, wholeStack);
    }

    private IReadOnlyList<InventoryContextMenuAction> BuildModuleActions(InventoryItem item)
    {
        List<InventoryContextMenuAction> actions = new();

        if (item == null)
        {
            return actions;
        }

        AddAttachModuleActions(actions, item);

        for (int i = 0; i < item.InstalledModules.Count; i++)
        {
            ItemData moduleItemData = item.InstalledModules[i];

            if (moduleItemData == null)
            {
                continue;
            }

            actions.Add(new InventoryContextMenuAction(string.Format(DETACH_MODULE_LABEL_FORMAT, moduleItemData.ItemName), () => DetachContextMenuModule(moduleItemData), WeaponModuleSupport.CanDetach(item, moduleItemData)));
        }

        return actions;
    }

    private void AddAttachModuleActions(ICollection<InventoryContextMenuAction> actions, InventoryItem moduleItem)
    {
        if (actions == null || moduleItem == null || moduleItem.ItemData == null || moduleItem.ItemData.ItemType != ItemType.Module || _equipmentSlotGrids == null)
        {
            return;
        }

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid targetGrid = _equipmentSlotGrids[i];
            InventoryItem weaponItem = targetGrid == null ? null : targetGrid.EquippedItem;

            if (weaponItem == null || WeaponModuleSupport.CanInstall(weaponItem, moduleItem.ItemData) == false)
            {
                continue;
            }

            string weaponName = string.IsNullOrWhiteSpace(weaponItem.ItemData.ShortName) ? weaponItem.ItemData.ItemName : weaponItem.ItemData.ShortName;
            actions.Add(new InventoryContextMenuAction(string.Format(ATTACH_MODULE_LABEL_FORMAT, weaponName), () => AttachContextMenuModule(targetGrid)));
        }
    }

    private void AttachContextMenuModule(EquipmentSlotGrid targetGrid)
    {
        InventoryItem moduleItem = _contextMenuItem;
        InventoryGrid sourceGrid = _contextMenuGrid;
        Hide();
        _attachWeaponModule?.Invoke(sourceGrid, moduleItem, targetGrid);
    }

    private void DetachContextMenuModule(ItemData moduleItemData)
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();
        _detachWeaponModule?.Invoke(grid, item, moduleItemData);
    }
}
