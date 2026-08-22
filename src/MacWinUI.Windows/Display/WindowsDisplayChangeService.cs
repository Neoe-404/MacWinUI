using System.ComponentModel;
using System.Windows;
using MacWinUI.Core.Interfaces;

namespace MacWinUI.Windows.Display;

public sealed class WindowsDisplayChangeService : IDisplayChangeService, IDisposable
{
    private bool _isDisposed;

    public WindowsDisplayChangeService()
    {
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
    }

    public event EventHandler? DisplayChanged;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        _isDisposed = true;
    }

    private void OnSystemParametersChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName ?? string.Empty;
        if (propertyName.Contains("Screen", StringComparison.Ordinal)
            || propertyName.Contains("WorkArea", StringComparison.Ordinal))
        {
            DisplayChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
