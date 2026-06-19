using System.Collections.Generic;
using UnityEngine;

public sealed class FirstPersonWeaponModule : MonoBehaviour
{
    [SerializeField] private ItemData _moduleItemData;
    [SerializeField] private ItemData[] _requiredModules = System.Array.Empty<ItemData>();
    [SerializeField] private Transform _attachPoint;
    [SerializeField] private GameObject[] _enabledWhenInstalled = System.Array.Empty<GameObject>();
    [SerializeField] private GameObject[] _disabledWhenInstalled = System.Array.Empty<GameObject>();

    public ItemData ModuleItemData => _moduleItemData;
    internal IReadOnlyList<ItemData> RequiredModules => _requiredModules;
    internal Transform AttachPoint => _attachPoint;

    public bool CanInstall(IReadOnlyList<ItemData> installedModules)
    {
        if (_moduleItemData == null || _moduleItemData.ItemType != ItemType.Module || Contains(installedModules, _moduleItemData))
        {
            return false;
        }

        return RequirementsSatisfied(installedModules);
    }

    public bool RequirementsSatisfied(IReadOnlyList<ItemData> installedModules)
    {
        bool hasRequirement = false;

        for (int i = 0; i < _requiredModules.Length; i++)
        {
            ItemData requiredModule = _requiredModules[i];

            if (requiredModule == null)
            {
                continue;
            }

            hasRequirement = true;

            if (Contains(installedModules, requiredModule))
            {
                return true;
            }
        }

        return hasRequirement == false;
    }

    internal bool IsInstalled(IReadOnlyList<ItemData> installedModules)
    {
        return Contains(installedModules, _moduleItemData);
    }

    internal void ApplyDefaultVisualState()
    {
        SetObjectsActive(_enabledWhenInstalled, false);
        SetObjectsActive(_disabledWhenInstalled, true);
    }

    internal void ApplyInstalledVisualState(Transform attachPoint)
    {
        SetObjectsActive(_enabledWhenInstalled, true);
        SetObjectsActive(_disabledWhenInstalled, false);

        if (attachPoint == null)
        {
            return;
        }

        for (int i = 0; i < _enabledWhenInstalled.Length; i++)
        {
            GameObject moduleVisual = _enabledWhenInstalled[i];

            if (moduleVisual != null)
            {
                moduleVisual.transform.position = attachPoint.position;
            }
        }
    }

    private static bool Contains(IReadOnlyList<ItemData> modules, ItemData module)
    {
        if (modules == null || module == null)
        {
            return false;
        }

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] == module)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetObjectsActive(IReadOnlyList<GameObject> objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            objects[i]?.SetActive(active);
        }
    }
}
