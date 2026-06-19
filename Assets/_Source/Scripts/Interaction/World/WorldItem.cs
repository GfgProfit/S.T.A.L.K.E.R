using System.Collections.Generic;
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
    [SerializeField] private bool _destroyOnPickup = true;

    public ItemData ItemData => _itemData;
    public Rigidbody ItemRigidbody => _itemRigidbody;
    public string ItemName => _itemData == null ? string.Empty : _itemData.ItemName;
    public int Amount => NormalizeAmount(_itemData, _amount);
    public float DurabilityPercent => NormalizeDurability(_itemData, _durabilityPercent);
    public IReadOnlyList<ItemData> InstalledModules => _installedModules;
    public float TotalWeight => _itemData == null ? 0f : _itemData.Weight * Amount + GetInstalledModulesWeight();
    public string DisplayName => Amount > 1 ? $"{ItemName} x{Amount}" : ItemName;

    public static bool TryGetByCollider(Collider itemCollider, out WorldItem worldItem)
    {
        worldItem = null;
        return itemCollider != null && WorldItemsByCollider.TryGetValue(itemCollider, out worldItem);
    }

    private void OnEnable()
    {
        RegisterColliders();
        ApplyInstalledModules();
    }
    private void OnDisable() => UnregisterColliders();

    public void Initialize(ItemData itemData, int amount) => Initialize(itemData, amount, itemData == null ? 100f : itemData.DefaultDurabilityPercent, null);

    public void Initialize(ItemData itemData, int amount, float durabilityPercent, IReadOnlyList<ItemData> installedModules = null)
    {
        _itemData = itemData;
        _amount = NormalizeAmount(itemData, amount);
        _durabilityPercent = NormalizeDurability(itemData, durabilityPercent);
        SetInstalledModules(installedModules);
        ApplyInstalledModules();
    }

    public bool TryPickUp(InventoryController inventoryController)
    {
        if (inventoryController == null || _itemData == null)
        {
            return false;
        }

        if (inventoryController.TryInsertItem(_itemData, Amount, DurabilityPercent, _installedModules) == false)
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
}
