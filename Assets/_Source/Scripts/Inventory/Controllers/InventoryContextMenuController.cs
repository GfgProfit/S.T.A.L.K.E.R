using System;
using UnityEngine;

internal sealed class InventoryContextMenuController
{
    private readonly InventoryItemContextMenu _contextMenu;
    private readonly IInventoryInput _playerInput;
    private readonly Func<InventoryGrid> _getSelectedGrid;
    private readonly Func<InventoryItem> _getSelectedItem;
    private readonly Func<Vector2Int> _getTileGridPosition;
    private readonly Action _hideItemInfoPanel;
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
    private readonly Func<InventoryGrid, InventoryItem, bool, bool> _dropItem;
    private InventoryGrid _contextMenuGrid;
    private InventoryItem _contextMenuItem;

    public InventoryContextMenuController(InventoryItemContextMenu contextMenu, IInventoryInput playerInput, Func<InventoryGrid> getSelectedGrid, Func<InventoryItem> getSelectedItem, Func<Vector2Int> getTileGridPosition, Action hideItemInfoPanel, Func<InventoryGrid, InventoryItem, bool> useItem, Func<InventoryGrid, InventoryItem, bool> unloadWeapon, Func<InventoryGrid, InventoryItem, bool> canEquipPrimaryWeapon, Func<InventoryGrid, InventoryItem, bool> equipPrimaryWeapon, Func<InventoryGrid, InventoryItem, bool> canEquipSecondaryWeapon, Func<InventoryGrid, InventoryItem, bool> equipSecondaryWeapon, Func<InventoryGrid, InventoryItem, bool> canEquipItem, Func<InventoryGrid, InventoryItem, bool> equipItem, Func<InventoryGrid, InventoryItem, bool> canUnequipItem, Func<InventoryGrid, InventoryItem, bool> unequipItem, Func<InventoryGrid, InventoryItem, bool, bool> dropItem)
    {
        _contextMenu = contextMenu;
        _playerInput = playerInput;
        _getSelectedGrid = getSelectedGrid;
        _getSelectedItem = getSelectedItem;
        _getTileGridPosition = getTileGridPosition;
        _hideItemInfoPanel = hideItemInfoPanel;
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
        _dropItem = dropItem;
    }

    public bool IsOpen => _contextMenu != null && _contextMenu.IsOpen;

    public void Initialize() => _contextMenu?.Initialize(UseContextMenuItem, UnloadContextMenuWeapon, EquipContextMenuPrimaryWeapon, EquipContextMenuSecondaryWeapon, EquipContextMenuItem, UnequipContextMenuItem, DropSingleItem, DropItemStack);

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

        _hideItemInfoPanel();
        bool canUnloadWeapon = CanUnloadWeapon(item);
        _contextMenu.Show(CanUseItem(item), canUnloadWeapon, canUnloadWeapon, CanEquipPrimaryWeapon(selectedGrid, item), CanEquipSecondaryWeapon(selectedGrid, item), CanEquipItem(selectedGrid, item), CanUnequipItem(selectedGrid, item), CanDropStack(item), _playerInput.GetPointerPosition());
    }

    private void UseContextMenuItem()
    {
        InventoryItem item = _contextMenuItem;
        InventoryGrid grid = _contextMenuGrid;
        Hide();

        _useItem(grid, item);
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
}
