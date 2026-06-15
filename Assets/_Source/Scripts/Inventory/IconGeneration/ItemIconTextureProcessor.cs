using UnityEngine;

internal static class ItemIconTextureProcessor
{
    private const byte VISIBLE_ALPHA_THRESHOLD = 8;

    public static bool HasVisiblePixels(Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > VISIBLE_ALPHA_THRESHOLD)
            {
                return true;
            }
        }

        return false;
    }

    public static Color32[] CreateProcessedPixels(Color32[] itemPixels, int width, int height, ItemIconPostProcessSettings settings) => ItemIconPostProcessor.Process(itemPixels, width, height, settings);
    public static ItemIconPostProcessSettings CreatePostProcessSettings(ItemData itemData) => new(itemData.IconUseShadow, itemData.IconShadowColor, itemData.IconShadowTextureOffset, itemData.IconShadowTextureBlur, itemData.IconUseOutline, itemData.IconOutlineColor, itemData.IconOutlineTextureWidth);
    
    public static Sprite CreateSprite(ItemData itemData, Texture2D texture)
    {
        Sprite sprite = Sprite.Create(texture, new(0f, 0f, texture.width, texture.height), new(0.5f, 0.5f), itemData.IconSpritePixelsPerUnit);

        sprite.hideFlags = HideFlags.HideAndDontSave;
        sprite.name = $"{itemData.name} Runtime Icon";

        return sprite;
    }
}