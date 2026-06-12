using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour
{
    private static readonly char[] WordSeparators = { ' ', '\t', '\n', '\r' };

    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private Vector2 cursorOffset;
    [SerializeField] private Vector2 screenPadding;
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform iconRectTransform;
    [SerializeField] private LayoutElement iconLayoutElement;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] [Min(1)] private int descriptionWordsPerLine;

    private readonly Vector3[] panelWorldCorners = new Vector3[4];

    private void Awake()
    {
        Hide();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    public void Show(InventoryItem item)
    {
        if (item == null || item.itemData == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        SetIcon(item);
        SetItemName(item.itemData);
        SetWeight(item);
        SetDescription(item.itemData);
        RebuildLayout();
        UpdatePosition();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetIcon(InventoryItem item)
    {
        Vector2 iconSize = new Vector2(
            item.BaseWidth * ItemGrid.tileSizeWidth,
            item.BaseHeight * ItemGrid.tileSizeHeight);

        if (iconRectTransform != null)
        {
            iconRectTransform.sizeDelta = iconSize;
            iconRectTransform.localRotation = Quaternion.identity;
            iconRectTransform.localScale = Vector3.one;
        }

        if (iconLayoutElement != null)
        {
            iconLayoutElement.minWidth = iconSize.x;
            iconLayoutElement.minHeight = iconSize.y;
            iconLayoutElement.preferredWidth = iconSize.x;
            iconLayoutElement.preferredHeight = iconSize.y;
            iconLayoutElement.flexibleWidth = 0f;
            iconLayoutElement.flexibleHeight = 0f;
        }

        if (iconImage == null)
        {
            return;
        }

        Sprite icon = item.itemData.GetIcon();
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.rectTransform.localRotation = Quaternion.identity;
        iconImage.rectTransform.localScale = Vector3.one;
    }

    private void SetItemName(ItemData itemData)
    {
        if (itemNameText == null)
        {
            return;
        }

        itemNameText.text = itemData.ItemName;
    }

    private void SetWeight(InventoryItem item)
    {
        if (weightText == null)
        {
            return;
        }

        weightText.text = item.CurrentAmount > 1
            ? $"Вес: {FormatWeight(item.TotalWeight)} ({FormatWeight(item.UnitWeight)})"
            : $"Вес: {FormatWeight(item.TotalWeight)}";
    }

    private void SetDescription(ItemData itemData)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.text = WrapByWords(itemData.Description);
    }

    private string WrapByWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string[] words = text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        if (descriptionWordsPerLine <= 0)
        {
            return text.Trim();
        }

        int wordsPerLine = descriptionWordsPerLine;
        StringBuilder builder = new StringBuilder(text.Length);

        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % wordsPerLine == 0 ? '\n' : ' ');
            }

            builder.Append(words[i]);
        }

        return builder.ToString();
    }

    private string FormatWeight(float weight)
    {
        float normalizedWeight = Mathf.Max(0f, weight);

        if (normalizedWeight < 1f)
        {
            return $"<color=orange>{Mathf.RoundToInt(normalizedWeight * 1000f)}</color> ГР";
        }

        return $"<color=orange>{normalizedWeight:0.#}</color> КГ";
    }

    private void RebuildLayout()
    {
        if (panelRectTransform == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
    }

    private void UpdatePosition()
    {
        if (panelRectTransform == null)
        {
            return;
        }

        panelRectTransform.position = (Vector2)Input.mousePosition + cursorOffset;
        ClampToScreen();
    }

    private void ClampToScreen()
    {
        panelRectTransform.GetWorldCorners(panelWorldCorners);

        float minX = panelWorldCorners[0].x;
        float minY = panelWorldCorners[0].y;
        float maxX = panelWorldCorners[2].x;
        float maxY = panelWorldCorners[2].y;

        float left = screenPadding.x;
        float right = Screen.width - screenPadding.x;
        float bottom = screenPadding.y;
        float top = Screen.height - screenPadding.y;

        Vector2 offset = Vector2.zero;

        if (minX < left)
        {
            offset.x = left - minX;
        }
        else if (maxX > right)
        {
            offset.x = right - maxX;
        }

        if (minY < bottom)
        {
            offset.y = bottom - minY;
        }
        else if (maxY > top)
        {
            offset.y = top - maxY;
        }

        panelRectTransform.position += (Vector3)offset;
    }
}
