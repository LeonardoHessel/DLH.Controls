using System.Collections.ObjectModel;
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

    private sealed record Row(string Name);
}
