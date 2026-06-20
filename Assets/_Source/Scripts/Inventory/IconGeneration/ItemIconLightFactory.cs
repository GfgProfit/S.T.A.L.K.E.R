using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

internal static class ItemIconLightFactory
{
    private const int MAX_LIGHT_COUNT = 4;
    private const float MIN_DIRECTION_MAGNITUDE = 0.0001f;

    public static Light[] CreateReusable(ItemIconGeneratorSettings settings)
    {
        Light[] lights = new Light[MAX_LIGHT_COUNT];

        for (int i = 0; i < lights.Length; i++)
        {
            lights[i] = CreateDirectionalLight(settings, $"Runtime Item Icon Light {i + 1}");
            lights[i].gameObject.SetActive(false);
        }

        return lights;
    }

    public static void Configure(IReadOnlyList<Light> lights, ItemData itemData, Transform cameraTransform, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        Disable(lights);

        if (lights == null || lights.Count == 0)
        {
            return;
        }

        int lightIndex = 0;
        float baseIntensity = renderProfile.LightIntensity;

        ConfigureLight(lights[lightIndex++], $"Runtime Icon Key Light - {itemData.name}", baseIntensity, cameraTransform.rotation);

        if (settings.UseFrontFillLight && lightIndex < lights.Count)
        {
            ConfigureLight(lights[lightIndex++], $"Runtime Icon Front Fill Light - {itemData.name}", baseIntensity * settings.FrontFillLightIntensityMultiplier, GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.FrontFillLightCameraDirection), cameraTransform.up));
        }

        if (settings.UseSideFillLight && lightIndex < lights.Count)
        {
            ConfigureLight(lights[lightIndex++], $"Runtime Icon Side Fill Light - {itemData.name}", baseIntensity * settings.SideFillLightIntensityMultiplier, GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.SideFillLightCameraDirection), cameraTransform.up));
        }

        if (settings.UseRimLight && lightIndex < lights.Count)
        {
            ConfigureLight(lights[lightIndex], $"Runtime Icon Rim Light - {itemData.name}", baseIntensity * settings.RimLightIntensityMultiplier, GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.RimLightCameraDirection), cameraTransform.up));
        }
    }

    public static void Disable(IReadOnlyList<Light> lights)
    {
        if (lights == null)
        {
            return;
        }

        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] != null)
            {
                lights[i].gameObject.SetActive(false);
            }
        }
    }

    private static Light CreateDirectionalLight(ItemIconGeneratorSettings settings, string lightName)
    {
        GameObject lightObject = new(lightName, typeof(Light))
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Light renderLight = lightObject.GetComponent<Light>();
        HDAdditionalLightData hdLightData = lightObject.AddComponent<HDAdditionalLightData>();
        renderLight.type = LightType.Directional;
        renderLight.cullingMask = settings.RenderLayerMask;
        renderLight.renderingLayerMask = (int)settings.RenderingLayerMask;
        renderLight.shadows = settings.GeneratedLightShadows;

        UnityEngine.Rendering.HighDefinition.RenderingLayerMask iconLightLayer = (UnityEngine.Rendering.HighDefinition.RenderingLayerMask)settings.RenderingLayerMask;
        hdLightData.SetLightLayer(iconLightLayer, iconLightLayer);
        return renderLight;
    }

    private static void ConfigureLight(Light renderLight, string lightName, float intensity, Quaternion rotation)
    {
        if (renderLight == null)
        {
            return;
        }

        renderLight.name = lightName;
        renderLight.intensity = Mathf.Max(0f, intensity);
        renderLight.transform.rotation = rotation;
        renderLight.gameObject.SetActive(true);
    }

    private static Quaternion GetDirectionalLightRotation(Vector3 direction, Vector3 up)
    {
        if (direction.sqrMagnitude <= MIN_DIRECTION_MAGNITUDE)
        {
            return Quaternion.identity;
        }

        Vector3 normalizedUp = up.sqrMagnitude > MIN_DIRECTION_MAGNITUDE ? up.normalized : Vector3.up;
        return Quaternion.LookRotation(direction.normalized, normalizedUp);
    }
}
