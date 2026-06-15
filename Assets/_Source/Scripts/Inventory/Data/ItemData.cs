using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _itemName;
    [SerializeField] private string _shortName;
    [SerializeField] [TextArea(3, 8)] private string _description;
    [SerializeField] private ItemType _itemType = ItemType.Misc;
    [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
    [SerializeField] [Min(0f)] private float _weight;
    [SerializeField] private bool _stackable;
    [SerializeField] private bool _useDurability;
    [SerializeField] [Range(0f, 100f)] private float _defaultDurabilityPercent = 100f;
    [SerializeField] private bool _canEquipHelmet = true;
    [SerializeField] private List<CharacterStatModifier> _statModifiers = new();
    [SerializeField] private WorldItem _worldItemPrefab;
    [SerializeField] [Min(1)] private int _width = 1;
    [SerializeField] [Min(1)] private int _height = 1;
    [SerializeField] private Sprite _itemIcon;
    [SerializeField] private bool _generateIconAtRuntime = true;
    [SerializeField] private GameObject _iconPrefab;
    [SerializeField] private List<ItemIconPart> _iconParts = new List<ItemIconPart>();
    [SerializeField] [Min(16)] private int _iconPixelsPerCell = 64;
    [SerializeField] [Range(1, 4)] private int _iconRenderScale = 2;
    [SerializeField] [Range(1f, 2f)] private float _iconPadding = 1.15f;
    [SerializeField] [Range(1, 8)] private int _iconAntiAliasing = 4;
    [SerializeField] private bool _iconUseOutline = true;
    [SerializeField] [Range(0, 8)] private int _iconOutlineWidth = 1;
    [SerializeField] private bool _iconUseShadow = true;
    [SerializeField] private Vector2 _iconShadowOffset = new(2f, -2f);
    [SerializeField] [Range(0, 8)] private int _iconShadowBlur = 2;
    [SerializeField] private Vector3 _iconModelEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 _iconModelScale = Vector3.one;
    [SerializeField] private Vector3 _iconCameraEulerAngles = new(25f, -35f, 0f);
    [SerializeField] private Vector3 _slotIconModelEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 _slotIconModelScale = Vector3.one;
    [SerializeField] private Vector3 _slotIconCameraEulerAngles = new(25f, -35f, 0f);
    [SerializeField] [Range(1f, 2f)] private float _slotIconPadding = 1.15f;
    [SerializeField] private bool _iconShowCellGrid = true;
    [SerializeField] private bool _iconShowCellGridBorder = true;
    [SerializeField] [Range(1, 8)] private int _iconCellGridBorderLineThickness = 2;
    [SerializeField] private bool _iconUseDirectionalLight = true;
    [SerializeField] private Vector3 _iconLightEulerAngles = new(50f, -30f, 0f);
    [SerializeField] private float _iconLightIntensity = 1.5f;
    [SerializeField] private bool _slotIconUseDirectionalLight = true;
    [SerializeField] private Vector3 _slotIconLightEulerAngles = new(50f, -30f, 0f);
    [SerializeField] private float _slotIconLightIntensity = 1.5f;
    [SerializeField, HideInInspector] private bool _slotIconSettingsInitialized;

    public string ItemName => string.IsNullOrWhiteSpace(_itemName) ? name : _itemName;
    public string ShortName => string.IsNullOrWhiteSpace(_shortName) ? string.Empty : _shortName;
    public string Description => _description ?? string.Empty;
    public ItemType ItemType => _itemType;
    public ItemRarity Rarity => _rarity;
    public Color ShortNameColor => Settings.GetShortNameColor(_rarity);
    public float Weight => Mathf.Max(0f, _weight);
    public bool IsStackable => _stackable && HasDurability == false;
    public bool HasDurability => _useDurability;
    public float DefaultDurabilityPercent => NormalizeDurability(_defaultDurabilityPercent);
    public bool CanEquipHelmet => _canEquipHelmet;
    public IReadOnlyList<CharacterStatModifier> StatModifiers => _statModifiers;
    public bool HasStatModifiers => CharacterStatUtility.HasAnyModifier(_statModifiers);
    public WorldItem WorldItemPrefab => _worldItemPrefab;
    public int Width => Mathf.Max(1, _width);
    public int Height => Mathf.Max(1, _height);

    public Sprite FallbackIcon => _itemIcon;
    public GameObject IconPrefab => _iconPrefab;
    public IReadOnlyList<ItemIconPart> IconParts => _iconParts;
    public int IconPixelsPerCell => Mathf.Max(16, _iconPixelsPerCell);
    public int IconRenderScale => Mathf.Clamp(_iconRenderScale, 1, 4);
    public int IconAntiAliasing => GetSupportedAntiAliasing(_iconAntiAliasing);
    public bool IconUseOutline => _iconUseOutline && IconOutlineTextureWidth > 0 && IconOutlineColor.a > 0f;
    public Color IconOutlineColor => Settings.IconOutlineColor;
    public int IconOutlineTextureWidth => Mathf.Max(0, _iconOutlineWidth) * IconRenderScale;
    public bool IconUseShadow => _iconUseShadow && IconShadowColor.a > 0f && (IconShadowTextureOffset != Vector2Int.zero || IconShadowTextureBlur > 0);
    public Color IconShadowColor => Settings.IconShadowColor;
    public Vector2Int IconShadowTextureOffset => Vector2Int.RoundToInt(_iconShadowOffset * IconRenderScale);
    public int IconShadowTextureBlur => Mathf.Max(0, _iconShadowBlur) * IconRenderScale;
    public float IconPadding => Mathf.Max(1f, _iconPadding);
    public Vector3 IconModelEulerAngles => _iconModelEulerAngles;
    public Vector3 IconModelScale => _iconModelScale == Vector3.zero ? Vector3.one : _iconModelScale;
    public Vector3 IconCameraEulerAngles => _iconCameraEulerAngles;

    public float SlotIconPadding
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return Mathf.Max(1f, _slotIconPadding);
        }
    }

    public Vector3 SlotIconModelEulerAngles
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return _slotIconModelEulerAngles;
        }
    }

    public Vector3 SlotIconModelScale
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return _slotIconModelScale == Vector3.zero ? Vector3.one : _slotIconModelScale;
        }
    }

    public Vector3 SlotIconCameraEulerAngles
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return _slotIconCameraEulerAngles;
        }
    }

    public Color IconBackgroundColor => Settings.GetIconBackgroundColor(_rarity);
    public bool IconShowCellGrid => _iconShowCellGrid;
    public bool IconShowCellGridBorder => _iconShowCellGridBorder;
    public Color IconCellGridBorderColor => Settings.GetIconCellGridBorderColor(_rarity);
    public float IconCellGridBorderLineThickness => Mathf.Max(1f, _iconCellGridBorderLineThickness);
    public bool IconUseDirectionalLight => _iconUseDirectionalLight;
    public Vector3 IconLightEulerAngles => _iconLightEulerAngles;
    public float IconLightIntensity => _iconLightIntensity;

    public bool SlotIconUseDirectionalLight
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return _slotIconUseDirectionalLight;
        }
    }

    public Vector3 SlotIconLightEulerAngles
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return _slotIconLightEulerAngles;
        }
    }

    public float SlotIconLightIntensity
    {
        get
        {
            EnsureSlotIconSettingsInitialized();
            return _slotIconLightIntensity;
        }
    }

    public int IconTextureWidth => Width * IconPixelsPerCell * IconRenderScale;
    public int IconTextureHeight => Height * IconPixelsPerCell * IconRenderScale;
    public float IconSpritePixelsPerUnit => IconPixelsPerCell * IconRenderScale;

    private GameProjectSettings Settings => GameProjectSettings.LoadDefault();

    private void OnEnable() => EnsureSlotIconSettingsInitialized();

    private void OnValidate()
    {
        EnsureSlotIconSettingsInitialized();
        _width = Mathf.Max(1, _width);
        _height = Mathf.Max(1, _height);
        _defaultDurabilityPercent = NormalizeDurability(_defaultDurabilityPercent);
    }

    public Sprite GetIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        if (HasRuntimeIconSource(runtimeIconParts))
        {
            return ItemIconCache.GetOrCreate(this, runtimeIconParts);
        }

        return _itemIcon;
    }

    public Sprite GetSlotIcon(int slotWidth, int slotHeight, IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        EnsureSlotIconSettingsInitialized();

        if (HasRuntimeIconSource(runtimeIconParts))
        {
            return ItemIconCache.GetOrCreateSlotIcon(this, slotWidth, slotHeight, runtimeIconParts);
        }

        return _itemIcon;
    }

    internal bool HasRuntimeIconSource(IReadOnlyList<ItemIconPart> runtimeIconParts = null) => ItemIconHashBuilder.HasRuntimeIconSource(_generateIconAtRuntime, _iconPrefab, _iconParts, runtimeIconParts);
    internal int BuildIconHash(IReadOnlyList<ItemIconPart> runtimeIconParts = null) => ItemIconHashBuilder.BuildHash(this, runtimeIconParts, Width, Height, false);
    internal int BuildSlotIconHash(int slotWidth, int slotHeight, IReadOnlyList<ItemIconPart> runtimeIconParts = null) => ItemIconHashBuilder.BuildHash(this, runtimeIconParts, Mathf.Max(1, slotWidth), Mathf.Max(1, slotHeight), true);
    public void SetWorldPrefab(WorldItem worldItem) => _worldItemPrefab = worldItem;
    public static float NormalizeDurability(float durabilityPercent) => Mathf.Clamp(durabilityPercent, 0f, 100f);

    private void EnsureSlotIconSettingsInitialized()
    {
        if (_slotIconSettingsInitialized)
        {
            return;
        }

        _slotIconModelEulerAngles = _iconModelEulerAngles;
        _slotIconModelScale = _iconModelScale;
        _slotIconCameraEulerAngles = _iconCameraEulerAngles;
        _slotIconPadding = _iconPadding;
        _slotIconUseDirectionalLight = _iconUseDirectionalLight;
        _slotIconLightEulerAngles = _iconLightEulerAngles;
        _slotIconLightIntensity = _iconLightIntensity;
        _slotIconSettingsInitialized = true;
    }

    private static int GetSupportedAntiAliasing(int value)
    {
        int clampedValue = Mathf.Clamp(value, 1, 8);

        if (clampedValue <= 1)
        {
            return 1;
        }
        if (clampedValue <= 2)
        { 
            return 2;
        }
        if (clampedValue <= 4)
        { 
            return 4;
        }

        return 8;
    }
}