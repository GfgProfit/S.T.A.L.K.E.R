using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using XRay.Importer.Core;
using XRay.Importer.Core.Ogf;
using XRay.Importer.Core.Omf;
using XRay.Importer.Core.Skeleton;

namespace XRay.Importer.Tests.Verification
{
    public sealed class VertexFrameResult
    {
        public string Action;
        public int Frame;
        public int AnimatedBones;
        public double PoseMatrixError;
        public string PoseWorstBone;
        public double PosePositionError;
        public double PoseRotationErrorDegrees;
        public double LocalMatrixError;
        public double MaxVertexError;
        public double RmsVertexError;
        public string WorstMesh;
        public double MaxDisplacementFromRest;
    }

    public sealed class VertexVerificationReport
    {
        public string Model;
        public double RestMatrixError;
        public string RestWorstBone;
        public double RestVertexError;
        public int MatchedVertices;
        public int UnmatchedVertices;
        public int AmbiguousVertices;
        public int MissingMeshes;
        public List<VertexFrameResult> Frames = new List<VertexFrameResult>();
        public StringBuilder Log = new StringBuilder();

        public double MaxPoseMatrixError
        {
            get
            {
                double max = 0.0;
                foreach (VertexFrameResult frame in Frames)
                {
                    max = Math.Max(max, frame.PoseMatrixError);
                }

                return max;
            }
        }

        public double MaxVertexError
        {
            get
            {
                double max = RestVertexError;
                foreach (VertexFrameResult frame in Frames)
                {
                    max = Math.Max(max, frame.MaxVertexError);
                }

                return max;
            }
        }
    }

    /// <summary>
    /// Vertex-by-vertex comparison with Blender. Raw OGF vertices are matched to Blender vertices by rest
    /// position (blender-xray merges vertices with equal position and weights, so counts differ);
    /// if several Blender vertices share a position, the closest deformed candidate is used.
    /// </summary>
    public static class VertexReferenceVerifier
    {
        private const double MATCH_TOLERANCE = 1e-4;
        private const double CELL = 1e-3;

