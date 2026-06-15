using System;

internal readonly struct IconCacheKey : IEquatable<IconCacheKey>
{
    private readonly int _itemId;
    private readonly int _iconHash;
    private readonly int _generatorHash;

    public IconCacheKey(int itemId, int iconHash, int generatorHash)
    {
        _itemId = itemId;
        _iconHash = iconHash;
        _generatorHash = generatorHash;
    }

    public bool Equals(IconCacheKey other) => _itemId == other._itemId && _iconHash == other._iconHash && _generatorHash == other._generatorHash;
    public override bool Equals(object obj) => obj is IconCacheKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_itemId, _iconHash, _generatorHash);
}