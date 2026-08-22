using MacWinUI.Core.Display;
using Xunit;

namespace MacWinUI.Core.Tests.Display;

public sealed class WindowPlacementTests
{
    [Fact]
    public void DockCentersAboveBottomEdgeOnNegativeCoordinateDisplay()
    {
        var workArea = new DisplayWorkArea(-1920, 0, 1920, 1040);

        var bounds = WindowPlacement.Dock(workArea, 600, 80, 12);

        Assert.Equal(-1260, bounds.Left);
        Assert.Equal(948, bounds.Top);
    }

    [Fact]
    public void MenuBarUsesTheEntireSelectedWorkAreaWidth()
    {
        var workArea = new DisplayWorkArea(1600, -120, 2560, 1400);

        var bounds = WindowPlacement.MenuBar(workArea, 28);

        Assert.Equal(new WindowBounds(1600, -120, 2560, 28), bounds);
    }

    [Fact]
    public void TopRightStaysInsideASmallWorkArea()
    {
        var workArea = new DisplayWorkArea(0, 0, 320, 240);

        var bounds = WindowPlacement.TopRight(workArea, 374, 400, 34, 12);

        Assert.Equal(0, bounds.Left);
        Assert.Equal(0, bounds.Top);
    }

    [Fact]
    public void InvalidWorkAreaIsRejected()
    {
        var workArea = new DisplayWorkArea(0, 0, 0, 1080);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WindowPlacement.Dock(workArea, 600, 80, 12));
    }
}
