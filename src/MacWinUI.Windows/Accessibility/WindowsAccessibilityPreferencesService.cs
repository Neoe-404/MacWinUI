using System.ComponentModel;
using System.Windows;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;

namespace MacWinUI.Windows.Accessibility;

public sealed class WindowsAccessibilityPreferencesService :
    IAccessibilityPreferencesService,
    IDisposable
{
    private bool _isDisposed;

    public WindowsAccessibilityPreferencesService()
    {
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
    }

    public event EventHandler? PreferencesChanged;

    public AccessibilityPreferences GetCurrent() => new(
        SystemParameters.ClientAreaAnimation,
        SystemParameters.HighContrast);

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
        if (e.PropertyName is nameof(SystemParameters.ClientAreaAnimation)
            or nameof(SystemParameters.HighContrast))
        {
            PreferencesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
