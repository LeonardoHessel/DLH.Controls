using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using ControlsScrollBar = DLH.Controls.Wpf.ScrollBar;
using ControlsDataGridView = DLH.Controls.Wpf.DataGridView;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("SharedControls")]
public sealed class SharedControlsTests
{
    [STATestMethod]
    public void ScrollBarDefaultsAndValidationAreStable()
    {
        var bar = new ControlsScrollBar();
        Assert.AreEqual(10d, bar.Thickness);
        Assert.AreEqual(new CornerRadius(5), bar.CornerRadius);
        Assert.IsFalse(bar.ShowButtons);
        Assert.IsFalse(bar.IsShadowEnabled);
        Assert.ThrowsExactly<ArgumentException>(() => bar.Thickness = 0);
        Assert.ThrowsExactly<ArgumentException>(() => bar.CornerRadius = new CornerRadius(2, 3, 2, 3));
        Assert.ThrowsExactly<ArgumentException>(() => bar.ShadowOpacity = 2);
    }

    [STATestMethod]
    public void ScrollBarUsesOrientationThicknessAndLimitsRenderedRadius()
    {
        var bar = new ControlsScrollBar { Thickness = 8, CornerRadius = new CornerRadius(20), Height = 160 };
        var window = Arrange(bar, 8, 160);
        try
        {
            var surface = (Border)bar.Template.FindName("TrackSurface", bar)!;
            Assert.AreEqual(8d, bar.ActualWidth);
            Assert.AreEqual(new CornerRadius(4), surface.CornerRadius);
            bar.Orientation = Orientation.Horizontal;
            bar.Thickness = 12;
            bar.Width = 180;
            bar.Measure(new Size(180, 12)); bar.Arrange(new Rect(0, 0, 180, 12)); bar.UpdateLayout();
            surface = (Border)bar.Template.FindName("TrackSurface", bar)!;
            Assert.AreEqual(12d, bar.ActualHeight);
            Assert.AreEqual(new CornerRadius(6), surface.CornerRadius);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ScrollBarButtonsAndShadowAreOptional()
    {
        var bar = new ControlsScrollBar { Height = 160, ShowButtons = true, IsShadowEnabled = true };
        var window = Arrange(bar, 10, 160);
        try
        {
            Assert.AreEqual(Visibility.Visible, ((FrameworkElement)bar.Template.FindName("DecreaseButton", bar)!).Visibility);
            Assert.IsInstanceOfType<DropShadowEffect>(((Border)bar.Template.FindName("TrackSurface", bar)!).Effect);
            bar.ShowButtons = false; bar.IsShadowEnabled = false; bar.UpdateLayout();
            Assert.AreEqual(Visibility.Collapsed, ((FrameworkElement)bar.Template.FindName("DecreaseButton", bar)!).Visibility);
            Assert.IsNull(((Border)bar.Template.FindName("TrackSurface", bar)!).Effect);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void DataGridViewUsesTheSharedScrollBars()
    {
        var grid = new ControlsDataGridView { Width = 300, Height = 180, ScrollBarThickness = 9 };
        grid.Columns.Add(new DataGridTextColumn { Header = "Valor", Width = 500 });
        grid.ItemsSource = Enumerable.Range(1, 30).Select(number => new { Valor = number });
        var window = Arrange(grid, 300, 180);
        try
        {
            var viewer = (System.Windows.Controls.ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ApplyTemplate();
            var vertical = (ControlsScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer)!;
            var horizontal = (ControlsScrollBar)viewer.Template.FindName("PART_HorizontalScrollBar", viewer)!;
            Assert.AreEqual(9d, vertical.Thickness);
            Assert.AreEqual(9d, horizontal.Thickness);
            Assert.AreSame(grid.ScrollBarThumbBrush, vertical.ThumbBrush);
        }
        finally { window.Close(); }
    }

    private static Window Arrange(FrameworkElement element, double width, double height)
    {
        var window = new Window { Content = element, Width = width, Height = height };
        element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height));
        element.ApplyTemplate(); element.UpdateLayout();
        return window;
    }
}

