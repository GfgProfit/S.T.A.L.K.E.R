using System;
using System.Collections.Generic;
using XRay.Importer.Core;

namespace XRay.Importer.Tests.Verification
{
    /// <summary>
    /// Reference data exported from Blender 3.6 + blender-xray (xray_blender_reference_*.json).
    /// Bone matrices are Blender armature-space matrices; mesh bounds are Blender positions mapped
    /// with (x, z, -y), which equals native X-Ray (x, y, -z).
    /// </summary>
    public sealed class BlenderReference
    {
        public string Armature { get; private set; }

        public List<string> MeshObjectNames { get; } = new List<string>();

        public ReferenceMeshStats Rest { get; private set; }

        public Dictionary<string, ReferenceAction> Actions { get; } = new Dictionary<string, ReferenceAction>();

        public static BlenderReference Parse(string json)
        {
            var root = (Dictionary<string, object>)MiniJson.Parse(json);
            var reference = new BlenderReference { Armature = (string)root["armature"] };
            foreach (object name in (List<object>)root["mesh_object_names"])
            {
                reference.MeshObjectNames.Add((string)name);
            }

            if (root.TryGetValue("rest", out object rest))
            {
                reference.Rest = ReferenceMeshStats.Parse((Dictionary<string, object>)rest);
            }

            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)root["actions"])
            {
                var source = (Dictionary<string, object>)pair.Value;
                var action = new ReferenceAction { Name = pair.Key };
                var range = (List<object>)source["frame_range"];
                action.FrameStart = (double)range[0];
                action.FrameEnd = (double)range[1];
                foreach (object frameObject in (List<object>)source["frames"])
                {
                    var frameSource = (Dictionary<string, object>)frameObject;
                    var frame = new ReferenceFrame
                    {
                        Frame = (int)Math.Round((double)frameSource["frame"]),
                        Mesh = ReferenceMeshStats.Parse((Dictionary<string, object>)frameSource["mesh"])
                    };

                    foreach (KeyValuePair<string, object> bonePair in (Dictionary<string, object>)frameSource["bones"])
                    {
                        var boneSource = (Dictionary<string, object>)bonePair.Value;
                        frame.Bones[bonePair.Key] = new ReferenceBone
                        {
                            Name = bonePair.Key,
                            Parent = boneSource.TryGetValue("parent", out object parent) ? parent as string : null,
                            Rest = ParseMatrix(boneSource["rest_matrix_armature_blender"]),
                            Pose = ParseMatrix(boneSource["pose_matrix_armature_blender"]),
                            Basis = ParseMatrix(boneSource["pose_basis_blender"])
                        };
                    }

                    action.Frames.Add(frame);
                }

                reference.Actions[pair.Key] = action;
            }

            return reference;
        }

        internal static XVector3 ParseVector(object value)
        {
            var list = (List<object>)value;
            return new XVector3((double)list[0], (double)list[1], (double)list[2]);
        }

        private static XMatrix4 ParseMatrix(object value)
        {
            var rows = (List<object>)value;
            var data = new double[16];
            for (int r = 0; r < 4; r++)
            {
                var row = (List<object>)rows[r];
                for (int c = 0; c < 4; c++)
                {
                    data[r * 4 + c] = (double)row[c];
                }
            }

            return XMatrix4.FromArray(data);
        }
    }

    public sealed class ReferenceMeshStats
    {
        public int Vertices { get; private set; }

        public XVector3 Min { get; private set; }

        public XVector3 Max { get; private set; }

        public XVector3 Size { get; private set; }

        public List<ReferenceMeshEntry> Meshes { get; } = new List<ReferenceMeshEntry>();

        internal static ReferenceMeshStats Parse(Dictionary<string, object> source)
        {
            var stats = new ReferenceMeshStats
            {
                Vertices = (int)(double)source["vertices"],
                Min = BlenderReference.ParseVector(source["bounds_min_unity"]),
                Max = BlenderReference.ParseVector(source["bounds_max_unity"]),
                Size = BlenderReference.ParseVector(source["bounds_size_unity"])
            };

            foreach (object meshObject in (List<object>)source["meshes"])
            {
                var mesh = (Dictionary<string, object>)meshObject;
                stats.Meshes.Add(new ReferenceMeshEntry
                {
                    ObjectName = (string)mesh["object"],
                    Vertices = (int)(double)mesh["vertices"],
                    Size = BlenderReference.ParseVector(mesh["bounds_size_unity"])
                });
            }

            return stats;
        }
    }

    public sealed class ReferenceMeshEntry
    {
        public string ObjectName { get; internal set; }

        public int Vertices { get; internal set; }

        public XVector3 Size { get; internal set; }
    }

    public sealed class ReferenceAction
    {
        public string Name { get; internal set; }

        public double FrameStart { get; internal set; }

        public double FrameEnd { get; internal set; }

        public List<ReferenceFrame> Frames { get; } = new List<ReferenceFrame>();
    }

    public sealed class ReferenceFrame
    {
        public int Frame { get; internal set; }

        public ReferenceMeshStats Mesh { get; internal set; }

        public Dictionary<string, ReferenceBone> Bones { get; } = new Dictionary<string, ReferenceBone>();
    }

    public sealed class ReferenceBone
    {
        public string Name { get; internal set; }

        public string Parent { get; internal set; }

        public XMatrix4 Rest { get; internal set; }

        public XMatrix4 Pose { get; internal set; }

        public XMatrix4 Basis { get; internal set; }
    }
}
