using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryItemContextMenu : MonoBehaviour
{
    private static readonly Vector2 MenuOffset = new Vector2(12f, -8f);
    private static readonly Vector2 ScreenPadding = new Vector2(8f, 8f);
    private static readonly Color TextColor = new Color(0.92f, 0.9f, 0.82f, 1f);
    private static readonly Color DisabledTextColor = new Color(0.48f, 0.47f, 0.42f, 1f);

    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private Button dropOneButton;
    [SerializeField] private Button dropStackButton;
    [SerializeField] private TMP_Text dropStackButtonText;
    [SerializeField] [Min(0f)] private float closeRadius = 220f;
    [SerializeField] private bool showCloseRadiusInEditor = true;
    [SerializeField] private Color closeRadiusEditorColor = new Color(1f, 0.65f, 0f, 0.9f);

    private readonly Vector3[] panelWorldCorners = new Vector3[4];
    private Action onDropOne;
    private Action onDropStack;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        CacheReferences();
        Hide();
    }

    public void Initialize(Action dropOneAction, Action dropStackAction)
    {
        CacheReferences();
        onDropOne = dropOneAction;
        onDropStack = dropStackAction;

        if (dropOneButton != null)
        {
            dropOneButton.onClick.RemoveListener(HandleDropOneClicked);
            dropOneButton.onClick.AddListener(HandleDropOneClicked);
        }

        if (dropStackButton != null)
        {
            dropStackButton.onClick.RemoveListener(HandleDropStackClicked);
            dropStackButton.onClick.AddListener(HandleDropStackClicked);
        }
    }

    public void Show(InventoryItem item, Vector2 screenPosition)
    {
        CacheReferences();

        if (item == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        bool canDropStack = item.IsStackable && item.CurrentAmount > 1;
        SetDropStackButtonEnabled(canDropStack);
        RebuildLayout();
        SetPosition(screenPosition);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        CacheReferences();

        return IsOpen &&
               panelRectTransform != null &&
               RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, screenPoint);
    }

    public bool ShouldCloseForPointer(Vector2 screenPoint)
    {
        if (IsOpen == false || closeRadius <= 0f)
        {
            return false;
        }

        return Vector2.SqrMagnitude(screenPoint - GetPanelCenterScreenPoint()) > closeRadius * closeRadius;
    }

    private void HandleDropOneClicked()
    {
        onDropOne?.Invoke();
    }

    private void HandleDropStackClicked()
    {
        onDropStack?.Invoke();
    }

    private void SetDropStackButtonEnabled(bool enabled)
    {
        if (dropStackButton != null)
        {
            dropStackButton.interactable = enabled;
        }

        if (dropStackButtonText != null)
        {
            dropStackButtonText.color = enabled ? TextColor : DisabledTextColor;
        }
    }

    private void SetPosition(Vector2 screenPosition)
    {
        if (panelRectTransform == null)
        {
            return;
        }

        panelRectTransform.position = screenPosition + MenuOffset;
        ClampToScreen();
    }

    private void ClampToScreen()
    {
        panelRectTransform.GetWorldCorners(panelWorldCorners);

        float minX = panelWorldCorners[0].x;
        float minY = panelWorldCorners[0].y;
        float maxX = panelWorldCorners[2].x;
        float maxY = panelWorldCorners[2].y;

        float left = ScreenPadding.x;
        float right = Screen.width - ScreenPadding.x;
        float bottom = ScreenPadding.y;
        float top = Screen.height - ScreenPadding.y;

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

    private void RebuildLayout()
    {
        if (panelRectTransform == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
    }

    private void CacheReferences()
    {
        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }

        if (dropOneButton == null)
        {
            dropOneButton = FindButton("Drop One Button");
        }

        if (dropStackButton == null)
        {
            dropStackButton = FindButton("Drop Stack Button");
        }

        if (dropStackButtonText == null && dropStackButton != null)
        {
            dropStackButtonText = dropStackButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private Button FindButton(string buttonName)
    {
        Transform buttonTransform = transform.Find(buttonName);
        return buttonTransform == null ? null : buttonTransform.GetComponent<Button>();
    }

    private Vector2 GetPanelCenterScreenPoint()
    {
        if (panelRectTransform == null)
        {
            return transform.position;
        }

        return RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), GetPanelCenterWorldPoint());
    }

    private Vector3 GetPanelCenterWorldPoint()
    {
        if (panelRectTransform == null)
        {
            return transform.position;
        }

        panelRectTransform.GetWorldCorners(panelWorldCorners);
        return (panelWorldCorners[0] + panelWorldCorners[2]) * 0.5f;
    }

    private Camera GetCanvasCamera()
    {
        Canvas canvas = panelRectTransform == null
            ? GetComponentInParent<Canvas>()
            : panelRectTransform.GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (showCloseRadiusInEditor == false || closeRadius <= 0f)
        {
            return;
        }

        CacheReferences();

        Vector3 center = GetPanelCenterWorldPoint();

        Handles.color = closeRadiusEditorColor;
        Handles.DrawWireDisc(center, panelRectTransform == null ? Vector3.forward : panelRectTransform.forward, GetCloseRadiusWorldUnits(center));
    }

    private float GetCloseRadiusWorldUnits(Vector3 center)
    {
        if (panelRectTransform == null)
        {
            return closeRadius;
        }

        Camera canvasCamera = GetCanvasCamera();
        Vector2 centerScreenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, center);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                panelRectTransform,
                centerScreenPoint + Vector2.right * closeRadius,
                canvasCamera,
                out Vector3 radiusWorldPoint) == false)
        {
            return closeRadius;
        }

        return Vector3.Distance(center, radiusWorldPoint);
    }
#endif

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
    }
}
