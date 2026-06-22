using UnityEngine;

internal static class ItemIconGeneratorSettingsHashBuilder
{
    public static int BuildHash(ItemIconGeneratorSettings settings)
    {
        ulong stableHash = BuildStableHash(settings);
        return unchecked((int)(stableHash ^ (stableHash >> 32)));
    }

    public static ulong BuildStableHash(ItemIconGeneratorSettings settings)
    {
        ulong hash = ItemIconStableHash.Begin();
        hash = ItemIconStableHash.Add(hash, settings.RenderLayer);
        hash = ItemIconStableHash.Add(hash, settings.RenderingLayer);
        hash = ItemIconStableHash.Add(hash, settings.RenderOrigin);
        hash = ItemIconStableHash.Add(hash, settings.ExcludeIconLayerFromSceneLights);
        hash = ItemIconStableHash.Add(hash, (int)settings.RendererLightProbeUsage);
        hash = ItemIconStableHash.Add(hash, (int)settings.RendererReflectionProbeUsage);
        hash = ItemIconStableHash.Add(hash, settings.RendererReceiveShadows);
        hash = ItemIconStableHash.Add(hash, (int)settings.RenderTextureFormat);
        hash = ItemIconStableHash.Add(hash, settings.IconRenderScale);
        hash = ItemIconStableHash.Add(hash, settings.AllowHdr);
        hash = ItemIconStableHash.Add(hash, settings.UseCameraMsaa);
        hash = ItemIconStableHash.Add(hash, settings.CameraDistanceMultiplier);
        hash = ItemIconStableHash.Add(hash, settings.CameraDistanceOffset);
        hash = ItemIconStableHash.Add(hash, settings.NearClipPlane);
        hash = ItemIconStableHash.Add(hash, settings.FarClipBoundsMultiplier);
        hash = ItemIconStableHash.Add(hash, settings.FarClipOffset);
        hash = ItemIconStableHash.Add(hash, settings.EnableDirectSpecularLighting);
        hash = ItemIconStableHash.Add(hash, settings.EnableExposureControl);
        hash = ItemIconStableHash.Add(hash, settings.EnableAtmosphericScattering);
        hash = ItemIconStableHash.Add(hash, settings.EnablePostProcess);
        hash = ItemIconStableHash.Add(hash, settings.EnableColorGrading);
        hash = ItemIconStableHash.Add(hash, settings.EnableTonemapping);
        hash = ItemIconStableHash.Add(hash, settings.EnableVolumetrics);
        hash = ItemIconStableHash.Add(hash, settings.EnableSkyReflection);
        hash = ItemIconStableHash.Add(hash, settings.UseFrontFillLight);
        hash = ItemIconStableHash.Add(hash, settings.FrontFillLightIntensityMultiplier);
        hash = ItemIconStableHash.Add(hash, settings.FrontFillLightCameraDirection);
        hash = ItemIconStableHash.Add(hash, settings.UseSideFillLight);
        hash = ItemIconStableHash.Add(hash, settings.SideFillLightIntensityMultiplier);
        hash = ItemIconStableHash.Add(hash, settings.SideFillLightCameraDirection);
        hash = ItemIconStableHash.Add(hash, settings.UseRimLight);
        hash = ItemIconStableHash.Add(hash, settings.RimLightIntensityMultiplier);
        hash = ItemIconStableHash.Add(hash, settings.RimLightCameraDirection);
        hash = ItemIconStableHash.Add(hash, (int)settings.GeneratedLightShadows);
        hash = ItemIconStableHash.Add(hash, (int)settings.SkyAmbientMode);
        hash = ItemIconStableHash.Add(hash, (int)settings.SkyRenderingSpace);
        hash = ItemIconStableHash.Add(hash, (int)settings.PhysicallyBasedSkyModel);
        hash = ItemIconStableHash.Add(hash, settings.PhysicallyBasedSkyGroundTint);
        hash = ItemIconStableHash.Add(hash, settings.FogEnabled);
        hash = ItemIconStableHash.Add(hash, (int)settings.FogColorMode);
        hash = ItemIconStableHash.Add(hash, settings.MaxFogDistance);
        hash = ItemIconStableHash.Add(hash, settings.MeanFreePath);
        hash = ItemIconStableHash.Add(hash, settings.EnableVolumetricFog);
        hash = ItemIconStableHash.Add(hash, settings.FogAnisotropy);
        hash = ItemIconStableHash.Add(hash, (int)settings.ExposureMode);
        hash = ItemIconStableHash.Add(hash, settings.FixedExposure);
        hash = ItemIconStableHash.Add(hash, settings.ExposureCompensation);
        hash = ItemIconStableHash.Add(hash, settings.ExposureLimitMin);
        hash = ItemIconStableHash.Add(hash, settings.ExposureLimitMax);
        hash = ItemIconStableHash.Add(hash, settings.HistogramPercentages);
        hash = ItemIconStableHash.Add(hash, settings.PostExposure);
        hash = ItemIconStableHash.Add(hash, settings.Contrast);
        hash = ItemIconStableHash.Add(hash, settings.ColorFilter);
        hash = ItemIconStableHash.Add(hash, settings.HueShift);
        hash = ItemIconStableHash.Add(hash, settings.Saturation);
        return ItemIconStableHash.Add(hash, (int)settings.TonemappingMode);
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
