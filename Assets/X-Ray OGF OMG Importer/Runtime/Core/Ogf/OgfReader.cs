using System;
using System.Collections.Generic;
using XRay.Importer.Core.IO;
using XRay.Importer.Core.Omf;
using XRay.Importer.Core.Skeleton;

namespace XRay.Importer.Core.Ogf
{
    /// <summary>
    /// Parser for standalone *.ogf files (format versions 4 and 3). Chunk handling follows
    /// blender-xray formats/ogf/imp. Geometry stays in native X-Ray space (no axis swap):
    /// blender-xray swaps Y/Z on read and reverses triangle winding; both operations are its
    /// Blender-specific handedness conversion and are intentionally not applied here.
    /// </summary>
    public static class OgfReader
    {
        public static OgfModel ReadFile(byte[] data, string fileName)
        {
            var model = new OgfModel();
            var reader = new XRayReader(data, fileName);
            model.Root = ReadVisual(reader, fileName, true, model);

            if (model.Root.FormatVersion == OgfFormat.VERSION_3 && model.HasSkeleton)
            {
                ConvertV3BoneLocalVertices(model);
            }

            return model;
        }

        private static OgfVisual ReadVisual(XRayReader reader, string name, bool isRoot, OgfModel model)
        {
            Dictionary<uint, XRayChunk> chunks = XRayChunks.ReadMap(reader);
            var consumed = new HashSet<uint>();
            var visual = new OgfVisual { Name = name };

            ReadHeader(chunks, consumed, visual, reader.Context);
            OgfChunkIds ids = visual.FormatVersion == OgfFormat.VERSION_4 ? OgfChunkIds.V4 : OgfChunkIds.V3;

            if (isRoot)
            {
                ReadUserData(chunks, consumed, ids, model);
            }

            switch (visual.Kind)
            {
                case OgfModelKind.SkeletonAnim:
                case OgfModelKind.SkeletonRigid:
                    RequireRoot(isRoot, visual, reader.Context);
                    if (visual.Kind == OgfModelKind.SkeletonAnim)
                    {
                        ReadMotionRefs(chunks, consumed, ids, model);
                    }

                    ReadDescription(chunks, consumed, ids, model);
                    ReadLods(chunks, consumed, ids, model);
                    ReadBones(chunks, consumed, ids, model, reader.Context);
                    ReadChildren(chunks, consumed, ids, visual, model, reader.Context);
                    if (visual.Kind == OgfModelKind.SkeletonAnim)
                    {
                        ReadEmbeddedMotions(chunks, consumed, ids, model, reader.Context);
                    }

                    break;
                case OgfModelKind.Hierarchy:
                    RequireRoot(isRoot, visual, reader.Context);
                    ReadDescription(chunks, consumed, ids, model);
                    ReadChildren(chunks, consumed, ids, visual, model, reader.Context);
                    break;
                case OgfModelKind.Normal:
                case OgfModelKind.Progressive:
                    ReadTexture(chunks, consumed, ids, visual, reader.Context);
                    ReadStaticVertices(chunks, consumed, ids, visual, reader.Context);
                    ReadIndices(chunks, consumed, ids, visual, reader.Context);
                    if (visual.Kind == OgfModelKind.Progressive)
                    {
                        ReadSwi(chunks, consumed, ids, visual, reader.Context);
                    }

                    break;
                case OgfModelKind.SkeletonGeomDefSt:
                case OgfModelKind.SkeletonGeomDefPm:
                    if (isRoot)
                    {
                        throw new XRayFormatException($"{reader.Context}: skinned geometry visual (model type {visual.ModelType}) cannot be the root of a standalone OGF.");
                    }

                    ReadTexture(chunks, consumed, ids, visual, reader.Context);
                    ReadSkinnedVertices(chunks, consumed, ids, visual, model, reader.Context);
                    ReadIndices(chunks, consumed, ids, visual, reader.Context);
                    if (visual.Kind == OgfModelKind.SkeletonGeomDefPm)
                    {
                        ReadSwi(chunks, consumed, ids, visual, reader.Context);
                    }

                    break;
                default:
                    throw new XRayFormatException($"{reader.Context}: OGF v{visual.FormatVersion} model type 0x{visual.ModelType:X} ({visual.Kind}) is not supported for standalone import (supported: NORMAL, PROGRESSIVE, HIERRARHY, SKELETON_ANIM, SKELETON_RIGID, SKELETON_GEOMDEF_ST/PM).");
            }

            foreach (uint id in chunks.Keys)
            {
                if (!consumed.Contains(id))
                {
                    model.Warnings.Add($"{reader.Context}: chunk {id} ({chunks[id].Length} bytes) of {visual.Kind} visual was not used.");
                }
            }

            return visual;
        }

