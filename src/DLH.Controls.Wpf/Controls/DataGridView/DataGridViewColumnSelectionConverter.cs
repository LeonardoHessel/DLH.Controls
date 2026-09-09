using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

internal sealed class DataGridViewColumnSelectionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 3 && values[0] is DataGridColumn column && ReferenceEquals(column, values[1]) &&
        values[2] is DataGridViewSelectionBehavior behavior && behavior == DataGridViewSelectionBehavior.Column;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        targetTypes.Select(_ => DependencyProperty.UnsetValue).ToArray();
}
