using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    private const string CountTextObjectName = "Item Count Text";
    private const string ShortNameTextObjectName = "Short Name Text";
    private const float ShortNameTextHeight = 20f;
    private const float ShortNameMinWidth = 48f;

    private static readonly Vector2 CountTextSize = new Vector2(48f, 20f);
    private static readonly Vector2 CountTextMargin = new Vector2(4f, 4f);
    private static readonly Vector2 ShortNameTextMargin = new Vector2(4f, 4f);

    public ItemData itemData;
    [SerializeField] [Min(1)] private int currentAmount = 1;

    public int onGridPositionX;
    public int onGridPositionY;
    public bool rotated;

    private Image itemImage;
    private Image cellBackgroundImage;
    private TMP_Text countText;
    private TMP_Text shortNameText;
    private RectTransform cellGridRoot;
    private RectTransform countTextRectTransform;
    private RectTransform shortNameTextRectTransform;
    private RectTransform rectTransform;
    private bool overlayTextsVisible = true;

    public int Width => itemData == null ? 0 : Mathf.Max(1, rotated ? itemData.height : itemData.width);
    public int Height => itemData == null ? 0 : Mathf.Max(1, rotated ? itemData.width : itemData.height);
    public int CurrentAmount => Mathf.Max(1, currentAmount);
    public bool IsStackable => itemData != null && itemData.IsStackable;
    public float UnitWeight => itemData == null ? 0f : itemData.Weight;
    public float TotalWeight => UnitWeight * CurrentAmount;

    internal void Set(ItemData itemData)
    {
        Set(itemData, 1, null);
    }

    internal void Set(ItemData itemData, int amount)
    {
        Set(itemData, amount, null);
    }

    internal void Set(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        Set(itemData, 1, runtimeIconParts);
    }

    internal void Set(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        EnsureVisuals();

        this.itemData = itemData;
        rotated = false;
        SetAmountInternal(amount);
        RefreshShortNameText();

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

    public void SetAmount(int amount)
    {
        SetAmountInternal(amount);
    }

    public void AddAmount(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        long totalAmount = (long)CurrentAmount + amount;
        SetAmountInternal(totalAmount > int.MaxValue ? int.MaxValue : (int)totalAmount);
    }

    public bool CanStackWith(ItemData targetItemData)
    {
        return targetItemData != null && itemData == targetItemData && IsStackable;
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

    internal void SetOverlayTextsVisible(bool visible)
    {
        overlayTextsVisible = visible;
        RefreshCountText();
        RefreshShortNameText();
    }

    public void Rotate()
    {
        rotated = !rotated;
        ApplyRotation();
    }

    private void OnValidate()
    {
        currentAmount = Mathf.Max(1, currentAmount);
    }

    private void ApplyRotation()
    {
        EnsureVisuals();
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotated ? -90f : 0f);
        ApplyCountTextLayout();
        ApplyShortNameTextLayout();
    }

    private void SetAmountInternal(int amount)
    {
        currentAmount = NormalizeAmount(amount);
        RefreshCountText();
    }

    private int NormalizeAmount(int amount)
    {
        if (itemData == null || itemData.IsStackable == false)
        {
            return 1;
        }

        return Mathf.Max(1, amount);
    }

    private void RefreshCountText()
    {
        EnsureOverlayTexts();

        if (countText == null)
        {
            return;
        }

        bool showCount = overlayTextsVisible && itemData != null && itemData.IsStackable && CurrentAmount > 1;
        countText.text = showCount ? $"x{CurrentAmount}" : string.Empty;
        countText.gameObject.SetActive(showCount);

        if (showCount)
        {
            BringOverlayTextsToFront();
        }
    }

    private void RefreshShortNameText()
    {
        EnsureOverlayTexts();

        if (shortNameText == null)
        {
            return;
        }

        string text = itemData == null ? string.Empty : itemData.ShortName;
        bool showShortName = overlayTextsVisible && string.IsNullOrWhiteSpace(text) == false;
        shortNameText.text = showShortName ? text : string.Empty;
        shortNameText.color = itemData == null ? Color.white : itemData.ShortNameColor;
        shortNameText.gameObject.SetActive(showShortName);

        if (showShortName)
        {
            BringOverlayTextsToFront();
        }
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

        EnsureOverlayTexts();

        if (itemImage != null)
        {
            BringOverlayTextsToFront();
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

        BringOverlayTextsToFront();
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
            BringOverlayTextsToFront();
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
        BringOverlayTextsToFront();
    }

    private void EnsureOverlayTexts()
    {
        EnsureOverlayText(CountTextObjectName, ref countText, ref countTextRectTransform);
        EnsureOverlayText(ShortNameTextObjectName, ref shortNameText, ref shortNameTextRectTransform);

        if (countText != null)
        {
            countText.raycastTarget = false;
            countText.alignment = TextAlignmentOptions.BottomRight;
        }

        if (shortNameText != null)
        {
            shortNameText.raycastTarget = false;
            shortNameText.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private void EnsureOverlayText(string objectName, ref TMP_Text text, ref RectTransform textRectTransform)
    {
        if (text != null && textRectTransform != null)
        {
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name != objectName)
            {
                continue;
            }

            text = texts[i];
            textRectTransform = text.GetComponent<RectTransform>();
            return;
        }

        text = null;
        textRectTransform = null;
    }

    private void BringOverlayTextsToFront()
    {
        EnsureOverlayTexts();

        if (shortNameTextRectTransform != null)
        {
            shortNameTextRectTransform.SetAsLastSibling();
        }

        if (countTextRectTransform != null)
        {
            countTextRectTransform.SetAsLastSibling();
        }
    }

    private void ApplyCountTextLayout()
    {
        EnsureOverlayTexts();

        if (countTextRectTransform == null)
        {
            return;
        }

        countTextRectTransform.anchorMin = rotated ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        countTextRectTransform.anchorMax = countTextRectTransform.anchorMin;
        countTextRectTransform.pivot = new Vector2(1f, 0f);
        countTextRectTransform.sizeDelta = CountTextSize;
        countTextRectTransform.anchoredPosition = rotated
            ? new Vector2(-CountTextMargin.y, -CountTextMargin.x)
            : new Vector2(-CountTextMargin.x, CountTextMargin.y);
        countTextRectTransform.localRotation = Quaternion.Euler(0f, 0f, rotated ? 90f : 0f);
        countTextRectTransform.localScale = Vector3.one;
        BringOverlayTextsToFront();
    }

    private void ApplyShortNameTextLayout()
    {
        EnsureOverlayTexts();

        if (shortNameTextRectTransform == null)
        {
            return;
        }

        shortNameTextRectTransform.anchorMin = rotated ? Vector2.zero : new Vector2(0f, 1f);
        shortNameTextRectTransform.anchorMax = shortNameTextRectTransform.anchorMin;
        shortNameTextRectTransform.pivot = new Vector2(0f, 1f);
        shortNameTextRectTransform.sizeDelta = new Vector2(GetShortNameTextWidth(), ShortNameTextHeight);
        shortNameTextRectTransform.anchoredPosition = rotated
            ? new Vector2(ShortNameTextMargin.y, ShortNameTextMargin.x)
            : new Vector2(ShortNameTextMargin.x, -ShortNameTextMargin.y);
        shortNameTextRectTransform.localRotation = Quaternion.Euler(0f, 0f, rotated ? 90f : 0f);
        shortNameTextRectTransform.localScale = Vector3.one;
        BringOverlayTextsToFront();
    }

    private float GetShortNameTextWidth()
    {
        float visualWidth = Mathf.Max(1, Width) * ItemGrid.tileSizeWidth;
        return Mathf.Max(ShortNameMinWidth, visualWidth - ShortNameTextMargin.x * 2f);
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
