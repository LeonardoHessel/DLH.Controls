using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.Demo;

public sealed class DemoConfiguration
{
    public int Version { get; set; } = 1;
    public int Theme { get; set; }
    public Dictionary<string, TabControlConfiguration> Controls { get; set; } = new();
}

public partial class MainWindow
{
    private static string ConfigurationPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TabControl.Demo", "configuration.json");
    private Dictionary<string, DLH.Controls.Wpf.TabControl> ConfigurationTargets => new()
    { ["Documentos"] = DynamicTabs, ["Lateral"] = SideTabs, ["Simples"] = SimpleTabs };

    private void WriteConfiguration(string? path = null)
    {
        path ??= ConfigurationPath;
        var data = new DemoConfiguration { Theme = themeIndex, Controls = ConfigurationTargets.ToDictionary(pair => pair.Key, pair => pair.Value.CaptureConfiguration()) };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private void ReadConfiguration(string? path = null)
    {
        path ??= ConfigurationPath;
        if (!File.Exists(path)) return;
        try
        {
            var data = JsonSerializer.Deserialize<DemoConfiguration>(File.ReadAllText(path)) ?? throw new ArgumentException("Configuração vazia.");
            if (data.Version != 1 || data.Theme < 0 || data.Theme >= ThemeNames.Length || data.Controls is null)
                throw new ArgumentException("Configuração inválida.");
            foreach (var entry in data.Controls)
            {
                if (!ConfigurationTargets.ContainsKey(entry.Key)) throw new ArgumentException("Modelo desconhecido.");
                DLH.Controls.Wpf.TabControl.ValidateConfiguration(entry.Value);
            }
            themeIndex = data.Theme; ApplyTheme();
            foreach (var entry in data.Controls) ConfigurationTargets[entry.Key].RestoreConfiguration(entry.Value);
            InteractionStatus.Text = "Configurações restauradas para cada modelo.";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or JsonException)
        { InteractionStatus.Text = "Não foi possível restaurar as configurações: " + error.Message; }
    }

    private void SaveConfiguration(object sender, RoutedEventArgs e)
    {
        try { WriteConfiguration(); InteractionStatus.Text = "Configurações dos três modelos e tema salvos."; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        { InteractionStatus.Text = "Não foi possível salvar configurações: " + error.Message; }
    }
    private void RestoreConfiguration(object sender, RoutedEventArgs e) => ReadConfiguration();

    private void ResetThemeBrushes()
    {
        foreach (var control in ConfigurationTargets.Values)
        {
            control.SetResourceReference(Control.BackgroundProperty, "Tabs.Surface");
            control.SetResourceReference(Control.ForegroundProperty, "Tabs.Text");
            control.SetResourceReference(Control.BorderBrushProperty, "Tabs.Edge");
        }
    }
}

