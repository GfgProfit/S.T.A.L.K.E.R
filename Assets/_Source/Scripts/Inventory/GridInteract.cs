using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryController inventoryController;
    private InventoryGrid inventoryGrid;

    private void Awake()
    {
        inventoryController = FindFirstObjectByType(typeof(InventoryController)) as InventoryController;
        inventoryGrid = GetComponentInParent<InventoryGrid>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventoryGrid == null) { return; }

        inventoryController.SelectedItemGrid = inventoryGrid;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryGrid currentGrid = eventData.pointerCurrentRaycast.gameObject == null
            ? null
            : eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<InventoryGrid>();

        if (currentGrid == inventoryGrid) { return; }

        inventoryController.SelectedItemGrid = null;
    }
}
