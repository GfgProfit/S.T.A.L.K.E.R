using System.Collections.Generic;

namespace XRay.Importer.Core.IO
{
    public readonly struct XRayChunk
    {
        public XRayChunk(uint id, byte[] buffer, int offset, int length)
        {
            Id = id;
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }

        public uint Id { get; }

        public byte[] Buffer { get; }

        public int Offset { get; }

        public int Length { get; }

        public XRayReader CreateReader(string context)
        {
            return new XRayReader(Buffer, Offset, Length, context);
        }
    }

    /// <summary>
    /// X-Ray chunk stream: [uint32 id][uint32 size][size bytes]...
    /// The high bit of the id marks LZHUF compression. Compressed chunks are reported as unsupported:
    /// the reference LZHUF code is licensed for non-commercial use only, and game OGF/OMF files do not use it.
    /// </summary>
    public static class XRayChunks
    {
        public const uint COMPRESSED_MASK = 0x80000000u;

        public static List<XRayChunk> ReadAll(XRayReader reader)
        {
            var chunks = new List<XRayChunk>();
            while (!reader.IsEnd)
            {
                if (reader.Remaining < 8)
                {
                    // Fewer than 8 bytes cannot hold a chunk header; some X-Ray tools leave such padding after the last chunk.
                    break;
                }

                uint id = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                if (size > (uint)reader.Remaining)
                {
                    throw new XRayFormatException($"{reader.Context}: chunk 0x{id:X} declares {size} bytes, only {reader.Remaining} left.");
                }

                if ((id & COMPRESSED_MASK) != 0)
                {
                    throw new XRayFormatException($"{reader.Context}: chunk 0x{id & ~COMPRESSED_MASK:X} is LZHUF-compressed. Compressed chunks are not supported; re-save the file uncompressed.");
                }

                chunks.Add(new XRayChunk(id, reader.Buffer, reader.AbsolutePosition, (int)size));
                reader.Skip((int)size);
            }

            return chunks;
        }

        public static Dictionary<uint, XRayChunk> ReadMap(XRayReader reader)
        {
            var map = new Dictionary<uint, XRayChunk>();
            foreach (XRayChunk chunk in ReadAll(reader))
            {
                // Matches blender-xray: later duplicates override earlier ones.
                map[chunk.Id] = chunk;
            }

            return map;
        }
    }
}
