using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace XRay.Importer.Editor
{
    [CustomEditor(typeof(XRayOmfImporter))]
    public sealed class XRayOmfImporterEditor : ScriptedImporterEditor
    {
        private const float FRAME_RATE = 30.0f;

        private string _filter = string.Empty;
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty rigGuid = serializedObject.FindProperty("_rigGuid");
            SerializedProperty excluded = serializedObject.FindProperty("_excludedMotions");

            EditorGUILayout.LabelField("Rig", EditorStyles.boldLabel);
            DrawRigField(rigGuid);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoDetectRig"));
            EditorGUILayout.HelpBox("Scale and coordinate system come from the rig's OGF importer; reimporting the rig reimports these motions.", MessageType.None);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_loopMode"));

            var importer = (XRayOmfImporter)target;
            var library = AssetDatabase.LoadAssetAtPath<XRayMotionLibrary>(importer.assetPath);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
            if (library == null || library.Motions.Count == 0)
            {
                EditorGUILayout.HelpBox("No motion information. The last import failed; see the Console for the reason.", MessageType.Warning);
            }
            else
            {
                DrawMotions(library, excluded);
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        private static void DrawRigField(SerializedProperty rigGuid)
        {
            string path = string.IsNullOrEmpty(rigGuid.stringValue) ? null : AssetDatabase.GUIDToAssetPath(rigGuid.stringValue);
            GameObject current = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorGUI.BeginChangeCheck();
            var selected = (GameObject)EditorGUILayout.ObjectField("OGF Rig", current, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                string selectedPath = selected != null ? AssetDatabase.GetAssetPath(selected) : null;
                if (selected == null)
                {
                    rigGuid.stringValue = string.Empty;
                }
                else if (selectedPath.EndsWith(".ogf", System.StringComparison.OrdinalIgnoreCase))
                {
                    rigGuid.stringValue = AssetDatabase.AssetPathToGUID(selectedPath);
                }
                else
                {
                    Debug.LogWarning($"'{selectedPath}' is not an *.ogf model; the rig must be an imported OGF.");
                }
            }
        }

        private void DrawMotions(XRayMotionLibrary library, SerializedProperty excluded)
        {
            var excludedSet = new HashSet<string>();
            for (int i = 0; i < excluded.arraySize; i++)
            {
                excludedSet.Add(excluded.GetArrayElementAtIndex(i).stringValue);
            }

            int clips = 0;
            float totalSeconds = 0.0f;
            foreach (XRayMotionEntry entry in library.Motions)
            {
                if (entry.Clip != null)
                {
                    clips++;
                }

                totalSeconds += Mathf.Max(0, entry.FrameCount - 1) / FRAME_RATE;
            }

            EditorGUILayout.LabelField("Rig", string.IsNullOrEmpty(library.RigName) ? "<none>" : library.RigName);
            EditorGUILayout.LabelField("Motions", $"{library.Motions.Count} ({clips} clips generated)");
            EditorGUILayout.LabelField("Bones", library.BoneNames.Count.ToString());
            EditorGUILayout.LabelField("Frame Rate", $"{library.FrameRate:0} fps");
            EditorGUILayout.LabelField("Total Duration", $"{totalSeconds:0.00} s");
            EditorGUILayout.LabelField("Params Version", library.ParamsVersion.ToString());

            EditorGUILayout.BeginHorizontal();
            _filter = EditorGUILayout.TextField("Filter", _filter);
            if (GUILayout.Button("All", GUILayout.Width(40.0f)))
            {
                excluded.ClearArray();
            }

            if (GUILayout.Button("None", GUILayout.Width(44.0f)))
            {
                excluded.ClearArray();
                foreach (XRayMotionEntry entry in library.Motions)
                {
                    AppendString(excluded, entry.Name);
                }
            }

            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(360.0f));
            foreach (XRayMotionEntry entry in library.Motions)
            {
                if (_filter.Length > 0 && entry.Name.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool included = !excludedSet.Contains(entry.Name);
                float duration = Mathf.Max(0, entry.FrameCount - 1) / FRAME_RATE;
                string flags = entry.StopAtEnd ? "stop" : "loop";
                bool toggled = EditorGUILayout.ToggleLeft($"{entry.Name}   {entry.FrameCount} fr, {duration:0.00} s, {flags}, speed {entry.Speed:0.##}", included);
                if (toggled != included)
                {
                    if (toggled)
                    {
                        RemoveString(excluded, entry.Name);
                    }
                    else
                    {
                        AppendString(excluded, entry.Name);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void AppendString(SerializedProperty array, string value)
        {
            array.InsertArrayElementAtIndex(array.arraySize);
            array.GetArrayElementAtIndex(array.arraySize - 1).stringValue = value;
        }

        private static void RemoveString(SerializedProperty array, string value)
        {
            for (int i = array.arraySize - 1; i >= 0; i--)
            {
                if (array.GetArrayElementAtIndex(i).stringValue == value)
                {
                    array.DeleteArrayElementAtIndex(i);
                }
            }
        }
    }
}
