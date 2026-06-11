using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldItemTooltipView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string interactKeyColor = "orange";

    private void Awake()
    {
        Hide();
    }

    public void Show(WorldItem worldItem, string interactKey)
    {
        if (worldItem == null)
        {
            Hide();
            return;
        }

        Show(worldItem.ItemName, interactKey);
    }

    public void Show(string itemName, string interactKey)
    {
        if (label == null || string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(interactKey))
        {
            Hide();
            return;
        }

        label.richText = true;
        label.text = $"[<color={interactKeyColor}>{interactKey}</color>] - {itemName}";

        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        gameObject.SetActive(visible);
    }
}
