using UnityEngine;

internal sealed class InventoryContextMenuPositioner
{
    private readonly RectTransform _panelRectTransform;
    private readonly Canvas _canvas;
    private readonly Transform _fallbackTransform;
    private readonly Vector2 _menuOffset;
    private readonly Vector2 _screenPadding;
    private readonly Vector3[] _panelWorldCorners = new Vector3[4];

    public InventoryContextMenuPositioner(RectTransform panelRectTransform, Canvas canvas, Transform fallbackTransform, Vector2 menuOffset, Vector2 screenPadding)
    {
        _panelRectTransform = panelRectTransform;
        _canvas = canvas;
        _fallbackTransform = fallbackTransform;
        _menuOffset = menuOffset;
        _screenPadding = screenPadding;
    }

    public bool ContainsScreenPoint(Vector2 screenPoint) => _panelRectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(_panelRectTransform, screenPoint);

    public bool ShouldCloseForPointer(Vector2 screenPoint, float closeRadius)
    {
        if (closeRadius <= 0f)
        {
            return false;
        }

        return Vector2.SqrMagnitude(screenPoint - GetPanelCenterScreenPoint()) > closeRadius * closeRadius;
    }

    public void SetPosition(Vector2 screenPosition)
    {
        if (_panelRectTransform == null)
        {
            return;
        }

        _panelRectTransform.position = screenPosition + _menuOffset;
        ClampToScreen();
    }

    public Vector3 GetPanelCenterWorldPoint()
    {
        if (_panelRectTransform == null)
        {
            return _fallbackTransform == null ? Vector3.zero : _fallbackTransform.position;
        }

        _panelRectTransform.GetWorldCorners(_panelWorldCorners);
        return (_panelWorldCorners[0] + _panelWorldCorners[2]) * 0.5f;
    }

    public float GetCloseRadiusWorldUnits(float closeRadius, Vector3 center)
    {
        if (_panelRectTransform == null)
        {
            return closeRadius;
        }

        Camera canvasCamera = GetCanvasCamera();
        Vector2 centerScreenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, center);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_panelRectTransform, centerScreenPoint + Vector2.right * closeRadius, canvasCamera, out Vector3 radiusWorldPoint) == false)
        {
            return closeRadius;
        }

        return Vector3.Distance(center, radiusWorldPoint);
    }

    private void ClampToScreen()
    {
        _panelRectTransform.GetWorldCorners(_panelWorldCorners);

        float minX = _panelWorldCorners[0].x;
        float minY = _panelWorldCorners[0].y;
        float maxX = _panelWorldCorners[2].x;
        float maxY = _panelWorldCorners[2].y;

        float left = _screenPadding.x;
        float right = Screen.width - _screenPadding.x;
        float bottom = _screenPadding.y;
        float top = Screen.height - _screenPadding.y;

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

        _panelRectTransform.position += (Vector3)offset;
    }

    private Vector2 GetPanelCenterScreenPoint()
    {
        return RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), GetPanelCenterWorldPoint());
    }

    private Camera GetCanvasCamera()
    {
        if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _canvas.worldCamera;
    }
}