using UnityEditor;
using UnityEngine;

public class AssignTexturesToMaterialsWindow : EditorWindow
{
    [SerializeField] private Material[] _materials;
    [SerializeField] private Texture2D[] _baseColorTextures;
    [SerializeField] private Texture2D[] _normalTextures;

    private SerializedObject _serializedObject;
    private SerializedProperty _materialsProperty;
    private SerializedProperty _baseColorTexturesProperty;
    private SerializedProperty _normalTexturesProperty;

    private const string BaseColorMapProperty = "_BaseColorMap";
    private const string NormalMapProperty = "_NormalMap";

    [MenuItem("Tools/Assign Textures To Materials")]
    private static void OpenWindow()
    {
        AssignTexturesToMaterialsWindow window = GetWindow<AssignTexturesToMaterialsWindow>();
        window.titleContent = new GUIContent("Assign Textures");
        window.minSize = new Vector2(450f, 350f);
        window.Show();
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);

        _materialsProperty = _serializedObject.FindProperty(nameof(_materials));
        _baseColorTexturesProperty = _serializedObject.FindProperty(nameof(_baseColorTextures));
        _normalTexturesProperty = _serializedObject.FindProperty(nameof(_normalTextures));
    }

    private void OnGUI()
    {
        if (_serializedObject == null)
        {
            OnEnable();
        }

        _serializedObject.Update();

        EditorGUILayout.LabelField("Assign Textures To Materials", EditorStyles.boldLabel);
        EditorGUILayout.Space(8f);

        EditorGUILayout.HelpBox(
            "Порядок должен совпадать:\n" +
            "Material[0] = Base Texture[0] = Normal Texture[0]\n" +
            "Material[1] = Base Texture[1] = Normal Texture[1]\n" +
            "и т.д.",
            MessageType.Info
        );

        EditorGUILayout.Space(8f);

        EditorGUILayout.PropertyField(_materialsProperty, new GUIContent("Materials"), true);
        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(_baseColorTexturesProperty, new GUIContent("Textures"), true);
        EditorGUILayout.Space(4f);

        EditorGUILayout.PropertyField(_normalTexturesProperty, new GUIContent("Normals"), true);

        EditorGUILayout.Space(12f);

        using (new EditorGUI.DisabledScope(_materials == null || _materials.Length == 0))
        {
            if (GUILayout.Button("Assign Textures To Materials", GUILayout.Height(35f)))
            {
                AssignTexturesToMaterials();
            }
        }

        _serializedObject.ApplyModifiedProperties();
    }

    private void AssignTexturesToMaterials()
    {
        if (_materials == null || _baseColorTextures == null || _normalTextures == null)
        {
            Debug.LogError("Один из массивов не заполнен.");
            return;
        }

        int count = _materials.Length;

        if (_baseColorTextures.Length != count || _normalTextures.Length != count)
        {
            Debug.LogError(
                $"Размеры массивов не совпадают:\n" +
                $"Materials: {_materials.Length}\n" +
                $"Textures: {_baseColorTextures.Length}\n" +
                $"Normals: {_normalTextures.Length}"
            );

            return;
        }

        int assignedCount = 0;

        for (int i = 0; i < count; i++)
        {
            Material material = _materials[i];
            Texture2D baseColorTexture = _baseColorTextures[i];
            Texture2D normalTexture = _normalTextures[i];

            if (material == null)
            {
                Debug.LogWarning($"Element {i}: материал не назначен, пропуск.");
                continue;
            }

            Undo.RecordObject(material, "Assign Textures To Material");

            if (baseColorTexture != null)
            {
                if (material.HasProperty(BaseColorMapProperty))
                {
                    material.SetTexture(BaseColorMapProperty, baseColorTexture);
                }
                else
                {
                    Debug.LogWarning(
                        $"{material.name}: shader не содержит property {BaseColorMapProperty}"
                    );
                }
            }

            if (normalTexture != null)
            {
                if (material.HasProperty(NormalMapProperty))
                {
                    material.SetTexture(NormalMapProperty, normalTexture);

                    // Для HDRP/Lit обычно достаточно SetTexture,
                    // но keyword полезен для корректного включения normal map.
                    material.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    Debug.LogWarning(
                        $"{material.name}: shader не содержит property {NormalMapProperty}"
                    );
                }
            }

            EditorUtility.SetDirty(material);
            assignedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Assign Textures To Materials завершено. Обработано материалов: {assignedCount}");
    }
}