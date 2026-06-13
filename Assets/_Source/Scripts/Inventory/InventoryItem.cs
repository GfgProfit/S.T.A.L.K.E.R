using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    private const string CountTextObjectName = "Item Count Text";
    private const string ShortNameTextObjectName = "Short Name Text";
    private const string DurabilityBackgroundObjectName = "Durability Background";
    private const string DurabilityFillObjectName = "Durability Fill";
    private const float ShortNameTextHeight = 20f;
    private const float ShortNameMinWidth = 48f;
    private const float DurabilityBarInset = 5f;
    private const float DurabilityBarThickness = 3.5f;
    private const float DurabilityBarOffset = 3f;

    private static readonly Vector2 CountTextSize = new Vector2(48f, 20f);
    private static readonly Vector2 CountTextMargin = new Vector2(4f, 4f);
    private static readonly Vector2 ShortNameTextMargin = new Vector2(4f, 4f);

    public ItemData itemData;
    [SerializeField] [Min(1)] private int currentAmount = 1;
    [SerializeField] [Range(0f, 100f)] private float currentDurabilityPercent = 100f;

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
    private RectTransform durabilityBackgroundRectTransform;
    private RectTransform durabilityFillRectTransform;
    private Graphic durabilityFillGraphic;
    private RectTransform rectTransform;
    private bool overlayTextsVisible = true;
    private bool hasVisualSizeOverride;
    private int visualWidthOverride = 1;
    private int visualHeightOverride = 1;
    private Sprite iconOverride;
    private IReadOnlyList<ItemIconPart> runtimeIconParts;

    public event Action<InventoryItem> DurabilityChanged;

    public int Width => itemData == null ? 0 : Mathf.Max(1, rotated ? itemData.height : itemData.width);
    public int Height => itemData == null ? 0 : Mathf.Max(1, rotated ? itemData.width : itemData.height);
    public int CurrentAmount => Mathf.Max(1, currentAmount);
    public bool IsStackable => itemData != null && itemData.IsStackable;
    public bool HasDurability => itemData != null && itemData.HasDurability;
    public float CurrentDurabilityPercent => HasDurability ? ItemData.NormalizeDurability(currentDurabilityPercent) : 0f;
    public float UnitWeight => itemData == null ? 0f : itemData.Weight;
    public float TotalWeight => UnitWeight * CurrentAmount;
    public int BaseWidth => itemData == null ? 0 : Mathf.Max(1, itemData.width);
    public int BaseHeight => itemData == null ? 0 : Mathf.Max(1, itemData.height);
    public bool CanRotate => BaseWidth != BaseHeight;
    internal IReadOnlyList<ItemIconPart> RuntimeIconParts => runtimeIconParts;

    private void Awake()
    {
        EnsureVisuals();
        RefreshDurabilityVisual(true);
    }

    internal void Set(ItemData itemData)
    {
        Set(itemData, 1, null, null);
    }

    internal void Set(ItemData itemData, int amount)
    {
        Set(itemData, amount, null, null);
    }

    internal void Set(ItemData itemData, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        Set(itemData, 1, runtimeIconParts, null);
    }

    internal void Set(ItemData itemData, int amount, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        Set(itemData, amount, runtimeIconParts, null);
    }

    internal void Set(
        ItemData itemData,
        int amount,
        IReadOnlyList<ItemIconPart> runtimeIconParts,
        float? durabilityPercent)
    {
        EnsureVisuals();

        this.itemData = itemData;
        this.runtimeIconParts = runtimeIconParts;
        rotated = false;
        ClearVisualOverride();
        SetAmountInternal(amount);
        SetDurabilityInternal(durabilityPercent ?? GetDefaultDurabilityPercent());
        RefreshShortNameText();

        RefreshIcon();
        RebuildCellVisuals();

        if (itemData == null)
        {
            return;
        }

        ApplyVisualSize();
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

    public void SetDurability(float durabilityPercent)
    {
        SetDurabilityInternal(durabilityPercent);
    }

    internal void RefreshIcon(IReadOnlyList<ItemIconPart> runtimeIconParts = null)
    {
        EnsureVisuals();

        if (runtimeIconParts != null)
        {
            this.runtimeIconParts = runtimeIconParts;
        }

        if (itemImage != null)
        {
            itemImage.sprite = iconOverride != null ? iconOverride : itemData == null ? null : itemData.GetIcon(this.runtimeIconParts);
        }
    }

    internal void ApplySlotVisual(int slotWidth, int slotHeight, bool useGeneratedSlotIcon)
    {
        EnsureVisuals();

        hasVisualSizeOverride = true;
        visualWidthOverride = Mathf.Max(1, slotWidth);
        visualHeightOverride = Mathf.Max(1, slotHeight);
        iconOverride = itemData == null
            ? null
            : useGeneratedSlotIcon
                ? itemData.GetSlotIcon(visualWidthOverride, visualHeightOverride, runtimeIconParts)
                : itemData.GetIcon(runtimeIconParts);

        ApplyVisualSize();
        RefreshIcon();
        RebuildCellVisuals();
        ApplyRotation();
        SetCellVisualsVisible(true);
    }

    internal void RestoreDefaultVisual()
    {
        EnsureVisuals();
        ClearVisualOverride();
        ApplyVisualSize();
        RefreshIcon();
        RebuildCellVisuals();
        ApplyRotation();
        SetCellVisualsVisible(true);
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

        RefreshDurabilityVisual(visible);
    }

    internal void SetOverlayTextsVisible(bool visible)
    {
        overlayTextsVisible = visible;
        RefreshCountText();
        RefreshShortNameText();
    }

    public void Rotate()
    {
        if (CanRotate == false)
        {
            SetRotated(false);
            return;
        }

        SetRotated(rotated == false);
    }

    internal void SetRotated(bool value)
    {
        bool normalizedValue = value && CanRotate;
        if (rotated == normalizedValue)
        {
            ApplyRotation();
            return;
        }

        rotated = normalizedValue;
        ApplyRotation();
    }

    private void OnValidate()
    {
        currentAmount = Mathf.Max(1, currentAmount);
        currentDurabilityPercent = ItemData.NormalizeDurability(currentDurabilityPercent);
    }

    private void ApplyRotation()
    {
        EnsureVisuals();
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, IsVisuallyRotated ? -90f : 0f);
        ApplyDurabilityLayout();
        ApplyCountTextLayout();
        ApplyShortNameTextLayout();
    }

    private void ApplyVisualSize()
    {
        EnsureVisuals();
        rectTransform.sizeDelta = new Vector2(
            VisualWidth * ItemGrid.tileSizeWidth,
            VisualHeight * ItemGrid.tileSizeHeight);
        ApplyDurabilityLayout();
    }

    private void ClearVisualOverride()
    {
        hasVisualSizeOverride = false;
        visualWidthOverride = 1;
        visualHeightOverride = 1;
        iconOverride = null;
    }

    private void SetAmountInternal(int amount)
    {
        currentAmount = NormalizeAmount(amount);
        RefreshCountText();
    }

    private void SetDurabilityInternal(float durabilityPercent)
    {
        float normalizedDurabilityPercent = ItemData.NormalizeDurability(durabilityPercent);
        bool durabilityChanged = Mathf.Approximately(currentDurabilityPercent, normalizedDurabilityPercent) == false;

        currentDurabilityPercent = normalizedDurabilityPercent;
        RefreshDurabilityVisual(true);

        if (durabilityChanged)
        {
            DurabilityChanged?.Invoke(this);
        }
    }

    private float GetDefaultDurabilityPercent()
    {
        return itemData == null ? 100f : itemData.DefaultDurabilityPercent;
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

    private void RefreshDurabilityVisual(bool cellVisualsVisible)
    {
        EnsureDurabilityVisuals();

        if (durabilityBackgroundRectTransform == null)
        {
            return;
        }

        bool showDurability = cellVisualsVisible && HasDurability;
        durabilityBackgroundRectTransform.gameObject.SetActive(showDurability);

        if (showDurability == false)
        {
            return;
        }

        ApplyDurabilityLayout();
        ApplyDurabilityFill();
        durabilityBackgroundRectTransform.SetAsLastSibling();
        BringOverlayTextsToFront();
    }

    private void ApplyDurabilityLayout()
    {
        EnsureDurabilityVisuals();

        if (durabilityBackgroundRectTransform == null)
        {
            return;
        }

        if (IsVisuallyRotated)
        {
            durabilityBackgroundRectTransform.anchorMin = new Vector2(1f, 0f);
            durabilityBackgroundRectTransform.anchorMax = new Vector2(1f, 1f);
            durabilityBackgroundRectTransform.pivot = new Vector2(1f, 0.5f);
            durabilityBackgroundRectTransform.anchoredPosition = new Vector2(-DurabilityBarOffset, 0f);
            durabilityBackgroundRectTransform.sizeDelta = new Vector2(DurabilityBarThickness, -DurabilityBarInset * 2f);
        }
        else
        {
            durabilityBackgroundRectTransform.anchorMin = new Vector2(0f, 0f);
            durabilityBackgroundRectTransform.anchorMax = new Vector2(1f, 0f);
            durabilityBackgroundRectTransform.pivot = new Vector2(0.5f, 0f);
            durabilityBackgroundRectTransform.anchoredPosition = new Vector2(0f, DurabilityBarOffset);
            durabilityBackgroundRectTransform.sizeDelta = new Vector2(-DurabilityBarInset * 2f, DurabilityBarThickness);
        }

        durabilityBackgroundRectTransform.localRotation = Quaternion.identity;
        durabilityBackgroundRectTransform.localScale = Vector3.one;
        ApplyDurabilityFill();
    }

    private void ApplyDurabilityFill()
    {
        EnsureDurabilityVisuals();

        if (durabilityFillRectTransform == null)
        {
            return;
        }

        float normalizedDurability = CurrentDurabilityPercent / 100f;
        bool showFill = normalizedDurability > 0f;
        durabilityFillRectTransform.gameObject.SetActive(showFill);

        if (showFill == false)
        {
            return;
        }

        if (durabilityFillGraphic != null)
        {
            durabilityFillGraphic.color = GameProjectSettings.LoadDefault().GetDurabilityColor(CurrentDurabilityPercent);
        }

        durabilityFillRectTransform.anchorMin = Vector2.zero;
        durabilityFillRectTransform.anchorMax = IsVisuallyRotated
            ? new Vector2(1f, normalizedDurability)
            : new Vector2(normalizedDurability, 1f);
        durabilityFillRectTransform.offsetMin = Vector2.one;
        durabilityFillRectTransform.offsetMax = -Vector2.one;
        durabilityFillRectTransform.localRotation = Quaternion.identity;
        durabilityFillRectTransform.localScale = Vector3.one;
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
        EnsureDurabilityVisuals();

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
            RefreshDurabilityVisual(true);
            BringOverlayTextsToFront();
            return;
        }

        Vector2 size = new Vector2(
            VisualWidth * ItemGrid.tileSizeWidth,
            VisualHeight * ItemGrid.tileSizeHeight);

        GameObject gridObject = new GameObject("Cell Grid", typeof(RectTransform));
        gridObject.transform.SetParent(transform, false);

        cellGridRoot = gridObject.GetComponent<RectTransform>();
        cellGridRoot.anchorMin = new Vector2(0f, 1f);
        cellGridRoot.anchorMax = new Vector2(0f, 1f);
        cellGridRoot.pivot = new Vector2(0f, 1f);
        cellGridRoot.anchoredPosition = Vector2.zero;
        cellGridRoot.sizeDelta = size;
        cellGridRoot.SetAsFirstSibling();

        int itemWidth = VisualWidth;
        int itemHeight = VisualHeight;
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
        RefreshDurabilityVisual(true);
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

    private void EnsureDurabilityVisuals()
    {
        if (durabilityBackgroundRectTransform == null)
        {
            durabilityBackgroundRectTransform = FindChildRectTransform(DurabilityBackgroundObjectName);
        }

        if (durabilityFillRectTransform == null && durabilityBackgroundRectTransform != null)
        {
            RectTransform[] childRectTransforms = durabilityBackgroundRectTransform.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < childRectTransforms.Length; i++)
            {
                if (childRectTransforms[i].name == DurabilityFillObjectName)
                {
                    durabilityFillRectTransform = childRectTransforms[i];
                    durabilityFillRectTransform.TryGetComponent(out durabilityFillGraphic);
                    break;
                }
            }
        }

        if (durabilityFillGraphic == null && durabilityFillRectTransform != null)
        {
            durabilityFillRectTransform.TryGetComponent(out durabilityFillGraphic);
        }

        DisableRaycastTarget(durabilityBackgroundRectTransform);
        DisableRaycastTarget(durabilityFillRectTransform);
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

    private RectTransform FindChildRectTransform(string objectName)
    {
        RectTransform[] childRectTransforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < childRectTransforms.Length; i++)
        {
            if (childRectTransforms[i].name == objectName)
            {
                return childRectTransforms[i];
            }
        }

        return null;
    }

    private static void DisableRaycastTarget(RectTransform target)
    {
        if (target == null || target.TryGetComponent(out Graphic graphic) == false)
        {
            return;
        }

        graphic.raycastTarget = false;
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

        countTextRectTransform.anchorMin = IsVisuallyRotated ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        countTextRectTransform.anchorMax = countTextRectTransform.anchorMin;
        countTextRectTransform.pivot = new Vector2(1f, 0f);
        countTextRectTransform.sizeDelta = CountTextSize;
        countTextRectTransform.anchoredPosition = IsVisuallyRotated
            ? new Vector2(-CountTextMargin.y, -CountTextMargin.x)
            : new Vector2(-CountTextMargin.x, CountTextMargin.y);
        countTextRectTransform.localRotation = Quaternion.Euler(0f, 0f, IsVisuallyRotated ? 90f : 0f);
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

        shortNameTextRectTransform.anchorMin = IsVisuallyRotated ? Vector2.zero : new Vector2(0f, 1f);
        shortNameTextRectTransform.anchorMax = shortNameTextRectTransform.anchorMin;
        shortNameTextRectTransform.pivot = new Vector2(0f, 1f);
        shortNameTextRectTransform.sizeDelta = new Vector2(GetShortNameTextWidth(), ShortNameTextHeight);
        shortNameTextRectTransform.anchoredPosition = IsVisuallyRotated
            ? new Vector2(ShortNameTextMargin.y, ShortNameTextMargin.x)
            : new Vector2(ShortNameTextMargin.x, -ShortNameTextMargin.y);
        shortNameTextRectTransform.localRotation = Quaternion.Euler(0f, 0f, IsVisuallyRotated ? 90f : 0f);
        shortNameTextRectTransform.localScale = Vector3.one;
        BringOverlayTextsToFront();
    }

    private float GetShortNameTextWidth()
    {
        float visualWidth = VisualWidth * ItemGrid.tileSizeWidth;
        return Mathf.Max(ShortNameMinWidth, visualWidth - ShortNameTextMargin.x * 2f);
    }

    private int VisualWidth => hasVisualSizeOverride ? visualWidthOverride : BaseWidth;
    private int VisualHeight => hasVisualSizeOverride ? visualHeightOverride : BaseHeight;
    private bool IsVisuallyRotated => hasVisualSizeOverride == false && rotated;

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
