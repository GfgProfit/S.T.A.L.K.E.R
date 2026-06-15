using UnityEngine;
using UnityEngine.UI;

internal static class InventoryItemOverlayLayout
{
    private const float SHORT_NAME_TEXT_HEIGHT = 20f;
    private const float SHORT_NAME_MIN_WIDTH = 48f;
    private const float DURABILITY_BAR_INSET = 5f;
    private const float DURABILITY_BAR_THICKNESS = 3.5f;
    private const float DURABILITY_BAR_OFFSET = 3f;

    private static readonly Vector2 _countTextSize = new(48f, 20f);
    private static readonly Vector2 _statusIconSize = new(18f, 18f);
    private static readonly Vector2 _countTextMargin = new(4f, 4f);
    private static readonly Vector2 _statusIconMargin = new(5f, 5f);
    private static readonly Vector2 _shortNameTextMargin = new(4f, 4f);

    public static void ApplyDurabilityLayout(RectTransform backgroundRectTransform, RectTransform fillRectTransform, Graphic fillGraphic, bool isVisuallyRotated, float currentDurabilityPercent)
    {
        if (backgroundRectTransform == null)
        {
            return;
        }

        if (isVisuallyRotated)
        {
            backgroundRectTransform.anchorMin = new(1f, 0f);
            backgroundRectTransform.anchorMax = new(1f, 1f);
            backgroundRectTransform.pivot = new(1f, 0.5f);
            backgroundRectTransform.anchoredPosition = new(-DURABILITY_BAR_OFFSET, 0f);
            backgroundRectTransform.sizeDelta = new(DURABILITY_BAR_THICKNESS, -DURABILITY_BAR_INSET * 2f);
        }
        else
        {
            backgroundRectTransform.anchorMin = new(0f, 0f);
            backgroundRectTransform.anchorMax = new(1f, 0f);
            backgroundRectTransform.pivot = new(0.5f, 0f);
            backgroundRectTransform.anchoredPosition = new(0f, DURABILITY_BAR_OFFSET);
            backgroundRectTransform.sizeDelta = new(-DURABILITY_BAR_INSET * 2f, DURABILITY_BAR_THICKNESS);
        }

        backgroundRectTransform.localRotation = Quaternion.identity;
        backgroundRectTransform.localScale = Vector3.one;
        ApplyDurabilityFill(fillRectTransform, fillGraphic, isVisuallyRotated, currentDurabilityPercent);
    }

    public static void ApplyDurabilityFill(RectTransform fillRectTransform, Graphic fillGraphic, bool isVisuallyRotated, float currentDurabilityPercent)
    {
        if (fillRectTransform == null)
        {
            return;
        }

        float normalizedDurability = currentDurabilityPercent / 100f;
        bool showFill = normalizedDurability > 0f;
        fillRectTransform.gameObject.SetActive(showFill);

        if (showFill == false)
        {
            return;
        }

        if (fillGraphic != null)
        {
            fillGraphic.color = GameProjectSettings.LoadDefault().GetDurabilityColor(currentDurabilityPercent);
        }

        fillRectTransform.anchorMin = Vector2.zero;
        fillRectTransform.anchorMax = isVisuallyRotated ? new(1f, normalizedDurability) : new(normalizedDurability, 1f);
        fillRectTransform.offsetMin = Vector2.one;
        fillRectTransform.offsetMax = -Vector2.one;
        fillRectTransform.localRotation = Quaternion.identity;
        fillRectTransform.localScale = Vector3.one;
    }

    public static void ApplyCountTextLayout(RectTransform countTextRectTransform, bool isVisuallyRotated)
    {
        if (countTextRectTransform == null)
        {
            return;
        }

        countTextRectTransform.anchorMin = isVisuallyRotated ? new(1f, 1f) : new(1f, 0f);
        countTextRectTransform.anchorMax = countTextRectTransform.anchorMin;
        countTextRectTransform.pivot = new(1f, 0f);
        countTextRectTransform.sizeDelta = _countTextSize;
        countTextRectTransform.anchoredPosition = isVisuallyRotated ? new(-_countTextMargin.y, -_countTextMargin.x) : new(-_countTextMargin.x, _countTextMargin.y);
        countTextRectTransform.localRotation = Quaternion.Euler(0f, 0f, isVisuallyRotated ? 90f : 0f);
        countTextRectTransform.localScale = Vector3.one;
    }

    public static void ApplyShortNameTextLayout(RectTransform shortNameTextRectTransform, bool isVisuallyRotated, int visualWidth)
    {
        if (shortNameTextRectTransform == null)
        {
            return;
        }

        shortNameTextRectTransform.anchorMin = isVisuallyRotated ? Vector2.zero : new(0f, 1f);
        shortNameTextRectTransform.anchorMax = shortNameTextRectTransform.anchorMin;
        shortNameTextRectTransform.pivot = new(0f, 1f);
        shortNameTextRectTransform.sizeDelta = new(GetShortNameTextWidth(visualWidth), SHORT_NAME_TEXT_HEIGHT);
        shortNameTextRectTransform.anchoredPosition = isVisuallyRotated ? new(_shortNameTextMargin.y, _shortNameTextMargin.x) : new(_shortNameTextMargin.x, -_shortNameTextMargin.y);
        shortNameTextRectTransform.localRotation = Quaternion.Euler(0f, 0f, isVisuallyRotated ? 90f : 0f);
        shortNameTextRectTransform.localScale = Vector3.one;
    }

    public static void ApplyStatusIconLayout(RectTransform statusIconRectTransform, bool isVisuallyRotated)
    {
        if (statusIconRectTransform == null)
        {
            return;
        }

        statusIconRectTransform.anchorMin = isVisuallyRotated ? new(1f, 1f) : new(1f, 0f);
        statusIconRectTransform.anchorMax = statusIconRectTransform.anchorMin;
        statusIconRectTransform.pivot = new(1f, 0f);
        statusIconRectTransform.sizeDelta = _statusIconSize;
        statusIconRectTransform.anchoredPosition = isVisuallyRotated ? new(-_statusIconMargin.y, -_statusIconMargin.x) : new(-_statusIconMargin.x, _statusIconMargin.y);
        statusIconRectTransform.localRotation = Quaternion.Euler(0f, 0f, isVisuallyRotated ? 90f : 0f);
        statusIconRectTransform.localScale = Vector3.one;
    }

    private static float GetShortNameTextWidth(int visualWidth)
    {
        float width = visualWidth * ItemGrid.TILE_SIZE_WIDTH;
        return Mathf.Max(SHORT_NAME_MIN_WIDTH, width - _shortNameTextMargin.x * 2f);
    }
}