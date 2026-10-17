using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Omf;

namespace XRay.Importer.Core.Skeleton
{
    /// <summary>
    /// Reference evaluation of skeleton poses and linear blend skinning in native space.
    /// The Unity importer produces exactly these local transforms; tests use this class to compare the
    /// format math against Blender and the Unity runtime result against this math.
    /// </summary>
    public static class XRayPose
    {
        /// <summary>World (model space) bone matrices of a motion frame. Bones without a track keep their rest local transform.</summary>
        public static XMatrix4[] EvaluateWorld(XRaySkeleton skeleton, OmfMotion motion, int frame)
        {
            var local = new XMatrix4[skeleton.Count];
            for (int i = 0; i < skeleton.Count; i++)
            {
                local[i] = skeleton.RestLocal[i];
            }

            if (motion != null)
            {
                foreach (OmfBoneTrack track in motion.Tracks)
                {
                    int bone = skeleton.IndexOf(track.BoneName);
                    if (bone >= 0)
                    {
                        local[bone] = XMatrix4.TranslationRotation(track.GetTranslation(frame), track.GetRotation(frame));
                    }
                }
            }

            return ComposeWorld(skeleton, local);
        }

        public static XMatrix4[] ComposeWorld(XRaySkeleton skeleton, XMatrix4[] local)
        {
            var world = new XMatrix4[skeleton.Count];
            var done = new bool[skeleton.Count];
            for (int i = 0; i < skeleton.Count; i++)
            {
                Compose(i, skeleton, local, world, done);
            }

            return world;
        }

        /// <summary>Linear blend skinning: p' = sum_i w_i * World_i * RestWorld_i^-1 * p.</summary>
        public static XVector3[] Skin(OgfVisual visual, XRaySkeleton skeleton, XMatrix4[] world)
        {
            var skin = new XMatrix4[skeleton.Count];
            for (int i = 0; i < skeleton.Count; i++)
            {
                skin[i] = world[i] * skeleton.RestWorld[i].InverseAffine();
            }

            var result = new XVector3[visual.VertexCount];
            for (int v = 0; v < visual.VertexCount; v++)
            {
                if (!visual.IsSkinned)
                {
                    result[v] = visual.Positions[v];
                    continue;
                }

                OgfVertexWeights weights = visual.Weights[v];
                XVector3 sum = XVector3.Zero;
                for (int k = 0; k < weights.Count; k++)
                {
                    sum = sum + skin[weights.GetBone(k)].TransformPoint(visual.Positions[v]) * weights.GetWeight(k);
                }

                result[v] = sum;
            }

            return result;
        }

        private static XMatrix4 Compose(int index, XRaySkeleton skeleton, XMatrix4[] local, XMatrix4[] world, bool[] done)
        {
            if (done[index])
            {
                return world[index];
            }

            int parent = skeleton.Parents[index];
            world[index] = parent >= 0 ? Compose(parent, skeleton, local, world, done) * local[index] : local[index];
            done[index] = true;
            return world[index];
        }
    }

    /// <summary>
    /// Conversions between native X-Ray/Unity space and the spaces used by blender-xray, for verification only.
    /// </summary>
    public static class BlenderSpace
    {
        // S: swap Y and Z (blender-xray PackedReader.getv3f / getv3fp).
        private static readonly XMatrix4 SWAP_YZ = XMatrix4.FromRows(1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1);

        // diag(1, 1, -1); MATRIX_BONE = SWAP_YZ * FLIP_Z.
        private static readonly XMatrix4 FLIP_Z = XMatrix4.FromRows(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);

        public static readonly XMatrix4 MATRIX_BONE = XMatrix4.FromRows(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1);

        /// <summary>Native bone world matrix to Blender armature-space bone matrix (rest or pose).</summary>
        public static XMatrix4 BoneToBlender(XMatrix4 nativeWorld)
        {
            return SWAP_YZ * nativeWorld * FLIP_Z;
        }

        public static XMatrix4 BoneFromBlender(XMatrix4 blenderMatrix)
        {
            return SWAP_YZ * blenderMatrix * FLIP_Z;
        }

        public static XVector3 PointToBlender(XVector3 native)
        {
            return new XVector3(native.X, native.Z, native.Y);
        }
    }
}
