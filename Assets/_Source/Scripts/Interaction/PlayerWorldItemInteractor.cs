using UnityEngine;

public class PlayerWorldItemInteractor : MonoBehaviour
{
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] [Min(0.1f)] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private WorldItemTooltipView tooltipView;

    [Inject] private IPlayerInput playerInput = null;

    private IPlayerInput fallbackPlayerInput;
    private WorldItem hoveredWorldItem;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (playerInput != null)
            {
                return playerInput;
            }

            fallbackPlayerInput ??= new LegacyPlayerInput();
            return fallbackPlayerInput;
        }
    }

    private void Update()
    {
        if (inventoryController != null && inventoryController.IsOpen)
        {
            SetHoveredWorldItem(null);
            return;
        }

        SetHoveredWorldItem(FindHoveredWorldItem());

        if (PlayerInput.IsInteractPressed() == false)
        {
            return;
        }

        TryInteract();
    }

    private bool TryInteract()
    {
        WorldItem worldItem = hoveredWorldItem != null ? hoveredWorldItem : FindHoveredWorldItem();
        if (worldItem == null)
        {
            return false;
        }

        if (worldItem.TryPickUp(inventoryController) == false)
        {
            return false;
        }

        SetHoveredWorldItem(null);
        return true;
    }

    private WorldItem FindHoveredWorldItem()
    {
        Ray ray = raycastCamera != null
            ? new Ray(raycastCamera.transform.position, raycastCamera.transform.forward)
            : new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, triggerInteraction) == false)
        {
            return null;
        }

        return WorldItem.TryGetByCollider(hit.collider, out WorldItem worldItem) ? worldItem : null;
    }

    private void SetHoveredWorldItem(WorldItem worldItem)
    {
        if (hoveredWorldItem == worldItem)
        {
            return;
        }

        hoveredWorldItem = worldItem;
        RefreshTooltip();
    }

    private void RefreshTooltip()
    {
        if (tooltipView == null && hoveredWorldItem != null)
        {
            return;
        }

        if (tooltipView == null)
        {
            return;
        }

        if (hoveredWorldItem == null)
        {
            tooltipView.Hide();
            return;
        }

        tooltipView.Show(hoveredWorldItem, PlayerInput.InteractKeyDisplayName);
    }

    private void OnDisable()
    {
        SetHoveredWorldItem(null);
    }
}
