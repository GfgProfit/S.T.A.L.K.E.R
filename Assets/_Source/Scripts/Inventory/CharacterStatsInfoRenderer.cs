using TMPro;
using UnityEngine;

public static class CharacterStatsInfoRenderer
{
    private const string StatsInfoObjectName = "Stats Info";

    private static readonly EntryInfo[] entries =
    {
        new EntryInfo(CharacterStatType.ThermalProtection, "Thermal Protection", "Thermal Protection Text"),
        new EntryInfo(CharacterStatType.ElectricalProtection, "Electrical Protection", "Electrical Protection Text"),
        new EntryInfo(CharacterStatType.ChemicalProtection, "Chemical Protection", "Chemical Protection Text"),
        new EntryInfo(CharacterStatType.RadiationProtection, "Radio Protection", "Radio Protection Text"),
        new EntryInfo(CharacterStatType.PsiProtection, "Psi Protection", "Psi Protection Text"),
        new EntryInfo(CharacterStatType.ShockRelief, "Shock Relief", "Shock Relief Text"),
        new EntryInfo(CharacterStatType.ArmorProtection, "Armor", "Armor Text"),
        new EntryInfo(CharacterStatType.Stamina, "Stamina", "Stamina Text"),
        new EntryInfo(CharacterStatType.ArtifactContainers, "Artefact Containers", "Artefact Containers Text"),
        new EntryInfo(CharacterStatType.CarryWeight, "Weight", "Weight Text")
    };

    public static GameObject FindStatsInfoRoot(Transform root, bool directChildOnly)
    {
        if (root == null)
        {
            return null;
        }

        Transform statsInfo = directChildOnly
            ? FindDirectChild(root, StatsInfoObjectName)
            : FindDeepChild(root, StatsInfoObjectName);

        return statsInfo == null ? null : statsInfo.gameObject;
    }

    public static void RenderItemStats(
        GameObject statsInfoRoot,
        InventoryItem item,
        Color currentValueColor,
        Color fullDurabilityValueColor,
        bool hideRootWhenEmpty)
    {
        if (statsInfoRoot == null)
        {
            return;
        }

        bool hasAnyVisibleStat = false;

        for (int i = 0; i < entries.Length; i++)
        {
            EntryInfo entry = entries[i];
            CharacterStatModifier modifier;
            bool show = TryGetModifier(item == null ? null : item.itemData, entry.StatType, out modifier);

            SetEntryActive(statsInfoRoot.transform, entry, show);

            if (show == false)
            {
                continue;
            }

            float durabilityPercent = item != null && item.HasDurability ? item.CurrentDurabilityPercent : 100f;
            float currentValue = CharacterStatUtility.CalculateCurrentValue(modifier, durabilityPercent);
            SetEntryText(
                statsInfoRoot.transform,
                entry,
                FormatItemValue(
                    entry.StatType,
                    currentValue,
                    modifier.ValueAtFullDurability,
                    durabilityPercent,
                    currentValueColor,
                    fullDurabilityValueColor));
            hasAnyVisibleStat = true;
        }

        statsInfoRoot.SetActive(hasAnyVisibleStat || hideRootWhenEmpty == false);
    }

    public static void RenderCharacterStats(
        GameObject statsInfoRoot,
        CharacterStatBlock stats,
        Color currentValueColor,
        bool hideRootWhenEmpty,
        bool showAllStats = false)
    {
        if (statsInfoRoot == null)
        {
            return;
        }

        bool hasAnyVisibleStat = false;

        for (int i = 0; i < entries.Length; i++)
        {
            EntryInfo entry = entries[i];
            float value = stats == null ? 0f : stats.Get(entry.StatType);
            bool show = showAllStats || CharacterStatUtility.IsNonZero(value);

            SetEntryActive(statsInfoRoot.transform, entry, show);

            if (show == false)
            {
                continue;
            }

            Color valueColor = CharacterStatUtility.IsNonZero(value) ? currentValueColor : Color.white;
            SetEntryText(statsInfoRoot.transform, entry, FormatColoredValue(entry.StatType, value, valueColor));
            hasAnyVisibleStat = true;
        }

        statsInfoRoot.SetActive(showAllStats || hasAnyVisibleStat || hideRootWhenEmpty == false);
    }

