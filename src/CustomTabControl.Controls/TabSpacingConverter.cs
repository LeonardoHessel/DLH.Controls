using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CustomTabControl.Controls;

public sealed class TabSpacingConverter : IValueConverter, IMultiValueConverter
{
    public static TabSpacingConverter Instance { get; } = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        new Thickness(0, 0, value is double spacing ? spacing : 0, 0);
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var spacing = values[0] is double number ? number : 0;
        return values.Length > 1 && values[1] is System.Windows.Controls.Dock.Left or System.Windows.Controls.Dock.Right
            ? new Thickness(0, 0, 0, spacing) : new Thickness(0, 0, spacing, 0);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

