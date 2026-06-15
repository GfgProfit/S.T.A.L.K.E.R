using UnityEngine;
using UnityEngine.UI;

internal static class ItemGridVisualBuilder
{
    public static RectTransform CreateCellGrid(RectTransform parent, int width, int height, Vector2 size, GameProjectSettings visualSettings)
    {
        if (parent == null || visualSettings == null || visualSettings.ShowCellGrid == false)
        {
            return null;
        }

        GameObject rootObject = new("Cell Grid");
        rootObject.transform.SetParent(parent, false);

        RectTransform cellGridRoot = rootObject.AddComponent<RectTransform>();
        cellGridRoot.anchorMin = new(0f, 1f);
        cellGridRoot.anchorMax = new(0f, 1f);
        cellGridRoot.pivot = new(0f, 1f);
        cellGridRoot.anchoredPosition = Vector2.zero;
        cellGridRoot.sizeDelta = size;
        cellGridRoot.SetAsFirstSibling();

        int firstVerticalLine = visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastVerticalLine = visualSettings.ShowCellGridBorder ? width : width - 1;
        int firstHorizontalLine = visualSettings.ShowCellGridBorder ? 0 : 1;
        int lastHorizontalLine = visualSettings.ShowCellGridBorder ? height : height - 1;

        for (int x = firstVerticalLine; x <= lastVerticalLine; x++)
        {
            bool isBorderLine = x == 0 || x == width;

            CreateGridLine(cellGridRoot, $"Vertical Grid Line {x}", new(x * ItemGrid.TILE_SIZE_WIDTH, 0f), new(visualSettings.GetLineThickness(isBorderLine), size.y), new(0.5f, 1f), visualSettings.GetLineColor(isBorderLine));
        }

        for (int y = firstHorizontalLine; y <= lastHorizontalLine; y++)
        {
            bool isBorderLine = y == 0 || y == height;

            CreateGridLine(cellGridRoot, $"Horizontal Grid Line {y}", new(0f, -y * ItemGrid.TILE_SIZE_HEIGHT), new(size.x, visualSettings.GetLineThickness(isBorderLine)), new(0f, 0.5f), visualSettings.GetLineColor(isBorderLine));
        }

        return cellGridRoot;
    }

    private static void CreateGridLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 pivot, Color color)
    {
        GameObject lineObject = new(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform lineRectTransform = lineObject.AddComponent<RectTransform>();
        lineObject.AddComponent<CanvasRenderer>();
        lineRectTransform.anchorMin = new(0f, 1f);
        lineRectTransform.anchorMax = new(0f, 1f);
        lineRectTransform.pivot = pivot;
        lineRectTransform.anchoredPosition = anchoredPosition;
        lineRectTransform.sizeDelta = size;

        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }
}