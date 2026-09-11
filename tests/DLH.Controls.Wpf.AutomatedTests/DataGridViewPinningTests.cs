using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        grid.LoadingRow += (_, args) => args.Row.Height = rows.IndexOf((Row)args.Row.Item) % 2 == 0 ? 30 : 42;
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
            var rowBoundary = layer.Children.OfType<Border>()
                .Single(element => Equals(element.Tag, "PinnedBoundarySeparator:RowStart"));
            var columnBoundary = layer.Children.OfType<Border>()
                .Single(element => Equals(element.Tag, "PinnedBoundarySeparator:ColumnStart"));
            Assert.AreEqual(grid.PinnedBoundarySeparatorThickness, rowBoundary.Height, 0.1d);
            Assert.AreEqual(grid.PinnedBoundarySeparatorThickness, columnBoundary.Width, 0.1d);
            Assert.AreSame(grid.PinnedBoundarySeparatorBrush, rowBoundary.Background);
            Assert.AreSame(grid.PinnedBoundarySeparatorBrush, columnBoundary.Background);
            var rowOverlay = layer.Children.OfType<DataGridView>()
                .Single(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.None && overlay.Columns.Count > 1);
            rowOverlay.UpdateLayout();
            Assert.AreEqual(0d, rowOverlay.ColumnHeaderHeight, 0.1d,
                "A representação de uma linha fixada não deve reservar espaço para o cabeçalho.");
            Assert.IsNotNull(rowOverlay.ItemContainerGenerator.ContainerFromIndex(0),
                "A célula da linha, e não o cabeçalho, deve ocupar a representação fixada.");
            Assert.IsGreaterThan(0d, rowOverlay.RowBackground.Opacity,
                "A linha fixada deve ter fundo opaco para ocultar o conteúdo que passa por baixo.");
            var fixedRow = (DataGridRow)rowOverlay.ItemContainerGenerator.ContainerFromIndex(0)!;
            Assert.IsInstanceOfType<SolidColorBrush>(fixedRow.Background);
            Assert.AreEqual(byte.MaxValue, ((SolidColorBrush)fixedRow.Background).Color.A,
                "O fundo efetivamente renderizado pela linha fixada deve ser totalmente opaco.");
            var intersection = layer.Children.OfType<DataGridView>()
                .Single(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.None && overlay.Columns.Count == 1);
            Assert.AreSame(rows[0], intersection.Items.Cast<Row>().Single(),
                "A interseção deve exibir o registro fixado, e não a linha móvel da coluna fixada.");
            Assert.AreEqual(firstColumn.Header, intersection.Columns[0].Header,
                "A interseção deve usar a célula da coluna fixada.");
            Assert.IsGreaterThan(0d, intersection.RowBackground.Opacity,
                "A interseção entre linha e coluna fixadas deve ser opaca.");
            var columnOverlay = layer.Children.OfType<DataGridView>()
                .Single(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.Column);
            columnOverlay.UpdateLayout();
            var sharedItem = rows.First(item =>
                grid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow &&
                columnOverlay.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow);
            var sourceHeight = ((DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(sharedItem)!).ActualHeight;
            var fixedColumnHeight = ((DataGridRow)columnOverlay.ItemContainerGenerator.ContainerFromItem(sharedItem)!).ActualHeight;
            Assert.AreEqual(sourceHeight, fixedColumnHeight, 0.1d,
                "A parte fixada e a parte móvel devem reutilizar a mesma altura de cada registro.");
            var backdrop = layer.Children.OfType<Border>()
                .Single(element => Equals(element.Tag, "PinnedRowBackdrop:Start"));
            Assert.IsInstanceOfType<SolidColorBrush>(backdrop.Background);
            Assert.AreEqual(byte.MaxValue, ((SolidColorBrush)backdrop.Background).Color.A,
                "O bloco de linhas fixadas deve ter uma superfície contínua e opaca atrás das linhas.");
            Assert.IsGreaterThanOrEqualTo(fixedRow.ActualHeight, backdrop.Height,
                "A superfície deve avançar até a borda do conjunto para eliminar frestas.");
            var viewport = (FrameworkElement)((ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!).Template
                .FindName("PART_ScrollContentPresenter", (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!)!;
            var viewportTop = viewport.TranslatePoint(new Point(), layer).Y;
            Assert.IsLessThan(viewportTop, Canvas.GetTop(backdrop),
                "O fundo fixado deve avançar sob o cabeçalho para vedar a junção fracionária.");
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
    public void PinnedColumnAdheresWhenItTouchesAnAlreadyPinnedColumn()
    {
        var rows = new ObservableCollection<Row>(Enumerable.Range(1, 20).Select(index => new Row($"Linha {index}")));
        var grid = new DataGridView
        {
            Width = 300,
            Height = 180,
            ItemsSource = rows,
            CanPinColumns = true
        };
        var columns = Enumerable.Range(1, 6).Select(index => new DataGridTextColumn
        {
            Header = $"Coluna {index}",
            Binding = new System.Windows.Data.Binding(nameof(Row.Name)),
            Width = 100
        }).ToArray();
        foreach (var column in columns) grid.Columns.Add(column);
        var window = new Window { Content = grid, Width = 320, Height = 220, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
        window.Show();
        try
        {
            grid.UpdateLayout();
            columns[0].SortMemberPath = nameof(Row.Name);
            Assert.IsTrue(grid.ApplySort(columns[0], ListSortDirection.Descending));
            Assert.IsTrue(grid.PinColumn(columns[0]));
            Assert.IsTrue(grid.PinColumn(columns[2]));
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;

            viewer.ScrollToHorizontalOffset(115);
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

            var columnOverlay = layer.Children.OfType<DataGridView>().Single();
            Assert.HasCount(2, columnOverlay.Columns,
                "As colunas aderentes devem compartilhar um único bloco visual.");
            Assert.AreEqual(200d, columnOverlay.ActualWidth, 1d,
                "O bloco deve ocupar a soma exata das larguras das colunas fixadas.");
            var columnBackdrop = layer.Children.OfType<Border>()
                .Single(element => Equals(element.Tag, "PinnedColumnBackdrop:Start"));
            Assert.IsInstanceOfType<SolidColorBrush>(columnBackdrop.Background);
            Assert.AreEqual(byte.MaxValue, ((SolidColorBrush)columnBackdrop.Background).Color.A,
                "O bloco de colunas fixadas deve ocultar completamente o conteúdo horizontal ao fundo.");
            Assert.IsGreaterThan(200d, columnBackdrop.Width,
                "A superfície deve avançar até a borda do conjunto para eliminar frestas.");
            var headerSeam = layer.Children.OfType<Border>()
                .Single(element => Equals(element.Tag, "PinnedColumnHeaderSeam:Start"));
            Assert.IsInstanceOfType<SolidColorBrush>(headerSeam.Background);
            Assert.AreEqual(byte.MaxValue, ((SolidColorBrush)headerSeam.Background).Color.A,
                "A junção entre o cabeçalho fixado e o primeiro registro deve ser opaca.");
            Assert.AreEqual(ListSortDirection.Descending, columnOverlay.Columns[0].SortDirection,
                "O cabeçalho fixado deve preservar o indicador da ordenação existente.");
            Assert.AreEqual("Linha 9", columnOverlay.Items.Cast<Row>().First().Name,
                "A representação fixada deve compartilhar a ordem já aplicada ao grid principal.");

            typeof(DataGridView).GetMethod("OnGridSorting",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(grid, [grid, new DataGridSortingEventArgs(columns[0])]);
            columns[0].SortDirection = ListSortDirection.Ascending;
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            columnOverlay = layer.Children.OfType<DataGridView>().Single();
            Assert.AreEqual(ListSortDirection.Ascending, columnOverlay.Columns[0].SortDirection,
                "O indicador fixado deve ser atualizado depois do clique que altera a ordenação.");
            Assert.HasCount(2, columnOverlay.Columns,
                "Atualizar o indicador não pode remover a segunda coluna aderente.");

            var secondDirection = columns[1].SortDirection;
            var hitTarget = layer.Children.OfType<Border>().Single(target => ReferenceEquals(target.Tag, columns[0]));
            var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                Source = hitTarget
            };
            hitTarget.RaiseEvent(click);
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            Assert.IsTrue(click.Handled, "O cabeçalho fixado deve consumir o clique.");
            Assert.AreEqual(ListSortDirection.Descending, columns[0].SortDirection);
            Assert.AreEqual(secondDirection, columns[1].SortDirection,
                "O clique não pode atravessar para o cabeçalho que está ao fundo.");
            Assert.HasCount(2, layer.Children.OfType<DataGridView>().Single().Columns,
                "Ordenar pelo cabeçalho fixado deve preservar o empilhamento das colunas.");
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
    public void MultiplePinnedRowIntersectionsMatchTheFullRowBounds()
    {
        var rows = new ObservableCollection<Row>(Enumerable.Range(1, 30).Select(index => new Row($"Linha {index}")));
        var grid = new DataGridView
        {
            Width = 300, Height = 180, ItemsSource = rows, CanPinRows = true, CanPinColumns = true
        };
        var columns = Enumerable.Range(1, 6).Select(index => new DataGridTextColumn
        {
            Header = $"Coluna {index}", Binding = new System.Windows.Data.Binding(nameof(Row.Name)), Width = 100
        }).ToArray();
        foreach (var column in columns) grid.Columns.Add(column);
        grid.LoadingRow += (_, args) => args.Row.Height = rows.IndexOf((Row)args.Row.Item) % 2 == 0 ? 31 : 43;
        var window = new Window { Content = grid, Width = 320, Height = 220, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
        window.Show();
        try
        {
            grid.UpdateLayout();
            grid.PinRow(rows[0]); grid.PinRow(rows[1]);
            grid.PinColumn(columns[0]); grid.PinColumn(columns[2]);
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ScrollToHorizontalOffset(115);
            viewer.ScrollToVerticalOffset(100);
            grid.UpdateLayout();
            grid.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;
            var fullRows = layer.Children.OfType<DataGridView>()
                .Where(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.None && overlay.Columns.Count == 6)
                .ToArray();
            var intersections = layer.Children.OfType<DataGridView>()
                .Where(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.None && overlay.Columns.Count == 2)
                .ToArray();
            Assert.HasCount(2, fullRows);
            Assert.HasCount(2, intersections);
            var columnOverlay = layer.Children.OfType<DataGridView>()
                .Single(overlay => overlay.HeadersVisibility == DataGridHeadersVisibility.Column);
            columnOverlay.UpdateLayout();
            var movingItem = rows.Skip(2).First(item =>
                grid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow &&
                columnOverlay.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow);
            var movingSourceRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(movingItem)!;
            var movingFixedColumnRow = (DataGridRow)columnOverlay.ItemContainerGenerator.ContainerFromItem(movingItem)!;
            Assert.AreEqual(
                movingSourceRow.TranslatePoint(new Point(), layer).Y,
                movingFixedColumnRow.TranslatePoint(new Point(), layer).Y,
                0.6d,
                "As linhas móveis das colunas fixadas devem permanecer alinhadas abaixo do conjunto aderente.");
            var separators = layer.Children.OfType<Border>()
                .Where(element => element.Tag is string tag && tag.StartsWith("PinnedRowSeparator:", StringComparison.Ordinal))
                .ToArray();
            Assert.HasCount(2, separators);
            Assert.IsEmpty(separators.Where(separator => separator.Background is not SolidColorBrush brush ||
                brush.Opacity < 1 || brush.Color.A != byte.MaxValue),
                "Os separadores canônicos devem ser totalmente opacos.");
            foreach (var fullRow in fullRows)
            {
                var item = fullRow.Items.Cast<Row>().Single();
                var intersection = intersections.Single(candidate => ReferenceEquals(candidate.Items.Cast<Row>().Single(), item));
                Assert.AreEqual(Canvas.GetTop(fullRow), Canvas.GetTop(intersection), 0.01d);
                Assert.AreEqual(fullRow.Height, intersection.Height, 0.01d,
                    "A interseção deve usar exatamente os mesmos limites físicos da linha fixada.");
                fullRow.UpdateLayout();
                intersection.UpdateLayout();
                var fullContainer = (DataGridRow)fullRow.ItemContainerGenerator.ContainerFromItem(item)!;
                var intersectionContainer = (DataGridRow)intersection.ItemContainerGenerator.ContainerFromItem(item)!;
                Assert.AreEqual(fullContainer.ActualHeight, intersectionContainer.ActualHeight, 0.01d,
                    "A linha interna da interseção deve ter a mesma altura renderizada que a linha completa.");
                Assert.AreEqual(DataGridGridLinesVisibility.None, fullRow.GridLinesVisibility);
                Assert.AreEqual(DataGridGridLinesVisibility.None, intersection.GridLinesVisibility,
                    "As camadas não devem desenhar separadores independentes em coordenadas diferentes.");
            }
        }
        finally { window.Close(); }
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
