using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[CreateAssetMenu(fileName = DEFAULT_RESOURCE_PATH, menuName = "Inventory/Item Icon Generator Settings")]
public class ItemIconGeneratorSettings : ScriptableObject
{
    public const string DEFAULT_RESOURCE_PATH = "ItemIconGeneratorSettings";

    private static ItemIconGeneratorSettings _defaultSettings;
    private static bool _defaultSettingsLoaded;

    [SerializeField] [HideInInspector] private List<ItemData> _prewarmItems = new();

    [Header("Prewarm")]
    [SerializeField] [Min(0.5f)] private float _prewarmFrameBudgetMilliseconds = 6f;

    [Header("Render Isolation")]
    [SerializeField] [Range(0, 31)] private int _renderLayer = 31;
    [SerializeField] [Range(0, 15)] private int _renderingLayer = 7;
    [SerializeField] private Vector3 _renderOrigin = Vector3.zero;
    [SerializeField] private bool _excludeIconLayerFromSceneLights = true;
    [SerializeField] private LightProbeUsage _rendererLightProbeUsage = LightProbeUsage.Off;
    [SerializeField] private ReflectionProbeUsage _rendererReflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
    [SerializeField] private bool _rendererReceiveShadows;

    [Header("Render Target")]
    [SerializeField] private RenderTextureFormat _renderTextureFormat = RenderTextureFormat.ARGBHalf;

    [Header("Camera Overrides")]
    [SerializeField] private bool _allowHdr = true;
    [SerializeField] private bool _useCameraMsaa = true;
    [SerializeField] [Min(0.1f)] private float _cameraDistanceMultiplier = 3f;
    [SerializeField] [Min(0f)] private float _cameraDistanceOffset = 1f;
    [SerializeField] [Min(0.01f)] private float _nearClipPlane = 0.01f;
    [SerializeField] [Min(1f)] private float _farClipBoundsMultiplier = 8f;
    [SerializeField] [Min(0f)] private float _farClipOffset = 10f;
    [SerializeField] private bool _enableDirectSpecularLighting = true;
    [SerializeField] private bool _enableExposureControl = true;
    [SerializeField] private bool _enableAtmosphericScattering = true;
    [SerializeField] private bool _enablePostProcess = true;
    [SerializeField] private bool _enableColorGrading = true;
    [SerializeField] private bool _enableTonemapping = true;
    [SerializeField] private bool _enableVolumetrics = true;
    [SerializeField] private bool _enableSkyReflection = true;

    [Header("Fill Lights")]
    [SerializeField] private bool _useFrontFillLight = true;
    [SerializeField] [Min(0f)] private float _frontFillLightIntensityMultiplier = 0.65f;
    [SerializeField] private Vector3 _frontFillLightCameraDirection = new Vector3(0f, 0.15f, 1f);
    [SerializeField] private bool _useSideFillLight = true;
    [SerializeField] [Min(0f)] private float _sideFillLightIntensityMultiplier = 0.5f;
    [SerializeField] private Vector3 _sideFillLightCameraDirection = new Vector3(-0.85f, 0.1f, 1f);
    [SerializeField] private bool _useRimLight = true;
    [SerializeField] [Min(0f)] private float _rimLightIntensityMultiplier = 0.25f;
    [SerializeField] private Vector3 _rimLightCameraDirection = new Vector3(0.35f, 0.2f, -1f);
    [SerializeField] private LightShadows _generatedLightShadows = LightShadows.None;

    [Header("Visual Environment")]
    [SerializeField] private SkyAmbientMode _skyAmbientMode = SkyAmbientMode.Dynamic;
    [SerializeField] private RenderingSpace _skyRenderingSpace = RenderingSpace.Camera;
    [SerializeField] private PhysicallyBasedSkyModel _physicallyBasedSkyModel = PhysicallyBasedSkyModel.EarthAdvanced;
    [SerializeField] private Color _physicallyBasedSkyGroundTint = new Color(0.122641504f, 0.1043775f, 0.09313812f, 1f);

    [Header("Fog")]
    [SerializeField] private bool _fogEnabled = true;
    [SerializeField] private FogColorMode _fogColorMode = FogColorMode.SkyColor;
    [SerializeField] [Min(0f)] private float _maxFogDistance = 5000f;
    [SerializeField] [Min(0.0001f)] private float _meanFreePath = 400f;
    [SerializeField] private bool _enableVolumetricFog = true;
    [SerializeField] [Range(-1f, 1f)] private float _fogAnisotropy = 0.65f;

    [Header("Exposure")]
    [SerializeField] private ExposureMode _exposureMode = ExposureMode.AutomaticHistogram;
    [SerializeField] private float _fixedExposure;
    [SerializeField] private float _exposureCompensation;
    [SerializeField] private float _exposureLimitMin = 2f;
    [SerializeField] private float _exposureLimitMax = 14f;
    [SerializeField] private Vector2 _histogramPercentages = new Vector2(40f, 90f);

    [Header("Color Adjustments")]
    [SerializeField] private float _postExposure;
    [SerializeField] [Range(-100f, 100f)] private float _contrast;
    [SerializeField] private Color _colorFilter = Color.white;
    [SerializeField] [Range(-180f, 180f)] private float _hueShift;
    [SerializeField] [Range(-100f, 100f)] private float _saturation;

    [Header("Tonemapping")]
    [SerializeField] private TonemappingMode _tonemappingMode = TonemappingMode.Neutral;

