using UnityEngine;

public class InventoryHighlight : MonoBehaviour
{
    [SerializeField] private RectTransform highlighter;

    public void Show(bool value)
    {
        highlighter.gameObject.SetActive(value);
    }

    public void SetSize(InventoryItem targetItem)
    {
        Vector2 size = new Vector2();

        size.x = targetItem.Width * ItemGrid.tileSizeWidth;
        size.y = targetItem.Height * ItemGrid.tileSizeHeight;

        highlighter.sizeDelta = size;
    }

    public void SetSize(InventoryGrid targetGrid, InventoryItem targetItem)
    {
        SetSize(targetGrid, targetItem, targetItem.onGridPositionX, targetItem.onGridPositionY);
    }

    public void SetSize(InventoryGrid targetGrid, InventoryItem targetItem, int posX, int posY)
    {
        highlighter.sizeDelta = targetGrid.GetHighlightSize(targetItem, posX, posY);
    }

    public void SetPosition(InventoryGrid targetGrid, InventoryItem targetItem)
    {
        Vector2 pos = targetGrid.GetHighlightPosition(targetItem, targetItem.onGridPositionX, targetItem.onGridPositionY);

        highlighter.localPosition = pos;
    }

    public void SetParent(InventoryGrid targetGrid)
    {
        if (targetGrid == null) { return; }

        highlighter.SetParent(targetGrid.RectTransform, false);
        int siblingIndex = Mathf.Min(targetGrid.HighlightSiblingIndex, highlighter.parent.childCount - 1);
        highlighter.SetSiblingIndex(siblingIndex);
    }

    public void SetPosition(InventoryGrid targetGrid, InventoryItem targetItem, int posX, int posY)
    {
        Vector2 pos = targetGrid.GetHighlightPosition(targetItem, posX, posY);

        highlighter.localPosition = pos;
    }
}
