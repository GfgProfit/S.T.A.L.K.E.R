using UnityEngine;

internal sealed class RawIconRenderResult
{
    public RawIconRenderResult(Texture2D texture, Color32[] itemPixels, int width, int height)
    {
        Texture = texture;
        ItemPixels = itemPixels;
        Width = width;
        Height = height;
    }

    public Texture2D Texture { get; }
    public Color32[] ItemPixels { get; }
    public int Width { get; }
    public int Height { get; }
}
