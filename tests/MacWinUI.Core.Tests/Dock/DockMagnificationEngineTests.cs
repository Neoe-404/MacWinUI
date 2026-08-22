using MacWinUI.Core.Dock;
using Xunit;

namespace MacWinUI.Core.Tests.Dock;

public sealed class DockMagnificationEngineTests
{
    private readonly DockMagnificationEngine _engine = new();

    [Fact]
    public void CalculateScale_WhenMouseIsAtIconCenter_ReturnsMaximumScale()
    {
        var scale = _engine.CalculateScale(
            mouseX: 120,
            iconCenterX: 120,
            maxScaleBoost: 0.5,
            sigma: 72);

        Assert.Equal(1.5, scale, precision: 10);
    }

    [Fact]
    public void CalculateScale_WhenMouseIsFarAway_ReturnsScaleNearOne()
    {
        var scale = _engine.CalculateScale(
            mouseX: 1_000,
            iconCenterX: 0,
            maxScaleBoost: 0.5,
            sigma: 72);

        Assert.InRange(scale, 1, 1.000001);
    }

    [Theory]
    [InlineData(-10_000)]
    [InlineData(-72)]
    [InlineData(0)]
    [InlineData(72)]
    [InlineData(10_000)]
    public void CalculateScale_NeverExceedsConfiguredMaximum(double mouseX)
    {
        var scale = _engine.CalculateScale(
            mouseX,
            iconCenterX: 0,
            maxScaleBoost: 0.5,
            sigma: 72);

        Assert.InRange(scale, 1, 1.5);
    }

    [Fact]
    public void CalculateScale_WhenSigmaIsNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _engine.CalculateScale(0, 0, maxScaleBoost: 0.5, sigma: 0));
    }

    [Fact]
    public void CalculateScale_AsDistanceIncreases_DecreasesContinuously()
    {
        var centerScale = _engine.CalculateScale(0, 0, 0.5, 72);
        var nearScale = _engine.CalculateScale(48, 0, 0.5, 72);
        var farScale = _engine.CalculateScale(144, 0, 0.5, 72);

        Assert.True(centerScale > nearScale);
        Assert.True(nearScale > farScale);
        Assert.True(farScale >= 1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(72)]
    [InlineData(240)]
    public void CalculateScale_IsSymmetricAroundIconCenter(double distance)
    {
        var leftScale = _engine.CalculateScale(-distance, 0, 0.5, 72);
        var rightScale = _engine.CalculateScale(distance, 0, 0.5, 72);

        Assert.Equal(leftScale, rightScale, precision: 12);
    }
}
