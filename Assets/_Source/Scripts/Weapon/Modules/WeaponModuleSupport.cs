using System.Collections.Generic;
using UnityEngine;

internal static class WeaponModuleSupport
{
    private static readonly Dictionary<GameObject, FirstPersonWeaponModule[]> _definitionCache = new();

    public static bool CanInstall(InventoryItem weaponItem, ItemData moduleItemData)
    {
        if (weaponItem == null || weaponItem.ItemData == null)
        {
            return false;
        }

        FirstPersonWeaponModule[] definitions = GetDefinitions(weaponItem.ItemData.FirstPersonWeaponPrefab);
        return CanInstall(definitions, weaponItem.InstalledModules, moduleItemData);
    }

    public static bool CanDetach(InventoryItem weaponItem, ItemData moduleItemData)
    {
        if (weaponItem == null || weaponItem.ItemData == null || weaponItem.HasInstalledModule(moduleItemData) == false)
        {
            return false;
        }

        if (moduleItemData.ModuleSlot == WeaponModuleSlot.Magazine && weaponItem.WeaponMagazineState.IsJammed)
        {
            return false;
        }

        IReadOnlyList<ItemData> installedModules = weaponItem.InstalledModules;
        FirstPersonWeaponModule[] definitions = GetDefinitions(weaponItem.ItemData.FirstPersonWeaponPrefab);
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
            if (TryGetDefinition(definitions, modulesAfterDetach[i], out FirstPersonWeaponModule definition) == false || definition.ConfigurationSatisfied(modulesAfterDetach) == false)
            {
                return false;
            }
        }

        return true;
    }

    public static float GetRecoilPercentModifier(IReadOnlyList<ItemData> installedModules)
    {
        float modifier = 0f;

        if (installedModules == null)
        {
            return modifier;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData module = installedModules[i];

            if (module != null)
            {
                modifier += module.ModuleWeaponRecoilPercentModifier;
            }
        }

        return modifier;
    }

    public static float GetDurabilityLossPercentModifier(IReadOnlyList<ItemData> installedModules)
    {
        float modifier = 0f;

        if (installedModules == null)
        {
            return modifier;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData module = installedModules[i];

            if (module != null)
            {
                modifier += module.ModuleWeaponDurabilityLossPercentModifier;
            }
        }

        return modifier;
    }

    public static float GetAccuracyMinutesOfAngleModifier(IReadOnlyList<ItemData> installedModules)
    {
        float modifier = 0f;

        if (installedModules == null)
        {
            return modifier;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData module = installedModules[i];

            if (module != null)
            {
                modifier += module.ModuleAccuracyMinutesOfAngleModifier;
            }
        }

        return modifier;
    }

    public static float GetAccuracyMinutesOfAngle(WeaponData weaponData, IReadOnlyList<ItemData> installedModules)
    {
        if (weaponData == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, weaponData.AccuracyMinutesOfAngle + GetAccuracyMinutesOfAngleModifier(installedModules));
    }

    public static float GetErgonomicsModifier(IReadOnlyList<ItemData> installedModules)
    {
        float modifier = 0f;

        if (installedModules == null)
        {
            return modifier;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData module = installedModules[i];

            if (module != null)
            {
                modifier += module.ModuleErgonomicsModifier;
            }
        }

        return modifier;
    }

    public static float GetErgonomics(WeaponData weaponData, IReadOnlyList<ItemData> installedModules)
    {
        if (weaponData == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, weaponData.BaseErgonomics + GetErgonomicsModifier(installedModules));
    }

    public static float GetAimSpeedMultiplier(WeaponData weaponData, IReadOnlyList<ItemData> installedModules)
    {
        if (weaponData == null)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, GetErgonomics(weaponData, installedModules) / weaponData.BaseErgonomics);
    }

    public static int GetMagazineCapacity(int baseCapacity, IReadOnlyList<ItemData> installedModules)
    {
        int capacity = Mathf.Max(1, baseCapacity);
        int installedMagazineCapacity = GetInstalledMagazineCapacity(installedModules);

        return installedMagazineCapacity > 0 ? installedMagazineCapacity : capacity;
    }

    public static int GetInstalledMagazineCapacity(IReadOnlyList<ItemData> installedModules)
    {
        if (installedModules == null)
        {
            return 0;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData module = installedModules[i];

            if (module != null && module.ModuleSlot == WeaponModuleSlot.Magazine && module.ModuleMagazineCapacity > 0)
            {
                return module.ModuleMagazineCapacity;
            }
        }

        return 0;
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

            if (definition != null && CanInstall(definitions, weaponItem.InstalledModules, definition))
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
        ApplyToVisual(definitions, installedModules);
    }

    public static bool HasInstalledModuleType(IReadOnlyList<ItemData> installedModules, WeaponModuleType moduleType)
    {
        if (installedModules == null || moduleType == WeaponModuleType.None)
        {
            return false;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            ItemData installedModule = installedModules[i];

            if (installedModule != null && installedModule.ModuleType == moduleType)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ApplyToVisual(IReadOnlyList<FirstPersonWeaponModule> definitions, IReadOnlyList<ItemData> installedModules)
    {
        if (definitions == null)
        {
            return;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            definitions[i]?.ApplyDefaultVisualState();
        }

        for (int i = 0; i < definitions.Count; i++)
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

    private static bool TryGetDefinition(IReadOnlyList<FirstPersonWeaponModule> definitions, ItemData moduleItemData, out FirstPersonWeaponModule definition)
    {
        definition = null;

        if (definitions == null || moduleItemData == null)
        {
            return false;
        }

        for (int i = 0; i < definitions.Count; i++)
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

    private static bool CanInstall(IReadOnlyList<FirstPersonWeaponModule> definitions, IReadOnlyList<ItemData> installedModules, ItemData moduleItemData)
    {
        if (moduleItemData == null ||
            moduleItemData.ItemType != ItemType.Module ||
            TryGetDefinition(definitions, moduleItemData, out FirstPersonWeaponModule definition) == false)
        {
            return false;
        }

        return CanInstall(definitions, installedModules, definition);
    }

    private static bool CanInstall(
        IReadOnlyList<FirstPersonWeaponModule> definitions,
        IReadOnlyList<ItemData> installedModules,
        FirstPersonWeaponModule definition)
    {
        ItemData moduleItemData = definition.ModuleItemData;

        if (moduleItemData == null ||
            moduleItemData.ItemType != ItemType.Module ||
            HasOccupiedModuleSlot(installedModules, moduleItemData) ||
            definition.CanInstall(installedModules) == false)
        {
            return false;
        }

        if (installedModules == null)
        {
            return true;
        }

        for (int i = 0; i < installedModules.Count; i++)
        {
            if (TryGetDefinition(definitions, installedModules[i], out FirstPersonWeaponModule installedDefinition) && installedDefinition.IsIncompatibleWith(moduleItemData))
            {
                return false;
            }
        }

        return true;
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
