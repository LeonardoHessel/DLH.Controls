using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using DLH.Controls.Wpf;
using Tabs = DLH.Controls.Wpf.CustomTabControl;

internal static class ConfigurationTests
{
    public static void Run()
    {
        void Check(bool valid, string message) { if (!valid) throw new Exception(message); Console.WriteLine("PASS: " + message); }
        var source = new Tabs { CornerRadius = new CornerRadius(19), HeaderIndent = double.NaN, TabStripPlacement = Dock.Right, ShadowOpacity = .37, CanCloseTabs = false, DragAnimationDuration = TimeSpan.FromMilliseconds(250), Background = Brushes.Orange };
        var configuration = source.CaptureConfiguration();
        var json = JsonSerializer.Serialize(configuration);
        var restored = new Tabs();
        var previousCulture = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US"); restored.RestoreConfiguration(JsonSerializer.Deserialize<TabControlConfiguration>(json)!); }
        finally { CultureInfo.CurrentCulture = previousCulture; }
        Check(restored.CornerRadius.TopLeft == 19 && double.IsNaN(restored.HeaderIndent) && restored.TabStripPlacement == Dock.Right && restored.ShadowOpacity == .37 && !restored.CanCloseTabs && restored.DragAnimationDuration.TotalMilliseconds == 250 && restored.Background.ToString() == Brushes.Orange.ToString(), "configuration JSON roundtrip is culture independent");
        var before = JsonSerializer.Serialize(restored.CaptureConfiguration());
        foreach (var invalid in new[] {
            new TabControlConfiguration { Version = 999 },
            new TabControlConfiguration { Values = new() { ["CornerRadius"] = "8", ["ShadowOpacity"] = "2" } },
            new TabControlConfiguration { Values = new() { ["Unknown"] = "1" } },
            new TabControlConfiguration { Values = new() { ["TabStripPlacement"] = "999" } } })
        {
            var rejected = false; try { restored.RestoreConfiguration(invalid); } catch (ArgumentException) { rejected = true; }
            Check(rejected && before == JsonSerializer.Serialize(restored.CaptureConfiguration()), "invalid configuration rejected without partial mutation");
        }
        var item = new TabItem { Header = "Keep" }; restored.Items.Add(item); restored.SelectedItem = item;
        var bindingSource = new Tabs { ShadowOpacity = .2 };
        restored.SetBinding(Tabs.ShadowOpacityProperty, new Binding("ShadowOpacity") { Source = bindingSource });
        restored.RestoreConfiguration(configuration);
        Check(BindingOperations.IsDataBound(restored, Tabs.ShadowOpacityProperty) && restored.Items.Count == 1 && restored.SelectedItem == item, "restoration preserves bindings and documents");
        restored.ResetConfiguration();
        Check(restored.CornerRadius.TopLeft == 12 && restored.ShadowOpacity == .5 && restored.CanCloseTabs && restored.TabStripPlacement == Dock.Top && restored.Items.Count == 1 && BindingOperations.IsDataBound(restored, Tabs.ShadowOpacityProperty), "reset restores library defaults without removing bindings/documents");
        var demo = new DLH.Controls.Wpf.Demo.MainWindow();
        var root = (FrameworkElement)demo.Content; root.Measure(new Size(1320,820)); root.Arrange(new Rect(0,0,1320,820)); root.UpdateLayout();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");
        var a = (Tabs)demo.FindName("DynamicTabs"); var b = (Tabs)demo.FindName("SideTabs");
        try
        {
            a.CornerRadius = new CornerRadius(21); b.CornerRadius = new CornerRadius(7);
            typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("WriteConfiguration", flags)!.Invoke(demo, new object?[] { path });
            a.CornerRadius = new CornerRadius(1); b.CornerRadius = new CornerRadius(1);
            typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ReadConfiguration", flags)!.Invoke(demo, new object?[] { path });
            Check(a.CornerRadius.TopLeft == 21 && b.CornerRadius.TopLeft == 7, "demo saves/restores separate model settings");
            var themeField = typeof(DLH.Controls.Wpf.Demo.MainWindow).GetField("themeIndex", flags)!;
            var applyTheme = typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ApplyTheme", flags)!;
            themeField.SetValue(demo, 2); applyTheme.Invoke(demo, null);
            typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("WriteConfiguration", flags)!.Invoke(demo, new object?[] { path });
            var fresh = new DLH.Controls.Wpf.Demo.MainWindow();
            try
            {
                typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ReadConfiguration", flags)!.Invoke(fresh, new object?[] { path });
                Check((int)themeField.GetValue(fresh)! == 2 && ((TextBlock)fresh.FindName("ThemeName")).Text == "Cinza e laranja" && ((Tabs)fresh.FindName("DynamicTabs")).CornerRadius.TopLeft == 21, "new window restores saved theme and settings");
                typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ToggleTheme", flags)!.Invoke(fresh, new object?[] { fresh, new RoutedEventArgs() });
                Check((int)themeField.GetValue(fresh)! == 0 && ((Tabs)fresh.FindName("DynamicTabs")).Background.ToString() == fresh.Resources["Tabs.Surface"].ToString(), "theme remains switchable after restoration");
            }
            finally { fresh.Close(); }
            var c = (Tabs)demo.FindName("SimpleTabs");
            var panel = new DLH.Controls.Wpf.Demo.TabSettingsWindow(new[] { a, b, c });
            try
            {
                var scope = (ComboBox)typeof(DLH.Controls.Wpf.Demo.TabSettingsWindow).GetField("scope", flags)!.GetValue(panel)!;
                scope.SelectedIndex = 2;
                var stack = (StackPanel)((ScrollViewer)panel.Content).Content;
                var resetButton = stack.Children.OfType<Button>().Single(button => Equals(button.Content, "Restaurar padrões"));
                resetButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Check(a.CornerRadius.TopLeft == 21 && b.CornerRadius.TopLeft == 12 && b.TabStripPlacement == Dock.Top && !b.ShowCloseButtons, "reset button only resets selected model");
                scope.SelectedIndex = 0;
                resetButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Check(new[] { a, b, c }.All(control => control.CornerRadius.TopLeft == 12 && control.CanCloseTabs && control.ShadowOpacity == .5), "reset button resets all three models");
            }
            finally { panel.Close(); }
            a.CornerRadius = new CornerRadius(21); b.CornerRadius = new CornerRadius(7);
            var data = JsonSerializer.Deserialize<DLH.Controls.Wpf.Demo.DemoConfiguration>(System.IO.File.ReadAllText(path))!;
            data.Controls["Documentos"].Values["CornerRadius"] = "3";
            data.Controls["Lateral"].Values["ShadowOpacity"] = "2";
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(data));
            typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ReadConfiguration", flags)!.Invoke(demo, new object?[] { path });
            Check(a.CornerRadius.TopLeft == 21 && b.CornerRadius.TopLeft == 7, "demo validates all models before applying a file");
            foreach (var invalidJson in new[] { "{", "null", "[]", "{\"Theme\":999}", "{\"Controls\":null}" })
            {
                System.IO.File.WriteAllText(path, invalidJson);
                typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ReadConfiguration", flags)!.Invoke(demo, new object?[] { path });
                Check(a.CornerRadius.TopLeft == 21 && b.CornerRadius.TopLeft == 7 && ((TextBlock)demo.FindName("InteractionStatus")).Text.StartsWith("Não foi possível"), "corrupt configuration file preserves models and reports error");
            }
            System.IO.File.Delete(path);
            typeof(DLH.Controls.Wpf.Demo.MainWindow).GetMethod("ReadConfiguration", flags)!.Invoke(demo, new object?[] { path });
            Check(a.CornerRadius.TopLeft == 21 && b.CornerRadius.TopLeft == 7, "missing configuration file leaves settings unchanged");
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); demo.Close(); }
    }
}
