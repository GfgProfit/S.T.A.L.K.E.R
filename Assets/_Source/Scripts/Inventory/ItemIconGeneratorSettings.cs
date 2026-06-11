using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[CreateAssetMenu(fileName = DefaultResourcePath, menuName = "Inventory/Item Icon Generator Settings")]
public class ItemIconGeneratorSettings : ScriptableObject
{
    public const string DefaultResourcePath = "ItemIconGeneratorSettings";

    private static ItemIconGeneratorSettings defaultSettings;
    private static bool defaultSettingsLoaded;

    [Header("Render Isolation")]
    [SerializeField] [Range(0, 31)] private int renderLayer = 31;
    [SerializeField] [Range(0, 15)] private int renderingLayer = 7;
    [SerializeField] private Vector3 renderOrigin = Vector3.zero;
    [SerializeField] private bool excludeIconLayerFromSceneLights = true;
    [SerializeField] private LightProbeUsage rendererLightProbeUsage = LightProbeUsage.Off;
    [SerializeField] private ReflectionProbeUsage rendererReflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
    [SerializeField] private bool rendererReceiveShadows;

    [Header("Render Target")]
    [SerializeField] private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGBHalf;

    [Header("Camera Overrides")]
    [SerializeField] private bool allowHdr = true;
    [SerializeField] private bool useCameraMsaa = true;
    [SerializeField] [Min(0.1f)] private float cameraDistanceMultiplier = 3f;
    [SerializeField] [Min(0f)] private float cameraDistanceOffset = 1f;
    [SerializeField] [Min(0.01f)] private float nearClipPlane = 0.01f;
    [SerializeField] [Min(1f)] private float farClipBoundsMultiplier = 8f;
    [SerializeField] [Min(0f)] private float farClipOffset = 10f;
    [SerializeField] private bool enableDirectSpecularLighting = true;
    [SerializeField] private bool enableExposureControl = true;
    [SerializeField] private bool enableAtmosphericScattering = true;
    [SerializeField] private bool enablePostProcess = true;
    [SerializeField] private bool enableColorGrading = true;
    [SerializeField] private bool enableTonemapping = true;
    [SerializeField] private bool enableVolumetrics = true;
    [SerializeField] private bool enableSkyReflection = true;

    [Header("Fill Lights")]
    [SerializeField] private bool useFrontFillLight = true;
    [SerializeField] [Min(0f)] private float frontFillLightIntensityMultiplier = 0.65f;
    [SerializeField] private Vector3 frontFillLightCameraDirection = new Vector3(0f, 0.15f, 1f);
    [SerializeField] private bool useSideFillLight = true;
    [SerializeField] [Min(0f)] private float sideFillLightIntensityMultiplier = 0.5f;
    [SerializeField] private Vector3 sideFillLightCameraDirection = new Vector3(-0.85f, 0.1f, 1f);
    [SerializeField] private bool useRimLight = true;
    [SerializeField] [Min(0f)] private float rimLightIntensityMultiplier = 0.25f;
    [SerializeField] private Vector3 rimLightCameraDirection = new Vector3(0.35f, 0.2f, -1f);
    [SerializeField] private LightShadows generatedLightShadows = LightShadows.None;

    [Header("Visual Environment")]
    [SerializeField] private SkyAmbientMode skyAmbientMode = SkyAmbientMode.Dynamic;
    [SerializeField] private RenderingSpace skyRenderingSpace = RenderingSpace.Camera;
    [SerializeField] private PhysicallyBasedSkyModel physicallyBasedSkyModel = PhysicallyBasedSkyModel.EarthAdvanced;
    [SerializeField] private Color physicallyBasedSkyGroundTint = new Color(0.122641504f, 0.1043775f, 0.09313812f, 1f);

    [Header("Fog")]
    [SerializeField] private bool fogEnabled = true;
    [SerializeField] private FogColorMode fogColorMode = FogColorMode.SkyColor;
    [SerializeField] [Min(0f)] private float maxFogDistance = 5000f;
    [SerializeField] [Min(0.0001f)] private float meanFreePath = 400f;
    [SerializeField] private bool enableVolumetricFog = true;
    [SerializeField] [Range(-1f, 1f)] private float fogAnisotropy = 0.65f;

    [Header("Exposure")]
    [SerializeField] private ExposureMode exposureMode = ExposureMode.AutomaticHistogram;
    [SerializeField] private float fixedExposure;
    [SerializeField] private float exposureCompensation;
    [SerializeField] private float exposureLimitMin = 2f;
    [SerializeField] private float exposureLimitMax = 14f;
    [SerializeField] private Vector2 histogramPercentages = new Vector2(40f, 90f);

