using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public ItemData itemData;

    public int onGridPositionX;
    public int onGridPositionY;
    public bool rotated;

    private Image itemImage;
    private Image cellBackgroundImage;
    private RectTransform cellGridRoot;
    private RectTransform rectTransform;

    public int Width => itemData == null ? 0 : Mathf.Max(1, rotated ? itemData.height : itemData.width);
    public int Height => itemData == null ? 0 : Mathf.Max(1, rotated ? itemData.width : itemData.height);

    internal void Set(ItemData itemData)
    {
        Set(itemData, null);
    }

    internal void Set(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        EnsureVisuals();

        this.itemData = itemData;
        rotated = false;

        RefreshIcon(runtimeIconParts);
        RebuildCellVisuals();

        if (itemData == null)
        {
            return;
        }

        Vector2 size = new Vector2();

        size.x = Mathf.Max(1, itemData.width) * ItemGrid.tileSizeWidth;
        size.y = Mathf.Max(1, itemData.height) * ItemGrid.tileSizeHeight;

        rectTransform.sizeDelta = size;
        ApplyRotation();
        SetCellVisualsVisible(true);
    }

    internal void RefreshIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        EnsureVisuals();

        if (itemImage != null)
        {
            itemImage.sprite = itemData == null ? null : itemData.GetIcon(runtimeIconParts);
        }
    }

    internal void SetCellVisualsVisible(bool visible)
    {
        EnsureVisuals();

        if (cellBackgroundImage != null)
        {
            cellBackgroundImage.enabled = visible && itemData != null && itemData.IconBackgroundColor.a > 0f;
        }

        if (cellGridRoot != null)
        {
            cellGridRoot.gameObject.SetActive(visible);
        }
    }

    public void Rotate()
    {
        rotated = !rotated;
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        EnsureVisuals();
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotated ? -90f : 0f);
    }

    private void EnsureVisuals()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (cellBackgroundImage == null)
        {
            cellBackgroundImage = GetComponent<Image>();
            if (cellBackgroundImage != null)
            {
                cellBackgroundImage.raycastTarget = false;
            }
        }

        if (itemImage != null)
        {
            return;
        }

        GameObject iconObject = new GameObject("Item Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
        iconRectTransform.anchorMin = Vector2.zero;
        iconRectTransform.anchorMax = Vector2.one;
        iconRectTransform.offsetMin = Vector2.zero;
        iconRectTransform.offsetMax = Vector2.zero;
        iconRectTransform.pivot = new Vector2(0.5f, 0.5f);

        itemImage = iconObject.GetComponent<Image>();
        itemImage.color = Color.white;
        itemImage.raycastTarget = false;
        itemImage.preserveAspect = false;
    }

    private void RebuildCellVisuals()
    {
        EnsureVisuals();
        DestroyCellGrid();

        if (cellBackgroundImage != null)
        {
            cellBackgroundImage.sprite = null;
            cellBackgroundImage.color = itemData == null ? Color.clear : itemData.IconBackgroundColor;
            cellBackgroundImage.raycastTarget = false;
            cellBackgroundImage.enabled = itemData != null && itemData.IconBackgroundColor.a > 0f;
        }

        if (itemData == null || itemData.IconShowCellGrid == false || itemData.IconShowCellGridBorder == false)
        {
            itemImage.transform.SetAsLastSibling();
            return;
        }

        Vector2 size = new Vector2(
            Mathf.Max(1, itemData.width) * ItemGrid.tileSizeWidth,
            Mathf.Max(1, itemData.height) * ItemGrid.tileSizeHeight);

        GameObject gridObject = new GameObject("Cell Grid", typeof(RectTransform));
        gridObject.transform.SetParent(transform, false);

        cellGridRoot = gridObject.GetComponent<RectTransform>();
        cellGridRoot.anchorMin = new Vector2(0f, 1f);
        cellGridRoot.anchorMax = new Vector2(0f, 1f);
        cellGridRoot.pivot = new Vector2(0f, 1f);
        cellGridRoot.anchoredPosition = Vector2.zero;
        cellGridRoot.sizeDelta = size;
        cellGridRoot.SetAsFirstSibling();

        int itemWidth = Mathf.Max(1, itemData.width);
        int itemHeight = Mathf.Max(1, itemData.height);
        Color borderColor = itemData.IconCellGridBorderColor;
        float borderThickness = itemData.IconCellGridBorderLineThickness;

        CreateGridLine(
            cellGridRoot,
            "Left Border Line",
            Vector2.zero,
            new Vector2(borderThickness, size.y),
            new Vector2(0.5f, 1f),
            borderColor);

        CreateGridLine(
            cellGridRoot,
            "Right Border Line",
            new Vector2(itemWidth * ItemGrid.tileSizeWidth, 0f),
            new Vector2(borderThickness, size.y),
            new Vector2(0.5f, 1f),
            borderColor);

        CreateGridLine(
            cellGridRoot,
            "Top Border Line",
            Vector2.zero,
            new Vector2(size.x, borderThickness),
            new Vector2(0f, 0.5f),
            borderColor);

        CreateGridLine(
            cellGridRoot,
            "Bottom Border Line",
            new Vector2(0f, -itemHeight * ItemGrid.tileSizeHeight),
            new Vector2(size.x, borderThickness),
            new Vector2(0f, 0.5f),
            borderColor);

        itemImage.transform.SetAsLastSibling();
    }

    private void CreateGridLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 pivot, Color color)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        RectTransform lineRectTransform = lineObject.GetComponent<RectTransform>();
        lineRectTransform.anchorMin = new Vector2(0f, 1f);
        lineRectTransform.anchorMax = new Vector2(0f, 1f);
        lineRectTransform.pivot = pivot;
        lineRectTransform.anchoredPosition = anchoredPosition;
        lineRectTransform.sizeDelta = size;

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }

    private void DestroyCellGrid()
    {
        if (cellGridRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(cellGridRoot.gameObject);
        }
        else
        {
            DestroyImmediate(cellGridRoot.gameObject);
        }

        cellGridRoot = null;
    }
}
