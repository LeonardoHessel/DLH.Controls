using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

/// <summary>Builds the accessible close action name from a tab's accessible name or header.</summary>
internal sealed class TabCloseAutomationNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = values.FirstOrDefault(value => value is string text && !string.IsNullOrWhiteSpace(text))?.ToString();
        if (string.IsNullOrWhiteSpace(name))
            name = values.Skip(1).FirstOrDefault(value => value is not null && !ReferenceEquals(value, DependencyProperty.UnsetValue))?.ToString();
        return string.IsNullOrWhiteSpace(name) ? "Fechar aba" : $"Fechar {name}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();
}
