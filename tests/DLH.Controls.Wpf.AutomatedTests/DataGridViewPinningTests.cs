using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Pinning")]
public sealed class DataGridViewPinningTests
{
    [TestMethod]
    public void StickyLayoutPinsOnlyAfterTouchingAViewportEdge()
    {
        var layoutType = typeof(DataGridView).Assembly.GetType("DLH.Controls.Wpf.DataGridViewStickyLayout")!;
        var calculate = layoutType.GetMethod("Calculate", System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic)!;

        static (string Edge, double Position) Result(System.Reflection.MethodInfo calculate, params object[] values)
        {
            var result = calculate.Invoke(null, values)!;
            var type = result.GetType();
            return (type.GetProperty("Edge")!.GetValue(result)!.ToString()!,
                (double)type.GetProperty("Position")!.GetValue(result)!);
        }

        Assert.AreEqual(("Natural", 40d), Result(calculate, 40d, 20d, 0d, 100d, 0d, 0d));
        Assert.AreEqual(("Start", 0d), Result(calculate, -1d, 20d, 0d, 100d, 0d, 0d));
        Assert.AreEqual(("End", 80d), Result(calculate, 81d, 20d, 0d, 100d, 0d, 0d));
        Assert.AreEqual(("Start", 20d), Result(calculate, 10d, 15d, 0d, 100d, 20d, 0d));
        Assert.AreEqual(("End", 70d), Result(calculate, 80d, 15d, 0d, 100d, 0d, 15d));
        Assert.ThrowsExactly<System.Reflection.TargetInvocationException>(() =>
            calculate.Invoke(null, [double.NaN, 20d, 0d, 100d, 0d, 0d]));
    }

    [STATestMethod]
    public void PinningIsOptionalLimitedAndObservable()
    {
        var first = new Row("Primeira");
        var second = new Row("Segunda");
        var rows = new ObservableCollection<Row> { first, second };
        var grid = new DataGridView { ItemsSource = rows, MaxPinnedRows = 1, MaxPinnedColumns = 1 };
        var firstColumn = new DataGridTextColumn { Header = "Nome", Binding = new System.Windows.Data.Binding(nameof(Row.Name)) };
        var secondColumn = new DataGridTextColumn { Header = "Outro" };
        grid.Columns.Add(firstColumn);
        grid.Columns.Add(secondColumn);

        Assert.IsFalse(grid.CanPinRows);
        Assert.IsFalse(grid.CanPinColumns);
        Assert.IsFalse(grid.PinRow(first));
        Assert.IsFalse(grid.PinColumn(firstColumn));

        var rowPinned = 0;
        var rowUnpinned = 0;
        var columnPinned = 0;
        var columnUnpinned = 0;
        grid.RowPinned += (_, args) => { Assert.AreSame(first, args.Item); rowPinned++; };
        grid.RowUnpinned += (_, args) => { Assert.AreSame(first, args.Item); rowUnpinned++; };
        grid.ColumnPinned += (_, args) => { Assert.AreSame(firstColumn, args.Column); columnPinned++; };
        grid.ColumnUnpinned += (_, args) => { Assert.AreSame(firstColumn, args.Column); columnUnpinned++; };

        grid.CanPinRows = true;
        grid.CanPinColumns = true;
        Assert.IsTrue(grid.PinRow(first));
        Assert.IsFalse(grid.PinRow(first));
        Assert.IsFalse(grid.PinRow(second));
        Assert.IsTrue(grid.PinColumn(firstColumn));
        Assert.IsFalse(grid.PinColumn(firstColumn));
        Assert.IsFalse(grid.PinColumn(secondColumn));
        Assert.HasCount(1, grid.PinnedRows);
        Assert.HasCount(1, grid.PinnedColumns);

        rows.Remove(first);
        Assert.IsEmpty(grid.PinnedRows);
        grid.Columns.Remove(firstColumn);
        Assert.IsEmpty(grid.PinnedColumns);
        Assert.AreEqual(1, rowPinned);
        Assert.AreEqual(1, rowUnpinned);
        Assert.AreEqual(1, columnPinned);
        Assert.AreEqual(1, columnUnpinned);
        Assert.ThrowsExactly<ArgumentException>(() => grid.MaxPinnedRows = 0);
        Assert.ThrowsExactly<ArgumentException>(() => grid.MaxPinnedColumns = 0);
    }

