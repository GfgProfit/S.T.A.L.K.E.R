namespace XRay.Importer.Core.Ogf
{
    /// <summary>OGF chunk ids, model types and vertex formats (see blender-xray formats/ogf/fmt.py).</summary>
    public static class OgfFormat
    {
        public const uint CHUNK_HEADER = 0x1;

        public const byte VERSION_3 = 3;
        public const byte VERSION_4 = 4;

        public const uint FVF_1L = 0x12071980;
        public const uint FVF_2L = 2 * 0x12071980;
        public const uint FVF_1L_CS = 1;
        public const uint FVF_2L_CS = 2;
        public const uint FVF_3L_CS = 3;
        public const uint FVF_4L_CS = 4;

        // D3DFVF_XYZ | D3DFVF_NORMAL | D3DFVF_TEX1
        public const uint FVF_OGF_STATIC = 0x112;

        public const uint BONE_VERSION_0 = 0;
    }

    /// <summary>Chunk ids that differ between OGF v3 and v4. Unsupported ids are 0.</summary>
    public sealed class OgfChunkIds
    {
        public static readonly OgfChunkIds V4 = new OgfChunkIds
        {
            Texture = 2,
            Vertices = 3,
            Indices = 4,
            SwiData = 6,
            VContainer = 7,
            IContainer = 8,
            Children = 9,
            ChildrenL = 10,
            LodDef2 = 11,
            TreeDef2 = 12,
            BoneNames = 13,
            Motions2 = 14,
            SmParams1 = 15,
            IkData2 = 16,
            UserData = 17,
            Desc = 18,
            MotionRefs0 = 19,
            SwiContainer = 20,
            GContainer = 21,
            FastPath = 22,
            Lods = 23,
            MotionRefs2 = 24
        };

        public static readonly OgfChunkIds V3 = new OgfChunkIds
        {
            Texture = 2,
            TextureL = 3,
            ChildRefs = 5,
            BBox = 6,
            Vertices = 7,
            Indices = 8,
            SwiData = 9,
            VContainer = 10,
            BSphere = 11,
            ChildrenL = 12,
            BoneNames = 13,
            Motions0 = 14,
            Lods = 16,
            Children = 17,
            SmParams0 = 18,
            IContainer = 19,
            SmParams1 = 20,
            LodDef2 = 21,
            TreeDef2 = 22,
            IkData0 = 23,
            UserData = 24,
            IkData1 = 25,
            Motions1 = 26,
            Desc = 27,
            IkData2 = 28,
            MotionRefs0 = 29
        };

        public uint Texture { get; private set; }
        public uint TextureL { get; private set; }
        public uint ChildRefs { get; private set; }
        public uint BBox { get; private set; }
        public uint BSphere { get; private set; }
        public uint Vertices { get; private set; }
        public uint Indices { get; private set; }
        public uint SwiData { get; private set; }
        public uint VContainer { get; private set; }
        public uint IContainer { get; private set; }
        public uint Children { get; private set; }
        public uint ChildrenL { get; private set; }
        public uint LodDef2 { get; private set; }
        public uint TreeDef2 { get; private set; }
        public uint BoneNames { get; private set; }
        public uint Motions0 { get; private set; }
        public uint Motions1 { get; private set; }
        public uint Motions2 { get; private set; }
        public uint SmParams0 { get; private set; }
        public uint SmParams1 { get; private set; }
        public uint IkData0 { get; private set; }
        public uint IkData1 { get; private set; }
        public uint IkData2 { get; private set; }
        public uint UserData { get; private set; }
        public uint Desc { get; private set; }
        public uint MotionRefs0 { get; private set; }
        public uint MotionRefs2 { get; private set; }
        public uint SwiContainer { get; private set; }
        public uint GContainer { get; private set; }
        public uint FastPath { get; private set; }
        public uint Lods { get; private set; }
    }

    public enum OgfModelKind
    {
        Unknown,
        Normal,
        Hierarchy,
        Progressive,
        SkeletonAnim,
        SkeletonGeomDefPm,
        SkeletonGeomDefSt,
        SkeletonRigid,
        Lod,
        Tree,
        Other
    }

    public static class OgfModelTypes
    {
        public static OgfModelKind Resolve(byte version, byte modelType)
        {
            if (version == OgfFormat.VERSION_4)
            {
                switch (modelType)
                {
                    case 0x0: return OgfModelKind.Normal;
                    case 0x1: return OgfModelKind.Hierarchy;
                    case 0x2: return OgfModelKind.Progressive;
                    case 0x3: return OgfModelKind.SkeletonAnim;
                    case 0x4: return OgfModelKind.SkeletonGeomDefPm;
                    case 0x5: return OgfModelKind.SkeletonGeomDefSt;
                    case 0x6: return OgfModelKind.Lod;
                    case 0x7: return OgfModelKind.Tree;
                    case 0xa: return OgfModelKind.SkeletonRigid;
                    case 0xb: return OgfModelKind.Tree;
                    default: return OgfModelKind.Other;
                }
            }

            if (version == OgfFormat.VERSION_3)
            {
                switch (modelType)
                {
                    case 0x0: return OgfModelKind.Normal;
                    case 0x1: return OgfModelKind.Hierarchy;
                    case 0x2: return OgfModelKind.Progressive;
                    case 0x3: return OgfModelKind.SkeletonGeomDefPm;
                    case 0x4: return OgfModelKind.SkeletonAnim;
                    case 0x7: return OgfModelKind.SkeletonGeomDefSt;
                    case 0xb: return OgfModelKind.Lod;
                    case 0xc: return OgfModelKind.Tree;
                    case 0xf: return OgfModelKind.SkeletonRigid;
                    default: return OgfModelKind.Other;
                }
            }

            return OgfModelKind.Unknown;
        }
    }
}