        private static void RequireRoot(bool isRoot, OgfVisual visual, string context)
        {
            if (!isRoot)
            {
                throw new XRayFormatException($"{context}: nested {visual.Kind} visuals are not supported.");
            }
        }

        private static bool TryTake(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, uint id, out XRayChunk chunk)
        {
            if (id != 0 && chunks.TryGetValue(id, out chunk))
            {
                consumed.Add(id);
                return true;
            }

            chunk = default;
            return false;
        }

        private static XRayChunk Take(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, uint id, string what, string context)
        {
            if (!TryTake(chunks, consumed, id, out XRayChunk chunk))
            {
                throw new XRayFormatException($"{context}: required chunk {what} (id {id}) is missing.");
            }

            return chunk;
        }

        private static void ReadHeader(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfVisual visual, string context)
        {
            XRayReader reader = Take(chunks, consumed, OgfFormat.CHUNK_HEADER, "HEADER", context).CreateReader(context + " HEADER");
            visual.FormatVersion = reader.ReadByte();
            if (visual.FormatVersion != OgfFormat.VERSION_4 && visual.FormatVersion != OgfFormat.VERSION_3)
            {
                throw new XRayFormatException($"{context}: OGF format version {visual.FormatVersion} is not supported (supported: 4 = SoC/CS/CoP, 3 = pre-release builds).");
            }

            visual.ModelType = reader.ReadByte();
            visual.ShaderId = reader.ReadUInt16();
            visual.Kind = OgfModelTypes.Resolve(visual.FormatVersion, visual.ModelType);

            // v4 header continues with bbox (6f) and bsphere (4f); v3 keeps them in separate chunks.
            // Bounds are recomputed by Unity, so they are only consumed here.
            if (visual.FormatVersion == OgfFormat.VERSION_3)
            {
                TryTake(chunks, consumed, OgfChunkIds.V3.BBox, out _);
                TryTake(chunks, consumed, OgfChunkIds.V3.BSphere, out _);
            }
        }

        private static void ReadUserData(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfModel model)
        {
            if (TryTake(chunks, consumed, ids.UserData, out XRayChunk chunk))
            {
                model.UserData = chunk.CreateReader("S_USERDATA").ReadStringZ();
            }
        }

        private static void ReadDescription(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfModel model)
        {
            if (!TryTake(chunks, consumed, ids.Desc, out XRayChunk chunk))
            {
                return;
            }

            XRayReader reader = chunk.CreateReader("S_DESC");
            var description = new OgfDescription { SourceFile = reader.ReadStringZ() };
            try
            {
                description.BuildName = reader.ReadStringZ();
                description.BuildTime = reader.ReadUInt32();
                description.CreatorName = reader.ReadStringZ();
                description.CreateTime = reader.ReadUInt32();
                description.ModifierName = reader.ReadStringZ();
                description.ModifyTime = reader.ReadUInt32();
            }
            catch (XRayFormatException exception)
            {
                model.Warnings.Add("Truncated S_DESC chunk: " + exception.Message);
            }

            model.Description = description;
        }

        private static void ReadLods(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfModel model)
        {
            if (TryTake(chunks, consumed, ids.Lods, out XRayChunk chunk))
            {
                string lod = chunk.CreateReader("S_LODS").ReadStringZ();
                model.LodReference = lod.TrimEnd('\r', '\n');
            }
        }

