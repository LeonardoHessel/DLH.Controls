using System.Configuration;
using System.Data;
using System.Windows;
using DLH.Controls.Wpf.Demo.Screenshots;

namespace DLH.Controls.Wpf.Demo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Window window = (e.Args.Length > 0 ? e.Args[0] : null) switch
        {
            "tabcontrol" => new TabControlScreenshotWindow(),
            "datagridview" => new DataGridViewScreenshotWindow(),
            "scrollbar" => new ScrollBarScreenshotWindow(),
            "contextmenu" => new ContextMenuScreenshotWindow(),
            _ => new DataGridViewDemoWindow()
        };
        window.Show();
    }
}