    [STATestMethod]
    public void PinnedRowAndColumnBecomeVisualOnlyAfterLeavingTheViewport()
    {
        var rows = new ObservableCollection<Row>(Enumerable.Range(1, 200).Select(index => new Row($"Linha {index}")));
        var grid = new DataGridView
        {
            Width = 320,
            Height = 220,
            ItemsSource = rows,
            CanPinRows = true,
            CanPinColumns = true
        };
        var firstColumn = new DataGridTextColumn { Header = "Nome", Binding = new System.Windows.Data.Binding(nameof(Row.Name)), Width = 220 };
        grid.Columns.Add(firstColumn);
        grid.Columns.Add(new DataGridTextColumn { Header = "Complemento", Binding = new System.Windows.Data.Binding(nameof(Row.Name)), Width = 420 });
        var window = new Window { Content = grid, Width = 340, Height = 260, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
        window.Show();
        try
        {
            grid.UpdateLayout();
            Assert.IsTrue(grid.PinRow(rows[0]));
            Assert.IsTrue(grid.PinColumn(firstColumn));
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;
            Assert.IsEmpty(layer.Children.Cast<UIElement>());

            viewer.ScrollToHorizontalOffset(260);
            viewer.ScrollToVerticalOffset(500);
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            Assert.IsGreaterThanOrEqualTo(2, layer.Children.Count,
                "A linha e a coluna devem ganhar representações aderentes depois de cruzarem as bordas.");
            var overlays = layer.Children.Cast<UIElement>().ToArray();
            viewer.ScrollToHorizontalOffset(300);
            viewer.ScrollToVerticalOffset(650);
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            CollectionAssert.AreEqual(overlays, layer.Children.Cast<UIElement>().ToArray(),
                "As camadas devem ser reutilizadas enquanto permanecerem na mesma borda.");

            viewer.ScrollToHorizontalOffset(0);
            viewer.ScrollToVerticalOffset(0);
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            Assert.IsEmpty(layer.Children.Cast<UIElement>(),
                "As representações aderentes devem desaparecer ao reencontrar as posições naturais.");
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void PinnedItemsCanAdhereToTheBottomAndRightEdges()
    {
        var rows = new ObservableCollection<Row>(Enumerable.Range(1, 100).Select(index => new Row($"Linha {index}")));
        var grid = new DataGridView { Width = 320, Height = 220, ItemsSource = rows, CanPinRows = true, CanPinColumns = true };
        grid.Columns.Add(new DataGridTextColumn { Header = "Primeira", Binding = new System.Windows.Data.Binding(nameof(Row.Name)), Width = 220 });
        var secondColumn = new DataGridTextColumn { Header = "Segunda", Binding = new System.Windows.Data.Binding(nameof(Row.Name)), Width = 420 };
        grid.Columns.Add(secondColumn);
        var window = new Window { Content = grid, Width = 340, Height = 260, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
        window.Show();
        try
        {
            grid.UpdateLayout();
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ScrollToHorizontalOffset(280);
            viewer.ScrollToVerticalOffset(500);
            grid.UpdateLayout();
            var visibleRowIndex = Enumerable.Range(1, rows.Count - 1)
                .First(index => grid.ItemContainerGenerator.ContainerFromIndex(index) is DataGridRow);
            Assert.IsTrue(grid.PinRow(rows[visibleRowIndex]));
            Assert.IsTrue(grid.PinColumn(secondColumn));

            viewer.ScrollToHorizontalOffset(0);
            viewer.ScrollToVerticalOffset(0);
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;
            Assert.IsGreaterThanOrEqualTo(2, layer.Children.Count,
                "Os itens devem permanecer visíveis ao cruzar as bordas inferior e direita.");
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void PinnedRowsAndColumnsRoundTripThroughGridState()
    {
        var rows = new ObservableCollection<Row> { new("Um"), new("Dois") };
        var grid = new DataGridView
        {
            ItemsSource = rows,
            CanPinRows = true,
            CanPinColumns = true,
            RowKeyMemberPath = nameof(Row.Name)
        };
        var column = new DataGridTextColumn
        {
            Header = "Nome",
            Binding = new System.Windows.Data.Binding(nameof(Row.Name)),
            SortMemberPath = nameof(Row.Name),
            Width = 120
        };
        grid.Columns.Add(column);
        column.DisplayIndex = 0;
        Assert.IsTrue(grid.PinRow(rows[1]));
        Assert.IsTrue(grid.PinColumn(column));

        var state = grid.CaptureState();
        Assert.HasCount(1, state.PinnedRowKeys);
        Assert.HasCount(1, state.PinnedColumnKeys);
        grid.UnpinAllRows();
        grid.UnpinAllColumns();
        grid.RestoreState(state);

        Assert.HasCount(1, grid.PinnedRows);
        Assert.HasCount(1, grid.PinnedColumns);
        Assert.AreSame(rows[1], grid.PinnedRows[0]);
        Assert.AreSame(column, grid.PinnedColumns[0]);
    }

    [STATestMethod]
    public void PinningMenusToggleTheSelectedRowAndColumn()
    {
        var row = new Row("Registro");
        var grid = new DataGridView { ItemsSource = new[] { row }, CanPinRows = true, CanPinColumns = true };
        var column = new DataGridTextColumn { Header = "Nome", SortMemberPath = nameof(Row.Name) };
        grid.Columns.Add(column);
        var headerMenu = (ContextMenu)typeof(DataGridView).GetMethod("CreateColumnHeaderMenu",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(grid, [column])!;
        var pinColumn = Assert.IsInstanceOfType<MenuItem>(headerMenu.Items[0]);
        Assert.AreEqual("Fixar coluna", pinColumn.Header);
        pinColumn.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.AreSame(column, grid.PinnedColumns.Single());

        var rowMenu = (ContextMenu)typeof(DataGridView).GetMethod("CreateRowPinMenu",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(grid, [row, column])!;
        var pinRow = Assert.IsInstanceOfType<MenuItem>(rowMenu.Items[0]);
        Assert.AreEqual("Fixar linha", pinRow.Header);
        pinRow.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Assert.AreSame(row, grid.PinnedRows.Single());
    }

    private sealed record Row(string Name);
}
