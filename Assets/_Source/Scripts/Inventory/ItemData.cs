using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
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
    [SerializeField] private Color iconOutlineColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] [Range(0, 8)] private int iconOutlineWidth = 1;
    [SerializeField] private bool iconUseShadow = true;
    [SerializeField] private Color iconShadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Vector2 iconShadowOffset = new Vector2(2f, -2f);
    [SerializeField] [Range(0, 8)] private int iconShadowBlur = 2;
    [SerializeField] private Vector3 iconModelEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 iconModelScale = Vector3.one;
    [SerializeField] private Vector3 iconCameraEulerAngles = new Vector3(25f, -35f, 0f);
    [SerializeField] private Color iconBackgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private bool iconShowCellGrid = true;
    [SerializeField] private bool iconShowCellGridBorder = true;
    [SerializeField] private Color iconCellGridBorderColor = new Color(0.745283f, 0.745283f, 0.745283f, 0.11764706f);
    [SerializeField] [Range(1, 8)] private int iconCellGridBorderLineThickness = 2;
    [SerializeField] private bool iconUseDirectionalLight = true;
    [SerializeField] private Vector3 iconLightEulerAngles = new Vector3(50f, -30f, 0f);
    [SerializeField] private float iconLightIntensity = 1.5f;

    public Sprite FallbackIcon => itemIcon;
    public GameObject IconPrefab => iconPrefab;
    public IReadOnlyList<ItemIconPart> IconParts => iconParts;
    public int IconPixelsPerCell => Mathf.Max(16, iconPixelsPerCell);
    public int IconRenderScale => Mathf.Clamp(iconRenderScale, 1, 4);
    public int IconAntiAliasing => GetSupportedAntiAliasing(iconAntiAliasing);
    public bool IconUseOutline => iconUseOutline && IconOutlineTextureWidth > 0 && iconOutlineColor.a > 0f;
    public Color IconOutlineColor => iconOutlineColor;
    public int IconOutlineTextureWidth => Mathf.Max(0, iconOutlineWidth) * IconRenderScale;
    public bool IconUseShadow => iconUseShadow && iconShadowColor.a > 0f && (IconShadowTextureOffset != Vector2Int.zero || IconShadowTextureBlur > 0);
    public Color IconShadowColor => iconShadowColor;
    public Vector2Int IconShadowTextureOffset => Vector2Int.RoundToInt(iconShadowOffset * IconRenderScale);
    public int IconShadowTextureBlur => Mathf.Max(0, iconShadowBlur) * IconRenderScale;
    public float IconPadding => Mathf.Max(1f, iconPadding);
    public Vector3 IconModelEulerAngles => iconModelEulerAngles;
    public Vector3 IconModelScale => iconModelScale == Vector3.zero ? Vector3.one : iconModelScale;
    public Vector3 IconCameraEulerAngles => iconCameraEulerAngles;
    public Color IconBackgroundColor => iconBackgroundColor;
    public bool IconShowCellGrid => iconShowCellGrid;
    public bool IconShowCellGridBorder => iconShowCellGridBorder;
    public Color IconCellGridBorderColor => iconCellGridBorderColor;
    public float IconCellGridBorderLineThickness => Mathf.Max(1f, iconCellGridBorderLineThickness);
    public bool IconUseDirectionalLight => iconUseDirectionalLight;
    public Vector3 IconLightEulerAngles => iconLightEulerAngles;
    public float IconLightIntensity => iconLightIntensity;

    public int IconTextureWidth => Mathf.Max(1, width) * IconPixelsPerCell * IconRenderScale;
    public int IconTextureHeight => Mathf.Max(1, height) * IconPixelsPerCell * IconRenderScale;
    public float IconSpritePixelsPerUnit => IconPixelsPerCell * IconRenderScale;

    public Sprite GetIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        if (HasRuntimeIconSource(runtimeIconParts))
        {
            return ItemIconCache.GetOrCreate(this, runtimeIconParts);
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
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + GetInstanceID();
            hash = hash * 31 + Mathf.Max(1, width);
            hash = hash * 31 + Mathf.Max(1, height);
            hash = hash * 31 + IconPixelsPerCell;
            hash = hash * 31 + IconRenderScale;
            hash = hash * 31 + IconAntiAliasing;
            hash = hash * 31 + (iconUseOutline ? 1 : 0);
            hash = hash * 31 + HashColor(iconOutlineColor);
            hash = hash * 31 + Mathf.Max(0, iconOutlineWidth);
            hash = hash * 31 + (iconUseShadow ? 1 : 0);
            hash = hash * 31 + HashColor(iconShadowColor);
            hash = hash * 31 + HashVector(iconShadowOffset);
            hash = hash * 31 + Mathf.Max(0, iconShadowBlur);
            hash = hash * 31 + Quantize(iconPadding);
            hash = hash * 31 + HashVector(iconModelEulerAngles);
            hash = hash * 31 + HashVector(IconModelScale);
            hash = hash * 31 + HashVector(iconCameraEulerAngles);
            hash = hash * 31 + (iconUseDirectionalLight ? 1 : 0);
            hash = hash * 31 + HashVector(iconLightEulerAngles);
            hash = hash * 31 + Quantize(iconLightIntensity);
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

    private static int GetSupportedAntiAliasing(int value)
    {
        int clampedValue = Mathf.Clamp(value, 1, 8);

        if (clampedValue <= 1) { return 1; }
        if (clampedValue <= 2) { return 2; }
        if (clampedValue <= 4) { return 4; }

        return 8;
    }
}
