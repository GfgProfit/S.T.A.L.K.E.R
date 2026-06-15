using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

internal static class ItemIconLightFactory
{
    private const float MIN_DIRECTION_MAGNITUDE = 0.0001f;

    public static List<GameObject> Create(ItemData itemData, Transform cameraTransform, ItemIconGeneratorSettings settings, IconRenderProfile renderProfile)
    {
        List<GameObject> lightObjects = new(4);
        float baseIntensity = renderProfile.LightIntensity;

        AddDirectionalLight(settings, lightObjects, $"Runtime Icon Key Light - {itemData.name}", baseIntensity, Quaternion.Euler(renderProfile.LightEulerAngles));

        if (settings.UseFrontFillLight)
        {
            AddDirectionalLight(settings, lightObjects, $"Runtime Icon Front Fill Light - {itemData.name}", baseIntensity * settings.FrontFillLightIntensityMultiplier, GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.FrontFillLightCameraDirection), cameraTransform.up));
        }

        if (settings.UseSideFillLight)
        {
            AddDirectionalLight(settings, lightObjects, $"Runtime Icon Side Fill Light - {itemData.name}", baseIntensity * settings.SideFillLightIntensityMultiplier, GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.SideFillLightCameraDirection), cameraTransform.up));
        }

        if (settings.UseRimLight)
        {
            AddDirectionalLight(settings, lightObjects, $"Runtime Icon Rim Light - {itemData.name}", baseIntensity * settings.RimLightIntensityMultiplier, GetDirectionalLightRotation(cameraTransform.TransformDirection(settings.RimLightCameraDirection), cameraTransform.up));
        }

        return lightObjects;
    }

    private static void AddDirectionalLight(ItemIconGeneratorSettings settings, List<GameObject> lightObjects, string lightName, float intensity, Quaternion rotation)
    {
        GameObject lightObject = new(lightName, typeof(Light))
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Light renderLight = lightObject.GetComponent<Light>();
        HDAdditionalLightData hdLightData = renderLight.gameObject.AddComponent<HDAdditionalLightData>();
        renderLight.type = LightType.Directional;
        renderLight.intensity = Mathf.Max(0f, intensity);
        renderLight.cullingMask = settings.RenderLayerMask;
        renderLight.renderingLayerMask = (int)settings.RenderingLayerMask;
        renderLight.shadows = settings.GeneratedLightShadows;
        renderLight.transform.rotation = rotation;

        UnityEngine.Rendering.HighDefinition.RenderingLayerMask iconLightLayer = (UnityEngine.Rendering.HighDefinition.RenderingLayerMask)settings.RenderingLayerMask;
        hdLightData.SetLightLayer(iconLightLayer, iconLightLayer);

        lightObjects.Add(lightObject);
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