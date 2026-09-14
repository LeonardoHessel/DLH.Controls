using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

internal sealed class MenuItemIsSubmenuConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is MenuItem item && ItemsControl.ItemsControlFromItemContainer(item) is MenuItem;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        DependencyProperty.UnsetValue;
}
