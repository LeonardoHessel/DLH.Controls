using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DLH.Controls.Wpf.Screenshots;

public partial class ScrollBarScreenshotWindow : Window
{
    private bool synchronizingScrollBars;

    public ScrollBarScreenshotWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        TextViewport.ScrollChanged += OnViewportScrolled;
        HorizontalBar.ValueChanged += OnHorizontalValueChanged;
        VerticalBar.ValueChanged += OnVerticalValueChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TextViewport.ScrollToHorizontalOffset(0);
        TextViewport.ScrollToVerticalOffset(0);
        SynchronizeScrollBars();
    }

    private void OnViewportScrolled(object sender, ScrollChangedEventArgs e) => SynchronizeScrollBars();

    private void OnHorizontalValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!synchronizingScrollBars)
            TextViewport.ScrollToHorizontalOffset(e.NewValue);
    }

    private void OnVerticalValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!synchronizingScrollBars)
            TextViewport.ScrollToVerticalOffset(e.NewValue);
    }

    private void SynchronizeScrollBars()
    {
        synchronizingScrollBars = true;
        try
        {
            HorizontalBar.Maximum = TextViewport.ScrollableWidth;
            HorizontalBar.ViewportSize = TextViewport.ViewportWidth;
            HorizontalBar.Value = TextViewport.HorizontalOffset;

            VerticalBar.Maximum = TextViewport.ScrollableHeight;
            VerticalBar.ViewportSize = TextViewport.ViewportHeight;
            VerticalBar.Value = TextViewport.VerticalOffset;
        }
        finally
        {
            synchronizingScrollBars = false;
        }
    }
}
