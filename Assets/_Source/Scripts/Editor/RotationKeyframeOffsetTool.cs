using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class RotationKeyframeOffsetTool : EditorWindow
{
    private const string WINDOW_TITLE = "Rotation Keyframe Offset";

    [SerializeField] private AnimationClip _clip;
    [SerializeField] private Vector3 _eulerOffset = new Vector3(90f, -180f, 0f);
    [SerializeField] private bool _createCopy = true;

    [MenuItem("Tools/Animation/Rotation Keyframe Offset")]
    private static void Open()
    {
        GetWindow<RotationKeyframeOffsetTool>(WINDOW_TITLE);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Clip Rotation Offset", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        _clip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip",
            _clip,
            typeof(AnimationClip),
            false
        );

        _eulerOffset = EditorGUILayout.Vector3Field("Euler Offset", _eulerOffset);

        _createCopy = EditorGUILayout.ToggleLeft(
            "Create copy instead of modifying original",
            _createCopy
        );

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(_clip == null))
        {
            if (GUILayout.Button("Apply Rotation Offset"))
            {
                Apply();
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Default offset: X +90, Y -180, Z +0.\n" +
            "For imported FBX clips, the tool will create a standalone .anim copy.",
            MessageType.Info
        );
    }

    private void Apply()
    {
        AnimationClip targetClip = GetWritableClip(_clip, _createCopy);

        Undo.RegisterCompleteObjectUndo(targetClip, "Apply Rotation Keyframe Offset");

        int changedKeys = ApplyRotationOffset(targetClip, _eulerOffset);

        targetClip.EnsureQuaternionContinuity();

        EditorUtility.SetDirty(targetClip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = targetClip;
        EditorGUIUtility.PingObject(targetClip);

        if (changedKeys <= 0)
        {
            Debug.LogWarning($"[{WINDOW_TITLE}] Rotation curves were not found in clip: {targetClip.name}");
            return;
        }

        Debug.Log($"[{WINDOW_TITLE}] Done. Changed {changedKeys} rotation keys in clip: {targetClip.name}");
    }

    private static AnimationClip GetWritableClip(AnimationClip sourceClip, bool createCopy)
    {
        string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
        bool isSubAsset = AssetDatabase.IsSubAsset(sourceClip);

        if (!createCopy && !isSubAsset)
        {
            return sourceClip;
        }

        if (!createCopy && isSubAsset)
        {
            Debug.LogWarning(
                $"[{WINDOW_TITLE}] Selected clip is an imported sub-asset. " +
                "A standalone .anim copy will be created instead."
            );
        }

        string folder = "Assets";

        if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
        {
            string sourceFolder = Path.GetDirectoryName(sourcePath);
            if (!string.IsNullOrEmpty(sourceFolder))
            {
                folder = sourceFolder.Replace("\\", "/");
            }
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{folder}/{sourceClip.name}_rot_offset.anim"
        );

        AnimationClip copy = Instantiate(sourceClip);
        copy.name = Path.GetFileNameWithoutExtension(assetPath);

        AssetDatabase.CreateAsset(copy, assetPath);

        AnimationUtility.SetAnimationClipSettings(
            copy,
            AnimationUtility.GetAnimationClipSettings(sourceClip)
        );

        AnimationUtility.SetAnimationEvents(
            copy,
            AnimationUtility.GetAnimationEvents(sourceClip)
        );

        AssetDatabase.SaveAssets();

        return copy;
    }

    private static int ApplyRotationOffset(AnimationClip clip, Vector3 eulerOffset)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        Dictionary<string, List<EditorCurveBinding>> groups = new Dictionary<string, List<EditorCurveBinding>>();

        foreach (EditorCurveBinding binding in bindings)
        {
            if (!IsRotationProperty(binding.propertyName))
            {
                continue;
            }

            string key = $"{binding.path}|{binding.type.FullName}";

            if (!groups.TryGetValue(key, out List<EditorCurveBinding> group))
            {
                group = new List<EditorCurveBinding>();
                groups.Add(key, group);
            }

            group.Add(binding);
        }

        int changedKeys = 0;

        foreach (List<EditorCurveBinding> group in groups.Values)
        {
            changedKeys += ApplyEulerRotationOffset(clip, group, eulerOffset);
            changedKeys += ApplyQuaternionRotationOffset(clip, group, eulerOffset);
        }

        return changedKeys;
    }

    private static int ApplyEulerRotationOffset(
        AnimationClip clip,
        List<EditorCurveBinding> group,
        Vector3 eulerOffset
    )
    {
        int changedKeys = 0;

        foreach (EditorCurveBinding binding in group)
        {
            if (!IsEulerRotationProperty(binding.propertyName))
            {
                continue;
            }

            if (!TryGetEulerAxisOffset(binding.propertyName, eulerOffset, out float axisOffset))
            {
                continue;
            }

            if (Mathf.Approximately(axisOffset, 0f))
            {
                continue;
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
            {
                continue;
            }

            Keyframe[] keys = curve.keys;

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].value += axisOffset;
                changedKeys++;
            }

            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        return changedKeys;
    }

    private static int ApplyQuaternionRotationOffset(
        AnimationClip clip,
        List<EditorCurveBinding> group,
        Vector3 eulerOffset
    )
    {
        if (!TryFindBinding(group, "m_LocalRotation.x", out EditorCurveBinding bindingX) ||
            !TryFindBinding(group, "m_LocalRotation.y", out EditorCurveBinding bindingY) ||
            !TryFindBinding(group, "m_LocalRotation.z", out EditorCurveBinding bindingZ) ||
            !TryFindBinding(group, "m_LocalRotation.w", out EditorCurveBinding bindingW))
        {
            return 0;
        }

        AnimationCurve sourceX = AnimationUtility.GetEditorCurve(clip, bindingX);
        AnimationCurve sourceY = AnimationUtility.GetEditorCurve(clip, bindingY);
        AnimationCurve sourceZ = AnimationUtility.GetEditorCurve(clip, bindingZ);
        AnimationCurve sourceW = AnimationUtility.GetEditorCurve(clip, bindingW);

        if (sourceX == null || sourceY == null || sourceZ == null || sourceW == null)
        {
            return 0;
        }

        SortedSet<float> times = new SortedSet<float>();

        AddKeyTimes(sourceX, times);
        AddKeyTimes(sourceY, times);
        AddKeyTimes(sourceZ, times);
        AddKeyTimes(sourceW, times);

        if (times.Count == 0)
        {
            return 0;
        }

        AnimationCurve resultX = new AnimationCurve();
        AnimationCurve resultY = new AnimationCurve();
        AnimationCurve resultZ = new AnimationCurve();
        AnimationCurve resultW = new AnimationCurve();

        foreach (float time in times)
        {
            Quaternion sourceRotation = new Quaternion(
                sourceX.Evaluate(time),
                sourceY.Evaluate(time),
                sourceZ.Evaluate(time),
                sourceW.Evaluate(time)
            );

            sourceRotation = NormalizeQuaternion(sourceRotation);

            Vector3 euler = sourceRotation.eulerAngles;
            euler.x += eulerOffset.x;
            euler.y += eulerOffset.y;
            euler.z += eulerOffset.z;

            Quaternion resultRotation = Quaternion.Euler(euler);

            resultX.AddKey(new Keyframe(time, resultRotation.x));
            resultY.AddKey(new Keyframe(time, resultRotation.y));
            resultZ.AddKey(new Keyframe(time, resultRotation.z));
            resultW.AddKey(new Keyframe(time, resultRotation.w));
        }

        SmoothCurve(resultX);
        SmoothCurve(resultY);
        SmoothCurve(resultZ);
        SmoothCurve(resultW);

        AnimationUtility.SetEditorCurve(clip, bindingX, resultX);
        AnimationUtility.SetEditorCurve(clip, bindingY, resultY);
        AnimationUtility.SetEditorCurve(clip, bindingZ, resultZ);
        AnimationUtility.SetEditorCurve(clip, bindingW, resultW);

        return times.Count * 4;
    }

    private static bool IsRotationProperty(string propertyName)
    {
        return IsEulerRotationProperty(propertyName) || IsQuaternionRotationProperty(propertyName);
    }

    private static bool IsEulerRotationProperty(string propertyName)
    {
        return propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal) &&
               (propertyName.EndsWith(".x", StringComparison.Ordinal) ||
                propertyName.EndsWith(".y", StringComparison.Ordinal) ||
                propertyName.EndsWith(".z", StringComparison.Ordinal));
    }

    private static bool IsQuaternionRotationProperty(string propertyName)
    {
        return propertyName == "m_LocalRotation.x" ||
               propertyName == "m_LocalRotation.y" ||
               propertyName == "m_LocalRotation.z" ||
               propertyName == "m_LocalRotation.w";
    }

    private static bool TryGetEulerAxisOffset(
        string propertyName,
        Vector3 eulerOffset,
        out float axisOffset
    )
    {
        if (propertyName.EndsWith(".x", StringComparison.Ordinal))
        {
            axisOffset = eulerOffset.x;
            return true;
        }

        if (propertyName.EndsWith(".y", StringComparison.Ordinal))
        {
            axisOffset = eulerOffset.y;
            return true;
        }

        if (propertyName.EndsWith(".z", StringComparison.Ordinal))
        {
            axisOffset = eulerOffset.z;
            return true;
        }

        axisOffset = 0f;
        return false;
    }

    private static bool TryFindBinding(
        List<EditorCurveBinding> bindings,
        string propertyName,
        out EditorCurveBinding result
    )
    {
        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.propertyName == propertyName)
            {
                result = binding;
                return true;
            }
        }

        result = default;
        return false;
    }

    private static void AddKeyTimes(AnimationCurve curve, SortedSet<float> times)
    {
        Keyframe[] keys = curve.keys;

        for (int i = 0; i < keys.Length; i++)
        {
            times.Add(keys[i].time);
        }
    }

    private static Quaternion NormalizeQuaternion(Quaternion quaternion)
    {
        float magnitude = Mathf.Sqrt(
            quaternion.x * quaternion.x +
            quaternion.y * quaternion.y +
            quaternion.z * quaternion.z +
            quaternion.w * quaternion.w
        );

        if (magnitude <= Mathf.Epsilon)
        {
            return Quaternion.identity;
        }

        return new Quaternion(
            quaternion.x / magnitude,
            quaternion.y / magnitude,
            quaternion.z / magnitude,
            quaternion.w / magnitude
        );
    }

    private static void SmoothCurve(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }
    }
}