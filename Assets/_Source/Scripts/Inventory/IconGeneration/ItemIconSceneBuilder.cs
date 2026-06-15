using System.Collections.Generic;
using UnityEngine;

internal static class ItemIconSceneBuilder
{
    public static bool InstantiateParts(IReadOnlyList<ItemIconPart> parts, Transform parent)
    {
        if (parts == null)
        {
            return false;
        }

        bool hasRenderablePart = false;

        for (int i = 0; i < parts.Count; i++)
        {
            ItemIconPart part = parts[i];
            if (part == null)
            {
                continue;
            }

            hasRenderablePart |= InstantiateSource(part.Prefab, parent, part);
        }

        return hasRenderablePart;
    }

    public static bool InstantiateSource(GameObject source, Transform parent, ItemIconPart transformOverride)
    {
        if (source == null)
        {
            return false;
        }

        GameObject instance = Object.Instantiate(source, parent);
        instance.hideFlags = HideFlags.HideAndDontSave;

        if (transformOverride == null)
        {
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
        }
        else
        {
            transformOverride.ApplyTo(instance.transform);
        }

        return true;
    }

    public static bool TryCalculateBounds(GameObject rootObject, out Bounds bounds)
    {
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(rootObject.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer.enabled == false)
            {
                continue;
            }

            if (hasBounds == false)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    public static void SetLayerRecursively(GameObject rootObject, int layer)
    {
        Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.layer = layer;
        }
    }

    public static void SetRendererIsolation(GameObject rootObject, ItemIconGeneratorSettings settings)
    {
        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.renderingLayerMask = settings.RenderingLayerMask;
            renderer.lightProbeUsage = settings.RendererLightProbeUsage;
            renderer.reflectionProbeUsage = settings.RendererReflectionProbeUsage;
            renderer.receiveShadows = settings.RendererReceiveShadows;
        }
    }
}