using UnityEngine;
using UnityEngine.Rendering;

internal sealed class ItemIconSceneIsolationScope
{
    private readonly Light[] _lights;
    private readonly bool[] _lightEnabledStates;
    private readonly Volume[] _volumes;
    private readonly bool[] _volumeEnabledStates;
    private readonly ReflectionProbe[] _reflectionProbes;
    private readonly bool[] _reflectionProbeEnabledStates;
    private bool _lightsDisabled;
    private bool _applied;

    public ItemIconSceneIsolationScope()
    {
        _lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _lightEnabledStates = new bool[_lights.Length];
        _volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _volumeEnabledStates = new bool[_volumes.Length];
        _reflectionProbes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _reflectionProbeEnabledStates = new bool[_reflectionProbes.Length];
    }

    public void Apply(bool disableSceneLights)
    {
        if (_applied)
        {
            return;
        }

        _applied = true;
        _lightsDisabled = disableSceneLights;

        if (_lightsDisabled)
        {
            DisableBehaviours(_lights, _lightEnabledStates);
        }

        DisableBehaviours(_volumes, _volumeEnabledStates);
        DisableBehaviours(_reflectionProbes, _reflectionProbeEnabledStates);
    }

    public void Restore()
    {
        if (_applied == false)
        {
            return;
        }

        RestoreBehaviours(_reflectionProbes, _reflectionProbeEnabledStates);
        RestoreBehaviours(_volumes, _volumeEnabledStates);

        if (_lightsDisabled)
        {
            RestoreBehaviours(_lights, _lightEnabledStates);
        }

        _lightsDisabled = false;
        _applied = false;
    }

    private static void DisableBehaviours<T>(T[] behaviours, bool[] enabledStates) where T : Behaviour
    {
        int count = Mathf.Min(behaviours.Length, enabledStates.Length);

        for (int i = 0; i < count; i++)
        {
            T behaviour = behaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            enabledStates[i] = behaviour.enabled;
            behaviour.enabled = false;
        }
    }

    private static void RestoreBehaviours<T>(T[] behaviours, bool[] enabledStates) where T : Behaviour
    {
        int count = Mathf.Min(behaviours.Length, enabledStates.Length);

        for (int i = 0; i < count; i++)
        {
            T behaviour = behaviours[i];

            if (behaviour != null)
            {
                behaviour.enabled = enabledStates[i];
            }
        }
    }
}
