using UnityEngine;

internal delegate bool TryGetStackMergeTargetDelegate(InventoryGrid targetGrid, Vector2Int tileGridPosition, out InventoryItem targetStack);