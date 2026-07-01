using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HorizontalSelector : MonoBehaviour
{
    [SerializeField] private CustomArrowButton _leftArrowButton;
    [SerializeField] private CustomArrowButton _rightArrowButton;
    [SerializeField] private TMP_Text _currentContentText;
    [SerializeField] private HorizontalLayoutGroup _horizontalLayoutGroup;
    [SerializeField] private RectTransform _itemPrefab;

    [Space]
    [SerializeField] private float _space = 10.0f;
    [SerializeField] private float _animationDuration = 0.2f;

    public int Index { get; set; } = 0;

    [SerializeField] private List<string> _contents;
    private readonly List<Image> _viewItems = new();

    private void Start()
    {
        _leftArrowButton.OnClick.AddListener(() => DecrementIndex());
        _rightArrowButton.OnClick.AddListener(() => IncrementIndex());

        Setup(_contents);
    }

    public void Setup(List<string> contents)
    {
        _contents = contents;

        Apply();
        CreateViewItemLines();
        SelectViewItem();
    }

    public string GetContentByIndex()
    {
        return _contents[Index];
    }

    private void IncrementIndex()
    {
        Index++;

        if (Index >= _contents.Count)
        {
            Index = 0;
        }

        SelectViewItem();
        AnimateRightContentItem();
    }

    private void DecrementIndex()
    {
        Index--;

        if (Index < 0)
        {
            Index = _contents.Count - 1;
        }

        SelectViewItem();
        AnimateLeftContentItem();
    }

    private void Apply()
    {
        _currentContentText.text = _contents[Index];
    }

    private void CreateViewItemLines()
    {
        RectTransform rectTransform = _horizontalLayoutGroup.GetComponent<RectTransform>();
        float containerWidth = rectTransform.rect.width;

        _horizontalLayoutGroup.spacing = _space;

        float padding = _horizontalLayoutGroup.padding.left + _horizontalLayoutGroup.padding.right;
        float totalSpaces = _space * (_contents.Count - 1);
        float freeWidth = containerWidth - padding - totalSpaces;
        float itemWidth = freeWidth / _contents.Count;

        for (int i = 0; i < _contents.Count; i++)
        {
            RectTransform item = Instantiate(_itemPrefab, _horizontalLayoutGroup.transform);

            Vector2 size = item.sizeDelta;
            size.x = itemWidth;
            item.sizeDelta = size;

            if (item.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement.minWidth = itemWidth;
                layoutElement.preferredWidth = itemWidth;
            }

            _viewItems.Add(item.GetComponent<Image>());
        }
    }

    private void SelectViewItem()
    {
        for (int i = 0; i < _viewItems.Count; i++)
        {
            if (i == Index)
            {
                Image viewItem = _viewItems[i];

                viewItem.DOColor(Color.white, _animationDuration);
                viewItem.rectTransform.DOScaleY(1.3f, _animationDuration);
            }
            else
            {
                _viewItems[i].DOColor(Color.gray, _animationDuration);
                _viewItems[i].rectTransform.DOScaleY(1.0f, _animationDuration);
            }
        }
    }

    private void AnimateLeftContentItem()
    {
        RectTransform contentTextTransform = _currentContentText.rectTransform;

        contentTextTransform.DOLocalMoveX(-350, _animationDuration)
            .OnComplete(() =>
            {
                contentTextTransform.localPosition = new(350, 0, 0);
                contentTextTransform.DOLocalMoveX(0, _animationDuration);
                Apply();
            }).SetEase(Ease.OutBack);
    }

    private void AnimateRightContentItem()
    {
        RectTransform contentTextTransform = _currentContentText.rectTransform;

        contentTextTransform.DOLocalMoveX(350, _animationDuration)
            .OnComplete(() =>
            {
                contentTextTransform.localPosition = new(-350, 0, 0);
                contentTextTransform.DOLocalMoveX(0, _animationDuration);
                Apply();
            }).SetEase(Ease.OutBack);
    }
}