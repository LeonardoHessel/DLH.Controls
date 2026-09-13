using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DLH.Controls.Wpf.Demo.Screenshots;

public partial class ContextMenuScreenshotWindow : Window
{
    public ContextMenuScreenshotWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Menu.PlacementTarget = TargetArea;
        Menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
        Menu.IsOpen = true;
        Dispatcher.BeginInvoke(() => ExportItem.IsSubmenuOpen = true, DispatcherPriority.Loaded);
    }
}
