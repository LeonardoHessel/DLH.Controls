using System.Collections.ObjectModel;
using System.Windows.Controls;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Pinning")]
public sealed class DataGridViewPinningTests
{
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

    private sealed record Row(string Name);
}
