using DG.Tweening;
using System;
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
    public event Action<int> IndexChanged;

    [SerializeField] private List<string> _contents;
    private readonly List<Image> _viewItems = new();
    private bool _isSetup;
    private bool _isSubscribed;

    private void Start()
    {
        SubscribeArrows();

        if (_isSetup == false)
        {
            Setup(_contents);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeArrows();
        _currentContentText?.rectTransform.DOKill();
    }

    public void Setup(List<string> contents)
    {
        _contents = contents ?? new List<string>();
        _isSetup = true;
        ClampIndex();

        Apply();
        CreateViewItemLines();
        SelectViewItem();
    }

    public void SetIndex(int index, bool notify = false)
    {
        Index = index;
        ClampIndex();
        Apply();
        SelectViewItem();

        if (notify)
        {
            NotifyIndexChanged();
        }
    }

    public string GetContentByIndex()
    {
        if (_contents == null || _contents.Count == 0)
        {
            return string.Empty;
        }

        return _contents[Index];
    }

    private void IncrementIndex()
    {
        if (_contents == null || _contents.Count == 0)
        {
            return;
        }

        Index++;

        if (Index >= _contents.Count)
        {
            Index = 0;
        }

        SelectViewItem();
        AnimateRightContentItem();
        NotifyIndexChanged();
    }

    private void DecrementIndex()
    {
        if (_contents == null || _contents.Count == 0)
        {
            return;
        }

        Index--;

        if (Index < 0)
        {
            Index = _contents.Count - 1;
        }

        SelectViewItem();
        AnimateLeftContentItem();
        NotifyIndexChanged();
    }

    private void Apply()
    {
        if (_currentContentText == null)
        {
            return;
        }

        _currentContentText.text = GetContentByIndex();
    }

    private void CreateViewItemLines()
    {
        ClearViewItemLines();

        if (_horizontalLayoutGroup == null || _itemPrefab == null || _contents == null || _contents.Count == 0)
        {
            return;
        }

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
            if (_viewItems[i] == null)
            {
                continue;
            }

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
        if (_currentContentText == null)
        {
            return;
        }

        RectTransform contentTextTransform = _currentContentText.rectTransform;
        contentTextTransform.DOKill();

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
        if (_currentContentText == null)
        {
            return;
        }

        RectTransform contentTextTransform = _currentContentText.rectTransform;
        contentTextTransform.DOKill();

        contentTextTransform.DOLocalMoveX(350, _animationDuration)
            .OnComplete(() =>
            {
                contentTextTransform.localPosition = new(-350, 0, 0);
                contentTextTransform.DOLocalMoveX(0, _animationDuration);
                Apply();
            }).SetEase(Ease.OutBack);
    }

    private void SubscribeArrows()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (_leftArrowButton != null)
        {
            _leftArrowButton.OnClick.AddListener(DecrementIndex);
        }

        if (_rightArrowButton != null)
        {
            _rightArrowButton.OnClick.AddListener(IncrementIndex);
        }

        _isSubscribed = true;
    }

    private void UnsubscribeArrows()
    {
        if (_isSubscribed == false)
        {
            return;
        }

        if (_leftArrowButton != null)
        {
            _leftArrowButton.OnClick.RemoveListener(DecrementIndex);
        }

        if (_rightArrowButton != null)
        {
            _rightArrowButton.OnClick.RemoveListener(IncrementIndex);
        }

        _isSubscribed = false;
    }

    private void ClampIndex()
    {
        if (_contents == null || _contents.Count == 0)
        {
            Index = 0;
            return;
        }

        Index = Mathf.Clamp(Index, 0, _contents.Count - 1);
    }

    private void ClearViewItemLines()
    {
        for (int i = 0; i < _viewItems.Count; i++)
        {
            if (_viewItems[i] == null)
            {
                continue;
            }

            _viewItems[i].DOKill();
            _viewItems[i].rectTransform.DOKill();
            Destroy(_viewItems[i].gameObject);
        }

        _viewItems.Clear();
    }

    private void NotifyIndexChanged()
    {
        IndexChanged?.Invoke(Index);
    }
}