    public int RenderLayer => Mathf.Clamp(_renderLayer, 0, 31);
    public IReadOnlyList<ItemData> PrewarmItems => _prewarmItems;
    public float PrewarmFrameBudgetMilliseconds => Mathf.Max(0.5f, _prewarmFrameBudgetMilliseconds);
    public int RenderLayerMask => 1 << RenderLayer;
    public int RenderingLayer => Mathf.Clamp(_renderingLayer, 0, 15);
    public uint RenderingLayerMask => 1u << RenderingLayer;
    public Vector3 RenderOrigin => _renderOrigin;
    public bool ExcludeIconLayerFromSceneLights => _excludeIconLayerFromSceneLights;
    public LightProbeUsage RendererLightProbeUsage => _rendererLightProbeUsage;
    public ReflectionProbeUsage RendererReflectionProbeUsage => _rendererReflectionProbeUsage;
    public bool RendererReceiveShadows => _rendererReceiveShadows;
    public RenderTextureFormat RenderTextureFormat => _renderTextureFormat;
    public bool AllowHdr => _allowHdr;
    public bool UseCameraMsaa => _useCameraMsaa;
    public float CameraDistanceMultiplier => Mathf.Max(0.1f, _cameraDistanceMultiplier);
    public float CameraDistanceOffset => Mathf.Max(0f, _cameraDistanceOffset);
    public float NearClipPlane => Mathf.Max(0.01f, _nearClipPlane);
    public float FarClipBoundsMultiplier => Mathf.Max(1f, _farClipBoundsMultiplier);
    public float FarClipOffset => Mathf.Max(0f, _farClipOffset);
    public bool EnableDirectSpecularLighting => _enableDirectSpecularLighting;
    public bool EnableExposureControl => _enableExposureControl;
    public bool EnableAtmosphericScattering => _enableAtmosphericScattering;
    public bool EnablePostProcess => _enablePostProcess;
    public bool EnableColorGrading => _enableColorGrading;
    public bool EnableTonemapping => _enableTonemapping;
    public bool EnableVolumetrics => _enableVolumetrics;
    public bool EnableSkyReflection => _enableSkyReflection;
    public bool UseFrontFillLight => _useFrontFillLight;
    public float FrontFillLightIntensityMultiplier => Mathf.Max(0f, _frontFillLightIntensityMultiplier);
    public Vector3 FrontFillLightCameraDirection => _frontFillLightCameraDirection;
    public bool UseSideFillLight => _useSideFillLight;
    public float SideFillLightIntensityMultiplier => Mathf.Max(0f, _sideFillLightIntensityMultiplier);
    public Vector3 SideFillLightCameraDirection => _sideFillLightCameraDirection;
    public bool UseRimLight => _useRimLight;
    public float RimLightIntensityMultiplier => Mathf.Max(0f, _rimLightIntensityMultiplier);
    public Vector3 RimLightCameraDirection => _rimLightCameraDirection;
    public LightShadows GeneratedLightShadows => _generatedLightShadows;
    public SkyAmbientMode SkyAmbientMode => _skyAmbientMode;
    public RenderingSpace SkyRenderingSpace => _skyRenderingSpace;
    public PhysicallyBasedSkyModel PhysicallyBasedSkyModel => _physicallyBasedSkyModel;
    public Color PhysicallyBasedSkyGroundTint => _physicallyBasedSkyGroundTint;
    public bool FogEnabled => _fogEnabled;
    public FogColorMode FogColorMode => _fogColorMode;
    public float MaxFogDistance => Mathf.Max(0f, _maxFogDistance);
    public float MeanFreePath => Mathf.Max(0.0001f, _meanFreePath);
    public bool EnableVolumetricFog => _enableVolumetricFog;
    public float FogAnisotropy => Mathf.Clamp(_fogAnisotropy, -1f, 1f);
    public ExposureMode ExposureMode => _exposureMode;
    public float FixedExposure => _fixedExposure;
    public float ExposureCompensation => _exposureCompensation;
    public float ExposureLimitMin => _exposureLimitMin;
    public float ExposureLimitMax => Mathf.Max(_exposureLimitMin, _exposureLimitMax);
    public Vector2 HistogramPercentages => new(Mathf.Clamp(_histogramPercentages.x, 0f, 100f), Mathf.Clamp(_histogramPercentages.y, 0f, 100f));
    public float PostExposure => _postExposure;
    public float Contrast => _contrast;
    public Color ColorFilter => _colorFilter;
    public float HueShift => _hueShift;
    public float Saturation => _saturation;
    public TonemappingMode TonemappingMode => _tonemappingMode;

    public int BuildHash() => ItemIconGeneratorSettingsHashBuilder.BuildHash(this);

    public static ItemIconGeneratorSettings LoadDefault()
    {
        if (_defaultSettingsLoaded)
        {
            return _defaultSettings;
        }

        _defaultSettingsLoaded = true;
        _defaultSettings = Resources.Load<ItemIconGeneratorSettings>(DEFAULT_RESOURCE_PATH);

        if (_defaultSettings == null)
        {
            _defaultSettings = CreateInstance<ItemIconGeneratorSettings>();
            _defaultSettings.name = "Runtime ItemIconGeneratorSettings";
            Debug.LogWarning($"Item icon generator settings asset was not found at Resources/{DEFAULT_RESOURCE_PATH}. A runtime default will be used.");
        }

        return _defaultSettings;
    }

    public static void ResetDefaultCache()
    {
        _defaultSettingsLoaded = false;
        _defaultSettings = null;
    }

}
