using UnityEngine;

internal readonly struct IconRenderProfile
{
    public readonly bool UseSlotSettings;
    public readonly int CellWidth;
    public readonly int CellHeight;
    public readonly int TextureWidth;
    public readonly int TextureHeight;
    public readonly Vector3 ModelEulerAngles;
    public readonly Vector3 ModelScale;
    public readonly Vector3 CameraEulerAngles;
    public readonly float Padding;
    public readonly bool UseDirectionalLight;
    public readonly Vector3 LightEulerAngles;
    public readonly float LightIntensity;

    private IconRenderProfile(bool useSlotSettings, int cellWidth, int cellHeight, int textureWidth, int textureHeight, Vector3 modelEulerAngles, Vector3 modelScale, Vector3 cameraEulerAngles, float padding, bool useDirectionalLight, Vector3 lightEulerAngles, float lightIntensity)
    {
        UseSlotSettings = useSlotSettings;
        CellWidth = Mathf.Max(1, cellWidth);
        CellHeight = Mathf.Max(1, cellHeight);
        TextureWidth = Mathf.Max(1, textureWidth);
        TextureHeight = Mathf.Max(1, textureHeight);
        ModelEulerAngles = modelEulerAngles;
        ModelScale = modelScale == Vector3.zero ? Vector3.one : modelScale;
        CameraEulerAngles = cameraEulerAngles;
        Padding = Mathf.Max(1f, padding);
        UseDirectionalLight = useDirectionalLight;
        LightEulerAngles = lightEulerAngles;
        LightIntensity = Mathf.Max(0f, lightIntensity);
    }

    public static IconRenderProfile CreateDefault(ItemData itemData) => new(false, itemData.Width, itemData.Height, itemData.IconTextureWidth, itemData.IconTextureHeight, itemData.IconModelEulerAngles, itemData.IconModelScale, itemData.IconCameraEulerAngles, itemData.IconPadding, itemData.IconUseDirectionalLight, itemData.IconLightEulerAngles, itemData.IconLightIntensity);
    public static IconRenderProfile CreateSlot(ItemData itemData, int slotWidth, int slotHeight)
    {
        int cellWidth = Mathf.Max(1, slotWidth);
        int cellHeight = Mathf.Max(1, slotHeight);

        return new(true, cellWidth, cellHeight, cellWidth * itemData.IconPixelsPerCell * itemData.IconRenderScale, cellHeight * itemData.IconPixelsPerCell * itemData.IconRenderScale, itemData.SlotIconModelEulerAngles, itemData.SlotIconModelScale, itemData.SlotIconCameraEulerAngles, itemData.SlotIconPadding, itemData.SlotIconUseDirectionalLight, itemData.SlotIconLightEulerAngles, itemData.SlotIconLightIntensity);
    }
}