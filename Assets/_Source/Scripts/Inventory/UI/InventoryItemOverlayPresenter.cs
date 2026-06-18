using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class InventoryItemOverlayPresenter
{
    public static void ApplySerializedSettings(Image cellBackgroundImage, Image itemImage, TMP_Text countText, RectTransform countTextRectTransform, TMP_Text shortNameText, RectTransform shortNameTextRectTransform, RectTransform durabilityBackgroundRectTransform, Graphic durabilityBackgroundGraphic, Graphic durabilityFillGraphic, Image statusIconImage, RectTransform statusIconRectTransform)
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
        }

        BringOverlayTextsToFront(shortNameTextRectTransform, countTextRectTransform, statusIconRectTransform, durabilityBackgroundRectTransform);
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

    public static void BringOverlayTextsToFront(RectTransform shortNameTextRectTransform, RectTransform countTextRectTransform, RectTransform statusIconRectTransform, RectTransform durabilityBackgroundRectTransform = null)
    {
        if (durabilityBackgroundRectTransform != null)
        {
            durabilityBackgroundRectTransform.SetAsLastSibling();
        }

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
