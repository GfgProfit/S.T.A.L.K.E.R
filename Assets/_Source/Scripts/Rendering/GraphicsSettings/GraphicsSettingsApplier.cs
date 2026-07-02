using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public sealed class GraphicsSettingsApplier
{
    private const int SCREEN_PERCENTAGE_MIN = 50;
    private const int SCREEN_PERCENTAGE_MAX = 100;
    private const int QUALITY_LOW = 0;
    private const int QUALITY_MEDIUM = 1;
    private const int QUALITY_HIGH = 2;
    private const int DEFAULT_UPSCALE_PERCENTAGE = 75;

    private static readonly uint[] DLSS_QUALITY_VALUES = { 2u, 1u, 0u, 3u, 4u };
    private static readonly uint[] FSR2_QUALITY_VALUES = { 0u, 1u, 2u, 3u };

    private static bool _hasRequestedResolution;
    private static int _requestedResolutionWidth;
    private static int _requestedResolutionHeight;
    private static uint _requestedRefreshRateNumerator;
    private static uint _requestedRefreshRateDenominator;
    private static FullScreenMode _requestedFullScreenMode;

    private readonly HashSet<string> _warnings = new();
    private readonly bool _logMissingReferences;

    public GraphicsSettingsApplier(bool logMissingReferences = true)
    {
        _logMissingReferences = logMissingReferences;
    }

    public bool ApplyGraphicsSettings(GraphicsData data, Camera targetCamera = null, HDAdditionalCameraData cameraData = null, Volume globalVolume = null)
    {
        if (data == null)
        {
            WarnOnce("Graphics settings data is null.");
            return false;
        }

        NormalizeUpscalerSettings(data);
        ApplyScreenSettings(data);
        ApplyImageSettings(data, targetCamera, cameraData);
        ApplyLightingSettings(data, cameraData, globalVolume);
        ApplyShadowSettings(data, cameraData);
        ApplyAtmosphereSettings(data, cameraData, globalVolume);
        ApplyPostFxSettings(data, cameraData, globalVolume);
        ApplyUnityQualitySettings(data);
        return true;
    }

    public void ApplyScreenSettings(GraphicsData data)
    {
        if (data == null)
        {
            return;
        }

        int displayIndex = Mathf.Clamp(data.displayIndex, 0, Mathf.Max(0, Display.displays.Length - 1));

        if (displayIndex > 0)
        {
            Display display = Display.displays[displayIndex];

            if (display.active == false)
            {
                display.Activate();
            }

            WarnOnce("Display selection can activate additional displays, but moving the main game window to another monitor is platform-dependent.");
        }

        QualitySettings.vSyncCount = Mathf.Clamp(data.vSync, 0, 2);
        Application.targetFrameRate = QualitySettings.vSyncCount > 0 ? -1 : data.frameLimit;

        List<Resolution> resolutions = GetResolutionList();
        Resolution resolution = resolutions[Mathf.Clamp(data.resolutionIndex, 0, resolutions.Count - 1)];
        List<RefreshRate> refreshRates = GetRefreshRates(resolution);
        RefreshRate refreshRate = refreshRates[Mathf.Clamp(data.refreshRateIndex, 0, refreshRates.Count - 1)];
        FullScreenMode fullScreenMode = GetFullScreenMode(data);

        if (ShouldSetResolution(resolution, refreshRate, fullScreenMode))
        {
            Screen.SetResolution(resolution.width, resolution.height, fullScreenMode, refreshRate);
        }
    }

    public void ApplyImageSettings(GraphicsData data, Camera targetCamera, HDAdditionalCameraData cameraData)
    {
        if (data == null)
        {
            return;
        }

        GraphicsUpscalerMode upscalerMode = GetSupportedUpscalerMode(data.upscaler);
        bool dynamicResolution = data.dynamicResolution || UsesUpscaler(upscalerMode);
        int screenPercentage = GetRuntimeScreenPercentage(data, upscalerMode, dynamicResolution);
        ConfigureHdrpDynamicResolutionSettings(upscalerMode, dynamicResolution, screenPercentage, data.upscaleSharpness);

        if (cameraData == null)
        {
            WarnMissingReference("HDAdditionalCameraData is not assigned. HDRP image settings were skipped.");
        }
        else
        {
            cameraData.antialiasing = upscalerMode == GraphicsUpscalerMode.TAAU ? HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing : GetAntialiasingMode(data.antiAliasing);
            cameraData.TAAQuality = (HDAdditionalCameraData.TAAQualityLevel)Mathf.Clamp(data.taaQuality, 0, 2);
            cameraData.SMAAQuality = (HDAdditionalCameraData.SMAAQualityLevel)Mathf.Clamp(data.smaaQuality, 0, 2);
            cameraData.dithering = data.dithering;
            cameraData.allowDynamicResolution = dynamicResolution;
            SetCameraFrameSetting(cameraData, FrameSettingsField.Antialiasing, data.antiAliasing != 0 || upscalerMode == GraphicsUpscalerMode.TAAU);
            SetCameraFrameSetting(cameraData, FrameSettingsField.Dithering, data.dithering);
        }

        if (targetCamera == null)
        {
            WarnMissingReference("Target Camera is not assigned. Dynamic resolution camera filter settings were skipped.");
        }
        else
        {
            targetCamera.allowDynamicResolution = dynamicResolution;
            DynamicResolutionHandler.SetUpscaleFilter(targetCamera, GetUpscaleFilter(upscalerMode));
        }

        DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
        DynamicResolutionHandler.SetDynamicResScaler(() => screenPercentage, DynamicResScalePolicyType.ReturnsPercentage);

        if (cameraData != null)
        {
            ApplyAdvancedUpscalerSettings(data, cameraData, upscalerMode, dynamicResolution);
        }
    }

    public void ApplyLightingSettings(GraphicsData data, HDAdditionalCameraData cameraData, Volume globalVolume)
    {
        if (data == null)
        {
            return;
        }

        SetCameraFrameSetting(cameraData, FrameSettingsField.SSAO, data.ssao > 0);
        SetCameraFrameSetting(cameraData, FrameSettingsField.SSGI, data.ssgi > 0);
        SetCameraFrameSetting(cameraData, FrameSettingsField.SSR, data.ssr > 0);
        SetCameraFrameSetting(cameraData, FrameSettingsField.TransparentSSR, data.transparentSSR);
        SetCameraFrameSetting(cameraData, FrameSettingsField.ContactShadows, data.contactShadows > 0);
        SetCameraFrameSetting(cameraData, FrameSettingsField.ScreenSpaceShadows, data.screenSpaceShadows);

        if (TryGetVolumeProfile(globalVolume, out VolumeProfile profile) == false)
        {
            return;
        }

        if (TryGetVolumeOverride(profile, "Ambient Occlusion", out ScreenSpaceAmbientOcclusion ssao))
        {
            bool enabled = data.ssao > 0;
            ssao.active = enabled;
            SetQuality(ssao, GetQualityLevelFromOff(data.ssao));
            SetFloat(ssao.intensity, enabled ? GetLevelValue(data.ssao, 0.5f, 1f, 1.5f) : 0f);
        }

        if (TryGetVolumeOverride(profile, "Screen Space Global Illumination", out GlobalIllumination ssgi))
        {
            bool enabled = data.ssgi > 0;
            ssgi.active = enabled;
            SetBool(ssgi.enable, enabled);
            SetQuality(ssgi, GetQualityLevelFromOff(data.ssgi));
            SetBool(ssgi.fullResolutionSS, data.ssgi >= 3);
        }

        if (TryGetVolumeOverride(profile, "Screen Space Reflection", out ScreenSpaceReflection ssr))
        {
            bool enabled = data.ssr > 0;
            ssr.active = enabled;
            SetBool(ssr.enabled, enabled);
            SetBool(ssr.enabledTransparent, data.transparentSSR);
            SetQuality(ssr, GetQualityLevelFromOff(data.ssr));
        }

        if (TryGetVolumeOverride(profile, "Contact Shadows", out ContactShadows contactShadows))
        {
            bool enabled = data.contactShadows > 0;
            contactShadows.active = enabled;
            SetBool(contactShadows.enable, enabled);
            SetQuality(contactShadows, GetQualityLevelFromOff(data.contactShadows));
        }
    }

    public void ApplyShadowSettings(GraphicsData data, HDAdditionalCameraData cameraData)
    {
        if (data == null)
        {
            return;
        }

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = GetShadowResolution(data.shadowResolution);
        QualitySettings.shadowDistance = Mathf.Max(1f, data.shadowDistance);
        QualitySettings.shadowCascades = Mathf.Clamp(data.shadowCascades, 1, 4);
        SetCameraFrameSetting(cameraData, FrameSettingsField.ShadowMaps, true);
        if (_logMissingReferences)
        {
            WarnOnce("HDRP shadow filtering is controlled by HDRP assets and light settings. Runtime selector value is saved, but filtering is not changed per scene light.");
        }
    }

    public void ApplyAtmosphereSettings(GraphicsData data, HDAdditionalCameraData cameraData, Volume globalVolume)
    {
        if (data == null)
        {
            return;
        }

        SetCameraFrameSetting(cameraData, FrameSettingsField.AtmosphericScattering, data.fog > 0);
        SetCameraFrameSetting(cameraData, FrameSettingsField.Volumetrics, data.volumetricFog);
        SetCameraFrameSetting(cameraData, FrameSettingsField.VolumetricClouds, data.volumetricClouds > 0);

        if (TryGetVolumeProfile(globalVolume, out VolumeProfile profile) == false)
        {
            return;
        }

        if (TryGetVolumeOverride(profile, "Fog", out Fog fog))
        {
            bool fogEnabled = data.fog > 0;
            fog.active = fogEnabled;
            SetBool(fog.enabled, fogEnabled);
            SetBool(fog.enableVolumetricFog, fogEnabled && data.volumetricFog);
            SetQuality(fog, Mathf.Clamp(data.volumetricQuality, QUALITY_LOW, QUALITY_HIGH));
            SetFloat(fog.meanFreePath, fogEnabled ? GetLevelValue(data.fog, 600f, 350f, 180f) : 100000f);
        }

        if (TryGetVolumeOverride(profile, "Volumetric Clouds", out VolumetricClouds clouds))
        {
            bool enabled = data.volumetricClouds > 0;
            clouds.active = enabled;
            SetBool(clouds.enable, enabled);
            SetEnum(clouds.cloudSimpleMode, data.volumetricClouds >= 2 ? VolumetricClouds.CloudSimpleMode.Quality : VolumetricClouds.CloudSimpleMode.Performance);
            SetInt(clouds.numPrimarySteps, GetLevelIntValue(data.volumetricClouds, 48, 96, 160));
            SetInt(clouds.numLightSteps, GetLevelIntValue(data.volumetricClouds, 4, 8, 12));
        }
    }

    public void ApplyPostFxSettings(GraphicsData data, HDAdditionalCameraData cameraData, Volume globalVolume)
    {
        if (data == null)
        {
            return;
        }

        SetCameraFrameSetting(cameraData, FrameSettingsField.MotionBlur, data.motionBlur > 0);
        SetCameraFrameSetting(cameraData, FrameSettingsField.Vignette, data.vignette > 0);

        if (TryGetVolumeProfile(globalVolume, out VolumeProfile profile) == false)
        {
            return;
        }

        if (TryGetVolumeOverride(profile, "Motion Blur", out MotionBlur motionBlur))
        {
            bool enabled = data.motionBlur > 0;
            motionBlur.active = enabled;
            SetQuality(motionBlur, GetQualityLevelFromOff(data.motionBlur));
            SetFloat(motionBlur.intensity, enabled ? GetLevelValue(data.motionBlur, 0.1f, 0.35f, 0.6f) : 0f);
        }

        if (TryGetVolumeOverride(profile, "Vignette", out Vignette vignette))
        {
            bool enabled = data.vignette > 0;
            vignette.active = enabled;
            SetFloat(vignette.intensity, enabled ? GetLevelValue(data.vignette, 0.15f, 0.28f, 0.42f) : 0f);
            SetFloat(vignette.smoothness, enabled ? 0.35f : 0.2f);
        }
    }

    public void ApplyUnityQualitySettings(GraphicsData data)
    {
        if (data == null)
        {
            return;
        }

        QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(data.textureQuality, 0, 3);
        QualitySettings.anisotropicFiltering = GetAnisotropicFiltering(data.anisotropicFiltering);
        QualitySettings.lodBias = Mathf.Max(0.1f, data.lodBias);
    }

    public static List<Resolution> GetResolutionList()
    {
        Resolution[] source = Screen.resolutions;
        List<Resolution> result = new();

        for (int i = 0; i < source.Length; i++)
        {
            if (ContainsResolution(result, source[i]) == false)
            {
                result.Add(source[i]);
            }
        }

        if (result.Count == 0)
        {
            result.Add(Screen.currentResolution);
        }

        return result;
    }

    public static List<RefreshRate> GetRefreshRates(Resolution resolution)
    {
        Resolution[] source = Screen.resolutions;
        List<RefreshRate> result = new();

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i].width == resolution.width && source[i].height == resolution.height && ContainsRefreshRate(result, source[i].refreshRateRatio) == false)
            {
                result.Add(source[i].refreshRateRatio);
            }
        }

        if (result.Count == 0)
        {
            result.Add(Screen.currentResolution.refreshRateRatio);
        }

        return result;
    }

    public static string GetResolutionLabel(Resolution resolution)
    {
        return $"{resolution.width} x {resolution.height}";
    }

    public static string GetRefreshRateLabel(RefreshRate refreshRate)
    {
        double value = GetRefreshRateValue(refreshRate);

        if (value <= 0d)
        {
            return "Default";
        }

        return $"{Mathf.RoundToInt((float)value)} Hz";
    }

    public static bool IsUpscalerAvailable(GraphicsUpscalerMode mode)
    {
        switch (mode)
        {
            case GraphicsUpscalerMode.DLSS:
                return HDDynamicResolutionPlatformCapabilities.DLSSDetected;
            case GraphicsUpscalerMode.FSR2:
                return HDDynamicResolutionPlatformCapabilities.FSR2Detected;
            case GraphicsUpscalerMode.STP:
                return SystemInfo.supportsDynamicResolution && SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3;
            default:
                return true;
        }
    }

    public static void NormalizeUpscalerSettings(GraphicsData data)
    {
        if (data == null)
        {
            return;
        }

        GraphicsUpscalerMode upscalerMode = GetSupportedUpscalerMode(data.upscaler);
        data.upscaler = (int)upscalerMode;

        if (UsesUpscaler(upscalerMode))
        {
            data.dynamicResolution = true;
        }

        if (upscalerMode == GraphicsUpscalerMode.DLSS && data.dlssMode >= 4)
        {
            data.dlssMode = 0;
        }

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 && data.dynamicResolution && data.screenMode == (int)FullScreenMode.ExclusiveFullScreen)
        {
            data.screenMode = (int)FullScreenMode.FullScreenWindow;
        }

        if (UsesFixedScaleUpscaler(upscalerMode) && data.screenPercentage >= SCREEN_PERCENTAGE_MAX)
        {
            data.screenPercentage = DEFAULT_UPSCALE_PERCENTAGE;
        }

        if (upscalerMode == GraphicsUpscalerMode.TAAU)
        {
            data.antiAliasing = 3;
        }
    }

    public void SuspendUpscalers(Camera targetCamera, HDAdditionalCameraData cameraData)
    {
        if (cameraData != null)
        {
            cameraData.allowDynamicResolution = false;
            cameraData.allowDeepLearningSuperSampling = false;
            cameraData.allowFidelityFX2SuperResolution = false;
            cameraData.fsrOverrideSharpness = false;
        }

        if (targetCamera != null)
        {
            targetCamera.allowDynamicResolution = false;
            DynamicResolutionHandler.SetUpscaleFilter(targetCamera, DynamicResUpscaleFilter.CatmullRom);
        }

        ConfigureHdrpDynamicResolutionSettings(GraphicsUpscalerMode.Off, false, SCREEN_PERCENTAGE_MAX, 0f);
        DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
        DynamicResolutionHandler.SetDynamicResScaler(GetFullResolutionScale, DynamicResScalePolicyType.ReturnsPercentage);
    }

    private void ApplyAdvancedUpscalerSettings(GraphicsData data, HDAdditionalCameraData cameraData, GraphicsUpscalerMode upscalerMode, bool dynamicResolution)
    {
        float sharpness = Mathf.Clamp01(data.upscaleSharpness);
        cameraData.allowDeepLearningSuperSampling = false;
        cameraData.allowFidelityFX2SuperResolution = false;
        cameraData.fsrOverrideSharpness = false;

        if (dynamicResolution == false)
        {
            return;
        }

        if (upscalerMode == GraphicsUpscalerMode.DLSS)
        {
            if (IsUpscalerAvailable(GraphicsUpscalerMode.DLSS) == false)
            {
                WarnOnce("DLSS is not available for the current build target.");
                return;
            }

            cameraData.allowDeepLearningSuperSampling = true;
            cameraData.deepLearningSuperSamplingUseCustomQualitySettings = true;
            cameraData.deepLearningSuperSamplingQuality = GetDlssQuality(data.dlssMode);
            cameraData.deepLearningSuperSamplingUseCustomAttributes = true;
            cameraData.deepLearningSuperSamplingUseOptimalSettings = true;
            cameraData.deepLearningSuperSamplingSharpening = sharpness;
        }

        if (upscalerMode == GraphicsUpscalerMode.FSR2)
        {
            if (IsUpscalerAvailable(GraphicsUpscalerMode.FSR2) == false)
            {
                WarnOnce("FSR2 is not available for the current build target.");
                return;
            }

            cameraData.allowFidelityFX2SuperResolution = true;
            cameraData.fidelityFX2SuperResolutionUseCustomQualitySettings = true;
            cameraData.fidelityFX2SuperResolutionQuality = GetFsr2Quality(data.fsr2Mode);
            cameraData.fidelityFX2SuperResolutionUseCustomAttributes = true;
            cameraData.fidelityFX2SuperResolutionUseOptimalSettings = true;
            cameraData.fidelityFX2SuperResolutionEnableSharpening = true;
            cameraData.fidelityFX2SuperResolutionSharpening = sharpness;
        }

        if (upscalerMode == GraphicsUpscalerMode.FSR1 || upscalerMode == GraphicsUpscalerMode.CAS)
        {
            cameraData.fsrOverrideSharpness = true;
            cameraData.fsrSharpness = sharpness;
        }

        if (upscalerMode == GraphicsUpscalerMode.TAAU && cameraData.antialiasing != HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing)
        {
            WarnOnce("TAAU requires TAA on the target camera.");
        }
    }

    private static HDAdditionalCameraData.AntialiasingMode GetAntialiasingMode(int value)
    {
        switch (value)
        {
            case 1:
                return HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
            case 2:
                return HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            case 3:
                return HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            default:
                return HDAdditionalCameraData.AntialiasingMode.None;
        }
    }

    private static DynamicResUpscaleFilter GetUpscaleFilter(GraphicsUpscalerMode mode)
    {
        switch (mode)
        {
            case GraphicsUpscalerMode.CAS:
                return DynamicResUpscaleFilter.ContrastAdaptiveSharpen;
            case GraphicsUpscalerMode.FSR1:
                return DynamicResUpscaleFilter.EdgeAdaptiveScalingUpres;
            case GraphicsUpscalerMode.TAAU:
            case GraphicsUpscalerMode.DLSS:
            case GraphicsUpscalerMode.FSR2:
            case GraphicsUpscalerMode.STP:
                return DynamicResUpscaleFilter.TAAU;
            default:
                return DynamicResUpscaleFilter.CatmullRom;
        }
    }

    private void ConfigureHdrpDynamicResolutionSettings(GraphicsUpscalerMode upscalerMode, bool dynamicResolution, int screenPercentage, float sharpness)
    {
        HDRenderPipelineAsset asset = GetActiveHdrpAsset();

        if (asset == null)
        {
            WarnOnce("Active HDRP Asset was not found. HDRP upscaler settings were skipped.");
            return;
        }

        RenderPipelineSettings settings = asset.currentPlatformRenderPipelineSettings;
        GlobalDynamicResolutionSettings dynamicResolutionSettings = settings.dynamicResolutionSettings;
        List<string> advancedUpscalerNames = BuildAdvancedUpscalerNames(upscalerMode, dynamicResolution);
        int minPercentage = dynamicResolution ? GetMinimumScreenPercentage(upscalerMode, screenPercentage) : SCREEN_PERCENTAGE_MAX;
        DynamicResUpscaleFilter upscaleFilter = dynamicResolution ? GetUpscaleFilter(upscalerMode) : DynamicResUpscaleFilter.CatmullRom;
        float clampedSharpness = Mathf.Clamp01(sharpness);
        bool fsrSharpnessOverride = dynamicResolution && (upscalerMode == GraphicsUpscalerMode.CAS || upscalerMode == GraphicsUpscalerMode.FSR1);

        if (IsDynamicResolutionConfigured(dynamicResolutionSettings, dynamicResolution, minPercentage, upscaleFilter, clampedSharpness, fsrSharpnessOverride, advancedUpscalerNames))
        {
            return;
        }

        dynamicResolutionSettings.enabled = dynamicResolution;
        dynamicResolutionSettings.useMipBias = true;
        dynamicResolutionSettings.maxPercentage = SCREEN_PERCENTAGE_MAX;
        dynamicResolutionSettings.minPercentage = minPercentage;
        dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
        dynamicResolutionSettings.upsampleFilter = upscaleFilter;
        dynamicResolutionSettings.forceResolution = false;
        dynamicResolutionSettings.defaultInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterPost;
        dynamicResolutionSettings.TAAUInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField;
        dynamicResolutionSettings.DLSSInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField;
        dynamicResolutionSettings.FSR2InjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField;
        dynamicResolutionSettings.STPInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField;
        dynamicResolutionSettings.DLSSUseOptimalSettings = true;
        dynamicResolutionSettings.FSR2UseOptimalSettings = true;
        dynamicResolutionSettings.FSR2EnableSharpness = true;
        dynamicResolutionSettings.FSR2Sharpness = clampedSharpness;
        dynamicResolutionSettings.fsrOverrideSharpness = fsrSharpnessOverride;
        dynamicResolutionSettings.fsrSharpness = clampedSharpness;
        dynamicResolutionSettings.advancedUpscalerNames = advancedUpscalerNames;
        settings.dynamicResolutionSettings = dynamicResolutionSettings;
        asset.currentPlatformRenderPipelineSettings = settings;
    }

    private static HDRenderPipelineAsset GetActiveHdrpAsset()
    {
        HDRenderPipelineAsset asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;

        if (asset != null)
        {
            return asset;
        }

        return GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;
    }

    private static bool IsDynamicResolutionConfigured(GlobalDynamicResolutionSettings settings, bool enabled, int minPercentage, DynamicResUpscaleFilter upscaleFilter, float sharpness, bool fsrSharpnessOverride, List<string> advancedUpscalerNames)
    {
        if (settings.enabled != enabled)
        {
            return false;
        }

        if (settings.useMipBias == false || Mathf.Approximately(settings.maxPercentage, SCREEN_PERCENTAGE_MAX) == false || Mathf.Approximately(settings.minPercentage, minPercentage) == false)
        {
            return false;
        }

        if (settings.dynResType != DynamicResolutionType.Hardware || settings.upsampleFilter != upscaleFilter || settings.forceResolution)
        {
            return false;
        }

        if (settings.defaultInjectionPoint != DynamicResolutionHandler.UpsamplerScheduleType.AfterPost || settings.TAAUInjectionPoint != DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField)
        {
            return false;
        }

        if (settings.DLSSInjectionPoint != DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField || settings.FSR2InjectionPoint != DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField || settings.STPInjectionPoint != DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField)
        {
            return false;
        }

        if (settings.DLSSUseOptimalSettings == false || settings.FSR2UseOptimalSettings == false || settings.FSR2EnableSharpness == false)
        {
            return false;
        }

        if (Mathf.Approximately(settings.FSR2Sharpness, sharpness) == false || settings.fsrOverrideSharpness != fsrSharpnessOverride || Mathf.Approximately(settings.fsrSharpness, sharpness) == false)
        {
            return false;
        }

        return AreAdvancedUpscalerNamesEqual(settings.advancedUpscalerNames, advancedUpscalerNames);
    }

    private static bool AreAdvancedUpscalerNamesEqual(List<string> currentNames, List<string> targetNames)
    {
        if (currentNames == null || targetNames == null)
        {
            return currentNames == targetNames;
        }

        if (currentNames.Count != targetNames.Count)
        {
            return false;
        }

        for (int i = 0; i < currentNames.Count; i++)
        {
            if (currentNames[i] != targetNames[i])
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> BuildAdvancedUpscalerNames(GraphicsUpscalerMode upscalerMode, bool dynamicResolution)
    {
        List<string> names = new();

        if (dynamicResolution == false)
        {
            return names;
        }

        switch (upscalerMode)
        {
            case GraphicsUpscalerMode.DLSS:
                names.Add("DLSS");
                break;
            case GraphicsUpscalerMode.FSR2:
                names.Add("FSR2");
                break;
            case GraphicsUpscalerMode.STP:
                names.Add("STP");
                break;
        }

        return names;
    }

    private static GraphicsUpscalerMode GetSupportedUpscalerMode(int value)
    {
        GraphicsUpscalerMode mode = (GraphicsUpscalerMode)value;
        return IsUpscalerAvailable(mode) ? mode : GraphicsUpscalerMode.Off;
    }

    private static bool UsesUpscaler(GraphicsUpscalerMode mode)
    {
        return mode != GraphicsUpscalerMode.Off;
    }

    private static bool UsesFixedScaleUpscaler(GraphicsUpscalerMode mode)
    {
        return mode == GraphicsUpscalerMode.CatmullRom || mode == GraphicsUpscalerMode.CAS || mode == GraphicsUpscalerMode.FSR1 || mode == GraphicsUpscalerMode.TAAU || mode == GraphicsUpscalerMode.STP;
    }

    private static int GetRuntimeScreenPercentage(GraphicsData data, GraphicsUpscalerMode upscalerMode, bool dynamicResolution)
    {
        if (dynamicResolution == false)
        {
            return SCREEN_PERCENTAGE_MAX;
        }

        if (UsesFixedScaleUpscaler(upscalerMode) && data.screenPercentage >= SCREEN_PERCENTAGE_MAX)
        {
            return DEFAULT_UPSCALE_PERCENTAGE;
        }

        return Mathf.Clamp(data.screenPercentage, GetMinimumScreenPercentage(upscalerMode, data.screenPercentage), SCREEN_PERCENTAGE_MAX);
    }

    private static int GetMinimumScreenPercentage(GraphicsUpscalerMode upscalerMode, int screenPercentage)
    {
        if (upscalerMode == GraphicsUpscalerMode.DLSS || upscalerMode == GraphicsUpscalerMode.FSR2)
        {
            return 33;
        }

        return Mathf.Clamp(Mathf.Min(SCREEN_PERCENTAGE_MIN, screenPercentage), 33, SCREEN_PERCENTAGE_MAX);
    }

    private static uint GetDlssQuality(int index)
    {
        return DLSS_QUALITY_VALUES[Mathf.Clamp(index, 0, DLSS_QUALITY_VALUES.Length - 1)];
    }

    private static uint GetFsr2Quality(int index)
    {
        return FSR2_QUALITY_VALUES[Mathf.Clamp(index, 0, FSR2_QUALITY_VALUES.Length - 1)];
    }

    private static float GetFullResolutionScale()
    {
        return SCREEN_PERCENTAGE_MAX;
    }

    private static FullScreenMode GetFullScreenMode(GraphicsData data)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 && data != null && data.dynamicResolution && data.screenMode == (int)FullScreenMode.ExclusiveFullScreen)
        {
            return FullScreenMode.FullScreenWindow;
        }

        switch (data != null ? data.screenMode : (int)FullScreenMode.FullScreenWindow)
        {
            case 0:
                return FullScreenMode.ExclusiveFullScreen;
            case 2:
                return FullScreenMode.MaximizedWindow;
            case 3:
                return FullScreenMode.Windowed;
            default:
                return FullScreenMode.FullScreenWindow;
        }
    }

    private static bool ShouldSetResolution(Resolution resolution, RefreshRate refreshRate, FullScreenMode fullScreenMode)
    {
        if (_hasRequestedResolution && _requestedResolutionWidth == resolution.width && _requestedResolutionHeight == resolution.height && _requestedRefreshRateNumerator == refreshRate.numerator && _requestedRefreshRateDenominator == refreshRate.denominator && _requestedFullScreenMode == fullScreenMode)
        {
            return false;
        }

        if (Screen.width != resolution.width || Screen.height != resolution.height || Screen.fullScreenMode != fullScreenMode)
        {
            RememberRequestedResolution(resolution, refreshRate, fullScreenMode);
            return true;
        }

        RefreshRate currentRefreshRate = Screen.currentResolution.refreshRateRatio;
        bool shouldSetResolution = currentRefreshRate.numerator != refreshRate.numerator || currentRefreshRate.denominator != refreshRate.denominator;

        if (shouldSetResolution)
        {
            RememberRequestedResolution(resolution, refreshRate, fullScreenMode);
        }

        return shouldSetResolution;
    }

    private static void RememberRequestedResolution(Resolution resolution, RefreshRate refreshRate, FullScreenMode fullScreenMode)
    {
        _hasRequestedResolution = true;
        _requestedResolutionWidth = resolution.width;
        _requestedResolutionHeight = resolution.height;
        _requestedRefreshRateNumerator = refreshRate.numerator;
        _requestedRefreshRateDenominator = refreshRate.denominator;
        _requestedFullScreenMode = fullScreenMode;
    }

    private static ShadowResolution GetShadowResolution(int value)
    {
        switch (Mathf.Clamp(value, 0, 3))
        {
            case 0:
                return ShadowResolution.Low;
            case 1:
                return ShadowResolution.Medium;
            case 2:
                return ShadowResolution.High;
            default:
                return ShadowResolution.VeryHigh;
        }
    }

    private static AnisotropicFiltering GetAnisotropicFiltering(int value)
    {
        switch (Mathf.Clamp(value, 0, 2))
        {
            case 0:
                return AnisotropicFiltering.Disable;
            case 2:
                return AnisotropicFiltering.ForceEnable;
            default:
                return AnisotropicFiltering.Enable;
        }
    }

    private static bool ContainsResolution(List<Resolution> resolutions, Resolution resolution)
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == resolution.width && resolutions[i].height == resolution.height)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsRefreshRate(List<RefreshRate> refreshRates, RefreshRate refreshRate)
    {
        for (int i = 0; i < refreshRates.Count; i++)
        {
            if (refreshRates[i].numerator == refreshRate.numerator && refreshRates[i].denominator == refreshRate.denominator)
            {
                return true;
            }
        }

        return false;
    }

    private static double GetRefreshRateValue(RefreshRate refreshRate)
    {
        if (refreshRate.denominator == 0)
        {
            return 0d;
        }

        return (double)refreshRate.numerator / refreshRate.denominator;
    }

    private bool TryGetVolumeProfile(Volume globalVolume, out VolumeProfile profile)
    {
        profile = null;

        if (globalVolume == null)
        {
            WarnMissingReference("Global Volume is not assigned. Volume-based graphics settings were skipped.");
            return false;
        }

        if (globalVolume.HasInstantiatedProfile())
        {
            profile = globalVolume.profile;
            return profile != null;
        }

        if (globalVolume.sharedProfile == null)
        {
            WarnOnce("Global Volume has no assigned VolumeProfile. Volume-based graphics settings were skipped.");
            return false;
        }

        profile = globalVolume.profile;
        return profile != null;
    }

    private bool TryGetVolumeOverride<T>(VolumeProfile profile, string overrideName, out T component) where T : VolumeComponent
    {
        component = null;

        if (profile == null)
        {
            return false;
        }

        if (profile.TryGet(out component))
        {
            return true;
        }

        WarnOnce($"{overrideName} override is missing in the assigned VolumeProfile. Add it manually to enable this setting.");
        return false;
    }

    private void SetCameraFrameSetting(HDAdditionalCameraData cameraData, FrameSettingsField field, bool enabled)
    {
        if (cameraData == null)
        {
            WarnMissingReference("HDAdditionalCameraData is not assigned. Camera frame settings were skipped.");
            return;
        }

        cameraData.customRenderingSettings = true;
        cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        cameraData.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
    }

    private static void SetQuality(VolumeComponentWithQuality component, int level)
    {
        if (component == null)
        {
            return;
        }

        component.quality.overrideState = true;
        component.quality.levelAndOverride = (Mathf.Clamp(level, QUALITY_LOW, QUALITY_HIGH), false);
    }

    private static int GetQualityLevelFromOff(int value)
    {
        return Mathf.Clamp(value - 1, QUALITY_LOW, QUALITY_HIGH);
    }

    private static float GetLevelValue(int value, float low, float medium, float high)
    {
        switch (Mathf.Clamp(value, 1, 3))
        {
            case 1:
                return low;
            case 2:
                return medium;
            default:
                return high;
        }
    }

    private static int GetLevelIntValue(int value, int low, int medium, int high)
    {
        switch (Mathf.Clamp(value, 1, 3))
        {
            case 1:
                return low;
            case 2:
                return medium;
            default:
                return high;
        }
    }

    private static void SetBool(BoolParameter parameter, bool value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private static void SetFloat(FloatParameter parameter, float value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private static void SetInt(IntParameter parameter, int value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private static void SetEnum<T>(VolumeParameter<T> parameter, T value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private void WarnMissingReference(string message)
    {
        if (_logMissingReferences == false)
        {
            return;
        }

        WarnOnce(message);
    }

    private void WarnOnce(string message)
    {
        if (_warnings.Add(message))
        {
            Debug.LogWarning(message);
        }
    }
}
