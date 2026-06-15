using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal static class ItemIconVolumeFactory
{
    private const float VOLUME_PRIORITY = 10000f;

    public static GameObject Create(string itemName, ItemIconGeneratorSettings settings, out VolumeProfile volumeProfile)
    {
        GameObject volumeObject = new($"Runtime Icon Volume - {itemName}")
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = settings.RenderLayer
        };

        volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        volumeProfile.hideFlags = HideFlags.HideAndDontSave;
        volumeProfile.name = $"{itemName} Runtime Icon Volume Profile";
        ConfigureProfile(volumeProfile, settings);

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = VOLUME_PRIORITY;
        volume.weight = 1f;
        volume.sharedProfile = volumeProfile;

        return volumeObject;
    }

    private static void ConfigureProfile(VolumeProfile volumeProfile, ItemIconGeneratorSettings settings)
    {
        VisualEnvironment visualEnvironment = volumeProfile.Add<VisualEnvironment>(true);
        visualEnvironment.skyType.Override(SkySettings.GetUniqueID<PhysicallyBasedSky>());
        visualEnvironment.cloudType.Override(0);
        visualEnvironment.skyAmbientMode.Override(settings.SkyAmbientMode);
        visualEnvironment.renderingSpace.Override(settings.SkyRenderingSpace);

        PhysicallyBasedSky physicallyBasedSky = volumeProfile.Add<PhysicallyBasedSky>(true);
        physicallyBasedSky.active = true;
        physicallyBasedSky.type.Override(settings.PhysicallyBasedSkyModel);
        physicallyBasedSky.groundTint.Override(settings.PhysicallyBasedSkyGroundTint);

        Fog fog = volumeProfile.Add<Fog>(true);
        fog.enabled.Override(settings.FogEnabled);
        fog.colorMode.Override(settings.FogColorMode);
        fog.maxFogDistance.Override(settings.MaxFogDistance);
        fog.meanFreePath.Override(settings.MeanFreePath);
        fog.enableVolumetricFog.Override(settings.EnableVolumetricFog);
        fog.anisotropy.Override(settings.FogAnisotropy);

        Exposure exposure = volumeProfile.Add<Exposure>(true);
        exposure.mode.Override(settings.ExposureMode);
        exposure.adaptationMode.Override(AdaptationMode.Fixed);
        exposure.fixedExposure.Override(settings.FixedExposure);
        exposure.compensation.Override(settings.ExposureCompensation);
        exposure.limitMin.Override(settings.ExposureLimitMin);
        exposure.limitMax.Override(settings.ExposureLimitMax);
        exposure.histogramPercentages.Override(settings.HistogramPercentages);

        ColorAdjustments colorAdjustments = volumeProfile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(settings.PostExposure);
        colorAdjustments.contrast.Override(settings.Contrast);
        colorAdjustments.colorFilter.Override(settings.ColorFilter);
        colorAdjustments.hueShift.Override(settings.HueShift);
        colorAdjustments.saturation.Override(settings.Saturation);

        Tonemapping tonemapping = volumeProfile.Add<Tonemapping>(true);
        tonemapping.mode.Override(settings.TonemappingMode);
    }
}