    [Header("Color Adjustments")]
    [SerializeField] private float postExposure;
    [SerializeField] [Range(-100f, 100f)] private float contrast;
    [SerializeField] private Color colorFilter = Color.white;
    [SerializeField] [Range(-180f, 180f)] private float hueShift;
    [SerializeField] [Range(-100f, 100f)] private float saturation;

    [Header("Tonemapping")]
    [SerializeField] private TonemappingMode tonemappingMode = TonemappingMode.Neutral;

    public int RenderLayer => Mathf.Clamp(renderLayer, 0, 31);
    public int RenderLayerMask => 1 << RenderLayer;
    public int RenderingLayer => Mathf.Clamp(renderingLayer, 0, 15);
    public uint RenderingLayerMask => 1u << RenderingLayer;
    public Vector3 RenderOrigin => renderOrigin;
    public bool ExcludeIconLayerFromSceneLights => excludeIconLayerFromSceneLights;
    public LightProbeUsage RendererLightProbeUsage => rendererLightProbeUsage;
    public ReflectionProbeUsage RendererReflectionProbeUsage => rendererReflectionProbeUsage;
    public bool RendererReceiveShadows => rendererReceiveShadows;
    public RenderTextureFormat RenderTextureFormat => renderTextureFormat;
    public bool AllowHdr => allowHdr;
    public bool UseCameraMsaa => useCameraMsaa;
    public float CameraDistanceMultiplier => Mathf.Max(0.1f, cameraDistanceMultiplier);
    public float CameraDistanceOffset => Mathf.Max(0f, cameraDistanceOffset);
    public float NearClipPlane => Mathf.Max(0.01f, nearClipPlane);
    public float FarClipBoundsMultiplier => Mathf.Max(1f, farClipBoundsMultiplier);
    public float FarClipOffset => Mathf.Max(0f, farClipOffset);
    public bool EnableDirectSpecularLighting => enableDirectSpecularLighting;
    public bool EnableExposureControl => enableExposureControl;
    public bool EnableAtmosphericScattering => enableAtmosphericScattering;
    public bool EnablePostProcess => enablePostProcess;
    public bool EnableColorGrading => enableColorGrading;
    public bool EnableTonemapping => enableTonemapping;
    public bool EnableVolumetrics => enableVolumetrics;
    public bool EnableSkyReflection => enableSkyReflection;
    public bool UseFrontFillLight => useFrontFillLight;
    public float FrontFillLightIntensityMultiplier => Mathf.Max(0f, frontFillLightIntensityMultiplier);
    public Vector3 FrontFillLightCameraDirection => frontFillLightCameraDirection;
    public bool UseSideFillLight => useSideFillLight;
    public float SideFillLightIntensityMultiplier => Mathf.Max(0f, sideFillLightIntensityMultiplier);
    public Vector3 SideFillLightCameraDirection => sideFillLightCameraDirection;
    public bool UseRimLight => useRimLight;
    public float RimLightIntensityMultiplier => Mathf.Max(0f, rimLightIntensityMultiplier);
    public Vector3 RimLightCameraDirection => rimLightCameraDirection;
    public LightShadows GeneratedLightShadows => generatedLightShadows;
    public SkyAmbientMode SkyAmbientMode => skyAmbientMode;
    public RenderingSpace SkyRenderingSpace => skyRenderingSpace;
    public PhysicallyBasedSkyModel PhysicallyBasedSkyModel => physicallyBasedSkyModel;
    public Color PhysicallyBasedSkyGroundTint => physicallyBasedSkyGroundTint;
    public bool FogEnabled => fogEnabled;
    public FogColorMode FogColorMode => fogColorMode;
    public float MaxFogDistance => Mathf.Max(0f, maxFogDistance);
    public float MeanFreePath => Mathf.Max(0.0001f, meanFreePath);
    public bool EnableVolumetricFog => enableVolumetricFog;
    public float FogAnisotropy => Mathf.Clamp(fogAnisotropy, -1f, 1f);
    public ExposureMode ExposureMode => exposureMode;
    public float FixedExposure => fixedExposure;
    public float ExposureCompensation => exposureCompensation;
    public float ExposureLimitMin => exposureLimitMin;
    public float ExposureLimitMax => Mathf.Max(exposureLimitMin, exposureLimitMax);
    public Vector2 HistogramPercentages => new Vector2(
        Mathf.Clamp(histogramPercentages.x, 0f, 100f),
        Mathf.Clamp(histogramPercentages.y, 0f, 100f));
    public float PostExposure => postExposure;
    public float Contrast => contrast;
    public Color ColorFilter => colorFilter;
    public float HueShift => hueShift;
    public float Saturation => saturation;
    public TonemappingMode TonemappingMode => tonemappingMode;

