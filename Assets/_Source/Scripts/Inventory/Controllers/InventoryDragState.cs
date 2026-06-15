using UnityEngine;

internal sealed class InventoryDragState
{
    private RectTransform _rectTransform;

    public InventoryItem SelectedItem { get; private set; }
    public InventoryGrid OriginGrid { get; private set; }
    public Vector2Int OriginPosition { get; private set; }
    public bool OriginRotated { get; private set; }
    public bool HasSelectedItem => SelectedItem != null;

    public void CaptureOrigin(InventoryGrid originGrid, InventoryItem item)
    {
        OriginGrid = originGrid;
        OriginPosition = item == null ? Vector2Int.zero : new Vector2Int(item.GridPositionX, item.GridPositionY);
        OriginRotated = item != null && item.IsRotated;
    }

    public void StartDragging(InventoryItem item, Transform canvasTransform, Vector2 pointerPosition)
    {
        SelectedItem = item;

        if (SelectedItem == null)
        {
            return;
        }

        SelectedItem.SetCellVisualsVisible(false);
        SelectedItem.SetOverlayTextsVisible(false);

        _rectTransform = SelectedItem.RectTransform;
        _rectTransform.SetParent(canvasTransform, false);
        _rectTransform.localScale = Vector3.one;
        _rectTransform.SetAsLastSibling();
        _rectTransform.position = pointerPosition;
    }

    public void Drag(Vector2 pointerPosition)
    {
        if (_rectTransform != null)
        {
            _rectTransform.position = pointerPosition;
        }
    }

    public bool ReturnToOrigin()
    {
        if (SelectedItem == null)
        {
            return true;
        }

        if (OriginGrid == null)
        {
            return false;
        }

        SelectedItem.SetRotated(OriginRotated);
        OriginGrid.PlaceItem(SelectedItem, OriginPosition.x, OriginPosition.y);

        Finish();

        return true;
    }

    public void Finish()
    {
        SelectedItem = null;
        _rectTransform = null;
        OriginGrid = null;
    }
}