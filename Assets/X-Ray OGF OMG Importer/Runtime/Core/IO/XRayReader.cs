using System;

namespace XRay.Importer.Core.IO
{
    /// <summary>
    /// Little-endian sequential reader over a byte range. Every read is bounds-checked and
    /// reports the failing offset, so truncated files produce actionable errors.
    /// </summary>
    public sealed class XRayReader
    {
        private readonly byte[] _data;
        private readonly int _start;
        private readonly int _end;
        private readonly string _context;
        private int _position;

        public XRayReader(byte[] data, string context) : this(data, 0, data.Length, context)
        {
        }

        public XRayReader(byte[] data, int offset, int length, string context)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || length < 0 || offset + length > data.Length)
            {
                throw new XRayFormatException($"{context}: invalid data range (offset {offset}, length {length}, buffer {data.Length}).");
            }

            _data = data;
            _start = offset;
            _end = offset + length;
            _position = offset;
            _context = context;
        }

        public int Length => _end - _start;

        public int Position => _position - _start;

        public int Remaining => _end - _position;

        public bool IsEnd => _position >= _end;

        public string Context => _context;

        public byte[] Buffer => _data;

        public int AbsolutePosition => _position;

        public void Skip(int count)
        {
            Require(count);
            _position += count;
        }

        public byte ReadByte()
        {
            Require(1);
            return _data[_position++];
        }

        public sbyte ReadSByte()
        {
            Require(1);
            return unchecked((sbyte)_data[_position++]);
        }

        public ushort ReadUInt16()
        {
            Require(2);
            int value = _data[_position] | (_data[_position + 1] << 8);
            _position += 2;
            return (ushort)value;
        }

        public short ReadInt16()
        {
            return unchecked((short)ReadUInt16());
        }

        public uint ReadUInt32()
        {
            Require(4);
            uint value = (uint)(_data[_position] | (_data[_position + 1] << 8) | (_data[_position + 2] << 16) | (_data[_position + 3] << 24));
            _position += 4;
            return value;
        }

        public int ReadInt32()
        {
            return unchecked((int)ReadUInt32());
        }

        public float ReadSingle()
        {
            Require(4);
            float value = BitConverter.ToSingle(_data, _position);
            if (!BitConverter.IsLittleEndian)
            {
                byte[] bytes = { _data[_position + 3], _data[_position + 2], _data[_position + 1], _data[_position] };
                value = BitConverter.ToSingle(bytes, 0);
            }

            _position += 4;
            return value;
        }

        public XVector3 ReadVector3()
        {
            double x = ReadSingle();
            double y = ReadSingle();
            double z = ReadSingle();
            return new XVector3(x, y, z);
        }

        /// <summary>Reads a zero-terminated cp1251 string. Accepts an unterminated string at the end of the range.</summary>
        public string ReadStringZ()
        {
            int begin = _position;
            int cursor = _position;
            while (cursor < _end && _data[cursor] != 0)
            {
                cursor++;
            }

            string value = Cp1251.Decode(_data, begin, cursor - begin);
            _position = cursor < _end ? cursor + 1 : cursor;
            return value;
        }

        /// <summary>Reads a line terminated by '\n' (optional '\r' is stripped). Used by motion marks.</summary>
        public string ReadStringLine()
        {
            int begin = _position;
            int cursor = _position;
            while (cursor < _end && _data[cursor] != 0x0A)
            {
                cursor++;
            }

            int count = cursor - begin;
            if (count > 0 && _data[begin + count - 1] == 0x0D)
            {
                count--;
            }

            string value = Cp1251.Decode(_data, begin, count);
            _position = cursor < _end ? cursor + 1 : cursor;
            return value;
        }

        public XRayReader Slice(int length, string context)
        {
            Require(length);
            var reader = new XRayReader(_data, _position, length, context);
            _position += length;
            return reader;
        }

        private void Require(int count)
        {
            if (count < 0 || _position + count > _end)
            {
                throw new XRayFormatException($"{_context}: unexpected end of data at offset {_position - _start} (need {count} bytes, {_end - _position} left).");
            }
        }
    }
}
