using System;
using System.Collections.Generic;
using XRay.Importer.Core;

namespace XRay.Importer.Tests.Verification
{
    /// <summary>
    /// Per-vertex reference produced by Tools~/blender_export_reference.py (Blender 3.6 + blender-xray):
    /// JSON metadata with Blender armature-space bone matrices plus a float32 blob of evaluated
    /// (armature-deformed) vertex positions in Blender axes.
    /// </summary>
    public sealed class BlenderVertexReference
    {
        private float[] _floats;

        public string Ogf { get; private set; }

        public string Omf { get; private set; }

        public List<string> Bones { get; } = new List<string>();

        public Dictionary<string, XMatrix4> Rest { get; } = new Dictionary<string, XMatrix4>();

        public List<Mesh> Meshes { get; } = new List<Mesh>();

        public List<Frame> Frames { get; } = new List<Frame>();

        public int RestVertexOffset { get; private set; }

        public int TotalVertexCount { get; private set; }

        public static BlenderVertexReference Load(string json, byte[] blob)
        {
            if (blob.Length % 4 != 0)
            {
                throw new FormatException("Reference vertex blob length is not a multiple of 4.");
            }

            var root = (Dictionary<string, object>)MiniJson.Parse(json);
            var reference = new BlenderVertexReference
            {
                Ogf = (string)root["ogf"],
                Omf = (string)root["omf"],
                RestVertexOffset = (int)(double)root["rest_vertex_offset"],
                _floats = new float[blob.Length / 4]
            };

            for (int i = 0; i < reference._floats.Length; i++)
            {
                reference._floats[i] = BitConverter.ToSingle(blob, i * 4);
            }

            foreach (object bone in (List<object>)root["bones"])
            {
                reference.Bones.Add((string)bone);
            }

            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)root["rest"])
            {
                reference.Rest[pair.Key] = ToMatrix(pair.Value);
            }

            foreach (object meshObject in (List<object>)root["meshes"])
            {
                var mesh = (Dictionary<string, object>)meshObject;
                var entry = new Mesh
                {
                    Name = (string)mesh["name"],
                    VertexCount = (int)(double)mesh["vertex_count"],
                    Offset = (int)(double)mesh["offset"]
                };
                reference.Meshes.Add(entry);
                reference.TotalVertexCount += entry.VertexCount;
            }

            foreach (object frameObject in (List<object>)root["frames"])
            {
                var source = (Dictionary<string, object>)frameObject;
                var frame = new Frame
                {
                    Action = (string)source["action"],
                    FrameIndex = (int)(double)source["frame"],
                    VertexOffset = (int)(double)source["vertex_offset"]
                };

                foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)source["pose"])
                {
                    frame.Pose[pair.Key] = ToMatrix(pair.Value);
                }

                reference.Frames.Add(frame);
            }

            return reference;
        }

        /// <summary>Vertex position in Blender axes. offset = frame or rest offset, mesh offset is added here.</summary>
        public XVector3 GetVertex(int baseOffset, Mesh mesh, int vertex)
        {
            int index = baseOffset + mesh.Offset + vertex * 3;
            return new XVector3(_floats[index], _floats[index + 1], _floats[index + 2]);
        }

        private static XMatrix4 ToMatrix(object value)
        {
            var list = (List<object>)value;
            var data = new double[16];
            for (int i = 0; i < 16; i++)
            {
                data[i] = (double)list[i];
            }

            return XMatrix4.FromArray(data);
        }

        public sealed class Mesh
        {
            public string Name;
            public int VertexCount;
            public int Offset;
        }

        public sealed class Frame
        {
            public string Action;
            public int FrameIndex;
            public int VertexOffset;
            public Dictionary<string, XMatrix4> Pose = new Dictionary<string, XMatrix4>();
        }
    }
}
