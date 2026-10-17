public interface IUIWindow
{
    string Id { get; }
    UIWindowType Type { get; }
    IUIWindow Parent { get; }
    bool IsOpen { get; }
    bool RequiresCursor { get; }
    bool BlocksGameplayInput { get; }
    bool PausesGame { get; }
    bool CanCloseWithBack { get; }
    void Open();
    void Close();
    void BringToFront();
}
