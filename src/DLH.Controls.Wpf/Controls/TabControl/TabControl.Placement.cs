using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

public partial class TabControl
{
    private Dock? appliedPlacement;
    private Grid? appliedLayout;

    private void ConfigurePlacement()
    {
        if (GetTemplateChild("PART_Layout") is not Grid grid || headers is null || body is null ||
            GetTemplateChild("PART_HeaderPanel") is not StackPanel panel) return;
        if (appliedPlacement == TabStripPlacement && ReferenceEquals(appliedLayout, grid)) return;
appliedPlacement = TabStripPlacement;
        appliedLayout = grid;
var vertical = IsVerticalTabStrip;
        var trailing = TabStripPlacement is Dock.Bottom or Dock.Right;
        panel.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
        if (GetTemplateChild("PART_HeaderFlow") is StackPanel flow)
            flow.Orientation = panel.Orientation;
        if (GetTemplateChild("PART_AddTabButton") is Button addButton)
            addButton.Margin = vertical ? new Thickness(0, 2, 0, 0) : new Thickness(2, 0, 0, 0);
        headers.HorizontalScrollBarVisibility = vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        headers.VerticalScrollBarVisibility = vertical ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        grid.RowDefinitions[0].Height = vertical ? new GridLength(1, GridUnitType.Star) : trailing ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        grid.RowDefinitions[1].Height = vertical ? new GridLength(0) : trailing ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        grid.ColumnDefinitions[0].Width = !vertical ? new GridLength(1, GridUnitType.Star) : trailing ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        grid.ColumnDefinitions[1].Width = !vertical ? new GridLength(0) : trailing ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(headers, vertical ? 0 : trailing ? 1 : 0);
        Grid.SetRow(body, vertical ? 0 : trailing ? 0 : 1);
        Grid.SetColumn(headers, !vertical ? 0 : trailing ? 1 : 0);
        Grid.SetColumn(body, !vertical ? 0 : trailing ? 0 : 1);
        lastShape = null;
    }

    private Geometry OrientedOutline(Rect bodyBounds, Rect tabBounds)
    {
        // Build a canonical top-tab outline, then map the complete contour to its side.
        var matrix = TabStripPlacement switch
        {
            Dock.Bottom => new Matrix(1, 0, 0, -1, 0, ActualHeight),
            Dock.Left => new Matrix(0, 1, 1, 0, 0, 0),
            Dock.Right => new Matrix(0, 1, -1, 0, ActualWidth, 0),
            _ => Matrix.Identity
        };
        var inverse = matrix;
        inverse.Invert();
        var normalize = new MatrixTransform(inverse);
        var normalizedBody = normalize.TransformBounds(bodyBounds);
        var normalizedTab = tabBounds.IsEmpty ? Rect.Empty : normalize.TransformBounds(tabBounds);
        if (!normalizedTab.IsEmpty) normalizedTab.Height = Math.Max(0, normalizedBody.Top - normalizedTab.Top);
        var radius = CornerRadius;
        radius = TabStripPlacement switch
        {
            Dock.Bottom => new CornerRadius(radius.BottomLeft, radius.BottomRight, radius.TopRight, radius.TopLeft),
            Dock.Left => new CornerRadius(radius.TopLeft, radius.BottomLeft, radius.BottomRight, radius.TopRight),
            Dock.Right => new CornerRadius(radius.TopRight, radius.BottomRight, radius.BottomLeft, radius.TopLeft),
            _ => radius
        };
        var geometry = RoundedRect(normalizedBody, radius, normalizedTab).Clone();
        geometry.Transform = new MatrixTransform(matrix);
        geometry.Freeze();
        return geometry;
    }
}

