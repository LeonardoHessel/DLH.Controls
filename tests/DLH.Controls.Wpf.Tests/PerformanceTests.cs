using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using ShapePath = System.Windows.Shapes.Path;

internal static partial class DragDropTests
{
    internal static class PerformanceTests
    {
        private sealed record Measurement(int Tabs, string Scenario, int Operations, int GeometryChanges, double TotalMilliseconds, long AllocatedBytes);

        public static void Run(string? reportPath)
        {
            var results = new List<Measurement>();
            foreach (var count in new[] { 2, 10, 50 })
            {
                using var f = new Fixture(count: count);
                var surface = (ShapePath)f.Tabs.Template.FindName("PART_Surface", f.Tabs);
                var updateSurface = (Action)typeof(DLH.Controls.Wpf.TabControl).GetMethod("UpdateSurface", Private)!.CreateDelegate(typeof(Action), f.Tabs);
                results.Add(Measure(f, surface, count, "idle", 1000, _ => updateSurface()));
                results.Add(Measure(f, surface, count, "resize", 30, i => { f.LayoutSize = new Size(720 + i % 2 * 40, 340 + i % 3 * 10); f.Layout(); }));
                results.Add(Measure(f, surface, count, "font", 30, i => { f.Tabs.FontSize = i % 2 == 0 ? 14 : 16; f.Layout(); }));
                results.Add(Measure(f, surface, count, "scroll", 30, i => { f.Scroll.ScrollToHorizontalOffset(i % 2 == 0 ? 0 : f.Scroll.ScrollableWidth); f.Layout(); }));
                results.Add(Measure(f, surface, count, "drag", 30, i =>
                {
                    if (!f.Dragging) f.Start();
                    f.Update(f.Position(Math.Min(f.Model.Items.Count - 1, i % Math.Max(1, f.Model.Items.Count)), true));
                    updateSurface();
                }));
                f.Cancel();
            }
            foreach (var result in results)
                Console.WriteLine($"PERF: {result.Tabs,2} tabs | {result.Scenario,-6} | {result.GeometryChanges,3}/{result.Operations} geometry | {result.TotalMilliseconds,8:F2} ms | {result.AllocatedBytes,9} B");
            if (reportPath is null) return;
            var directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(reportPath));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new
            {
                CreatedUtc = DateTime.UtcNow,
                Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                ProcessorCount = Environment.ProcessorCount,
                Scope = "Release WPF layout in an unshown window; relative diagnostic baseline, not an end-user UI benchmark.",
                Results = results
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static Measurement Measure(Fixture fixture, ShapePath surface, int tabs, string scenario, int operations, Action<int> operation)
        {
            operation(0); fixture.Layout();
            var geometry = surface.Data; var changes = 0;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var i = 0; i < operations; i++)
            {
                operation(i);
                if (!ReferenceEquals(geometry, surface.Data)) { changes++; geometry = surface.Data; }
            }
            timer.Stop();
            return new(tabs, scenario, operations, changes, timer.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - allocated);
        }
    }
}

internal static class PerformanceTests
{
    public static void Run(string? reportPath) => DragDropTests.PerformanceTests.Run(reportPath);
}

