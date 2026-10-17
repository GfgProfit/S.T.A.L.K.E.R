using System.Collections.Generic;
using UnityEngine;

namespace XRay.Importer
{
    /// <summary>Metadata of an imported *.ogf, placed on the prefab root by the importer.</summary>
    [DisallowMultipleComponent]
    public sealed class XRayModel : MonoBehaviour
    {
        [SerializeField] private string _sourceFile;
        [SerializeField] private int _formatVersion;
        [SerializeField] private string _modelType;
        [SerializeField] private float _scale = 1.0f;
        [SerializeField] private Transform[] _bones = new Transform[0];
        [SerializeField] private string[] _boneNames = new string[0];
        [SerializeField] private string[] _motionReferences = new string[0];
        [SerializeField] private string _userData;
        [SerializeField] private string _lodReference;
        [SerializeField] private XRayMotionLibrary _embeddedMotions;
        [SerializeField] private int _meshCount;
        [SerializeField] private int _vertexCount;
        [SerializeField] private int _triangleCount;
        [SerializeField] private string[] _textures = new string[0];

        public string SourceFile => _sourceFile;

        public int FormatVersion => _formatVersion;

        public string ModelType => _modelType;

        public float Scale => _scale;

        /// <summary>Bone transforms in OGF bone index order.</summary>
        public IReadOnlyList<Transform> Bones => _bones;

        /// <summary>Original X-Ray bone names in OGF bone index order.</summary>
        public IReadOnlyList<string> BoneNames => _boneNames;

        public IReadOnlyList<string> MotionReferences => _motionReferences;

        public string UserData => _userData;

        public string LodReference => _lodReference;

        public XRayMotionLibrary EmbeddedMotions => _embeddedMotions;

        public int MeshCount => _meshCount;

        public int VertexCount => _vertexCount;

        public int TriangleCount => _triangleCount;

        public IReadOnlyList<string> Textures => _textures;

        public Transform FindBone(string boneName)
        {
            for (int i = 0; i < _boneNames.Length; i++)
            {
                if (_boneNames[i] == boneName)
                {
                    return _bones[i];
                }
            }

            return null;
        }

        public void SetImportData(string sourceFile, int formatVersion, string modelType, float scale, Transform[] bones, string[] boneNames, string[] motionReferences, string userData, string lodReference, XRayMotionLibrary embeddedMotions, int meshCount, int vertexCount, int triangleCount, string[] textures)
        {
            _sourceFile = sourceFile;
            _formatVersion = formatVersion;
            _modelType = modelType;
            _scale = scale;
            _bones = bones;
            _boneNames = boneNames;
            _motionReferences = motionReferences;
            _userData = userData;
            _lodReference = lodReference;
            _embeddedMotions = embeddedMotions;
            _meshCount = meshCount;
            _vertexCount = vertexCount;
            _triangleCount = triangleCount;
            _textures = textures;
        }
    }
}
