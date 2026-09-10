using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Unit")]
public sealed class DataGridViewApiTests
{
    private sealed record Row(string Name, int Quantity, string Status);

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
