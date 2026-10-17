using System.Collections.Generic;
using XRay.Importer.Core.Omf;

namespace XRay.Importer.Core.Ogf
{
    /// <summary>
    /// One OGF visual. All spatial data is kept in native X-Ray model space
    /// (left-handed, Y up, Z forward), which is identical to Unity's coordinate system.
    /// </summary>
    public sealed class OgfVisual
    {
        public byte FormatVersion { get; internal set; }

        public byte ModelType { get; internal set; }

        public OgfModelKind Kind { get; internal set; }

        public ushort ShaderId { get; internal set; }

        public string Name { get; internal set; }

        public string Texture { get; internal set; }

        public string Shader { get; internal set; }

        public uint VertexFormat { get; internal set; }

        public XVector3[] Positions { get; internal set; }

        public XVector3[] Normals { get; internal set; }

        /// <summary>Texture coordinates as stored (D3D convention, V points down).</summary>
        public double[] U { get; internal set; }

        public double[] V { get; internal set; }

        /// <summary>Per vertex influences, null for static visuals.</summary>
        public OgfVertexWeights[] Weights { get; internal set; }

        /// <summary>Triangle list in file order (clockwise front faces, same as Unity).</summary>
        public int[] Indices { get; internal set; }

        public List<OgfVisual> Children { get; } = new List<OgfVisual>();

        public bool IsSkinned => Weights != null;

        public int VertexCount => Positions == null ? 0 : Positions.Length;

        public int TriangleCount => Indices == null ? 0 : Indices.Length / 3;
    }

    public struct OgfVertexWeights
    {
        public int Count;
        public int Bone0;
        public int Bone1;
        public int Bone2;
        public int Bone3;
        public double Weight0;
        public double Weight1;
        public double Weight2;
        public double Weight3;

        public int GetBone(int index)
        {
            switch (index)
            {
                case 0: return Bone0;
                case 1: return Bone1;
                case 2: return Bone2;
                default: return Bone3;
            }
        }

        public double GetWeight(int index)
        {
            switch (index)
            {
                case 0: return Weight0;
                case 1: return Weight1;
                case 2: return Weight2;
                default: return Weight3;
            }
        }
    }

    public sealed class OgfBone
    {
        public int Index { get; internal set; }

        public string Name { get; internal set; }

        public string ParentName { get; internal set; }

        /// <summary>-1 for root bones.</summary>
        public int ParentIndex { get; internal set; } = -1;

        /// <summary>IK data: rest rotation of the bone relative to its parent, radians, raw file order.</summary>
        public XVector3 BindRotation { get; internal set; }

        /// <summary>IK data: rest offset of the bone relative to its parent, raw file order.</summary>
        public XVector3 BindTranslation { get; internal set; }

        public uint IkVersion { get; internal set; }

        public string GameMaterial { get; internal set; }

        public ushort ShapeType { get; internal set; }

        public ushort ShapeFlags { get; internal set; }

        public uint JointType { get; internal set; }

        public uint IkFlags { get; internal set; }

        public double Mass { get; internal set; }

        public XVector3 MassCenter { get; internal set; }

        public double Friction { get; internal set; }
    }

    public sealed class OgfDescription
    {
        public string SourceFile { get; internal set; }

        public string BuildName { get; internal set; }

        public uint BuildTime { get; internal set; }

        public string CreatorName { get; internal set; }

        public uint CreateTime { get; internal set; }

        public string ModifierName { get; internal set; }

        public uint ModifyTime { get; internal set; }
    }

    /// <summary>Root of a parsed *.ogf file.</summary>
    public sealed class OgfModel
    {
        public OgfVisual Root { get; internal set; }

        public List<OgfBone> Bones { get; } = new List<OgfBone>();

        public List<string> MotionRefs { get; } = new List<string>();

        public string UserData { get; internal set; }

        public string LodReference { get; internal set; }

        public OgfDescription Description { get; internal set; }

        /// <summary>Motions stored inside the OGF itself (S_MOTIONS + S_SMPARAMS), null if absent.</summary>
        public OmfData EmbeddedMotions { get; internal set; }

        public List<string> Warnings { get; } = new List<string>();

        public bool HasSkeleton => Bones.Count > 0;

        public int FindBone(string name)
        {
            for (int i = 0; i < Bones.Count; i++)
            {
                if (Bones[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Visuals that carry geometry, depth-first in file order (matches blender-xray object numbering).</summary>
        public List<OgfVisual> GetMeshVisuals()
        {
            var ordered = new List<OgfVisual>();
            CollectMeshes(Root, ordered);
            return ordered;
        }

        private static void CollectMeshes(OgfVisual visual, List<OgfVisual> output)
        {
            if (visual.Positions != null && visual.Indices != null)
            {
                output.Add(visual);
            }

            foreach (OgfVisual child in visual.Children)
            {
                CollectMeshes(child, output);
            }
        }
    }
}
