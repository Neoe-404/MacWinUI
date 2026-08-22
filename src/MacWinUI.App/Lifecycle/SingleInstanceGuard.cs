using System.Threading;

namespace MacWinUI.App.Lifecycle;

internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceGuard(Mutex mutex, bool isPrimaryInstance)
    {
        _mutex = mutex;
        IsPrimaryInstance = isPrimaryInstance;
    }

    public bool IsPrimaryInstance { get; }

    public static SingleInstanceGuard Acquire(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var mutex = new Mutex(
            initiallyOwned: true,
            name,
            out var createdNew);

        return new SingleInstanceGuard(mutex, createdNew);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsPrimaryInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex can already be released during abnormal application shutdown.
            }
        }

        _mutex.Dispose();
        _disposed = true;
    }
}
