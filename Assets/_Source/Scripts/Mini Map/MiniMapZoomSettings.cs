using UnityEngine;

internal readonly struct MiniMapZoomSettings
{
    public MiniMapZoomSettings(int step, Vector2Int minMaxZoom)
    {
        Step = Mathf.Max(1, step);
        MinZoom = Mathf.Min(minMaxZoom.x, minMaxZoom.y);
        MaxZoom = Mathf.Max(minMaxZoom.x, minMaxZoom.y);
        MaxLevel = Mathf.Max(0, Mathf.CeilToInt((MaxZoom - MinZoom) / (float)Step));
    }

    public int Step { get; }
    public int MinZoom { get; }
    public int MaxZoom { get; }
    public int MaxLevel { get; }

    public int ClampLevel(int zoomLevel) => Mathf.Clamp(zoomLevel, 0, MaxLevel);

    public int GetNearestLevel(float zoom)
    {
        float clampedZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);

        if (clampedZoom >= MaxZoom)
        {
            return MaxLevel;
        }

        return ClampLevel(Mathf.RoundToInt((clampedZoom - MinZoom) / Step));
    }

    public float GetZoom(int zoomLevel)
    {
        int clampedLevel = ClampLevel(zoomLevel);
        int zoom = MinZoom + clampedLevel * Step;

        return Mathf.Clamp(zoom, MinZoom, MaxZoom);
    }
}