        private static void ReadMotionRefs(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfModel model)
        {
            if (TryTake(chunks, consumed, ids.MotionRefs0, out XRayChunk soc))
            {
                foreach (string reference in soc.CreateReader("S_MOTION_REFS_0").ReadStringZ().Split(','))
                {
                    if (reference.Length > 0)
                    {
                        model.MotionRefs.Add(reference);
                    }
                }
            }
            else if (TryTake(chunks, consumed, ids.MotionRefs2, out XRayChunk cop))
            {
                XRayReader reader = cop.CreateReader("S_MOTION_REFS_2");
                uint count = reader.ReadUInt32();
                for (uint i = 0; i < count; i++)
                {
                    model.MotionRefs.Add(reader.ReadStringZ());
                }
            }
        }

        private static void ReadBones(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfModel model, string context)
        {
            XRayReader names = Take(chunks, consumed, ids.BoneNames, "S_BONE_NAMES", context).CreateReader(context + " S_BONE_NAMES");
            uint count = names.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                var bone = new OgfBone { Index = i, Name = names.ReadStringZ(), ParentName = names.ReadStringZ() };

                // Oriented bounding box (rotation 3x3, translation, half size). Not part of the rest pose;
                // blender-xray reads and discards it as well.
                names.Skip(15 * 4);
                model.Bones.Add(bone);
            }

            bool hasVersion = true;
            if (!TryTake(chunks, consumed, ids.IkData2, out XRayChunk ikChunk))
            {
                hasVersion = false;
                if (!TryTake(chunks, consumed, ids.IkData1, out ikChunk) && !TryTake(chunks, consumed, ids.IkData0, out ikChunk))
                {
                    throw new XRayFormatException($"{context}: skeleton has {count} bones but no S_IKDATA chunk; the rest pose cannot be built.");
                }
            }

            XRayReader ik = ikChunk.CreateReader(context + " S_IKDATA");
            foreach (OgfBone bone in model.Bones)
            {
                bone.IkVersion = hasVersion ? ik.ReadUInt32() : OgfFormat.BONE_VERSION_0;
                bone.GameMaterial = ik.ReadStringZ();
                bone.ShapeType = ik.ReadUInt16();
                bone.ShapeFlags = ik.ReadUInt16();
                ik.Skip(15 * 4);    // box: rotation 3x3, translation, half size
                ik.Skip(4 * 4);     // sphere: center, radius
                ik.Skip(8 * 4);     // cylinder: center, direction, height, radius
                bone.JointType = ik.ReadUInt32();
                ik.Skip(3 * 4 * 4); // x/y/z limits: min, max, spring, damping
                ik.Skip(2 * 4);     // joint spring, damping
                bone.IkFlags = ik.ReadUInt32();
                ik.Skip(2 * 4);     // breakable force, torque
                if (bone.IkVersion > OgfFormat.BONE_VERSION_0)
                {
                    bone.Friction = ik.ReadSingle();
                }

                bone.BindRotation = ik.ReadVector3();
                bone.BindTranslation = ik.ReadVector3();
                bone.Mass = ik.ReadSingle();
                bone.MassCenter = ik.ReadVector3();
            }

            if (!ik.IsEnd)
            {
                model.Warnings.Add($"{ik.Context}: {ik.Remaining} unread bytes after bone IK data.");
            }

            foreach (OgfBone bone in model.Bones)
            {
                if (string.IsNullOrEmpty(bone.ParentName))
                {
                    continue;
                }

                bone.ParentIndex = model.FindBone(bone.ParentName);
                if (bone.ParentIndex < 0)
                {
                    model.Warnings.Add($"Bone '{bone.Name}' references missing parent '{bone.ParentName}'; it is treated as a root bone.");
                }
                else if (bone.ParentIndex == bone.Index)
                {
                    throw new XRayFormatException($"{context}: bone '{bone.Name}' is its own parent.");
                }
            }

            XRaySkeleton.ValidateHierarchy(model.Bones);
        }

