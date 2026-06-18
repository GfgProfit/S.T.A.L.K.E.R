#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class HdrpTextureNormalMaterialGenerator : EditorWindow
{
    [SerializeField] private Texture2D[] _textures;
    [SerializeField] private Texture2D[] _normals;
    [SerializeField] private Texture2D[] _maskMaps;

    [SerializeField] private DefaultAsset _outputFolder;

    [SerializeField] private bool _overwriteExistingMaterials = true;
    [SerializeField] private bool _autoSetNormalsImportType = true;
    [SerializeField] private bool _requireMaskMap = false;

    [SerializeField, Range(0f, 8f)] private float _normalScale = 1f;

    private SerializedObject _serializedObject;

    private const string WINDOW_TITLE = "HDRP Material Generator";
    private const string MENU_PATH = "Tools/Materials/HDRP Texture + Normal + Mask Generator";

    private const string HDRP_LIT_SHADER_NAME = "HDRP/Lit";

    private const string BASE_COLOR_MAP = "_BaseColorMap";
    private const string NORMAL_MAP = "_NormalMap";
    private const string MASK_MAP = "_MaskMap";

    private const string NORMAL_SCALE = "_NormalScale";
    private const string NORMAL_MAP_SPACE = "_NormalMapSpace";

    private const string TANGENT_NORMAL_KEYWORD = "_NORMALMAP_TANGENT_SPACE";
    private const string OBJECT_NORMAL_KEYWORD = "_NORMALMAP_OBJECT_SPACE";
    private const string MASK_MAP_KEYWORD = "_MASKMAP";

    private static readonly string[] MASK_SUFFIXES =
    {
        "_mask",
        "_maskmap",
        "_m"
    };

    [MenuItem(MENU_PATH)]
    private static void Open()
    {
        HdrpTextureNormalMaterialGenerator window = GetWindow<HdrpTextureNormalMaterialGenerator>();
        window.titleContent = new GUIContent(WINDOW_TITLE);
        window.minSize = new Vector2(560f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);
    }

    private void OnGUI()
    {
        _serializedObject.Update();

        EditorGUILayout.Space(6f);

        EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_textures)),
            new GUIContent("Textures"),
            true
        );

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_normals)),
            new GUIContent("Normals"),
            true
        );

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_maskMaps)),
            new GUIContent("Mask Maps"),
            true
        );

        EditorGUILayout.Space(8f);

        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_outputFolder)),
            new GUIContent("Output Folder")
        );

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_overwriteExistingMaterials)),
            new GUIContent("Overwrite Existing Materials")
        );

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_autoSetNormalsImportType)),
            new GUIContent("Auto Set Normals Import Type")
        );

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_requireMaskMap)),
            new GUIContent("Require Mask Map")
        );

        EditorGUILayout.PropertyField(
            _serializedObject.FindProperty(nameof(_normalScale)),
            new GUIContent("Normal Scale")
        );

        EditorGUILayout.Space(12f);

        using (new EditorGUI.DisabledScope(_textures == null || _textures.Length == 0))
        {
            if (GUILayout.Button("Create HDRP Materials", GUILayout.Height(34f)))
            {
                CreateMaterials();
            }
        }

        _serializedObject.ApplyModifiedProperties();
    }

    private void CreateMaterials()
    {
        Shader hdrpLitShader = Shader.Find(HDRP_LIT_SHADER_NAME);

        if (hdrpLitShader == null)
        {
            EditorUtility.DisplayDialog(
                "HDRP/Lit not found",
                $"Shader '{HDRP_LIT_SHADER_NAME}' was not found.\n\nCheck that HDRP is installed and active.",
                "OK"
            );

            return;
        }

        string outputFolderPath = GetOutputFolderPath();

        if (string.IsNullOrWhiteSpace(outputFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Invalid folder",
                "Output Folder must be inside the Assets folder.",
                "OK"
            );

            return;
        }

        EnsureFolderExists(outputFolderPath);

        Dictionary<string, Texture2D> normalByBaseName = BuildNormalLookup();
        Dictionary<string, Texture2D> maskByBaseName = BuildMaskLookup();

        int created = 0;
        int updated = 0;
        int skipped = 0;

        int nullTextures = 0;
        int existingSkipped = 0;

        int missingNormalsCount = 0;
        int missingMasksCount = 0;
        int skippedByMissingMask = 0;

        List<string> missingNormalTextures = new List<string>();
        List<string> missingMaskTextures = new List<string>();

        List<string> createdMaterials = new List<string>();
        List<string> updatedMaterials = new List<string>();
        List<string> skippedMaterials = new List<string>();

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (Texture2D texture in _textures)
            {
                if (texture == null)
                {
                    skipped++;
                    nullTextures++;
                    skippedMaterials.Add("NULL texture slot");
                    continue;
                }

                string textureName = GetCleanAssetName(texture.name);
                string expectedNormalName = $"{textureName}_n";

                if (!normalByBaseName.TryGetValue(textureName, out Texture2D normal))
                {
                    skipped++;
                    missingNormalsCount++;

                    missingNormalTextures.Add($"{textureName} -> expected normal: {expectedNormalName}");
                    skippedMaterials.Add($"{textureName} -> normal not found, material was NOT created");

                    Debug.LogWarning(
                        $"[HDRP Material Generator] Normal not found for texture '{textureName}'. " +
                        $"Expected normal name: '{expectedNormalName}'. Material was NOT created."
                    );

                    continue;
                }

                maskByBaseName.TryGetValue(textureName, out Texture2D maskMap);

                if (maskMap == null)
                {
                    missingMasksCount++;

                    string expectedMaskNames =
                        $"{textureName}_mask / {textureName}_maskmap / {textureName}_m";

                    missingMaskTextures.Add($"{textureName} -> expected mask: {expectedMaskNames}");

                    if (_requireMaskMap)
                    {
                        skipped++;
                        skippedByMissingMask++;

                        skippedMaterials.Add($"{textureName} -> mask map not found, material was NOT created");

                        Debug.LogWarning(
                            $"[HDRP Material Generator] Mask Map not found for texture '{textureName}'. " +
                            $"Expected: {expectedMaskNames}. Material was NOT created because Require Mask Map is enabled."
                        );

                        continue;
                    }

                    Debug.LogWarning(
                        $"[HDRP Material Generator] Mask Map not found for texture '{textureName}'. " +
                        "Material will be created without Mask Map."
                    );
                }

                if (_autoSetNormalsImportType)
                {
                    TrySetTextureAsNormalMap(normal);
                }

                string materialName = textureName;
                string materialPath = $"{outputFolderPath}/{materialName}.mat";

                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material != null && !_overwriteExistingMaterials)
                {
                    skipped++;
                    existingSkipped++;

                    skippedMaterials.Add($"{materialName} -> material already exists: {materialPath}");

                    Debug.LogWarning($"[HDRP Material Generator] Material already exists, skipped: {materialPath}");
                    continue;
                }

                bool isNewMaterial = material == null;

                if (isNewMaterial)
                {
                    material = new Material(hdrpLitShader);
                    material.name = materialName;
                }
                else
                {
                    material.shader = hdrpLitShader;
                    material.name = materialName;
                }

                ApplyHdrpLitTextures(material, texture, normal, maskMap);

                if (isNewMaterial)
                {
                    AssetDatabase.CreateAsset(material, materialPath);
                    created++;
                    createdMaterials.Add($"{materialName} -> {materialPath}");
                }
                else
                {
                    EditorUtility.SetDirty(material);
                    updated++;
                    updatedMaterials.Add($"{materialName} -> {materialPath}");
                }

                Debug.Log($"[HDRP Material Generator] Material ready: {materialPath}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        int totalMade = created + updated;

        string logPath = CreateDesktopLog(
            outputFolderPath,
            created,
            updated,
            totalMade,
            skipped,
            nullTextures,
            existingSkipped,
            missingNormalsCount,
            missingMasksCount,
            skippedByMissingMask,
            missingNormalTextures,
            missingMaskTextures,
            createdMaterials,
            updatedMaterials,
            skippedMaterials
        );

        EditorUtility.DisplayDialog(
            "HDRP Materials Created",
            $"Created: {created}\n" +
            $"Updated: {updated}\n" +
            $"Total made: {totalMade}\n" +
            $"Skipped: {skipped}\n" +
            $"Missing normals: {missingNormalsCount}\n" +
            $"Missing mask maps: {missingMasksCount}\n" +
            $"Skipped by missing mask: {skippedByMissingMask}\n\n" +
            $"Log saved:\n{logPath}",
            "OK"
        );
    }

    private Dictionary<string, Texture2D> BuildNormalLookup()
    {
        Dictionary<string, Texture2D> result = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        if (_normals == null)
        {
            return result;
        }

        foreach (Texture2D normal in _normals)
        {
            if (normal == null)
            {
                continue;
            }

            string normalName = GetCleanAssetName(normal.name);

            if (!TryGetBaseNameFromNormalName(normalName, out string baseName))
            {
                Debug.LogWarning($"[HDRP Material Generator] Normal skipped because name does not end with '_n': {normalName}");
                continue;
            }

            if (result.ContainsKey(baseName))
            {
                Debug.LogWarning($"[HDRP Material Generator] Duplicate normal for '{baseName}' skipped: {normal.name}");
                continue;
            }

            result.Add(baseName, normal);
        }

        return result;
    }

    private Dictionary<string, Texture2D> BuildMaskLookup()
    {
        Dictionary<string, Texture2D> result = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        if (_maskMaps == null)
        {
            return result;
        }

        foreach (Texture2D maskMap in _maskMaps)
        {
            if (maskMap == null)
            {
                continue;
            }

            string maskName = GetCleanAssetName(maskMap.name);

            if (!TryGetBaseNameFromMaskName(maskName, out string baseName))
            {
                Debug.LogWarning(
                    $"[HDRP Material Generator] Mask Map skipped because name does not end with supported suffix: {maskName}. " +
                    "Supported suffixes: _mask, _maskmap, _m"
                );

                continue;
            }

            if (result.ContainsKey(baseName))
            {
                Debug.LogWarning($"[HDRP Material Generator] Duplicate mask map for '{baseName}' skipped: {maskMap.name}");
                continue;
            }

            result.Add(baseName, maskMap);
        }

        return result;
    }

    private static bool TryGetBaseNameFromNormalName(string normalName, out string baseName)
    {
        baseName = null;

        if (string.IsNullOrWhiteSpace(normalName))
        {
            return false;
        }

        const string normalSuffix = "_n";

        if (!normalName.EndsWith(normalSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        baseName = normalName.Substring(0, normalName.Length - normalSuffix.Length);
        return !string.IsNullOrWhiteSpace(baseName);
    }

    private static bool TryGetBaseNameFromMaskName(string maskName, out string baseName)
    {
        baseName = null;

        if (string.IsNullOrWhiteSpace(maskName))
        {
            return false;
        }

        foreach (string suffix in MASK_SUFFIXES)
        {
            if (!maskName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            baseName = maskName.Substring(0, maskName.Length - suffix.Length);
            return !string.IsNullOrWhiteSpace(baseName);
        }

        return false;
    }

    private void ApplyHdrpLitTextures(
        Material material,
        Texture2D texture,
        Texture2D normal,
        Texture2D maskMap
    )
    {
        if (material.HasProperty(BASE_COLOR_MAP))
        {
            material.SetTexture(BASE_COLOR_MAP, texture);
        }
        else
        {
            Debug.LogWarning($"[HDRP Material Generator] Material does not have property: {BASE_COLOR_MAP}");
        }

        if (material.HasProperty(NORMAL_MAP))
        {
            material.SetTexture(NORMAL_MAP, normal);
        }
        else
        {
            Debug.LogWarning($"[HDRP Material Generator] Material does not have property: {NORMAL_MAP}");
        }

        if (maskMap != null)
        {
            if (material.HasProperty(MASK_MAP))
            {
                material.SetTexture(MASK_MAP, maskMap);
                material.EnableKeyword(MASK_MAP_KEYWORD);
            }
            else
            {
                Debug.LogWarning($"[HDRP Material Generator] Material does not have property: {MASK_MAP}");
            }
        }
        else
        {
            if (material.HasProperty(MASK_MAP))
            {
                material.SetTexture(MASK_MAP, null);
            }

            material.DisableKeyword(MASK_MAP_KEYWORD);
        }

        if (material.HasProperty(NORMAL_SCALE))
        {
            material.SetFloat(NORMAL_SCALE, _normalScale);
        }

        // 0 = Tangent Space normal map in HDRP/Lit.
        if (material.HasProperty(NORMAL_MAP_SPACE))
        {
            material.SetFloat(NORMAL_MAP_SPACE, 0f);
        }

        material.EnableKeyword(TANGENT_NORMAL_KEYWORD);
        material.DisableKeyword(OBJECT_NORMAL_KEYWORD);

        EditorUtility.SetDirty(material);
    }

    private string GetOutputFolderPath()
    {
        if (_outputFolder == null)
        {
            return "Assets/GeneratedMaterials";
        }

        string path = AssetDatabase.GetAssetPath(_outputFolder);

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            return null;
        }

        if (!path.StartsWith("Assets", StringComparison.Ordinal))
        {
            return null;
        }

        return path;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static void TrySetTextureAsNormalMap(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        if (importer.textureType == TextureImporterType.NormalMap)
        {
            return;
        }

        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();

        Debug.Log($"[HDRP Material Generator] Set texture import type to Normal Map: {path}");
    }

    private static string CreateDesktopLog(
        string outputFolderPath,
        int created,
        int updated,
        int totalMade,
        int skipped,
        int nullTextures,
        int existingSkipped,
        int missingNormalsCount,
        int missingMasksCount,
        int skippedByMissingMask,
        List<string> missingNormalTextures,
        List<string> missingMaskTextures,
        List<string> createdMaterials,
        List<string> updatedMaterials,
        List<string> skippedMaterials
    )
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        if (string.IsNullOrWhiteSpace(desktopPath))
        {
            desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        string fileName = $"HDRP_MATERIAL_GENERATOR_LOG_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        string logPath = Path.Combine(desktopPath, fileName);

        List<string> lines = new List<string>
        {
            "HDRP MATERIAL GENERATOR LOG",
            "===========================",
            $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Output folder: {outputFolderPath}",
            "",
            "SUMMARY",
            "-------",
            $"Created materials: {created}",
            $"Updated materials: {updated}",
            $"Total made materials: {totalMade}",
            $"Skipped total: {skipped}",
            $"Skipped null texture slots: {nullTextures}",
            $"Skipped existing materials: {existingSkipped}",
            $"Missing normals: {missingNormalsCount}",
            $"Missing mask maps: {missingMasksCount}",
            $"Skipped by missing mask: {skippedByMissingMask}",
            "",
            "MISSING NORMALS",
            "---------------"
        };

        AddListToLog(lines, missingNormalTextures);

        lines.Add("");
        lines.Add("MISSING MASK MAPS");
        lines.Add("-----------------");

        AddListToLog(lines, missingMaskTextures);

        lines.Add("");
        lines.Add("CREATED MATERIALS");
        lines.Add("-----------------");

        AddListToLog(lines, createdMaterials);

        lines.Add("");
        lines.Add("UPDATED MATERIALS");
        lines.Add("-----------------");

        AddListToLog(lines, updatedMaterials);

        lines.Add("");
        lines.Add("SKIPPED");
        lines.Add("-------");

        AddListToLog(lines, skippedMaterials);

        File.WriteAllLines(logPath, lines);
        Debug.Log($"[HDRP Material Generator] Log saved to desktop: {logPath}");

        return logPath;
    }

    private static void AddListToLog(List<string> lines, List<string> items)
    {
        if (items == null || items.Count == 0)
        {
            lines.Add("None");
            return;
        }

        foreach (string item in items)
        {
            lines.Add(item);
        }
    }

    private static string GetCleanAssetName(string assetName)
    {
        string name = Path.GetFileNameWithoutExtension(assetName);

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return name.Trim();
    }
}

#endif