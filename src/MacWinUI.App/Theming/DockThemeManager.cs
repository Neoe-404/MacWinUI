using System.Windows;
using System.Windows.Media;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;

namespace MacWinUI.App.Theming;

public sealed class DockThemeManager(ISystemThemeService systemThemeService)
{
    private static readonly string[] SemanticResourceNames =
    [
        "DockBackgroundBrush",
        "DockBorderBrush",
        "DockHoverBrush",
        "DockIndicatorBrush",
        "DockActiveIndicatorBrush",
        "DockSeparatorBrush",
        "PrimaryTextBrush",
        "SecondaryTextBrush",
        "DockItemBackgroundBrush",
        "DockIconPlateBrush",
        "DockTooltipBackgroundBrush",
        "DockGlassHighlightBrush",
        "DockShadowColor",
        "DockIconShadowColor",
        "MenuBarBackgroundBrush",
        "MenuBarBorderBrush",
        "MenuBarTextBrush",
        "MenuBarHoverBrush",
        "ControlCenterBackgroundBrush",
        "ControlCenterBorderBrush",
        "ControlCenterCardBrush",
        "ControlCenterCardHoverBrush",
        "ControlCenterCardPressedBrush",
        "ControlCenterIconSurfaceBrush",
        "ControlCenterDividerBrush",
        "ControlCenterAccentBrush",
        "ControlCenterSuccessBrush",
        "ControlCenterDangerBrush",
        "ControlCenterSliderTrackBrush",
        "ControlCenterSliderThumbBrush",
        "ControlCenterSelectedTextBrush",
        "ControlCenterHighlightBrush"
    ];

    public void Apply(
        ResourceDictionary resources,
        DockTheme requestedTheme,
        bool highContrast = false)
    {
        ArgumentNullException.ThrowIfNull(resources);

        if (highContrast)
        {
            ApplyHighContrast(resources);
            return;
        }

        var resolvedTheme = requestedTheme is DockTheme.Auto
            ? systemThemeService.GetSystemTheme()
            : requestedTheme;
        var palettePrefix = resolvedTheme switch
        {
            DockTheme.BigSur => "BigSur",
            DockTheme.Light => "Light",
            _ => "Dark"
        };

        foreach (var semanticName in SemanticResourceNames)
        {
            var paletteKey = $"{palettePrefix}{semanticName}";
            var paletteValue = resources[paletteKey]
                ?? throw new InvalidOperationException(
                    $"Theme resource '{paletteKey}' is not defined.");
            resources[semanticName] = paletteValue is Freezable freezable
                ? freezable.CloneCurrentValue()
                : paletteValue;
        }
    }

    public void ApplyMaterialIntensity(
        ResourceDictionary resources,
        double intensity,
        bool highContrast = false)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (highContrast)
        {
            return;
        }

        var brushOpacity = 0.55 + (Math.Clamp(intensity, 0.35, 1) * 0.45);
        foreach (var resourceName in new[]
                 {
                     "DockBackgroundBrush",
                     "MenuBarBackgroundBrush",
                     "ControlCenterBackgroundBrush"
                 })
        {
            if (resources[resourceName] is Brush brush && !brush.IsFrozen)
            {
                brush.Opacity = brushOpacity;
            }
        }
    }

    private static void ApplyHighContrast(ResourceDictionary resources)
    {
        resources["DockBackgroundBrush"] = SystemColors.WindowBrush;
        resources["DockBorderBrush"] = SystemColors.WindowTextBrush;
        resources["DockHoverBrush"] = SystemColors.HighlightBrush;
        resources["DockIndicatorBrush"] = SystemColors.WindowTextBrush;
        resources["DockActiveIndicatorBrush"] = SystemColors.HighlightBrush;
        resources["DockSeparatorBrush"] = SystemColors.GrayTextBrush;
        resources["PrimaryTextBrush"] = SystemColors.WindowTextBrush;
        resources["SecondaryTextBrush"] = SystemColors.GrayTextBrush;
        resources["DockItemBackgroundBrush"] = Brushes.Transparent;
        resources["DockIconPlateBrush"] = Brushes.Transparent;
        resources["DockTooltipBackgroundBrush"] = SystemColors.WindowBrush;
        resources["DockGlassHighlightBrush"] = SystemColors.WindowTextBrush;
        resources["DockShadowColor"] = Colors.Transparent;
        resources["DockIconShadowColor"] = Colors.Transparent;
        resources["MenuBarBackgroundBrush"] = SystemColors.WindowBrush;
        resources["MenuBarBorderBrush"] = SystemColors.WindowTextBrush;
        resources["MenuBarTextBrush"] = SystemColors.WindowTextBrush;
        resources["MenuBarHoverBrush"] = SystemColors.HighlightBrush;
        resources["ControlCenterBackgroundBrush"] = SystemColors.WindowBrush;
        resources["ControlCenterBorderBrush"] = SystemColors.WindowTextBrush;
        resources["ControlCenterCardBrush"] = SystemColors.ControlBrush;
        resources["ControlCenterCardHoverBrush"] = SystemColors.HighlightBrush;
        resources["ControlCenterCardPressedBrush"] = SystemColors.HighlightBrush;
        resources["ControlCenterIconSurfaceBrush"] = SystemColors.ControlBrush;
        resources["ControlCenterDividerBrush"] = SystemColors.WindowTextBrush;
        resources["ControlCenterAccentBrush"] = SystemColors.HighlightBrush;
        resources["ControlCenterSuccessBrush"] = SystemColors.HighlightBrush;
        resources["ControlCenterDangerBrush"] = SystemColors.HighlightBrush;
        resources["ControlCenterSliderTrackBrush"] = SystemColors.GrayTextBrush;
        resources["ControlCenterSliderThumbBrush"] = SystemColors.HighlightTextBrush;
        resources["ControlCenterSelectedTextBrush"] = SystemColors.HighlightTextBrush;
        resources["ControlCenterHighlightBrush"] = SystemColors.WindowTextBrush;
    }
}
