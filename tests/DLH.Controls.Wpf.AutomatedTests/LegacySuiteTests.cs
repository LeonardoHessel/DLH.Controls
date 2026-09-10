using System.Diagnostics;
using System.IO;
using DLH.Controls.Wpf.Tests;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[DoNotParallelize]
public sealed class LegacySuiteTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod, TestCategory("WPF"), TestCategory("TabControl")]
    public void TabControlIntegration() => RunSuite(Path.Combine(TestContext.ResultsDirectory ?? Path.GetTempPath(), "preview.png"));

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
}
