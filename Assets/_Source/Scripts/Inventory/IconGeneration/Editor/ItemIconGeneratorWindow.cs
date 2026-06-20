using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public sealed class ItemIconGeneratorWindow : EditorWindow
{
    private const string DEFAULT_ASSET_PATH = "Assets/Resources/" + ItemIconGeneratorSettings.DEFAULT_RESOURCE_PATH + ".asset";
    private const string RESOURCES_FOLDER_PATH = "Assets/Resources";

    private ItemIconGeneratorSettings _settings;
    private SerializedObject _settingsObject;
    private ItemData _previewItem;
    private Texture2D _previewTexture;
    private Vector2 _settingsScroll;
    private bool _autoRefresh = true;
    private bool _previewRenderScheduled;
    private CancellationTokenSource _previewRenderCancellation;

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
        _settings = LoadOrCreateDefaultSettings();
        RebuildSerializedSettings();
        RequestPreviewRender();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        CancelPreviewRender();
        DestroyPreviewTexture();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_settings == null)
        {
            EditorGUILayout.HelpBox($"Default _settings asset was not found at {DEFAULT_ASSET_PATH}.", MessageType.Warning);

            if (GUILayout.Button("Create Default Settings"))
            {
                _settings = LoadOrCreateDefaultSettings();
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
            _previewItem = (ItemData)EditorGUILayout.ObjectField(_previewItem, typeof(ItemData), false, GUILayout.MinWidth(220f));

            if (EditorGUI.EndChangeCheck())
            {
                RequestPreviewRender();
            }

            GUILayout.FlexibleSpace();

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100f));

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
                Selection.activeObject = _settings;
                EditorGUIUtility.PingObject(_settings);
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
                EditorGUILayout.ObjectField("Loaded SO", _settings, typeof(ItemIconGeneratorSettings), false);
            }

            _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);
            EditorGUI.BeginChangeCheck();
            _settingsObject.Update();
            DrawSerializedSettings(_settingsObject);
            bool changed = EditorGUI.EndChangeCheck();
            _settingsObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            if (changed)
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                ItemIconGeneratorSettings.ResetDefaultCache();
                ItemIconCache.Clear();

                if (_autoRefresh)
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

            if (_previewTexture == null)
            {
                string message = GetPreviewPlaceholderText();
                GUI.Label(previewRect, message, CenteredLabelStyle);
                return;
            }

            Rect imageRect = GetCenteredImageRect(previewRect, _previewTexture);
            EditorGUI.DrawTextureTransparent(imageRect, _previewTexture, ScaleMode.ScaleToFit);

            EditorGUILayout.LabelField($"{_previewTexture.width} x {_previewTexture.height}", EditorStyles.miniLabel);
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

    private async UniTask RenderPreviewAsync()
    {
        DestroyPreviewTexture();

        if (CanRenderPreview() == false)
        {
            Repaint();
            return;
        }

        CancellationTokenSource cancellation = new();
        _previewRenderCancellation = cancellation;

        try
        {
            Texture2D previewTexture = await ItemIconCache.RenderPreviewTextureAsync(_previewItem, _settings, cancellation.Token);

            if (cancellation.IsCancellationRequested || this == null)
            {
                DestroyImmediate(previewTexture);
                return;
            }

            _previewTexture = previewTexture;

            if (_previewTexture != null)
            {
                _previewTexture.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _previewTexture = null;
        }
        finally
        {
            if (ReferenceEquals(_previewRenderCancellation, cancellation))
            {
                _previewRenderCancellation = null;
            }

            cancellation.Dispose();
        }

        if (this != null)
        {
            Repaint();
        }
    }

    private void RequestPreviewRender()
    {
        CancelPreviewRender();

        if (_previewRenderScheduled)
        {
            return;
        }

        _previewRenderScheduled = true;
        EditorApplication.delayCall += RenderPreviewIfScheduled;
    }

    private void RenderPreviewIfScheduled()
    {
        _previewRenderScheduled = false;

        if (this == null)
        {
            return;
        }

        RenderPreviewAsync().Forget(Debug.LogException);
    }

    private bool CanRenderPreview()
    {
        if (_settings == null || _previewItem == null)
        {
            return false;
        }

        return EditorApplication.isPlayingOrWillChangePlaymode == false && EditorApplication.isCompiling == false && EditorApplication.isUpdating == false;
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
        {
            CancelPreviewRender();
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
        if (_previewItem == null)
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
        if (_previewTexture == null)
        {
            return;
        }

        DestroyImmediate(_previewTexture);
        _previewTexture = null;
    }

    private void CancelPreviewRender()
    {
        if (_previewRenderCancellation == null)
        {
            return;
        }

        _previewRenderCancellation.Cancel();
        _previewRenderCancellation = null;
    }

    private void RebuildSerializedSettings()
    {
        _settingsObject = _settings == null ? null : new SerializedObject(_settings);
    }

    internal static ItemIconGeneratorSettings LoadOrCreateDefaultSettings()
    {
        ItemIconGeneratorSettings loadedSettings = AssetDatabase.LoadAssetAtPath<ItemIconGeneratorSettings>(DEFAULT_ASSET_PATH);

        if (loadedSettings != null)
        {
            return loadedSettings;
        }

        if (AssetDatabase.IsValidFolder(RESOURCES_FOLDER_PATH) == false)
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        loadedSettings = CreateInstance<ItemIconGeneratorSettings>();
        AssetDatabase.CreateAsset(loadedSettings, DEFAULT_ASSET_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(DEFAULT_ASSET_PATH);
        ItemIconGeneratorSettings.ResetDefaultCache();
        return loadedSettings;
    }

    private static Rect GetCenteredImageRect(Rect containerRect, Texture2D texture)
    {
        float scale = Mathf.Min(containerRect.width / texture.width, containerRect.height / texture.height);
        float width = texture.width * scale;
        float height = texture.height * scale;

        return new Rect(containerRect.x + (containerRect.width - width) * 0.5f, containerRect.y + (containerRect.height - height) * 0.5f, width, height);
    }

    private static GUIStyle _centeredLabelStyle;
    private static GUIStyle CenteredLabelStyle
    {
        get
        {
            _centeredLabelStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

            return _centeredLabelStyle;
        }
    }
}
