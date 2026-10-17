using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using XRay.Importer.Core;
using XRay.Importer.Core.IO;
using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Omf;

namespace XRay.Importer.Tests
{
    /// <summary>Binary format parsing of the real test files and explicit failure on unsupported data.</summary>
    public sealed class FormatTests
    {
        [Test]
        public void HandsOgf_HasSkeletonAndSkinnedMeshes()
        {
            OgfModel model = XRayTestData.Ogf(XRayTestData.HANDS_OGF);
            Assert.AreEqual(OgfModelKind.SkeletonAnim, model.Root.Kind);
            Assert.AreEqual(4, model.Root.FormatVersion);
            Assert.AreEqual(42, model.Bones.Count);
            Assert.AreEqual(4, model.GetMeshVisuals().Count);
            Assert.AreEqual("bip01", model.Bones[0].Name);
            Assert.AreEqual(-1, model.Bones[0].ParentIndex);
            AssertSkinning(model, 4);
            AssertWinding(model);
        }

        [Test]
        public void WeaponOgf_HasSkeletonAndRigidParts()
        {
            OgfModel model = XRayTestData.Ogf(XRayTestData.WEAPON_OGF);
            Assert.AreEqual(OgfModelKind.SkeletonAnim, model.Root.Kind);
            Assert.AreEqual(17, model.Bones.Count);
            Assert.AreEqual(18, model.GetMeshVisuals().Count);
            Assert.GreaterOrEqual(model.FindBone("launcher_scope"), 0);
            Assert.GreaterOrEqual(model.FindBone("shell"), 0);
            AssertSkinning(model, 1);
            AssertWinding(model);
        }

        [Test]
        public void HandsOmf_ContainsAllMotions()
        {
            OmfData omf = XRayTestData.Omf(XRayTestData.HANDS_OMF);
            Assert.AreEqual(4, omf.ParamsVersion);
            Assert.AreEqual(42, omf.BoneNames.Count);
            Assert.AreEqual(96, omf.Motions.Count);
            Assert.AreEqual(100, omf.FindMotion("ak74_idle").FrameCount);
            Assert.AreEqual(2, omf.FindMotion("ak74_idle_aim_g").FrameCount);
            Assert.IsEmpty(omf.Warnings);
            AssertTracks(omf);
        }

        [Test]
        public void WeaponOmf_ContainsWeaponMotionsOnly()
        {
            OmfData omf = XRayTestData.Omf(XRayTestData.WEAPON_OMF);
            Assert.AreEqual(17, omf.BoneNames.Count);
            Assert.AreEqual(10, omf.Motions.Count);
            Assert.AreEqual(2, omf.FindMotion("idle_gl").FrameCount);
            Assert.IsNull(omf.FindMotion("ak74_idle"));
            AssertTracks(omf);
        }

        [Test]
        public void BindEuler_MatchesUnityQuaternionEuler()
        {
            // The native rest rotation Ry * Rx * Rz must coincide with Unity's Euler convention,
            // which is what lets the importer write localRotation directly.
            var random = new System.Random(12345);
            for (int i = 0; i < 200; i++)
            {
                var radians = new XVector3(random.NextDouble() * 6.0 - 3.0, random.NextDouble() * 6.0 - 3.0, random.NextDouble() * 6.0 - 3.0);
                XQuaternion native = XQuaternion.FromXRayBindEuler(radians);
                Quaternion unity = Quaternion.Euler((float)(radians.X * 180.0 / Math.PI), (float)(radians.Y * 180.0 / Math.PI), (float)(radians.Z * 180.0 / Math.PI));
                double dot = Math.Abs(native.X * unity.x + native.Y * unity.y + native.Z * unity.z + native.W * unity.w);
                Assert.Greater(dot, 1.0 - 1e-6, $"Euler {radians}");
            }
        }

        [Test]
        public void TruncatedOgf_ThrowsFormatException()
        {
            byte[] data = System.IO.File.ReadAllBytes(XRayTestData.Require(XRayTestData.WEAPON_OGF));
            var truncated = new byte[data.Length / 2];
            Array.Copy(data, truncated, truncated.Length);
            Assert.Throws<XRayFormatException>(() => OgfReader.ReadFile(truncated, "truncated.ogf"));
        }

