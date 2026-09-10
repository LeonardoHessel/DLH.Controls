using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DLH.Controls.Wpf;

internal static class DataGridViewTests
{
    private sealed record Row(string Name, int Quantity, string Status);

    private sealed class Command(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
    {
        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    public static void Run()
    {
        var passed = 0;
        void Test(string name, Action action)
        {
            action(); passed++; Console.WriteLine("PASS: " + name);
        }
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Pump()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        var rows = new ObservableCollection<Row>
        {
            new("Embarque 20", 20, "Pendente"),
            new("Embarque 3", 3, "Concluído"),
            new("Embarque 11", 11, "Pendente")
        };
        var grid = new DataGridView { ItemsSource = rows, Width = 620, Height = 260 };
        var name = new DataGridTextColumn { Header = "Embarque", Binding = new Binding(nameof(Row.Name)), SortMemberPath = nameof(Row.Name) };
        var quantity = new DataGridTextColumn { Header = "Quantidade", Binding = new Binding(nameof(Row.Quantity)), SortMemberPath = nameof(Row.Quantity) };
        var status = new DataGridTextColumn { Header = "Status", Binding = new Binding(nameof(Row.Status)), SortMemberPath = nameof(Row.Status) };
        grid.Columns.Add(name); grid.Columns.Add(quantity); grid.Columns.Add(status);
        var window = new Window { Content = grid };
        grid.Measure(new Size(620, 260)); grid.Arrange(new Rect(0, 0, 620, 260)); grid.UpdateLayout(); Pump();
        try
        {
            Test("datagrid/defaults and virtualization", () =>
            {
                Check(grid.SelectionBehavior == DataGridViewSelectionBehavior.None, "Selection should be disabled by default");
                Check(grid.CanUserReorderColumns && grid.CanUserToggleColumnVisibility && grid.ShowRowSeparators, "Column options use wrong defaults");
                Check(grid.EnableRowVirtualization && grid.EnableColumnVirtualization && !grid.AutoGenerateColumns && grid.IsReadOnly, "Safe WPF defaults were not applied");
                Check(grid.CellPadding == new Thickness(10, 7, 10, 7), "Cell padding default is wrong");
                Check(grid.Density == DataGridViewDensity.Default && grid.CornerRadius == new CornerRadius(10), "Visual defaults are wrong");
                Check(!grid.IsLoading && grid.LoadingMessage == "Carregando..." && grid.EmptyMessage == "Nenhum registro encontrado.", "State defaults are wrong");
                Check(grid.ShowSortIndicators && grid.SortIconSize == 14 && grid.UnsortedIcon is not null &&
                      grid.AscendingSortIcon is not null && grid.DescendingSortIcon is not null,
                      "Sort indicator defaults are wrong");
                Check(grid.ShowClearSortMenuItem && grid.ShowRestoreDefaultSortMenuItem,
                      "Sort menu actions should be visible by default");
                Check(grid.ScrollBarThickness == 10 && grid.ScrollBarTrackBrush is not null &&
                      grid.ScrollBarThumbBrush is not null && grid.ScrollBarThumbHoverBrush is not null,
                      "Scrollbar defaults are wrong");
            });

            Test("datagrid/density and data states", () =>
            {
                grid.Density = DataGridViewDensity.Compact;
                Check(grid.CellPadding == new Thickness(12, 6, 12, 6), "Compact density was not applied");
                grid.Density = DataGridViewDensity.Comfortable;
                Check(grid.CellPadding == new Thickness(18, 12, 18, 12), "Comfortable density was not applied");
                grid.IsLoading = true;
                grid.ErrorMessage = "Falha de teste";
                Check(grid.IsLoading && grid.ErrorMessage == "Falha de teste", "Data states were not retained");
                grid.IsLoading = false;
                grid.ErrorMessage = null;
            });

            Test("datagrid/rounded content clipping", () =>
            {
                grid.CornerRadius = new CornerRadius(16);
                grid.ApplyTemplate();
                grid.UpdateLayout();
                var clipRoot = (FrameworkElement)grid.Template.FindName("PART_ClipRoot", grid)!;
                Check(clipRoot.Clip is RectangleGeometry { RadiusX: 15, RadiusY: 15 },
                    "Content does not use the configured rounded clipping");
            });

            Test("datagrid/scrollbar radius is half its cross axis", () =>
            {
                var converterType = typeof(DataGridView).Assembly.GetType("DLH.Controls.Wpf.HalfValueToCornerRadiusConverter")!;
                var converter = (IValueConverter)Activator.CreateInstance(converterType, nonPublic: true)!;
                Check((CornerRadius)converter.Convert(6d, typeof(CornerRadius), null!, System.Globalization.CultureInfo.InvariantCulture) == new CornerRadius(3),
                    "Thin scrollbar radius exceeded half its dimension");
                Check((CornerRadius)converter.Convert(20d, typeof(CornerRadius), null!, System.Globalization.CultureInfo.InvariantCulture) == new CornerRadius(10),
                    "Thick scrollbar radius exceeded half its dimension");
            });

            Test("datagrid/row, column and cell selection command", () =>
            {
                var notifications = new List<DataGridViewSelection>();
                grid.SelectionChangedCommand = new Command(value => notifications.Add((DataGridViewSelection)value!));
                grid.SelectionBehavior = DataGridViewSelectionBehavior.Row;
                grid.SelectedItem = rows[0]; Pump();
                Check(notifications.Last() is { Item: not null, Column: null }, "Row selection payload is wrong");

                grid.SelectionBehavior = DataGridViewSelectionBehavior.Column;
                grid.SelectedColumn = quantity; Pump();
                Check(notifications.Last() is { Item: null, Column: not null } selection && ReferenceEquals(selection.Column, quantity), "Column selection payload is wrong");

                grid.SelectionBehavior = DataGridViewSelectionBehavior.Cell;
                grid.CurrentCell = new DataGridCellInfo(rows[1], status);
                grid.SelectedCells.Clear(); grid.SelectedCells.Add(grid.CurrentCell); Pump();
                Check(notifications.Last() is { Item: not null, Column: not null } cell && ReferenceEquals(cell.Item, rows[1]) && ReferenceEquals(cell.Column, status), "Cell selection payload is wrong");

                grid.SelectionBehavior = DataGridViewSelectionBehavior.None;
                Check(grid.SelectedItem is null && grid.SelectedColumn is null && !grid.CurrentCell.IsValid, "None did not clear selection");
            });

            Test("datagrid/column reorder", () =>
            {
                status.DisplayIndex = 0;
                Check(status.DisplayIndex == 0 && name.DisplayIndex == 1 && quantity.DisplayIndex == 2, "Native column reorder failed");
            });

            Test("datagrid/multiple row selection and batch action", () =>
            {
                var changes = new List<DataGridViewBatchSelection>();
                DataGridViewBatchSelection? action = null;
                grid.BatchSelectionChangedCommand = new Command(value => changes.Add((DataGridViewBatchSelection)value!));
                grid.BatchActionCommand = new Command(value => action = (DataGridViewBatchSelection)value!);
                grid.SelectionBehavior = DataGridViewSelectionBehavior.Row;
                grid.AllowMultipleSelection = true;
                grid.SelectedItems.Add(rows[0]); grid.SelectedItems.Add(rows[1]); Pump();
                Check(grid.SelectionMode == DataGridSelectionMode.Extended && grid.GetBatchSelection().Items.Count == 2,
                    "Extended row selection did not retain both items");
                Check(changes.Count > 0 && grid.ExecuteBatchAction() && action?.Items.Count == 2,
                    "Batch commands did not receive the selection");
                grid.UnselectAll(); grid.AllowMultipleSelection = false;
                Check(grid.SelectionMode == DataGridSelectionMode.Single, "Single selection was not restored");
            });

            Test("datagrid/optional editing and validation appearance", () =>
            {
                Check(!grid.IsCellEditingEnabled && grid.IsReadOnly && grid.ShowValidationErrors,
                    "Safe editing defaults are wrong");
                grid.IsCellEditingEnabled = true;
                Check(!grid.IsReadOnly, "Editing did not unlock the native grid");
                var brush = new SolidColorBrush(Colors.OrangeRed);
                grid.ValidationErrorBrush = brush;
                Check(ReferenceEquals(grid.ValidationErrorBrush, brush), "Validation brush was not retained");
                grid.IsCellEditingEnabled = false;
                Check(grid.IsReadOnly, "Read-only mode was not restored");
            });

            Test("datagrid/csv follows view and visible display order", () =>
            {
                name.DisplayIndex = 0; quantity.DisplayIndex = 1; status.DisplayIndex = 2;
                quantity.Visibility = Visibility.Collapsed;
                grid.SetFilter(status, "Pendente", DataGridViewFilterOperator.Equals);
                grid.ApplySort(name, ListSortDirection.Descending);
                using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
                grid.ExportCsv(writer);
                var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                Check(lines.Length == 3 && lines[0] == "Embarque;Status" && lines[1].StartsWith("Embarque 20;"),
                    "CSV ignored visible columns, filtering or sorting");
                using var customWriter = new StringWriter();
                grid.ExportCsv(customWriter, new DataGridViewCsvOptions
                {
                    Delimiter = ",",
                    ValueSelector = (item, column) => column == status ? "texto, com \"aspas\"" : null
                });
                Check(customWriter.ToString().Contains("\"texto, com \"\"aspas\"\"\""), "CSV escaping is invalid");
                grid.ClearFilters(); grid.ClearSorting(); quantity.Visibility = Visibility.Visible;
            });

            Test("datagrid/grouping and expandable details", () =>
            {
                grid.GroupMemberPath = nameof(Row.Status);
                grid.IsGroupingEnabled = true;
                var view = CollectionViewSource.GetDefaultView(rows);
                Check(view.GroupDescriptions.OfType<PropertyGroupDescription>().Any(group => group.PropertyName == nameof(Row.Status)),
                    "Configured grouping was not applied");
                grid.ShowRowDetailsOnSelection = true;
                Check(grid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.VisibleWhenSelected,
                    "Selection details mode was not applied");
                grid.ShowRowDetailsOnSelection = false;
                grid.IsGroupingEnabled = false;
                Check(view.GroupDescriptions.Count == 0 && grid.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.Collapsed,
                    "Grouping or details mode was not cleared");
            });

            Test("datagrid/sort indicator customization", () =>
            {
                grid.ShowSortIndicators = false;
                grid.SortIconSize = 18;
                var custom = Geometry.Parse("M 0,0 L 5,5");
                grid.AscendingSortIcon = custom;
                Check(!grid.ShowSortIndicators && grid.SortIconSize == 18 && ReferenceEquals(grid.AscendingSortIcon, custom),
                    "Sort indicator customization was not retained");
                var rejected = false;
                try { grid.SortIconSize = 0; } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Invalid sort icon size was accepted");
                grid.ShowSortIndicators = true;
            });

            Test("datagrid/global sorting switch", () =>
            {
                grid.CanUserSortColumns = false;
                Check(!grid.CanUserSortColumns, "Global sorting was not disabled");
                grid.CanUserSortColumns = true;
                Check(grid.CanUserSortColumns, "Global sorting was not restored");
            });

            Test("datagrid/clear and restore default sorting", () =>
            {
                grid.DefaultSortMemberPath = nameof(Row.Quantity);
                grid.DefaultSortDirection = ListSortDirection.Descending;
                Check(grid.ApplyDefaultSort(), "Default sorting could not be applied");
                Check(quantity.SortDirection == ListSortDirection.Descending &&
                      CollectionViewSource.GetDefaultView(rows).SortDescriptions is [{ PropertyName: nameof(Row.Quantity), Direction: ListSortDirection.Descending }],
                      "Default sorting used the wrong column or direction");
                grid.ClearSorting();
                Check(grid.Columns.All(column => column.SortDirection is null) &&
                      CollectionViewSource.GetDefaultView(rows).SortDescriptions.Count == 0,
                      "Sorting was not cleared");
            });

            Test("datagrid/multiple sorting and priority", () =>
            {
                grid.IsMultiColumnSortEnabled = true;
                Check(grid.ApplySort(status, ListSortDirection.Ascending), "Primary sort was not applied");
                Check(grid.ApplySort(quantity, ListSortDirection.Descending, append: true), "Secondary sort was not applied");
                var descriptions = CollectionViewSource.GetDefaultView(rows).SortDescriptions;
                Check(descriptions.Count == 2 && descriptions[0].PropertyName == nameof(Row.Status) &&
                      descriptions[1].PropertyName == nameof(Row.Quantity), "Multiple sort order is wrong");
                Check(DataGridView.GetSortPriority(status) == 1 && DataGridView.GetSortPriority(quantity) == 2,
                    "Multiple sort priorities are wrong");
                Check(grid.CaptureState().Sorting.Select(sort => sort.ColumnKey)
                    .SequenceEqual(new[] { nameof(Row.Status), nameof(Row.Quantity) }),
                    "Multiple sorting was not captured in state");
                Check(grid.RemoveSort(status) && descriptions.Count == 1 &&
                      DataGridView.GetSortPriority(status) == 0 && DataGridView.GetSortPriority(quantity) == 0,
                    "Removing a sort did not update priorities");
                grid.ClearSorting();
                grid.IsMultiColumnSortEnabled = false;
            });

            Test("datagrid/column filters compose and preserve external filter", () =>
            {
                var view = CollectionViewSource.GetDefaultView(rows);
                view.Filter = item => ((Row)item).Quantity >= 3;
                Check(grid.SetFilter(status, "pendente", DataGridViewFilterOperator.Equals), "Status filter was rejected");
                Check(view.Cast<Row>().Count() == 2, "Case-insensitive equality filter returned the wrong rows");
                Check(grid.SetFilter(quantity, "15", DataGridViewFilterOperator.GreaterThan), "Numeric filter was rejected");
                Check(view.Cast<Row>().SequenceEqual(new[] { rows[0] }), "Column filters were not combined");
                Check(grid.ClearFilter(quantity) && view.Cast<Row>().Count() == 2, "Column filter was not cleared");
                grid.ClearFilters();
                Check(view.Cast<Row>().Count() == 3 && view.Filter is not null, "External filter was not restored");
                view.Filter = null;
            });

            Test("datagrid/filter options and menu", () =>
            {
                DataGridView.SetCanUserFilter(quantity, false);
                Check(!grid.SetFilter(quantity, "10"), "A disabled column accepted a filter");
                DataGridView.SetCanUserFilter(quantity, true);
                DataGridView.SetFilterMemberPath(status, nameof(Row.Status));
                grid.SetFilter(status, "Pendente");
                var method = typeof(DataGridView).GetMethod("CreateColumnHeaderMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                var menu = (ContextMenu)method.Invoke(grid, new object?[] { status })!;
                Check(menu.Items.OfType<MenuItem>().Any(item => Equals(item.Header, "Limpar filtro desta coluna")) &&
                      menu.Items.OfType<MenuItem>().Any(item => Equals(item.Header, "Limpar todos os filtros")),
                    "Filter cleanup actions are missing");
                grid.ClearFilters();
            });

            Test("datagrid/column state roundtrip, JSON and new columns", () =>
            {
                name.DisplayIndex = 0; quantity.DisplayIndex = 1; status.DisplayIndex = 2;
                name.Width = new DataGridLength(2, DataGridLengthUnitType.Star);
                quantity.Width = new DataGridLength(96, DataGridLengthUnitType.Pixel);
                quantity.Visibility = Visibility.Collapsed;
                grid.DefaultSortMemberPath = nameof(Row.Quantity);
                grid.DefaultSortDirection = ListSortDirection.Descending;
                grid.ApplyDefaultSort();
                var state = grid.CaptureState();
                Check(state.Version == 1 && state.Columns.Count == 3 && state.Sorting.Count == 1,
                    "Captured state is incomplete");

                var extra = new DataGridTextColumn { Header = "Extra", SortMemberPath = "Extra", Width = new DataGridLength(40) };
                grid.Columns.Add(extra);
                status.DisplayIndex = 0;
                name.Width = new DataGridLength(33);
                quantity.Visibility = Visibility.Visible;
                grid.ClearSorting();
                var restored = 0;
                grid.StateRestored += (_, e) => { restored++; Check(e.State.Version == 1, "Restored event state is invalid"); };
                grid.RestoreState(state);
                Check(name.DisplayIndex == 0 && quantity.DisplayIndex == 1 && status.DisplayIndex == 2 && extra.DisplayIndex == 3,
                    "Column order or new-column placement was not restored");
                Check(name.Width.UnitType == DataGridLengthUnitType.Star && name.Width.Value == 2 &&
                      quantity.Width.UnitType == DataGridLengthUnitType.Pixel && quantity.Width.Value == 96,
                    "Column widths were not restored");
                Check(quantity.Visibility == Visibility.Collapsed && quantity.SortDirection == ListSortDirection.Descending,
                    "Visibility or sorting was not restored");

                using var stream = new MemoryStream();
                grid.SaveState(stream);
                stream.Position = 0;
                name.Width = new DataGridLength(51);
                grid.LoadState(stream);
                Check(name.Width.UnitType == DataGridLengthUnitType.Star && restored == 2,
                    "JSON state did not restore the layout or event");

                var beforeInvalid = grid.CaptureState();
                var invalid = grid.CaptureState();
                invalid.Columns[1].DisplayIndex = invalid.Columns[0].DisplayIndex;
                var rejected = false;
                try { grid.RestoreState(invalid); } catch (ArgumentException) { rejected = true; }
                Check(rejected && grid.CaptureState().Columns.Select(column => column.DisplayIndex)
                        .SequenceEqual(beforeInvalid.Columns.Select(column => column.DisplayIndex)),
                    "Invalid state was not rejected atomically");

                grid.Columns.Remove(extra);
                name.Width = DataGridLength.Auto;
                quantity.Width = DataGridLength.Auto;
                quantity.Visibility = Visibility.Visible;
                grid.ClearSorting();
            });

            Test("datagrid/sort menu action visibility", () =>
            {
                var method = typeof(DataGridView).GetMethod("CreateColumnHeaderMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                grid.ShowClearSortMenuItem = false;
                grid.ShowRestoreDefaultSortMenuItem = true;
                var menu = (ContextMenu)method.Invoke(grid, new object?[] { null })!;
                Check(menu.Items.OfType<MenuItem>().All(item => !Equals(item.Header, "Limpar ordenação")) &&
                      menu.Items.OfType<MenuItem>().Any(item => Equals(item.Header, "Restaurar ordenação padrão")),
                      "Clear action visibility was not respected");
                grid.ShowRestoreDefaultSortMenuItem = false;
                menu = (ContextMenu)method.Invoke(grid, new object?[] { null })!;
                Check(menu.Items.OfType<MenuItem>().All(item => !Equals(item.Header, "Restaurar ordenação padrão")),
                      "Restore action visibility was not respected");
                grid.ShowClearSortMenuItem = true;
                grid.ShowRestoreDefaultSortMenuItem = true;
            });

            Test("datagrid/column visibility menu and last visible guard", () =>
            {
                var method = typeof(DataGridView).GetMethod("CreateColumnVisibilityMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                var menu = (ContextMenu)method.Invoke(grid, null)!;
                var columnItems = menu.Items.OfType<MenuItem>().Where(item => item.Tag is DataGridColumn).ToList();
                Check(columnItems.Count == 3 && columnItems.All(item => item.IsCheckable && item.IsChecked), "Visibility menu does not represent columns");
                var statusItem = columnItems.Single(item => ReferenceEquals(item.Tag, status));
                statusItem.IsChecked = false; statusItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Check(status.Visibility == Visibility.Collapsed, "Menu did not hide column");
                name.Visibility = Visibility.Collapsed;
                menu = (ContextMenu)method.Invoke(grid, null)!;
                var onlyVisible = menu.Items.OfType<MenuItem>().Single(item => ReferenceEquals(item.Tag, quantity));
                Check(onlyVisible.IsChecked && !onlyVisible.IsEnabled, "Last visible column can be hidden");
                var hidden = menu.Items.OfType<MenuItem>().Single(item => ReferenceEquals(item.Tag, status));
                Check(!hidden.IsChecked && hidden.IsEnabled, "Hidden column cannot be restored");
                hidden.IsChecked = true; hidden.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Check(status.Visibility == Visibility.Visible, "Menu did not restore column");
            });

            Test("datagrid/observable source and row separators", () =>
            {
                rows.Add(new("Embarque 4", 4, "Pendente")); Pump();
                Check(grid.Items.Count == 4, "Observable source addition was not reflected");
                grid.ShowRowSeparators = false;
                Check(grid.GridLinesVisibility == DataGridGridLinesVisibility.None, "Separators were not disabled");
                grid.ShowRowSeparators = true;
                Check(grid.GridLinesVisibility == DataGridGridLinesVisibility.Horizontal, "Separators were not restored");
            });

            Test("datagrid/invalid padding rejected", () =>
            {
                var rejected = false;
                try { grid.CellPadding = new Thickness(-1); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Negative padding was accepted");
                rejected = false;
                try { grid.ScrollBarThickness = 0; } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Invalid scrollbar thickness was accepted");
            });
        }
        finally { window.Close(); Pump(); }
        Console.WriteLine($"DATAGRIDVIEW: {passed}/{passed} passed; 0 failed.");
    }
}
