using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FirstPersonWeaponMagazineState
{
    [SerializeField] [Min(0)] private int _requestedAmmoIndex;
    [SerializeField] private ItemData _requestedAmmoData;
    [SerializeField] private ItemData _loadedAmmoData;
    [SerializeField] [Min(0)] private int _loadedAmmoAmount;
    [SerializeField] private bool _isJammed;

    public int RequestedAmmoIndex => _requestedAmmoIndex;
    public ItemData RequestedAmmoData => _requestedAmmoData;
    public ItemData LoadedAmmoData => _loadedAmmoData;
    public int LoadedAmmoAmount => _loadedAmmoAmount;
    public bool IsJammed => _isJammed;

    public void SetRequestedAmmo(int requestedAmmoIndex, ItemData requestedAmmoData)
    {
        _requestedAmmoIndex = requestedAmmoIndex < 0 ? 0 : requestedAmmoIndex;
        _requestedAmmoData = requestedAmmoData;
    }

    public void SetLoadedAmmo(ItemData loadedAmmoData, int loadedAmmoAmount)
    {
        if (loadedAmmoData == null || loadedAmmoAmount <= 0)
        {
            ClearLoadedAmmo();
            return;
        }

        _loadedAmmoData = loadedAmmoData;
        _loadedAmmoAmount = loadedAmmoAmount;
    }

    public void ClearLoadedAmmo()
    {
        _loadedAmmoData = null;
        _loadedAmmoAmount = 0;
    }

    public void SetJammed(bool isJammed) => _isJammed = isJammed;

    public void CopyFrom(FirstPersonWeaponMagazineState source)
    {
        if (source == null)
        {
            Clear();
            return;
        }

        SetRequestedAmmo(source.RequestedAmmoIndex, source.RequestedAmmoData);
        SetLoadedAmmo(source.LoadedAmmoData, source.LoadedAmmoAmount);
        SetJammed(source.IsJammed);
    }

    public void NormalizeForWeapon(ItemData itemData, IReadOnlyList<ItemData> installedModules)
    {
        WeaponData weaponData = itemData == null ? null : itemData.WeaponData;

        if (weaponData == null)
        {
            Clear();
            return;
        }

        NormalizeRequestedAmmo(weaponData);
        NormalizeLoadedAmmo(weaponData, installedModules);
        _isJammed = _isJammed && _loadedAmmoData != null && _loadedAmmoAmount > 0;
    }

    public void Clear()
    {
        _requestedAmmoIndex = 0;
        _requestedAmmoData = null;
        ClearLoadedAmmo();
        _isJammed = false;
    }

    private void NormalizeRequestedAmmo(WeaponData weaponData)
    {
        int requestedAmmoIndex = _requestedAmmoIndex < 0 ? 0 : _requestedAmmoIndex;
        ItemData requestedAmmoData = weaponData.GetCompatibleAmmo(requestedAmmoIndex);

        if (requestedAmmoData == null && _requestedAmmoData != null)
        {
            int fallbackIndex = weaponData.GetCompatibleAmmoIndex(_requestedAmmoData);

            if (fallbackIndex >= 0)
            {
                requestedAmmoIndex = fallbackIndex;
                requestedAmmoData = _requestedAmmoData;
            }
        }

        if (requestedAmmoData == null)
        {
            requestedAmmoIndex = 0;
            requestedAmmoData = weaponData.GetCompatibleAmmo(requestedAmmoIndex);
        }

        _requestedAmmoIndex = requestedAmmoIndex;
        _requestedAmmoData = requestedAmmoData;
    }

    private void NormalizeLoadedAmmo(WeaponData weaponData, IReadOnlyList<ItemData> installedModules)
    {
        if (_loadedAmmoData == null || _loadedAmmoAmount <= 0 || weaponData.GetCompatibleAmmoIndex(_loadedAmmoData) < 0)
        {
            ClearLoadedAmmo();
            return;
        }

        int magazineCapacity = WeaponModuleSupport.GetMagazineCapacity(weaponData.MagazineCapacity, installedModules);
        _loadedAmmoAmount = Mathf.Clamp(_loadedAmmoAmount, 0, magazineCapacity);

        if (_loadedAmmoAmount <= 0)
        {
            ClearLoadedAmmo();
        }
    }
}