        private static void ReadChildren(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfVisual parent, OgfModel model, string context)
        {
            if (!TryTake(chunks, consumed, ids.Children, out XRayChunk chunk))
            {
                if (ids.ChildrenL != 0 && chunks.ContainsKey(ids.ChildrenL))
                {
                    throw new XRayFormatException($"{context}: CHILDREN_L references level visuals; level geometry is not supported by the standalone importer.");
                }

                return;
            }

            List<XRayChunk> children = XRayChunks.ReadAll(chunk.CreateReader(context + " CHILDREN"));
            foreach (XRayChunk child in children)
            {
                string childName = parent.Name + " " + child.Id.ToString("00");
                parent.Children.Add(ReadVisual(child.CreateReader(childName), childName, false, model));
            }
        }

        private static void ReadEmbeddedMotions(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfModel model, string context)
        {
            bool hasParamsVersion = true;
            if (!TryTake(chunks, consumed, ids.SmParams1, out XRayChunk paramsChunk))
            {
                hasParamsVersion = false;
                if (!TryTake(chunks, consumed, ids.SmParams0, out paramsChunk))
                {
                    return;
                }
            }

            int motionsVersion;
            XRayChunk motionsChunk;
            if (TryTake(chunks, consumed, ids.Motions2, out motionsChunk))
            {
                motionsVersion = 2;
            }
            else if (TryTake(chunks, consumed, ids.Motions1, out motionsChunk))
            {
                motionsVersion = 1;
            }
            else if (TryTake(chunks, consumed, ids.Motions0, out motionsChunk))
            {
                motionsVersion = 0;
            }
            else
            {
                return;
            }

            var names = new List<string>();
            foreach (OgfBone bone in model.Bones)
            {
                names.Add(bone.Name);
            }

            var omf = new OmfData();
            OmfReader.ReadParams(paramsChunk.CreateReader(context + " S_SMPARAMS"), hasParamsVersion, names, omf);
            OmfReader.ReadMotions(motionsChunk.CreateReader(context + " S_MOTIONS"), motionsVersion, omf);
            model.EmbeddedMotions = omf;
        }

        private static void ReadTexture(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfVisual visual, string context)
        {
            if (TryTake(chunks, consumed, ids.Texture, out XRayChunk chunk))
            {
                XRayReader reader = chunk.CreateReader(context + " TEXTURE");
                visual.Texture = reader.ReadStringZ();
                visual.Shader = reader.ReadStringZ();
                return;
            }

            if (ids.TextureL != 0 && chunks.ContainsKey(ids.TextureL))
            {
                throw new XRayFormatException($"{context}: TEXTURE_L references level shaders; level visuals are not supported.");
            }

            throw new XRayFormatException($"{context}: required chunk TEXTURE (id {ids.Texture}) is missing.");
        }

        private static void RejectContainers(Dictionary<uint, XRayChunk> chunks, OgfChunkIds ids, string context)
        {
            if ((ids.VContainer != 0 && chunks.ContainsKey(ids.VContainer)) || (ids.GContainer != 0 && chunks.ContainsKey(ids.GContainer)))
            {
                throw new XRayFormatException($"{context}: geometry is stored in level vertex buffers (VCONTAINER/GCONTAINER); level visuals are not supported.");
            }
        }

        private static void ReadStaticVertices(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfVisual visual, string context)
        {
            RejectContainers(chunks, ids, context);
            XRayReader reader = Take(chunks, consumed, ids.Vertices, "VERTICES", context).CreateReader(context + " VERTICES");
            uint format = reader.ReadUInt32();
            int count = (int)reader.ReadUInt32();
            if (format != OgfFormat.FVF_OGF_STATIC)
            {
                throw new XRayFormatException($"{context}: static vertex format 0x{format:X} is not supported (expected 0x{OgfFormat.FVF_OGF_STATIC:X}).");
            }

            AllocateVertices(visual, count, false);
            visual.VertexFormat = format;
            for (int i = 0; i < count; i++)
            {
                visual.Positions[i] = reader.ReadVector3();
                visual.Normals[i] = reader.ReadVector3().Normalized();
                visual.U[i] = reader.ReadSingle();
                visual.V[i] = reader.ReadSingle();
            }
        }

