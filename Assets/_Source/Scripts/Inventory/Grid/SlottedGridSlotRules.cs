internal static class SlottedGridSlotRules
{
    public static bool IsArtifactSlot(SlottedGridSlotState slot) => slot != null && slot.Definition.RestrictItemType && slot.Definition.AcceptedItemType == ItemType.Artifact;
    public static bool ShouldResetRotation(SlottedGridSlotState slot) => IsArtifactSlot(slot);
    public static bool ShouldHideOverlayTexts(SlottedGridSlotState slot) => IsArtifactSlot(slot);
}