using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldItem : MonoBehaviour
{
    private static readonly Dictionary<Collider, WorldItem> WorldItemsByCollider = new();

    [SerializeField] private ItemData _itemData;
    [SerializeField] private Rigidbody _itemRigidbody;
    [SerializeField] private List<Collider> _itemColliders = new();
    [SerializeField] [Min(1)] private int _amount = 1;
    [SerializeField] [Range(0f, 100f)] private float _durabilityPercent = 100f;
    [SerializeField] private List<ItemData> _installedModules = new();
    [SerializeField] private FirstPersonWeaponMagazineState _weaponMagazineState = new();
    [SerializeField] private bool _destroyOnPickup = true;

    public ItemData ItemData => _itemData;
    public Rigidbody ItemRigidbody => _itemRigidbody;
    public string ItemName => _itemData == null ? string.Empty : _itemData.ItemName;
    public int Amount => NormalizeAmount(_itemData, _amount);
    public float DurabilityPercent => NormalizeDurability(_itemData, _durabilityPercent);
    public IReadOnlyList<ItemData> InstalledModules => _installedModules;
    public FirstPersonWeaponMagazineState WeaponMagazineState => GetWeaponMagazineState();
    public float TotalWeight => _itemData == null ? 0f : _itemData.Weight * Amount + GetInstalledModulesWeight() + GetLoadedMagazineWeight();
    public string DisplayName => Amount > 1 ? $"{ItemName} x{Amount}" : ItemName;

    public static bool TryGetByCollider(Collider itemCollider, out WorldItem worldItem)
    {
        worldItem = null;
        return itemCollider != null && WorldItemsByCollider.TryGetValue(itemCollider, out worldItem);
    }

    private void OnEnable()
    {
        RegisterColliders();
        GetWeaponMagazineState().NormalizeForWeapon(_itemData, _installedModules);
        ApplyInstalledModules();
    }
    private void OnDisable() => UnregisterColliders();

    public void Initialize(ItemData itemData, int amount) => Initialize(itemData, amount, itemData == null ? 100f : itemData.DefaultDurabilityPercent, null, null);

    public void Initialize(ItemData itemData, int amount, float durabilityPercent, IReadOnlyList<ItemData> installedModules = null, FirstPersonWeaponMagazineState weaponMagazineState = null)
    {
        _itemData = itemData;
        _amount = NormalizeAmount(itemData, amount);
        _durabilityPercent = NormalizeDurability(itemData, durabilityPercent);
        SetInstalledModules(installedModules);
        SetWeaponMagazineState(weaponMagazineState);
        ApplyInstalledModules();
    }

    public bool TryPickUp(InventoryController inventoryController)
    {
        if (inventoryController == null || _itemData == null)
        {
            return false;
        }

        if (inventoryController.TryInsertItem(_itemData, Amount, DurabilityPercent, _installedModules, WeaponMagazineState) == false)
        {
            return false;
        }

        if (_destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        return true;
    }

    private void OnValidate()
    {
        _amount = NormalizeAmount(_itemData, _amount);
        _durabilityPercent = NormalizeDurability(_itemData, _durabilityPercent);
        NormalizeInstalledModules();
        GetWeaponMagazineState().NormalizeForWeapon(_itemData, _installedModules);
    }

    private void RegisterColliders()
    {
        for (int i = 0; i < _itemColliders.Count; i++)
        {
            Collider itemCollider = _itemColliders[i];

            if (itemCollider != null)
            {
                WorldItemsByCollider[itemCollider] = this;
            }
        }
    }

    private void UnregisterColliders()
    {
        for (int i = 0; i < _itemColliders.Count; i++)
        {
            Collider itemCollider = _itemColliders[i];

            if (itemCollider != null && WorldItemsByCollider.TryGetValue(itemCollider, out WorldItem registeredWorldItem) && registeredWorldItem == this)
            {
                WorldItemsByCollider.Remove(itemCollider);
            }
        }
    }

    private static int NormalizeAmount(ItemData itemData, int amount) => itemData != null && itemData.IsStackable ? Mathf.Max(1, amount) : 1;
    private static float NormalizeDurability(ItemData itemData, float durabilityPercent) => itemData != null && itemData.HasDurability ? ItemData.NormalizeDurability(durabilityPercent) : 100f;

    private void SetInstalledModules(IReadOnlyList<ItemData> installedModules)
    {
        _installedModules.Clear();

        if (installedModules == null)
        {
            return;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData module = installedModules[i];

            if (module != null && module.ItemType == ItemType.Module && _installedModules.Contains(module) == false)
            {
                _installedModules.Add(module);
            }
        }
    }

    private void NormalizeInstalledModules()
    {
        for (int i = _installedModules.Count - 1; i >= 0; i--)
        {
            ItemData module = _installedModules[i];

            if (module == null || module.ItemType != ItemType.Module || _installedModules.IndexOf(module) != i)
            {
                _installedModules.RemoveAt(i);
            }
        }
    }

    private void ApplyInstalledModules() => WeaponModuleSupport.ApplyToVisual(gameObject, _installedModules);

    private FirstPersonWeaponMagazineState GetWeaponMagazineState()
    {
        _weaponMagazineState ??= new FirstPersonWeaponMagazineState();
        return _weaponMagazineState;
    }

    private void SetWeaponMagazineState(FirstPersonWeaponMagazineState weaponMagazineState)
    {
        FirstPersonWeaponMagazineState targetState = GetWeaponMagazineState();
        targetState.CopyFrom(weaponMagazineState);
        targetState.NormalizeForWeapon(_itemData, _installedModules);
    }

    private float GetInstalledModulesWeight()
    {
        float weight = 0f;

        for (int i = 0; i < _installedModules.Count; i++)
        {
            if (_installedModules[i] != null)
            {
                weight += _installedModules[i].Weight;
            }
        }

        return weight;
    }

    private float GetLoadedMagazineWeight()
    {
        FirstPersonWeaponMagazineState magazineState = WeaponMagazineState;
        return magazineState.LoadedAmmoData == null || magazineState.LoadedAmmoAmount <= 0 ? 0f : magazineState.LoadedAmmoData.Weight * magazineState.LoadedAmmoAmount;
    }

    [Button]
    private void Setup()
    {
        _itemColliders = null;
        _itemColliders = new();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        _itemColliders = colliders.ToList();

        _itemRigidbody = null;
        _itemRigidbody = GetComponent<Rigidbody>();
    }
}
