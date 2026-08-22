namespace MacWinUI.Core.System;

public static class AudioVolume
{
    public static double NormalizePercent(double value)
    {
        return double.IsFinite(value)
            ? Math.Clamp(value, 0, 100)
            : 0;
    }

    public static float PercentToScalar(double percent)
    {
        return (float)(NormalizePercent(percent) / 100);
    }

    public static double ScalarToPercent(float scalar)
    {
        return NormalizePercent(scalar * 100d);
    }
}
