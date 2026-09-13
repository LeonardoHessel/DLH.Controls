using System.Windows;
using System.Windows.Controls;

namespace DLH.Controls.Wpf.Screenshots;

public partial class ScreenshotLauncherWindow : Window
{
    public ScreenshotLauncherWindow() => InitializeComponent();

    private void OpenScreenshot_Click(object sender, RoutedEventArgs e)
    {
        Window window = ((Button)sender).Tag switch
        {
            "tabcontrol" => new TabControlScreenshotWindow(),
            "datagridview" => new DataGridViewScreenshotWindow(),
            "scrollbar" => new ScrollBarScreenshotWindow(),
            "contextmenu" => new ContextMenuScreenshotWindow(),
            _ => throw new InvalidOperationException("Composição de captura desconhecida.")
        };

        window.Owner = this;
        window.Show();
    }
}

