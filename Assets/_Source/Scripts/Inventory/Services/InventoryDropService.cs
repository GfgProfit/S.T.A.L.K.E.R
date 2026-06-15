using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

internal sealed class InventoryDropService
{
    public bool TrySpawnDroppedWorldItem(ItemData itemData, int amount, float durabilityPercent, InventoryDropContext context)
    {
        if (itemData == null)
        {
            return false;
        }

        WorldItem worldItemPrefab = itemData.WorldItemPrefab;

        if (worldItemPrefab == null)
        {
            return false;
        }

        Vector3 dropPosition = CalculateDropPosition(context);
        Vector3 dropForward = GetDropForwardDirection(context);
        Quaternion dropRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        WorldItem worldItem = Object.Instantiate(worldItemPrefab, dropPosition, dropRotation);

        worldItem.Initialize(itemData, amount, durabilityPercent);
        ApplyDropImpulse(worldItem.ItemRigidbody, dropForward, context.DropImpulse);
        return true;
    }

    private static Vector3 CalculateDropPosition(InventoryDropContext context)
    {
        Transform origin = GetDropOrigin(context);
        Vector3 dropForward = GetDropForwardDirection(context);
        Vector3 rayStart = origin.position + Vector3.up * context.DropUpOffset;
        Vector3 position = rayStart + dropForward * context.DropForwardDistance;

        if (Physics.Raycast(rayStart, dropForward, out RaycastHit obstacleHit, context.DropForwardDistance, context.DropObstacleLayers, QueryTriggerInteraction.Ignore))
        {
            position = obstacleHit.point - dropForward * context.DropObstacleClearance;
        }

        rayStart = position + Vector3.up * context.DropGroundProbeHeight;
        float rayDistance = context.DropGroundProbeHeight + context.DropGroundProbeDistance;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, context.DropGroundLayers, QueryTriggerInteraction.Ignore))
        {
            position = hit.point + Vector3.up * context.DropGroundOffset;
        }

        return position;
    }

    private static Transform GetDropOrigin(InventoryDropContext context)
    {
        if (context.DropOrigin != null)
        {
            return context.DropOrigin;
        }

        if (context.PlayerController != null)
        {
            return context.PlayerController.transform;
        }

        return context.FallbackTransform;
    }

    private static Vector3 GetDropForwardDirection(InventoryDropContext context)
    {
        Transform forwardSource = context.DropCamera != null ? context.DropCamera.transform : context.DropOrigin != null ? context.DropOrigin : context.PlayerController != null ? context.PlayerController.transform : context.FallbackTransform;

        Vector3 forward = forwardSource.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = forwardSource.forward;
        }

        return forward.normalized;
    }

    private static void ApplyDropImpulse(Rigidbody rigidbody, Vector3 dropForward, float dropImpulse)
    {
        if (dropImpulse <= 0f || rigidbody == null)
        {
            return;
        }

        Vector3 impulseDirection = (dropForward + Vector3.up * 0.25f).normalized;
        rigidbody.AddForce(impulseDirection * dropImpulse, ForceMode.VelocityChange);
    }
}