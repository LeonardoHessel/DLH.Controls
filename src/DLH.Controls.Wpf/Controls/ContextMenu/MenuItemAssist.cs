using System.Windows;

namespace DLH.Controls.Wpf;

/// <summary>Provides optional value-column content for any WPF menu item.</summary>
public static class MenuItemAssist
{
    public static readonly DependencyProperty SubmenuPlacementDirectionProperty = DependencyProperty.RegisterAttached(
        "SubmenuPlacementDirection", typeof(SubmenuPlacementDirection), typeof(MenuItemAssist),
        new FrameworkPropertyMetadata(SubmenuPlacementDirection.Right, FrameworkPropertyMetadataOptions.Inherits));

    public static SubmenuPlacementDirection GetSubmenuPlacementDirection(DependencyObject element) =>
        (SubmenuPlacementDirection)element.GetValue(SubmenuPlacementDirectionProperty);

    public static void SetSubmenuPlacementDirection(DependencyObject element, SubmenuPlacementDirection value) =>
        element.SetValue(SubmenuPlacementDirectionProperty, value);

    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        "Value", typeof(object), typeof(MenuItemAssist), new FrameworkPropertyMetadata(null));

    public static object? GetValue(DependencyObject element) => element.GetValue(ValueProperty);

    public static void SetValue(DependencyObject element, object? value) => element.SetValue(ValueProperty, value);

    public static readonly DependencyProperty ValueTemplateProperty = DependencyProperty.RegisterAttached(
        "ValueTemplate", typeof(DataTemplate), typeof(MenuItemAssist), new FrameworkPropertyMetadata(null));

    public static DataTemplate? GetValueTemplate(DependencyObject element) =>
        element.GetValue(ValueTemplateProperty) as DataTemplate;

    public static void SetValueTemplate(DependencyObject element, DataTemplate? value) =>
        element.SetValue(ValueTemplateProperty, value);
}
