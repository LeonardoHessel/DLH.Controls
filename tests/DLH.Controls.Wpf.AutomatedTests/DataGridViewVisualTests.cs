using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DLH.Controls.Wpf.Demo;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("WPF")]
[TestCategory("DataGridView")]
[TestCategory("Visual")]
public sealed class DataGridViewVisualTests
{
    public TestContext TestContext { get; set; } = null!;

    [STATestMethod]
    public void DataGridViewMatchesApprovedSnapshot()
    {
        const int width = 1120;
        const int height = 720;
        var output = TestContext.ResultsDirectory ?? Path.GetTempPath();
        var actualPath = Path.Combine(output, "data-grid-view.actual.png");
        var differencePath = Path.Combine(output, "data-grid-view.diff.png");
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "VisualBaselines", "data-grid-view.png");
        var window = new DataGridViewDemoWindow();
        try
        {
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(width, height));
            root.Arrange(new Rect(0, 0, width, height));
            root.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var background = new DrawingVisual();
            using (var drawing = background.RenderOpen())
                drawing.DrawRectangle(window.Background, null, new Rect(0, 0, width, height));
            bitmap.Render(background);
            bitmap.Render(root);
            Save(actualPath, bitmap);
            AssertHeaderSeparatorContinuesThroughScrollbarGutter(window, root, bitmap);
        }
        finally { window.Close(); }

        if (Environment.GetEnvironmentVariable("UPDATE_VISUAL_BASELINES") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.Copy(actualPath, expectedPath, true);
            return;
        }
        Compare(expectedPath, actualPath, differencePath);
    }

    private void AssertHeaderSeparatorContinuesThroughScrollbarGutter(
        DataGridViewDemoWindow window, FrameworkElement root, BitmapSource bitmap)
    {
        var grid = (DLH.Controls.Wpf.DataGridView)window.FindName("ShipmentsGrid");
        grid.ApplyTemplate();
        var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid);
        viewer.ApplyTemplate();
        var headers = (FrameworkElement)viewer.Template.FindName("PART_ColumnHeadersPresenter", viewer);
        var verticalBar = (FrameworkElement)viewer.Template.FindName("PART_VerticalScrollBar", viewer);
        Assert.AreEqual(0, Grid.GetColumn(headers),
            "Os cabeçalhos precisam permanecer na coluna reservada ao conteúdo.");
        Assert.AreEqual(1, Grid.GetColumnSpan(headers),
            "Os cabeçalhos não podem cobrir a coluna reservada à barra vertical.");
        Assert.AreEqual(Visibility.Visible, verticalBar.Visibility,
            "O cenário visual precisa manter a barra vertical visível.");

        Assert.IsTrue(verticalBar.ActualWidth > 0,
            "A barra vertical precisa ocupar uma largura mensurável.");
        var headerLine = headers.TranslatePoint(new Point(0, headers.ActualHeight - 1), root);
        var verticalBarOrigin = verticalBar.TranslatePoint(new Point(0, 0), root);
        var y = (int)Math.Floor(headerLine.Y);
        var headerX = (int)Math.Floor(verticalBarOrigin.X) - 2;
        var gutterX = (int)Math.Floor(verticalBarOrigin.X + (verticalBar.ActualWidth / 2));
        Assert.IsTrue(y >= 0 && y < bitmap.PixelHeight &&
                      headerX >= 0 && headerX < bitmap.PixelWidth &&
                      gutterX >= 0 && gutterX < bitmap.PixelWidth,
            $"A amostragem do canto precisa estar dentro da captura: cabeçalho ({headerX}, {y}), " +
            $"canto ({gutterX}, {y}), imagem {bitmap.PixelWidth}x{bitmap.PixelHeight}.");
        var headerColor = ReadPixel(bitmap, headerX, y);
        var gutterColor = ReadPixel(bitmap, gutterX, y);
        TestContext.WriteLine($"Separador do cabeçalho: ({headerX}, {y}) e ({gutterX}, {y}).");

        Assert.IsTrue(ChannelDifference(headerColor, gutterColor) <= 3,
            $"A linha inferior do cabeçalho não continua alinhada sobre a barra vertical: " +
            $"cabeçalho #{headerColor.R:X2}{headerColor.G:X2}{headerColor.B:X2}, " +
            $"canto #{gutterColor.R:X2}{gutterColor.G:X2}{gutterColor.B:X2}.");
    }

    private static Color ReadPixel(BitmapSource bitmap, int x, int y)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    private static int ChannelDifference(Color first, Color second) => Math.Max(
        Math.Abs(first.R - second.R),
        Math.Max(Math.Abs(first.G - second.G), Math.Abs(first.B - second.B)));

    private void Compare(string expectedPath, string actualPath, string differencePath)
    {
        var expected = Load(expectedPath);
        var actual = Load(actualPath);
        Assert.AreEqual(expected.Width, actual.Width, "A largura da captura do DataGridView mudou.");
        Assert.AreEqual(expected.Height, actual.Height, "A altura da captura do DataGridView mudou.");
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
        Save(differencePath, BitmapSource.Create(actual.Width, actual.Height, 96, 96,
            PixelFormats.Bgra32, null, difference, actual.Width * 4));
        TestContext.AddResultFile(actualPath);
        TestContext.AddResultFile(differencePath);
        var pixelCount = actual.Width * actual.Height;
        var changedRatio = (double)changed / pixelCount;
        var meanDifference = (double)totalDifference / (pixelCount * 3);
        TestContext.WriteLine($"Comparação visual: {changedRatio:P3} pixels alterados; diferença média {meanDifference:F3}.");
        Assert.IsTrue(changedRatio <= 0.02 && meanDifference <= 3,
            $"Regressão visual no DataGridView: {changedRatio:P3} pixels e média {meanDifference:F3}.");
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
