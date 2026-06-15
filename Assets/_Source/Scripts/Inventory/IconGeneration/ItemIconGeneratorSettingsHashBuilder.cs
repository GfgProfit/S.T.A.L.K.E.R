using UnityEngine;

internal static class ItemIconGeneratorSettingsHashBuilder
{
    public static int BuildHash(ItemIconGeneratorSettings settings)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + settings.RenderLayer;
            hash = hash * 31 + settings.RenderingLayer;
            hash = hash * 31 + HashVector(settings.RenderOrigin);
            hash = hash * 31 + (settings.ExcludeIconLayerFromSceneLights ? 1 : 0);
            hash = hash * 31 + (int)settings.RendererLightProbeUsage;
            hash = hash * 31 + (int)settings.RendererReflectionProbeUsage;
            hash = hash * 31 + (settings.RendererReceiveShadows ? 1 : 0);
            hash = hash * 31 + (int)settings.RenderTextureFormat;
            hash = hash * 31 + (settings.AllowHdr ? 1 : 0);
            hash = hash * 31 + (settings.UseCameraMsaa ? 1 : 0);
            hash = hash * 31 + Quantize(settings.CameraDistanceMultiplier);
            hash = hash * 31 + Quantize(settings.CameraDistanceOffset);
            hash = hash * 31 + Quantize(settings.NearClipPlane);
            hash = hash * 31 + Quantize(settings.FarClipBoundsMultiplier);
            hash = hash * 31 + Quantize(settings.FarClipOffset);
            hash = hash * 31 + (settings.EnableDirectSpecularLighting ? 1 : 0);
            hash = hash * 31 + (settings.EnableExposureControl ? 1 : 0);
            hash = hash * 31 + (settings.EnableAtmosphericScattering ? 1 : 0);
            hash = hash * 31 + (settings.EnablePostProcess ? 1 : 0);
            hash = hash * 31 + (settings.EnableColorGrading ? 1 : 0);
            hash = hash * 31 + (settings.EnableTonemapping ? 1 : 0);
            hash = hash * 31 + (settings.EnableVolumetrics ? 1 : 0);
            hash = hash * 31 + (settings.EnableSkyReflection ? 1 : 0);
            hash = hash * 31 + (settings.UseFrontFillLight ? 1 : 0);
            hash = hash * 31 + Quantize(settings.FrontFillLightIntensityMultiplier);
            hash = hash * 31 + HashVector(settings.FrontFillLightCameraDirection);
            hash = hash * 31 + (settings.UseSideFillLight ? 1 : 0);
            hash = hash * 31 + Quantize(settings.SideFillLightIntensityMultiplier);
            hash = hash * 31 + HashVector(settings.SideFillLightCameraDirection);
            hash = hash * 31 + (settings.UseRimLight ? 1 : 0);
            hash = hash * 31 + Quantize(settings.RimLightIntensityMultiplier);
            hash = hash * 31 + HashVector(settings.RimLightCameraDirection);
            hash = hash * 31 + (int)settings.GeneratedLightShadows;
            hash = hash * 31 + (int)settings.SkyAmbientMode;
            hash = hash * 31 + (int)settings.SkyRenderingSpace;
            hash = hash * 31 + (int)settings.PhysicallyBasedSkyModel;
            hash = hash * 31 + HashColor(settings.PhysicallyBasedSkyGroundTint);
            hash = hash * 31 + (settings.FogEnabled ? 1 : 0);
            hash = hash * 31 + (int)settings.FogColorMode;
            hash = hash * 31 + Quantize(settings.MaxFogDistance);
            hash = hash * 31 + Quantize(settings.MeanFreePath);
            hash = hash * 31 + (settings.EnableVolumetricFog ? 1 : 0);
            hash = hash * 31 + Quantize(settings.FogAnisotropy);
            hash = hash * 31 + (int)settings.ExposureMode;
            hash = hash * 31 + Quantize(settings.FixedExposure);
            hash = hash * 31 + Quantize(settings.ExposureCompensation);
            hash = hash * 31 + Quantize(settings.ExposureLimitMin);
            hash = hash * 31 + Quantize(settings.ExposureLimitMax);
            hash = hash * 31 + HashVector(settings.HistogramPercentages);
            hash = hash * 31 + Quantize(settings.PostExposure);
            hash = hash * 31 + Quantize(settings.Contrast);
            hash = hash * 31 + HashColor(settings.ColorFilter);
            hash = hash * 31 + Quantize(settings.HueShift);
            hash = hash * 31 + Quantize(settings.Saturation);
            hash = hash * 31 + (int)settings.TonemappingMode;
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
}