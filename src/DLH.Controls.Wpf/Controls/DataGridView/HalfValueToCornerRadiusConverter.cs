using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

internal sealed class HalfValueToCornerRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var size = value is double number && double.IsFinite(number) ? Math.Max(0, number) : 0;
        return new CornerRadius(size / 2);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
