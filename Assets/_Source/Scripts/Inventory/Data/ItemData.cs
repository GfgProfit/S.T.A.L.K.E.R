using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    [SerializeField] [BoxGroup("Identity")] private string _itemName;
    [SerializeField] [BoxGroup("Identity")] private string _shortName;
    [SerializeField] [BoxGroup("Identity")] [TextArea(3, 8)] private string _description;
    [SerializeField] [BoxGroup("Identity")] private ItemType _itemType = ItemType.Misc;
    [SerializeField] [BoxGroup("Identity")] private ItemRarity _rarity = ItemRarity.Common;
    [SerializeField] [BoxGroup("Inventory")] [Min(0f)] private float _weight;
    [SerializeField] [BoxGroup("Inventory")] private bool _stackable;
    [SerializeField] [BoxGroup("Durability")] private bool _useDurability;
    [SerializeField] [BoxGroup("Durability")] [ShowIf(nameof(UsesDurability))] [Range(0f, 100f)] private float _defaultDurabilityPercent = 100f;
    [SerializeField] [BoxGroup("Equipment")] [ShowIf(nameof(IsArmor))] private bool _canEquipHelmet = true;
    [SerializeField] [BoxGroup("Ammo")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoFleshDamage;
    [SerializeField] [BoxGroup("Ammo")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoArmorPenetration;
    [SerializeField] [BoxGroup("Ammo")] [ShowIf(nameof(IsAmmo))] private float _ammoWeaponRecoilPercentModifier;
    [SerializeField] [BoxGroup("Ammo")] [ShowIf(nameof(IsAmmo))] private float _ammoWeaponDurabilityLossPercentModifier;
    [SerializeField] [BoxGroup("Ammo/Visual")] [ShowIf(nameof(IsAmmo))] private Material _ammoMaterial;
    [SerializeField] [BoxGroup("Ammo/Ballistics")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoBulletVelocityMetersPerSecondFallback;
    [SerializeField] [BoxGroup("Ammo/Ballistics")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoBulletMassGrams;
    [SerializeField] [BoxGroup("Ammo/Ballistics")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoBulletDiameterMillimeters;
    [SerializeField] [BoxGroup("Ammo/Ballistics")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoBulletDragCoefficient;
    [SerializeField] [BoxGroup("Ammo/Ballistics")] [ShowIf(nameof(IsAmmo))] [Range(0f, 1f)] private float _ammoRicochetChance;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] private bool _ammoTracerEnabled;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] private Material _ammoTracerMaterial;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] [ColorUsage(true, true)] private Color _ammoTracerColor = new(1f, 0.15f, 0.02f, 1f);
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoTracerWidthMeters = 0.006f;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoTracerTrailDurationSeconds = 0.02f;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoTracerIgnitionDistanceMeters;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoTracerBurnDurationSeconds = 3f;
    [SerializeField] [BoxGroup("Ammo/Tracer")] [ShowIf(nameof(IsAmmo))] [Min(0f)] private float _ammoTracerEmissionIntensity = 10f;
    [SerializeField] [BoxGroup("First Person")] [ShowIf(nameof(IsArmor))] private string _firstPersonLegsMeshName;
    [SerializeField] [BoxGroup("First Person")] [ShowIf(nameof(IsArmor))] private string _firstPersonHandsMeshName;
    [SerializeField] [BoxGroup("First Person")] [ShowIf(nameof(UsesFirstPersonWeapon))] private GameObject _firstPersonWeaponPrefab;
    [SerializeField] [BoxGroup("First Person")] [ShowIf(nameof(UsesFirstPersonWeapon))] private Vector3 _firstPersonWeaponSpawnScale = Vector3.one;
    [SerializeField] [BoxGroup("Weapon")] [ShowIf(nameof(UsesWeaponData))] private WeaponData _weaponData;
    [SerializeField] [BoxGroup("Module")] [ShowIf(nameof(IsModule))] private WeaponModuleSlot _moduleSlot;
    [SerializeField] [BoxGroup("Module")] [ShowIf(nameof(IsModule))] private Vector2Int _moduleInventorySizeDelta;
    [SerializeField] [BoxGroup("Stats")] [ShowIf(nameof(CanHaveStats))] private List<CharacterStatModifier> _statModifiers = new();
    [SerializeField] [BoxGroup("World")] private WorldItem _worldItemPrefab;
    [SerializeField] [BoxGroup("Grid Size")] [Min(1)] private int _width = 1;
    [SerializeField] [BoxGroup("Grid Size")] [Min(1)] private int _height = 1;
    [SerializeField] [BoxGroup("Icon/Base")] private Sprite _itemIcon;
    [SerializeField] [BoxGroup("Icon/Runtime")] private bool _generateIconAtRuntime = true;
    [SerializeField] [BoxGroup("Icon/Runtime")] [EnableIf(nameof(GeneratesIconAtRuntime))] private GameObject _iconPrefab;
    [SerializeField] [BoxGroup("Icon/Weapon Variants")] [ShowIf(nameof(UsesFirstPersonWeapon))] private ItemData[] _defaultIconModules = System.Array.Empty<ItemData>();
    [SerializeField] [BoxGroup("Icon/Texture")] [Min(16)] private int _iconPixelsPerCell = 64;
    [SerializeField] [BoxGroup("Icon/Texture")] [Range(1, 4)] private int _iconRenderScale = 2;
    [SerializeField] [BoxGroup("Icon/Texture")] [Range(1f, 2f)] private float _iconPadding = 1.15f;
    [SerializeField] [BoxGroup("Icon/Texture")] [Range(1, 8)] private int _iconAntiAliasing = 4;
    [SerializeField] [BoxGroup("Icon/Effects")] private bool _iconUseOutline = true;
    [SerializeField] [BoxGroup("Icon/Effects")] [EnableIf(nameof(UsesIconOutline))] [Range(0, 8)] private int _iconOutlineWidth = 1;
    [SerializeField] [BoxGroup("Icon/Effects")] private bool _iconUseShadow = true;
    [SerializeField] [BoxGroup("Icon/Effects")] [EnableIf(nameof(UsesIconShadow))] private Vector2 _iconShadowOffset = new(2f, -2f);
    [SerializeField] [BoxGroup("Icon/Effects")] [EnableIf(nameof(UsesIconShadow))] [Range(0, 8)] private int _iconShadowBlur = 2;
    [SerializeField] [BoxGroup("Icon/Model")] private Vector3 _iconModelEulerAngles = Vector3.zero;
    [SerializeField] [BoxGroup("Icon/Model")] private Vector3 _iconModelScale = Vector3.one;
    [SerializeField] [BoxGroup("Icon/Model")] private Vector3 _iconCameraEulerAngles = new(25f, -35f, 0f);
    [SerializeField] [BoxGroup("Slot Icon/Model")] private Vector3 _slotIconModelEulerAngles = Vector3.zero;
    [SerializeField] [BoxGroup("Slot Icon/Model")] private Vector3 _slotIconModelScale = Vector3.one;
    [SerializeField] [BoxGroup("Slot Icon/Model")] private Vector3 _slotIconCameraEulerAngles = new(25f, -35f, 0f);
    [SerializeField] [BoxGroup("Slot Icon/Texture")] [Range(1f, 2f)] private float _slotIconPadding = 1.15f;
    [SerializeField] [BoxGroup("Icon/Grid")] private bool _iconShowCellGrid = true;
    [SerializeField] [BoxGroup("Icon/Grid")] [EnableIf(nameof(ShowsIconCellGrid))] private bool _iconShowCellGridBorder = true;
    [SerializeField] [BoxGroup("Icon/Grid")] [EnableIf(nameof(ShowsIconCellGridBorder))] [Range(1, 8)] private int _iconCellGridBorderLineThickness = 2;
    [SerializeField] [BoxGroup("Icon/Light")] private bool _iconUseDirectionalLight = true;
    [SerializeField] [BoxGroup("Icon/Light")] [EnableIf(nameof(UsesIconDirectionalLight))] private Vector3 _iconLightEulerAngles = new(50f, -30f, 0f);
    [SerializeField] [BoxGroup("Icon/Light")] [EnableIf(nameof(UsesIconDirectionalLight))] private float _iconLightIntensity = 1.5f;
    [SerializeField] [BoxGroup("Slot Icon/Light")] private bool _slotIconUseDirectionalLight = true;
    [SerializeField] [BoxGroup("Slot Icon/Light")] [EnableIf(nameof(UsesSlotIconDirectionalLight))] private Vector3 _slotIconLightEulerAngles = new(50f, -30f, 0f);
    [SerializeField] [BoxGroup("Slot Icon/Light")] [EnableIf(nameof(UsesSlotIconDirectionalLight))] private float _slotIconLightIntensity = 1.5f;
    [SerializeField, HideInInspector] private bool _slotIconSettingsInitialized;

    public string ItemName => string.IsNullOrWhiteSpace(_itemName) ? name : _itemName;
    public string ShortName => string.IsNullOrWhiteSpace(_shortName) ? string.Empty : _shortName;
    public string Description => _description ?? string.Empty;
    public ItemType ItemType => _itemType;
    public ItemRarity Rarity => _rarity;
    public Color ShortNameColor => Settings.GetShortNameColor(_rarity);
    public float Weight => Mathf.Max(0f, _weight);
    public bool IsStackable => _stackable && HasDurability == false && _itemType != ItemType.Module;
    public bool HasDurability => _useDurability;
    public float DefaultDurabilityPercent => NormalizeDurability(_defaultDurabilityPercent);
    public bool CanEquipHelmet => _canEquipHelmet;
    public float AmmoFleshDamage => Mathf.Max(0f, _ammoFleshDamage);
    public float AmmoArmorPenetration => Mathf.Max(0f, _ammoArmorPenetration);
    public float AmmoBulletVelocityMetersPerSecondFallback => Mathf.Max(0f, _ammoBulletVelocityMetersPerSecondFallback);
    public float AmmoBulletMassGrams => Mathf.Max(0f, _ammoBulletMassGrams);
    public float AmmoBulletDiameterMillimeters => Mathf.Max(0f, _ammoBulletDiameterMillimeters);
    public float AmmoBulletMassKilograms => Mathf.Max(0f, _ammoBulletMassGrams) * 0.001f;
    public float AmmoBulletDiameterMeters => Mathf.Max(0f, _ammoBulletDiameterMillimeters) * 0.001f;
    public float AmmoBulletDragCoefficient => Mathf.Max(0f, _ammoBulletDragCoefficient);
    public float AmmoRicochetChance => Mathf.Clamp01(_ammoRicochetChance);
    public Material AmmoMaterial => _ammoMaterial;
    public bool AmmoTracerEnabled => _ammoTracerEnabled;
    public Material AmmoTracerMaterial => _ammoTracerMaterial;
    public Color AmmoTracerColor => _ammoTracerColor;
    public float AmmoTracerWidthMeters => Mathf.Max(0f, _ammoTracerWidthMeters);
    public float AmmoTracerTrailDurationSeconds => Mathf.Max(0f, _ammoTracerTrailDurationSeconds);
    public float AmmoTracerIgnitionDistanceMeters => Mathf.Max(0f, _ammoTracerIgnitionDistanceMeters);
    public float AmmoTracerBurnDurationSeconds => Mathf.Max(0f, _ammoTracerBurnDurationSeconds);
    public float AmmoTracerEmissionIntensity => Mathf.Max(0f, _ammoTracerEmissionIntensity);
    public float AmmoWeaponRecoilPercentModifier => _ammoWeaponRecoilPercentModifier;
    public float AmmoWeaponDurabilityLossPercentModifier => _ammoWeaponDurabilityLossPercentModifier;
    public string FirstPersonLegsMeshName => string.IsNullOrWhiteSpace(_firstPersonLegsMeshName) ? string.Empty : _firstPersonLegsMeshName;
    public string FirstPersonHandsMeshName => string.IsNullOrWhiteSpace(_firstPersonHandsMeshName) ? string.Empty : _firstPersonHandsMeshName;
    public GameObject FirstPersonWeaponPrefab => _firstPersonWeaponPrefab;
    public Vector3 FirstPersonWeaponSpawnScale => _firstPersonWeaponSpawnScale == Vector3.zero ? Vector3.one : _firstPersonWeaponSpawnScale;
    public WeaponData WeaponData => _weaponData;
    public WeaponModuleSlot ModuleSlot => _itemType == ItemType.Module ? _moduleSlot : WeaponModuleSlot.None;
    public Vector2Int ModuleInventorySizeDelta => new(Mathf.Max(0, _moduleInventorySizeDelta.x), Mathf.Max(0, _moduleInventorySizeDelta.y));
    public IReadOnlyList<CharacterStatModifier> StatModifiers => _statModifiers;
    public bool HasStatModifiers => CharacterStatUtility.HasAnyModifier(_statModifiers);
    public WorldItem WorldItemPrefab => _worldItemPrefab;
    public int Width => Mathf.Max(1, _width);
    public int Height => Mathf.Max(1, _height);

    public Sprite FallbackIcon => _itemIcon;
    public GameObject IconPrefab => _iconPrefab;
    public IReadOnlyList<ItemData> DefaultIconModules => _defaultIconModules ?? System.Array.Empty<ItemData>();
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
    private bool UsesDurability() => _useDurability;
    private bool IsArmor() => _itemType == ItemType.Armor;
    private bool IsAmmo() => _itemType == ItemType.Ammo;
    private bool IsModule() => _itemType == ItemType.Module;
    private bool UsesFirstPersonWeapon() => _itemType == ItemType.Weapon || _itemType == ItemType.Pistol || _itemType == ItemType.Knife;
    private bool UsesWeaponData() => _itemType == ItemType.Weapon || _itemType == ItemType.Pistol;
    private bool CanHaveStats() => _itemType == ItemType.Armor || _itemType == ItemType.Helmet || _itemType == ItemType.Artifact;
    private bool GeneratesIconAtRuntime() => _generateIconAtRuntime;
    private bool UsesIconOutline() => _iconUseOutline;
    private bool UsesIconShadow() => _iconUseShadow;
    private bool ShowsIconCellGrid() => _iconShowCellGrid;
    private bool ShowsIconCellGridBorder() => _iconShowCellGrid && _iconShowCellGridBorder;
    private bool UsesIconDirectionalLight() => _iconUseDirectionalLight;
    private bool UsesSlotIconDirectionalLight() => _slotIconUseDirectionalLight;

    private void OnEnable() => EnsureSlotIconSettingsInitialized();

    private void OnValidate()
    {
        EnsureSlotIconSettingsInitialized();
        _width = Mathf.Max(1, _width);
        _height = Mathf.Max(1, _height);
        _defaultDurabilityPercent = NormalizeDurability(_defaultDurabilityPercent);
        _firstPersonWeaponSpawnScale = _firstPersonWeaponSpawnScale == Vector3.zero ? Vector3.one : _firstPersonWeaponSpawnScale;
        _ammoFleshDamage = Mathf.Max(0f, _ammoFleshDamage);
        _ammoArmorPenetration = Mathf.Max(0f, _ammoArmorPenetration);
        _ammoBulletVelocityMetersPerSecondFallback = Mathf.Max(0f, _ammoBulletVelocityMetersPerSecondFallback);
        _ammoBulletMassGrams = Mathf.Max(0f, _ammoBulletMassGrams);
        _ammoBulletDiameterMillimeters = Mathf.Max(0f, _ammoBulletDiameterMillimeters);
        _ammoBulletDragCoefficient = Mathf.Max(0f, _ammoBulletDragCoefficient);
        _ammoRicochetChance = Mathf.Clamp01(_ammoRicochetChance);
        _ammoTracerWidthMeters = Mathf.Max(0f, _ammoTracerWidthMeters);
        _ammoTracerTrailDurationSeconds = Mathf.Max(0f, _ammoTracerTrailDurationSeconds);
        _ammoTracerIgnitionDistanceMeters = Mathf.Max(0f, _ammoTracerIgnitionDistanceMeters);
        _ammoTracerBurnDurationSeconds = Mathf.Max(0f, _ammoTracerBurnDurationSeconds);
        _ammoTracerEmissionIntensity = Mathf.Max(0f, _ammoTracerEmissionIntensity);
        _moduleInventorySizeDelta = new Vector2Int(Mathf.Max(0, _moduleInventorySizeDelta.x), Mathf.Max(0, _moduleInventorySizeDelta.y));
    }

    public Sprite GetIcon(IReadOnlyList<ItemData> installedModules = null)
    {
        return GetIcon(Width, Height, installedModules);
    }

    public Sprite GetIcon(int width, int height, IReadOnlyList<ItemData> installedModules = null)
    {
        if (HasRuntimeIconSource())
        {
            return ItemIconCache.GetOrCreate(this, width, height, installedModules);
        }

        return _itemIcon;
    }

    public Sprite GetSlotIcon(int slotWidth, int slotHeight, IReadOnlyList<ItemData> installedModules = null)
    {
        EnsureSlotIconSettingsInitialized();

        if (HasRuntimeIconSource())
        {
            return ItemIconCache.GetOrCreateSlotIcon(this, slotWidth, slotHeight, installedModules);
        }

        return _itemIcon;
    }

    public UniTask<Sprite> GetIconAsync(IReadOnlyList<ItemData> installedModules = null, CancellationToken cancellationToken = default)
    {
        return GetIconAsync(Width, Height, installedModules, cancellationToken);
    }

    public UniTask<Sprite> GetIconAsync(int width, int height, IReadOnlyList<ItemData> installedModules = null, CancellationToken cancellationToken = default)
    {
        return HasRuntimeIconSource()
            ? ItemIconCache.GetOrCreateAsync(this, width, height, installedModules, cancellationToken)
            : UniTask.FromResult(_itemIcon);
    }

    public UniTask<Sprite> GetSlotIconAsync(int slotWidth, int slotHeight, IReadOnlyList<ItemData> installedModules = null, CancellationToken cancellationToken = default)
    {
        EnsureSlotIconSettingsInitialized();

        return HasRuntimeIconSource()
            ? ItemIconCache.GetOrCreateSlotIconAsync(this, slotWidth, slotHeight, installedModules, cancellationToken)
            : UniTask.FromResult(_itemIcon);
    }

    internal bool HasRuntimeIconSource() => ItemIconHashBuilder.HasRuntimeIconSource(_generateIconAtRuntime, _iconPrefab);
    internal int BuildIconHash(IReadOnlyList<ItemData> installedModules = null) => BuildIconHash(Width, Height, installedModules);
    internal int BuildIconHash(int width, int height, IReadOnlyList<ItemData> installedModules = null) => ItemIconHashBuilder.BuildHash(this, installedModules, Mathf.Max(1, width), Mathf.Max(1, height), false);
    internal int BuildSlotIconHash(int slotWidth, int slotHeight, IReadOnlyList<ItemData> installedModules = null) => ItemIconHashBuilder.BuildHash(this, installedModules, Mathf.Max(1, slotWidth), Mathf.Max(1, slotHeight), true);
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
