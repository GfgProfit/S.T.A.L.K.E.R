using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public sealed class ItemIconGeneratorWindow : EditorWindow
{
    private const string DEFAULT_ASSET_PATH = "Assets/Resources/" + ItemIconGeneratorSettings.DEFAULT_RESOURCE_PATH + ".asset";
    private const string RESOURCES_FOLDER_PATH = "Assets/Resources";
    private const int LARGE_BAKE_CONFIRMATION_THRESHOLD = 2000;
    private const float DEFAULT_VARIANT_COUNT_HEIGHT = 82f;
    private const float MIN_VARIANT_COUNT_HEIGHT = 32f;
    private const float MAX_VARIANT_COUNT_HEIGHT = 260f;
    private const float VARIANT_COUNT_RESIZE_HANDLE_HEIGHT = 6f;

    private static readonly ItemType[] BAKE_ITEM_TYPES = (ItemType[])Enum.GetValues(typeof(ItemType));
    private static readonly string[] BAKE_ITEM_TYPE_LABELS = Enum.GetNames(typeof(ItemType));
    private static readonly int ALL_BAKE_ITEM_TYPE_MASK = BuildAllBakeItemTypeMask();

    private ItemIconGeneratorSettings _settings;
    private SerializedObject _settingsObject;
    private ItemData _previewItem;
    private Texture2D _previewTexture;
    private Vector2 _settingsScroll;
    private Vector2 _previewScroll;
    private bool _autoRefresh = true;
    private bool _previewRenderScheduled;
    private CancellationTokenSource _previewRenderCancellation;
    private CancellationTokenSource _bakeCancellation;
    private ItemIconVariantAnalysis _allVariantAnalysis;
    private ItemIconVariantAnalysis _previewVariantAnalysis;
    private string[] _previewVariantLabels = Array.Empty<string>();
    private int _selectedVariantIndex;
    private int _selectedBakeItemTypeMask = GetBakeItemTypeMask(ItemType.Weapon);
    private bool _isBaking;
    private string _bakeStatus = "Analysis not refreshed";
    private Vector2 _variantCountScroll;
    private float _variantCountHeight = DEFAULT_VARIANT_COUNT_HEIGHT;
    private bool _isResizingVariantCount;

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
        RefreshVariantAnalysis();
        RequestPreviewRender();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        CancelPreviewRender();
        CancelBake();
        EditorUtility.ClearProgressBar();
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
        DrawBakePanel();
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
                RefreshPreviewVariantAnalysis();
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

            if (GUILayout.Button("Analyze", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RefreshVariantAnalysis();
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
                RefreshVariantAnalysis();

                if (_autoRefresh)
                {
                    RequestPreviewRender();
                }
            }
        }
    }

    private void DrawBakePanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Editor-baked Catalog", EditorStyles.boldLabel);

            if (_allVariantAnalysis != null)
            {
                EditorGUILayout.LabelField(
                    $"Total: {_allVariantAnalysis.Variants.Count} | Default: {_allVariantAnalysis.DefaultVariantCount} | Slot: {_allVariantAnalysis.SlotVariantCount} | Estimated raw RGBA: {EditorUtility.FormatBytes(_allVariantAnalysis.EstimatedTextureBytes)}");

                if (_allVariantAnalysis.ExcessiveVariantItems.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"{_allVariantAnalysis.ExcessiveVariantItems.Count} weapon(s) exceed {ItemIconVariantEnumerator.LARGE_WEAPON_VARIANT_WARNING} variants. Review counts before baking.",
                        MessageType.Warning);
                }

                _variantCountScroll = EditorGUILayout.BeginScrollView(_variantCountScroll, GUILayout.Height(_variantCountHeight));

                foreach (KeyValuePair<ItemData, int> pair in _allVariantAnalysis.VariantCountsByItem)
                {
                    if (pair.Key.ItemType == ItemType.Weapon || pair.Key.ItemType == ItemType.Pistol)
                    {
                        EditorGUILayout.LabelField($"{pair.Key.ItemName}: {pair.Value}", EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndScrollView();
                DrawVariantCountResizeHandle();
            }

            if (_previewVariantLabels.Length > 0)
            {
                _selectedVariantIndex = EditorGUILayout.Popup("Selected Variant", Mathf.Clamp(_selectedVariantIndex, 0, _previewVariantLabels.Length - 1), _previewVariantLabels);
            }

            using (new EditorGUI.DisabledScope(_isBaking))
            using (new EditorGUILayout.HorizontalScope())
            {
                _selectedBakeItemTypeMask = EditorGUILayout.MaskField("Item Types", _selectedBakeItemTypeMask, BAKE_ITEM_TYPE_LABELS);

                if (GUILayout.Button("Bake Item Types", GUILayout.Width(140f)))
                {
                    StartBakeSelectedItemTypes();
                }
            }

            using (new EditorGUI.DisabledScope(_isBaking))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bake All"))
                {
                    StartBakeAll();
                }

                if (GUILayout.Button("Bake Selected ItemData"))
                {
                    StartBakeSelectedItems();
                }

                using (new EditorGUI.DisabledScope(_previewVariantAnalysis == null || _previewVariantAnalysis.Variants.Count == 0))
                {
                    if (GUILayout.Button("Bake Selected Variant"))
                    {
                        StartBakeSelectedVariant();
                    }
                }

                if (GUILayout.Button("Validate Catalog"))
                {
                    ValidateCatalog();
                }

                if (GUILayout.Button("Clear Generated Icons"))
                {
                    ClearGeneratedIcons();
                }
            }

            if (_isBaking && GUILayout.Button("Cancel Bake"))
            {
                CancelBake();
            }

            EditorGUILayout.LabelField(_bakeStatus, EditorStyles.miniLabel);
        }
    }

    private void StartBakeAll()
    {
        RefreshVariantAnalysis();

        if (_allVariantAnalysis == null || _allVariantAnalysis.Variants.Count == 0)
        {
            EditorUtility.DisplayDialog("Item Icon Bake", "No valid icon variants were found.", "OK");
            return;
        }

        if (RequiresLargeBakeConfirmation(_allVariantAnalysis) &&
            EditorUtility.DisplayDialog(
                "Large Item Icon Bake",
                $"Bake {_allVariantAnalysis.Variants.Count} icon variants? Estimated raw RGBA size is {EditorUtility.FormatBytes(_allVariantAnalysis.EstimatedTextureBytes)}.",
                "Bake",
                "Cancel") == false)
        {
            return;
        }

        RunBakeAsync(_allVariantAnalysis.Variants, true).Forget(Debug.LogException);
    }

    private void StartBakeSelectedItems()
    {
        List<ItemData> selectedItems = new();

        for (int i = 0; i < Selection.objects.Length; i++)
        {
            if (Selection.objects[i] is ItemData itemData && selectedItems.Contains(itemData) == false)
            {
                selectedItems.Add(itemData);
            }
        }

        if (selectedItems.Count == 0 && _previewItem != null)
        {
            selectedItems.Add(_previewItem);
        }

        ItemIconVariantAnalysis analysis = ItemIconVariantEnumerator.Enumerate(selectedItems, _settings);

        if (analysis.Variants.Count == 0)
        {
            EditorUtility.DisplayDialog("Item Icon Bake", "Select at least one valid ItemData with an icon source.", "OK");
            return;
        }

        if (RequiresLargeBakeConfirmation(analysis) &&
            EditorUtility.DisplayDialog("Large Item Icon Bake", $"Bake {analysis.Variants.Count} selected variants?", "Bake", "Cancel") == false)
        {
            return;
        }

        RunBakeAsync(analysis.Variants, false).Forget(Debug.LogException);
    }

    private void StartBakeSelectedItemTypes()
    {
        if (HasSelectedBakeItemTypes() == false)
        {
            EditorUtility.DisplayDialog("Item Icon Bake", "Select at least one item type.", "OK");
            return;
        }

        List<ItemData> selectedItems = new();
        List<ItemData> allItems = ItemDataIdValidator.LoadAllItems();

        for (int i = 0; i < allItems.Count; i++)
        {
            ItemData itemData = allItems[i];

            if (itemData != null && IsBakeItemTypeSelected(itemData.ItemType))
            {
                selectedItems.Add(itemData);
            }
        }

        ItemIconVariantAnalysis analysis = ItemIconVariantEnumerator.Enumerate(selectedItems, _settings);
        string itemTypeLabel = GetSelectedBakeItemTypeLabel();

        if (analysis.Variants.Count == 0)
        {
            EditorUtility.DisplayDialog("Item Icon Bake", $"No valid {itemTypeLabel} icon variants were found.", "OK");
            return;
        }

        if (RequiresLargeBakeConfirmation(analysis) &&
            EditorUtility.DisplayDialog(
                "Large Item Icon Bake",
                $"Bake {analysis.Variants.Count} {itemTypeLabel} icon variant(s)? Estimated raw RGBA size is {EditorUtility.FormatBytes(analysis.EstimatedTextureBytes)}.",
                "Bake",
                "Cancel") == false)
        {
            return;
        }

        RunBakeAsync(analysis.Variants, false).Forget(Debug.LogException);
    }

    private void StartBakeSelectedVariant()
    {
        if (_previewVariantAnalysis == null || _previewVariantAnalysis.Variants.Count == 0)
        {
            return;
        }

        int index = Mathf.Clamp(_selectedVariantIndex, 0, _previewVariantAnalysis.Variants.Count - 1);
        RunBakeAsync(new[] { _previewVariantAnalysis.Variants[index] }, false).Forget(Debug.LogException);
    }

    private async UniTask RunBakeAsync(IReadOnlyList<ItemIconBakeVariant> variants, bool replaceCatalog)
    {
        ItemDataIdValidationResult idValidation = ItemDataIdValidator.Validate(ItemDataIdValidator.LoadAllItems());

        if (idValidation.IsValid == false)
        {
            EditorUtility.DisplayDialog("Item Icon Bake", $"ItemId validation failed with {idValidation.Errors.Count} issue(s). Run Validate Item IDs first.", "OK");
            return;
        }

        CancelBake();
        _bakeCancellation = new CancellationTokenSource();
        CancellationTokenSource cancellation = _bakeCancellation;
        _isBaking = true;
        string resultStatus = null;

        try
        {
            int bakedCount = await ItemIconBakeCoordinator.BakeAsync(variants, _settings, replaceCatalog, UpdateBakeProgress, cancellation.Token);
            resultStatus = $"Baked {bakedCount} variant(s).";
        }
        catch (OperationCanceledException)
        {
            resultStatus = "Bake canceled. Completed variants were retained in the catalog.";
        }
        catch (Exception exception)
        {
            resultStatus = $"Bake failed: {exception.Message}";
            Debug.LogException(exception);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            _isBaking = false;

            if (ReferenceEquals(_bakeCancellation, cancellation))
            {
                _bakeCancellation = null;
            }

            cancellation.Dispose();
            RefreshVariantAnalysis();
            _bakeStatus = resultStatus ?? _bakeStatus;
            Repaint();
        }
    }

    private void UpdateBakeProgress(int completed, int total, ItemIconBakeVariant variant)
    {
        if (total <= 0)
        {
            return;
        }

        string label = variant == null ? "Saving catalog" : variant.DisplayName;
        _bakeStatus = $"Baking {Mathf.Min(completed + 1, total)}/{total}: {label}";
        bool canceled = EditorUtility.DisplayCancelableProgressBar("Baking Item Icons", label, Mathf.Clamp01((float)completed / total));

        if (canceled)
        {
            _bakeCancellation?.Cancel();
        }
    }

    private void ValidateCatalog()
    {
        ItemIconCatalogValidationResult result = ItemIconCatalogValidator.Validate(true);
        _bakeStatus = result.IsValid
            ? $"Catalog is valid: {result.EntryCount} entries."
            : $"Catalog validation failed: {result.Errors.Count} issue(s). See Console.";

        if (result.IsValid)
        {
            Debug.Log(_bakeStatus);
        }
        else
        {
            Debug.LogError($"{_bakeStatus}\n{string.Join("\n", result.Errors)}");
        }
    }

    private void ClearGeneratedIcons()
    {
        if (EditorUtility.DisplayDialog("Clear Generated Item Icons", "Delete all generated icon assets and clear the baked catalog?", "Clear", "Cancel") == false)
        {
            return;
        }

        ItemIconGeneratedAssetWriter.ClearGeneratedAssets();
        _bakeStatus = "Generated icons and catalog entries were cleared.";
        RefreshVariantAnalysis();
    }

    private void RefreshVariantAnalysis()
    {
        if (_settings == null)
        {
            _allVariantAnalysis = null;
            _previewVariantAnalysis = null;
            _previewVariantLabels = Array.Empty<string>();
            return;
        }

        List<ItemData> items = ItemDataIdValidator.LoadAllItems();
        ItemDataIdValidationResult validation = ItemDataIdValidator.Validate(items);

        if (validation.IsValid == false)
        {
            _allVariantAnalysis = null;
            _bakeStatus = $"ItemId validation failed: {validation.Errors.Count} issue(s).";
        }
        else
        {
            _allVariantAnalysis = ItemIconVariantEnumerator.Enumerate(items, _settings);
            _bakeStatus = $"Analyzed {_allVariantAnalysis.Variants.Count} bake variant(s).";
        }

        RefreshPreviewVariantAnalysis();
    }

    private void RefreshPreviewVariantAnalysis()
    {
        _previewVariantAnalysis = _settings == null ? null : ItemIconVariantEnumerator.Enumerate(_previewItem, _settings);
        int count = _previewVariantAnalysis?.Variants.Count ?? 0;
        _previewVariantLabels = new string[count];

        for (int i = 0; i < count; i++)
        {
            _previewVariantLabels[i] = _previewVariantAnalysis.Variants[i].DisplayName;
        }

        _selectedVariantIndex = Mathf.Clamp(_selectedVariantIndex, 0, Mathf.Max(0, count - 1));
    }

    private static bool RequiresLargeBakeConfirmation(ItemIconVariantAnalysis analysis)
    {
        return analysis.Variants.Count >= LARGE_BAKE_CONFIRMATION_THRESHOLD || analysis.ExcessiveVariantItems.Count > 0;
    }

    private static int BuildAllBakeItemTypeMask()
    {
        int mask = 0;

        for (int i = 0; i < BAKE_ITEM_TYPES.Length; i++)
        {
            mask |= 1 << i;
        }

        return mask;
    }

    private static int GetBakeItemTypeMask(ItemType itemType)
    {
        for (int i = 0; i < BAKE_ITEM_TYPES.Length; i++)
        {
            if (BAKE_ITEM_TYPES[i] == itemType)
            {
                return 1 << i;
            }
        }

        return 0;
    }

    private bool HasSelectedBakeItemTypes() => _selectedBakeItemTypeMask != 0;

    private bool IsBakeItemTypeSelected(ItemType itemType)
    {
        int normalizedMask = NormalizeBakeItemTypeMask(_selectedBakeItemTypeMask);

        for (int i = 0; i < BAKE_ITEM_TYPES.Length; i++)
        {
            if (BAKE_ITEM_TYPES[i] == itemType)
            {
                return (normalizedMask & (1 << i)) != 0;
            }
        }

        return false;
    }

    private string GetSelectedBakeItemTypeLabel()
    {
        int normalizedMask = NormalizeBakeItemTypeMask(_selectedBakeItemTypeMask);

        if (normalizedMask == ALL_BAKE_ITEM_TYPE_MASK)
        {
            return "all item types";
        }

        List<string> selectedLabels = new();

        for (int i = 0; i < BAKE_ITEM_TYPES.Length; i++)
        {
            if ((normalizedMask & (1 << i)) != 0)
            {
                selectedLabels.Add(BAKE_ITEM_TYPE_LABELS[i]);
            }
        }

        if (selectedLabels.Count <= 3)
        {
            return string.Join(", ", selectedLabels);
        }

        return $"{selectedLabels.Count} selected item types";
    }

    private static int NormalizeBakeItemTypeMask(int mask) => mask == -1 ? ALL_BAKE_ITEM_TYPE_MASK : mask & ALL_BAKE_ITEM_TYPE_MASK;

    private void CancelBake()
    {
        _bakeCancellation?.Cancel();
    }

    private void DrawPreviewPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect previewRect = GUILayoutUtility.GetRect(256f, 256f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(previewRect, new Color(0.14f, 0.14f, 0.14f, 1f));

            if (_previewTexture == null)
            {
                string message = GetPreviewPlaceholderText();
                GUI.Label(previewRect, message, CenteredLabelStyle);
            }
            else
            {
                Rect imageRect = GetCenteredImageRect(previewRect, _previewTexture);
                EditorGUI.DrawTextureTransparent(imageRect, _previewTexture, ScaleMode.ScaleToFit);

                EditorGUILayout.LabelField($"{_previewTexture.width} x {_previewTexture.height}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawVariantCountResizeHandle()
    {
        Rect handleRect = GUILayoutUtility.GetRect(1f, VARIANT_COUNT_RESIZE_HANDLE_HEIGHT, GUILayout.ExpandWidth(true));
        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeVertical);
        EditorGUI.DrawRect(new Rect(handleRect.x, handleRect.center.y, handleRect.width, 1f), new Color(0.32f, 0.32f, 0.32f, 1f));

        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && handleRect.Contains(currentEvent.mousePosition))
        {
            _isResizingVariantCount = true;
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseDrag && _isResizingVariantCount)
        {
            _variantCountHeight = Mathf.Clamp(_variantCountHeight + currentEvent.delta.y, MIN_VARIANT_COUNT_HEIGHT, MAX_VARIANT_COUNT_HEIGHT);
            currentEvent.Use();
            Repaint();
        }
        else if (currentEvent.type == EventType.MouseUp && _isResizingVariantCount)
        {
            _isResizingVariantCount = false;
            currentEvent.Use();
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
