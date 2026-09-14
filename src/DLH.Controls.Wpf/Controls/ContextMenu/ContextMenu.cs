using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace DLH.Controls.Wpf;

/// <summary>
/// A themed context menu that preserves the native WPF command, keyboard,
/// checkable item, submenu and MVVM behavior.
/// </summary>
public class ContextMenu : System.Windows.Controls.ContextMenu
{
    private readonly HashSet<DependencyObject> preparedElements = [];
    private readonly HashSet<DependencyObject> consumerStyledElements = [];
    private readonly HashSet<DependencyObject> sharedStyledElements = [];

    static ContextMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ContextMenu),
            new FrameworkPropertyMetadata(typeof(ContextMenu)));
    }

    public ContextMenu()
    {
        MenuInteraction.SetIsScopeRoot(this, true);
        Opened += (_, _) => ApplyItemStyles(Items);
        Closed += (_, _) => MenuInteraction.CollapseAll(this);
    }

    public void ActivatePath(MenuItem anchor) => MenuInteraction.ActivatePath(this, anchor);

    public void CollapseAfter(MenuItem anchor) => MenuInteraction.CollapseAfter(this, anchor);

    public void CollapseAll() => MenuInteraction.CollapseAll(this);

    public static readonly DependencyProperty SubmenuPlacementDirectionProperty =
        MenuItemAssist.SubmenuPlacementDirectionProperty.AddOwner(typeof(ContextMenu),
            new FrameworkPropertyMetadata(SubmenuPlacementDirection.Right, FrameworkPropertyMetadataOptions.Inherits));

    public SubmenuPlacementDirection SubmenuPlacementDirection
    {
        get => (SubmenuPlacementDirection)GetValue(SubmenuPlacementDirectionProperty);
        set => SetValue(SubmenuPlacementDirectionProperty, value);
    }

    public static readonly DependencyProperty SubmenuHorizontalOffsetProperty = DependencyProperty.Register(
        nameof(SubmenuHorizontalOffset), typeof(double), typeof(ContextMenu), new PropertyMetadata(0d), IsFinite);

    public double SubmenuHorizontalOffset
    {
        get => (double)GetValue(SubmenuHorizontalOffsetProperty);
        set => SetValue(SubmenuHorizontalOffsetProperty, value);
    }

    public static readonly DependencyProperty SubmenuVerticalOffsetProperty = DependencyProperty.Register(
        nameof(SubmenuVerticalOffset), typeof(double), typeof(ContextMenu), new PropertyMetadata(0d), IsFinite);

    public double SubmenuVerticalOffset
    {
        get => (double)GetValue(SubmenuVerticalOffsetProperty);
        set => SetValue(SubmenuVerticalOffsetProperty, value);
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        RememberConsumerStyle(element);
        base.PrepareContainerForItemOverride(element, item);
        preparedElements.Add(element);
        ApplyItemStyle(element);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyItemStyles(Items);
    }

    private void ApplyItemStyles(ItemCollection items)
    {
        foreach (var item in items)
        {
            if (item is not DependencyObject element) continue;
            ApplyItemStyle(element);
            if (item is MenuItem menuItem && menuItem.HasItems)
                ApplyItemStyles(menuItem.Items);
        }
    }

    private void ApplyItemStyle(DependencyObject element)
    {
        if (!preparedElements.Contains(element)) RememberConsumerStyle(element);
        var canApplyDefaultStyle = !consumerStyledElements.Contains(element);
        if (element is MenuItem menuItem)
        {
            menuItem.Resources["ContextMenu.Surface"] = Background;
            menuItem.Resources["ContextMenu.Text"] = Foreground;
            menuItem.Resources["ContextMenu.Edge"] = BorderBrush;
            menuItem.Resources["ContextMenu.Hover"] = HoverBrush;
            menuItem.Resources["ContextMenu.Checked"] = CheckedBrush;
            menuItem.Resources["ContextMenu.ItemBorder"] = ItemBorderBrush;
            menuItem.Resources["ContextMenu.ItemHoverBorder"] = ItemHoverBorderBrush;
            menuItem.Resources["ContextMenu.ItemCheckedBorder"] = ItemCheckedBorderBrush;
            menuItem.Resources["ContextMenu.ItemBorderThickness"] = ItemBorderThickness;
            menuItem.Resources["ContextMenu.ItemHoverBorderThickness"] = ItemHoverBorderThickness;
            menuItem.Resources["ContextMenu.ItemCheckedBorderThickness"] = ItemCheckedBorderThickness;
            menuItem.Resources["ContextMenu.ItemCornerRadius"] = ItemCornerRadius;
            menuItem.Resources["ContextMenu.DisabledOpacity"] = DisabledOpacity;
            menuItem.Resources["ContextMenu.ItemPadding"] = ItemPadding;
            menuItem.Resources["ContextMenu.IconSize"] = IconSize;
            menuItem.Resources["ContextMenu.IconColumnWidth"] = new GridLength(IconColumnWidth);
            menuItem.Resources["ContextMenu.TitleColumnWidth"] = TitleColumnWidth;
            menuItem.Resources["ContextMenu.ValueColumnWidth"] = ValueColumnWidth;
            menuItem.Resources["ContextMenu.InputGestureColumnWidth"] = InputGestureColumnWidth;
            menuItem.Resources["ContextMenu.SubmenuTitleColumnWidth"] = SubmenuTitleColumnWidth;
            menuItem.Resources["ContextMenu.SubmenuValueColumnWidth"] = SubmenuValueColumnWidth;
            menuItem.Resources["ContextMenu.SubmenuInputGestureColumnWidth"] = SubmenuInputGestureColumnWidth;
            var arrowWidth = menuItem is ChoiceMenuItem choice ? choice.DropDownButtonWidth : ArrowColumnWidth;
            menuItem.Resources["ContextMenu.IconAreaWidth"] = new GridLength(IconColumnWidth + ItemPadding.Left);
            menuItem.Resources["ContextMenu.ArrowAreaWidth"] = arrowWidth + ItemPadding.Right;
            menuItem.Resources["ContextMenu.SubmenuIconAreaWidth"] = new GridLength(SubmenuIconColumnWidth + ItemPadding.Left);
            menuItem.Resources["ContextMenu.SubmenuArrowAreaWidth"] = SubmenuArrowColumnWidth + ItemPadding.Right;
            menuItem.Resources["ContextMenu.SubmenuHorizontalOffset"] = SubmenuHorizontalOffset;
            menuItem.Resources["ContextMenu.SubmenuVerticalOffset"] = SubmenuVerticalOffset;
            menuItem.Resources["ContextMenu.ItemVerticalMargin"] = new Thickness(0, ItemPadding.Top, 0, ItemPadding.Bottom);
            menuItem.Resources["ContextMenu.CornerRadius"] = CornerRadius;
            menuItem.Resources["ContextMenu.Padding"] = Padding;
            menuItem.Resources["ContextMenu.BorderThickness"] = BorderThickness;
            menuItem.Resources["ContextMenu.ShadowColor"] = ShadowColor;
            menuItem.Resources["ContextMenu.ShadowOpacity"] = IsShadowEnabled ? ShadowOpacity : 0d;
            menuItem.Resources["ContextMenu.ShadowBlurRadius"] = ShadowBlurRadius;
            menuItem.Resources["ContextMenu.ShadowDepth"] = ShadowDepth;
            menuItem.Resources["ContextMenu.ShadowMargin"] = IsShadowEnabled ? new Thickness(6) : new Thickness(0);
            var menuItemStyle = ItemContainerStyle ?? FindSharedStyle(menuItem);
            if (menuItemStyle is not null) menuItem.Resources["ContextMenu.ItemStyle"] = menuItemStyle;
            if (canApplyDefaultStyle && menuItemStyle is not null)
            {
                sharedStyledElements.Add(menuItem);
                menuItem.SetCurrentValue(StyleProperty, menuItemStyle);
            }
        }
        else if (element is Separator separator)
        {
            separator.Resources["ContextMenu.Separator"] = SeparatorBrush;
            if (canApplyDefaultStyle && FindSharedStyle(separator) is { } separatorStyle)
            {
                sharedStyledElements.Add(separator);
                separator.SetCurrentValue(StyleProperty, separatorStyle);
            }
        }
    }

    private void RememberConsumerStyle(DependencyObject element)
    {
        if (element.ReadLocalValue(StyleProperty) == DependencyProperty.UnsetValue || sharedStyledElements.Contains(element)) return;
        consumerStyledElements.Add(element);
    }

    private Style? FindSharedStyle(DependencyObject element)
    {
        var key = element is Separator ? "ContextMenu.SeparatorStyle" : "ContextMenu.ItemStyle";
        var targetType = element is Separator ? typeof(Separator) : typeof(MenuItem);
        return TryFindResource(key) as Style ?? Style?.Resources[targetType] as Style;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (Items.Count > 0 && IsItemAppearanceProperty(e.Property)) ApplyItemStyles(Items);
    }

    private static bool IsItemAppearanceProperty(DependencyProperty property) =>
        property == BackgroundProperty || property == ForegroundProperty || property == BorderBrushProperty ||
        property == BorderThicknessProperty || property == PaddingProperty || property == CornerRadiusProperty ||
        property == ItemPaddingProperty || property == IconSizeProperty || property == IconColumnWidthProperty ||
        property == TitleColumnWidthProperty || property == ValueColumnWidthProperty || property == InputGestureColumnWidthProperty ||
        property == ArrowColumnWidthProperty || property == SubmenuIconColumnWidthProperty ||
        property == SubmenuTitleColumnWidthProperty || property == SubmenuValueColumnWidthProperty ||
        property == SubmenuInputGestureColumnWidthProperty || property == SubmenuArrowColumnWidthProperty ||
        property == HoverBrushProperty || property == CheckedBrushProperty || property == SeparatorBrushProperty ||
        property == ItemBorderBrushProperty || property == ItemHoverBorderBrushProperty || property == ItemCheckedBorderBrushProperty ||
        property == ItemBorderThicknessProperty || property == ItemHoverBorderThicknessProperty ||
        property == ItemCheckedBorderThicknessProperty || property == ItemCornerRadiusProperty ||
        property == DisabledOpacityProperty || property == IsShadowEnabledProperty || property == ShadowColorProperty ||
        property == ShadowOpacityProperty || property == ShadowBlurRadiusProperty || property == ShadowDepthProperty ||
        property == SubmenuPlacementDirectionProperty || property == SubmenuHorizontalOffsetProperty ||
        property == SubmenuVerticalOffsetProperty;

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(ContextMenu),
        new PropertyMetadata(new CornerRadius(8)), IsValidCornerRadius);

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty ItemPaddingProperty = DependencyProperty.Register(
        nameof(ItemPadding), typeof(Thickness), typeof(ContextMenu),
        new PropertyMetadata(new Thickness(10, 7, 10, 7)), IsValidThickness);

    public Thickness ItemPadding
    {
        get => (Thickness)GetValue(ItemPaddingProperty);
        set => SetValue(ItemPaddingProperty, value);
    }

    public static readonly DependencyProperty ItemCornerRadiusProperty = DependencyProperty.Register(
        nameof(ItemCornerRadius), typeof(CornerRadius), typeof(ContextMenu),
        new PropertyMetadata(new CornerRadius(4)), IsValidCornerRadius);

    public CornerRadius ItemCornerRadius
    {
        get => (CornerRadius)GetValue(ItemCornerRadiusProperty);
        set => SetValue(ItemCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty ItemBorderBrushProperty = DependencyProperty.Register(
        nameof(ItemBorderBrush), typeof(Brush), typeof(ContextMenu), new PropertyMetadata(Brushes.Transparent));

    public Brush ItemBorderBrush
    {
        get => (Brush)GetValue(ItemBorderBrushProperty);
        set => SetValue(ItemBorderBrushProperty, value);
    }

    public static readonly DependencyProperty ItemHoverBorderBrushProperty = DependencyProperty.Register(
        nameof(ItemHoverBorderBrush), typeof(Brush), typeof(ContextMenu), new PropertyMetadata(Brushes.Transparent));

    public Brush ItemHoverBorderBrush
    {
        get => (Brush)GetValue(ItemHoverBorderBrushProperty);
        set => SetValue(ItemHoverBorderBrushProperty, value);
    }

    public static readonly DependencyProperty ItemCheckedBorderBrushProperty = DependencyProperty.Register(
        nameof(ItemCheckedBorderBrush), typeof(Brush), typeof(ContextMenu), new PropertyMetadata(Brushes.Transparent));

    public Brush ItemCheckedBorderBrush
    {
        get => (Brush)GetValue(ItemCheckedBorderBrushProperty);
        set => SetValue(ItemCheckedBorderBrushProperty, value);
    }

    public static readonly DependencyProperty ItemBorderThicknessProperty = DependencyProperty.Register(
        nameof(ItemBorderThickness), typeof(Thickness), typeof(ContextMenu),
        new PropertyMetadata(new Thickness(0)), IsValidThickness);

    public Thickness ItemBorderThickness
    {
        get => (Thickness)GetValue(ItemBorderThicknessProperty);
        set => SetValue(ItemBorderThicknessProperty, value);
    }

    public static readonly DependencyProperty ItemHoverBorderThicknessProperty = DependencyProperty.Register(
        nameof(ItemHoverBorderThickness), typeof(Thickness), typeof(ContextMenu),
        new PropertyMetadata(new Thickness(0)), IsValidThickness);

    public Thickness ItemHoverBorderThickness
    {
        get => (Thickness)GetValue(ItemHoverBorderThicknessProperty);
        set => SetValue(ItemHoverBorderThicknessProperty, value);
    }

    public static readonly DependencyProperty ItemCheckedBorderThicknessProperty = DependencyProperty.Register(
        nameof(ItemCheckedBorderThickness), typeof(Thickness), typeof(ContextMenu),
        new PropertyMetadata(new Thickness(0)), IsValidThickness);

    public Thickness ItemCheckedBorderThickness
    {
        get => (Thickness)GetValue(ItemCheckedBorderThicknessProperty);
        set => SetValue(ItemCheckedBorderThicknessProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize), typeof(double), typeof(ContextMenu), new PropertyMetadata(16d), IsFiniteNonNegative);

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconColumnWidthProperty = DependencyProperty.Register(
        nameof(IconColumnWidth), typeof(double), typeof(ContextMenu), new PropertyMetadata(26d), IsFiniteNonNegative);

    public double IconColumnWidth
    {
        get => (double)GetValue(IconColumnWidthProperty);
        set => SetValue(IconColumnWidthProperty, value);
    }

    public static readonly DependencyProperty TitleColumnWidthProperty = DependencyProperty.Register(
        nameof(TitleColumnWidth), typeof(GridLength), typeof(ContextMenu),
        new PropertyMetadata(new GridLength(1, GridUnitType.Star)), IsValidGridLength);

    public GridLength TitleColumnWidth
    {
        get => (GridLength)GetValue(TitleColumnWidthProperty);
        set => SetValue(TitleColumnWidthProperty, value);
    }

    public static readonly DependencyProperty ValueColumnWidthProperty = DependencyProperty.Register(
        nameof(ValueColumnWidth), typeof(GridLength), typeof(ContextMenu),
        new PropertyMetadata(GridLength.Auto), IsValidGridLength);

    public GridLength ValueColumnWidth
    {
        get => (GridLength)GetValue(ValueColumnWidthProperty);
        set => SetValue(ValueColumnWidthProperty, value);
    }

    public static readonly DependencyProperty InputGestureColumnWidthProperty = DependencyProperty.Register(
        nameof(InputGestureColumnWidth), typeof(GridLength), typeof(ContextMenu),
        new PropertyMetadata(GridLength.Auto), IsValidGridLength);

    public GridLength InputGestureColumnWidth
    {
        get => (GridLength)GetValue(InputGestureColumnWidthProperty);
        set => SetValue(InputGestureColumnWidthProperty, value);
    }

    public static readonly DependencyProperty SubmenuIconColumnWidthProperty = DependencyProperty.Register(
        nameof(SubmenuIconColumnWidth), typeof(double), typeof(ContextMenu), new PropertyMetadata(26d), IsFiniteNonNegative);

    public double SubmenuIconColumnWidth
    {
        get => (double)GetValue(SubmenuIconColumnWidthProperty);
        set => SetValue(SubmenuIconColumnWidthProperty, value);
    }

    public static readonly DependencyProperty SubmenuTitleColumnWidthProperty = DependencyProperty.Register(
        nameof(SubmenuTitleColumnWidth), typeof(GridLength), typeof(ContextMenu),
        new PropertyMetadata(new GridLength(1, GridUnitType.Star)), IsValidGridLength);

    public GridLength SubmenuTitleColumnWidth
    {
        get => (GridLength)GetValue(SubmenuTitleColumnWidthProperty);
        set => SetValue(SubmenuTitleColumnWidthProperty, value);
    }

    public static readonly DependencyProperty SubmenuValueColumnWidthProperty = DependencyProperty.Register(
        nameof(SubmenuValueColumnWidth), typeof(GridLength), typeof(ContextMenu),
        new PropertyMetadata(GridLength.Auto), IsValidGridLength);

    public GridLength SubmenuValueColumnWidth
    {
        get => (GridLength)GetValue(SubmenuValueColumnWidthProperty);
        set => SetValue(SubmenuValueColumnWidthProperty, value);
    }

    public static readonly DependencyProperty SubmenuInputGestureColumnWidthProperty = DependencyProperty.Register(
        nameof(SubmenuInputGestureColumnWidth), typeof(GridLength), typeof(ContextMenu),
        new PropertyMetadata(GridLength.Auto), IsValidGridLength);

    public GridLength SubmenuInputGestureColumnWidth
    {
        get => (GridLength)GetValue(SubmenuInputGestureColumnWidthProperty);
        set => SetValue(SubmenuInputGestureColumnWidthProperty, value);
    }

    public static readonly DependencyProperty SubmenuArrowColumnWidthProperty = DependencyProperty.Register(
        nameof(SubmenuArrowColumnWidth), typeof(double), typeof(ContextMenu), new PropertyMetadata(24d), IsFiniteNonNegative);

    public double SubmenuArrowColumnWidth
    {
        get => (double)GetValue(SubmenuArrowColumnWidthProperty);
        set => SetValue(SubmenuArrowColumnWidthProperty, value);
    }

    public static readonly DependencyProperty ArrowColumnWidthProperty = DependencyProperty.Register(
        nameof(ArrowColumnWidth), typeof(double), typeof(ContextMenu), new PropertyMetadata(24d), IsFiniteNonNegative);

    public double ArrowColumnWidth
    {
        get => (double)GetValue(ArrowColumnWidthProperty);
        set => SetValue(ArrowColumnWidthProperty, value);
    }

    public static readonly DependencyProperty HoverBrushProperty = DependencyProperty.Register(
        nameof(HoverBrush), typeof(Brush), typeof(ContextMenu), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(69, 72, 80))));

    public Brush HoverBrush
    {
        get => (Brush)GetValue(HoverBrushProperty);
        set => SetValue(HoverBrushProperty, value);
    }

    public static readonly DependencyProperty CheckedBrushProperty = DependencyProperty.Register(
        nameof(CheckedBrush), typeof(Brush), typeof(ContextMenu), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(156, 201, 255))));

    public Brush CheckedBrush
    {
        get => (Brush)GetValue(CheckedBrushProperty);
        set => SetValue(CheckedBrushProperty, value);
    }

    public static readonly DependencyProperty SeparatorBrushProperty = DependencyProperty.Register(
        nameof(SeparatorBrush), typeof(Brush), typeof(ContextMenu), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(76, 80, 88))));

    public Brush SeparatorBrush
    {
        get => (Brush)GetValue(SeparatorBrushProperty);
        set => SetValue(SeparatorBrushProperty, value);
    }

    public static readonly DependencyProperty DisabledOpacityProperty = DependencyProperty.Register(
        nameof(DisabledOpacity), typeof(double), typeof(ContextMenu), new PropertyMetadata(.45d),
        value => value is double number && double.IsFinite(number) && number is >= 0 and <= 1);

    public double DisabledOpacity
    {
        get => (double)GetValue(DisabledOpacityProperty);
        set => SetValue(DisabledOpacityProperty, value);
    }

    public static readonly DependencyProperty IsShadowEnabledProperty = DependencyProperty.Register(
        nameof(IsShadowEnabled), typeof(bool), typeof(ContextMenu), new PropertyMetadata(true));

    public bool IsShadowEnabled
    {
        get => (bool)GetValue(IsShadowEnabledProperty);
        set => SetValue(IsShadowEnabledProperty, value);
    }

    public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
        nameof(ShadowColor), typeof(Color), typeof(ContextMenu), new PropertyMetadata(Color.FromRgb(73, 73, 73)));

    public Color ShadowColor
    {
        get => (Color)GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(
        nameof(ShadowOpacity), typeof(double), typeof(ContextMenu), new PropertyMetadata(.5d),
        value => value is double number && double.IsFinite(number) && number is >= 0 and <= 1);

    public double ShadowOpacity
    {
        get => (double)GetValue(ShadowOpacityProperty);
        set => SetValue(ShadowOpacityProperty, value);
    }

    public static readonly DependencyProperty ShadowBlurRadiusProperty = DependencyProperty.Register(
        nameof(ShadowBlurRadius), typeof(double), typeof(ContextMenu), new PropertyMetadata(10d), IsFiniteNonNegative);

    public double ShadowBlurRadius
    {
        get => (double)GetValue(ShadowBlurRadiusProperty);
        set => SetValue(ShadowBlurRadiusProperty, value);
    }

    public static readonly DependencyProperty ShadowDepthProperty = DependencyProperty.Register(
        nameof(ShadowDepth), typeof(double), typeof(ContextMenu), new PropertyMetadata(0d), IsFinite);

    public double ShadowDepth
    {
        get => (double)GetValue(ShadowDepthProperty);
        set => SetValue(ShadowDepthProperty, value);
    }

    private static bool IsFinite(object value) => value is double number && double.IsFinite(number);
    private static bool IsFiniteNonNegative(object value) => value is double number && double.IsFinite(number) && number >= 0;

    private static bool IsValidThickness(object value) => value is Thickness thickness &&
        IsFiniteNonNegative(thickness.Left) && IsFiniteNonNegative(thickness.Top) &&
        IsFiniteNonNegative(thickness.Right) && IsFiniteNonNegative(thickness.Bottom);

    private static bool IsValidCornerRadius(object value) => value is CornerRadius radius &&
        IsFiniteNonNegative(radius.TopLeft) && IsFiniteNonNegative(radius.TopRight) &&
        IsFiniteNonNegative(radius.BottomRight) && IsFiniteNonNegative(radius.BottomLeft);

    private static bool IsValidGridLength(object value) => value is GridLength length &&
        (length.IsAuto || double.IsFinite(length.Value) && length.Value >= 0);

    private static bool IsFiniteNonNegative(double value) => double.IsFinite(value) && value >= 0;
}
