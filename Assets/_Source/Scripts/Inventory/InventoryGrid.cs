using UnityEngine;

public abstract class InventoryGrid : MonoBehaviour
{
    public virtual int HighlightSiblingIndex => 0;
    public abstract RectTransform RectTransform { get; }

    public virtual bool TryFindStack(ItemData itemData, out InventoryItem stack)
    {
        stack = null;
        return false;
    }

    public virtual Vector2 GetHighlightSize(InventoryItem inventoryItem, int posX, int posY)
    {
        return new Vector2(inventoryItem.Width * ItemGrid.tileSizeWidth, inventoryItem.Height * ItemGrid.tileSizeHeight);
    }

    public virtual Vector2 GetHighlightPosition(InventoryItem inventoryItem, int posX, int posY)
    {
        return CalculatePositionOnGrid(inventoryItem, posX, posY);
    }

    public virtual bool CanMergeStackAt(int posX, int posY)
    {
        return true;
    }

    public abstract InventoryItem PickUpItem(int x, int y);
    public abstract InventoryItem GetItem(int x, int y);
    public abstract Vector2Int GetTileGridPosition(Vector2 mousePosition, InventoryItem selectedItem = null);
    public abstract Vector2Int? FindSpaceForObject(InventoryItem itemToInsert);
    public abstract bool CanPlaceItem(InventoryItem inventoryItem, int posX, int posY);
    public abstract bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem);
    public abstract void PlaceItem(InventoryItem inventoryItem, int posX, int posY);
    public abstract Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY);
    public abstract bool BoundryCheck(int posX, int posY, int width, int height);
}
