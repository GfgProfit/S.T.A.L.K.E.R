using System;
using UnityEngine;

internal sealed class EquipmentSlotClosedVisualController
{
    private readonly RectTransform _slotRoot;
    private readonly Func<Vector2> _getVisualSize;

    private RectTransform _closedSlotInstanceRectTransform;

    public EquipmentSlotClosedVisualController(RectTransform slotRoot, Func<Vector2> getVisualSize)
    {
        _slotRoot = slotRoot;
        _getVisualSize = getVisualSize;
    }

    public void Show(GameObject closedSlotPrefab)
    {
        if (closedSlotPrefab == null || _slotRoot == null)
        {
            return;
        }

        if (_closedSlotInstanceRectTransform == null)
        {
            GameObject closedSlotInstance = UnityEngine.Object.Instantiate(closedSlotPrefab, _slotRoot, false);
            closedSlotInstance.name = closedSlotPrefab.name;
            _closedSlotInstanceRectTransform = closedSlotInstance.transform as RectTransform;
        }

        RectTransform closedRectTransform = _closedSlotInstanceRectTransform;

        if (closedRectTransform != null)
        {
            closedRectTransform.anchorMin = new(0f, 1f);
            closedRectTransform.anchorMax = new(0f, 1f);
            closedRectTransform.pivot = new(0f, 1f);
            closedRectTransform.anchoredPosition = Vector2.zero;
            closedRectTransform.sizeDelta = _getVisualSize();
            closedRectTransform.localRotation = Quaternion.identity;
            closedRectTransform.localScale = Vector3.one;
        }

        _closedSlotInstanceRectTransform.SetAsFirstSibling();
        _closedSlotInstanceRectTransform.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_closedSlotInstanceRectTransform == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(_closedSlotInstanceRectTransform.gameObject);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(_closedSlotInstanceRectTransform.gameObject);
        }

        _closedSlotInstanceRectTransform = null;
    }
}