        public static VertexVerificationReport Verify(OgfModel model, OmfData motions, BlenderVertexReference reference)
        {
            var report = new VertexVerificationReport { Model = reference.Ogf };
            var skeleton = new XRaySkeleton(model.Bones);

            foreach (KeyValuePair<string, XMatrix4> pair in reference.Rest)
            {
                int index = skeleton.IndexOf(pair.Key);
                if (index < 0)
                {
                    throw new InvalidOperationException($"Reference bone '{pair.Key}' is missing in the OGF skeleton.");
                }

                double error = XMatrix4.MaxAbsDifference(BlenderSpace.BoneToBlender(skeleton.RestWorld[index]), pair.Value);
                if (error > report.RestMatrixError)
                {
                    report.RestMatrixError = error;
                    report.RestWorstBone = pair.Key;
                }
            }

            var mappings = new List<MeshMapping>();
            foreach (OgfVisual visual in model.GetMeshVisuals())
            {
                BlenderVertexReference.Mesh mesh = reference.Meshes.Find(m => m.Name == visual.Name);
                if (mesh == null)
                {
                    report.MissingMeshes++;
                    continue;
                }

                MeshMapping mapping = Map(visual, mesh, reference, report);
                mappings.Add(mapping);
            }

            Line(report, $"[{report.Model}] rest matrix err {F(report.RestMatrixError)} ('{report.RestWorstBone}'), vertices matched {report.MatchedVertices}, unmatched {report.UnmatchedVertices}, ambiguous {report.AmbiguousVertices}, rest residual {F(report.RestVertexError)}, missing meshes {report.MissingMeshes}");

            XMatrix4[] restWorld = XRayPose.EvaluateWorld(skeleton, null, 0);
            foreach (BlenderVertexReference.Frame frame in reference.Frames)
            {
                OmfMotion motion = motions.FindMotion(frame.Action);
                if (motion == null)
                {
                    throw new InvalidOperationException($"Motion '{frame.Action}' is missing in {reference.Omf}.");
                }

                int key = Math.Min(frame.FrameIndex, motion.FrameCount - 1);
                XMatrix4[] world = XRayPose.EvaluateWorld(skeleton, motion, key);
                var result = new VertexFrameResult { Action = frame.Action, Frame = frame.FrameIndex };

                foreach (KeyValuePair<string, XMatrix4> pair in frame.Pose)
                {
                    int index = skeleton.IndexOf(pair.Key);
                    XMatrix4 computed = BlenderSpace.BoneToBlender(world[index]);
                    if (XMatrix4.MaxAbsDifference(pair.Value, reference.Rest[pair.Key]) > 1e-6)
                    {
                        result.AnimatedBones++;
                    }

                    double error = XMatrix4.MaxAbsDifference(computed, pair.Value);
                    if (error > result.PoseMatrixError)
                    {
                        result.PoseMatrixError = error;
                        result.PoseWorstBone = pair.Key;
                    }

                    result.PosePositionError = Math.Max(result.PosePositionError, (computed.GetTranslation() - pair.Value.GetTranslation()).Length);
                    double dot = Math.Min(1.0, Math.Abs(XQuaternion.Dot(computed.GetRotation(), pair.Value.GetRotation())));
                    result.PoseRotationErrorDegrees = Math.Max(result.PoseRotationErrorDegrees, 2.0 * Math.Acos(dot) * 180.0 / Math.PI);

                    int parent = skeleton.Parents[index];
                    if (parent >= 0)
                    {
                        XMatrix4 computedLocal = BlenderSpace.BoneToBlender(world[parent]).InverseAffine() * computed;
                        XMatrix4 referenceLocal = frame.Pose[skeleton.Names[parent]].InverseAffine() * pair.Value;
                        result.LocalMatrixError = Math.Max(result.LocalMatrixError, XMatrix4.MaxAbsDifference(computedLocal, referenceLocal));
                    }
                }

                double sumSquares = 0.0;
                int count = 0;
                foreach (MeshMapping mapping in mappings)
                {
                    XVector3[] skinned = XRayPose.Skin(mapping.Visual, skeleton, world);
                    XVector3[] rest = XRayPose.Skin(mapping.Visual, skeleton, restWorld);
                    for (int v = 0; v < skinned.Length; v++)
                    {
                        int[] candidates = mapping.Candidates[v];
                        if (candidates == null)
                        {
                            continue;
                        }

                        XVector3 computed = BlenderSpace.PointToBlender(skinned[v]);
                        double best = double.MaxValue;
                        foreach (int candidate in candidates)
                        {
                            best = Math.Min(best, (computed - reference.GetVertex(frame.VertexOffset, mapping.Mesh, candidate)).Length);
                        }

                        sumSquares += best * best;
                        count++;
                        if (best > result.MaxVertexError)
                        {
                            result.MaxVertexError = best;
                            result.WorstMesh = mapping.Visual.Name;
                        }

                        result.MaxDisplacementFromRest = Math.Max(result.MaxDisplacementFromRest, (skinned[v] - rest[v]).Length);
                    }
                }

                result.RmsVertexError = count > 0 ? Math.Sqrt(sumSquares / count) : 0.0;
                report.Frames.Add(result);
                Line(report, $"  {frame.Action} f{frame.FrameIndex}: animated {result.AnimatedBones}, pose err {F(result.PoseMatrixError)} ('{result.PoseWorstBone}'), pos {F(result.PosePositionError)}, rot {result.PoseRotationErrorDegrees.ToString("0.00000", CultureInfo.InvariantCulture)} deg, local {F(result.LocalMatrixError)}, vertex max {F(result.MaxVertexError)} rms {F(result.RmsVertexError)} ({result.WorstMesh}), max displacement from rest {result.MaxDisplacementFromRest.ToString("0.0000", CultureInfo.InvariantCulture)}");
            }

            return report;
        }

