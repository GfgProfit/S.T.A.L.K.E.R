using UnityEditor;
using UnityEngine;

public sealed class ItemIconGeneratorWindow : EditorWindow
{
    private const string DefaultAssetPath = "Assets/Resources/" + ItemIconGeneratorSettings.DefaultResourcePath + ".asset";
    private const string ResourcesFolderPath = "Assets/Resources";

    private ItemIconGeneratorSettings settings;
    private SerializedObject settingsObject;
    private ItemData previewItem;
    private Texture2D previewTexture;
    private Vector2 settingsScroll;
    private bool autoRefresh = true;
    private bool previewRenderScheduled;

    [MenuItem("Tools/Inventory/Item Icon Generator")]
    private static void Open()
    {
        ItemIconGeneratorWindow window = GetWindow<ItemIconGeneratorWindow>();
        window.titleContent = new GUIContent("Item Icon Generator");
        window.minSize = new Vector2(820f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        settings = LoadOrCreateDefaultSettings();
        RebuildSerializedSettings();
        RequestPreviewRender();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        DestroyPreviewTexture();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (settings == null)
        {
            EditorGUILayout.HelpBox($"Default settings asset was not found at {DefaultAssetPath}.", MessageType.Warning);
            if (GUILayout.Button("Create Default Settings"))
            {
                settings = LoadOrCreateDefaultSettings();
                RebuildSerializedSettings();
            }

            return;
        }

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSettingsPanel();
            DrawPreviewPanel();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            previewItem = (ItemData)EditorGUILayout.ObjectField(previewItem, typeof(ItemData), false, GUILayout.MinWidth(220f));
            if (EditorGUI.EndChangeCheck())
            {
                RequestPreviewRender();
            }

            GUILayout.FlexibleSpace();

            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100f));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RequestPreviewRender();
            }

            if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton, GUILayout.Width(86f)))
            {
                ItemIconCache.Clear();
                RequestPreviewRender();
            }

            if (GUILayout.Button("Select SO", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }
    }

    private void DrawSettingsPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.54f)))
        {
            EditorGUILayout.LabelField("Global Generator Settings", EditorStyles.boldLabel);
            using (EditorGUI.DisabledScope disabledScope = new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Loaded SO", settings, typeof(ItemIconGeneratorSettings), false);
            }

            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
            EditorGUI.BeginChangeCheck();
            settingsObject.Update();
            DrawSerializedSettings(settingsObject);
            bool changed = EditorGUI.EndChangeCheck();
            settingsObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                ItemIconGeneratorSettings.ResetDefaultCache();
                ItemIconCache.Clear();

                if (autoRefresh)
                {
                    RequestPreviewRender();
                }
            }
        }
    }

    private void DrawPreviewPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(256f, 256f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(previewRect, new Color(0.14f, 0.14f, 0.14f, 1f));

            if (previewTexture == null)
            {
                string message = GetPreviewPlaceholderText();
                GUI.Label(previewRect, message, CenteredLabelStyle);
                return;
            }

            Rect imageRect = GetCenteredImageRect(previewRect, previewTexture);
            EditorGUI.DrawTextureTransparent(imageRect, previewTexture, ScaleMode.ScaleToFit);

            EditorGUILayout.LabelField($"{previewTexture.width} x {previewTexture.height}", EditorStyles.miniLabel);
        }
    }

    private void DrawSerializedSettings(SerializedObject serializedSettings)
    {
        SerializedProperty property = serializedSettings.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                EditorGUILayout.PropertyField(property, true);
            }

            enterChildren = false;
        }
    }

    private void RenderPreview()
    {
        DestroyPreviewTexture();

        if (CanRenderPreview() == false)
        {
            Repaint();
            return;
        }

        try
        {
            previewTexture = ItemIconCache.RenderPreviewTexture(previewItem, settings);
            if (previewTexture != null)
            {
                previewTexture.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            previewTexture = null;
        }

        Repaint();
    }

    private void RequestPreviewRender()
    {
        if (previewRenderScheduled)
        {
            return;
        }

        previewRenderScheduled = true;
        EditorApplication.delayCall += RenderPreviewIfScheduled;
    }

    private void RenderPreviewIfScheduled()
    {
        previewRenderScheduled = false;

        if (this == null)
        {
            return;
        }

        RenderPreview();
    }

    private bool CanRenderPreview()
    {
        if (settings == null || previewItem == null)
        {
            return false;
        }

        return EditorApplication.isPlayingOrWillChangePlaymode == false
            && EditorApplication.isCompiling == false
            && EditorApplication.isUpdating == false;
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            DestroyPreviewTexture();
            Repaint();
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            RequestPreviewRender();
        }
    }

    private string GetPreviewPlaceholderText()
    {
        if (previewItem == null)
        {
            return "Assign ItemData for preview";
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return "Preview is disabled in Play Mode";
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return "Preview is waiting for editor update";
        }

        return "No generated preview";
    }

    private void DestroyPreviewTexture()
    {
        if (previewTexture == null)
        {
            return;
        }

        DestroyImmediate(previewTexture);
        previewTexture = null;
    }

    private void RebuildSerializedSettings()
    {
        settingsObject = settings == null ? null : new SerializedObject(settings);
    }

    private static ItemIconGeneratorSettings LoadOrCreateDefaultSettings()
    {
        ItemIconGeneratorSettings loadedSettings = AssetDatabase.LoadAssetAtPath<ItemIconGeneratorSettings>(DefaultAssetPath);
        if (loadedSettings != null)
        {
            return loadedSettings;
        }

        if (AssetDatabase.IsValidFolder(ResourcesFolderPath) == false)
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        loadedSettings = CreateInstance<ItemIconGeneratorSettings>();
        AssetDatabase.CreateAsset(loadedSettings, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(DefaultAssetPath);
        ItemIconGeneratorSettings.ResetDefaultCache();
        return loadedSettings;
    }

    private static Rect GetCenteredImageRect(Rect containerRect, Texture2D texture)
    {
        float scale = Mathf.Min(containerRect.width / texture.width, containerRect.height / texture.height);
        float width = texture.width * scale;
        float height = texture.height * scale;
        return new Rect(
            containerRect.x + (containerRect.width - width) * 0.5f,
            containerRect.y + (containerRect.height - height) * 0.5f,
            width,
            height);
    }

    private static GUIStyle centeredLabelStyle;
    private static GUIStyle CenteredLabelStyle
    {
        get
        {
            if (centeredLabelStyle == null)
            {
                centeredLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }

            return centeredLabelStyle;
        }
    }
}
