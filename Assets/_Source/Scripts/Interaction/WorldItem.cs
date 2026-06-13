using System.Collections.Generic;
using UnityEngine;

public class WorldItem : MonoBehaviour
{
    private static readonly Dictionary<Collider, WorldItem> WorldItemsByCollider = new Dictionary<Collider, WorldItem>();

    [SerializeField] private ItemData itemData;
    [SerializeField] private Rigidbody itemRigidbody;
    [SerializeField] private List<Collider> itemColliders = new List<Collider>();
    [SerializeField] [Min(1)] private int amount = 1;
    [SerializeField] [Range(0f, 100f)] private float durabilityPercent = 100f;
    [SerializeField] private bool destroyOnPickup = true;

    public ItemData ItemData => itemData;
    public Rigidbody ItemRigidbody => itemRigidbody;
    public string ItemName => itemData == null ? string.Empty : itemData.ItemName;
    public int Amount => NormalizeAmount(itemData, amount);
    public float DurabilityPercent => NormalizeDurability(itemData, durabilityPercent);
    public float TotalWeight => itemData == null ? 0f : itemData.Weight * Amount;
    public string DisplayName => Amount > 1 ? $"{ItemName} x{Amount}" : ItemName;

    public static bool TryGetByCollider(Collider itemCollider, out WorldItem worldItem)
    {
        worldItem = null;
        return itemCollider != null && WorldItemsByCollider.TryGetValue(itemCollider, out worldItem);
    }

    private void OnEnable()
    {
        RegisterColliders();
    }

    private void OnDisable()
    {
        UnregisterColliders();
    }

    public void Initialize(ItemData itemData, int amount)
    {
        Initialize(itemData, amount, itemData == null ? 100f : itemData.DefaultDurabilityPercent);
    }

    public void Initialize(ItemData itemData, int amount, float durabilityPercent)
    {
        this.itemData = itemData;
        this.amount = NormalizeAmount(itemData, amount);
        this.durabilityPercent = NormalizeDurability(itemData, durabilityPercent);
    }

    public bool TryPickUp(InventoryController inventoryController)
    {
        if (inventoryController == null || itemData == null)
        {
            return false;
        }

        if (inventoryController.TryInsertItem(itemData, Amount, DurabilityPercent) == false)
        {
            return false;
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        return true;
    }

    [ContextMenu("Setup Item Data World Item")]
    private void SetupItemDataWorldItem()
    {
        itemData.SetWorldPrefab(this);
    }

    private void OnValidate()
    {
        amount = NormalizeAmount(itemData, amount);
        durabilityPercent = NormalizeDurability(itemData, durabilityPercent);
    }

    private void RegisterColliders()
    {
        for (int i = 0; i < itemColliders.Count; i++)
        {
            Collider itemCollider = itemColliders[i];
            if (itemCollider != null)
            {
                WorldItemsByCollider[itemCollider] = this;
            }
        }
    }

    private void UnregisterColliders()
    {
        for (int i = 0; i < itemColliders.Count; i++)
        {
            Collider itemCollider = itemColliders[i];
            if (itemCollider != null &&
                WorldItemsByCollider.TryGetValue(itemCollider, out WorldItem registeredWorldItem) &&
                registeredWorldItem == this)
            {
                WorldItemsByCollider.Remove(itemCollider);
            }
        }
    }

    private static int NormalizeAmount(ItemData itemData, int amount)
    {
        return itemData != null && itemData.IsStackable ? Mathf.Max(1, amount) : 1;
    }

    private static float NormalizeDurability(ItemData itemData, float durabilityPercent)
    {
        return itemData != null && itemData.HasDurability
            ? ItemData.NormalizeDurability(durabilityPercent)
            : 100f;
    }
}
