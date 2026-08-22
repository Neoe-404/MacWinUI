namespace MacWinUI.Core.Dock;

public sealed class DockMagnificationEngine
{
    public double CalculateScale(
        double mouseX,
        double iconCenterX,
        double maxScaleBoost = 0.5,
        double sigma = 72)
    {
        EnsureFinite(mouseX, nameof(mouseX));
        EnsureFinite(iconCenterX, nameof(iconCenterX));
        EnsureFinite(maxScaleBoost, nameof(maxScaleBoost));
        EnsureFinite(sigma, nameof(sigma));

        if (maxScaleBoost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxScaleBoost),
                maxScaleBoost,
                "Maximum scale boost cannot be negative.");
        }

        if (sigma <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sigma),
                sigma,
                "Sigma must be greater than zero.");
        }

        var distance = mouseX - iconCenterX;
        var exponent = -(distance * distance) / (2 * sigma * sigma);
        var scale = 1 + (maxScaleBoost * Math.Exp(exponent));

        return Math.Clamp(scale, 1, 1 + maxScaleBoost);
    }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value must be finite.");
        }
    }
}
