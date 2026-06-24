using UnityEngine;

internal sealed class ItemInfoPanelPositioner
{
    private readonly Vector3[] _panelWorldCorners = new Vector3[4];

    public void UpdatePosition(RectTransform panelRectTransform, Vector2 pointerPosition, Vector2 cursorOffset, Vector2 screenPadding)
    {
        if (panelRectTransform == null)
        {
            return;
        }

        panelRectTransform.position = pointerPosition + cursorOffset;
        ClampToScreen(panelRectTransform, screenPadding);
    }

    public void CenterOnScreen(RectTransform panelRectTransform, Vector2 screenPadding)
    {
        if (panelRectTransform == null)
        {
            return;
        }

        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
        panelRectTransform.position = screenCenter;
        panelRectTransform.GetWorldCorners(_panelWorldCorners);
        Vector2 panelCenter = ((Vector2)_panelWorldCorners[0] + (Vector2)_panelWorldCorners[2]) * 0.5f;
        panelRectTransform.position += (Vector3)(screenCenter - panelCenter);
        ClampToScreen(panelRectTransform, screenPadding);
    }

    public void ClampToScreen(RectTransform panelRectTransform, Vector2 screenPadding)
    {
        panelRectTransform.GetWorldCorners(_panelWorldCorners);

        float minX = _panelWorldCorners[0].x;
        float minY = _panelWorldCorners[0].y;
        float maxX = _panelWorldCorners[2].x;
        float maxY = _panelWorldCorners[2].y;
        float width = maxX - minX;
        float height = maxY - minY;

        float left = screenPadding.x;
        float right = Screen.width - screenPadding.x;
        float bottom = screenPadding.y;
        float top = Screen.height - screenPadding.y;
        float availableWidth = right - left;
        float availableHeight = top - bottom;

        Vector2 offset = Vector2.zero;

        if (width > availableWidth)
        {
            offset.x = left - minX;
        }
        else if (minX < left)
        {
            offset.x = left - minX;
        }
        else if (maxX > right)
        {
            offset.x = right - maxX;
        }

        if (height > availableHeight)
        {
            offset.y = top - maxY;
        }
        else if (minY < bottom)
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
