using UnityEngine;
using UnityEngine.UI;

internal static class InventoryItemCellGridBuilder
{
    public static RectTransform RebuildCellGrid(Transform ownerTransform, RectTransform existingCellGridRoot, Image cellBackgroundImage, Image itemImage, ItemData itemData, int visualWidth, int visualHeight)
    {
        DestroyCellGrid(existingCellGridRoot);
        ApplyBackground(cellBackgroundImage, itemData);

        if (itemData == null || itemData.IconShowCellGrid == false || itemData.IconShowCellGridBorder == false)
        {
            BringItemImageToFront(itemImage);
            return null;
        }

        Vector2 size = new(visualWidth * ItemGrid.TILE_SIZE_WIDTH, visualHeight * ItemGrid.TILE_SIZE_HEIGHT);

        GameObject gridObject = new("Cell Grid");
        gridObject.transform.SetParent(ownerTransform, false);

        RectTransform cellGridRoot = gridObject.AddComponent<RectTransform>();
        cellGridRoot.anchorMin = new(0f, 1f);
        cellGridRoot.anchorMax = new(0f, 1f);
        cellGridRoot.pivot = new(0f, 1f);
        cellGridRoot.anchoredPosition =Vector2.zero;
        cellGridRoot.sizeDelta = size;
        cellGridRoot.SetAsFirstSibling();

        Color borderColor = itemData.IconCellGridBorderColor;
        float borderThickness = itemData.IconCellGridBorderLineThickness;

        CreateGridLine(cellGridRoot, "Left Border Line", Vector2.zero, new(borderThickness, size.y), new(0.5f, 1f), borderColor);
        CreateGridLine(cellGridRoot, "Right Border Line", new(visualWidth * ItemGrid.TILE_SIZE_WIDTH, 0f), new(borderThickness, size.y), new(0.5f, 1f), borderColor);
        CreateGridLine(cellGridRoot, "Top Border Line", Vector2.zero, new(size.x, borderThickness), new (0f, 0.5f), borderColor);
        CreateGridLine(cellGridRoot, "Bottom Border Line", new(0f, -visualHeight * ItemGrid.TILE_SIZE_HEIGHT), new(size.x, borderThickness), new(0f, 0.5f), borderColor);

        BringItemImageToFront(itemImage);
        return cellGridRoot;
    }

    private static void ApplyBackground(Image cellBackgroundImage, ItemData itemData)
    {
        if (cellBackgroundImage == null)
        {
            return;
        }

        cellBackgroundImage.sprite = null;
        cellBackgroundImage.color = itemData == null ? Color.clear : itemData.IconBackgroundColor;
        cellBackgroundImage.raycastTarget = false;
        cellBackgroundImage.enabled = itemData != null && itemData.IconBackgroundColor.a > 0f;
    }

    private static void BringItemImageToFront(Image itemImage)
    {
        if (itemImage != null)
        {
            itemImage.transform.SetAsLastSibling();
        }
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

    private static void DestroyCellGrid(RectTransform cellGridRoot)
    {
        if (cellGridRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(cellGridRoot.gameObject);
        }
        else
        {
            Object.DestroyImmediate(cellGridRoot.gameObject);
        }
    }
}