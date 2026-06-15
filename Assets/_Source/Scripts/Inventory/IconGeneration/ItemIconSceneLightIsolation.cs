using UnityEngine;

internal static class ItemIconSceneLightIsolation
{
    public static int[] ExcludeIconLayerFromSceneLights(ItemIconGeneratorSettings settings, out Light[] sceneLights)
    {
        sceneLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int[] previousCullingMasks = new int[sceneLights.Length];

        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light sceneLight = sceneLights[i];

            if (sceneLight == null)
            {
                continue;
            }

            previousCullingMasks[i] = sceneLight.cullingMask;
            sceneLight.cullingMask &= ~settings.RenderLayerMask;
        }

        return previousCullingMasks;
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