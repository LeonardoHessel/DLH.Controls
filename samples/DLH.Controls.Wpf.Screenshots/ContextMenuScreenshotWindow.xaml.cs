using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DLH.Controls.Wpf.Screenshots;

public partial class ContextMenuScreenshotWindow : Window
{
    public ContextMenuScreenshotWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Activated += (_, _) => Dispatcher.BeginInvoke(OpenMenu, DispatcherPriority.ContextIdle);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => OpenMenu();

    private void OpenMenu()
    {
        Menu.CollapseAll();
        Menu.PlacementTarget = TargetArea;
        Menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
        Menu.IsOpen = true;
        var submenuTimer = new DispatcherTimer(DispatcherPriority.Loaded)
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        submenuTimer.Tick += (_, _) =>
        {
            submenuTimer.Stop();
            Menu.CollapseAll();
            ExportItem.IsSubmenuOpen = true;
        };
        submenuTimer.Start();
    }
}

