using UnityEngine;

public enum ItemIconProfileType
{
    Default = 0,
    Slot = 1
}

public readonly struct IconRenderProfile
{
    public readonly bool UseSlotSettings;
    public readonly int CellWidth;
    public readonly int CellHeight;
    public readonly int RenderScale;
    public readonly int TextureWidth;
    public readonly int TextureHeight;
    public readonly float SpritePixelsPerUnit;
    public readonly Vector3 ModelEulerAngles;
    public readonly Vector3 ModelScale;
    public readonly Vector3 CameraEulerAngles;
    public readonly float Padding;
    public readonly bool UseDirectionalLight;
    public readonly float LightIntensity;

    public ItemIconProfileType ProfileType => UseSlotSettings ? ItemIconProfileType.Slot : ItemIconProfileType.Default;

    private IconRenderProfile(bool useSlotSettings, int cellWidth, int cellHeight, int renderScale, int textureWidth, int textureHeight, float spritePixelsPerUnit, Vector3 modelEulerAngles, Vector3 modelScale, Vector3 cameraEulerAngles, float padding, bool useDirectionalLight, float lightIntensity)
    {
        UseSlotSettings = useSlotSettings;
        CellWidth = Mathf.Max(1, cellWidth);
        CellHeight = Mathf.Max(1, cellHeight);
        RenderScale = Mathf.Clamp(renderScale, 1, 4);
        TextureWidth = Mathf.Max(1, textureWidth);
        TextureHeight = Mathf.Max(1, textureHeight);
        SpritePixelsPerUnit = Mathf.Max(1f, spritePixelsPerUnit);
        ModelEulerAngles = modelEulerAngles;
        ModelScale = modelScale == Vector3.zero ? Vector3.one : modelScale;
        CameraEulerAngles = cameraEulerAngles;
        Padding = Mathf.Max(1f, padding);
        UseDirectionalLight = useDirectionalLight;
        LightIntensity = Mathf.Max(0f, lightIntensity);
    }

    public static IconRenderProfile CreateDefault(ItemData itemData) => CreateDefault(itemData, itemData.Width, itemData.Height, ItemIconGeneratorSettings.LoadDefault());
    public static IconRenderProfile CreateDefault(ItemData itemData, ItemIconGeneratorSettings settings) => CreateDefault(itemData, itemData.Width, itemData.Height, settings);
    public static IconRenderProfile CreateDefault(ItemData itemData, int width, int height) => CreateDefault(itemData, width, height, ItemIconGeneratorSettings.LoadDefault());
    public static IconRenderProfile CreateDefault(ItemData itemData, int width, int height, ItemIconGeneratorSettings settings)
    {
        int cellWidth = Mathf.Max(1, width);
        int cellHeight = Mathf.Max(1, height);
        int renderScale = ResolveRenderScale(settings);

        return new(false, cellWidth, cellHeight, renderScale, cellWidth * itemData.IconPixelsPerCell * renderScale, cellHeight * itemData.IconPixelsPerCell * renderScale, itemData.GetIconSpritePixelsPerUnit(renderScale), itemData.IconModelEulerAngles, itemData.IconModelScale, itemData.IconCameraEulerAngles, itemData.IconPadding, itemData.IconUseDirectionalLight, itemData.IconLightIntensity);
    }
    public static IconRenderProfile CreateSlot(ItemData itemData, int slotWidth, int slotHeight) => CreateSlot(itemData, slotWidth, slotHeight, ItemIconGeneratorSettings.LoadDefault());
    public static IconRenderProfile CreateSlot(ItemData itemData, int slotWidth, int slotHeight, ItemIconGeneratorSettings settings)
    {
        int cellWidth = Mathf.Max(1, slotWidth);
        int cellHeight = Mathf.Max(1, slotHeight);
        int renderScale = ResolveRenderScale(settings);

        return new(true, cellWidth, cellHeight, renderScale, cellWidth * itemData.IconPixelsPerCell * renderScale, cellHeight * itemData.IconPixelsPerCell * renderScale, itemData.GetIconSpritePixelsPerUnit(renderScale), itemData.SlotIconModelEulerAngles, itemData.SlotIconModelScale, itemData.SlotIconCameraEulerAngles, itemData.SlotIconPadding, itemData.SlotIconUseDirectionalLight, itemData.SlotIconLightIntensity);
    }

    private static int ResolveRenderScale(ItemIconGeneratorSettings settings)
    {
        return (settings == null ? ItemIconGeneratorSettings.LoadDefault() : settings).IconRenderScale;
    }
}
