using MacWinUI.Core.Models;

namespace MacWinUI.Core.Accessibility;

public static class AccessibilityBehavior
{
    public static bool ShouldReduceMotion(
        bool userRequestedReduceMotion,
        AccessibilityPreferences systemPreferences) =>
        userRequestedReduceMotion || !systemPreferences.AnimationsEnabled;

    public static bool CanUseWindowMaterial(
        bool userEnabledMaterial,
        AccessibilityPreferences systemPreferences) =>
        userEnabledMaterial && !systemPreferences.HighContrastEnabled;
}
