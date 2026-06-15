using System;

public abstract class ViewModelBase : IViewModel
{
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        DisposeManaged();
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeManaged()
    {
    }
}