        private static void ReadSkinnedVertices(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfVisual visual, OgfModel model, string context)
        {
            RejectContainers(chunks, ids, context);
            XRayReader reader = Take(chunks, consumed, ids.Vertices, "VERTICES", context).CreateReader(context + " VERTICES");
            uint format = reader.ReadUInt32();
            int count = (int)reader.ReadUInt32();
            visual.VertexFormat = format;
            AllocateVertices(visual, count, true);
            int boneCount = model.Bones.Count;

            if (format == OgfFormat.FVF_1L || format == OgfFormat.FVF_1L_CS)
            {
                // Two layouts exist: without tangent frame (36 bytes) and with it (60 bytes).
                bool compact = count * 36 == reader.Length - 8;
                for (int i = 0; i < count; i++)
                {
                    visual.Positions[i] = reader.ReadVector3();
                    visual.Normals[i] = reader.ReadVector3().Normalized();
                    if (!compact)
                    {
                        reader.Skip(24);
                    }

                    visual.U[i] = reader.ReadSingle();
                    visual.V[i] = reader.ReadSingle();
                    int bone = (int)reader.ReadUInt32();
                    visual.Weights[i] = MakeWeights(context, i, boneCount, 1, bone, 0, 0, 0, 1.0, 0.0, 0.0);
                }
            }
            else if (format == OgfFormat.FVF_2L || format == OgfFormat.FVF_2L_CS)
            {
                for (int i = 0; i < count; i++)
                {
                    int bone0 = reader.ReadUInt16();
                    int bone1 = reader.ReadUInt16();
                    visual.Positions[i] = reader.ReadVector3();
                    visual.Normals[i] = reader.ReadVector3().Normalized();
                    reader.Skip(24);
                    double weight = reader.ReadSingle();
                    visual.U[i] = reader.ReadSingle();
                    visual.V[i] = reader.ReadSingle();

                    // The stored weight belongs to the second bone (engine: lerp(M0, M1, w)).
                    visual.Weights[i] = MakeWeights(context, i, boneCount, 2, bone0, bone1, 0, 0, 1.0 - weight, weight, 0.0);
                }
            }
            else if (format == OgfFormat.FVF_3L_CS)
            {
                for (int i = 0; i < count; i++)
                {
                    int bone0 = reader.ReadUInt16();
                    int bone1 = reader.ReadUInt16();
                    int bone2 = reader.ReadUInt16();
                    visual.Positions[i] = reader.ReadVector3();
                    visual.Normals[i] = reader.ReadVector3().Normalized();
                    reader.Skip(24);
                    double weight0 = reader.ReadSingle();
                    double weight1 = reader.ReadSingle();
                    visual.U[i] = reader.ReadSingle();
                    visual.V[i] = reader.ReadSingle();
                    visual.Weights[i] = MakeWeights(context, i, boneCount, 3, bone0, bone1, bone2, 0, weight0, weight1, 0.0);
                }
            }
            else if (format == OgfFormat.FVF_4L_CS)
            {
                for (int i = 0; i < count; i++)
                {
                    int bone0 = reader.ReadUInt16();
                    int bone1 = reader.ReadUInt16();
                    int bone2 = reader.ReadUInt16();
                    int bone3 = reader.ReadUInt16();
                    visual.Positions[i] = reader.ReadVector3();
                    visual.Normals[i] = reader.ReadVector3().Normalized();
                    reader.Skip(24);
                    double weight0 = reader.ReadSingle();
                    double weight1 = reader.ReadSingle();
                    double weight2 = reader.ReadSingle();
                    visual.U[i] = reader.ReadSingle();
                    visual.V[i] = reader.ReadSingle();
                    visual.Weights[i] = MakeWeights(context, i, boneCount, 4, bone0, bone1, bone2, bone3, weight0, weight1, weight2);
                }
            }
            else
            {
                throw new XRayFormatException($"{context}: skinned vertex format 0x{format:X} is not supported (supported: 1-4 link).");
            }

            if (!reader.IsEnd)
            {
                model.Warnings.Add($"{reader.Context}: {reader.Remaining} unread bytes after {count} vertices.");
            }
        }

