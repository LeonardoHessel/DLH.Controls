using System.Windows;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

public partial class CustomTabControl
{
    public static readonly DependencyProperty IsShadowEnabledProperty = DependencyProperty.Register(
        nameof(IsShadowEnabled), typeof(bool), typeof(CustomTabControl), new PropertyMetadata(true));
    public bool IsShadowEnabled { get => (bool)GetValue(IsShadowEnabledProperty); set => SetValue(IsShadowEnabledProperty, value); }

    public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
        nameof(ShadowColor), typeof(Color), typeof(CustomTabControl), new PropertyMetadata(Color.FromRgb(73, 73, 73)));
    public Color ShadowColor { get => (Color)GetValue(ShadowColorProperty); set => SetValue(ShadowColorProperty, value); }

    public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(
        nameof(ShadowOpacity), typeof(double), typeof(CustomTabControl), new PropertyMetadata(0.5d),
        value => value is double number && double.IsFinite(number) && number >= 0 && number <= 1);
    public double ShadowOpacity { get => (double)GetValue(ShadowOpacityProperty); set => SetValue(ShadowOpacityProperty, value); }

    public static readonly DependencyProperty ShadowBlurRadiusProperty = DependencyProperty.Register(
        nameof(ShadowBlurRadius), typeof(double), typeof(CustomTabControl), new PropertyMetadata(10d),
        value => value is double number && double.IsFinite(number) && number >= 0);
    public double ShadowBlurRadius { get => (double)GetValue(ShadowBlurRadiusProperty); set => SetValue(ShadowBlurRadiusProperty, value); }

    public static readonly DependencyProperty ShadowDepthProperty = DependencyProperty.Register(
        nameof(ShadowDepth), typeof(double), typeof(CustomTabControl), new PropertyMetadata(0d),
        value => value is double number && double.IsFinite(number) && number >= 0);
    public double ShadowDepth { get => (double)GetValue(ShadowDepthProperty); set => SetValue(ShadowDepthProperty, value); }

    public static readonly DependencyProperty ShadowDirectionProperty = DependencyProperty.Register(
        nameof(ShadowDirection), typeof(double), typeof(CustomTabControl), new PropertyMetadata(315d),
        value => value is double number && double.IsFinite(number));
    public double ShadowDirection { get => (double)GetValue(ShadowDirectionProperty); set => SetValue(ShadowDirectionProperty, value); }
}
