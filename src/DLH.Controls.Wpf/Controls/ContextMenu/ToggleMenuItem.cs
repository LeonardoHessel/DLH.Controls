using System.Windows;
using System.Windows.Controls;

namespace DLH.Controls.Wpf;

/// <summary>
/// A menu item that represents a two-state value and can display a distinct
/// icon for each state.
/// </summary>
public class ToggleMenuItem : MenuItem
{
    static ToggleMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleMenuItem),
            new FrameworkPropertyMetadata(typeof(ToggleMenuItem)));
        IsCheckableProperty.OverrideMetadata(typeof(ToggleMenuItem),
            new FrameworkPropertyMetadata(true));
        IsCheckedProperty.OverrideMetadata(typeof(ToggleMenuItem),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnStateChanged));
    }

    public static readonly DependencyProperty CheckedIconProperty = DependencyProperty.Register(
        nameof(CheckedIcon), typeof(object), typeof(ToggleMenuItem),
        new PropertyMetadata(null, OnStateChanged));

    public object? CheckedIcon
    {
        get => GetValue(CheckedIconProperty);
        set => SetValue(CheckedIconProperty, value);
    }

    public static readonly DependencyProperty UncheckedIconProperty = DependencyProperty.Register(
        nameof(UncheckedIcon), typeof(object), typeof(ToggleMenuItem),
        new PropertyMetadata(null, OnStateChanged));

    public object? UncheckedIcon
    {
        get => GetValue(UncheckedIconProperty);
        set => SetValue(UncheckedIconProperty, value);
    }

    private static void OnStateChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs _) =>
        ((ToggleMenuItem)dependencyObject).UpdateIcon();

    private void UpdateIcon()
    {
        if (CheckedIcon is null && UncheckedIcon is null) return;
        SetCurrentValue(IconProperty, IsChecked ? CheckedIcon : UncheckedIcon);
    }
}
