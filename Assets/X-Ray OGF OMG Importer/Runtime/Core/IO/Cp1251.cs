using System.Text;

namespace XRay.Importer.Core.IO
{
    /// <summary>
    /// Windows-1251 decoder. X-Ray stores names in cp1251; System.Text.Encoding.GetEncoding(1251)
    /// is not guaranteed to exist on every Unity scripting backend, so the table is embedded.
    /// </summary>
    public static class Cp1251
    {
        private static readonly char[] HIGH_TABLE =
        {
            'Ђ', 'Ѓ', '‚', 'ѓ', '„', '…', '†', '‡', '€', '‰', 'Љ', '‹', 'Њ', 'Ќ', 'Ћ', 'Џ',
            'ђ', '‘', '’', '“', '”', '•', '–', '—', '�', '™', 'љ', '›', 'њ', 'ќ', 'ћ', 'џ',
            ' ', 'Ў', 'ў', 'Ј', '¤', 'Ґ', '¦', '§', 'Ё', '©', 'Є', '«', '¬', '­', '®', 'Ї',
            '°', '±', 'І', 'і', 'ґ', 'µ', '¶', '·', 'ё', '№', 'є', '»', 'ј', 'Ѕ', 'ѕ', 'ї'
        };

        public static string Decode(byte[] data, int offset, int count)
        {
            var builder = new StringBuilder(count);
            for (int i = 0; i < count; i++)
            {
                byte value = data[offset + i];
                if (value < 0x80)
                {
                    builder.Append((char)value);
                }
                else if (value < 0xC0)
                {
                    builder.Append(HIGH_TABLE[value - 0x80]);
                }
                else
                {
                    // 0xC0..0xFF map linearly onto U+0410..U+044F.
                    builder.Append((char)(0x0410 + (value - 0xC0)));
                }
            }

            return builder.ToString();
        }
    }
}
