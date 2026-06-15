using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class InventoryIconPrewarmController
{
    private readonly IReadOnlyList<ItemData> _items;
    private readonly bool _prewarmOnStart;
    private readonly bool _logProgress;
    private readonly Object _logContext;

    public InventoryIconPrewarmController(IReadOnlyList<ItemData> items, bool prewarmOnStart, bool logProgress, Object logContext)
    {
        _items = items;
        _prewarmOnStart = prewarmOnStart;
        _logProgress = logProgress;
        _logContext = logContext;
        IconsReady = prewarmOnStart == false;
    }

    public bool IconsReady { get; private set; }

    public IEnumerator Prewarm()
    {
        if (_prewarmOnStart == false)
        {
            yield break;
        }

        yield return ItemIconCache.PrewarmCoroutine(_items, HandleProgress);

        IconsReady = true;
    }

    private void HandleProgress(int completedCount, int totalCount, ItemData itemData)
    {
        if (_logProgress == false)
        {
            return;
        }

        string itemName = itemData == null ? "None" : itemData.ItemName;
        Debug.Log($"Item icon bootstrap: {completedCount}/{totalCount} ({itemName})", _logContext);
    }
}