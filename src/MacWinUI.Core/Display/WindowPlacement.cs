namespace MacWinUI.Core.Display;

public static class WindowPlacement
{
    public static WindowBounds Dock(
        DisplayWorkArea workArea,
        double width,
        double height,
        double bottomMargin)
    {
        Validate(workArea, width, height);
        var margin = NormalizeNonNegative(bottomMargin);
        return new WindowBounds(
            workArea.Left + Math.Max(0, (workArea.Width - width) / 2),
            Math.Max(workArea.Top, workArea.Bottom - height - margin),
            width,
            height);
    }

    public static WindowBounds MenuBar(
        DisplayWorkArea workArea,
        double height)
    {
        Validate(workArea, workArea.Width, height);
        return new WindowBounds(
            workArea.Left,
            workArea.Top,
            workArea.Width,
            Math.Min(height, workArea.Height));
    }

    public static WindowBounds TopRight(
        DisplayWorkArea workArea,
        double width,
        double height,
        double topOffset,
        double rightMargin)
    {
        Validate(workArea, width, height);
        var offset = NormalizeNonNegative(topOffset);
        var margin = NormalizeNonNegative(rightMargin);
        return new WindowBounds(
            Math.Max(workArea.Left, workArea.Right - width - margin),
            Math.Min(
                Math.Max(workArea.Top, workArea.Top + offset),
                Math.Max(workArea.Top, workArea.Bottom - height)),
            width,
            height);
    }

    private static void Validate(
        DisplayWorkArea workArea,
        double width,
        double height)
    {
        if (!double.IsFinite(workArea.Left)
            || !double.IsFinite(workArea.Top)
            || !double.IsFinite(workArea.Width)
            || !double.IsFinite(workArea.Height)
            || workArea.Width <= 0
            || workArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workArea),
                workArea,
                "Display work area must be finite and have a positive size.");
        }

        if (!double.IsFinite(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
    }

    private static double NormalizeNonNegative(double value) =>
        double.IsFinite(value) ? Math.Max(0, value) : 0;
}
