using UnityEngine;

internal static class ItemIconSceneLightIsolation
{
    public static Light[] CaptureSceneLights()
    {
        return Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    public static void ExcludeIconLayerFromSceneLights(ItemIconGeneratorSettings settings, Light[] sceneLights, int[] previousCullingMasks)
    {
        if (sceneLights == null || previousCullingMasks == null)
        {
            return;
        }

        int count = Mathf.Min(sceneLights.Length, previousCullingMasks.Length);

        for (int i = 0; i < count; i++)
        {
            Light sceneLight = sceneLights[i];

            if (sceneLight == null)
            {
                continue;
            }

            previousCullingMasks[i] = sceneLight.cullingMask;
            sceneLight.cullingMask &= ~settings.RenderLayerMask;
        }
    }

    public static void RestoreSceneLightCullingMasks(Light[] sceneLights, int[] previousCullingMasks)
    {
        if (sceneLights == null || previousCullingMasks == null)
        {
            return;
        }

        int count = Mathf.Min(sceneLights.Length, previousCullingMasks.Length);

        for (int i = 0; i < count; i++)
        {
            Light sceneLight = sceneLights[i];
            if (sceneLight != null)
            {
                sceneLight.cullingMask = previousCullingMasks[i];
            }
        }
    }
}
