using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DLH.Controls.Wpf.Tests;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[DoNotParallelize]
public sealed class LegacySuiteTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod, TestCategory("WPF"), TestCategory("TabControl"), TestCategory("Visual")]
    public void TabControlIntegration()
    {
        var actual = Path.Combine(TestContext.ResultsDirectory ?? Path.GetTempPath(), "custom-tab-control.actual.png");
        RunSuite(actual);
        CompareSnapshot(Path.Combine(AppContext.BaseDirectory, "VisualBaselines", "custom-tab-control.png"), actual,
            Path.Combine(TestContext.ResultsDirectory ?? Path.GetTempPath(), "custom-tab-control.diff.png"));
    }

    [TestMethod, TestCategory("WPF"), TestCategory("DataGridView")]
    public void DataGridViewBehavior() => RunSuite("--datagrid-only");

    [TestMethod, TestCategory("WPF"), TestCategory("Demo")]
    public void DemoSettings() => RunSuite("--settings-only");

    [TestMethod, TestCategory("WPF"), TestCategory("Configuration")]
    public void ConfigurationRoundTrip() => RunSuite("--configuration-only");

    private void RunSuite(params string[] arguments)
    {
        var assembly = typeof(TestHostMarker).Assembly.Location;
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(assembly);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new AssertFailedException("Não foi possível iniciar a suíte WPF.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(180_000))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("A suíte WPF excedeu três minutos e foi encerrada.");
        }
        var text = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
        TestContext.WriteLine(text);
        Assert.AreEqual(0, process.ExitCode, text);
    }

    private void CompareSnapshot(string expectedPath, string actualPath, string differencePath)
    {
        var expected = LoadPixels(expectedPath);
        var actual = LoadPixels(actualPath);
        Assert.AreEqual(expected.Width, actual.Width, "A largura da captura visual mudou.");
        Assert.AreEqual(expected.Height, actual.Height, "A altura da captura visual mudou.");
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
            difference[index] = 0;
            difference[index + 1] = 0;
            difference[index + 2] = (byte)maximum;
            difference[index + 3] = 255;
        }
        SavePixels(differencePath, actual.Width, actual.Height, difference);
        TestContext.AddResultFile(actualPath);
        TestContext.AddResultFile(differencePath);
        var pixels = actual.Width * actual.Height;
        var changedRatio = (double)changed / pixels;
        var meanDifference = (double)totalDifference / (pixels * 3);
        TestContext.WriteLine($"Comparação visual: {changedRatio:P3} pixels alterados; diferença média {meanDifference:F3}.");
        Assert.IsTrue(changedRatio <= 0.02 && meanDifference <= 3,
            $"Regressão visual acima da tolerância: {changedRatio:P3} pixels e média {meanDifference:F3}. Consulte as imagens actual e diff.");
    }

    private static (int Width, int Height, byte[] Pixels) LoadPixels(string path)
    {
        using var stream = File.OpenRead(path);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var bitmap = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var stride = bitmap.PixelWidth * 4;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return (bitmap.PixelWidth, bitmap.PixelHeight, pixels);
    }

    private static void SavePixels(string path, int width, int height, byte[] pixels)
    {
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
