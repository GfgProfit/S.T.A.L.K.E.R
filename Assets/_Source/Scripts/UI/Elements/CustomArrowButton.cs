using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomArrowButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Graphic _target;
    [SerializeField] private float _animationDuration = 0.2f;

    [field: SerializeField] public UnityEvent OnClick { get; private set; } = new();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnClick.Invoke();

        _target.DOColor(Color.gray, _animationDuration / 2.0f)
              .OnComplete(() => _target.DOColor(Color.white, _animationDuration / 2.0f));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _target.DOColor(Color.white, _animationDuration);
        _target.rectTransform.DOScale(1.1f, _animationDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _target.DOColor(Color.gray, _animationDuration);
        _target.rectTransform.DOScale(1.0f, _animationDuration);
    }
}