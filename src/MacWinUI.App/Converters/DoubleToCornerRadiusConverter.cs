using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MacWinUI.App.Converters;

public sealed class DoubleToCornerRadiusConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is double radius
            ? new CornerRadius(radius)
            : new CornerRadius(22);
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
