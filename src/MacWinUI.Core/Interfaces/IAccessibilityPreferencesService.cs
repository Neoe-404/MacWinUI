using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IAccessibilityPreferencesService
{
    event EventHandler? PreferencesChanged;

    AccessibilityPreferences GetCurrent();
}
