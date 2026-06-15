using R3;
using UnityEngine;

public sealed class InventoryHighlightViewModel : ViewModelBase
{
    private readonly ReactiveProperty<bool> _visible = new();
    private readonly ReactiveProperty<Vector2> _size = new();
    private readonly ReactiveProperty<Vector2> _position = new();

    public ReadOnlyReactiveProperty<bool> Visible => _visible;
    public ReadOnlyReactiveProperty<Vector2> Size => _size;
    public ReadOnlyReactiveProperty<Vector2> Position => _position;

    public void SetVisible(bool visible)
    {
        _visible.Value = visible;
    }

    public void SetSize(Vector2 size)
    {
        _size.Value = size;
    }

    public void SetPosition(Vector2 position)
    {
        _position.Value = position;
    }

    protected override void DisposeManaged()
    {
        _visible.Dispose();
        _size.Dispose();
        _position.Dispose();
    }
}