        /// <summary>
        /// Builds normalized influences. The last weight of 3/4-link vertices is implicit (1 - sum).
        /// Repeated bone indices are merged by summing their weights, which is what the engine's
        /// skinning sum produces. Small negative weights from float rounding are clamped to zero.
        /// </summary>
        private static OgfVertexWeights MakeWeights(string context, int vertex, int boneCount, int links, int b0, int b1, int b2, int b3, double w0, double w1, double w2)
        {
            var bones = new[] { b0, b1, b2, b3 };
            var weights = new double[4];
            weights[0] = w0;
            if (links == 2)
            {
                weights[1] = w1;
            }
            else if (links == 3)
            {
                weights[1] = w1;
                weights[2] = 1.0 - w0 - w1;
            }
            else if (links == 4)
            {
                weights[1] = w1;
                weights[2] = w2;
                weights[3] = 1.0 - w0 - w1 - w2;
            }

            var mergedBones = new int[4];
            var mergedWeights = new double[4];
            int count = 0;
            for (int i = 0; i < links; i++)
            {
                if (bones[i] < 0 || bones[i] >= boneCount)
                {
                    throw new XRayFormatException($"{context}: vertex {vertex} references bone {bones[i]}, skeleton has {boneCount} bones.");
                }

                double weight = Math.Max(0.0, weights[i]);
                int slot = Array.IndexOf(mergedBones, bones[i], 0, count);
                if (slot >= 0)
                {
                    mergedWeights[slot] += weight;
                }
                else
                {
                    mergedBones[count] = bones[i];
                    mergedWeights[count] = weight;
                    count++;
                }
            }

            double total = 0.0;
            for (int i = 0; i < count; i++)
            {
                total += mergedWeights[i];
            }

            if (total <= 1e-12)
            {
                mergedWeights[0] = 1.0;
                for (int i = 1; i < count; i++)
                {
                    mergedWeights[i] = 0.0;
                }

                total = 1.0;
            }

            // Sort by descending weight (required by Unity for BoneWeight1, harmless otherwise).
            for (int i = 1; i < count; i++)
            {
                for (int j = i; j > 0 && mergedWeights[j] > mergedWeights[j - 1]; j--)
                {
                    double tw = mergedWeights[j];
                    mergedWeights[j] = mergedWeights[j - 1];
                    mergedWeights[j - 1] = tw;
                    int tb = mergedBones[j];
                    mergedBones[j] = mergedBones[j - 1];
                    mergedBones[j - 1] = tb;
                }
            }

            return new OgfVertexWeights
            {
                Count = count,
                Bone0 = mergedBones[0],
                Bone1 = mergedBones[1],
                Bone2 = mergedBones[2],
                Bone3 = mergedBones[3],
                Weight0 = mergedWeights[0] / total,
                Weight1 = mergedWeights[1] / total,
                Weight2 = mergedWeights[2] / total,
                Weight3 = mergedWeights[3] / total
            };
        }

        private static void AllocateVertices(OgfVisual visual, int count, bool skinned)
        {
            if (count < 0 || count > 16 * 1024 * 1024)
            {
                throw new XRayFormatException($"{visual.Name}: invalid vertex count {count}.");
            }

            visual.Positions = new XVector3[count];
            visual.Normals = new XVector3[count];
            visual.U = new double[count];
            visual.V = new double[count];
            visual.Weights = skinned ? new OgfVertexWeights[count] : null;
        }

        private static void ReadIndices(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfVisual visual, string context)
        {
            if (ids.IContainer != 0 && chunks.ContainsKey(ids.IContainer))
            {
                throw new XRayFormatException($"{context}: indices are stored in a level index buffer (ICONTAINER); level visuals are not supported.");
            }

            XRayReader reader = Take(chunks, consumed, ids.Indices, "INDICES", context).CreateReader(context + " INDICES");
            int count = (int)reader.ReadUInt32();
            if (count % 3 != 0)
            {
                throw new XRayFormatException($"{context}: index count {count} is not a multiple of 3.");
            }

            var indices = new int[count];
            for (int i = 0; i < count; i++)
            {
                int index = reader.ReadUInt16();
                if (index >= visual.VertexCount)
                {
                    throw new XRayFormatException($"{context}: index {index} at position {i} exceeds vertex count {visual.VertexCount}.");
                }

                indices[i] = index;
            }

            visual.Indices = indices;
        }

