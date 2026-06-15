using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private InventoryController _inventoryController;
    [SerializeField] private InventoryGrid _inventoryGrid;
    [SerializeField] private RectTransform _interactRectTransform;

    internal void Initialize(InventoryController inventoryController, InventoryGrid inventoryGrid)
    {
        _inventoryController = inventoryController;
        _inventoryGrid = inventoryGrid;
        _interactRectTransform = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_inventoryController == null || _inventoryGrid == null)
        {
            return;
        }

        _inventoryController.SelectedItemGrid = _inventoryGrid;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_inventoryController == null || _inventoryGrid == null)
        {
            return;
        }

        if (_interactRectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(_interactRectTransform, eventData.position, eventData.enterEventCamera))
        {
            return;
        }

        if (_inventoryController.SelectedItemGrid != _inventoryGrid)
        {
            return;
        }

        _inventoryController.SelectedItemGrid = null;
    }
}