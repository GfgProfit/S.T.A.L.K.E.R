using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public sealed class GraphicsSettingsMenuView : MonoBehaviour, IView<GraphicsSettingsViewModel>
{
    private static readonly List<string> ON_OFF_LABELS = new() { "Выкл.", "Вкл." };
    private static readonly List<string> QUALITY_LABELS = new() { "Низкое", "Среднее", "Высокое" };
    private static readonly List<string> QUALITY_WITH_OFF_LABELS = new() { "Выкл.", "Низкое", "Среднее", "Высокое" };
    private static readonly List<string> SHADOW_QUALITY_LABELS = new() { "Низкое", "Среднее", "Высокое", "Ультра" };
    private static readonly List<string> SCREEN_MODE_LABELS = new() { "Полноэкранный", "Полноэкранный в окне", "Развёрнутое окно", "В окне" };
    private static readonly List<string> AA_LABELS = new() { "Выкл.", "FXAA", "SMAA", "TAA" };
    private static readonly List<string> DLSS_MODE_LABELS = new() { "Качество", "Баланс", "Производительность", "Ультра производительность" };
    private static readonly List<string> FSR2_MODE_LABELS = new() { "Качество", "Баланс", "Производительность", "Ультра производительность" };
    private static readonly List<string> TEXTURE_QUALITY_LABELS = new() { "Ультра", "Высокое", "Среднее", "Низкое" };
    private static readonly List<string> ANISO_LABELS = new() { "Отключена", "По текстуре", "Принудительно" };
    private static readonly int[] FRAME_LIMIT_VALUES = { -1, 30, 60, 75, 90, 120, 144, 165, 240 };
    private static readonly int[] SCREEN_PERCENTAGE_VALUES = { 50, 60, 67, 75, 85, 100 };
    private static readonly float[] UPSCALE_SHARPNESS_VALUES = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    private static readonly int[] SHADOW_DISTANCE_VALUES = { 25, 50, 100, 150, 200 };
    private static readonly int[] SHADOW_CASCADE_VALUES = { 1, 2, 3, 4 };
    private static readonly float[] LOD_BIAS_VALUES = { 0.5f, 1f, 1.5f, 2f };
    private const float APPLY_DEBOUNCE_SECONDS = 0.25f;

    private static float _lastApplyRealtime = -1f;

    [Header("Screen")]
    [SerializeField] private HorizontalSelector _selDisplay;
    [SerializeField] private HorizontalSelector _selScreenMode;
    [SerializeField] private HorizontalSelector _selResolution;
    [SerializeField] private HorizontalSelector _selRefresh;
    [SerializeField] private HorizontalSelector _selVSync;
    [SerializeField] private HorizontalSelector _selFrameLimit;

    [Header("HDRP / Image")]
    [SerializeField] private HorizontalSelector _selAA;
    [SerializeField] private HorizontalSelector _selTAAQuality;
    [SerializeField] private HorizontalSelector _selSMAAQuality;
    [SerializeField] private HorizontalSelector _selDithering;

    [SerializeField] private HorizontalSelector _selDynamicResolution;
    [SerializeField] private HorizontalSelector _selScreenPercentage;
    [SerializeField] private HorizontalSelector _selUpscaler;
    [SerializeField] private HorizontalSelector _selDLSSMode;
    [SerializeField] private HorizontalSelector _selFSR2Mode;
    [SerializeField] private HorizontalSelector _selUpscaleSharpness;

    [Header("HDRP / Lighting")]
    [SerializeField] private HorizontalSelector _selSSAO;
    [SerializeField] private HorizontalSelector _selSSGI;
    [SerializeField] private HorizontalSelector _selSSR;
    [SerializeField] private HorizontalSelector _selTransparentSSR;
    [SerializeField] private HorizontalSelector _selContactShadows;
    [SerializeField] private HorizontalSelector _selScreenSpaceShadows;

    [Header("HDRP / Shadows")]
    [SerializeField] private HorizontalSelector _selShadowQuality;
    [SerializeField] private HorizontalSelector _selShadowRes;
    [SerializeField] private HorizontalSelector _selShadowDist;
    [SerializeField] private HorizontalSelector _selShadowCasc;
    [SerializeField] private HorizontalSelector _selShadowFiltering;

    [Header("HDRP / Atmosphere")]
    [SerializeField] private HorizontalSelector _selFog;
    [SerializeField] private HorizontalSelector _selVolumetricFog;
    [SerializeField] private HorizontalSelector _selVolumetricQuality;
    [SerializeField] private HorizontalSelector _selVolumetricClouds;

    [Header("HDRP / Post FX")]
    [SerializeField] private HorizontalSelector _selMotionBlur;
    [SerializeField] private HorizontalSelector _selVignette;

    [Header("Unity Quality")]
    [SerializeField] private HorizontalSelector _selTexQuality;
    [SerializeField] private HorizontalSelector _selAniso;
    [SerializeField] private HorizontalSelector _selLodBias;

    [Header("Apply")]
    [SerializeField] private CustomButton _customBtnApply;

    [Header("HDRP References")]
    [SerializeField] private Volume _globalVolume;
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private HDAdditionalCameraData _cameraData;

    private readonly HashSet<string> _warnings = new();
    private readonly List<int> _upscalerOptionValues = new();
    private GraphicsSettingsViewModel _viewModel;
    private GraphicsSettingsApplier _applier;
    private List<Resolution> _resolutions = new();
    private List<RefreshRate> _refreshRates = new();
    private bool _selectorEventsBound;
    private bool _isInitialized;
    private bool _ownsViewModel;
    private bool _applyButtonBound;
    private bool _isApplying;

    private void OnEnable()
    {
        BindApplyButton();

        if (_viewModel != null && _isInitialized)
        {
            DiscardPendingGraphicsSettings();
        }
    }

    private void Start()
    {
        if (_viewModel == null)
        {
            Bind(new GraphicsSettingsViewModel());
            _ownsViewModel = true;
        }

        InitializeGraphicsSettings();
        BindApplyButton();
    }

    private void OnDisable()
    {
        UnbindApplyButton();
        UnbindSelectorEvents();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(GraphicsSettingsViewModel viewModel)
    {
        Unbind();
        _viewModel = viewModel;
        _applier = new GraphicsSettingsApplier();
        _ownsViewModel = false;
    }

    public void Unbind()
    {
        UnbindApplyButton();
        UnbindSelectorEvents();
        if (_ownsViewModel)
        {
            _viewModel?.Dispose();
        }

        _viewModel = null;
        _applier = null;
        _isInitialized = false;
        _ownsViewModel = false;
        _applyButtonBound = false;
        _isApplying = false;
    }

    public void InitializeGraphicsSettings()
    {
        if (_viewModel == null)
        {
            return;
        }

        LoadGraphicsSettings();
        SetupSelectors(_viewModel.Current);
        BindSelectorEvents();
        ApplyGraphicsSettings(_viewModel.Current);
        _isInitialized = true;
        BindApplyButton();
    }

    public void LoadGraphicsSettings()
    {
        _viewModel?.LoadGraphicsSettings();
    }

    public bool SaveGraphicsSettings(GraphicsData data)
    {
        return GraphicsSettingsStorage.SaveGraphicsSettings(data);
    }

    public bool ApplyGraphicsSettings(GraphicsData data)
    {
        if (_applier == null)
        {
            _applier = new GraphicsSettingsApplier();
        }

        return _applier.ApplyGraphicsSettings(data, _targetCamera, _cameraData, _globalVolume);
    }

    public void ApplyPendingGraphicsSettings()
    {
        if (_isApplying || IsApplyDebounced())
        {
            return;
        }

        if (_viewModel == null)
        {
            Debug.LogWarning($"{nameof(GraphicsSettingsMenuView)}: settings are not initialized. Apply was ignored.");
            return;
        }

        _isApplying = true;

        try
        {
            GraphicsData pendingData = BuildPendingGraphicsDataFromSelectors();
            _viewModel.SetPendingGraphicsData(pendingData);

            if (ApplyGraphicsSettings(pendingData) == false)
            {
                Debug.LogWarning($"{nameof(GraphicsSettingsMenuView)}: graphics settings were not saved because applying failed.");
                return;
            }

            if (SaveGraphicsSettings(pendingData) == false)
            {
                return;
            }

            _viewModel.ApplyPendingGraphicsSettings();
            SetupSelectors(_viewModel.Current);
            BindSelectorEvents();
        }
        finally
        {
            _isApplying = false;
        }
    }

    public void DiscardPendingGraphicsSettings()
    {
        if (_viewModel == null || _viewModel.Current == null)
        {
            return;
        }

        _viewModel.DiscardPendingGraphicsSettings();
        SetupSelectors(_viewModel.Current);
        BindSelectorEvents();
    }

    public string GetGraphicsSavePath()
    {
        return GraphicsSettingsStorage.GetGraphicsSavePath();
    }

    public GraphicsData CreateDefaultGraphicsData()
    {
        return GraphicsSettingsStorage.CreateDefaultGraphicsData();
    }

    public GraphicsData BuildPendingGraphicsDataFromSelectors()
    {
        GraphicsData data = _viewModel?.Pending?.Clone() ?? CreateDefaultGraphicsData();
        data.displayIndex = GetSelectorIndex(_selDisplay);
        data.screenMode = GetSelectorIndex(_selScreenMode);
        data.resolutionIndex = GetSelectorIndex(_selResolution);
        data.refreshRateIndex = GetSelectorIndex(_selRefresh);
        data.vSync = GetSelectorIndex(_selVSync);
        data.frameLimit = GetArrayValue(FRAME_LIMIT_VALUES, GetSelectorIndex(_selFrameLimit));
        data.antiAliasing = GetSelectorIndex(_selAA);
        data.taaQuality = GetSelectorIndex(_selTAAQuality);
        data.smaaQuality = GetSelectorIndex(_selSMAAQuality);
        data.dithering = GetSelectorIndex(_selDithering) == 1;
        data.dynamicResolution = GetSelectorIndex(_selDynamicResolution) == 1;
        data.screenPercentage = GetArrayValue(SCREEN_PERCENTAGE_VALUES, GetSelectorIndex(_selScreenPercentage));
        data.upscaler = GetUpscalerValue(GetSelectorIndex(_selUpscaler));
        data.dlssMode = GetSelectorIndex(_selDLSSMode);
        data.fsr2Mode = GetSelectorIndex(_selFSR2Mode);
        data.upscaleSharpness = GetArrayValue(UPSCALE_SHARPNESS_VALUES, GetSelectorIndex(_selUpscaleSharpness));
        data.ssao = GetSelectorIndex(_selSSAO);
        data.ssgi = GetSelectorIndex(_selSSGI);
        data.ssr = GetSelectorIndex(_selSSR);
        data.transparentSSR = GetSelectorIndex(_selTransparentSSR) == 1;
        data.contactShadows = GetSelectorIndex(_selContactShadows);
        data.screenSpaceShadows = GetSelectorIndex(_selScreenSpaceShadows) == 1;
        data.shadowQuality = GetSelectorIndex(_selShadowQuality);
        data.shadowResolution = GetSelectorIndex(_selShadowRes);
        data.shadowDistance = GetArrayValue(SHADOW_DISTANCE_VALUES, GetSelectorIndex(_selShadowDist));
        data.shadowCascades = GetArrayValue(SHADOW_CASCADE_VALUES, GetSelectorIndex(_selShadowCasc));
        data.shadowFiltering = GetSelectorIndex(_selShadowFiltering);
        data.fog = GetSelectorIndex(_selFog);
        data.volumetricFog = GetSelectorIndex(_selVolumetricFog) == 1;
        data.volumetricQuality = GetSelectorIndex(_selVolumetricQuality);
        data.volumetricClouds = GetSelectorIndex(_selVolumetricClouds);
        data.motionBlur = GetSelectorIndex(_selMotionBlur);
        data.vignette = GetSelectorIndex(_selVignette);
        data.textureQuality = GetSelectorIndex(_selTexQuality);
        data.anisotropicFiltering = GetSelectorIndex(_selAniso);
        data.lodBias = GetArrayValue(LOD_BIAS_VALUES, GetSelectorIndex(_selLodBias));
        GraphicsSettingsApplier.NormalizeUpscalerSettings(data);
        return data;
    }

    private void SetupSelectors(GraphicsData data)
    {
        if (data == null)
        {
            return;
        }

        UnbindSelectorEvents();
        SetupSelector(_selDisplay, BuildDisplayOptions(), data.displayIndex, nameof(_selDisplay));
        SetupSelector(_selScreenMode, SCREEN_MODE_LABELS, data.screenMode, nameof(_selScreenMode));
        SetupResolutionSelector(data);
        SetupRefreshSelector(data);
        SetupSelector(_selVSync, BuildVSyncOptions(), data.vSync, nameof(_selVSync));
        SetupSelector(_selFrameLimit, BuildIntOptions(FRAME_LIMIT_VALUES, "Без ограничений"), GetArrayIndex(FRAME_LIMIT_VALUES, data.frameLimit), nameof(_selFrameLimit));
        SetupSelector(_selAA, AA_LABELS, data.antiAliasing, nameof(_selAA));
        SetupSelector(_selTAAQuality, QUALITY_LABELS, data.taaQuality, nameof(_selTAAQuality));
        SetupSelector(_selSMAAQuality, QUALITY_LABELS, data.smaaQuality, nameof(_selSMAAQuality));
        SetupSelector(_selDithering, ON_OFF_LABELS, data.dithering ? 1 : 0, nameof(_selDithering));
        SetupSelector(_selDynamicResolution, ON_OFF_LABELS, data.dynamicResolution ? 1 : 0, nameof(_selDynamicResolution));
        SetupSelector(_selScreenPercentage, BuildPercentOptions(SCREEN_PERCENTAGE_VALUES), GetArrayIndex(SCREEN_PERCENTAGE_VALUES, data.screenPercentage), nameof(_selScreenPercentage));
        SetupUpscalerSelector(data);
        SetupSelector(_selDLSSMode, DLSS_MODE_LABELS, data.dlssMode, nameof(_selDLSSMode));
        SetupSelector(_selFSR2Mode, FSR2_MODE_LABELS, data.fsr2Mode, nameof(_selFSR2Mode));
        SetupSelector(_selUpscaleSharpness, BuildPercentOptions(UPSCALE_SHARPNESS_VALUES), GetArrayIndex(UPSCALE_SHARPNESS_VALUES, data.upscaleSharpness), nameof(_selUpscaleSharpness));
        SetupSelector(_selSSAO, QUALITY_WITH_OFF_LABELS, data.ssao, nameof(_selSSAO));
        SetupSelector(_selSSGI, QUALITY_WITH_OFF_LABELS, data.ssgi, nameof(_selSSGI));
        SetupSelector(_selSSR, QUALITY_WITH_OFF_LABELS, data.ssr, nameof(_selSSR));
        SetupSelector(_selTransparentSSR, ON_OFF_LABELS, data.transparentSSR ? 1 : 0, nameof(_selTransparentSSR));
        SetupSelector(_selContactShadows, QUALITY_WITH_OFF_LABELS, data.contactShadows, nameof(_selContactShadows));
        SetupSelector(_selScreenSpaceShadows, ON_OFF_LABELS, data.screenSpaceShadows ? 1 : 0, nameof(_selScreenSpaceShadows));
        SetupSelector(_selShadowQuality, SHADOW_QUALITY_LABELS, data.shadowQuality, nameof(_selShadowQuality));
        SetupSelector(_selShadowRes, SHADOW_QUALITY_LABELS, data.shadowResolution, nameof(_selShadowRes));
        SetupSelector(_selShadowDist, BuildMetersOptions(SHADOW_DISTANCE_VALUES), GetArrayIndex(SHADOW_DISTANCE_VALUES, data.shadowDistance), nameof(_selShadowDist));
        SetupSelector(_selShadowCasc, BuildIntOptions(SHADOW_CASCADE_VALUES), GetArrayIndex(SHADOW_CASCADE_VALUES, data.shadowCascades), nameof(_selShadowCasc));
        SetupSelector(_selShadowFiltering, QUALITY_LABELS, data.shadowFiltering, nameof(_selShadowFiltering));
        SetupSelector(_selFog, QUALITY_WITH_OFF_LABELS, data.fog, nameof(_selFog));
        SetupSelector(_selVolumetricFog, ON_OFF_LABELS, data.volumetricFog ? 1 : 0, nameof(_selVolumetricFog));
        SetupSelector(_selVolumetricQuality, QUALITY_LABELS, data.volumetricQuality, nameof(_selVolumetricQuality));
        SetupSelector(_selVolumetricClouds, QUALITY_WITH_OFF_LABELS, data.volumetricClouds, nameof(_selVolumetricClouds));
        SetupSelector(_selMotionBlur, QUALITY_WITH_OFF_LABELS, data.motionBlur, nameof(_selMotionBlur));
        SetupSelector(_selVignette, QUALITY_WITH_OFF_LABELS, data.vignette, nameof(_selVignette));
        SetupSelector(_selTexQuality, TEXTURE_QUALITY_LABELS, data.textureQuality, nameof(_selTexQuality));
        SetupSelector(_selAniso, ANISO_LABELS, data.anisotropicFiltering, nameof(_selAniso));
        SetupSelector(_selLodBias, BuildFloatOptions(LOD_BIAS_VALUES), GetArrayIndex(LOD_BIAS_VALUES, data.lodBias), nameof(_selLodBias));
    }

    private void SetupResolutionSelector(GraphicsData data)
    {
        _resolutions = GraphicsSettingsApplier.GetResolutionList();
        List<string> labels = new();

        for (int i = 0; i < _resolutions.Count; i++)
        {
            labels.Add(GraphicsSettingsApplier.GetResolutionLabel(_resolutions[i]));
        }

        SetupSelector(_selResolution, labels, data.resolutionIndex, nameof(_selResolution));
    }

    private void SetupRefreshSelector(GraphicsData data)
    {
        int resolutionIndex = Mathf.Clamp(data.resolutionIndex, 0, Mathf.Max(0, _resolutions.Count - 1));
        Resolution resolution = _resolutions.Count > 0 ? _resolutions[resolutionIndex] : Screen.currentResolution;
        _refreshRates = GraphicsSettingsApplier.GetRefreshRates(resolution);
        List<string> labels = new();

        for (int i = 0; i < _refreshRates.Count; i++)
        {
            labels.Add(GraphicsSettingsApplier.GetRefreshRateLabel(_refreshRates[i]));
        }

        SetupSelector(_selRefresh, labels, data.refreshRateIndex, nameof(_selRefresh));
    }

    private void SetupUpscalerSelector(GraphicsData data)
    {
        _upscalerOptionValues.Clear();
        List<string> labels = new();
        AddUpscalerOption(labels, GraphicsUpscalerMode.Off, "Выключено");
        AddUpscalerOption(labels, GraphicsUpscalerMode.CatmullRom, "Catmull-Rom");
        AddUpscalerOption(labels, GraphicsUpscalerMode.CAS, "CAS");
        AddUpscalerOption(labels, GraphicsUpscalerMode.FSR1, "FSR1");
        AddUpscalerOption(labels, GraphicsUpscalerMode.TAAU, "TAAU");
        AddUpscalerOption(labels, GraphicsUpscalerMode.DLSS, "DLSS");
        AddUpscalerOption(labels, GraphicsUpscalerMode.FSR2, "FSR2");
        AddUpscalerOption(labels, GraphicsUpscalerMode.STP, "STP");
        SetupSelector(_selUpscaler, labels, GetUpscalerIndex(data.upscaler), nameof(_selUpscaler));
    }

    private void AddUpscalerOption(List<string> labels, GraphicsUpscalerMode mode, string label)
    {
        if (GraphicsSettingsApplier.IsUpscalerAvailable(mode) == false)
        {
            return;
        }

        _upscalerOptionValues.Add((int)mode);
        labels.Add(label);
    }

    private void BindSelectorEvents()
    {
        if (_selectorEventsBound)
        {
            UnbindSelectorEvents();
        }

        Subscribe(_selDisplay, OnDisplayChanged);
        Subscribe(_selScreenMode, OnScreenModeChanged);
        Subscribe(_selResolution, OnResolutionChanged);
        Subscribe(_selRefresh, OnRefreshChanged);
        Subscribe(_selVSync, OnVSyncChanged);
        Subscribe(_selFrameLimit, OnFrameLimitChanged);
        Subscribe(_selAA, OnAAChanged);
        Subscribe(_selTAAQuality, OnTAAQualityChanged);
        Subscribe(_selSMAAQuality, OnSMAAQualityChanged);
        Subscribe(_selDithering, OnDitheringChanged);
        Subscribe(_selDynamicResolution, OnDynamicResolutionChanged);
        Subscribe(_selScreenPercentage, OnScreenPercentageChanged);
        Subscribe(_selUpscaler, OnUpscalerChanged);
        Subscribe(_selDLSSMode, OnDLSSModeChanged);
        Subscribe(_selFSR2Mode, OnFSR2ModeChanged);
        Subscribe(_selUpscaleSharpness, OnUpscaleSharpnessChanged);
        Subscribe(_selSSAO, OnSSAOChanged);
        Subscribe(_selSSGI, OnSSGIChanged);
        Subscribe(_selSSR, OnSSRChanged);
        Subscribe(_selTransparentSSR, OnTransparentSSRChanged);
        Subscribe(_selContactShadows, OnContactShadowsChanged);
        Subscribe(_selScreenSpaceShadows, OnScreenSpaceShadowsChanged);
        Subscribe(_selShadowQuality, OnShadowQualityChanged);
        Subscribe(_selShadowRes, OnShadowResChanged);
        Subscribe(_selShadowDist, OnShadowDistChanged);
        Subscribe(_selShadowCasc, OnShadowCascChanged);
        Subscribe(_selShadowFiltering, OnShadowFilteringChanged);
        Subscribe(_selFog, OnFogChanged);
        Subscribe(_selVolumetricFog, OnVolumetricFogChanged);
        Subscribe(_selVolumetricQuality, OnVolumetricQualityChanged);
        Subscribe(_selVolumetricClouds, OnVolumetricCloudsChanged);
        Subscribe(_selMotionBlur, OnMotionBlurChanged);
        Subscribe(_selVignette, OnVignetteChanged);
        Subscribe(_selTexQuality, OnTexQualityChanged);
        Subscribe(_selAniso, OnAnisoChanged);
        Subscribe(_selLodBias, OnLodBiasChanged);
        _selectorEventsBound = true;
    }

    private void UnbindSelectorEvents()
    {
        if (_selectorEventsBound == false)
        {
            return;
        }

        Unsubscribe(_selDisplay, OnDisplayChanged);
        Unsubscribe(_selScreenMode, OnScreenModeChanged);
        Unsubscribe(_selResolution, OnResolutionChanged);
        Unsubscribe(_selRefresh, OnRefreshChanged);
        Unsubscribe(_selVSync, OnVSyncChanged);
        Unsubscribe(_selFrameLimit, OnFrameLimitChanged);
        Unsubscribe(_selAA, OnAAChanged);
        Unsubscribe(_selTAAQuality, OnTAAQualityChanged);
        Unsubscribe(_selSMAAQuality, OnSMAAQualityChanged);
        Unsubscribe(_selDithering, OnDitheringChanged);
        Unsubscribe(_selDynamicResolution, OnDynamicResolutionChanged);
        Unsubscribe(_selScreenPercentage, OnScreenPercentageChanged);
        Unsubscribe(_selUpscaler, OnUpscalerChanged);
        Unsubscribe(_selDLSSMode, OnDLSSModeChanged);
        Unsubscribe(_selFSR2Mode, OnFSR2ModeChanged);
        Unsubscribe(_selUpscaleSharpness, OnUpscaleSharpnessChanged);
        Unsubscribe(_selSSAO, OnSSAOChanged);
        Unsubscribe(_selSSGI, OnSSGIChanged);
        Unsubscribe(_selSSR, OnSSRChanged);
        Unsubscribe(_selTransparentSSR, OnTransparentSSRChanged);
        Unsubscribe(_selContactShadows, OnContactShadowsChanged);
        Unsubscribe(_selScreenSpaceShadows, OnScreenSpaceShadowsChanged);
        Unsubscribe(_selShadowQuality, OnShadowQualityChanged);
        Unsubscribe(_selShadowRes, OnShadowResChanged);
        Unsubscribe(_selShadowDist, OnShadowDistChanged);
        Unsubscribe(_selShadowCasc, OnShadowCascChanged);
        Unsubscribe(_selShadowFiltering, OnShadowFilteringChanged);
        Unsubscribe(_selFog, OnFogChanged);
        Unsubscribe(_selVolumetricFog, OnVolumetricFogChanged);
        Unsubscribe(_selVolumetricQuality, OnVolumetricQualityChanged);
        Unsubscribe(_selVolumetricClouds, OnVolumetricCloudsChanged);
        Unsubscribe(_selMotionBlur, OnMotionBlurChanged);
        Unsubscribe(_selVignette, OnVignetteChanged);
        Unsubscribe(_selTexQuality, OnTexQualityChanged);
        Unsubscribe(_selAniso, OnAnisoChanged);
        Unsubscribe(_selLodBias, OnLodBiasChanged);
        _selectorEventsBound = false;
    }

    private void BindApplyButton()
    {
        if (_customBtnApply == null)
        {
            WarnOnce("Apply button is not assigned. Graphics settings can be changed in UI but cannot be applied from this view.");
            return;
        }

        if (_applyButtonBound)
        {
            return;
        }

        _customBtnApply.OnClick.RemoveListener(ApplyPendingGraphicsSettings);
        _customBtnApply.OnClick.AddListener(ApplyPendingGraphicsSettings);
        _applyButtonBound = true;
    }

    private void UnbindApplyButton()
    {
        if (_customBtnApply != null && _applyButtonBound)
        {
            _customBtnApply.OnClick.RemoveListener(ApplyPendingGraphicsSettings);
        }

        _applyButtonBound = false;
    }

    private bool IsApplyDebounced()
    {
        float realtime = Time.realtimeSinceStartup;

        if (_lastApplyRealtime >= 0f && realtime - _lastApplyRealtime < APPLY_DEBOUNCE_SECONDS)
        {
            return true;
        }

        _lastApplyRealtime = realtime;
        return false;
    }

    private void Subscribe(HorizontalSelector selector, Action<int> handler)
    {
        if (selector != null)
        {
            selector.IndexChanged += handler;
        }
    }

    private void Unsubscribe(HorizontalSelector selector, Action<int> handler)
    {
        if (selector != null)
        {
            selector.IndexChanged -= handler;
        }
    }

    private void SetupSelector(HorizontalSelector selector, List<string> labels, int index, string selectorName)
    {
        if (selector == null)
        {
            WarnOnce($"{selectorName} is not assigned.");
            return;
        }

        selector.Setup(labels);
        selector.SetIndex(index, false);
    }

    private void UpdatePending(Action<GraphicsData> update)
    {
        if (_viewModel == null)
        {
            return;
        }

        GraphicsData data = _viewModel.Pending?.Clone() ?? CreateDefaultGraphicsData();
        update?.Invoke(data);
        _viewModel.SetPendingGraphicsData(data);
    }

    private void OnDisplayChanged(int index)
    {
        UpdatePending(data => data.displayIndex = index);
    }

    private void OnScreenModeChanged(int index)
    {
        UpdatePending(data => data.screenMode = index);
    }

    private void OnResolutionChanged(int index)
    {
        UpdatePending(data =>
        {
            data.resolutionIndex = index;
            data.refreshRateIndex = 0;
            SetupRefreshSelector(data);
        });
    }

    private void OnRefreshChanged(int index)
    {
        UpdatePending(data => data.refreshRateIndex = index);
    }

    private void OnVSyncChanged(int index)
    {
        UpdatePending(data => data.vSync = index);
    }

    private void OnFrameLimitChanged(int index)
    {
        UpdatePending(data => data.frameLimit = GetArrayValue(FRAME_LIMIT_VALUES, index));
    }

    private void OnAAChanged(int index)
    {
        UpdatePending(data => data.antiAliasing = index);
    }

    private void OnTAAQualityChanged(int index)
    {
        UpdatePending(data => data.taaQuality = index);
    }

    private void OnSMAAQualityChanged(int index)
    {
        UpdatePending(data => data.smaaQuality = index);
    }

    private void OnDitheringChanged(int index)
    {
        UpdatePending(data => data.dithering = index == 1);
    }

    private void OnDynamicResolutionChanged(int index)
    {
        UpdatePending(data => data.dynamicResolution = index == 1);
    }

    private void OnScreenPercentageChanged(int index)
    {
        UpdatePending(data => data.screenPercentage = GetArrayValue(SCREEN_PERCENTAGE_VALUES, index));
    }

    private void OnUpscalerChanged(int index)
    {
        UpdatePending(data =>
        {
            data.upscaler = GetUpscalerValue(index);
            GraphicsSettingsApplier.NormalizeUpscalerSettings(data);
        });
    }

    private void OnDLSSModeChanged(int index)
    {
        UpdatePending(data => data.dlssMode = index);
    }

    private void OnFSR2ModeChanged(int index)
    {
        UpdatePending(data => data.fsr2Mode = index);
    }

    private void OnUpscaleSharpnessChanged(int index)
    {
        UpdatePending(data => data.upscaleSharpness = GetArrayValue(UPSCALE_SHARPNESS_VALUES, index));
    }

    private void OnSSAOChanged(int index)
    {
        UpdatePending(data => data.ssao = index);
    }

    private void OnSSGIChanged(int index)
    {
        UpdatePending(data => data.ssgi = index);
    }

    private void OnSSRChanged(int index)
    {
        UpdatePending(data => data.ssr = index);
    }

    private void OnTransparentSSRChanged(int index)
    {
        UpdatePending(data => data.transparentSSR = index == 1);
    }

    private void OnContactShadowsChanged(int index)
    {
        UpdatePending(data => data.contactShadows = index);
    }

    private void OnScreenSpaceShadowsChanged(int index)
    {
        UpdatePending(data => data.screenSpaceShadows = index == 1);
    }

    private void OnShadowQualityChanged(int index)
    {
        UpdatePending(data => data.shadowQuality = index);
    }

    private void OnShadowResChanged(int index)
    {
        UpdatePending(data => data.shadowResolution = index);
    }

    private void OnShadowDistChanged(int index)
    {
        UpdatePending(data => data.shadowDistance = GetArrayValue(SHADOW_DISTANCE_VALUES, index));
    }

    private void OnShadowCascChanged(int index)
    {
        UpdatePending(data => data.shadowCascades = GetArrayValue(SHADOW_CASCADE_VALUES, index));
    }

    private void OnShadowFilteringChanged(int index)
    {
        UpdatePending(data => data.shadowFiltering = index);
    }

    private void OnFogChanged(int index)
    {
        UpdatePending(data => data.fog = index);
    }

    private void OnVolumetricFogChanged(int index)
    {
        UpdatePending(data => data.volumetricFog = index == 1);
    }

    private void OnVolumetricQualityChanged(int index)
    {
        UpdatePending(data => data.volumetricQuality = index);
    }

    private void OnVolumetricCloudsChanged(int index)
    {
        UpdatePending(data => data.volumetricClouds = index);
    }

    private void OnMotionBlurChanged(int index)
    {
        UpdatePending(data => data.motionBlur = index);
    }

    private void OnVignetteChanged(int index)
    {
        UpdatePending(data => data.vignette = index);
    }

    private void OnTexQualityChanged(int index)
    {
        UpdatePending(data => data.textureQuality = index);
    }

    private void OnAnisoChanged(int index)
    {
        UpdatePending(data => data.anisotropicFiltering = index);
    }

    private void OnLodBiasChanged(int index)
    {
        UpdatePending(data => data.lodBias = GetArrayValue(LOD_BIAS_VALUES, index));
    }

    private List<string> BuildDisplayOptions()
    {
        int count = Mathf.Max(1, Display.displays.Length);
        List<string> labels = new();

        for (int i = 0; i < count; i++)
        {
            labels.Add($"Монитор: {i + 1}");
        }

        return labels;
    }

    private List<string> BuildVSyncOptions()
    {
        return new List<string> { "Выключена", "Включена", "Половина" };
    }

    private List<string> BuildIntOptions(int[] values, string firstLabel = null)
    {
        List<string> labels = new();

        for (int i = 0; i < values.Length; i++)
        {
            labels.Add(i == 0 && string.IsNullOrEmpty(firstLabel) == false ? firstLabel : values[i].ToString());
        }

        return labels;
    }

    private List<string> BuildMetersOptions(int[] values)
    {
        List<string> labels = new();

        for (int i = 0; i < values.Length; i++)
        {
            labels.Add($"{values[i]} м");
        }

        return labels;
    }

    private List<string> BuildPercentOptions(int[] values)
    {
        List<string> labels = new();

        for (int i = 0; i < values.Length; i++)
        {
            labels.Add($"{values[i]}%");
        }

        return labels;
    }

    private List<string> BuildPercentOptions(float[] values)
    {
        List<string> labels = new();

        for (int i = 0; i < values.Length; i++)
        {
            labels.Add($"{Mathf.RoundToInt(values[i] * 100f)}%");
        }

        return labels;
    }

    private List<string> BuildFloatOptions(float[] values)
    {
        List<string> labels = new();

        for (int i = 0; i < values.Length; i++)
        {
            labels.Add(values[i].ToString("0.##"));
        }

        return labels;
    }

    private int GetSelectorIndex(HorizontalSelector selector)
    {
        return selector != null ? selector.Index : 0;
    }

    private int GetUpscalerValue(int selectorIndex)
    {
        if (_upscalerOptionValues.Count == 0)
        {
            return (int)GraphicsUpscalerMode.Off;
        }

        return _upscalerOptionValues[Mathf.Clamp(selectorIndex, 0, _upscalerOptionValues.Count - 1)];
    }

    private int GetUpscalerIndex(int value)
    {
        for (int i = 0; i < _upscalerOptionValues.Count; i++)
        {
            if (_upscalerOptionValues[i] == value)
            {
                return i;
            }
        }

        return 0;
    }

    private int GetArrayIndex(int[] values, int value)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == value)
            {
                return i;
            }
        }

        return 0;
    }

    private int GetArrayIndex(float[] values, float value)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (Mathf.Approximately(values[i], value))
            {
                return i;
            }
        }

        return 0;
    }

    private int GetArrayValue(int[] values, int index)
    {
        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private float GetArrayValue(float[] values, int index)
    {
        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private void WarnOnce(string message)
    {
        if (_warnings.Add(message))
        {
            Debug.LogWarning(message);
        }
    }
}