        private static void ReadSwi(Dictionary<uint, XRayChunk> chunks, HashSet<uint> consumed, OgfChunkIds ids, OgfVisual visual, string context)
        {
            if (!TryTake(chunks, consumed, ids.SwiData, out XRayChunk chunk))
            {
                return;
            }

            if (visual.FormatVersion == OgfFormat.VERSION_4)
            {
                // FSlideWindowItem list: 4 reserved dwords (absent in old files), count, items.
                XRayReader reader = chunk.CreateReader(context + " SWIDATA");
                uint reserved = 0;
                uint itemsCount = 0;
                while (reserved == 0 && !reader.IsEnd)
                {
                    reserved = reader.ReadUInt32();
                    if (reserved != 0)
                    {
                        itemsCount = reserved;
                    }
                }

                if (itemsCount == 0)
                {
                    return;
                }

                // Highest detail level is the first sliding window.
                int offset = (int)reader.ReadUInt32();
                int triangles = reader.ReadUInt16();
                reader.ReadUInt16();
                if (offset < 0 || offset + triangles * 3 > visual.Indices.Length)
                {
                    throw new XRayFormatException($"{context}: SWI window (offset {offset}, {triangles} triangles) exceeds the index buffer ({visual.Indices.Length}).");
                }

                var lod0 = new int[triangles * 3];
                Array.Copy(visual.Indices, offset, lod0, 0, lod0.Length);
                visual.Indices = lod0;
            }
            else
            {
                ApplySwiV3(chunk, visual, context);
            }
        }

        private static void ApplySwiV3(XRayChunk chunk, OgfVisual visual, string context)
        {
            int minVertices = -1;
            var vsplitFix = new List<int>();
            int[] facesAffected = null;
            foreach (XRayChunk sub in XRayChunks.ReadAll(chunk.CreateReader(context + " SWIDATA")))
            {
                XRayReader reader = sub.CreateReader(context + " SWIDATA." + sub.Id);
                if (sub.Id == 1)
                {
                    minVertices = (int)reader.ReadUInt32();
                    reader.ReadUInt32();
                }
                else if (sub.Id == 2)
                {
                    int splitCount = sub.Length / 4;
                    for (int i = 0; i < splitCount; i++)
                    {
                        reader.ReadUInt16();
                        reader.ReadByte();
                        vsplitFix.Add(reader.ReadByte());
                    }
                }
                else if (sub.Id == 3)
                {
                    int count = (int)reader.ReadUInt32();
                    facesAffected = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        facesAffected[i] = reader.ReadUInt16();
                    }
                }
            }

            if (minVertices < 0 || facesAffected == null)
            {
                throw new XRayFormatException($"{context}: incomplete v3 SWIDATA (header or faces missing).");
            }

            int fixCurrent = 0;
            for (int vertex = minVertices; vertex < visual.VertexCount; vertex++)
            {
                int fixFaces = vsplitFix[vertex - minVertices];
                for (int i = fixCurrent; i < fixCurrent + fixFaces; i++)
                {
                    visual.Indices[facesAffected[i]] = vertex;
                }

                fixCurrent += fixFaces;
            }
        }

        /// <summary>
        /// OGF v3 stores skinned positions in the space of the first influencing bone
        /// (blender-xray update_verts_coords_v3). They are moved to model space with the rest pose.
        /// </summary>
        private static void ConvertV3BoneLocalVertices(OgfModel model)
        {
            var skeleton = new XRaySkeleton(model.Bones);
            foreach (OgfVisual visual in model.GetMeshVisuals())
            {
                if (!visual.IsSkinned)
                {
                    continue;
                }

                for (int i = 0; i < visual.VertexCount; i++)
                {
                    XMatrix4 rest = skeleton.RestWorld[visual.Weights[i].Bone0];
                    visual.Positions[i] = rest.TransformPoint(visual.Positions[i]);
                    visual.Normals[i] = rest.TransformVector(visual.Normals[i]).Normalized();
                }
            }

            model.Warnings.Add("OGF v3 skinned geometry was converted from bone-local space following blender-xray; v3 support has not been verified against reference data.");
        }
    }
}
