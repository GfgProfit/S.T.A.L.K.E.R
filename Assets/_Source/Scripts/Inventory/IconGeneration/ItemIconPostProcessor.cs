using UnityEngine;

internal static class ItemIconPostProcessor
{
    private const byte VISIBLE_ALPHA_THRESHOLD = 8;

    public static Color32[] Process(Color32[] itemPixels, int width, int height, ItemIconPostProcessSettings settings)
    {
        Color32[] pixels = CreateBackgroundPixels(width, height, new(0, 0, 0, 0));
        TryApplyShadow(pixels, itemPixels, width, height, settings);
        CompositeSourceOver(pixels, itemPixels);
        TryApplyOutline(pixels, itemPixels, width, height, settings);
        return pixels;
    }

    private static Color32[] CreateBackgroundPixels(int width, int height, Color32 backgroundColor)
    {
        Color32[] pixels = new Color32[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }

        return pixels;
    }

    private static bool TryApplyShadow(Color32[] pixels, Color32[] sourcePixels, int width, int height, ItemIconPostProcessSettings settings)
    {
        if (settings.UseShadow == false)
        {
            return false;
        }

        Vector2Int offset = settings.ShadowOffset;
        int blurRadius = settings.ShadowBlur;
        Color shadowColor = settings.ShadowColor;
        int blurRadiusSquared = blurRadius * blurRadius;
        bool changed = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float shadowAlpha = GetShadowAlpha(sourcePixels, width, height, x - offset.x, y - offset.y, blurRadius, blurRadiusSquared);

                if (shadowAlpha <= 0f)
                {
                    continue;
                }

                int index = y * width + x;
                Color32 shadowPixel = new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * shadowAlpha);
                pixels[index] = CompositeOver(shadowPixel, pixels[index]);
                changed = true;
            }
        }

        return changed;
    }

    private static float GetShadowAlpha(Color32[] sourcePixels, int width, int height, int centerX, int centerY, int blurRadius, int blurRadiusSquared)
    {
        if (blurRadius <= 0)
        {
            if (centerX < 0 || centerY < 0 || centerX >= width || centerY >= height)
            {
                return 0f;
            }

            return sourcePixels[centerY * width + centerX].a / 255f;
        }

        float alpha = 0f;

        for (int offsetY = -blurRadius; offsetY <= blurRadius; offsetY++)
        {
            int sampleY = centerY + offsetY;

            if (sampleY < 0 || sampleY >= height)
            {
                continue;
            }

            for (int offsetX = -blurRadius; offsetX <= blurRadius; offsetX++)
            {
                int distanceSquared = offsetX * offsetX + offsetY * offsetY;
                if (distanceSquared > blurRadiusSquared)
                {
                    continue;
                }

                int sampleX = centerX + offsetX;

                if (sampleX < 0 || sampleX >= width)
                {
                    continue;
                }

                float sampleAlpha = sourcePixels[sampleY * width + sampleX].a / 255f;

                if (sampleAlpha <= 0f)
                {
                    continue;
                }

                float falloff = 1f - Mathf.Sqrt(distanceSquared) / (blurRadius + 1f);
                alpha = Mathf.Max(alpha, sampleAlpha * falloff);
            }
        }

        return alpha;
    }

    private static void CompositeSourceOver(Color32[] pixels, Color32[] sourcePixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            if (sourcePixels[i].a == 0)
            {
                continue;
            }

            pixels[i] = CompositeOver(sourcePixels[i], pixels[i]);
        }
    }

    private static Color32 CompositeOver(Color32 foreground, Color32 background)
    {
        float foregroundAlpha = foreground.a / 255f;
        float backgroundAlpha = background.a / 255f;
        float outputAlpha = foregroundAlpha + backgroundAlpha * (1f - foregroundAlpha);

        if (outputAlpha <= 0f)
        {
            return new Color32(0, 0, 0, 0);
        }

        byte r = (byte)Mathf.RoundToInt(((foreground.r / 255f) * foregroundAlpha + (background.r / 255f) * backgroundAlpha * (1f - foregroundAlpha)) / outputAlpha * 255f);
        byte g = (byte)Mathf.RoundToInt(((foreground.g / 255f) * foregroundAlpha + (background.g / 255f) * backgroundAlpha * (1f - foregroundAlpha)) / outputAlpha * 255f);
        byte b = (byte)Mathf.RoundToInt(((foreground.b / 255f) * foregroundAlpha + (background.b / 255f) * backgroundAlpha * (1f - foregroundAlpha)) / outputAlpha * 255f);
        byte a = (byte)Mathf.RoundToInt(outputAlpha * 255f);

        return new(r, g, b, a);
    }

    private static bool TryApplyOutline(Color32[] pixels, Color32[] sourcePixels, int width, int height, ItemIconPostProcessSettings settings)
    {
        if (settings.UseOutline == false)
        {
            return false;
        }

        int radius = settings.OutlineWidth;

        if (radius <= 0)
        {
            return false;
        }

        Color32 outlineColor = settings.OutlineColor;
        int radiusSquared = radius * radius;
        bool changed = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                if (sourcePixels[index].a > VISIBLE_ALPHA_THRESHOLD)
                {
                    continue;
                }

                bool touchesVisiblePixel = false;

                for (int offsetY = -radius; offsetY <= radius && touchesVisiblePixel == false; offsetY++)
                {
                    int sampleY = y + offsetY;

                    if (sampleY < 0 || sampleY >= height)
                    {
                        continue;
                    }

                    for (int offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
                        {
                            continue;
                        }

                        int sampleX = x + offsetX;

                        if (sampleX < 0 || sampleX >= width)
                        {
                            continue;
                        }

                        if (sourcePixels[sampleY * width + sampleX].a > VISIBLE_ALPHA_THRESHOLD)
                        {
                            touchesVisiblePixel = true;
                            break;
                        }
                    }
                }

                if (touchesVisiblePixel)
                {
                    pixels[index] = CompositeOver(outlineColor, pixels[index]);
                    changed = true;
                }
            }
        }

        return changed;
    }
}