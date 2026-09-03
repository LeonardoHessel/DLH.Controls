using System.Windows;
using System.Windows.Media;

namespace CustomTabControl.Demo;

public partial class MainWindow : Window
{
    private bool light;
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DemoViewModel();
    }
    private void ToggleTheme(object sender, RoutedEventArgs e)
    {
        light = !light;
        var colors = new Dictionary<string, string>
        {
            ["Demo.Background"] = light ? "#E9ECF1" : "#27292E",
            ["Tabs.Surface"] = light ? "#FFFFFF" : "#35373C",
            ["Tabs.Hover"] = light ? "#DCE3ED" : "#454850",
            ["Tabs.Text"] = light ? "#202734" : "#F2F3F5",
            ["Tabs.Muted"] = light ? "#596477" : "#BCC0CA",
            ["Tabs.Edge"] = light ? "#C9D1DD" : "#4C5058",
            ["Tabs.Focus"] = light ? "#225AA6" : "#9CC9FF"
        };
        foreach (var (key, value) in colors)
            Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    }
}
