using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Pinning")]
public sealed class DataGridViewPinningPerformanceTests
{
    public TestContext TestContext { get; set; } = null!;

    [STATestMethod]
    public void PinnedMetricsDoNotAllocateForEveryCellVisual()
    {
        var rows = Enumerable.Range(0, 1000).Select(index => $"Registro {index}").ToArray();
        var grid = new DataGridView
        {
            Width = 700, Height = 450, ItemsSource = rows, CanPinRows = true, CanPinColumns = true
        };
        for (var index = 0; index < 12; index++)
            grid.Columns.Add(new DataGridTextColumn { Header = $"C{index}", Width = 150, Binding = new Binding() });
        var window = new Window { Content = grid, Width = 740, Height = 500, ShowInTaskbar = false };
        window.Show();
        void Drain()
        {
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }
        try
        {
            Drain();
            grid.PinRow(rows[0]);
            grid.PinRow(rows[1]);
            grid.PinColumn(grid.Columns[0]);
            grid.PinColumn(grid.Columns[2]);
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ScrollToVerticalOffset(200);
            viewer.ScrollToHorizontalOffset(200);
            Drain();

            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;
            var overlays = layer.Children.OfType<DataGridView>().ToArray();
            var rowOverlays = overlays.Where(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.None).ToArray();
            var stationaryRowLayouts = 0;
            foreach (var overlay in rowOverlays)
                overlay.LayoutUpdated += (_, _) => stationaryRowLayouts++;
            var times = new List<double>();
            var scrollAllocated = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 80; index++)
            {
                var stopwatch = Stopwatch.StartNew();
                viewer.ScrollToVerticalOffset(220 + index * 6);
                Drain();
                times.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            TestContext.WriteLine($"Rolagem: média {times.Average():F2} ms, P95 {times.Order().ElementAt(75):F2} ms, " +
                $"alocações {(GC.GetAllocatedBytesForCurrentThread() - scrollAllocated) / 1024 / 1024} MB.");
            CollectionAssert.AreEqual(overlays, layer.Children.OfType<DataGridView>().ToArray(),
                "Rolar dentro da mesma área fixa não deve reconstruir as interseções.");
            TestContext.WriteLine($"Notificações de layout com linhas e colunas fixadas: {stationaryRowLayouts}.");

            var capture = (Action)Delegate.CreateDelegate(typeof(Action), grid,
                typeof(DataGridView).GetMethod("CapturePinnedMetrics", BindingFlags.Instance | BindingFlags.NonPublic)!);
            capture();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var elapsed = Stopwatch.StartNew();
            for (var index = 0; index < 200; index++) capture();
            var bytesPerCapture = (GC.GetAllocatedBytesForCurrentThread() - allocated) / 200;
            TestContext.WriteLine($"Métricas: {elapsed.Elapsed.TotalMilliseconds / 200:F3} ms e {bytesPerCapture} bytes por coleta.");
            // A full traversal through cell templates previously allocated about 405 KB
            // per capture. Keep a generous ceiling without asserting machine-dependent timing.
            Assert.IsLessThan(64L * 1024, bytesPerCapture,
                "A coleta deve visitar apenas os contêineres necessários, sem percorrer células e cópias fixadas.");
        }
        finally { window.Close(); }
    }
}
