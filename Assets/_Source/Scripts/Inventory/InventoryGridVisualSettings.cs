using UnityEngine;

[CreateAssetMenu(fileName = "InventoryGridVisualSettings", menuName = "Inventory/Grid Visual Settings")]
public class InventoryGridVisualSettings : ScriptableObject
{
    private const string DefaultResourcePath = "InventoryGridVisualSettings";

    private static InventoryGridVisualSettings defaultSettings;
    private static bool defaultSettingsLoaded;

    [SerializeField] private bool showCellGrid = true;
    [SerializeField] private bool showCellGridBorder = true;
    [SerializeField] private Color cellGridColor = new Color(1f, 1f, 1f, 0.11764706f);
    [SerializeField] private float cellGridLineThickness = 1f;
    [SerializeField] private Color cellGridBorderColor = new Color(0.745283f, 0.745283f, 0.745283f, 0.11764706f);
    [SerializeField] private float cellGridBorderLineThickness = 2f;

    public bool ShowCellGrid => showCellGrid;
    public bool ShowCellGridBorder => showCellGridBorder;

    public Color GetLineColor(bool isBorderLine)
    {
        return isBorderLine ? cellGridBorderColor : cellGridColor;
    }

    public float GetLineThickness(bool isBorderLine)
    {
        float thickness = isBorderLine ? cellGridBorderLineThickness : cellGridLineThickness;
        return Mathf.Max(1f, thickness);
    }

    public static InventoryGridVisualSettings LoadDefault()
    {
        if (defaultSettingsLoaded)
        {
            return defaultSettings;
        }

        defaultSettingsLoaded = true;
        defaultSettings = Resources.Load<InventoryGridVisualSettings>(DefaultResourcePath);

        if (defaultSettings == null)
        {
            defaultSettings = CreateInstance<InventoryGridVisualSettings>();
            defaultSettings.name = "Runtime InventoryGridVisualSettings";
            Debug.LogWarning($"Inventory grid visual settings asset was not found at Resources/{DefaultResourcePath}. A runtime default will be used.");
        }

        return defaultSettings;
    }
}
