using UnityEngine;

public class PlayerWorldItemInteractor : MonoBehaviour
{
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] [Min(0.1f)] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Inject] private IPlayerInput playerInput = null;

    private IPlayerInput fallbackPlayerInput;

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

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }
    }

    private void Update()
    {
        if (PlayerInput.IsInteractPressed() == false)
        {
            return;
        }

        if (inventoryController != null && inventoryController.IsOpen)
        {
            return;
        }

        TryInteract();
    }

    private bool TryInteract()
    {
        Ray ray = raycastCamera != null
            ? new Ray(raycastCamera.transform.position, raycastCamera.transform.forward)
            : new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, triggerInteraction) == false)
        {
            return false;
        }

        WorldItem worldItem = hit.collider.GetComponentInParent<WorldItem>();
        if (worldItem == null)
        {
            return false;
        }

        return worldItem.TryPickUp(inventoryController);
    }
}
