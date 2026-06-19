using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

internal sealed class InventoryIconPrewarmController
{
    private readonly bool _prewarmOnStart;
    private readonly bool _logProgress;
    private readonly Object _logContext;
    private readonly IReadOnlyList<EquipmentSlotGrid> _equipmentSlotGrids;

    public InventoryIconPrewarmController(bool prewarmOnStart, bool logProgress, Object logContext, IReadOnlyList<EquipmentSlotGrid> equipmentSlotGrids)
    {
        _prewarmOnStart = prewarmOnStart;
        _logProgress = logProgress;
        _logContext = logContext;
        _equipmentSlotGrids = equipmentSlotGrids;
        IconsReady = prewarmOnStart == false || ItemIconCache.IsPrewarmed;
    }

    public bool IconsReady { get; private set; }

    public async UniTask PrewarmAsync(CancellationToken cancellationToken)
    {
        if (_prewarmOnStart == false || ItemIconCache.IsPrewarmed)
        {
            IconsReady = true;
            return;
        }

        ItemIconGeneratorSettings settings = ItemIconGeneratorSettings.LoadDefault();
        IReadOnlyList<ItemData> items = settings.PrewarmItems;

        if (items.Count == 0)
        {
            Debug.LogWarning("Item icon prewarm catalog is empty. Open the project in Unity Editor to rebuild it.", _logContext);
        }

        try
        {
            await ItemIconCache.PrewarmAsync(items, BuildSlotProfiles(), HandleProgress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, _logContext);
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested == false)
            {
                IconsReady = true;
            }
        }
    }

    private List<ItemIconSlotProfile> BuildSlotProfiles()
    {
        List<ItemIconSlotProfile> profiles = new();

        if (_equipmentSlotGrids == null)
        {
            return profiles;
        }

        for (int i = 0; i < _equipmentSlotGrids.Count; i++)
        {
            EquipmentSlotGrid grid = _equipmentSlotGrids[i];

            if (grid == null)
            {
                continue;
            }

            ItemIconSlotProfile profile = new(grid.RestrictsItemType, grid.AcceptedItemType, grid.GridWidth, grid.GridHeight);

            if (profiles.Contains(profile) == false)
            {
                profiles.Add(profile);
            }
        }

        return profiles;
    }

    private void HandleProgress(int completedCount, int totalCount, ItemData itemData)
    {
        if (_logProgress == false || (completedCount < totalCount && completedCount % 10 != 0))
        {
            return;
        }

        string itemName = itemData == null ? "None" : itemData.ItemName;
        Debug.Log($"Item icon bootstrap: {completedCount}/{totalCount} ({itemName})", _logContext);
    }
}
