using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] [Min(1)] private int amount = 1;
    [SerializeField] private bool destroyOnPickup = true;

    public ItemData ItemData => itemData;
    public string ItemName => itemData == null ? string.Empty : itemData.ItemName;
    public int Amount => itemData != null && itemData.IsStackable ? Mathf.Max(1, amount) : 1;
    public float TotalWeight => itemData == null ? 0f : itemData.Weight * Amount;
    public string DisplayName => Amount > 1 ? $"{ItemName} x{Amount}" : ItemName;

    public bool TryPickUp(InventoryController inventoryController)
    {
        if (inventoryController == null || itemData == null)
        {
            return false;
        }

        if (inventoryController.TryInsertItem(itemData, Amount) == false)
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

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }
}
