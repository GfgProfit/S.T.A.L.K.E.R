using System;

public sealed class DelegateUIWindow : IUIWindow
{
    private readonly Func<bool> _isOpen;
    private readonly Action _open;
    private readonly Action _close;
    private readonly Action _bringToFront;

    public DelegateUIWindow(string id, UIWindowType type, Func<bool> isOpen, Action open, Action close, Action bringToFront = null, IUIWindow parent = null, bool requiresCursor = true, bool blocksGameplayInput = true, bool pausesGame = false, bool canCloseWithBack = true)
    {
        Id = id;
        Type = type;
        Parent = parent;
        RequiresCursor = requiresCursor;
        BlocksGameplayInput = blocksGameplayInput;
        PausesGame = pausesGame;
        CanCloseWithBack = canCloseWithBack;
        _isOpen = isOpen;
        _open = open;
        _close = close;
        _bringToFront = bringToFront;
    }

    public string Id { get; }
    public UIWindowType Type { get; }
    public IUIWindow Parent { get; }
    public bool IsOpen => _isOpen != null && _isOpen();
    public bool RequiresCursor { get; }
    public bool BlocksGameplayInput { get; }
    public bool PausesGame { get; }
    public bool CanCloseWithBack { get; }

    public void Open() => _open?.Invoke();
    public void Close() => _close?.Invoke();
    public void BringToFront() => _bringToFront?.Invoke();
}
