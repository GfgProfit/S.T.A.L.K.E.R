using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace XRay.Importer.Tests.Verification
{
    /// <summary>
    /// Minimal JSON reader for the Blender reference dumps. Objects become Dictionary&lt;string, object&gt;,
    /// arrays List&lt;object&gt;, numbers double. Kept free of UnityEngine so the same verification code
    /// runs inside the Unity Test Runner and in a plain .NET console harness.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            int position = 0;
            object value = ParseValue(text, ref position);
            SkipWhitespace(text, ref position);
            if (position != text.Length)
            {
                throw new FormatException($"Unexpected trailing JSON content at {position}.");
            }

            return value;
        }

        private static object ParseValue(string text, ref int position)
        {
            SkipWhitespace(text, ref position);
            if (position >= text.Length)
            {
                throw new FormatException("Unexpected end of JSON.");
            }

            char c = text[position];
            switch (c)
            {
                case '{':
                    return ParseObject(text, ref position);
                case '[':
                    return ParseArray(text, ref position);
                case '"':
                    return ParseString(text, ref position);
                case 't':
                    Expect(text, ref position, "true");
                    return true;
                case 'f':
                    Expect(text, ref position, "false");
                    return false;
                case 'n':
                    Expect(text, ref position, "null");
                    return null;
                default:
                    return ParseNumber(text, ref position);
            }
        }

        private static Dictionary<string, object> ParseObject(string text, ref int position)
        {
            var result = new Dictionary<string, object>();
            position++;
            SkipWhitespace(text, ref position);
            if (text[position] == '}')
            {
                position++;
                return result;
            }

            while (true)
            {
                SkipWhitespace(text, ref position);
                string key = ParseString(text, ref position);
                SkipWhitespace(text, ref position);
                if (text[position] != ':')
                {
                    throw new FormatException($"Expected ':' at {position}.");
                }

                position++;
                result[key] = ParseValue(text, ref position);
                SkipWhitespace(text, ref position);
                if (text[position] == ',')
                {
                    position++;
                    continue;
                }

                if (text[position] == '}')
                {
                    position++;
                    return result;
                }

                throw new FormatException($"Expected ',' or '}}' at {position}.");
            }
        }

        private static List<object> ParseArray(string text, ref int position)
        {
            var result = new List<object>();
            position++;
            SkipWhitespace(text, ref position);
            if (text[position] == ']')
            {
                position++;
                return result;
            }

            while (true)
            {
                result.Add(ParseValue(text, ref position));
                SkipWhitespace(text, ref position);
                if (text[position] == ',')
                {
                    position++;
                    continue;
                }

                if (text[position] == ']')
                {
                    position++;
                    return result;
                }

                throw new FormatException($"Expected ',' or ']' at {position}.");
            }
        }

        private static string ParseString(string text, ref int position)
        {
            if (text[position] != '"')
            {
                throw new FormatException($"Expected string at {position}.");
            }

            position++;
            var builder = new StringBuilder();
            while (position < text.Length)
            {
                char c = text[position++];
                if (c == '"')
                {
                    return builder.ToString();
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                char escape = text[position++];
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        builder.Append((char)int.Parse(text.Substring(position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        position += 4;
                        break;
                    default:
                        throw new FormatException($"Invalid escape '\\{escape}' at {position}.");
                }
            }

            throw new FormatException("Unterminated JSON string.");
        }

        private static double ParseNumber(string text, ref int position)
        {
            int start = position;
            while (position < text.Length && "+-0123456789.eE".IndexOf(text[position]) >= 0)
            {
                position++;
            }

            if (start == position)
            {
                throw new FormatException($"Unexpected character '{text[position]}' at {position}.");
            }

            return double.Parse(text.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static void Expect(string text, ref int position, string literal)
        {
            if (string.CompareOrdinal(text, position, literal, 0, literal.Length) != 0)
            {
                throw new FormatException($"Expected '{literal}' at {position}.");
            }

            position += literal.Length;
        }

        private static void SkipWhitespace(string text, ref int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
        }
    }
}
