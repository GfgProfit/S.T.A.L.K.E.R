using UnityEngine;

public class PlayerWorldItemInteractor : MonoBehaviour
{
    [SerializeField] private Camera _raycastCamera;
    [SerializeField] private InventoryController _inventoryController;
    [SerializeField] [Min(0.1f)] private float _interactDistance = 3f;
    [SerializeField] private LayerMask _interactLayers = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private WorldItemTooltipView _tooltipView;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private WorldItem _hoveredWorldItem;

    private IPlayerInteractionInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private void Update()
    {
        if (_inventoryController != null && _inventoryController.IsOpen)
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
        WorldItem worldItem = _hoveredWorldItem != null ? _hoveredWorldItem : FindHoveredWorldItem();

        if (worldItem == null)
        {
            return false;
        }

        if (worldItem.TryPickUp(_inventoryController) == false)
        {
            return false;
        }

        SetHoveredWorldItem(null);
        return true;
    }

    private WorldItem FindHoveredWorldItem()
    {
        if (Physics.Raycast(_raycastCamera.transform.position, _raycastCamera.transform.forward, out RaycastHit hit, _interactDistance, _interactLayers, _triggerInteraction) == false)
        {
            return null;
        }

        return WorldItem.TryGetByCollider(hit.collider, out WorldItem worldItem) ? worldItem : null;
    }

    private void SetHoveredWorldItem(WorldItem worldItem)
    {
        if (_hoveredWorldItem == worldItem)
        {
            return;
        }

        _hoveredWorldItem = worldItem;
        RefreshTooltip();
    }

    private void RefreshTooltip()
    {
        if (_tooltipView == null && _hoveredWorldItem != null)
        {
            return;
        }

        if (_tooltipView == null)
        {
            return;
        }

        if (_hoveredWorldItem == null)
        {
            _tooltipView.Hide();
            return;
        }

        _tooltipView.Show(_hoveredWorldItem, PlayerInput.InteractKeyDisplayName);
    }

    private void OnDisable() => SetHoveredWorldItem(null);
}