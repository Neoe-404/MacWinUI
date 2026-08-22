using MacWinUI.Core.System;
using Xunit;

namespace MacWinUI.Core.Tests.System;

public sealed class AudioVolumeTests
{
    [Theory]
    [InlineData(-25, 0)]
    [InlineData(0, 0)]
    [InlineData(42.5, 42.5)]
    [InlineData(100, 100)]
    [InlineData(250, 100)]
    public void NormalizePercent_ClampsToValidRange(double input, double expected)
    {
        Assert.Equal(expected, AudioVolume.NormalizePercent(input));
    }

    [Fact]
    public void PercentToScalar_ConvertsNormalizedValue()
    {
        Assert.Equal(0.75f, AudioVolume.PercentToScalar(75));
    }

    [Theory]
    [InlineData(-0.5f, 0)]
    [InlineData(0.5f, 50)]
    [InlineData(1.5f, 100)]
    public void ScalarToPercent_ClampsAndConverts(float scalar, double expected)
    {
        Assert.Equal(expected, AudioVolume.ScalarToPercent(scalar));
    }
}
