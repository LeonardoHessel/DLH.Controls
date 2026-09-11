using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ControlsContextMenu = DLH.Controls.Wpf.ContextMenu;
using ControlsScrollBar = DLH.Controls.Wpf.ScrollBar;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("WPF")]
[TestCategory("SharedControls")]
[TestCategory("Visual")]
public sealed class SharedControlsVisualTests
{
    public TestContext TestContext { get; set; } = null!;

    [STATestMethod]
    public void SharedControlsMatchApprovedSnapshot()
    {
        const int width = 720;
        const int height = 380;
        var output = TestContext.ResultsDirectory ?? Path.GetTempPath();
        var actualPath = Path.Combine(output, "shared-controls.actual.png");
        var differencePath = Path.Combine(output, "shared-controls.diff.png");
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "VisualBaselines", "shared-controls.png");

        var vertical = new ControlsScrollBar { Height = 250, Thickness = 12, Minimum = 0, Maximum = 100, Value = 38, ViewportSize = 20, ShowButtons = true, IsShadowEnabled = true };
        var horizontal = new ControlsScrollBar { Width = 270, Thickness = 12, Orientation = Orientation.Horizontal, Minimum = 0, Maximum = 100, Value = 38, ViewportSize = 20 };
        var menu = new ControlsContextMenu { Width = 270 };
        menu.Items.Add(new MenuItem { Header = "Atualizar", InputGestureText = "F5", Icon = new TextBlock { Text = "↻", Foreground = Brushes.DeepSkyBlue } });
        menu.Items.Add(new MenuItem { Header = "Exibir detalhes", IsCheckable = true, IsChecked = true });
        menu.Items.Add(new Separator());
        var export = new MenuItem { Header = "Exportar" };
        export.Items.Add(new MenuItem { Header = "Arquivo CSV" });
        menu.Items.Add(export);
        menu.Items.Add(new MenuItem { Header = "Ação indisponível", IsEnabled = false });

        var root = new Grid { Width = width, Height = height, Background = new SolidColorBrush(Color.FromRgb(28, 29, 32)), Margin = new Thickness(0) };
        root.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/DLH.Controls.Wpf;component/Themes/Generic.xaml", UriKind.Relative) });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        root.ColumnDefinitions.Add(new ColumnDefinition());

        var leftLayout = new Grid();
        leftLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftLayout.RowDefinitions.Add(new RowDefinition());
        leftLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftLayout.Children.Add(new TextBlock { Text = "ScrollBar", FontSize = 18, Foreground = Brushes.White });
        Grid.SetRow(vertical, 1); vertical.HorizontalAlignment = HorizontalAlignment.Right; vertical.Margin = new Thickness(0, 18, 0, 10); leftLayout.Children.Add(vertical);
        Grid.SetRow(horizontal, 2); horizontal.Margin = new Thickness(0, 0, 0, 4); leftLayout.Children.Add(horizontal);
        var leftCard = CreateCard(leftLayout); leftCard.Margin = new Thickness(28, 28, 14, 28); root.Children.Add(leftCard);

        var rightLayout = new StackPanel();
        rightLayout.Children.Add(new TextBlock { Text = "ContextMenu", FontSize = 18, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 16) });
        var rightCard = CreateCard(rightLayout); rightCard.Margin = new Thickness(14, 28, 28, 28); Grid.SetColumn(rightCard, 1); root.Children.Add(rightCard);

        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        var rootBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rootBitmap.Render(root);

        menu.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/DLH.Controls.Wpf;component/Themes/Generic.xaml", UriKind.Relative) });
        menu.Foreground = Brushes.White;
        menu.Background = new SolidColorBrush(Color.FromRgb(53, 55, 60));
        var placementTarget = new Button { Width = 1, Height = 1 };
        var host = new Window { Content = placementTarget, Width = 1, Height = 1, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, WindowStyle = WindowStyle.None };
        host.Show();
        menu.PlacementTarget = placementTarget;
        menu.IsOpen = true;
        menu.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
        menu.ApplyTemplate();
        foreach (var element in menu.Items.OfType<FrameworkElement>()) element.ApplyTemplate();
        menu.UpdateLayout();
        var menuBitmap = new RenderTargetBitmap(270, Math.Max(1, (int)Math.Ceiling(menu.ActualHeight)), 96, 96, PixelFormats.Pbgra32);
        menuBitmap.Render(menu);

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var composite = new DrawingVisual();
        using (var drawing = composite.RenderOpen())
        {
            drawing.DrawImage(rootBitmap, new Rect(0, 0, width, height));
            drawing.DrawImage(menuBitmap, new Rect(390, 82, 270, menuBitmap.PixelHeight));
        }
        bitmap.Render(composite);
        menu.IsOpen = false;
        host.Close();
        Save(actualPath, bitmap);

        if (Environment.GetEnvironmentVariable("UPDATE_VISUAL_BASELINES") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.Copy(actualPath, expectedPath, true);
            return;
        }

        Compare(expectedPath, actualPath, differencePath);
    }

    private static Border CreateCard(UIElement content) => new()
    {
        Padding = new Thickness(24), Background = new SolidColorBrush(Color.FromRgb(41, 43, 47)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(69, 72, 79)), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12), Child = content
    };

    private void Compare(string expectedPath, string actualPath, string differencePath)
    {
        var expected = Load(expectedPath);
        var actual = Load(actualPath);
        Assert.AreEqual(expected.Width, actual.Width);
        Assert.AreEqual(expected.Height, actual.Height);
        var changed = 0;
        long totalDifference = 0;
        var difference = new byte[actual.Pixels.Length];
        for (var index = 0; index < actual.Pixels.Length; index += 4)
        {
            var maximum = 0;
            for (var channel = 0; channel < 3; channel++)
            {
                var delta = Math.Abs(expected.Pixels[index + channel] - actual.Pixels[index + channel]);
                maximum = Math.Max(maximum, delta);
                totalDifference += delta;
            }
            if (maximum > 20) changed++;
            difference[index + 2] = (byte)maximum;
            difference[index + 3] = 255;
        }
        Save(differencePath, BitmapSource.Create(actual.Width, actual.Height, 96, 96, PixelFormats.Bgra32, null, difference, actual.Width * 4));
        TestContext.AddResultFile(actualPath);
        TestContext.AddResultFile(differencePath);
        var pixels = actual.Width * actual.Height;
        var changedRatio = (double)changed / pixels;
        var meanDifference = (double)totalDifference / (pixels * 3);
        Assert.IsTrue(changedRatio <= .02 && meanDifference <= 3,
            $"Regressão visual nos controles compartilhados: {changedRatio:P3} pixels e média {meanDifference:F3}.");
    }

    private static (int Width, int Height, byte[] Pixels) Load(string path)
    {
        using var stream = File.OpenRead(path);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var bitmap = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        return (bitmap.PixelWidth, bitmap.PixelHeight, pixels);
    }

    private static void Save(string path, BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
