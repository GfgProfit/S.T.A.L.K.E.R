using UnityEngine;

public class InventoryHighlight : MonoBehaviour
{
    [SerializeField] private RectTransform _highlighter;

    public void Show(bool value) => _highlighter.gameObject.SetActive(value);

    public void SetSize(InventoryItem targetItem)
    {
        Vector2 size = new()
        {
            x = targetItem.Width * ItemGrid.TILE_SIZE_WIDTH,
            y = targetItem.Height * ItemGrid.TILE_SIZE_HEIGHT
        };

        _highlighter.sizeDelta = size;
    }

    public void SetSize(InventoryGrid targetGrid, InventoryItem targetItem) => SetSize(targetGrid, targetItem, targetItem.GridPositionX, targetItem.GridPositionY);
    public void SetSize(InventoryGrid targetGrid, InventoryItem targetItem, int posX, int posY) => _highlighter.sizeDelta = targetGrid.GetHighlightSize(targetItem, posX, posY);

    public void SetPosition(InventoryGrid targetGrid, InventoryItem targetItem)
    {
        Vector2 pos = targetGrid.GetHighlightPosition(targetItem, targetItem.GridPositionX, targetItem.GridPositionY);

        _highlighter.localPosition = pos;
    }

    public void SetParent(InventoryGrid targetGrid)
    {
        if (targetGrid == null)
        {
            return;
        }

        _highlighter.SetParent(targetGrid.RectTransform, false);
        int siblingIndex = Mathf.Min(targetGrid.HighlightSiblingIndex, _highlighter.parent.childCount - 1);
        _highlighter.SetSiblingIndex(siblingIndex);
    }

    public void SetPosition(InventoryGrid targetGrid, InventoryItem targetItem, int posX, int posY)
    {
        Vector2 pos = targetGrid.GetHighlightPosition(targetItem, posX, posY);

        _highlighter.localPosition = pos;
    }
}