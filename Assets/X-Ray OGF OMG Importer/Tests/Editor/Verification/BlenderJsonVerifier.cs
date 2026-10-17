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
    public sealed class MotionSource
    {
        public MotionSource(string label, OmfData data)
        {
            Label = label;
            Data = data;
        }

        public string Label { get; }

        public OmfData Data { get; }
    }

    public sealed class FrameVerification
    {
        public string Action;
        public int Frame;
        public string Source;
        public bool Skipped;
        public bool GeometryAlignedToConstrainedRoot;
        public bool UnsupportedConstraint;
        public List<string> ConstrainedBones = new List<string>();
        public double DirectPoseMatrixError;
        public int AnimatedBones;
        public double PoseMatrixError;
        public string PoseWorstBone;
        public double PosePositionError;
        public double PoseRotationErrorDegrees;
        public double LocalMatrixError;
        public double BoundsError;
        public double MeshSizeError;
        public string MeshSizeWorst;
        public XVector3 ComputedSize;
        public XVector3 ReferenceSize;
    }

    public sealed class VerificationReport
    {
        public string Model;
        public int BoneCount;
        public int ReferenceBoneCount;
        public List<string> MissingBones = new List<string>();
        public double RestMatrixError;
        public string RestWorstBone;
        public double RestBoundsError;
        public double RestMeshSizeError;
        public List<FrameVerification> Frames = new List<FrameVerification>();
        public StringBuilder Log = new StringBuilder();

        public double MaxPoseMatrixError
        {
            get
            {
                double max = 0.0;
                foreach (FrameVerification frame in Frames)
                {
                    max = Math.Max(max, frame.PoseMatrixError);
                }

                return max;
            }
        }

        public double MaxBoundsError
        {
            get
            {
                double max = RestBoundsError;
                foreach (FrameVerification frame in Frames)
                {
                    max = Math.Max(max, Math.Max(frame.BoundsError, frame.MeshSizeError));
                }

                return max;
            }
        }
    }

    /// <summary>
    /// Compares the native-space importer math against Blender reference data:
    /// A) rest matrices, B) pose matrices (world and parent-relative, position and rotation separately),
    /// C) skinned geometry bounds per mesh and overall.
    /// </summary>
    public static class BlenderJsonVerifier
    {
        public static VerificationReport Verify(OgfModel model, IList<MotionSource> sources, BlenderReference reference)
        {
            var report = new VerificationReport { Model = reference.Armature };
            var skeleton = new XRaySkeleton(model.Bones);
            report.BoneCount = skeleton.Count;
            List<OgfVisual> meshes = model.GetMeshVisuals();

            ReferenceFrame anyFrame = null;
            foreach (ReferenceAction action in reference.Actions.Values)
            {
                if (action.Frames.Count > 0)
                {
                    anyFrame = action.Frames[0];
                    break;
                }
            }

            if (anyFrame == null)
            {
                throw new InvalidOperationException("Reference contains no frames.");
            }

            report.ReferenceBoneCount = anyFrame.Bones.Count;

            // A) rest pose
            foreach (ReferenceBone bone in anyFrame.Bones.Values)
            {
                int index = skeleton.IndexOf(bone.Name);
                if (index < 0)
                {
                    report.MissingBones.Add(bone.Name);
                    continue;
                }

                XMatrix4 computed = BlenderSpace.BoneToBlender(skeleton.RestWorld[index]);
                double error = XMatrix4.MaxAbsDifference(computed, bone.Rest);
                if (error > report.RestMatrixError)
                {
                    report.RestMatrixError = error;
                    report.RestWorstBone = bone.Name;
                }
            }

            Line(report, $"[{report.Model}] bones {skeleton.Count}, reference {report.ReferenceBoneCount}, missing {report.MissingBones.Count}");
            Line(report, $"  A) rest matrix max |element error| = {F(report.RestMatrixError)} (bone '{report.RestWorstBone}')");

            if (reference.Rest != null)
            {
                XMatrix4[] restWorld = XRayPose.EvaluateWorld(skeleton, null, 0);
                CompareMesh(meshes, skeleton, restWorld, XMatrix4.Identity, reference.Rest, out report.RestBoundsError, out report.RestMeshSizeError, out string worst, out XVector3 size);
                Line(report, $"  C) rest bounds error {F(report.RestBoundsError)}, mesh size error {F(report.RestMeshSizeError)} ({worst}), size {size}");
            }

            // B) + C) animated frames
            foreach (ReferenceAction action in reference.Actions.Values)
            {
                OmfMotion motion = FindMotion(action.Name, skeleton, sources, out string sourceLabel);
                foreach (ReferenceFrame frame in action.Frames)
                {
                    var result = new FrameVerification { Action = action.Name, Frame = frame.Frame, Source = sourceLabel };
                    if (motion == null)
                    {
                        // The dump evaluated an action of the other rig on this armature: Blender keeps whatever
                        // pose basis the previous action left, so the frame carries no information about this rig.
                        result.Skipped = true;
                        report.Frames.Add(result);
                        Line(report, $"  B) {action.Name} f{frame.Frame}: skipped, no motion of this rig has this name (Blender pose is stale state of a previous action)");
                        continue;
                    }

                    int key = Math.Min(frame.Frame, motion.FrameCount - 1);
                    XMatrix4[] world = XRayPose.EvaluateWorld(skeleton, motion, key);
                    Dictionary<string, XMatrix4> unconstrained = ComposeReferenceWithoutConstraints(skeleton, frame);

                    foreach (ReferenceBone bone in frame.Bones.Values)
                    {
                        int index = skeleton.IndexOf(bone.Name);
                        if (index < 0)
                        {
                            continue;
                        }

                        XMatrix4 expected = unconstrained[bone.Name];
                        if (XMatrix4.MaxAbsDifference(expected, bone.Rest) > 1e-6)
                        {
                            result.AnimatedBones++;
                        }

                        if (XMatrix4.MaxAbsDifference(expected, bone.Pose) > CONSTRAINT_TOLERANCE)
                        {
                            result.ConstrainedBones.Add(bone.Name);
                        }

                        XMatrix4 computed = BlenderSpace.BoneToBlender(world[index]);
                        double error = XMatrix4.MaxAbsDifference(computed, expected);
                        if (error > result.PoseMatrixError)
                        {
                            result.PoseMatrixError = error;
                            result.PoseWorstBone = bone.Name;
                        }

                        result.DirectPoseMatrixError = Math.Max(result.DirectPoseMatrixError, XMatrix4.MaxAbsDifference(computed, bone.Pose));
                        result.PosePositionError = Math.Max(result.PosePositionError, (computed.GetTranslation() - expected.GetTranslation()).Length);
                        result.PoseRotationErrorDegrees = Math.Max(result.PoseRotationErrorDegrees, RotationAngleDegrees(computed, expected));

                        int parent = skeleton.Parents[index];
                        if (parent >= 0 && frame.Bones.TryGetValue(skeleton.Names[parent], out ReferenceBone parentBone))
                        {
                            XMatrix4 computedParent = BlenderSpace.BoneToBlender(world[parent]);
                            XMatrix4 computedLocal = computedParent.InverseAffine() * computed;
                            XMatrix4 referenceLocal = parentBone.Pose.InverseAffine() * bone.Pose;
                            result.LocalMatrixError = Math.Max(result.LocalMatrixError, XMatrix4.MaxAbsDifference(computedLocal, referenceLocal));
                        }
                    }

                    // A constraint on a root bone (blender-xray HUD "dependency object") moves the whole rig rigidly.
                    // Geometry is then compared after applying the same rigid offset, taken from the reference itself.
                    XMatrix4 correction = XMatrix4.Identity;
                    int root = SingleRoot(skeleton);
                    if (result.ConstrainedBones.Count > 0)
                    {
                        if (root >= 0 && result.ConstrainedBones.Contains(skeleton.Names[root]))
                        {
                            string rootName = skeleton.Names[root];
                            correction = frame.Bones[rootName].Pose * unconstrained[rootName].InverseAffine();
                            result.GeometryAlignedToConstrainedRoot = true;
                        }
                        else
                        {
                            result.UnsupportedConstraint = true;
                        }
                    }

                    CompareMesh(meshes, skeleton, world, correction, frame.Mesh, out result.BoundsError, out result.MeshSizeError, out result.MeshSizeWorst, out result.ComputedSize);
                    result.ReferenceSize = frame.Mesh.Size;
                    report.Frames.Add(result);
                    string constraintNote = result.ConstrainedBones.Count > 0 ? $", Blender constraints on [{string.Join(", ", result.ConstrainedBones)}] (direct pose err {F(result.DirectPoseMatrixError)})" : string.Empty;
                    Line(report, $"  B) {action.Name} f{frame.Frame} [{sourceLabel}] animated {result.AnimatedBones}: pose err {F(result.PoseMatrixError)} ('{result.PoseWorstBone}'), pos {F(result.PosePositionError)}, rot {result.PoseRotationErrorDegrees.ToString("0.0000", CultureInfo.InvariantCulture)} deg, local {F(result.LocalMatrixError)}{constraintNote}");
                    Line(report, $"  C) {action.Name} f{frame.Frame}: bounds err {F(result.BoundsError)}, mesh size err {F(result.MeshSizeError)} ({result.MeshSizeWorst}), size {result.ComputedSize} vs {result.ReferenceSize}{(result.GeometryAlignedToConstrainedRoot ? ", aligned to constrained root" : string.Empty)}");
                }
            }

            return report;
        }

        private const double CONSTRAINT_TOLERANCE = 1e-4;

        /// <summary>
        /// Blender pose without constraints, rebuilt from the reference's own rest and basis matrices:
        /// P = P_parent * Rest_parent^-1 * Rest * Basis (Blender's armature evaluation).
        /// </summary>
        private static Dictionary<string, XMatrix4> ComposeReferenceWithoutConstraints(XRaySkeleton skeleton, ReferenceFrame frame)
        {
            var result = new Dictionary<string, XMatrix4>();
            for (int i = 0; i < skeleton.Count; i++)
            {
                Compose(i, skeleton, frame, result);
            }

            return result;
        }

        private static XMatrix4 Compose(int index, XRaySkeleton skeleton, ReferenceFrame frame, Dictionary<string, XMatrix4> result)
        {
            string name = skeleton.Names[index];
            if (result.TryGetValue(name, out XMatrix4 cached))
            {
                return cached;
            }

            ReferenceBone bone = frame.Bones[name];
            int parent = skeleton.Parents[index];
            XMatrix4 pose;
            if (parent >= 0)
            {
                ReferenceBone parentBone = frame.Bones[skeleton.Names[parent]];
                pose = Compose(parent, skeleton, frame, result) * parentBone.Rest.InverseAffine() * bone.Rest * bone.Basis;
            }
            else
            {
                pose = bone.Rest * bone.Basis;
            }

            result[name] = pose;
            return pose;
        }

        private static int SingleRoot(XRaySkeleton skeleton)
        {
            int root = -1;
            for (int i = 0; i < skeleton.Count; i++)
            {
                if (skeleton.Parents[i] < 0)
                {
                    if (root >= 0)
                    {
                        return -1;
                    }

                    root = i;
                }
            }

            return root;
        }

        /// <summary>
        /// Picks the motion Blender evaluated for an action name: the first source whose motion with that name
        /// animates bones of this skeleton. Actions that belong to another rig leave the pose at rest in Blender.
        /// </summary>
        public static OmfMotion FindMotion(string name, XRaySkeleton skeleton, IList<MotionSource> sources, out string label)
        {
            foreach (MotionSource source in sources)
            {
                OmfMotion motion = source.Data.FindMotion(name);
                if (motion == null)
                {
                    continue;
                }

                foreach (OmfBoneTrack track in motion.Tracks)
                {
                    if (skeleton.IndexOf(track.BoneName) >= 0)
                    {
                        label = source.Label;
                        return motion;
                    }
                }
            }

            label = "rest: no motion for this rig";
            return null;
        }

        /// <param name="blenderCorrection">Rigid transform applied in Blender armature space before comparing (identity if none).</param>
        private static void CompareMesh(List<OgfVisual> meshes, XRaySkeleton skeleton, XMatrix4[] world, XMatrix4 blenderCorrection, ReferenceMeshStats stats, out double boundsError, out double sizeError, out string worst, out XVector3 totalSize)
        {
            var min = new XVector3(double.MaxValue, double.MaxValue, double.MaxValue);
            var max = new XVector3(double.MinValue, double.MinValue, double.MinValue);
            sizeError = 0.0;
            worst = "-";
            foreach (OgfVisual mesh in meshes)
            {
                XVector3[] positions = XRayPose.Skin(mesh, skeleton, world);
                var meshMin = new XVector3(double.MaxValue, double.MaxValue, double.MaxValue);
                var meshMax = new XVector3(double.MinValue, double.MinValue, double.MinValue);
                foreach (XVector3 p in positions)
                {
                    XVector3 b = blenderCorrection.TransformPoint(BlenderSpace.PointToBlender(p));
                    var r = new XVector3(b.X, b.Z, -b.Y);
                    meshMin = XVector3.Min(meshMin, r);
                    meshMax = XVector3.Max(meshMax, r);
                }

                min = XVector3.Min(min, meshMin);
                max = XVector3.Max(max, meshMax);
                foreach (ReferenceMeshEntry entry in stats.Meshes)
                {
                    if (entry.ObjectName == mesh.Name)
                    {
                        double error = MaxComponent(meshMax - meshMin - entry.Size);
                        if (error > sizeError)
                        {
                            sizeError = error;
                            worst = mesh.Name;
                        }
                    }
                }
            }

            boundsError = Math.Max(MaxComponent(min - stats.Min), MaxComponent(max - stats.Max));
            totalSize = max - min;
        }

        /// <summary>Native (x, y, z) -> Blender (x, z, y) -> reference "unity" (x, z, -y) = (x, y, -z).</summary>
        public static XVector3 ToReferenceUnity(XVector3 native)
        {
            XVector3 blender = BlenderSpace.PointToBlender(native);
            return new XVector3(blender.X, blender.Z, -blender.Y);
        }

        private static double MaxComponent(XVector3 v)
        {
            return Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));
        }

        private static double RotationAngleDegrees(XMatrix4 a, XMatrix4 b)
        {
            XQuaternion qa = a.GetRotation();
            XQuaternion qb = b.GetRotation();
            double dot = Math.Min(1.0, Math.Abs(XQuaternion.Dot(qa, qb)));
            return 2.0 * Math.Acos(dot) * 180.0 / Math.PI;
        }

        private static string F(double value)
        {
            return value.ToString("0.000E+0", CultureInfo.InvariantCulture);
        }

        private static void Line(VerificationReport report, string text)
        {
            report.Log.AppendLine(text);
        }
    }
}
