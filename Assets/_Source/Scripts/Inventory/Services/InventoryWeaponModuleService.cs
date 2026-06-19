using System;
using UnityEngine;

internal sealed class InventoryWeaponModuleService
{
    private readonly Func<ItemData, int, bool> _returnModuleToInventoryOrDrop;

    public InventoryWeaponModuleService(Func<ItemData, int, bool> returnModuleToInventoryOrDrop)
    {
        _returnModuleToInventoryOrDrop = returnModuleToInventoryOrDrop;
    }

    public bool CanInstall(InventoryItem weaponItem, ItemData moduleItemData) => WeaponModuleSupport.CanInstall(weaponItem, moduleItemData);

    public bool TryInstall(InventoryGrid grid, InventoryItem weaponItem, ItemData moduleItemData)
    {
        if (grid == null || WeaponModuleSupport.CanInstall(weaponItem, moduleItemData) == false)
        {
            return false;
        }

        return TryChangeModules(grid, weaponItem, () => weaponItem.AddInstalledModule(moduleItemData), () => weaponItem.RemoveInstalledModule(moduleItemData));
    }

    public bool TryDetach(InventoryGrid grid, InventoryItem weaponItem, ItemData moduleItemData)
    {
        if (grid == null || WeaponModuleSupport.CanDetach(weaponItem, moduleItemData) == false)
        {
            return false;
        }

        if (TryChangeModules(grid, weaponItem, () => weaponItem.RemoveInstalledModule(moduleItemData), () => weaponItem.AddInstalledModule(moduleItemData)) == false)
        {
            return false;
        }

        if (_returnModuleToInventoryOrDrop != null && _returnModuleToInventoryOrDrop(moduleItemData, 1))
        {
            return true;
        }

        TryChangeModules(grid, weaponItem, () => weaponItem.AddInstalledModule(moduleItemData), () => weaponItem.RemoveInstalledModule(moduleItemData));
        return false;
    }

    private static bool TryChangeModules(InventoryGrid grid, InventoryItem weaponItem, Func<bool> applyChange, Func<bool> rollbackChange)
    {
        if (grid == null || weaponItem == null || applyChange == null || rollbackChange == null)
        {
            return false;
        }

        int positionX = weaponItem.GridPositionX;
        int positionY = weaponItem.GridPositionY;
        bool wasRotated = weaponItem.IsRotated;
        InventoryItem pickedItem = grid.PickUpItem(positionX, positionY);

        if (pickedItem != weaponItem)
        {
            if (pickedItem != null)
            {
                grid.PlaceItem(pickedItem, positionX, positionY);
            }

            return false;
        }

        if (applyChange() && grid.CanPlaceItem(weaponItem, positionX, positionY))
        {
            grid.PlaceItem(weaponItem, positionX, positionY);
            return true;
        }

        rollbackChange();
        weaponItem.SetRotated(wasRotated);

        if (grid.CanPlaceItem(weaponItem, positionX, positionY))
        {
            grid.PlaceItem(weaponItem, positionX, positionY);
        }
        else
        {
            Debug.LogError($"[{nameof(InventoryWeaponModuleService)}] Failed to restore {weaponItem.name} after a module change rollback.");
        }

        return false;
    }
}
