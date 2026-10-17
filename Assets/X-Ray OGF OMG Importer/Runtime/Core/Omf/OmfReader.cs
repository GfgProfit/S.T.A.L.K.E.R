using System.Collections.Generic;
using XRay.Importer.Core.IO;

namespace XRay.Importer.Core.Omf
{
    /// <summary>
    /// Reader for skeletal motions (*.omf files and the S_MOTIONS/S_SMPARAMS chunks embedded in OGF).
    /// Binary layout follows blender-xray formats/omf/imp.py.
    ///
    /// Coordinate conversion. A key stores a parent-relative quaternion q = (x, y, z, w) / 32767 and a
    /// translation t in X-Ray model space. blender-xray builds pose matrices
    ///     P_blender = MATRIX_BONE * prod_i( T(Z t_i) * R(w, x, y, -z)_i ),  MATRIX_BONE = S * Z,
    /// where S swaps Y/Z and Z = diag(1, 1, -1). Moving the reflections inside the product gives
    ///     P_blender = S * prod_i( T(t_i) * R(conj(q_i)) ) * Z,
    /// i.e. in native space (left-handed, Y up = Unity) the parent-relative transform of a key is
    /// T(t) * R(conj(q)). No rest pose enters this expression: the rest matrices in blender-xray's
    /// xmat cancel out against Blender's own rest composition.
    /// </summary>
    public static class OmfReader
    {
        public static OmfData ReadFile(byte[] data, string context)
        {
            var reader = new XRayReader(data, context);
            Dictionary<uint, XRayChunk> chunks = XRayChunks.ReadMap(reader);

            if (!chunks.TryGetValue(OmfFormat.CHUNK_SMPARAMS_1, out XRayChunk paramsChunk))
            {
                throw new XRayFormatException($"{context}: S_SMPARAMS chunk ({OmfFormat.CHUNK_SMPARAMS_1}) is missing, this is not an OMF motion file.");
            }

            if (!chunks.TryGetValue(OmfFormat.CHUNK_MOTIONS_2, out XRayChunk motionsChunk))
            {
                throw new XRayFormatException($"{context}: S_MOTIONS chunk ({OmfFormat.CHUNK_MOTIONS_2}) is missing.");
            }

            var omf = new OmfData();
            ReadParams(paramsChunk.CreateReader(context + " S_SMPARAMS"), true, null, omf);
            ReadMotions(motionsChunk.CreateReader(context + " S_MOTIONS"), 2, omf);

            foreach (uint id in chunks.Keys)
            {
                if (id != OmfFormat.CHUNK_SMPARAMS_1 && id != OmfFormat.CHUNK_MOTIONS_2)
                {
                    omf.Warnings.Add($"Unknown OMF chunk {id} ignored.");
                }
            }

            return omf;
        }

        /// <param name="hasVersion">True for S_SMPARAMS_1 (starts with a version word), false for S_SMPARAMS_0.</param>
        /// <param name="ogfBoneNames">OGF bone names by index, required by params versions 0 and 1 that store bone ids only.</param>
        public static void ReadParams(XRayReader reader, bool hasVersion, IList<string> ogfBoneNames, OmfData omf)
        {
            ushort version = 0;
            if (hasVersion)
            {
                version = reader.ReadUInt16();
                if (version < 1 || version > 4)
                {
                    throw new XRayFormatException($"{reader.Context}: unsupported motion params version {version} (supported: 1-4).");
                }
            }

            omf.ParamsVersion = version;

            ushort partCount = reader.ReadUInt16();
            int runningIndex = 0;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                var part = new OmfBonePart { Name = reader.ReadStringZ() };
                ushort boneCount = reader.ReadUInt16();
                for (int bone = 0; bone < boneCount; bone++)
                {
                    string boneName;
                    int boneId;
                    if (version == 3 || version == 4)
                    {
                        boneName = reader.ReadStringZ();
                        boneId = (int)reader.ReadUInt32();
                    }
                    else if (version == 2)
                    {
                        // Version 2 stores names only. The engine resolves them against the skeleton, so the
                        // OGF bone index is used when known; otherwise the running index across partitions.
                        // blender-xray uses the index inside the partition, which collides for several partitions.
                        boneName = reader.ReadStringZ();
                        boneId = ogfBoneNames != null ? ogfBoneNames.IndexOf(boneName) : runningIndex;
                        if (boneId < 0)
                        {
                            throw new XRayFormatException($"{reader.Context}: motion params bone '{boneName}' is not present in the OGF skeleton.");
                        }

                        if (ogfBoneNames == null && partIndex == 0 && bone == 0)
                        {
                            omf.Warnings.Add("Motion params version 2 has no bone ids; bone order is assumed to follow the partitions.");
                        }
                    }
                    else
                    {
                        boneId = (int)reader.ReadUInt32();
                        if (ogfBoneNames == null || boneId < 0 || boneId >= ogfBoneNames.Count)
                        {
                            throw new XRayFormatException($"{reader.Context}: motion params version {version} references bone id {boneId} which cannot be resolved without OGF bone names.");
                        }

                        boneName = ogfBoneNames[boneId];
                    }

                    if (omf.BoneNames.TryGetValue(boneId, out string existing) && existing != boneName)
                    {
                        throw new XRayFormatException($"{reader.Context}: bone id {boneId} is assigned to both '{existing}' and '{boneName}'.");
                    }

                    part.BoneNames.Add(boneName);
                    part.BoneIds.Add(boneId);
                    omf.BoneNames[boneId] = boneName;
                    runningIndex++;
                }

                omf.BoneParts.Add(part);
            }

