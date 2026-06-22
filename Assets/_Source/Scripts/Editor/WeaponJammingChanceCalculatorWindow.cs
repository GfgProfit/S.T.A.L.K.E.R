using UnityEditor;
using UnityEngine;

public sealed class WeaponJammingChanceCalculatorWindow : EditorWindow
{
    private const float MIN_DURABILITY_STEP_PERCENT = 0.1f;
    private const float BUTTON_HEIGHT = 32f;
    private const float DURABILITY_COLUMN_WIDTH = 110f;
    private const float THRESHOLD_COLUMN_WIDTH = 190f;
    private const float BASE_CHANCE_COLUMN_WIDTH = 150f;
    private const float REAL_CHANCE_COLUMN_WIDTH = 150f;

    [SerializeField] private WeaponData _weaponData;
    [SerializeField] private float _startDurabilityPercent = 100f;
    [SerializeField] private float _endDurabilityPercent;
    [SerializeField] private float _durabilityStepPercent = 1f;

    private JammingChanceRow[] _rows;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Weapon/Jamming Chance Calculator")]
    private static void OpenWindow()
    {
        WeaponJammingChanceCalculatorWindow window = GetWindow<WeaponJammingChanceCalculatorWindow>();
        window.titleContent = new GUIContent("Jamming Calculator");
        window.minSize = new Vector2(680f, 320f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Weapon Jamming Chance Calculator", EditorStyles.boldLabel);
        EditorGUILayout.Space(8f);

        EditorGUI.BeginChangeCheck();
        _weaponData = (WeaponData)EditorGUILayout.ObjectField("Weapon Data", _weaponData, typeof(WeaponData), false);
        DrawSettings();

        if (EditorGUI.EndChangeCheck())
        {
            _rows = null;
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(_weaponData == null))
        {
            if (GUILayout.Button("Calculate", GUILayout.Height(BUTTON_HEIGHT)))
            {
                Calculate();
            }
        }

        EditorGUILayout.Space(10f);
        DrawResult();
    }

    private void Calculate()
    {
        if (_weaponData == null)
        {
            _rows = null;
            Debug.LogWarning("Weapon Data is not assigned.");
            return;
        }

        float threshold = _weaponData.JammingDurabilityThreshold;
        float baseChance = _weaponData.BaseJammedChance;
        float startDurabilityPercent = Mathf.Clamp(_startDurabilityPercent, 0f, 100f);
        float endDurabilityPercent = Mathf.Clamp(_endDurabilityPercent, 0f, 100f);
        float durabilityStepPercent = Mathf.Max(MIN_DURABILITY_STEP_PERCENT, _durabilityStepPercent);
        float direction = startDurabilityPercent >= endDurabilityPercent ? -1f : 1f;
        float durabilityRange = Mathf.Abs(startDurabilityPercent - endDurabilityPercent);
        int rowCount = Mathf.FloorToInt(durabilityRange / durabilityStepPercent) + 1;
        float lastDurabilityPercent = startDurabilityPercent + direction * durabilityStepPercent * (rowCount - 1);

        if (!Mathf.Approximately(lastDurabilityPercent, endDurabilityPercent))
        {
            rowCount++;
        }

        _rows = new JammingChanceRow[rowCount];

        for (int i = 0; i < rowCount; i++)
        {
            float durabilityPercent = i == rowCount - 1 ? endDurabilityPercent : startDurabilityPercent + direction * durabilityStepPercent * i;
            float realChance = _weaponData.GetJammedChancePercent(durabilityPercent);
            _rows[i] = new JammingChanceRow(durabilityPercent, threshold, baseChance, realChance);
        }
    }

    private void DrawSettings()
    {
        _startDurabilityPercent = EditorGUILayout.Slider("Start Durability Percent", _startDurabilityPercent, 0f, 100f);
        _endDurabilityPercent = EditorGUILayout.Slider("End Durability Percent", _endDurabilityPercent, 0f, 100f);
        _durabilityStepPercent = EditorGUILayout.FloatField("Durability Step Percent", _durabilityStepPercent);
        _durabilityStepPercent = Mathf.Max(MIN_DURABILITY_STEP_PERCENT, _durabilityStepPercent);
    }

    private void DrawResult()
    {
        if (_weaponData == null)
        {
            EditorGUILayout.HelpBox("Assign Weapon Data asset.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Maximum Jammed Chance: {FormatPercent(_weaponData.MaximumJammedChance)}", EditorStyles.miniLabel);

        if (_rows == null || _rows.Length == 0)
        {
            EditorGUILayout.HelpBox("Click Calculate to build the table.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawHeaderCell("Durability", DURABILITY_COLUMN_WIDTH);
        DrawHeaderCell("_jammingDurabilityThreshold", THRESHOLD_COLUMN_WIDTH);
        DrawHeaderCell("_baseJammedChance", BASE_CHANCE_COLUMN_WIDTH);
        DrawHeaderCell("Real Jammed Chance", REAL_CHANCE_COLUMN_WIDTH);
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

        foreach (JammingChanceRow row in _rows)
        {
            EditorGUILayout.BeginHorizontal();
            DrawCell(FormatPercent(row.DurabilityPercent), DURABILITY_COLUMN_WIDTH);
            DrawCell(FormatPercent(row.JammingDurabilityThreshold), THRESHOLD_COLUMN_WIDTH);
            DrawCell(FormatPercent(row.BaseJammedChance), BASE_CHANCE_COLUMN_WIDTH);
            DrawCell(FormatPercent(row.RealJammedChance), REAL_CHANCE_COLUMN_WIDTH);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawHeaderCell(string value, float width)
    {
        EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Width(width));
    }

    private static void DrawCell(string value, float width)
    {
        EditorGUILayout.LabelField(value, GUILayout.Width(width));
    }

    private static string FormatPercent(float value)
    {
        return $"{value:0.##}%";
    }

    private readonly struct JammingChanceRow
    {
        public JammingChanceRow(float durabilityPercent, float jammingDurabilityThreshold, float baseJammedChance, float realJammedChance)
        {
            DurabilityPercent = durabilityPercent;
            JammingDurabilityThreshold = jammingDurabilityThreshold;
            BaseJammedChance = baseJammedChance;
            RealJammedChance = realJammedChance;
        }

        public float DurabilityPercent { get; }
        public float JammingDurabilityThreshold { get; }
        public float BaseJammedChance { get; }
        public float RealJammedChance { get; }
    }
}
