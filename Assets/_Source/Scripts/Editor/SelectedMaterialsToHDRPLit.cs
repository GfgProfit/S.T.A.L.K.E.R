using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SelectedMaterialsToHDRPLit
{
    private const string HDRP_LIT_SHADER_NAME = "HDRP/Lit";
    private const string HDRP_BASE_COLOR_MAP = "_BaseColorMap";

    private static readonly string[] SourceTextureProperties =
    {
        "_BaseColorMap", // HDRP
        "_BaseMap",     // URP Lit
        "_MainTex",     // Built-in Standard / Legacy
        "_AlbedoMap",
        "_DiffuseMap"
    };

    [MenuItem("Tools/Materials/Convert Selected Materials To HDRP Lit")]
    private static void ConvertSelectedMaterials()
    {
        Material[] materials = Selection.GetFiltered<Material>(SelectionMode.Assets);

        if (materials == null || materials.Length == 0)
        {
            Debug.LogWarning("No materials selected. Select materials in Project window.");
            return;
        }

        Shader hdrpLitShader = Shader.Find(HDRP_LIT_SHADER_NAME);

        if (hdrpLitShader == null)
        {
            Debug.LogError($"Shader not found: {HDRP_LIT_SHADER_NAME}. Make sure HDRP is installed.");
            return;
        }

        int convertedCount = 0;

        foreach (Material material in materials)
        {
            if (material == null)
            {
                continue;
            }

            Texture sourceTexture = FindTexture(material);

            if (sourceTexture == null)
            {
                Debug.LogWarning($"Skipped '{material.name}': no texture found in known shader properties.");
                continue;
            }

            Undo.RecordObject(material, "Convert Material To HDRP Lit");

            material.shader = hdrpLitShader;
            material.SetTexture(HDRP_BASE_COLOR_MAP, sourceTexture);

            EditorUtility.SetDirty(material);

            Debug.Log($"Converted '{material.name}' -> HDRP/Lit, texture: '{sourceTexture.name}'");

            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Done. Converted materials: {convertedCount}");
    }

    private static Texture FindTexture(Material material)
    {
        foreach (string propertyName in SourceTextureProperties)
        {
            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);

                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return FindAnyTextureInShader(material);
    }

    private static Texture FindAnyTextureInShader(Material material)
    {
        Shader shader = material.shader;

        if (shader == null)
        {
            return null;
        }

        int propertyCount = shader.GetPropertyCount();

        for (int i = 0; i < propertyCount; i++)
        {
            if (shader.GetPropertyType(i) != ShaderPropertyType.Texture)
            {
                continue;
            }

            string propertyName = shader.GetPropertyName(i);

            if (!material.HasProperty(propertyName))
            {
                continue;
            }

            Texture texture = material.GetTexture(propertyName);

            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }
}