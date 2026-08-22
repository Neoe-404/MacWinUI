namespace MacWinUI.Core.Display;

public readonly record struct DisplayWorkArea(
    double Left,
    double Top,
    double Width,
    double Height)
{
    public double Right => Left + Width;

    public double Bottom => Top + Height;
}
