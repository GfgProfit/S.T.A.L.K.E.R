using UnityEngine;

internal sealed class IconCacheEntry
{
    private IconCacheEntry(Sprite sprite, Texture2D texture, bool ownsSprite, bool ownsTexture)
    {
        Sprite = sprite;
        Texture = texture;
        OwnsSprite = ownsSprite;
        OwnsTexture = ownsTexture;
    }

    public Sprite Sprite { get; }
    public Texture2D Texture { get; }
    public bool OwnsSprite { get; }
    public bool OwnsTexture { get; }

    public static IconCacheEntry CreateGenerated(Sprite sprite, Texture2D texture) => new(sprite, texture, true, true);
    public static IconCacheEntry CreateFallback(Sprite sprite) => new(sprite, null, false, false);
    public static IconCacheEntry CreateExternal(Sprite sprite) => new(sprite, null, false, false);
}
