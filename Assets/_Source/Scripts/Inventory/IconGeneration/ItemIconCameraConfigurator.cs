using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

internal static class ItemIconCameraConfigurator
{
    private const float MIN_ORTHOGRAPHIC_SIZE = 0.01f;
    private const float MIN_BOUNDS_RADIUS = 0.1f;

    public static void ConfigureHdCamera(HDAdditionalCameraData hdCameraData, ItemIconGeneratorSettings settings)
    {
        hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hdCameraData.backgroundColorHDR = new Color(0f, 0f, 0f, 0f);
        hdCameraData.clearDepth = true;
        hdCameraData.volumeLayerMask = settings.RenderLayerMask;
        hdCameraData.volumeAnchorOverride = null;
        hdCameraData.probeLayerMask = settings.RenderLayerMask;
        hdCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        hdCameraData.dithering = false;
        hdCameraData.stopNaNs = false;
        hdCameraData.xrRendering = false;
        hdCameraData.allowDynamicResolution = false;
        hdCameraData.customRenderingSettings = true;

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.OpaqueObjects, true);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.TransparentObjects, true);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LightLayers, true);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.DirectSpecularLighting, settings.EnableDirectSpecularLighting);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ExposureControl, settings.EnableExposureControl);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.AtmosphericScattering, settings.EnableAtmosphericScattering);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Postprocess, settings.EnablePostProcess);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.CustomPostProcess, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.AfterPostprocess, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Antialiasing, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Bloom, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ColorGrading, settings.EnableColorGrading);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Tonemapping, settings.EnableTonemapping);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Vignette, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.DepthOfField, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.MotionBlur, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LensDistortion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ChromaticAberration, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.FilmGrain, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Dithering, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LensFlareDataDriven, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.LensFlareScreenSpace, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Volumetrics, settings.EnableVolumetrics);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ReprojectionForVolumetrics, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.VolumetricClouds, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.FullResolutionCloudsForSky, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SSAO, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SSGI, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SSR, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.TransparentSSR, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ScreenSpaceShadows, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ContactShadows, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Shadowmask, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ShadowMaps, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ReflectionProbe, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.PlanarProbe, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.SkyReflection, settings.EnableSkyReflection);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.AdaptiveProbeVolume, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.NormalizeReflectionProbeWithProbeVolume, false);

        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Decals, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.DecalLayers, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.CustomPass, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.MotionVectors, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.ObjectMotionVectors, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.TransparentsWriteMotionVector, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Refraction, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Distortion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.RoughDistortion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.Water, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.WaterDecals, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.WaterExclusion, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.RayTracing, false);
        SetCustomFrameSetting(hdCameraData, FrameSettingsField.RaytracingVFX, false);
    }

    public static void ConfigureRenderCamera(Camera renderCamera, Bounds bounds, RenderTexture renderTexture, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile, int antiAliasing)
    {
        Quaternion cameraRotation = Quaternion.Euler(renderProfile.CameraEulerAngles);
        float boundsRadius = Mathf.Max(MIN_BOUNDS_RADIUS, bounds.extents.magnitude);
        int textureWidth = renderProfile.TextureWidth;
        int textureHeight = renderProfile.TextureHeight;

        renderCamera.enabled = false;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;
        renderCamera.cullingMask = settings.RenderLayerMask;
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = CalculateOrthographicSize(bounds, cameraRotation, (float)textureWidth / textureHeight, renderProfile.Padding);
        renderCamera.nearClipPlane = settings.NearClipPlane;
        renderCamera.farClipPlane = boundsRadius * settings.FarClipBoundsMultiplier + settings.FarClipOffset;
        renderCamera.targetTexture = renderTexture;
        renderCamera.transform.rotation = cameraRotation;
        renderCamera.transform.position = bounds.center - renderCamera.transform.forward * (boundsRadius * settings.CameraDistanceMultiplier + settings.CameraDistanceOffset);
        renderCamera.allowHDR = settings.AllowHdr;
        renderCamera.allowMSAA = settings.UseCameraMsaa && antiAliasing > 1;
    }

    private static void SetCustomFrameSetting(HDAdditionalCameraData hdCameraData, FrameSettingsField field, bool enabled)
    {
        hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        hdCameraData.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
    }

    private static float CalculateOrthographicSize(Bounds bounds, Quaternion cameraRotation, float aspect, float padding)
    {
        Vector3[] corners = GetBoundsCorners(bounds);
        Quaternion inverseCameraRotation = Quaternion.Inverse(cameraRotation);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 cameraLocalPoint = inverseCameraRotation * (corners[i] - bounds.center);
            minX = Mathf.Min(minX, cameraLocalPoint.x);
            maxX = Mathf.Max(maxX, cameraLocalPoint.x);
            minY = Mathf.Min(minY, cameraLocalPoint.y);
            maxY = Mathf.Max(maxY, cameraLocalPoint.y);
        }

        float halfWidth = (maxX - minX) / 2f;
        float halfHeight = (maxY - minY) / 2f;

        return Mathf.Max(MIN_ORTHOGRAPHIC_SIZE, halfHeight, halfWidth / Mathf.Max(MIN_ORTHOGRAPHIC_SIZE, aspect)) * padding;
    }

    private static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new[]
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z),
        };
    }
}