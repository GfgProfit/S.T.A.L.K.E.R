using TMPro;
using UnityEngine;

public class WorldItemTooltipView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private string _interactKeyColor = "orange";

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

        Show(worldItem.DisplayName, interactKey);
    }

    public void Show(string itemName, string interactKey)
    {
        if (_label == null || string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(interactKey))
        {
            Hide();
            return;
        }

        _label.richText = true;
        _label.text = $"[<color={_interactKeyColor}>{interactKey}</color>] - {itemName}";

        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            return;
        }

        gameObject.SetActive(visible);
    }
}