    public static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static bool TryGetModifier(ItemData itemData, CharacterStatType statType, out CharacterStatModifier modifier)
    {
        modifier = default;

        if (itemData == null || itemData.StatModifiers == null)
        {
            return false;
        }

        for (int i = 0; i < itemData.StatModifiers.Count; i++)
        {
            CharacterStatModifier candidate = itemData.StatModifiers[i];
            if (candidate.StatType == statType && candidate.HasValue)
            {
                modifier = candidate;
                return true;
            }
        }

        return false;
    }

    private static string FormatItemValue(
        CharacterStatType statType,
        float currentValue,
        float fullDurabilityValue,
        float durabilityPercent,
        Color currentValueColor,
        Color fullDurabilityValueColor)
    {
        string current = FormatColoredValue(statType, currentValue, currentValueColor);
        bool showFullValue = CharacterStatUtility.IsAffectedByDurability(statType) &&
                             Mathf.Approximately(durabilityPercent, 100f) == false &&
                             Mathf.Approximately(currentValue, fullDurabilityValue) == false;

        if (showFullValue == false)
        {
            return current;
        }

        return $"{current} ({FormatColoredValue(statType, fullDurabilityValue, fullDurabilityValueColor)})";
    }

    private static string FormatColoredValue(CharacterStatType statType, float value, Color color)
    {
        string htmlColor = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{htmlColor}>{FormatSignedValue(statType, value)}</color>";
    }

    private static string FormatSignedValue(CharacterStatType statType, float value)
    {
        if (CharacterStatUtility.IsNonZero(value) == false)
        {
            return FormatStatValue(statType, 0f);
        }

        string sign = value >= 0f ? "+" : string.Empty;
        return $"{sign}{FormatStatValue(statType, value)}";
    }

    private static string FormatStatValue(CharacterStatType statType, float value)
    {
        switch (statType)
        {
            case CharacterStatType.CarryWeight:
                return $"{FormatNumber(value)} КГ";
            case CharacterStatType.ArtifactContainers:
                return $"{FormatNumber(value)} конт.";
            default:
                return $"{FormatNumber(value)}%";
        }
    }

    private static string FormatNumber(float value)
    {
        float roundedTenths = Mathf.Round(value * 10f) / 10f;
        if (Mathf.Approximately(roundedTenths, Mathf.Round(roundedTenths)))
        {
            return Mathf.RoundToInt(roundedTenths).ToString();
        }

        return roundedTenths.ToString("0.#");
    }

    private static void SetEntryActive(Transform root, EntryInfo entry, bool active)
    {
        Transform iconRow = FindDeepChild(root, entry.IconObjectName);
        if (iconRow != null)
        {
            iconRow.gameObject.SetActive(active);
        }

        TMP_Text valueText = FindText(root, entry.TextObjectName);
        if (valueText != null)
        {
            valueText.gameObject.SetActive(active);
        }
    }

    private static void SetEntryText(Transform root, EntryInfo entry, string text)
    {
        TMP_Text valueText = FindText(root, entry.TextObjectName);
        if (valueText == null)
        {
            return;
        }

        valueText.richText = true;
        valueText.text = text;
    }

    private static TMP_Text FindText(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private readonly struct EntryInfo
    {
        public readonly CharacterStatType StatType;
        public readonly string IconObjectName;
        public readonly string TextObjectName;

        public EntryInfo(CharacterStatType statType, string iconObjectName, string textObjectName)
        {
            StatType = statType;
            IconObjectName = iconObjectName;
            TextObjectName = textObjectName;
        }
    }
}
