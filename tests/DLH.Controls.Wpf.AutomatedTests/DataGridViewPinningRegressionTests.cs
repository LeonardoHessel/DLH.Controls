using System.Collections;
using System.Collections.Specialized;
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

    // MouseButtonEventArgs.ClickCount has no public setter; WPF only assigns it while
    // routing a real physical click. Tests simulating a double click need to poke the
    // private backing field directly.
    private static void SetClickCount(MouseButtonEventArgs args, int clickCount) =>
        typeof(MouseButtonEventArgs).GetField("_count", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(args, clickCount);

    [STATestMethod]
    public void HidingPinnedColumnReleasesItsSlotAndRaisesUnpinned() => WithPinnedColumn((grid, layer, column) =>
    {
        var unpinned = 0;
        grid.ColumnUnpinned += (_, args) =>
        {
            Assert.AreSame(column, args.Column);
            unpinned++;
        };
        column.Visibility = Visibility.Collapsed;
        DrainLayout(grid);
        Assert.IsEmpty(layer.Children.OfType<DataGridView>());
        Assert.IsEmpty(layer.Children.OfType<Border>().Where(border => ReferenceEquals(border.Tag, column)));
        Assert.IsEmpty(grid.PinnedColumns, "Ocultar deve liberar imediatamente o limite de colunas fixadas.");
        Assert.AreEqual(1, unpinned);
        column.Visibility = Visibility.Visible;
        DrainLayout(grid);
        Assert.IsEmpty(grid.PinnedColumns, "Exibir novamente não deve restaurar uma fixação removida pelo usuário.");
    });

    [STATestMethod]
    public void RebuildingPinnedColumnOverlayDoesNotRetainSourceSubscription()
    {
        var source = new CountingRows(new EditableRow { Name = "Original" });
        var grid = new DataGridView
        {
            Width = 300, Height = 180, CanPinColumns = true, ItemsSource = source
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
            grid.PinColumn(column);
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ScrollToHorizontalOffset(150);
            DrainLayout(grid);
            var subscriptionsWithOverlay = source.SubscriberCount;

            grid.PinnedBoundarySeparatorThickness = 3;
            DrainLayout(grid);

            Assert.AreEqual(subscriptionsWithOverlay, source.SubscriberCount,
                "Substituir o overlay deve remover sua assinatura antes de criar a próxima representação.");
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void FilteredPinnedRowInvalidatesItsCapturedOverlay()
    {
        var rows = Enumerable.Range(0, 30).Select(index => new EditableRow { Name = $"Linha {index}" }).ToArray();
        var grid = new DataGridView
        {
            Width = 300, Height = 180, ItemsSource = rows, CanPinRows = true
        };
        var column = new DataGridTextColumn
        {
            Header = "Nome", Binding = new Binding(nameof(EditableRow.Name)), SortMemberPath = nameof(EditableRow.Name), Width = 600
        };
        grid.Columns.Add(column);
        var window = new Window
        {
            Content = grid, Width = 320, Height = 220,
            ShowInTaskbar = false, WindowStyle = WindowStyle.None
        };
        window.Show();
        try
        {
            DrainLayout(grid);
            Assert.IsTrue(grid.PinRow(rows[0]));
            DrainLayout(grid);
            var view = CollectionViewSource.GetDefaultView(rows);
            view.Filter = item => !ReferenceEquals(item, rows[0]);
            view.Refresh();
            var viewer = (ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ScrollToVerticalOffset(150);
            DrainLayout(grid);
            var layer = (Canvas)grid.Template.FindName("PART_PinningLayer", grid)!;

            Assert.AreSame(rows[0], grid.PinnedRows.Single(),
                "O filtro não deve apagar a preferência de fixação.");
            Assert.IsEmpty(layer.Children.OfType<DataGridView>().Where(overlay => overlay.Items.Contains(rows[0])),
                "Uma linha sem contêiner atual não pode reutilizar posição ou fundo capturados anteriormente.");
        }
        finally { window.Close(); }
    }

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
        Assert.IsTrue(overlay.IsReadOnly, "A representação fixada deve ser estritamente visual.");
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
        var beginningEdit = 0;
        var cellEditEnding = 0;
        var rowEditEnding = 0;
        grid.BeginningEdit += (_, _) => beginningEdit++;
        grid.CellEditEnding += (_, _) => cellEditEnding++;
        grid.RowEditEnding += (_, _) => rowEditEnding++;
        Assert.IsTrue(grid.BeginEdit(), "A edição deve ocorrer no grid original.");
        var editor = (TextBox)column.GetCellContent(item);
        editor.Text = "Editado";
        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Row, true));
        Assert.AreEqual("Editado", ((EditableRow)item).Name);
        Assert.AreEqual(1, beginningEdit);
        Assert.AreEqual(1, cellEditEnding);
        Assert.AreEqual(1, rowEditEnding);
    });

    [STATestMethod]
    public void DoubleClickingPinnedCellBeginsEditOnOriginalColumn() => WithPinnedColumn((grid, layer, column) =>
    {
        var overlay = layer.Children.OfType<DataGridView>().Single();
        var item = grid.Items[0];
        var overlayCell = (DataGridCell)overlay.Columns[0].GetCellContent(item).Parent;
        var beginningEdit = 0;
        grid.BeginningEdit += (_, _) => beginningEdit++;
        var doubleClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = Mouse.PreviewMouseDownEvent, Source = overlayCell
        };
        SetClickCount(doubleClick, 2);
        overlayCell.RaiseEvent(doubleClick);
        DrainLayout(grid);
        Assert.AreEqual(1, beginningEdit, "O duplo clique na célula fixada deve iniciar a edição no grid original.");
        var editor = Assert.IsInstanceOfType<TextBox>(column.GetCellContent(item), "A edição deve abrir no grid original, não no overlay.");
        editor.Text = "Editado";
        Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Row, true));
        Assert.AreEqual("Editado", ((EditableRow)item).Name);
    });

    [STATestMethod]
    public void PinnedCheckboxEditingIsOwnedByOriginalGridEvents()
    {
        var row = new EditableBooleanRow();
        var grid = new DataGridView
        {
            Width = 300, Height = 180, CanPinColumns = true,
            ItemsSource = new[] { row }, SelectionBehavior = DataGridViewSelectionBehavior.Cell,
            IsCellEditingEnabled = true
        };
        var column = new DataGridCheckBoxColumn
        {
            Header = "Ativo", Binding = new Binding(nameof(EditableBooleanRow.IsActive)) { Mode = BindingMode.TwoWay }, Width = 100
        };
        grid.Columns.Add(column);
        grid.Columns.Add(new DataGridTextColumn { Header = "Espaço", Binding = new Binding(), Width = 600 });
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
            var overlay = layer.Children.OfType<DataGridView>().Single();
            Assert.IsTrue(overlay.IsReadOnly, "Checkboxes clonados não podem iniciar uma edição própria.");
            var overlayCell = (DataGridCell)overlay.Columns[0].GetCellContent(row).Parent;
            overlayCell.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = Mouse.PreviewMouseDownEvent, Source = overlayCell
            });
            DrainLayout(grid);

            var beginningEdit = 0;
            var cellEditEnding = 0;
            var rowEditEnding = 0;
            grid.BeginningEdit += (_, _) => beginningEdit++;
            grid.CellEditEnding += (_, _) => cellEditEnding++;
            grid.RowEditEnding += (_, _) => rowEditEnding++;
            Assert.IsTrue(grid.BeginEdit());
            var editor = Assert.IsInstanceOfType<CheckBox>(column.GetCellContent(row));
            editor.IsChecked = true;
            Assert.IsTrue(grid.CommitEdit(DataGridEditingUnit.Row, true));

            Assert.IsTrue(row.IsActive);
            Assert.AreEqual(1, beginningEdit);
            Assert.AreEqual(1, cellEditEnding);
            Assert.AreEqual(1, rowEditEnding);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void RestoreRollbackDoesNotRepublishUnchangedPinEvents()
    {
        var rows = new[] { new EditableRow { Name = "Original" } };
        var grid = new DataGridView
        {
            ItemsSource = rows,
            CanPinRows = true,
            CanPinColumns = true,
            RowKeyMemberPath = nameof(EditableRow.Name)
        };
        var column = new DataGridTextColumn
        {
            Header = "Nome", Binding = new Binding(nameof(EditableRow.Name)),
            SortMemberPath = nameof(EditableRow.Name)
        };
        grid.Columns.Add(column);
        column.DisplayIndex = 0;
        Assert.IsTrue(grid.PinRow(rows[0]));
        Assert.IsTrue(grid.PinColumn(column));
        var state = grid.CaptureState();
        state.Sorting.Add(new DataGridViewSortState
        {
            ColumnKey = nameof(EditableRow.Name), Direction = System.ComponentModel.ListSortDirection.Ascending
        });

        var view = CollectionViewSource.GetDefaultView(rows);
        var changes = (INotifyCollectionChanged)view.SortDescriptions;
        var throwOnce = true;
        NotifyCollectionChangedEventHandler failure = (_, _) =>
        {
            if (!throwOnce) return;
            throwOnce = false;
            throw new InvalidOperationException("Falha simulada durante a restauração.");
        };
        changes.CollectionChanged += failure;
        var pinEvents = 0;
        grid.RowPinned += (_, _) => pinEvents++;
        grid.RowUnpinned += (_, _) => pinEvents++;
        grid.ColumnPinned += (_, _) => pinEvents++;
        grid.ColumnUnpinned += (_, _) => pinEvents++;
        Exception? restoreError = null;
        try
        {
            grid.RestoreState(state);
        }
        catch (Exception error) { restoreError = error; }
        finally { changes.CollectionChanged -= failure; }

        Assert.IsNotNull(restoreError, "A falha simulada deve acionar o rollback.");
        Assert.AreSame(rows[0], grid.PinnedRows.Single());
        Assert.AreSame(column, grid.PinnedColumns.Single());
        Assert.AreEqual(0, pinEvents,
            "O rollback não deve anunciar desfixação e refixação quando o conjunto efetivo permaneceu igual.");
    }

    [STATestMethod]
    public void RejectedSortingDoesNotRebuildPinnedOverlays() => WithPinnedColumn((grid, layer, column) =>
    {
        column.SortMemberPath = nameof(EditableRow.Name);
        column.CanUserSort = false;
        grid.SetCurrentValue(DataGrid.CanUserSortColumnsProperty, true);
        DrainLayout(grid);
        var overlays = layer.Children.Cast<UIElement>().ToArray();

        typeof(DataGridView).GetMethod("OnGridSorting",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(grid, [grid, new DataGridSortingEventArgs(column)]);
        DrainLayout(grid);

        CollectionAssert.AreEqual(overlays, layer.Children.Cast<UIElement>().ToArray(),
            "Uma coluna não ordenável não deve invalidar nem reconstruir as camadas fixadas.");
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

    private sealed class EditableBooleanRow
    {
        public bool IsActive { get; set; }
    }

    private sealed class CountingRows(params EditableRow[] items) : IEnumerable<EditableRow>, INotifyCollectionChanged
    {
        private readonly IReadOnlyList<EditableRow> items = items;
        private NotifyCollectionChangedEventHandler? collectionChanged;

        public int SubscriberCount { get; private set; }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { collectionChanged += value; SubscriberCount++; }
            remove { collectionChanged -= value; SubscriberCount--; }
        }

        public IEnumerator<EditableRow> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
