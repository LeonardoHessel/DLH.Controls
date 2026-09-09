using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
                grid.SelectedItem = rows[1];
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

            Test("datagrid/column visibility menu and last visible guard", () =>
            {
                var method = typeof(DataGridView).GetMethod("CreateColumnVisibilityMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                var menu = (ContextMenu)method.Invoke(grid, null)!;
                Check(menu.Items.Count == 3 && menu.Items.Cast<MenuItem>().All(item => item.IsCheckable && item.IsChecked), "Visibility menu does not represent columns");
                var statusItem = menu.Items.Cast<MenuItem>().Single(item => ReferenceEquals(item.Tag, status));
                statusItem.IsChecked = false; statusItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Check(status.Visibility == Visibility.Collapsed, "Menu did not hide column");
                name.Visibility = Visibility.Collapsed;
                menu = (ContextMenu)method.Invoke(grid, null)!;
                var onlyVisible = menu.Items.Cast<MenuItem>().Single(item => ReferenceEquals(item.Tag, quantity));
                Check(onlyVisible.IsChecked && !onlyVisible.IsEnabled, "Last visible column can be hidden");
                var hidden = menu.Items.Cast<MenuItem>().Single(item => ReferenceEquals(item.Tag, status));
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
            });
        }
        finally { window.Close(); Pump(); }
        Console.WriteLine($"DATAGRIDVIEW: {passed}/{passed} passed; 0 failed.");
    }
}
