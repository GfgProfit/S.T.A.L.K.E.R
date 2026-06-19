using System.Collections.Generic;
using UnityEngine;

internal static class WeaponModuleSupport
{
    private static readonly Dictionary<GameObject, FirstPersonWeaponModule[]> _definitionCache = new();

    public static bool CanInstall(InventoryItem weaponItem, ItemData moduleItemData)
    {
        return weaponItem != null &&
               HasOccupiedModuleSlot(weaponItem.InstalledModules, moduleItemData) == false &&
               TryGetDefinition(weaponItem, moduleItemData, out FirstPersonWeaponModule definition) &&
               definition.CanInstall(weaponItem.InstalledModules);
    }

    public static bool CanDetach(InventoryItem weaponItem, ItemData moduleItemData)
    {
        if (weaponItem == null || weaponItem.HasInstalledModule(moduleItemData) == false)
        {
            return false;
        }

        IReadOnlyList<ItemData> installedModules = weaponItem.InstalledModules;
        List<ItemData> modulesAfterDetach = new(installedModules.Count - 1);

        for (int i = 0; i < installedModules.Count; i++)
        {
            if (installedModules[i] != moduleItemData)
            {
                modulesAfterDetach.Add(installedModules[i]);
            }
        }

        for (int i = 0; i < modulesAfterDetach.Count; i++)
        {
            if (TryGetDefinition(weaponItem, modulesAfterDetach[i], out FirstPersonWeaponModule definition) == false || definition.RequirementsSatisfied(modulesAfterDetach) == false)
            {
                return false;
            }
        }

        return true;
    }

    public static void CollectInstallableModules(InventoryItem weaponItem, ISet<ItemData> compatibleItemData)
    {
        if (weaponItem == null || compatibleItemData == null || weaponItem.ItemData == null || weaponItem.ItemData.FirstPersonWeaponPrefab == null)
        {
            return;
        }

        FirstPersonWeaponModule[] definitions = GetDefinitions(weaponItem.ItemData.FirstPersonWeaponPrefab);

        for (int i = 0; i < definitions.Length; i++)
        {
            FirstPersonWeaponModule definition = definitions[i];

            if (definition != null &&
                HasOccupiedModuleSlot(weaponItem.InstalledModules, definition.ModuleItemData) == false &&
                definition.CanInstall(weaponItem.InstalledModules))
            {
                compatibleItemData.Add(definition.ModuleItemData);
            }
        }
    }

    public static void ApplyToVisual(GameObject weaponVisual, IReadOnlyList<ItemData> installedModules)
    {
        if (weaponVisual == null)
        {
            return;
        }

        FirstPersonWeaponModule[] definitions = weaponVisual.GetComponentsInChildren<FirstPersonWeaponModule>(true);

        for (int i = 0; i < definitions.Length; i++)
        {
            definitions[i]?.ApplyDefaultVisualState();
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            FirstPersonWeaponModule definition = definitions[i];

            if (definition == null || definition.IsInstalled(installedModules) == false)
            {
                continue;
            }

            Transform attachPoint = FindAttachPoint(definition, definitions, installedModules);
            definition.ApplyInstalledVisualState(attachPoint);
        }
    }

    private static Transform FindAttachPoint(FirstPersonWeaponModule module, IReadOnlyList<FirstPersonWeaponModule> definitions, IReadOnlyList<ItemData> installedModules)
    {
        IReadOnlyList<ItemData> requiredModules = module.RequiredModules;

        for (int requiredIndex = 0; requiredIndex < requiredModules.Count; requiredIndex++)
        {
            ItemData requiredModule = requiredModules[requiredIndex];

            if (requiredModule == null || Contains(installedModules, requiredModule) == false)
            {
                continue;
            }

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                FirstPersonWeaponModule definition = definitions[definitionIndex];

                if (definition != null && definition.ModuleItemData == requiredModule && definition.AttachPoint != null)
                {
                    return definition.AttachPoint;
                }
            }
        }

        return null;
    }

    private static bool TryGetDefinition(InventoryItem weaponItem, ItemData moduleItemData, out FirstPersonWeaponModule definition)
    {
        definition = null;

        if (weaponItem == null || moduleItemData == null || moduleItemData.ItemType != ItemType.Module || weaponItem.ItemData == null || weaponItem.ItemData.FirstPersonWeaponPrefab == null)
        {
            return false;
        }

        FirstPersonWeaponModule[] definitions = GetDefinitions(weaponItem.ItemData.FirstPersonWeaponPrefab);

        for (int i = 0; i < definitions.Length; i++)
        {
            FirstPersonWeaponModule candidate = definitions[i];

            if (candidate != null && candidate.ModuleItemData == moduleItemData)
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    private static FirstPersonWeaponModule[] GetDefinitions(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            return System.Array.Empty<FirstPersonWeaponModule>();
        }

        if (_definitionCache.TryGetValue(weaponPrefab, out FirstPersonWeaponModule[] definitions))
        {
            return definitions;
        }

        definitions = weaponPrefab.GetComponentsInChildren<FirstPersonWeaponModule>(true);
        _definitionCache.Add(weaponPrefab, definitions);
        return definitions;
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

    private static bool HasOccupiedModuleSlot(IReadOnlyList<ItemData> installedModules, ItemData moduleItemData)
    {
        WeaponModuleSlot moduleSlot = moduleItemData == null ? WeaponModuleSlot.None : moduleItemData.ModuleSlot;

        if (moduleSlot == WeaponModuleSlot.None || installedModules == null)
        {
            return false;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData installedModule = installedModules[i];

            if (installedModule != null && installedModule.ModuleSlot == moduleSlot)
            {
                return true;
            }
        }

        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _definitionCache.Clear();
}
