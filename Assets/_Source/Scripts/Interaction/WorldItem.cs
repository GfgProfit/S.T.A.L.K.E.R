using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private bool destroyOnPickup = true;

    public ItemData ItemData => itemData;
    public string ItemName => itemData == null ? string.Empty : itemData.ItemName;

    public bool TryPickUp(InventoryController inventoryController)
    {
        if (inventoryController == null || itemData == null)
        {
            return false;
        }

        if (inventoryController.TryInsertItem(itemData) == false)
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
}
