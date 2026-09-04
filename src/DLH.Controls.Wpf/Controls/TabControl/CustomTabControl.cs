using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DLH.Controls.Wpf;

public partial class CustomTabControl : TabControl
{
    private Path? surface;
    private Path? hoverSurface;
    private TabItem? hoveredItem;
    private FrameworkElement? body;
    private ScrollViewer? headers;
    private (Rect Body, Rect Tab, Rect Hover, CornerRadius Radius, double Stroke)? lastShape;

    static CustomTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomTabControl), new FrameworkPropertyMetadata(typeof(CustomTabControl)));
        TabStripPlacementProperty.OverrideMetadata(typeof(CustomTabControl),
            new FrameworkPropertyMetadata(Dock.Top, FrameworkPropertyMetadataOptions.AffectsMeasure, (owner, _) =>
            {
                var control = (CustomTabControl)owner;
                control.CancelTabDrag();
                control.ResetDragPreview();
                control.ConfigurePlacement();
            }));
    }

    public CustomTabControl()
    {
        CommandBindings.Add(new CommandBinding(CloseTab,
            (_, e) => { if (ResolveCloseItem(e.Parameter) is { } item) RequestCloseTab(item); e.Handled = true; },
            (_, e) => { e.CanExecute = CanRequestClose(ResolveCloseItem(e.Parameter)); e.Handled = true; }));
        LayoutUpdated += (_, _) => UpdateSurface();
        Unloaded += (_, _) => CancelTabDrag();
    }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(CustomTabControl), new FrameworkPropertyMetadata(new CornerRadius(12), FrameworkPropertyMetadataOptions.AffectsArrange),
        value => value is CornerRadius radius && double.IsFinite(radius.TopLeft) && radius.TopLeft >= 0 &&
            radius.TopLeft == radius.TopRight && radius.TopLeft == radius.BottomRight && radius.TopLeft == radius.BottomLeft);
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly DependencyProperty TabSpacingProperty = DependencyProperty.Register(
        nameof(TabSpacing), typeof(double), typeof(CustomTabControl), new PropertyMetadata(0d),
        value => value is double number && double.IsFinite(number) && number >= 0);
    public double TabSpacing { get => (double)GetValue(TabSpacingProperty); set => SetValue(TabSpacingProperty, value); }

    /// <summary>Left header inset; NaN automatically reserves room for the corner transitions.</summary>
    public static readonly DependencyProperty HeaderIndentProperty = DependencyProperty.Register(
        nameof(HeaderIndent), typeof(double), typeof(CustomTabControl),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsArrange),
        value => value is double number && (double.IsNaN(number) || double.IsFinite(number) && number >= 0));
    public double HeaderIndent { get => (double)GetValue(HeaderIndentProperty); set => SetValue(HeaderIndentProperty, value); }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is TabItem;
    protected override DependencyObject GetContainerForItemOverride() => new CustomTabItem();

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        if (e.Source == this) RevealSelectedTab();
    }

    public override void OnApplyTemplate()
    {
        CancelTabDrag();
        base.OnApplyTemplate();
        surface = GetTemplateChild("PART_Surface") as Path;
        hoverSurface = GetTemplateChild("PART_HoverSurface") as Path;
        body = GetTemplateChild("PART_Body") as FrameworkElement;
        headers = GetTemplateChild("PART_HeaderScrollViewer") as ScrollViewer;
        lastShape = null;
        ConfigurePlacement();
        RevealSelectedTab();
    }

    private void RevealSelectedTab() => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
    {
        if (ItemContainerGenerator.ContainerFromIndex(SelectedIndex) is TabItem tab)
            tab.BringIntoView();
        UpdateSurface();
    }));

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var item = e.OriginalSource is DependencyObject source
            ? ItemsControl.ContainerFromElement(this, source) as TabItem : null;
        // Content inside a selected tab is not a header hover target.
        if (item?.IsSelected == true || dragging) item = null;
        if (hoveredItem == item) return;
        hoveredItem = item;
        UpdateSurface();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (!dragging) CancelTabDrag();
        hoveredItem = null;
        UpdateSurface();
    }

    private void UpdateSurface()
    {
        if (surface is null || body is null || headers is null || body.ActualWidth <= 0 || body.ActualHeight <= 0) return;
        if (dragging && headerMotions.Values.Any(motion => motion.Root.RenderSize != motion.InitialSize))
        {
            // A title, icon or font change invalidates the bitmap and insertion bounds captured for this drag.
            // End the session instead of allowing a drop based on stale geometry.
            CancelTabDrag();
            return;
        }
        ConfigurePlacement();
        // Reserve room for both the panel corner and the concave tab transition.
        var inset = double.IsNaN(HeaderIndent) ? 2 * CornerRadius.TopLeft : HeaderIndent;
        var headerMargin = IsVerticalTabStrip ? new Thickness(0, inset, 0, 2 * CornerRadius.TopRight) : new Thickness(inset, 0, 2 * CornerRadius.TopRight, 0);
        if (headers.Margin != headerMargin)
        {
            headers.Margin = headerMargin;
            return;
        }
        var bodyRect = body.TransformToVisual(surface).TransformBounds(new Rect(body.RenderSize));
        Rect HeaderBounds(TabItem? item)
        {
            if (item is null || ItemsControl.ItemsControlFromItemContainer(item) != this ||
                item.Template?.FindName("Surface", item) is not FrameworkElement header || header.ActualWidth <= 0)
                return Rect.Empty;
            var bounds = header.TransformToVisual(surface).TransformBounds(new Rect(header.RenderSize));
            // The surface stays attached to its layout position while header content previews move.
            if (headerMotions.TryGetValue(item, out var motion)) bounds.Offset(-motion.Offset.X, -motion.Offset.Y);
            if (headers.Template.FindName("PART_ScrollContentPresenter", headers) is FrameworkElement viewport)
                bounds.Intersect(viewport.TransformToVisual(surface).TransformBounds(new Rect(viewport.RenderSize)));

            return bounds;
        }
        var tabRect = dragging && dragPreview is not null ? Rect.Empty : HeaderBounds(ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as TabItem);
        var hoverRect = HeaderBounds(hoveredItem is { IsSelected: false, IsEnabled: true } ? hoveredItem : null);
        var thickness = BorderThickness;
        var stroke = Math.Max(Math.Max(thickness.Left, thickness.Right), Math.Max(thickness.Top, thickness.Bottom));
        var state = (bodyRect, tabRect, hoverRect, CornerRadius, stroke);
        if (lastShape == state) return;
        lastShape = state;
        Geometry outline = OrientedOutline(bodyRect, tabRect);
        outline.Freeze();
        if (hoverSurface is not null)
        {
            // Paint the same continuous silhouette behind the active surface.
            // Its body is covered by the active panel; no rectangular hover corner can cover the selected tab.
            hoverSurface.Data = hoverRect.IsEmpty ? Geometry.Empty : OrientedOutline(bodyRect, hoverRect);
        }
        surface.Data = outline;
        surface.StrokeThickness = stroke;
    }

    private static Geometry RoundedRect(Rect rect, CornerRadius radius, Rect tab)
    {
        // One effective radius for all visible curves; shrink uniformly if space is tight.
        var effectiveRadius = Math.Min(radius.TopLeft, Math.Min(rect.Width, rect.Height) / 2);
        var hasTab = !tab.IsEmpty && tab.Width > 0 && tab.Height > 0;
        var leftSpace = hasTab ? Math.Max(0, tab.Left - rect.Left) : 0;
        var rightSpace = hasTab ? Math.Max(0, rect.Right - tab.Right) : 0;
        if (hasTab)
        {
            effectiveRadius = Math.Min(effectiveRadius, Math.Min(tab.Width, tab.Height) / 2);
            if (leftSpace > 0.01) effectiveRadius = Math.Min(effectiveRadius, leftSpace / 2);
            if (rightSpace > 0.01) effectiveRadius = Math.Min(effectiveRadius, rightSpace / 2);
        }
        var tl = hasTab && leftSpace <= 0.01 ? 0 : effectiveRadius;
        var tr = hasTab && rightSpace <= 0.01 ? 0 : effectiveRadius;
        var br = effectiveRadius;
        var bl = effectiveRadius;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left + tl, rect.Top), true, true);
            if (!tab.IsEmpty && tab.Width > 0 && tab.Height > 0)
            {
                var topLeft = effectiveRadius;
                var topRight = effectiveRadius;
                var joinLeft = leftSpace <= 0.01 ? 0 : effectiveRadius;
                var joinRight = rightSpace <= 0.01 ? 0 : effectiveRadius;
                context.LineTo(new Point(tab.Left - joinLeft, rect.Top), true, false);
                Corner(context, new Point(tab.Left, rect.Top - joinLeft), joinLeft, SweepDirection.Counterclockwise);
                context.LineTo(new Point(tab.Left, tab.Top + topLeft), true, false);
                Corner(context, new Point(tab.Left + topLeft, tab.Top), topLeft);
                context.LineTo(new Point(tab.Right - topRight, tab.Top), true, false);
                Corner(context, new Point(tab.Right, tab.Top + topRight), topRight);
                context.LineTo(new Point(tab.Right, rect.Top - joinRight), true, false);
                Corner(context, new Point(tab.Right + joinRight, rect.Top), joinRight, SweepDirection.Counterclockwise);
            }
            context.LineTo(new Point(rect.Right - tr, rect.Top), true, false);
            Corner(context, new Point(rect.Right, rect.Top + tr), tr);
            context.LineTo(new Point(rect.Right, rect.Bottom - br), true, false);
            Corner(context, new Point(rect.Right - br, rect.Bottom), br);
            context.LineTo(new Point(rect.Left + bl, rect.Bottom), true, false);
            Corner(context, new Point(rect.Left, rect.Bottom - bl), bl);
            context.LineTo(new Point(rect.Left, rect.Top + tl), true, false);
            Corner(context, new Point(rect.Left + tl, rect.Top), tl);
        }
        geometry.Freeze();
        return geometry;
    }

    private static void Corner(StreamGeometryContext context, Point point, double radius, SweepDirection sweep = SweepDirection.Clockwise)
    {
        if (radius > 0) context.ArcTo(point, new Size(radius, radius), 0, false, sweep, true, false);
        else context.LineTo(point, true, false);
    }
}

public class CustomTabItem : TabItem
{
    static CustomTabItem() => DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CustomTabItem), new FrameworkPropertyMetadata(typeof(CustomTabItem)));
}
