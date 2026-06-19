using System;

public readonly struct InventoryContextMenuAction
{
    public InventoryContextMenuAction(string label, Action execute, bool interactable = true)
    {
        Label = label ?? string.Empty;
        Execute = execute;
        Interactable = interactable;
    }

    public string Label { get; }
    public Action Execute { get; }
    public bool Interactable { get; }
}
