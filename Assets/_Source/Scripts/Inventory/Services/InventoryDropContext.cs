using UnityEngine;

internal readonly struct InventoryDropContext
{
    public InventoryDropContext(Transform dropOrigin, PlayerController playerController, Transform fallbackTransform, Camera dropCamera, float dropForwardDistance, float dropUpOffset, float dropGroundProbeHeight, float dropGroundProbeDistance, float dropGroundOffset, float dropObstacleClearance, float dropImpulse, LayerMask dropGroundLayers, LayerMask dropObstacleLayers)
    {
        DropOrigin = dropOrigin;
        PlayerController = playerController;
        FallbackTransform = fallbackTransform;
        DropCamera = dropCamera;
        DropForwardDistance = dropForwardDistance;
        DropUpOffset = dropUpOffset;
        DropGroundProbeHeight = dropGroundProbeHeight;
        DropGroundProbeDistance = dropGroundProbeDistance;
        DropGroundOffset = dropGroundOffset;
        DropObstacleClearance = dropObstacleClearance;
        DropImpulse = dropImpulse;
        DropGroundLayers = dropGroundLayers;
        DropObstacleLayers = dropObstacleLayers;
    }

    public Transform DropOrigin { get; }
    public PlayerController PlayerController { get; }
    public Transform FallbackTransform { get; }
    public Camera DropCamera { get; }
    public float DropForwardDistance { get; }
    public float DropUpOffset { get; }
    public float DropGroundProbeHeight { get; }
    public float DropGroundProbeDistance { get; }
    public float DropGroundOffset { get; }
    public float DropObstacleClearance { get; }
    public float DropImpulse { get; }
    public LayerMask DropGroundLayers { get; }
    public LayerMask DropObstacleLayers { get; }
}