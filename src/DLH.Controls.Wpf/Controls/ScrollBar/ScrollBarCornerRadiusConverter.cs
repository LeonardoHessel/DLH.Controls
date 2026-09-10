using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

internal sealed class ScrollBarCornerRadiusConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not CornerRadius requested || values[1] is not double crossAxis ||
            !double.IsFinite(crossAxis)) return new CornerRadius();
        return new CornerRadius(Math.Min(requested.TopLeft, Math.Max(0, crossAxis / 2)));
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();
}
