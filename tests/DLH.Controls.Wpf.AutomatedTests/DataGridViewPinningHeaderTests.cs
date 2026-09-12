using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DLH.Controls.Wpf;
using DLH.Controls.Wpf.Demo;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Pinning")]
public sealed class DataGridViewPinningHeaderTests
{
    public TestContext TestContext { get; set; } = null!;

    [STATestMethod]
    [DataRow(true, 13d)]
    [DataRow(true, 14d)]
    [DataRow(true, 15d)]
    [DataRow(false, 14d)]
    public void DemoPinnedHeaderMatchesTheScrollableHeaderBoundary(bool pinRows, double fontSize)
    {
        var window = new DataGridViewDemoWindow { FontSize = fontSize };
        window.Show();
        try
        {
            var grid = (DataGridView)window.FindName("ShipmentsGrid");
            void Drain()
            {
                grid.UpdateLayout();
                grid.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            }
            Drain();
            var quantity = grid.Columns.Single(column => column.Header?.ToString() == "Quantidade");
            quantity.DisplayIndex = 1;
            grid.PinColumn(grid.Columns[0]);
            grid.PinColumn(quantity);
            if (pinRows)
            {
                grid.PinRow(window.Rows[0]);
                grid.PinRow(window.Rows[2]);
            }
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            Drain();
            var status = grid.Columns.Single(column => column.Header?.ToString() == "Status");
            var statusOffset = grid.Columns.Where(column => column.DisplayIndex < status.DisplayIndex)
                .Sum(column => column.ActualWidth);
            viewer.ScrollToHorizontalOffset(statusOffset - grid.Columns[0].ActualWidth - quantity.ActualWidth);
            viewer.ScrollToBottom();
            Drain();
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;
            var overlay = layer.Children.OfType<DataGridView>().Single(candidate => candidate.HeadersVisibility == DataGridHeadersVisibility.Column);
            var viewport = (FrameworkElement)viewer.Template.FindName("PART_ScrollContentPresenter", viewer)!;
            var headerBottom = viewport.TranslatePoint(new Point(), grid).Y;
            if (pinRows)
            {
                var firstPinnedRow = layer.Children.OfType<DataGridView>().Single(candidate =>
                    candidate.HeadersVisibility == DataGridHeadersVisibility.None &&
                    candidate.Columns.Count == grid.Columns.Count && ReferenceEquals(candidate.Items[0], window.Rows[0]));
                Assert.AreEqual(headerBottom, firstPinnedRow.TranslatePoint(new Point(), grid).Y, 0.01,
                    "A linha fixa deve começar exatamente onde termina o cabeçalho, sem fresta ou sobreposição.");
            }
            var overlayViewer = (ScrollViewer)overlay.Template.FindName("DG_ScrollViewer", overlay)!;
            var overlayViewport = (FrameworkElement)overlayViewer.Template.FindName("PART_ScrollContentPresenter", overlayViewer)!;
            Assert.AreEqual(headerBottom, overlayViewport.TranslatePoint(new Point(), grid).Y, 0.01,
                "A espessura visual não deve ser compensada deslocando as células.");
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(grid.ActualWidth), (int)Math.Ceiling(grid.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            var drawing = new DrawingVisual();
            using (var context = drawing.RenderOpen())
                context.DrawRectangle(new VisualBrush(grid), null, new Rect(0, 0, grid.ActualWidth, grid.ActualHeight));
            bitmap.Render(drawing);
            var path = Path.Combine(TestContext.ResultsDirectory ?? Path.GetTempPath(), $"demo-pinned-header-rows-{pinRows}-font-{fontSize}.png");
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(path)) encoder.Save(file);
            TestContext.AddResultFile(path);
            TestContext.WriteLine(path);
            var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
            bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
            if (pinRows)
            {
                foreach (var offset in new[] { viewer.ScrollableHeight - 63.5, viewer.ScrollableHeight - 121.25 })
                {
                    viewer.ScrollToVerticalOffset(offset);
                    Drain();
                    var next = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                    next.Render(drawing);
                    var after = new byte[pixels.Length];
                    next.CopyPixels(after, next.PixelWidth * 4, 0);
                    for (var y = (int)Math.Floor(headerBottom) - 1; y <= (int)Math.Ceiling(headerBottom) + 2; y++)
                        for (var x = 5; x < bitmap.PixelWidth - 25; x++)
                            for (var channel = 0; channel < 3; channel++)
                            {
                                var index = (y * bitmap.PixelWidth + x) * 4 + channel;
                                Assert.IsLessThanOrEqualTo(2, Math.Abs(pixels[index] - after[index]),
                                    $"Conteúdo móvel vazou na junção fixa em ({x}, {y}).");
                            }
                }
            }
            var left = 10;
            var right = (int)Math.Ceiling(overlay.ActualWidth) + 25;
            var lastPixel = (int)Math.Ceiling(headerBottom) + (pinRows ? 2 : -1);
            for (var y = (int)Math.Floor(headerBottom) - 3; y <= lastPixel; y++)
            {
                var a = (y * bitmap.PixelWidth + left) * 4;
                var b = (y * bitmap.PixelWidth + right) * 4;
                Assert.AreEqual(255, (int)pixels[a + 3], "A captura precisa conter a superfície opaca do grid.");
                // Allow only a two-level color difference for fractional antialiasing.
                // An extra/masked border changes these pixels by roughly 30 levels.
                for (var channel = 0; channel < 3; channel++)
                    Assert.IsLessThanOrEqualTo(2, Math.Abs(pixels[a + channel] - pixels[b + channel]),
                        $"A borda do cabeçalho precisa ter a mesma espessura e posição em Y={y}.");
            }
        }
        finally { window.Close(); }
    }
}
