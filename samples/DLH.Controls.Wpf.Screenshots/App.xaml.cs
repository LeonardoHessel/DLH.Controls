using System.Windows;

namespace DLH.Controls.Wpf.Screenshots;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Window window = (e.Args.FirstOrDefault()?.ToLowerInvariant()) switch
        {
            "tabcontrol" => new TabControlScreenshotWindow(),
            "datagridview" => new DataGridViewScreenshotWindow(),
            "scrollbar" => new ScrollBarScreenshotWindow(),
            "contextmenu" => new ContextMenuScreenshotWindow(),
            _ => new ScreenshotLauncherWindow()
        };

        MainWindow = window;
        window.Show();
    }
}

