using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using XRay.Importer.Tests.Verification;

namespace XRay.Importer.Tests
{
    /// <summary>
    /// Importer math (Core) against data exported from Blender 3.6 + blender-xray:
    /// rest matrices (A), pose matrices (B), skinned geometry (C) for hands and weapon separately (D).
    /// </summary>
    public sealed class BlenderReferenceTests
    {
        private const double MATRIX_TOLERANCE = 1e-4;
        private const double GEOMETRY_TOLERANCE = 1e-4;

        [Test]
        public void Hands_MatchesUserBlenderDump()
        {
            VerificationReport report = VerifyJson(XRayTestData.HANDS_OGF, XRayTestData.HANDS_JSON);
            AssertJsonReport(report, 42, 5);
        }

        [Test]
        public void Weapon_MatchesUserBlenderDump()
        {
            VerificationReport report = VerifyJson(XRayTestData.WEAPON_OGF, XRayTestData.WEAPON_JSON);
            AssertJsonReport(report, 17, 2);
        }

        [Test]
        public void Hands_MatchesBlenderPerVertex()
        {
            VertexVerificationReport report = VertexReferenceVerifier.Verify(XRayTestData.Ogf(XRayTestData.HANDS_OGF), XRayTestData.Omf(XRayTestData.HANDS_OMF), XRayTestData.VertexReference(XRayTestData.HANDS_VERTEX_REFERENCE));
            Debug.Log(report.Log.ToString());
            AssertVertexReport(report);
        }

        [Test]
        public void Weapon_MatchesBlenderPerVertex()
        {
            VertexVerificationReport report = VertexReferenceVerifier.Verify(XRayTestData.Ogf(XRayTestData.WEAPON_OGF), XRayTestData.Omf(XRayTestData.WEAPON_OMF), XRayTestData.VertexReference(XRayTestData.WEAPON_VERTEX_REFERENCE));
            Debug.Log(report.Log.ToString());
            AssertVertexReport(report);
        }

        private static VerificationReport VerifyJson(string ogf, string json)
        {
            var sources = new List<MotionSource>
            {
                new MotionSource("hands omf", XRayTestData.Omf(XRayTestData.HANDS_OMF)),
                new MotionSource("weapon omf", XRayTestData.Omf(XRayTestData.WEAPON_OMF))
            };

            VerificationReport report = BlenderJsonVerifier.Verify(XRayTestData.Ogf(ogf), sources, XRayTestData.Json(json));
            Debug.Log(report.Log.ToString());
            return report;
        }

        private static void AssertJsonReport(VerificationReport report, int bones, int minimumComparedFrames)
        {
            Assert.AreEqual(bones, report.BoneCount);
            Assert.IsEmpty(report.MissingBones);
            Assert.Less(report.RestMatrixError, MATRIX_TOLERANCE, "rest matrix, bone " + report.RestWorstBone);
            Assert.Less(report.RestBoundsError, GEOMETRY_TOLERANCE, "rest bounds");
            Assert.Less(report.RestMeshSizeError, GEOMETRY_TOLERANCE, "rest mesh sizes");

            int compared = 0;
            foreach (FrameVerification frame in report.Frames)
            {
                if (frame.Skipped)
                {
                    continue;
                }

                compared++;
                string label = $"{frame.Action} f{frame.Frame}";
                Assert.IsFalse(frame.UnsupportedConstraint, label + ": Blender constraint on a non-root bone");
                Assert.Less(frame.PoseMatrixError, MATRIX_TOLERANCE, label + " pose, bone " + frame.PoseWorstBone);
                Assert.Less(frame.LocalMatrixError, MATRIX_TOLERANCE, label + " local pose");
                Assert.Less(frame.BoundsError, GEOMETRY_TOLERANCE, label + " bounds");
                Assert.Less(frame.MeshSizeError, GEOMETRY_TOLERANCE, label + " mesh size " + frame.MeshSizeWorst);
            }

            Assert.GreaterOrEqual(compared, minimumComparedFrames, "too few comparable frames in the reference");
        }

        private static void AssertVertexReport(VertexVerificationReport report)
        {
            Assert.AreEqual(0, report.MissingMeshes);
            Assert.AreEqual(0, report.UnmatchedVertices);
            Assert.Greater(report.MatchedVertices, 0);
            Assert.Greater(report.Frames.Count, 0);
            Assert.Less(report.RestMatrixError, MATRIX_TOLERANCE, "rest, bone " + report.RestWorstBone);
            foreach (VertexFrameResult frame in report.Frames)
            {
                string label = $"{frame.Action} f{frame.Frame}";
                Assert.Less(frame.PoseMatrixError, MATRIX_TOLERANCE, label + " pose, bone " + frame.PoseWorstBone);
                Assert.Less(frame.LocalMatrixError, MATRIX_TOLERANCE, label + " local pose");
                Assert.Less(frame.MaxVertexError, GEOMETRY_TOLERANCE, label + " vertices, mesh " + frame.WorstMesh);
            }
        }
    }
}
