using System.IO;
using System.Text.Json;
using DLH.Controls.Wpf;
using System.Windows;
using System.Windows.Media;

namespace DLH.Controls.Wpf.Demo;

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
        foreach (var tabs in new[] { SideTabs, SimpleTabs })
        {
            tabs.ShowCloseButtons = true;
            tabs.CornerRadius = DynamicTabs.CornerRadius;
            tabs.TabReordered += OnTabReordered;
            tabs.TabClosing += ConfirmTabClosing;
        }
        Loaded += (_, _) => { if (File.Exists(LayoutPath)) ReadOrganization(false); ReadConfiguration(); };
        ApplyTheme();
    }

    private void ToggleAllAnimation(object sender, RoutedEventArgs e)
    {
        if (SimpleTabs is null) return;
        foreach (var tabs in new[] { DynamicTabs, SideTabs, SimpleTabs }) tabs.IsAnimationEnabled = ((System.Windows.Controls.CheckBox)sender).IsChecked == true;
    }
    private void ToggleAllClosing(object sender, RoutedEventArgs e)
    {
        if (SimpleTabs is null) return;
        foreach (var tabs in new[] { DynamicTabs, SideTabs, SimpleTabs }) tabs.CanCloseTabs = ((System.Windows.Controls.CheckBox)sender).IsChecked == true;
    }
    private void ToggleAllPreview(object sender, RoutedEventArgs e)
    {
        if (SimpleTabs is null) return;
        foreach (var tabs in new[] { DynamicTabs, SideTabs, SimpleTabs }) tabs.IsDragPreviewEnabled = ((System.Windows.Controls.CheckBox)sender).IsChecked == true;
    }
    private void OpenTabSettings(object sender, RoutedEventArgs e) => new TabSettingsWindow([DynamicTabs, SideTabs, SimpleTabs]) { Owner = this }.ShowDialog();

    private void ToggleTheme(object sender, RoutedEventArgs e)
    {
        ResetThemeBrushes();
        themeIndex = (themeIndex + 1) % ThemeColors.Length;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        for (var i = 0; i < ColorKeys.Length; i++)
            Resources[ColorKeys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemeColors[themeIndex][i]));
        ThemeName.Text = ThemeNames[themeIndex];
    }
    private static string LayoutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomTabControl.Demo", "layout.json");

    private void ConfirmTabClosing(object? sender, TabClosingEventArgs e)
    {
        if (e.Item is TabDocument { Notes.Length: > 0 } document)
            e.Cancel = MessageBox.Show(this, $"A aba '{document.Header}' tem anotações. Fechar e descartar essas anotações?", "Fechar aba",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK;
    }
    private void CloseSelectedTab(object sender, RoutedEventArgs e)
    {
        if (DynamicTabs.SelectedItem is { } item) DynamicTabs.RequestCloseTab(item);
    }
    private void OnTabReordered(object? sender, TabReorderedEventArgs e) =>
        InteractionStatus.Text = $"Aba movida da posição {e.OldIndex + 1} para {e.NewIndex + 1}.";

    private void SaveOrganization(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LayoutPath)!);
            var temporary = LayoutPath + ".tmp";
            using (var stream = File.Create(temporary)) JsonSerializer.Serialize(stream, new Dictionary<string, TabControlState> { ["Documentos"] = DynamicTabs.CaptureState(), ["Lateral"] = SideTabs.CaptureState(), ["Simples"] = SimpleTabs.CaptureState() });
            File.Move(temporary, LayoutPath, true);
            InteractionStatus.Text = "Ordem e seleção salvas. As anotações não fazem parte deste arquivo.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        { MessageBox.Show(this, error.Message, "Não foi possível salvar"); }
    }
    private void RestoreOrganization(object sender, RoutedEventArgs e) => ReadOrganization(true);
    private void ReadOrganization(bool notifyIfMissing, string? sourcePath = null)
    {
        sourcePath ??= LayoutPath;
        if (!File.Exists(sourcePath))
        {
            if (notifyIfMissing) InteractionStatus.Text = "Nenhuma organização foi salva ainda.";
            return;
        }
        try
        {
            using var stream = File.OpenRead(sourcePath);
            using var json = JsonDocument.Parse(stream);
            if (json.RootElement.TryGetProperty("Documentos", out _))
            {
                var states = json.RootElement.Deserialize<Dictionary<string, TabControlState>>()!;
                foreach (var pair in new[] { ("Documentos", DynamicTabs), ("Lateral", SideTabs), ("Simples", SimpleTabs) })
                    if (states.TryGetValue(pair.Item1, out var state)) pair.Item2.RestoreState(state);
            }
            else DynamicTabs.RestoreState(json.RootElement.Deserialize<TabControlState>()!);
            InteractionStatus.Text = "Ordem e seleção restauradas para as abas existentes.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or JsonException)
        { InteractionStatus.Text = "Não foi possível restaurar a organização: " + error.Message; }
    }}
