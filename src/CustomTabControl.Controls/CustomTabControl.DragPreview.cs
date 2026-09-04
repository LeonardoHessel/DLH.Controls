using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace CustomTabControl.Controls;

public partial class CustomTabControl
{
    private sealed record HeaderMotion(FrameworkElement Root, Transform Original, TranslateTransform Offset);
    private readonly Dictionary<TabItem, HeaderMotion> headerMotions = new();
    private Border? dragPreview;
    private Canvas? previewLayer;
    private Point grabOffset;

    private bool IsVerticalTabStrip => TabStripPlacement is Dock.Left or Dock.Right;
    private DependencyProperty PreviewAxis => IsVerticalTabStrip ? TranslateTransform.YProperty : TranslateTransform.XProperty;
    private int previewTarget = -2;
    private bool committingDrop;

    private void BeginDragPreview()
    {
        ResetDragPreview();
        if (!IsDragPreviewEnabled || dragCandidate is null || GetTemplateChild("PART_DragPreviewLayer") is not Canvas layer) return;
        previewLayer = layer;
        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is not TabItem tab ||
                tab.Template.FindName("Root", tab) is not FrameworkElement root) continue;
            var offset = new TranslateTransform();
            var original = root.RenderTransform;
            root.SetCurrentValue(RenderTransformProperty, new TransformGroup { Children = { original, offset } });
            headerMotions[tab] = new HeaderMotion(root, original, offset);
        }
        if (!headerMotions.TryGetValue(dragCandidate, out var source)) return;
        var width = source.Root.ActualWidth;
        var height = source.Root.ActualHeight;
        if (width <= 0 || height <= 0) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var snapshot = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(width * dpi.DpiScaleX)),
            Math.Max(1, (int)Math.Ceiling(height * dpi.DpiScaleY)), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        snapshot.Render(source.Root);
        snapshot.Freeze();
        dragPreview = new Border
        {
            Width = width, Height = height, CornerRadius = CornerRadius, Background = Background,
            BorderBrush = BorderBrush, BorderThickness = new Thickness(1), IsHitTestVisible = false,
            Opacity = DragPreviewOpacity, Child = new Image { Source = snapshot, Stretch = Stretch.Fill },
            Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 3, Opacity = 0.3 }
        };
        layer.Children.Add(dragPreview);
        var bounds = dragCandidate.TransformToVisual(this).TransformBounds(new Rect(dragCandidate.RenderSize));
        grabOffset = new Point(Math.Clamp(dragOrigin.X - bounds.Left, 0, width), Math.Clamp(dragOrigin.Y - bounds.Top, 0, height));

        source.Root.BeginAnimation(OpacityProperty, new DoubleAnimation(0, IsAnimationEnabled ? DragAnimationDuration : TimeSpan.Zero));
        hoveredItem = null;
        UpdateSurface();
    }

    private Point ConstrainPreviewPosition(Point point, Size size)
    {
        var viewport = HeaderViewport();
        if (viewport.IsEmpty) viewport = new Rect(RenderSize);
        return IsVerticalTabStrip
            ? new Point(viewport.Left, Math.Clamp(point.Y - grabOffset.Y, viewport.Top, Math.Max(viewport.Top, viewport.Bottom - size.Height)))
            : new Point(Math.Clamp(point.X - grabOffset.X, viewport.Left, Math.Max(viewport.Left, viewport.Right - size.Width)), viewport.Top);
    }

    private void UpdateDragPreview(Point point, int target)
    {
        if (dragPreview is null || previewLayer is null || dragCandidate is null) return;
        var position = TranslatePoint(ConstrainPreviewPosition(point, new Size(dragPreview.Width, dragPreview.Height)), previewLayer);
        Canvas.SetLeft(dragPreview, position.X);
        Canvas.SetTop(dragPreview, position.Y);
        if (previewTarget == target) return;
        previewTarget = target;
        var sourceIndex = ItemContainerGenerator.IndexFromContainer(dragCandidate);
        var width = IsVerticalTabStrip ? dragCandidate.ActualHeight : dragCandidate.ActualWidth;
        foreach (var (tab, motion) in headerMotions)
        {
            var index = ItemContainerGenerator.IndexFromContainer(tab);
            var shift = target >= 0 && index != sourceIndex
                ? sourceIndex < target && index > sourceIndex && index <= target ? -width
                : target < sourceIndex && index >= target && index < sourceIndex ? width : 0 : 0;
            if (!IsAnimationEnabled || DragAnimationDuration == TimeSpan.Zero)
            {
                motion.Offset.BeginAnimation(PreviewAxis, null);
                motion.Offset.SetValue(PreviewAxis, shift);
                continue;
            }
            motion.Offset.BeginAnimation(PreviewAxis, new DoubleAnimation(shift, DragAnimationDuration)
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
    }

    private void EndDragPreview()
    {
        if (dragPreview is not null) previewLayer?.Children.Remove(dragPreview);
        dragPreview = null;
        if (committingDrop || !IsAnimationEnabled || DragAnimationDuration == TimeSpan.Zero) { ResetDragPreview(); return; }
        foreach (var (tab, motion) in headerMotions.ToArray())
        {
            motion.Root.BeginAnimation(OpacityProperty, null);
            var animation = new DoubleAnimation(0, DragAnimationDuration)
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            animation.Completed += (_, _) =>
            {
                // A new drag may already own this element by the time the old animation completes.
                if (motion.Root.RenderTransform is TransformGroup group && group.Children.Contains(motion.Offset))
                {
                    motion.Root.SetCurrentValue(RenderTransformProperty, motion.Original);
                    motion.Offset.BeginAnimation(TranslateTransform.XProperty, null);
            motion.Offset.BeginAnimation(TranslateTransform.YProperty, null);
                    if (headerMotions.TryGetValue(tab, out var current) && ReferenceEquals(current, motion)) headerMotions.Remove(tab);
                }
            };
            motion.Offset.BeginAnimation(PreviewAxis, animation);
        }
        previewTarget = -2;
    }

    private void ResetDragPreview()
    {
        if (dragPreview is not null) previewLayer?.Children.Remove(dragPreview);
        dragPreview = null;
        foreach (var motion in headerMotions.Values)
        {
            motion.Offset.BeginAnimation(TranslateTransform.XProperty, null);
            motion.Offset.BeginAnimation(TranslateTransform.YProperty, null);
            motion.Root.BeginAnimation(OpacityProperty, null);
            motion.Root.SetCurrentValue(RenderTransformProperty, motion.Original);
        }
        headerMotions.Clear();
        previewLayer = null;
        previewTarget = -2;
    }
}
