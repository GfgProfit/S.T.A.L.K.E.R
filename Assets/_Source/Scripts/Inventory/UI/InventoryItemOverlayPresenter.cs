using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class InventoryItemOverlayPresenter
{
    public static void ApplySerializedSettings(Image cellBackgroundImage, Image itemImage, TMP_Text countText, RectTransform countTextRectTransform, TMP_Text shortNameText, RectTransform shortNameTextRectTransform, Graphic durabilityBackgroundGraphic, Graphic durabilityFillGraphic, Image statusIconImage, RectTransform statusIconRectTransform)
    {
        ApplyOverlayTextSettings(countText, shortNameText);
        ApplyDurabilityVisualSettings(durabilityBackgroundGraphic, durabilityFillGraphic);
        ApplyStatusIconSettings(statusIconImage);

        if (cellBackgroundImage != null)
        {
            cellBackgroundImage.raycastTarget = false;
        }

        if (itemImage != null)
        {
            itemImage.color = Color.white;
            itemImage.raycastTarget = false;
            itemImage.preserveAspect = false;
            BringOverlayTextsToFront(shortNameTextRectTransform, countTextRectTransform, statusIconRectTransform);
        }
    }

    public static void RefreshCountText(TMP_Text countText, RectTransform countTextRectTransform, RectTransform shortNameTextRectTransform, RectTransform statusIconRectTransform, bool overlayTextsVisible, ItemData itemData, int currentAmount)
    {
        ApplyOverlayTextSettings(countText, null);

        if (countText == null)
        {
            return;
        }

        bool showCount = overlayTextsVisible && itemData != null && itemData.IsStackable && currentAmount > 1;
        countText.text = showCount ? $"x{currentAmount}" : string.Empty;
        countText.gameObject.SetActive(showCount);

        if (showCount)
        {
            BringOverlayTextsToFront(shortNameTextRectTransform, countTextRectTransform, statusIconRectTransform);
        }
    }

    public static void RefreshShortNameText(TMP_Text shortNameText, RectTransform shortNameTextRectTransform, RectTransform countTextRectTransform, RectTransform statusIconRectTransform, bool overlayTextsVisible, ItemData itemData)
    {
        ApplyOverlayTextSettings(null, shortNameText);

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
            BringOverlayTextsToFront(shortNameTextRectTransform, countTextRectTransform, statusIconRectTransform);
        }
    }

    public static void RefreshStatusIcon(RectTransform statusIconRectTransform, Image statusIconImage, Sprite questStatusIcon, bool cellVisualsVisible, ItemData itemData, bool isVisuallyRotated)
    {
        ApplyStatusIconSettings(statusIconImage);

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
            InventoryItemOverlayLayout.ApplyStatusIconLayout(statusIconRectTransform, isVisuallyRotated);
            statusIconRectTransform.SetAsLastSibling();
        }
    }

    public static void ApplyOverlayTextSettings(TMP_Text countText, TMP_Text shortNameText)
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

    public static void ApplyDurabilityVisualSettings(Graphic durabilityBackgroundGraphic, Graphic durabilityFillGraphic)
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

    public static void ApplyStatusIconSettings(Image statusIconImage)
    {
        if (statusIconImage == null)
        {
            return;
        }

        statusIconImage.raycastTarget = false;
        statusIconImage.preserveAspect = true;
    }

    public static void BringOverlayTextsToFront(RectTransform shortNameTextRectTransform, RectTransform countTextRectTransform, RectTransform statusIconRectTransform)
    {
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
}