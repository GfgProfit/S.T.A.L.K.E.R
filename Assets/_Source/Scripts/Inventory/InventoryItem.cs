using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    private const float ShortNameTextHeight = 20f;
    private const float ShortNameMinWidth = 48f;
    private const float DurabilityBarInset = 5f;
    private const float DurabilityBarThickness = 3.5f;
    private const float DurabilityBarOffset = 3f;

    private static readonly Vector2 CountTextSize = new Vector2(48f, 20f);
    private static readonly Vector2 StatusIconSize = new Vector2(18f, 18f);
    private static readonly Vector2 CountTextMargin = new Vector2(4f, 4f);
    private static readonly Vector2 StatusIconMargin = new Vector2(5f, 5f);
    private static readonly Vector2 ShortNameTextMargin = new Vector2(4f, 4f);

    public ItemData itemData;
    [SerializeField] [Min(1)] private int currentAmount = 1;
    [SerializeField] [Range(0f, 100f)] private float currentDurabilityPercent = 100f;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image itemImage;
    [SerializeField] private Image cellBackgroundImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private RectTransform countTextRectTransform;
    [SerializeField] private TMP_Text shortNameText;
    [SerializeField] private RectTransform shortNameTextRectTransform;
    [SerializeField] private RectTransform durabilityBackgroundRectTransform;
    [SerializeField] private Graphic durabilityBackgroundGraphic;
    [SerializeField] private RectTransform durabilityFillRectTransform;
    [SerializeField] private Graphic durabilityFillGraphic;
    [SerializeField] private Sprite questStatusIcon;
    [SerializeField] private Image statusIconImage;
    [SerializeField] private RectTransform statusIconRectTransform;

    public int onGridPositionX;
    public int onGridPositionY;
    public bool rotated;

    private RectTransform cellGridRoot;
    private bool overlayTextsVisible = true;
    private bool cellVisualsVisible = true;
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
    public RectTransform RectTransform => rectTransform;
    internal IReadOnlyList<ItemIconPart> RuntimeIconParts => runtimeIconParts;

    private void Awake()
    {
        ApplySerializedVisualSettings();
        RefreshDurabilityVisual(true);
        RefreshStatusIcon();
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
        ApplySerializedVisualSettings();

        this.itemData = itemData;
        this.runtimeIconParts = runtimeIconParts;
        rotated = false;
        ClearVisualOverride();
        SetAmountInternal(amount);
        SetDurabilityInternal(durabilityPercent ?? GetDefaultDurabilityPercent());
        RefreshShortNameText();

        RefreshIcon();
        RebuildCellVisuals();
        RefreshStatusIcon();

        if (itemData == null)
        {
            return;
        }

        ApplyVisualSize();
        ApplyRotation();
        SetCellVisualsVisible(true);
        RefreshStatusIcon();
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
        ApplySerializedVisualSettings();

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
        ApplySerializedVisualSettings();

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
        ApplySerializedVisualSettings();
        ClearVisualOverride();
        ApplyVisualSize();
        RefreshIcon();
        RebuildCellVisuals();
        ApplyRotation();
        SetCellVisualsVisible(true);
    }

    internal void SetCellVisualsVisible(bool visible)
    {
        ApplySerializedVisualSettings();
        cellVisualsVisible = visible;

        if (cellBackgroundImage != null)
        {
            cellBackgroundImage.enabled = visible && itemData != null && itemData.IconBackgroundColor.a > 0f;
        }

        if (cellGridRoot != null)
        {
            cellGridRoot.gameObject.SetActive(visible);
        }

        RefreshDurabilityVisual(visible);
        RefreshStatusIcon();
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
        ApplySerializedVisualSettings();
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, IsVisuallyRotated ? -90f : 0f);
        ApplyDurabilityLayout();
        ApplyCountTextLayout();
        ApplyShortNameTextLayout();
        ApplyStatusIconLayout();
    }

    private void ApplyVisualSize()
    {
        ApplySerializedVisualSettings();
        rectTransform.sizeDelta = new Vector2(
            VisualWidth * ItemGrid.tileSizeWidth,
            VisualHeight * ItemGrid.tileSizeHeight);
        ApplyDurabilityLayout();
        ApplyStatusIconLayout();
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
        ApplyOverlayTextSettings();

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
        ApplyOverlayTextSettings();

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
        ApplyDurabilityVisualSettings();

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
        ApplyDurabilityVisualSettings();

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
        ApplyDurabilityVisualSettings();

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

    private void RefreshStatusIcon()
    {
        ApplyStatusIconSettings();

        if (statusIconRectTransform == null || statusIconImage == null)
        {
            return;
        }

        bool showStatusIcon = cellVisualsVisible && itemData != null && itemData.ItemType == ItemType.Quest && questStatusIcon != null;
        statusIconImage.sprite = showStatusIcon ? questStatusIcon : null;
        statusIconImage.enabled = showStatusIcon;
        statusIconRectTransform.gameObject.SetActive(showStatusIcon);

        if (showStatusIcon)
        {
            ApplyStatusIconLayout();
            statusIconRectTransform.SetAsLastSibling();
        }
    }

    private void ApplyStatusIconLayout()
    {
        ApplyStatusIconSettings();

        if (statusIconRectTransform == null)
        {
            return;
        }

        statusIconRectTransform.anchorMin = IsVisuallyRotated ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        statusIconRectTransform.anchorMax = statusIconRectTransform.anchorMin;
        statusIconRectTransform.pivot = new Vector2(1f, 0f);
        statusIconRectTransform.sizeDelta = StatusIconSize;
        statusIconRectTransform.anchoredPosition = IsVisuallyRotated
            ? new Vector2(-StatusIconMargin.y, -StatusIconMargin.x)
            : new Vector2(-StatusIconMargin.x, StatusIconMargin.y);
        statusIconRectTransform.localRotation = Quaternion.Euler(0f, 0f, IsVisuallyRotated ? 90f : 0f);
        statusIconRectTransform.localScale = Vector3.one;
    }

    private void ApplySerializedVisualSettings()
    {
        ApplyOverlayTextSettings();
        ApplyDurabilityVisualSettings();
        ApplyStatusIconSettings();

        if (cellBackgroundImage != null)
        {
            cellBackgroundImage.raycastTarget = false;
        }

        if (itemImage != null)
        {
            itemImage.color = Color.white;
            itemImage.raycastTarget = false;
            itemImage.preserveAspect = false;
            BringOverlayTextsToFront();
        }
    }

    private void RebuildCellVisuals()
    {
        ApplySerializedVisualSettings();
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
            if (itemImage != null)
            {
                itemImage.transform.SetAsLastSibling();
            }

            RefreshDurabilityVisual(true);
            BringOverlayTextsToFront();
            return;
        }

        Vector2 size = new Vector2(
            VisualWidth * ItemGrid.tileSizeWidth,
            VisualHeight * ItemGrid.tileSizeHeight);

        GameObject gridObject = new GameObject("Cell Grid");
        gridObject.transform.SetParent(transform, false);

        cellGridRoot = gridObject.AddComponent<RectTransform>();
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

        if (itemImage != null)
        {
            itemImage.transform.SetAsLastSibling();
        }

        RefreshDurabilityVisual(true);
        BringOverlayTextsToFront();
        RefreshStatusIcon();
    }

    private void ApplyOverlayTextSettings()
    {
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

    private void ApplyDurabilityVisualSettings()
    {
        if (durabilityBackgroundGraphic != null)
        {
            durabilityBackgroundGraphic.raycastTarget = false;
        }

        if (durabilityFillGraphic != null)
        {
            durabilityFillGraphic.raycastTarget = false;
        }
    }

    private void ApplyStatusIconSettings()
    {
        if (statusIconImage == null)
        {
            return;
        }

        statusIconImage.raycastTarget = false;
        statusIconImage.preserveAspect = true;
    }

    private void BringOverlayTextsToFront()
    {
        ApplyOverlayTextSettings();

        if (shortNameTextRectTransform != null)
        {
            shortNameTextRectTransform.SetAsLastSibling();
        }

        if (countTextRectTransform != null)
        {
            countTextRectTransform.SetAsLastSibling();
        }

        if (statusIconRectTransform != null && statusIconRectTransform.gameObject.activeSelf)
        {
            statusIconRectTransform.SetAsLastSibling();
        }
    }

    private void ApplyCountTextLayout()
    {
        ApplyOverlayTextSettings();

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
        ApplyOverlayTextSettings();

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
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform lineRectTransform = lineObject.AddComponent<RectTransform>();
        lineObject.AddComponent<CanvasRenderer>();
        lineRectTransform.anchorMin = new Vector2(0f, 1f);
        lineRectTransform.anchorMax = new Vector2(0f, 1f);
        lineRectTransform.pivot = pivot;
        lineRectTransform.anchoredPosition = anchoredPosition;
        lineRectTransform.sizeDelta = size;

        Image lineImage = lineObject.AddComponent<Image>();
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