    public int BuildHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + RenderLayer;
            hash = hash * 31 + RenderingLayer;
            hash = hash * 31 + HashVector(RenderOrigin);
            hash = hash * 31 + (ExcludeIconLayerFromSceneLights ? 1 : 0);
            hash = hash * 31 + (int)RendererLightProbeUsage;
            hash = hash * 31 + (int)RendererReflectionProbeUsage;
            hash = hash * 31 + (RendererReceiveShadows ? 1 : 0);
            hash = hash * 31 + (int)RenderTextureFormat;
            hash = hash * 31 + (AllowHdr ? 1 : 0);
            hash = hash * 31 + (UseCameraMsaa ? 1 : 0);
            hash = hash * 31 + Quantize(CameraDistanceMultiplier);
            hash = hash * 31 + Quantize(CameraDistanceOffset);
            hash = hash * 31 + Quantize(NearClipPlane);
            hash = hash * 31 + Quantize(FarClipBoundsMultiplier);
            hash = hash * 31 + Quantize(FarClipOffset);
            hash = hash * 31 + (EnableDirectSpecularLighting ? 1 : 0);
            hash = hash * 31 + (EnableExposureControl ? 1 : 0);
            hash = hash * 31 + (EnableAtmosphericScattering ? 1 : 0);
            hash = hash * 31 + (EnablePostProcess ? 1 : 0);
            hash = hash * 31 + (EnableColorGrading ? 1 : 0);
            hash = hash * 31 + (EnableTonemapping ? 1 : 0);
            hash = hash * 31 + (EnableVolumetrics ? 1 : 0);
            hash = hash * 31 + (EnableSkyReflection ? 1 : 0);
            hash = hash * 31 + (UseFrontFillLight ? 1 : 0);
            hash = hash * 31 + Quantize(FrontFillLightIntensityMultiplier);
            hash = hash * 31 + HashVector(FrontFillLightCameraDirection);
            hash = hash * 31 + (UseSideFillLight ? 1 : 0);
            hash = hash * 31 + Quantize(SideFillLightIntensityMultiplier);
            hash = hash * 31 + HashVector(SideFillLightCameraDirection);
            hash = hash * 31 + (UseRimLight ? 1 : 0);
            hash = hash * 31 + Quantize(RimLightIntensityMultiplier);
            hash = hash * 31 + HashVector(RimLightCameraDirection);
            hash = hash * 31 + (int)GeneratedLightShadows;
            hash = hash * 31 + (int)SkyAmbientMode;
            hash = hash * 31 + (int)SkyRenderingSpace;
            hash = hash * 31 + (int)PhysicallyBasedSkyModel;
            hash = hash * 31 + HashColor(PhysicallyBasedSkyGroundTint);
            hash = hash * 31 + (FogEnabled ? 1 : 0);
            hash = hash * 31 + (int)FogColorMode;
            hash = hash * 31 + Quantize(MaxFogDistance);
            hash = hash * 31 + Quantize(MeanFreePath);
            hash = hash * 31 + (EnableVolumetricFog ? 1 : 0);
            hash = hash * 31 + Quantize(FogAnisotropy);
            hash = hash * 31 + (int)ExposureMode;
            hash = hash * 31 + Quantize(FixedExposure);
            hash = hash * 31 + Quantize(ExposureCompensation);
            hash = hash * 31 + Quantize(ExposureLimitMin);
            hash = hash * 31 + Quantize(ExposureLimitMax);
            hash = hash * 31 + HashVector(HistogramPercentages);
            hash = hash * 31 + Quantize(PostExposure);
            hash = hash * 31 + Quantize(Contrast);
            hash = hash * 31 + HashColor(ColorFilter);
            hash = hash * 31 + Quantize(HueShift);
            hash = hash * 31 + Quantize(Saturation);
            hash = hash * 31 + (int)TonemappingMode;
            return hash;
        }
    }

    public static ItemIconGeneratorSettings LoadDefault()
    {
        if (defaultSettingsLoaded)
        {
            return defaultSettings;
        }

        defaultSettingsLoaded = true;
        defaultSettings = Resources.Load<ItemIconGeneratorSettings>(DefaultResourcePath);

        if (defaultSettings == null)
        {
            defaultSettings = CreateInstance<ItemIconGeneratorSettings>();
            defaultSettings.name = "Runtime ItemIconGeneratorSettings";
            Debug.LogWarning($"Item icon generator settings asset was not found at Resources/{DefaultResourcePath}. A runtime default will be used.");
        }

        return defaultSettings;
    }

    public static void ResetDefaultCache()
    {
        defaultSettingsLoaded = false;
        defaultSettings = null;
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
}
