using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private string shortName;
    [SerializeField] [TextArea(3, 8)] private string description;
    [SerializeField] private ItemType itemType = ItemType.Misc;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] [Min(0f)] private float weight;
    [SerializeField] private bool stackable;
    [SerializeField] private bool useDurability;
    [SerializeField] [Range(0f, 100f)] private float defaultDurabilityPercent = 100f;
    [SerializeField] private bool canEquipHelmet = true;
    [SerializeField] private List<CharacterStatModifier> statModifiers = new List<CharacterStatModifier>();
    [SerializeField] private WorldItem worldItemPrefab;

    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;

    [SerializeField] private Sprite itemIcon;
    [SerializeField] private bool generateIconAtRuntime = true;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private List<ItemIconPart> iconParts = new List<ItemIconPart>();
    [SerializeField] [Min(16)] private int iconPixelsPerCell = 64;
    [SerializeField] [Range(1, 4)] private int iconRenderScale = 2;
    [SerializeField] [Range(1f, 2f)] private float iconPadding = 1.15f;
    [SerializeField] [Range(1, 8)] private int iconAntiAliasing = 4;
    [SerializeField] private bool iconUseOutline = true;
    [SerializeField] [Range(0, 8)] private int iconOutlineWidth = 1;
    [SerializeField] private bool iconUseShadow = true;
    [SerializeField] private Vector2 iconShadowOffset = new Vector2(2f, -2f);
    [SerializeField] [Range(0, 8)] private int iconShadowBlur = 2;
    [SerializeField] private Vector3 iconModelEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 iconModelScale = Vector3.one;
    [SerializeField] private Vector3 iconCameraEulerAngles = new Vector3(25f, -35f, 0f);
    [SerializeField] private Vector3 slotIconModelEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 slotIconModelScale = Vector3.one;
    [SerializeField] private Vector3 slotIconCameraEulerAngles = new Vector3(25f, -35f, 0f);
    [SerializeField] [Range(1f, 2f)] private float slotIconPadding = 1.15f;
    [SerializeField] private bool iconShowCellGrid = true;
    [SerializeField] private bool iconShowCellGridBorder = true;
    [SerializeField] [Range(1, 8)] private int iconCellGridBorderLineThickness = 2;
    [SerializeField] private bool iconUseDirectionalLight = true;
    [SerializeField] private Vector3 iconLightEulerAngles = new Vector3(50f, -30f, 0f);
    [SerializeField] private float iconLightIntensity = 1.5f;
    [SerializeField] private bool slotIconUseDirectionalLight = true;
    [SerializeField] private Vector3 slotIconLightEulerAngles = new Vector3(50f, -30f, 0f);
    [SerializeField] private float slotIconLightIntensity = 1.5f;
    [SerializeField, HideInInspector] private bool slotIconSettingsInitialized;

    public string ItemName => string.IsNullOrWhiteSpace(itemName) ? name : itemName;
    public string ShortName => string.IsNullOrWhiteSpace(shortName) ? string.Empty : shortName;
    public string Description => description ?? string.Empty;
    public ItemType ItemType => itemType;
    public ItemRarity Rarity => rarity;
    public Color ShortNameColor => Settings.GetShortNameColor(rarity);
    public float Weight => Mathf.Max(0f, weight);
    public bool IsStackable => stackable && HasDurability == false;
    public bool HasDurability => useDurability;
    public float DefaultDurabilityPercent => NormalizeDurability(defaultDurabilityPercent);
    public bool CanEquipHelmet => canEquipHelmet;
    public IReadOnlyList<CharacterStatModifier> StatModifiers => statModifiers;
    public bool HasStatModifiers => CharacterStatUtility.HasAnyModifier(statModifiers);
    public WorldItem WorldItemPrefab => worldItemPrefab;

    public Sprite FallbackIcon => itemIcon;
    public GameObject IconPrefab => iconPrefab;
    public IReadOnlyList<ItemIconPart> IconParts => iconParts;
    public int IconPixelsPerCell => Mathf.Max(16, iconPixelsPerCell);
    public int IconRenderScale => Mathf.Clamp(iconRenderScale, 1, 4);
    public int IconAntiAliasing => GetSupportedAntiAliasing(iconAntiAliasing);
    public bool IconUseOutline => iconUseOutline && IconOutlineTextureWidth > 0 && IconOutlineColor.a > 0f;
    public Color IconOutlineColor => Settings.IconOutlineColor;
    public int IconOutlineTextureWidth => Mathf.Max(0, iconOutlineWidth) * IconRenderScale;
    public bool IconUseShadow => iconUseShadow && IconShadowColor.a > 0f && (IconShadowTextureOffset != Vector2Int.zero || IconShadowTextureBlur > 0);
    public Color IconShadowColor => Settings.IconShadowColor;
    public Vector2Int IconShadowTextureOffset => Vector2Int.RoundToInt(iconShadowOffset * IconRenderScale);
    public int IconShadowTextureBlur => Mathf.Max(0, iconShadowBlur) * IconRenderScale;
    public float IconPadding => Mathf.Max(1f, iconPadding);
    public Vector3 IconModelEulerAngles => iconModelEulerAngles;
    public Vector3 IconModelScale => iconModelScale == Vector3.zero ? Vector3.one : iconModelScale;
    public Vector3 IconCameraEulerAngles => iconCameraEulerAngles;
    public float SlotIconPadding
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return Mathf.Max(1f, slotIconPadding);
        }
    }

    public Vector3 SlotIconModelEulerAngles
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return slotIconModelEulerAngles;
        }
    }

    public Vector3 SlotIconModelScale
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return slotIconModelScale == Vector3.zero ? Vector3.one : slotIconModelScale;
        }
    }

    public Vector3 SlotIconCameraEulerAngles
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return slotIconCameraEulerAngles;
        }
    }
    public Color IconBackgroundColor => Settings.GetIconBackgroundColor(rarity);
    public bool IconShowCellGrid => iconShowCellGrid;
    public bool IconShowCellGridBorder => iconShowCellGridBorder;
    public Color IconCellGridBorderColor => Settings.GetIconCellGridBorderColor(rarity);
    public float IconCellGridBorderLineThickness => Mathf.Max(1f, iconCellGridBorderLineThickness);
    public bool IconUseDirectionalLight => iconUseDirectionalLight;
    public Vector3 IconLightEulerAngles => iconLightEulerAngles;
    public float IconLightIntensity => iconLightIntensity;
    public bool SlotIconUseDirectionalLight
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return slotIconUseDirectionalLight;
        }
    }

    public Vector3 SlotIconLightEulerAngles
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return slotIconLightEulerAngles;
        }
    }

    public float SlotIconLightIntensity
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return slotIconLightIntensity;
        }
    }

    public int IconTextureWidth => Mathf.Max(1, width) * IconPixelsPerCell * IconRenderScale;
    public int IconTextureHeight => Mathf.Max(1, height) * IconPixelsPerCell * IconRenderScale;
    public float IconSpritePixelsPerUnit => IconPixelsPerCell * IconRenderScale;

    private GameProjectSettings Settings => GameProjectSettings.LoadDefault();

    private void OnEnable()
    {
        EnsureSlotIconSettingsInitialized();
    }

    private void OnValidate()
    {
        EnsureSlotIconSettingsInitialized();
        defaultDurabilityPercent = NormalizeDurability(defaultDurabilityPercent);
    }

    public Sprite GetIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        if (HasRuntimeIconSource(runtimeIconParts))
        {
            return ItemIconCache.GetOrCreate(this, runtimeIconParts);
        }

        return itemIcon;
    }

    public Sprite GetSlotIcon(int slotWidth, int slotHeight, IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        EnsureSlotIconSettingsInitialized();

        if (HasRuntimeIconSource(runtimeIconParts))
        {
            return ItemIconCache.GetOrCreateSlotIcon(this, slotWidth, slotHeight, runtimeIconParts);
        }

        return itemIcon;
    }

    internal bool HasRuntimeIconSource(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        if (generateIconAtRuntime == false)
        {
            return false;
        }

        return iconPrefab != null || HasValidPart(iconParts) || HasValidPart(runtimeIconParts);
    }

    internal int BuildIconHash(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        return BuildIconHash(runtimeIconParts, Mathf.Max(1, width), Mathf.Max(1, height), false);
    }

    internal int BuildSlotIconHash(int slotWidth, int slotHeight, IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        return BuildIconHash(runtimeIconParts, Mathf.Max(1, slotWidth), Mathf.Max(1, slotHeight), true);
    }

    public void SetWorldPrefab(WorldItem worldItem) => worldItemPrefab = worldItem;

    public static float NormalizeDurability(float durabilityPercent)
    {
        return Mathf.Clamp(durabilityPercent, 0f, 100f);
    }

    private int BuildIconHash(IReadOnlyList<ItemIconPart> runtimeIconParts, int targetWidth, int targetHeight, bool useSlotIconSettings)
    {
        if (useSlotIconSettings)
        {
            EnsureSlotIconSettingsInitialized();
        }

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + GetInstanceID();
            hash = hash * 31 + (int)rarity;
            hash = hash * 31 + Settings.BuildItemRarityVisualHash();
            hash = hash * 31 + targetWidth;
            hash = hash * 31 + targetHeight;
            hash = hash * 31 + IconPixelsPerCell;
            hash = hash * 31 + IconRenderScale;
            hash = hash * 31 + IconAntiAliasing;
            hash = hash * 31 + (iconUseOutline ? 1 : 0);
            hash = hash * 31 + HashColor(IconOutlineColor);
            hash = hash * 31 + Mathf.Max(0, iconOutlineWidth);
            hash = hash * 31 + (iconUseShadow ? 1 : 0);
            hash = hash * 31 + HashColor(IconShadowColor);
            hash = hash * 31 + HashVector(iconShadowOffset);
            hash = hash * 31 + Mathf.Max(0, iconShadowBlur);
            hash = hash * 31 + Quantize(useSlotIconSettings ? SlotIconPadding : IconPadding);
            hash = hash * 31 + HashVector(useSlotIconSettings ? slotIconModelEulerAngles : iconModelEulerAngles);
            hash = hash * 31 + HashVector(useSlotIconSettings ? SlotIconModelScale : IconModelScale);
            hash = hash * 31 + HashVector(useSlotIconSettings ? slotIconCameraEulerAngles : iconCameraEulerAngles);
            bool useDirectionalLight = useSlotIconSettings ? SlotIconUseDirectionalLight : IconUseDirectionalLight;
            hash = hash * 31 + (useDirectionalLight ? 1 : 0);
            hash = hash * 31 + HashVector(useSlotIconSettings ? slotIconLightEulerAngles : iconLightEulerAngles);
            hash = hash * 31 + Quantize(useSlotIconSettings ? slotIconLightIntensity : iconLightIntensity);
            hash = hash * 31 + (iconPrefab == null ? 0 : iconPrefab.GetInstanceID());
            hash = hash * 31 + HashParts(iconParts);
            hash = hash * 31 + HashParts(runtimeIconParts);
            return hash;
        }
    }

    private static bool HasValidPart(IReadOnlyList<ItemIconPart> parts)
    {
        if (parts == null)
        {
            return false;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].Prefab != null)
            {
                return true;
            }
        }

        return false;
    }

    private static int HashParts(IReadOnlyList<ItemIconPart> parts)
    {
        if (parts == null)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;

            for (int i = 0; i < parts.Count; i++)
            {
                hash = hash * 31 + (parts[i] == null ? 0 : parts[i].BuildHash());
            }

            return hash;
        }
    }

    private static int HashVector(Vector3 value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Quantize(value.x);
            hash = hash * 31 + Quantize(value.y);
            hash = hash * 31 + Quantize(value.z);
            return hash;
        }
    }

    private static int HashVector(Vector2 value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Quantize(value.x);
            hash = hash * 31 + Quantize(value.y);
            return hash;
        }
    }

    private static int HashColor(Color value)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Quantize(value.r);
            hash = hash * 31 + Quantize(value.g);
            hash = hash * 31 + Quantize(value.b);
            hash = hash * 31 + Quantize(value.a);
            return hash;
        }
    }

    private static int Quantize(float value)
    {
        return Mathf.RoundToInt(value * 1000f);
    }

    private void EnsureSlotIconSettingsInitialized()
    {
        if (slotIconSettingsInitialized)
        {
            return;
        }

        slotIconModelEulerAngles = iconModelEulerAngles;
        slotIconModelScale = iconModelScale;
        slotIconCameraEulerAngles = iconCameraEulerAngles;
        slotIconPadding = iconPadding;
        slotIconUseDirectionalLight = iconUseDirectionalLight;
        slotIconLightEulerAngles = iconLightEulerAngles;
        slotIconLightIntensity = iconLightIntensity;
        slotIconSettingsInitialized = true;
    }

    private static int GetSupportedAntiAliasing(int value)
    {
        int clampedValue = Mathf.Clamp(value, 1, 8);

        if (clampedValue <= 1) { return 1; }
        if (clampedValue <= 2) { return 2; }
        if (clampedValue <= 4) { return 4; }

        return 8;
    }
}
