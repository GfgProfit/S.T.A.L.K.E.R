using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRay.Importer
{
    /// <summary>
    /// Main asset of an imported *.omf (and sub-asset of an *.ogf with embedded motions):
    /// the generated AnimationClips together with the X-Ray motion parameters.
    /// </summary>
    public sealed class XRayMotionLibrary : ScriptableObject
    {
        [SerializeField] private string _sourceFile;
        [SerializeField] private string _rigName;
        [SerializeField] private int _paramsVersion;
        [SerializeField] private float _frameRate = 30.0f;
        [SerializeField] private List<string> _boneNames = new List<string>();
        [SerializeField] private List<XRayBonePart> _boneParts = new List<XRayBonePart>();
        [SerializeField] private List<XRayMotionEntry> _motions = new List<XRayMotionEntry>();

        public string SourceFile => _sourceFile;

        public string RigName => _rigName;

        public int ParamsVersion => _paramsVersion;

        public float FrameRate => _frameRate;

        public IReadOnlyList<string> BoneNames => _boneNames;

        public IReadOnlyList<XRayBonePart> BoneParts => _boneParts;

        public IReadOnlyList<XRayMotionEntry> Motions => _motions;

        public XRayMotionEntry Find(string motionName)
        {
            for (int i = 0; i < _motions.Count; i++)
            {
                if (_motions[i].Name == motionName)
                {
                    return _motions[i];
                }
            }

            return null;
        }

        public bool TryGetClip(string motionName, out AnimationClip clip)
        {
            XRayMotionEntry entry = Find(motionName);
            clip = entry != null ? entry.Clip : null;
            return clip != null;
        }

        public void SetImportData(string sourceFile, string rigName, int paramsVersion, float frameRate, List<string> boneNames, List<XRayBonePart> boneParts, List<XRayMotionEntry> motions)
        {
            _sourceFile = sourceFile;
            _rigName = rigName;
            _paramsVersion = paramsVersion;
            _frameRate = frameRate;
            _boneNames = boneNames;
            _boneParts = boneParts;
            _motions = motions;
        }
    }

    [Serializable]
    public sealed class XRayBonePart
    {
        [SerializeField] private string _name;
        [SerializeField] private List<string> _bones = new List<string>();

        public XRayBonePart(string name, List<string> bones)
        {
            _name = name;
            _bones = bones;
        }

        public string Name => _name;

        public IReadOnlyList<string> Bones => _bones;
    }

    [Serializable]
    public sealed class XRayMotionMark
    {
        [SerializeField] private string _name;
        [SerializeField] private List<Vector2> _intervals = new List<Vector2>();

        public XRayMotionMark(string name, List<Vector2> intervals)
        {
            _name = name;
            _intervals = intervals;
        }

        public string Name => _name;

        /// <summary>Start and end time of each interval in seconds.</summary>
        public IReadOnlyList<Vector2> Intervals => _intervals;
    }

    [Serializable]
    public sealed class XRayMotionEntry
    {
        [SerializeField] private string _name;
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private int _frameCount;
        [SerializeField] private uint _flags;
        [SerializeField] private int _bonePart;
        [SerializeField] private float _speed;
        [SerializeField] private float _power;
        [SerializeField] private float _accrue;
        [SerializeField] private float _falloff;
        [SerializeField] private List<XRayMotionMark> _marks = new List<XRayMotionMark>();

        public XRayMotionEntry(string name, AnimationClip clip, int frameCount, uint flags, int bonePart, float speed, float power, float accrue, float falloff, List<XRayMotionMark> marks)
        {
            _name = name;
            _clip = clip;
            _frameCount = frameCount;
            _flags = flags;
            _bonePart = bonePart;
            _speed = speed;
            _power = power;
            _accrue = accrue;
            _falloff = falloff;
            _marks = marks;
        }

        public string Name => _name;

        public AnimationClip Clip => _clip;

        public int FrameCount => _frameCount;

        /// <summary>Raw X-Ray motion flags (FX = 1, STOP_AT_END = 2, NO_MIX = 4, SYNC_PART = 8, ...).</summary>
        public uint Flags => _flags;

        /// <summary>Bone part index, or 0xFFFF for all parts.</summary>
        public int BonePart => _bonePart;

        public float Speed => _speed;

        public float Power => _power;

        public float Accrue => _accrue;

        public float Falloff => _falloff;

        public IReadOnlyList<XRayMotionMark> Marks => _marks;

        public bool StopAtEnd => (_flags & 2u) != 0;

        public bool IsFx => (_flags & 1u) != 0;
    }
}
