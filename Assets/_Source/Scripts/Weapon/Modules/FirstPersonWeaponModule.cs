using System.Collections.Generic;
using UnityEngine;

public sealed class FirstPersonWeaponModule : MonoBehaviour
{
    [SerializeField] private ItemData _moduleItemData;
    [SerializeField] private ItemData[] _requiredModules = System.Array.Empty<ItemData>();
    [SerializeField] private GameObject[] _enabledWhenInstalled = System.Array.Empty<GameObject>();
    [SerializeField] private GameObject[] _disabledWhenInstalled = System.Array.Empty<GameObject>();

    public ItemData ModuleItemData => _moduleItemData;

    public bool CanInstall(IReadOnlyList<ItemData> installedModules)
    {
        if (_moduleItemData == null || _moduleItemData.ItemType != ItemType.Module || Contains(installedModules, _moduleItemData))
        {
            return false;
        }

        for (int i = 0; i < _requiredModules.Length; i++)
        {
            ItemData requiredModule = _requiredModules[i];

            if (requiredModule != null && Contains(installedModules, requiredModule) == false)
            {
                return false;
            }
        }

        return true;
    }

    public bool RequirementsSatisfied(IReadOnlyList<ItemData> installedModules)
    {
        for (int i = 0; i < _requiredModules.Length; i++)
        {
            ItemData requiredModule = _requiredModules[i];

            if (requiredModule != null && Contains(installedModules, requiredModule) == false)
            {
                return false;
            }
        }

        return true;
    }

    public void Apply(IReadOnlyList<ItemData> installedModules)
    {
        bool installed = Contains(installedModules, _moduleItemData);
        SetObjectsActive(_enabledWhenInstalled, installed);
        SetObjectsActive(_disabledWhenInstalled, installed == false);
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
