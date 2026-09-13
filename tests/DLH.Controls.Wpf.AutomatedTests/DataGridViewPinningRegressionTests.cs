using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Pinning")]
public sealed class DataGridViewPinningRegressionTests
{
    private static void WithPinnedColumn(Action<DataGridView, Canvas, DataGridTextColumn> check)
    {
        var grid = new DataGridView
        {
            Width = 300, Height = 180, CanPinColumns = true,
            ItemsSource = new[] { new EditableRow { Name = "Original" } },
            SelectionBehavior = DataGridViewSelectionBehavior.Cell,
            IsCellEditingEnabled = true
        };
        var column = new DataGridTextColumn
        {
            Header = "A", Binding = new Binding(nameof(EditableRow.Name)), Width = 100
        };
        grid.Columns.Add(column);
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "B", Binding = new Binding(nameof(EditableRow.Name)), Width = 600
        });
        var window = new Window
        {
            Content = grid, Width = 320, Height = 220,
            ShowInTaskbar = false, WindowStyle = WindowStyle.None
        };
        window.Show();
        try
        {
            DrainLayout(grid);
            Assert.IsTrue(grid.PinColumn(column));
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ScrollToHorizontalOffset(150);
            DrainLayout(grid);
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;
            Assert.HasCount(1, layer.Children.OfType<DataGridView>().ToArray());
            check(grid, layer, column);
        }
        finally { window.Close(); }
    }

    private static void DrainLayout(DataGridView grid)
    {
        grid.UpdateLayout();
        grid.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        grid.UpdateLayout();
    }

    [STATestMethod]
    public void HiddenPinnedColumnDisappearsAndReturnsWhenShown() => WithPinnedColumn((grid, layer, column) =>
    {
        column.Visibility = Visibility.Collapsed;
        DrainLayout(grid);
        Assert.IsEmpty(layer.Children.OfType<DataGridView>());
        Assert.IsEmpty(layer.Children.OfType<Border>().Where(border => ReferenceEquals(border.Tag, column)));
        Assert.AreSame(column, grid.PinnedColumns.Single(), "Ocultar deve preservar a preferência de fixação.");
        column.Visibility = Visibility.Visible;
        DrainLayout(grid);
        Assert.AreEqual(column.Header, layer.Children.OfType<DataGridView>().Single().Columns.Single().Header);
    });

    [STATestMethod]
    public void ReplacingItemsSourceUpdatesPinnedColumnWithoutScrolling() => WithPinnedColumn((grid, layer, column) =>
    {
        var replacement = new EditableRow { Name = "Novo" };
        grid.ItemsSource = new[] { replacement };
        DrainLayout(grid);
        var overlay = layer.Children.OfType<DataGridView>().Single();
        Assert.AreSame(replacement, overlay.Items[0]);
        Assert.AreEqual("Novo", ((TextBlock)overlay.Columns[0].GetCellContent(replacement)).Text);
        Assert.AreSame(column, grid.PinnedColumns.Single());
    });

    [STATestMethod]
    public void ClickingPinnedCellSelectsAndEditsOriginalColumn() => WithPinnedColumn((grid, layer, column) =>
    {
        var overlay = layer.Children.OfType<DataGridView>().Single();
        var item = grid.Items[0];
        var overlayCell = (DataGridCell)overlay.Columns[0].GetCellContent(item).Parent;
        var point = overlayCell.TranslatePoint(new Point(20, overlayCell.ActualHeight / 2), grid);
        var hit = grid.InputHitTest(point) as DependencyObject;
        while (hit is not null && hit is not DataGridCell) hit = VisualTreeHelper.GetParent(hit);
        Assert.AreSame(overlayCell, hit, "O clique não deve atravessar a coluna fixada.");
        overlayCell.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = Mouse.PreviewMouseDownEvent, Source = overlayCell
        });
        DrainLayout(grid);
        Assert.AreSame(column, grid.CurrentCell.Column);
        Assert.AreSame(item, grid.CurrentCell.Item);
        Assert.AreSame(column, grid.SelectedCells.Single().Column);
        Assert.IsTrue(grid.BeginEdit(), "A edição deve ocorrer no grid original.");
        var editor = (TextBox)column.GetCellContent(item);
        editor.Text = "Editado";
        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Row, true));
        Assert.AreEqual("Editado", ((EditableRow)item).Name);
    });

    [STATestMethod]
    public void ClickingPinnedCellRespectsDisabledSelection() => WithPinnedColumn((grid, layer, column) =>
    {
        grid.SelectionBehavior = DataGridViewSelectionBehavior.None;
        var overlay = layer.Children.OfType<DataGridView>().Single();
        var cell = (DataGridCell)overlay.Columns[0].GetCellContent(grid.Items[0]).Parent;
        cell.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = Mouse.PreviewMouseDownEvent, Source = cell
        });
        Assert.IsFalse(grid.CurrentCell.IsValid);
        Assert.IsEmpty(grid.SelectedCells);
        Assert.IsNull(grid.SelectedItem);
    });

    [STATestMethod]
    public void ClickingPinnedCellSelectsOriginalRow() => WithPinnedColumn((grid, layer, column) =>
    {
        grid.SelectionBehavior = DataGridViewSelectionBehavior.Row;
        var item = grid.Items[0];
        var overlay = layer.Children.OfType<DataGridView>().Single();
        var cell = (DataGridCell)overlay.Columns[0].GetCellContent(item).Parent;
        cell.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = Mouse.PreviewMouseDownEvent, Source = cell
        });
        DrainLayout(grid);
        Assert.AreSame(item, grid.SelectedItem);
        Assert.AreSame(column, grid.CurrentCell.Column);
    });

    [STATestMethod]
    public void PinnedCellMenuTargetsOriginalColumn() => WithPinnedColumn((grid, layer, column) =>
    {
        var overlay = layer.Children.OfType<DataGridView>().Single();
        var cell = (DataGridCell)overlay.Columns[0].GetCellContent(grid.Items[0]).Parent;
        try
        {
            cell.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
            {
                RoutedEvent = Mouse.PreviewMouseDownEvent, Source = cell
            });
            DrainLayout(grid);
            Assert.IsNotNull(cell.ContextMenu);
            Assert.IsTrue(cell.ContextMenu.IsOpen);
            var unpin = cell.ContextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Desafixar coluna “A”"));
            unpin.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.IsEmpty(grid.PinnedColumns);
        }
        finally
        {
            if (cell.ContextMenu is not null) cell.ContextMenu.IsOpen = false;
        }
    });

    [STATestMethod]
    public void PinnedRowsDoNotRenderAnExtraHeaderBand() => WithPinnedColumn((grid, layer, column) =>
    {
        var rows = Enumerable.Range(0, 30).Select(index => new EditableRow { Name = $"Linha {index}" }).ToArray();
        grid.ItemsSource = rows;
        grid.CanPinRows = true;
        DrainLayout(grid);
        grid.PinRow(rows[0]);
        grid.PinRow(rows[1]);
        var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
        viewer.ScrollToVerticalOffset(150);
        DrainLayout(grid);
        var overlays = layer.Children.OfType<DataGridView>()
            .Where(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.None).ToArray();
        Assert.HasCount(4, overlays, "Duas linhas e suas interseções devem estar fixadas.");
        foreach (var overlay in overlays)
        {
            var overlayViewer = (ScrollViewer)overlay.Template.FindName("DG_ScrollViewer", overlay)!;
            var viewport = (FrameworkElement)overlayViewer.Template.FindName("PART_ScrollContentPresenter", overlayViewer)!;
            Assert.AreEqual(0d, viewport.TranslatePoint(new Point(), overlay).Y, 0.01d,
                "Uma camada sem cabeçalho não deve reservar nem desenhar uma faixa acima da célula.");
        }
    });

    [STATestMethod]
    public void CombinedPinningKeepsVariableHeightRowsAlignedInBothScrollDirections() => WithPinnedColumn((grid, layer, column) =>
    {
        var rows = Enumerable.Range(0, 100).Select(index => new EditableRow { Name = $"Linha {index}" }).ToArray();
        grid.LoadingRow += (_, args) => args.Row.Height = Array.IndexOf(rows, args.Row.Item) % 2 == 0 ? 31 : 43;
        grid.ItemsSource = rows;
        grid.CanPinRows = true;
        DrainLayout(grid);
        grid.PinRow(rows[0]);
        grid.PinRow(rows[1]);
        var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
        var viewport = (FrameworkElement)viewer.Template.FindName("PART_ScrollContentPresenter", viewer)!;
        foreach (var offset in new[] { 150d, 180d, 240d, 310d, 640d, 310d, 240d, 180d, 150d })
        {
            viewer.ScrollToVerticalOffset(offset);
            DrainLayout(grid);
            var overlay = layer.Children.OfType<DataGridView>()
                .Single(candidate => candidate.HeadersVisibility == DataGridHeadersVisibility.Column);
            var shared = rows.Skip(2).Where(item =>
                grid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow source &&
                source.TranslatePoint(new Point(), viewport).Y >= 0 &&
                source.TranslatePoint(new Point(), viewport).Y < viewport.ActualHeight &&
                overlay.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow).ToArray();
            Assert.IsNotEmpty(shared);
            foreach (var item in shared)
            {
                var sourceRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(item)!;
                var pinnedRow = (DataGridRow)overlay.ItemContainerGenerator.ContainerFromItem(item)!;
                Assert.AreEqual(sourceRow.TranslatePoint(new Point(), layer).Y,
                    pinnedRow.TranslatePoint(new Point(), layer).Y, 0.6d,
                    $"A coluna deve continuar alinhada em {offset}, para {item.Name}.");
            }
        }
    });

    [STATestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PinnedColumnsPreserveFractionalRowHeights(bool roundLayout) => WithPinnedColumn((grid, layer, column) =>
    {
        var rows = Enumerable.Range(0, 100).Select(index => new EditableRow { Name = $"Linha {index}" }).ToArray();
        grid.UseLayoutRounding = roundLayout;
        grid.RowHeight = 36.4;
        grid.ColumnHeaderHeight = 35.6;
        grid.ItemsSource = rows;
        grid.CanPinRows = true;
        DrainLayout(grid);
        grid.PinRow(rows[0]);
        grid.PinRow(rows[1]);
        var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
        var viewport = (FrameworkElement)viewer.Template.FindName("PART_ScrollContentPresenter", viewer)!;
        foreach (var offset in new[] { 650.35, 690.75, 725.15, 690.75 })
        {
            viewer.ScrollToVerticalOffset(offset);
            DrainLayout(grid);
            var overlay = layer.Children.OfType<DataGridView>()
                .Single(candidate => candidate.HeadersVisibility == DataGridHeadersVisibility.Column);
            var shared = rows.Where(item =>
                grid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow source &&
                source.TranslatePoint(new Point(), viewport).Y >= 0 &&
                source.TranslatePoint(new Point(), viewport).Y < viewport.ActualHeight &&
                overlay.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow).ToArray();
            Assert.IsNotEmpty(shared);
            foreach (var item in shared)
            {
                var source = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(item)!;
                var pinned = (DataGridRow)overlay.ItemContainerGenerator.ContainerFromItem(item)!;
                Assert.AreEqual(source.ActualHeight, pinned.ActualHeight, 0.01,
                    "Arredondar apenas a coluna fixa acumula erro a cada linha.");
                Assert.AreEqual(source.TranslatePoint(new Point(), layer).Y,
                    pinned.TranslatePoint(new Point(), layer).Y, 0.01,
                    "Os separadores devem coincidir também em posições fracionárias de rolagem.");
            }
            if (!roundLayout && offset == 650.35)
            {
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)Math.Ceiling(grid.ActualWidth), (int)Math.Ceiling(grid.ActualHeight),
                    96, 96, PixelFormats.Pbgra32);
                bitmap.Render(grid);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using var file = System.IO.File.Create(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "DLH.Controls-pinning-alignment.png"));
                encoder.Save(file);
            }
        }
    });

    public sealed class EditableRow
    {
        public string Name { get; set; } = string.Empty;
    }
}