            ushort motionCount = reader.ReadUInt16();
            for (int motionIndex = 0; motionIndex < motionCount; motionIndex++)
            {
                var prm = new OmfMotionParams { Name = reader.ReadStringZ() };
                prm.Flags = reader.ReadUInt32();
                prm.BoneOrPart = reader.ReadUInt16();
                prm.MotionIndex = reader.ReadUInt16();
                prm.Speed = reader.ReadSingle();
                prm.Power = reader.ReadSingle();
                prm.Accrue = reader.ReadSingle();
                prm.Falloff = reader.ReadSingle();

                if (version == 4)
                {
                    uint markCount = reader.ReadUInt32();
                    for (uint markIndex = 0; markIndex < markCount; markIndex++)
                    {
                        var mark = new OmfMotionMark { Name = reader.ReadStringLine() };
                        uint intervalCount = reader.ReadUInt32();
                        for (uint i = 0; i < intervalCount; i++)
                        {
                            mark.Intervals.Add(reader.ReadSingle());
                            mark.Intervals.Add(reader.ReadSingle());
                        }

                        prm.Marks.Add(mark);
                    }
                }
                else if (version == 0)
                {
                    // bNoLoop byte of the oldest params layout.
                    if (reader.ReadByte() != 0)
                    {
                        prm.Flags |= OmfFormat.MOTION_STOP_AT_END;
                    }
                }

                omf.Params.Add(prm);
            }

            if (!reader.IsEnd)
            {
                omf.Warnings.Add($"{reader.Context}: {reader.Remaining} unread bytes after motion params.");
            }
        }

        public static void ReadMotions(XRayReader reader, int motionsVersion, OmfData omf)
        {
            if (motionsVersion != 2)
            {
                throw new XRayFormatException($"{reader.Context}: motion data version {motionsVersion} (S_MOTIONS_{motionsVersion}, pre-release builds) is not supported. Only S_MOTIONS_2 (SoC/CS/CoP) is supported.");
            }

            omf.MotionsVersion = motionsVersion;
            List<XRayChunk> chunks = XRayChunks.ReadAll(reader);
            if (chunks.Count == 0 || chunks[0].Id != OmfFormat.CHUNK_MOTIONS_COUNT)
            {
                throw new XRayFormatException($"{reader.Context}: the first motions sub-chunk must be the motion count chunk.");
            }

            uint declaredCount = chunks[0].CreateReader(reader.Context + " count").ReadUInt32();
            if (declaredCount != chunks.Count - 1)
            {
                omf.Warnings.Add($"{reader.Context}: motion count chunk declares {declaredCount} motions, file contains {chunks.Count - 1}.");
            }

            var paramsByName = new Dictionary<string, OmfMotionParams>();
            foreach (OmfMotionParams prm in omf.Params)
            {
                if (!paramsByName.ContainsKey(prm.Name))
                {
                    paramsByName.Add(prm.Name, prm);
                }
            }

            int boneCount = omf.BoneNames.Count;
            for (int i = 1; i < chunks.Count; i++)
            {
                XRayChunk chunk = chunks[i];
                int motionId = (int)chunk.Id - 1;
                XRayReader motionReader = chunk.CreateReader($"{reader.Context} motion #{motionId}");
                omf.Motions.Add(ReadMotion(motionReader, motionId, boneCount, omf, paramsByName));
            }
        }

