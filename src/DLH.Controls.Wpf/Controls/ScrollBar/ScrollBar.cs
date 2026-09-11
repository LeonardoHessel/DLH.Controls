using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

public class ScrollBar : System.Windows.Controls.Primitives.ScrollBar
{
    static ScrollBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollBar),
            new FrameworkPropertyMetadata(typeof(ScrollBar)));
        OrientationProperty.OverrideMetadata(typeof(ScrollBar),
            new FrameworkPropertyMetadata(System.Windows.Controls.Orientation.Vertical, OnOrientationChanged));
        WidthProperty.OverrideMetadata(typeof(ScrollBar),
            new FrameworkPropertyMetadata(double.NaN, null, CoerceWidth));
        HeightProperty.OverrideMetadata(typeof(ScrollBar),
            new FrameworkPropertyMetadata(double.NaN, null, CoerceHeight));
    }

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(ScrollBar),
        new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure, OnThicknessChanged),
        value => value is double number && double.IsFinite(number) && number > 0);
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(ScrollBar),
        new FrameworkPropertyMetadata(new CornerRadius(5), FrameworkPropertyMetadataOptions.AffectsRender),
        value => value is CornerRadius radius && IsUniformNonNegative(radius));
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(ScrollBar),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x3D, 0x40, 0x46))));
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    public static readonly DependencyProperty ThumbBrushProperty = DependencyProperty.Register(
        nameof(ThumbBrush), typeof(Brush), typeof(ScrollBar),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x68, 0x6D, 0x77))));
    public Brush ThumbBrush { get => (Brush)GetValue(ThumbBrushProperty); set => SetValue(ThumbBrushProperty, value); }

    public static readonly DependencyProperty ThumbHoverBrushProperty = DependencyProperty.Register(
        nameof(ThumbHoverBrush), typeof(Brush), typeof(ScrollBar),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x8B, 0x91, 0x9D))));
    public Brush ThumbHoverBrush { get => (Brush)GetValue(ThumbHoverBrushProperty); set => SetValue(ThumbHoverBrushProperty, value); }

    public static readonly DependencyProperty ThumbPressedBrushProperty = DependencyProperty.Register(
        nameof(ThumbPressedBrush), typeof(Brush), typeof(ScrollBar),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xA6, 0xAC, 0xB8))));
    public Brush ThumbPressedBrush { get => (Brush)GetValue(ThumbPressedBrushProperty); set => SetValue(ThumbPressedBrushProperty, value); }

    public static readonly DependencyProperty TrackPaddingProperty = DependencyProperty.Register(
        nameof(TrackPadding), typeof(Thickness), typeof(ScrollBar),
        new FrameworkPropertyMetadata(new Thickness(2), FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => value is Thickness thickness && IsValidThickness(thickness));
    public Thickness TrackPadding { get => (Thickness)GetValue(TrackPaddingProperty); set => SetValue(TrackPaddingProperty, value); }

    public static readonly DependencyProperty ThumbOpacityProperty = DependencyProperty.Register(
        nameof(ThumbOpacity), typeof(double), typeof(ScrollBar), new PropertyMetadata(0.72d),
        value => value is double number && double.IsFinite(number) && number is >= 0 and <= 1);
    public double ThumbOpacity { get => (double)GetValue(ThumbOpacityProperty); set => SetValue(ThumbOpacityProperty, value); }

    public static readonly DependencyProperty ShowButtonsProperty = DependencyProperty.Register(
        nameof(ShowButtons), typeof(bool), typeof(ScrollBar), new PropertyMetadata(false));
    public bool ShowButtons { get => (bool)GetValue(ShowButtonsProperty); set => SetValue(ShowButtonsProperty, value); }

    public static readonly DependencyProperty IsShadowEnabledProperty = DependencyProperty.Register(
        nameof(IsShadowEnabled), typeof(bool), typeof(ScrollBar), new PropertyMetadata(false));
    public bool IsShadowEnabled { get => (bool)GetValue(IsShadowEnabledProperty); set => SetValue(IsShadowEnabledProperty, value); }

    public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
        nameof(ShadowColor), typeof(Color), typeof(ScrollBar), new PropertyMetadata(Color.FromRgb(73, 73, 73)));
    public Color ShadowColor { get => (Color)GetValue(ShadowColorProperty); set => SetValue(ShadowColorProperty, value); }

    public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(
        nameof(ShadowOpacity), typeof(double), typeof(ScrollBar), new PropertyMetadata(0.5d),
        value => value is double number && double.IsFinite(number) && number is >= 0 and <= 1);
    public double ShadowOpacity { get => (double)GetValue(ShadowOpacityProperty); set => SetValue(ShadowOpacityProperty, value); }

    public static readonly DependencyProperty ShadowBlurRadiusProperty = DependencyProperty.Register(
        nameof(ShadowBlurRadius), typeof(double), typeof(ScrollBar), new PropertyMetadata(10d),
        value => value is double number && double.IsFinite(number) && number >= 0);
    public double ShadowBlurRadius { get => (double)GetValue(ShadowBlurRadiusProperty); set => SetValue(ShadowBlurRadiusProperty, value); }

    public static readonly DependencyProperty ShadowDepthProperty = DependencyProperty.Register(
        nameof(ShadowDepth), typeof(double), typeof(ScrollBar), new PropertyMetadata(0d),
        value => value is double number && double.IsFinite(number));
    public double ShadowDepth { get => (double)GetValue(ShadowDepthProperty); set => SetValue(ShadowDepthProperty, value); }

    private static void OnOrientationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        dependencyObject.CoerceValue(WidthProperty);
        dependencyObject.CoerceValue(HeightProperty);
    }

    private static void OnThicknessChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        dependencyObject.CoerceValue(WidthProperty);
        dependencyObject.CoerceValue(HeightProperty);
    }

    private static object CoerceWidth(DependencyObject dependencyObject, object baseValue) =>
        ((ScrollBar)dependencyObject).Orientation == System.Windows.Controls.Orientation.Vertical
            ? ((ScrollBar)dependencyObject).Thickness
            : baseValue;

    private static object CoerceHeight(DependencyObject dependencyObject, object baseValue) =>
        ((ScrollBar)dependencyObject).Orientation == System.Windows.Controls.Orientation.Horizontal
            ? ((ScrollBar)dependencyObject).Thickness
            : baseValue;

    private static bool IsUniformNonNegative(CornerRadius radius) => radius.TopLeft >= 0 &&
        radius.TopLeft == radius.TopRight && radius.TopLeft == radius.BottomRight && radius.TopLeft == radius.BottomLeft;
    private static bool IsValidThickness(Thickness thickness) => new[] { thickness.Left, thickness.Top, thickness.Right, thickness.Bottom }
        .All(value => double.IsFinite(value) && value >= 0);
}

