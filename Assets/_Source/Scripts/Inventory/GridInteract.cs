using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private InventoryGrid inventoryGrid;
    [SerializeField] private RectTransform interactRectTransform;

    internal void Initialize(InventoryController inventoryController, InventoryGrid inventoryGrid)
    {
        this.inventoryController = inventoryController;
        this.inventoryGrid = inventoryGrid;
        interactRectTransform = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventoryController == null || inventoryGrid == null) { return; }

        inventoryController.SelectedItemGrid = inventoryGrid;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventoryController == null || inventoryGrid == null)
        {
            return;
        }

        if (interactRectTransform != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                interactRectTransform,
                eventData.position,
                eventData.enterEventCamera))
        {
            return;
        }

        if (inventoryController.SelectedItemGrid != inventoryGrid)
        {
            return;
        }

        inventoryController.SelectedItemGrid = null;
    }
}
