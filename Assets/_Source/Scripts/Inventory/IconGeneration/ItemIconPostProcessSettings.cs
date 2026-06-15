using UnityEngine;

internal readonly struct ItemIconPostProcessSettings
{
    public readonly bool UseShadow;
    public readonly Color ShadowColor;
    public readonly Vector2Int ShadowOffset;
    public readonly int ShadowBlur;
    public readonly bool UseOutline;
    public readonly Color32 OutlineColor;
    public readonly int OutlineWidth;

    public ItemIconPostProcessSettings(bool useShadow, Color shadowColor, Vector2Int shadowOffset, int shadowBlur, bool useOutline, Color outlineColor, int outlineWidth)
    {
        UseShadow = useShadow;
        ShadowColor = shadowColor;
        ShadowOffset = shadowOffset;
        ShadowBlur = shadowBlur;
        UseOutline = useOutline;
        OutlineColor = outlineColor;
        OutlineWidth = outlineWidth;
    }
}