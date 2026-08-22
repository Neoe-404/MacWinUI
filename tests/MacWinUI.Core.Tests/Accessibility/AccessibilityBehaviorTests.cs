using MacWinUI.Core.Accessibility;
using MacWinUI.Core.Models;
using Xunit;

namespace MacWinUI.Core.Tests.Accessibility;

public sealed class AccessibilityBehaviorTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public void ReduceMotionCombinesUserAndSystemPreferences(
        bool userRequested,
        bool systemAnimationsEnabled,
        bool expected)
    {
        var preferences = new AccessibilityPreferences(
            systemAnimationsEnabled,
            HighContrastEnabled: false);

        Assert.Equal(
            expected,
            AccessibilityBehavior.ShouldReduceMotion(userRequested, preferences));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void HighContrastAlwaysDisablesWindowMaterial(
        bool userEnabled,
        bool highContrast,
        bool expected)
    {
        var preferences = new AccessibilityPreferences(
            AnimationsEnabled: true,
            highContrast);

        Assert.Equal(
            expected,
            AccessibilityBehavior.CanUseWindowMaterial(userEnabled, preferences));
    }
}
