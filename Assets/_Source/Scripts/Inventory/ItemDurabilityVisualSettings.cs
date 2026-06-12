using UnityEngine;

[CreateAssetMenu(fileName = DefaultResourcePath, menuName = "Inventory/Item Durability Visual Settings")]
public class ItemDurabilityVisualSettings : ScriptableObject
{
    public const string DefaultResourcePath = "ItemDurabilityVisualSettings";

    private static ItemDurabilityVisualSettings defaultSettings;
    private static bool defaultSettingsLoaded;

    [SerializeField] private Color highDurabilityColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color mediumDurabilityColor = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color lowDurabilityColor = new Color(1f, 0.25f, 0.2f, 1f);

    public Color GetColor(float durabilityPercent)
    {
        float normalizedDurability = ItemData.NormalizeDurability(durabilityPercent);

        if (normalizedDurability > 66f)
        {
            return highDurabilityColor;
        }

        if (normalizedDurability > 33f)
        {
            return mediumDurabilityColor;
        }

        return lowDurabilityColor;
    }

    public static ItemDurabilityVisualSettings LoadDefault()
    {
        if (defaultSettingsLoaded)
        {
            return defaultSettings;
        }

        defaultSettingsLoaded = true;
        defaultSettings = Resources.Load<ItemDurabilityVisualSettings>(DefaultResourcePath);

        if (defaultSettings == null)
        {
            defaultSettings = CreateInstance<ItemDurabilityVisualSettings>();
            defaultSettings.name = "Runtime ItemDurabilityVisualSettings";
            Debug.LogWarning($"Item durability visual settings asset was not found at Resources/{DefaultResourcePath}. A runtime default will be used.");
        }

        return defaultSettings;
    }
}
