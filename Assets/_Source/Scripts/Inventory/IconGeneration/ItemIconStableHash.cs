using System;
using UnityEngine;

internal static class ItemIconStableHash
{
    private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
    private const ulong FNV_PRIME = 1099511628211UL;

    public static ulong Begin() => FNV_OFFSET_BASIS;

    public static ulong Add(ulong hash, bool value) => Add(hash, value ? 1 : 0);
    public static ulong Add(ulong hash, int value) => Add(hash, unchecked((uint)value));
    public static ulong Add(ulong hash, uint value)
    {
        for (int shift = 0; shift < 32; shift += 8)
        {
            hash ^= (byte)(value >> shift);
            hash *= FNV_PRIME;
        }

        return hash;
    }

    public static ulong Add(ulong hash, ulong value)
    {
        for (int shift = 0; shift < 64; shift += 8)
        {
            hash ^= (byte)(value >> shift);
            hash *= FNV_PRIME;
        }

        return hash;
    }

    public static ulong Add(ulong hash, float value) => Add(hash, Mathf.RoundToInt(value * 1000f));
    public static ulong Add(ulong hash, Vector2 value) => Add(Add(hash, value.x), value.y);
    public static ulong Add(ulong hash, Vector3 value) => Add(Add(Add(hash, value.x), value.y), value.z);
    public static ulong Add(ulong hash, Color value) => Add(Add(Add(Add(hash, value.r), value.g), value.b), value.a);

    public static ulong Add(ulong hash, string value)
    {
        value ??= string.Empty;

        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            hash ^= (byte)character;
            hash *= FNV_PRIME;
            hash ^= (byte)(character >> 8);
            hash *= FNV_PRIME;
        }

        hash ^= 0xff;
        hash *= FNV_PRIME;
        return hash;
    }

    public static ulong BuildKeyHash(
        string itemId,
        string moduleItemIds,
        int width,
        int height,
        ItemIconProfileType profileType,
        ulong visualSignature)
    {
        ulong hash = Begin();
        hash = Add(hash, itemId);
        hash = Add(hash, moduleItemIds);
        hash = Add(hash, width);
        hash = Add(hash, height);
        hash = Add(hash, (int)profileType);
        return Add(hash, visualSignature);
    }

    public static string ToHex(ulong value) => value.ToString("x16");

    public static bool TryParseHex(string value, out ulong result)
    {
        return ulong.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out result);
    }
}