        [Test]
        public void UnsupportedOgfVersion_ThrowsFormatException()
        {
            byte[] data = Chunk(1, new byte[] { 2, 0, 0, 0 });
            XRayFormatException exception = Assert.Throws<XRayFormatException>(() => OgfReader.ReadFile(data, "v2.ogf"));
            StringAssert.Contains("version 2", exception.Message);
        }

        [Test]
        public void CompressedChunk_ThrowsFormatException()
        {
            byte[] data = Chunk(0x80000001u, new byte[] { 4, 0, 0, 0, 0, 0, 0, 0 });
            XRayFormatException exception = Assert.Throws<XRayFormatException>(() => OgfReader.ReadFile(data, "compressed.ogf"));
            StringAssert.Contains("compressed", exception.Message);
        }

        [Test]
        public void OmfWithoutParams_ThrowsFormatException()
        {
            byte[] data = Chunk(OmfFormat.CHUNK_MOTIONS_2, new byte[0]);
            Assert.Throws<XRayFormatException>(() => OmfReader.ReadFile(data, "broken.omf"));
        }

        private static void AssertSkinning(OgfModel model, int expectedMaxInfluences)
        {
            int maxInfluences = 0;
            foreach (OgfVisual visual in model.GetMeshVisuals())
            {
                Assert.IsTrue(visual.IsSkinned, visual.Name);
                for (int i = 0; i < visual.VertexCount; i++)
                {
                    OgfVertexWeights weights = visual.Weights[i];
                    double sum = 0.0;
                    for (int k = 0; k < weights.Count; k++)
                    {
                        Assert.That(weights.GetBone(k), Is.InRange(0, model.Bones.Count - 1));
                        Assert.GreaterOrEqual(weights.GetWeight(k), 0.0);
                        sum += weights.GetWeight(k);
                    }

                    Assert.AreEqual(1.0, sum, 1e-9, $"{visual.Name} vertex {i}");
                    maxInfluences = Math.Max(maxInfluences, weights.Count);
                }
            }

            Assert.AreEqual(expectedMaxInfluences, maxInfluences);
        }

        /// <summary>Unity front faces are clockwise in a left-handed space: cross(b - a, c - a) must follow the stored normals.</summary>
        private static void AssertWinding(OgfModel model)
        {
            int agree = 0;
            int total = 0;
            foreach (OgfVisual visual in model.GetMeshVisuals())
            {
                for (int t = 0; t < visual.Indices.Length; t += 3)
                {
                    XVector3 a = visual.Positions[visual.Indices[t]];
                    XVector3 b = visual.Positions[visual.Indices[t + 1]];
                    XVector3 c = visual.Positions[visual.Indices[t + 2]];
                    XVector3 face = XVector3.Cross(b - a, c - a);
                    if (face.Length < 1e-12)
                    {
                        continue;
                    }

                    XVector3 normal = visual.Normals[visual.Indices[t]] + visual.Normals[visual.Indices[t + 1]] + visual.Normals[visual.Indices[t + 2]];
                    total++;
                    if (XVector3.Dot(face, normal) > 0.0)
                    {
                        agree++;
                    }
                }
            }

            Assert.Greater(agree, total * 0.999, $"{agree}/{total} triangles agree with stored normals");
        }

        private static void AssertTracks(OmfData omf)
        {
            foreach (OmfMotion motion in omf.Motions)
            {
                Assert.AreEqual(omf.BoneNames.Count, motion.Tracks.Count, motion.Name);
                foreach (OmfBoneTrack track in motion.Tracks)
                {
                    Assert.That(track.Rotations.Length == 1 || track.Rotations.Length == motion.FrameCount, motion.Name + "/" + track.BoneName);
                    Assert.That(track.Translations.Length == 1 || track.Translations.Length == motion.FrameCount, motion.Name + "/" + track.BoneName);
                    foreach (XQuaternion q in track.Rotations)
                    {
                        Assert.AreEqual(1.0, q.Norm, 1e-9);
                    }
                }
            }
        }

        private static byte[] Chunk(uint id, byte[] payload)
        {
            var result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(id));
            result.AddRange(BitConverter.GetBytes((uint)payload.Length));
            result.AddRange(payload);
            return result.ToArray();
        }
    }
}