        /// <summary>
        /// Compares externally deformed vertices (e.g. SkinnedMeshRenderer.BakeMesh output converted to native space,
        /// one array per mesh visual in OgfModel.GetMeshVisuals order) with one Blender reference frame.
        /// Returns the maximum per-vertex distance.
        /// </summary>
        public static double CompareDeformedVertices(OgfModel model, IList<XVector3[]> deformed, BlenderVertexReference reference, BlenderVertexReference.Frame frame, out int compared)
        {
            var scratch = new VertexVerificationReport();
            List<OgfVisual> visuals = model.GetMeshVisuals();
            double max = 0.0;
            compared = 0;
            for (int m = 0; m < visuals.Count; m++)
            {
                BlenderVertexReference.Mesh mesh = reference.Meshes.Find(x => x.Name == visuals[m].Name);
                if (mesh == null)
                {
                    throw new InvalidOperationException($"Reference has no mesh '{visuals[m].Name}'.");
                }

                MeshMapping mapping = Map(visuals[m], mesh, reference, scratch);
                for (int v = 0; v < visuals[m].VertexCount; v++)
                {
                    int[] candidates = mapping.Candidates[v];
                    if (candidates == null)
                    {
                        continue;
                    }

                    XVector3 computed = BlenderSpace.PointToBlender(deformed[m][v]);
                    double best = double.MaxValue;
                    foreach (int candidate in candidates)
                    {
                        best = Math.Min(best, (computed - reference.GetVertex(frame.VertexOffset, mesh, candidate)).Length);
                    }

                    max = Math.Max(max, best);
                    compared++;
                }
            }

            return max;
        }

        private static MeshMapping Map(OgfVisual visual, BlenderVertexReference.Mesh mesh, BlenderVertexReference reference, VertexVerificationReport report)
        {
            var grid = new Dictionary<long, List<int>>();
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                XVector3 p = reference.GetVertex(reference.RestVertexOffset, mesh, i);
                long key = Key(Cell(p.X), Cell(p.Y), Cell(p.Z));
                if (!grid.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>();
                    grid.Add(key, list);
                }

                list.Add(i);
            }

            var mapping = new MeshMapping { Visual = visual, Mesh = mesh, Candidates = new int[visual.VertexCount][] };
            for (int v = 0; v < visual.VertexCount; v++)
            {
                XVector3 p = BlenderSpace.PointToBlender(visual.Positions[v]);
                long cx = Cell(p.X);
                long cy = Cell(p.Y);
                long cz = Cell(p.Z);
                double best = double.MaxValue;
                var found = new List<KeyValuePair<int, double>>();
                for (long dx = -1; dx <= 1; dx++)
                {
                    for (long dy = -1; dy <= 1; dy++)
                    {
                        for (long dz = -1; dz <= 1; dz++)
                        {
                            if (!grid.TryGetValue(Key(cx + dx, cy + dy, cz + dz), out List<int> list))
                            {
                                continue;
                            }

                            foreach (int index in list)
                            {
                                double distance = (reference.GetVertex(reference.RestVertexOffset, mesh, index) - p).Length;
                                if (distance <= MATCH_TOLERANCE)
                                {
                                    found.Add(new KeyValuePair<int, double>(index, distance));
                                    best = Math.Min(best, distance);
                                }
                            }
                        }
                    }
                }

                if (found.Count == 0)
                {
                    report.UnmatchedVertices++;
                    continue;
                }

                var candidates = new List<int>();
                foreach (KeyValuePair<int, double> pair in found)
                {
                    if (pair.Value <= best + 1e-6)
                    {
                        candidates.Add(pair.Key);
                    }
                }

                if (candidates.Count > 1)
                {
                    report.AmbiguousVertices++;
                }

                report.MatchedVertices++;
                report.RestVertexError = Math.Max(report.RestVertexError, best);
                mapping.Candidates[v] = candidates.ToArray();
            }

            return mapping;
        }

        private static long Cell(double value)
        {
            return (long)Math.Floor(value / CELL);
        }

        private static long Key(long x, long y, long z)
        {
            return ((x & 0x1FFFFF) << 42) | ((y & 0x1FFFFF) << 21) | (z & 0x1FFFFF);
        }

        private static string F(double value)
        {
            return value.ToString("0.000E+0", CultureInfo.InvariantCulture);
        }

        private static void Line(VertexVerificationReport report, string text)
        {
            report.Log.AppendLine(text);
        }

        private sealed class MeshMapping
        {
            public OgfVisual Visual;
            public BlenderVertexReference.Mesh Mesh;
            public int[][] Candidates;
        }
    }
}
