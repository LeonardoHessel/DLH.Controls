using System.Windows;
using System.Windows.Media;

namespace CustomTabControl.Demo;

public partial class MainWindow : Window
{
    private int themeIndex;
    private static readonly string[] ThemeNames = ["Escuro", "Claro", "Cinza e laranja"];
    // Background, surface, hover, text, muted text, border, focus, accent, input.
    private static readonly string[][] ThemeColors =
    [
        ["#27292E", "#35373C", "#454850", "#F2F3F5", "#BCC0CA", "#4C5058", "#9CC9FF", "#F2F3F5", "#27292E"],
        ["#E9ECF1", "#FFFFFF", "#DCE3ED", "#202734", "#596477", "#C9D1DD", "#225AA6", "#202734", "#E9ECF1"],
        ["#3D3D3B", "#5D5D5D", "#686868", "#E0E0E0", "#D0D0D0", "#585858", "#FF8A00", "#FF8A00", "#606060"]
    ];
    private static readonly string[] ColorKeys =
        ["Demo.Background", "Tabs.Surface", "Tabs.Hover", "Tabs.Text", "Tabs.Muted", "Tabs.Edge", "Tabs.Focus", "Demo.Accent", "Demo.Input"];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DemoViewModel();
        ApplyTheme();
    }

    private void ToggleTheme(object sender, RoutedEventArgs e)
    {
        themeIndex = (themeIndex + 1) % ThemeColors.Length;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        for (var i = 0; i < ColorKeys.Length; i++)
            Resources[ColorKeys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemeColors[themeIndex][i]));
        ThemeName.Text = ThemeNames[themeIndex];
    }
}
