using System;
using UnityEngine;

internal sealed class SlottedGridClosedSlotVisualController
{
    private readonly Func<InventorySlotDefinition, Vector2> _getSlotVisualSize;

    public SlottedGridClosedSlotVisualController(Func<InventorySlotDefinition, Vector2> getSlotVisualSize)
    {
        _getSlotVisualSize = getSlotVisualSize;
    }

    public void Show(SlottedGridSlotState slot, GameObject closedSlotPrefab)
    {
        if (slot == null || slot.VisualRoot == null || closedSlotPrefab == null)
        {
            return;
        }

        if (slot.ClosedSlotInstance == null)
        {
            GameObject closedSlotInstance = UnityEngine.Object.Instantiate(closedSlotPrefab, slot.VisualRoot, false);
            closedSlotInstance.name = closedSlotPrefab.name;
            slot.ClosedSlotInstance = closedSlotInstance.transform as RectTransform;
        }

        RectTransform closedRectTransform = slot.ClosedSlotInstance;

        if (closedRectTransform != null)
        {
            closedRectTransform.anchorMin = new Vector2(0f, 1f);
            closedRectTransform.anchorMax = new Vector2(0f, 1f);
            closedRectTransform.pivot = new Vector2(0f, 1f);
            closedRectTransform.anchoredPosition = Vector2.zero;
            closedRectTransform.sizeDelta = _getSlotVisualSize(slot.Definition);
            closedRectTransform.localRotation = Quaternion.identity;
            closedRectTransform.localScale = Vector3.one;
        }

        slot.ClosedSlotInstance.SetAsFirstSibling();
        slot.ClosedSlotInstance.gameObject.SetActive(true);
    }

    public void Hide(SlottedGridSlotState slot)
    {
        if (slot == null || slot.ClosedSlotInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(slot.ClosedSlotInstance.gameObject);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(slot.ClosedSlotInstance.gameObject);
        }

        slot.ClosedSlotInstance = null;
    }
}