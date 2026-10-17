using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace XRay.Importer.Editor
{
    [CustomEditor(typeof(XRayOgfImporter))]
    public sealed class XRayOgfImporterEditor : ScriptedImporterEditor
    {
        private bool _showBones;
        private bool _showTextures = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_scale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_addAnimator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_readableMeshes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_updateWhenOffscreen"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            SerializedProperty importMaterials = serializedObject.FindProperty("_importMaterials");
            EditorGUILayout.PropertyField(importMaterials, new GUIContent("Generate HDRP Materials"));
            using (new EditorGUI.DisabledScope(!importMaterials.boolValue))
            {
                DrawFolderField(serializedObject.FindProperty("_texturesFolder"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_searchTexturesByName"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Embedded Motions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_importEmbeddedMotions"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_embeddedLoopMode"));
            serializedObject.ApplyModifiedProperties();

            DrawInfo();
            ApplyRevertGUI();
        }

        private void DrawInfo()
        {
            var importer = (XRayOgfImporter)target;
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            XRayModel model = root != null ? root.GetComponent<XRayModel>() : null;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
            if (model == null)
            {
                EditorGUILayout.HelpBox("No model information. The last import failed; see the Console for the reason.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Format", $"OGF v{model.FormatVersion}, {model.ModelType}");
            EditorGUILayout.LabelField("Meshes", model.MeshCount.ToString());
            EditorGUILayout.LabelField("Vertices", model.VertexCount.ToString());
            EditorGUILayout.LabelField("Triangles", model.TriangleCount.ToString());
            EditorGUILayout.LabelField("Bones", model.BoneNames.Count.ToString());
            EditorGUILayout.LabelField("Motion References", model.MotionReferences.Count.ToString());
            if (model.EmbeddedMotions != null)
            {
                EditorGUILayout.LabelField("Embedded Motions", model.EmbeddedMotions.Motions.Count.ToString());
            }

            if (!string.IsNullOrEmpty(model.LodReference))
            {
                EditorGUILayout.LabelField("LOD", model.LodReference);
            }

            _showTextures = EditorGUILayout.Foldout(_showTextures, $"Textures ({model.Textures.Count})", true);
            if (_showTextures)
            {
                EditorGUI.indentLevel++;
                foreach (string texture in model.Textures)
                {
                    EditorGUILayout.SelectableLabel(texture, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }

                EditorGUI.indentLevel--;
            }

            _showBones = EditorGUILayout.Foldout(_showBones, $"Bones ({model.BoneNames.Count})", true);
            if (_showBones)
            {
                EditorGUI.indentLevel++;
                foreach (string bone in model.BoneNames)
                {
                    EditorGUILayout.LabelField(bone);
                }

                EditorGUI.indentLevel--;
            }
        }

        private static void DrawFolderField(SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, new GUIContent("Textures Folder"));
            if (GUILayout.Button("...", GUILayout.Width(28.0f)))
            {
                string selected = EditorUtility.OpenFolderPanel("X-Ray textures folder (gamedata/textures)", "Assets", string.Empty);
                string projectRoot = System.IO.Path.GetFullPath(".").Replace('\\', '/') + "/";
                selected = selected.Replace('\\', '/');
                if (selected.StartsWith(projectRoot))
                {
                    property.stringValue = selected.Substring(projectRoot.Length);
                }
                else if (!string.IsNullOrEmpty(selected))
                {
                    Debug.LogWarning("The textures folder must be inside the project.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
