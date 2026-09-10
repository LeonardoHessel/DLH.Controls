using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Unit")]
public sealed class DataGridViewApiTests
{
    private sealed record Row(string Name, int Quantity, string Status);
    private sealed class Command(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    [STATestMethod]
    public void StateRoundTripRestoresLayoutAndSorting()
    {
        var (grid, _, name, quantity, _) = CreateGrid();
        name.Width = new DataGridLength(2, DataGridLengthUnitType.Star);
        quantity.Visibility = System.Windows.Visibility.Collapsed;
        grid.ApplySort(name, ListSortDirection.Descending);
        using var state = new MemoryStream();
        grid.SaveState(state);
        name.Width = new DataGridLength(30);
        quantity.Visibility = System.Windows.Visibility.Visible;
        grid.ClearSorting();
        state.Position = 0;
        grid.LoadState(state);
        Assert.AreEqual(DataGridLengthUnitType.Star, name.Width.UnitType);
        Assert.AreEqual(System.Windows.Visibility.Collapsed, quantity.Visibility);
        Assert.AreEqual(ListSortDirection.Descending, name.SortDirection);
    }

    [STATestMethod]
    public void FiltersAndMultiSortComposeOnTheView()
    {
        var (grid, rows, name, quantity, status) = CreateGrid();
        grid.IsMultiColumnSortEnabled = true;
        Assert.IsTrue(grid.ApplySort(status, ListSortDirection.Ascending));
        Assert.IsTrue(grid.ApplySort(quantity, ListSortDirection.Descending, append: true));
        Assert.IsTrue(grid.SetFilter(status, "Pendente", DataGridViewFilterOperator.Equals));
        var result = CollectionViewSource.GetDefaultView(rows).Cast<Row>().ToArray();
        CollectionAssert.AreEqual(new[] { rows[0], rows[2] }, result);
        Assert.AreEqual(1, DataGridView.GetSortPriority(status));
        Assert.AreEqual(2, DataGridView.GetSortPriority(quantity));
    }

    [STATestMethod]
    public void CsvUsesVisibleColumnsAndEscapesValues()
    {
        var (grid, _, _, quantity, status) = CreateGrid();
        quantity.Visibility = System.Windows.Visibility.Collapsed;
        using var writer = new StringWriter();
        grid.ExportCsv(writer, new DataGridViewCsvOptions
        {
            ValueSelector = (item, column) => column == status ? "texto; com \"aspas\"" : null
        });
        StringAssert.Contains(writer.ToString(), "\"texto; com \"\"aspas\"\"\"");
        Assert.DoesNotContain("Quantidade", writer.ToString());
    }

    [STATestMethod]
    public void GroupingCanBeEnabledAndRemovedWithoutChangingTheSource()
    {
        var (grid, rows, _, _, _) = CreateGrid();
        grid.GroupMemberPath = nameof(Row.Status);
        grid.IsGroupingEnabled = true;
        var view = CollectionViewSource.GetDefaultView(rows);
        Assert.HasCount(1, view.GroupDescriptions);
        grid.IsGroupingEnabled = false;
        Assert.IsEmpty(view.GroupDescriptions);
        Assert.HasCount(3, rows);
    }

    [STATestMethod]
    public void CompleteUserFlowRestoresLayoutAndExportsCurrentView()
    {
        var (grid, rows, name, quantity, status) = CreateGrid();
        grid.IsMultiColumnSortEnabled = true;
        grid.ApplySort(status, ListSortDirection.Ascending);
        grid.ApplySort(quantity, ListSortDirection.Descending, append: true);
        grid.SetFilter(status, "Pendente", DataGridViewFilterOperator.Equals);
        quantity.Visibility = Visibility.Collapsed;
        status.DisplayIndex = 0; name.DisplayIndex = 1; quantity.DisplayIndex = 2;
        using var state = new MemoryStream();
        grid.SaveState(state);
        using var csv = new StringWriter();
        grid.ExportCsv(csv);
        var exported = csv.ToString();
        StringAssert.StartsWith(exported, "Status;Name");
        Assert.DoesNotContain("Concluído", exported);
        Assert.DoesNotContain("Quantity", exported);

        grid.ClearFilters(); grid.ClearSorting(); quantity.Visibility = Visibility.Visible;
        state.Position = 0; grid.LoadState(state);
        Assert.AreEqual(Visibility.Collapsed, quantity.Visibility);
        Assert.HasCount(2, CollectionViewSource.GetDefaultView(rows).SortDescriptions);
    }

    [STATestMethod]
    [DataRow("pt-BR", "12,5")]
    [DataRow("en-US", "12.5")]
    public void CsvUsesTheConfiguredCulture(string cultureName, string expectedNumber)
    {
        var (grid, _, name, quantity, status) = CreateGrid();
        name.Visibility = Visibility.Collapsed;
        status.Visibility = Visibility.Collapsed;
        using var writer = new StringWriter();
        grid.ExportCsv(writer, new DataGridViewCsvOptions
        {
            Culture = CultureInfo.GetCultureInfo(cultureName),
            ValueSelector = (_, column) => column == quantity ? 12.5m : null
        });
        StringAssert.Contains(writer.ToString(), expectedNumber);
    }

    [STATestMethod]
    public void MultipleSelectionIsExposedToBatchCommands()
    {
        var (grid, rows, _, _, _) = CreateGrid();
        DataGridViewBatchSelection? received = null;
        grid.SelectionBehavior = DataGridViewSelectionBehavior.Row;
        grid.AllowMultipleSelection = true;
        grid.BatchActionCommand = new Command(value => received = (DataGridViewBatchSelection)value!);
        grid.SelectedItems.Add(rows[0]);
        grid.SelectedItems.Add(rows[2]);
        Assert.HasCount(2, grid.GetBatchSelection().Items);
        Assert.IsTrue(grid.ExecuteBatchAction());
        Assert.IsNotNull(received);
        Assert.HasCount(2, received.Items);
    }

    [STATestMethod]
    public void EditingAndValidationRemainOptInAndCustomizable()
    {
        var (grid, _, _, _, _) = CreateGrid();
        Assert.IsFalse(grid.IsCellEditingEnabled);
        Assert.IsTrue(grid.IsReadOnly);
        Assert.IsTrue(grid.ShowValidationErrors);
        grid.IsCellEditingEnabled = true;
        grid.ValidationErrorBrush = Brushes.OrangeRed;
        Assert.IsFalse(grid.IsReadOnly);
        Assert.AreSame(Brushes.OrangeRed, grid.ValidationErrorBrush);
        grid.ShowValidationErrors = false;
        Assert.IsFalse(grid.ShowValidationErrors);
        grid.IsCellEditingEnabled = false;
        Assert.IsTrue(grid.IsReadOnly);
    }

    private static (DataGridView Grid, ObservableCollection<Row> Rows, DataGridTextColumn Name,
        DataGridTextColumn Quantity, DataGridTextColumn Status) CreateGrid()
    {
        var rows = new ObservableCollection<Row>
        {
            new("Embarque 20", 20, "Pendente"),
            new("Embarque 3", 3, "Concluído"),
            new("Embarque 11", 11, "Pendente")
        };
        var grid = new DataGridView { ItemsSource = rows };
        var name = Column(nameof(Row.Name));
        var quantity = Column(nameof(Row.Quantity));
        var status = Column(nameof(Row.Status));
        grid.Columns.Add(name); grid.Columns.Add(quantity); grid.Columns.Add(status);
        name.DisplayIndex = 0; quantity.DisplayIndex = 1; status.DisplayIndex = 2;
        return (grid, rows, name, quantity, status);
    }

    private static DataGridTextColumn Column(string path) => new()
    {
        Header = path,
        Binding = new Binding(path),
        SortMemberPath = path
    };
}