        private static OmfMotion ReadMotion(XRayReader reader, int motionId, int boneCount, OmfData omf, Dictionary<string, OmfMotionParams> paramsByName)
        {
            string name = reader.ReadStringZ();
            int length = (int)reader.ReadUInt32();
            if (length <= 0)
            {
                throw new XRayFormatException($"{reader.Context}: motion '{name}' has invalid key count {length}.");
            }

            OmfMotionParams prm = null;
            if (!string.IsNullOrEmpty(name))
            {
                paramsByName.TryGetValue(name, out prm);
            }

            if (prm == null)
            {
                if (motionId < 0 || motionId >= omf.Params.Count)
                {
                    throw new XRayFormatException($"{reader.Context}: motion '{name}' has no params entry (by name or index {motionId}).");
                }

                prm = omf.Params[motionId];
                omf.Warnings.Add($"Motion #{motionId} name '{name}' does not match params, using params name '{prm.Name}'.");
                name = prm.Name;
            }

            var motion = new OmfMotion { Name = name, Index = motionId, FrameCount = length, Params = prm };

            for (int boneId = 0; boneId < boneCount; boneId++)
            {
                if (!omf.BoneNames.TryGetValue(boneId, out string boneName))
                {
                    throw new XRayFormatException($"{reader.Context}: motion data expects bone id {boneId}, which is missing from the bone partitions.");
                }

                var track = new OmfBoneTrack { BoneId = boneId, BoneName = boneName };
                byte flags = reader.ReadByte();
                track.Flags = flags;
                bool translationPresent = (flags & OmfFormat.FL_T_KEY_PRESENT) != 0;
                bool rotationAbsent = (flags & OmfFormat.FL_R_KEY_ABSENT) != 0;
                bool highQuality = (flags & OmfFormat.KPF_T_HQ) != 0;

                if (rotationAbsent)
                {
                    track.Rotations = new[] { ReadQuaternion(reader) };
                }
                else
                {
                    reader.ReadUInt32();
                    var rotations = new XQuaternion[length];
                    for (int key = 0; key < length; key++)
                    {
                        rotations[key] = ReadQuaternion(reader);
                    }

                    track.Rotations = rotations;
                }

                if (translationPresent)
                {
                    reader.ReadUInt32();
                    var raw = new int[length * 3];
                    for (int key = 0; key < length * 3; key++)
                    {
                        raw[key] = highQuality ? reader.ReadInt16() : reader.ReadSByte();
                    }

                    XVector3 size = reader.ReadVector3();
                    XVector3 init = reader.ReadVector3();
                    var translations = new XVector3[length];
                    for (int key = 0; key < length; key++)
                    {
                        translations[key] = new XVector3(raw[key * 3] * size.X + init.X, raw[key * 3 + 1] * size.Y + init.Y, raw[key * 3 + 2] * size.Z + init.Z);
                    }

                    track.Translations = translations;
                }
                else
                {
                    track.Translations = new[] { reader.ReadVector3() };
                }

                motion.Tracks.Add(track);
            }

            if (!reader.IsEnd)
            {
                omf.Warnings.Add($"{reader.Context}: {reader.Remaining} unread bytes after motion '{name}'.");
            }

            return motion;
        }

        /// <summary>Dequantizes a CKeyQR and converts it to the native column-vector convention (see class remarks).</summary>
        private static XQuaternion ReadQuaternion(XRayReader reader)
        {
            double x = reader.ReadInt16() / OmfFormat.QUAT_QUANT;
            double y = reader.ReadInt16() / OmfFormat.QUAT_QUANT;
            double z = reader.ReadInt16() / OmfFormat.QUAT_QUANT;
            double w = reader.ReadInt16() / OmfFormat.QUAT_QUANT;
            var stored = new XQuaternion(x, y, z, w);
            if (stored.Norm < 1e-6)
            {
                throw new XRayFormatException($"{reader.Context}: degenerate (zero) rotation key at offset {reader.Position - 8}.");
            }

            return stored.Conjugate().Normalized();
        }
    }
}
