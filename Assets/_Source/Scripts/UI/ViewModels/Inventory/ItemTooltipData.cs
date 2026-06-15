using System.Collections.Generic;

public readonly struct ItemTooltipData
{
    public ItemTooltipData(ItemData itemData, int amount, float unitWeight, float totalWeight, bool hasDurability, float durabilityPercent, int baseWidth, int baseHeight, IReadOnlyList<ItemIconPart> runtimeIconParts)
    {
        ItemData = itemData;
        Amount = amount;
        UnitWeight = unitWeight;
        TotalWeight = totalWeight;
        HasDurability = hasDurability;
        DurabilityPercent = durabilityPercent;
        BaseWidth = baseWidth;
        BaseHeight = baseHeight;
        RuntimeIconParts = runtimeIconParts;
    }

    public ItemData ItemData { get; }
    public int Amount { get; }
    public float UnitWeight { get; }
    public float TotalWeight { get; }
    public bool HasDurability { get; }
    public float DurabilityPercent { get; }
    public int BaseWidth { get; }
    public int BaseHeight { get; }
    public IReadOnlyList<ItemIconPart> RuntimeIconParts { get; }
    public bool IsValid => ItemData != null;
}
