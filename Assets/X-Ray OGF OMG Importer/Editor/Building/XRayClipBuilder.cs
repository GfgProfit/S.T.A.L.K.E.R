using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XRay.Importer.Core;
using XRay.Importer.Core.Omf;

namespace XRay.Importer.Editor
{
    public enum XRayLoopMode
    {
        /// <summary>Loop unless the motion has the STOP_AT_END flag.</summary>
        FromMotionFlags,
        Always,
        Never
    }

    /// <summary>
    /// Converts OMF motions into generic AnimationClips. Every key is written verbatim at
    /// t = key / 30 s as Transform.localPosition / localRotation of the bone (see OmfReader for the
    /// derivation); no rest pose, retargeting or correction is involved. Tangents are linear, so
    /// positions interpolate exactly like the engine and rotations differ from the engine's slerp
    /// only by Unity's normalized-lerp between neighbouring 1/30 s keys.
    /// </summary>
    public static class XRayClipBuilder
    {
        private static readonly string[] POSITION_PROPERTIES = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };
        private static readonly string[] ROTATION_PROPERTIES = { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };

        public static AnimationClip Build(OmfMotion motion, XRayRigPaths rig, float scale, XRayLoopMode loopMode, List<string> missingBones)
        {
            var bindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();
            int frames = motion.FrameCount;
            float frameTime = (float)(1.0 / OmfFormat.SAMPLE_FPS);

            foreach (OmfBoneTrack track in motion.Tracks)
            {
                if (!rig.TryGetPath(track.BoneName, out string path))
                {
                    missingBones?.Add(track.BoneName);
                    continue;
                }

                int positionKeys = track.HasTranslationKeys ? frames : 1;
                var positions = new float[3][];
                for (int axis = 0; axis < 3; axis++)
                {
                    positions[axis] = new float[positionKeys];
                }

                for (int key = 0; key < positionKeys; key++)
                {
                    Vector3 value = XRayUnitySpace.Position(track.Translations[key], scale);
                    positions[0][key] = value.x;
                    positions[1][key] = value.y;
                    positions[2][key] = value.z;
                }

                for (int axis = 0; axis < 3; axis++)
                {
                    bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), POSITION_PROPERTIES[axis]));
                    curves.Add(CreateLinearCurve(positions[axis], frames, frameTime));
                }

                int rotationKeys = track.HasRotationKeys ? frames : 1;
                var rotations = new float[4][];
                for (int axis = 0; axis < 4; axis++)
                {
                    rotations[axis] = new float[rotationKeys];
                }

                XQuaternion previous = XQuaternion.Identity;
                for (int key = 0; key < rotationKeys; key++)
                {
                    XQuaternion q = track.Rotations[key];

                    // q and -q are the same rotation; keep consecutive keys in one hemisphere so the
                    // component-wise curve interpolation takes the short arc.
                    if (key > 0 && XQuaternion.Dot(q, previous) < 0.0)
                    {
                        q = q.Negated();
                    }

                    previous = q;
                    Quaternion value = XRayUnitySpace.Rotation(q);
                    rotations[0][key] = value.x;
                    rotations[1][key] = value.y;
                    rotations[2][key] = value.z;
                    rotations[3][key] = value.w;
                }

                for (int axis = 0; axis < 4; axis++)
                {
                    bindings.Add(EditorCurveBinding.FloatCurve(path, typeof(Transform), ROTATION_PROPERTIES[axis]));
                    curves.Add(CreateLinearCurve(rotations[axis], frames, frameTime));
                }
            }

            var clip = new AnimationClip { name = motion.Name, frameRate = (float)OmfFormat.SAMPLE_FPS, legacy = false };
            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = IsLooping(motion, loopMode);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        public static bool IsLooping(OmfMotion motion, XRayLoopMode loopMode)
        {
            switch (loopMode)
            {
                case XRayLoopMode.Always:
                    return true;
                case XRayLoopMode.Never:
                    return false;
                default:
                    return motion.Params == null || !motion.Params.StopAtEnd;
            }
        }

        /// <summary>
        /// One key per frame with linear tangents. A constant channel gets keys at the first and last
        /// frame so every clip defines the full pose of each animated bone for its whole length.
        /// </summary>
        private static AnimationCurve CreateLinearCurve(float[] values, int frames, float frameTime)
        {
            Keyframe[] keys;
            if (values.Length == 1)
            {
                if (frames <= 1)
                {
                    keys = new[] { new Keyframe(0.0f, values[0], 0.0f, 0.0f) };
                }
                else
                {
                    keys = new[] { new Keyframe(0.0f, values[0], 0.0f, 0.0f), new Keyframe((frames - 1) * frameTime, values[0], 0.0f, 0.0f) };
                }

                return new AnimationCurve(keys);
            }

            keys = new Keyframe[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                float inSlope = i > 0 ? (values[i] - values[i - 1]) / frameTime : 0.0f;
                float outSlope = i < values.Length - 1 ? (values[i + 1] - values[i]) / frameTime : 0.0f;
                if (i == 0)
                {
                    inSlope = outSlope;
                }

                if (i == values.Length - 1)
                {
                    outSlope = inSlope;
                }

                keys[i] = new Keyframe(i * frameTime, values[i], inSlope, outSlope);
            }

            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }

            return curve;
        }
    }
}
