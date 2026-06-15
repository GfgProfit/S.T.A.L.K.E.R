using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class SlottedGridArtifactSlotController
{
    private readonly IReadOnlyList<SlottedGridSlotState> _slots;
    private readonly Func<SlottedGridSlotState, InventoryItem> _getSlotOccupant;
    private readonly SlottedGridClosedSlotVisualController _closedSlotVisualController;

    public SlottedGridArtifactSlotController(IReadOnlyList<SlottedGridSlotState> slots, Func<SlottedGridSlotState, InventoryItem> getSlotOccupant, SlottedGridClosedSlotVisualController closedSlotVisualController)
    {
        _slots = slots;
        _getSlotOccupant = getSlotOccupant;
        _closedSlotVisualController = closedSlotVisualController;
    }

    public bool HasArtifactSlots
    {
        get
        {
            foreach (SlottedGridSlotState slot in _slots)
            {
                if (SlottedGridSlotRules.IsArtifactSlot(slot))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public int ArtifactSlotCount
    {
        get
        {
            int count = 0;

            foreach (SlottedGridSlotState slot in _slots)
            {
                if (SlottedGridSlotRules.IsArtifactSlot(slot))
                {
                    count++;
                }
            }

            return count;
        }
    }

    public bool CanSetOpenArtifactSlotCount(int openSlotBudget)
    {
        int remainingOpenSlots = Mathf.Max(0, openSlotBudget);

        foreach (SlottedGridSlotState slot in _slots)
        {
            if (SlottedGridSlotRules.IsArtifactSlot(slot) == false)
            {
                continue;
            }

            bool shouldOpen = remainingOpenSlots > 0;

            if (shouldOpen)
            {
                remainingOpenSlots--;
                continue;
            }

            if (_getSlotOccupant(slot) != null)
            {
                return false;
            }
        }

        return true;
    }

    public int SetOpenArtifactSlotCount(int openSlotBudget, GameObject closedSlotPrefab)
    {
        int remainingOpenSlots = Mathf.Max(0, openSlotBudget);

        foreach (SlottedGridSlotState slot in _slots)
        {
            if (SlottedGridSlotRules.IsArtifactSlot(slot) == false)
            {
                continue;
            }

            bool shouldOpen = remainingOpenSlots > 0;

            if (shouldOpen)
            {
                remainingOpenSlots--;
            }

            SetSlotClosed(slot, shouldOpen == false, closedSlotPrefab);
        }

        return remainingOpenSlots;
    }

    private void SetSlotClosed(SlottedGridSlotState slot, bool closed, GameObject closedSlotPrefab)
    {
        if (slot == null)
        {
            return;
        }

        if (closed && _getSlotOccupant(slot) != null)
        {
            closed = false;
        }

        slot.IsClosed = closed;

        if (slot.IsClosed)
        {
            _closedSlotVisualController.Show(slot, closedSlotPrefab);
        }
        else
        {
            _closedSlotVisualController.Hide(slot);
        }
    }
}