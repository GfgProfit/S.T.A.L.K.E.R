using UnityEngine;

internal sealed class InventoryItemState
{
    private float _durabilityPercent = 100f;

    public ItemData ItemData { get; private set; }
    public int CurrentAmount { get; private set; } = 1;
    public bool IsRotated { get; private set; }
    public float DurabilityPercent => _durabilityPercent;
    public int Width => ItemData == null ? 0 : Mathf.Max(1, IsRotated ? ItemData.Height : ItemData.Width);
    public int Height => ItemData == null ? 0 : Mathf.Max(1, IsRotated ? ItemData.Width : ItemData.Height);
    public bool IsStackable => ItemData != null && ItemData.IsStackable;
    public bool HasDurability => ItemData != null && ItemData.HasDurability;
    public float CurrentDurabilityPercent => HasDurability ? ItemData.NormalizeDurability(_durabilityPercent) : 0f;
    public float UnitWeight => ItemData == null ? 0f : ItemData.Weight;
    public float TotalWeight => UnitWeight * CurrentAmount;
    public int BaseWidth => ItemData == null ? 0 : Mathf.Max(1, ItemData.Width);
    public int BaseHeight => ItemData == null ? 0 : Mathf.Max(1, ItemData.Height);
    public bool CanRotate => BaseWidth != BaseHeight;

    public void Initialize(ItemData itemData, int amount, float durabilityPercent, bool isRotated)
    {
        ItemData = itemData;
        CurrentAmount = NormalizeAmount(itemData, amount);
        _durabilityPercent = NormalizeDurability(itemData, durabilityPercent);
        IsRotated = isRotated && CanRotate;
    }

    public void SetItem(ItemData itemData, int amount, float durabilityPercent)
    {
        Initialize(itemData, amount, durabilityPercent, false);
    }

    public void SetAmount(int amount)
    {
        CurrentAmount = NormalizeAmount(ItemData, amount);
    }

    public void AddAmount(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        long totalAmount = (long)CurrentAmount + amount;
        SetAmount(totalAmount > int.MaxValue ? int.MaxValue : (int)totalAmount);
    }

    public bool SetDurability(float durabilityPercent)
    {
        float normalizedDurabilityPercent = NormalizeDurability(ItemData, durabilityPercent);
        bool durabilityChanged = Mathf.Approximately(_durabilityPercent, normalizedDurabilityPercent) == false;

        _durabilityPercent = normalizedDurabilityPercent;
        return durabilityChanged;
    }

    public void SetRotated(bool isRotated)
    {
        IsRotated = isRotated && CanRotate;
    }

    public bool CanStackWith(ItemData targetItemData)
    {
        return targetItemData != null && ItemData == targetItemData && IsStackable;
    }

    public static int NormalizeAmount(ItemData itemData, int amount)
    {
        if (itemData == null || itemData.IsStackable == false)
        {
            return 1;
        }

        return Mathf.Max(1, amount);
    }

    public static float NormalizeDurability(ItemData itemData, float durabilityPercent)
    {
        return itemData == null ? 100f : global::ItemData.NormalizeDurability(durabilityPercent);
    }

    public static float GetDefaultDurabilityPercent(ItemData itemData)
    {
        return itemData == null ? 100f : itemData.DefaultDurabilityPercent;
    }
}
