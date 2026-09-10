using System.IO;
using System.Windows;
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
