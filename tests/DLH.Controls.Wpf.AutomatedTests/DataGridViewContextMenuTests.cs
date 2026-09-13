using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DLH.Controls.Wpf;
using GridMenu = DLH.Controls.Wpf.ContextMenu;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
public sealed class DataGridViewContextMenuTests
{
    private sealed record Row(string Name, int Value);

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            ItemsSource = new[] { new Row("a", 1), new Row("aa", 2) },
            CanPinRows = true, CanPinColumns = true, MaxPinnedRows = 1, MaxPinnedColumns = 1
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Nome", SortMemberPath = "Name", Binding = new Binding("Name") });
        grid.Columns.Add(new DataGridTextColumn { Header = "Valor", SortMemberPath = "Value", Binding = new Binding("Value") });
        return grid;
    }

    private static GridMenu Header(DataGridView grid, DataGridColumn? column = null) =>
        (GridMenu)typeof(DataGridView).GetMethod("CreateColumnHeaderMenu", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(grid, [column])!;
    private static GridMenu RowMenu(DataGridView grid, object row, DataGridColumn? column = null) =>
        (GridMenu)typeof(DataGridView).GetMethod("CreateRowPinMenu", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(grid, [row, column])!;
    private static string[] Labels(ItemsControl menu) => menu.Items.Cast<object>()
        .Select(item => item is MenuItem action ? action.Header.ToString()! : "|").ToArray();
    private static MenuItem Find(ItemsControl menu, string label) => menu.Items.OfType<MenuItem>()
        .Single(item => Equals(item.Header, label));
    private static void Click(MenuItem item) => item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

    [STATestMethod]
    public void HeaderGroupsColumnActionsOptionsAndGlobalCleanup()
    {
        var grid = CreateGrid();
        grid.MaxPinnedColumns = 2;
        grid.DefaultSortMemberPath = "Name";
        Assert.IsTrue(grid.SetFilter(grid.Columns[0], "a"));
        Assert.IsTrue(grid.ApplySort(grid.Columns[0], ListSortDirection.Ascending));
        grid.PinColumn(grid.Columns[0]);
        grid.PinColumn(grid.Columns[1]);
        var menu = Header(grid, grid.Columns[0]);
        CollectionAssert.AreEqual(new[] { "Desafixar coluna", "Limpar filtro desta coluna", "|", "Ordenação",
            "Colunas visíveis", "|", "Limpar todos os filtros", "Desafixar todas as colunas" }, Labels(menu));
        CollectionAssert.AreEqual(new[] { "Limpar ordenação", "Restaurar ordenação padrão" }, Labels(Find(menu, "Ordenação")));
        Click(Find(Find(menu, "Ordenação"), "Limpar ordenação"));
        Assert.IsNull(grid.Columns[0].SortDirection);
        Click(Find(Find(menu, "Ordenação"), "Restaurar ordenação padrão"));
        Assert.AreEqual(grid.DefaultSortDirection, grid.Columns[0].SortDirection);
        Click(Find(menu, "Limpar filtro desta coluna"));
        Assert.IsEmpty(grid.Filters);
        Click(Find(menu, "Desafixar todas as colunas"));
        Assert.IsEmpty(grid.PinnedColumns);
    }

    [STATestMethod]
    public void RowMenuNamesTheColumnAndOnlyOffersBulkActionsForMultiplePins()
    {
        var grid = CreateGrid();
        var first = grid.Items[0];
        var second = grid.Items[1];
        var menu = RowMenu(grid, first, grid.Columns[0]);
        CollectionAssert.AreEqual(new[] { "Fixar linha", "Fixar coluna “Nome”" }, Labels(menu));
        Click(Find(menu, "Fixar linha"));
        Click(Find(menu, "Fixar coluna “Nome”"));
        CollectionAssert.AreEqual(new[] { "Desafixar linha", "Desafixar coluna “Nome”" }, Labels(RowMenu(grid, first, grid.Columns[0])));
        grid.MaxPinnedRows = grid.MaxPinnedColumns = 2;
        grid.PinRow(second);
        grid.PinColumn(grid.Columns[1]);
        menu = RowMenu(grid, first, grid.Columns[0]);
        CollectionAssert.AreEqual(new[] { "Desafixar linha", "Desafixar coluna “Nome”", "|",
            "Desafixar todas as linhas", "Desafixar todas as colunas" }, Labels(menu));
        Click(Find(menu, "Desafixar todas as colunas"));
        Assert.IsEmpty(grid.PinnedColumns);
        Assert.HasCount(2, grid.PinnedRows);
        Click(Find(menu, "Desafixar todas as linhas"));
        Assert.IsEmpty(grid.PinnedRows);
    }

    [STATestMethod]
    public void PinLimitsHaveTooltipsAndDoNotDisableUnpinning()
    {
        var grid = CreateGrid();
        grid.PinRow(grid.Items[0]);
        grid.PinColumn(grid.Columns[0]);
        var menu = RowMenu(grid, grid.Items[1], grid.Columns[1]);
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            Assert.IsFalse(item.IsEnabled);
            Assert.IsTrue(ToolTipService.GetShowOnDisabled(item));
            StringAssert.Contains((string)item.ToolTip, "(1)");
        }
        Assert.IsFalse(Find(Header(grid, grid.Columns[1]), "Fixar coluna").IsEnabled);
        Assert.IsTrue(Find(Header(grid, grid.Columns[0]), "Desafixar coluna").IsEnabled);
        Assert.IsTrue(Find(RowMenu(grid, grid.Items[0]), "Desafixar linha").IsEnabled);
    }

    [STATestMethod]
    public void EmptyGroupsDisappearAndVisibilityKeepsDisplayOrderAndLastColumnGuard()
    {
        var grid = CreateGrid();
        grid.Columns[1].DisplayIndex = 0;
        var menu = Header(grid);
        CollectionAssert.AreEqual(new[] { "Colunas visíveis" }, Labels(menu));
        var visibility = Find(menu, "Colunas visíveis");
        CollectionAssert.AreEqual(new[] { "Valor", "Nome" }, Labels(visibility));
        var value = Find(visibility, "Valor");
        value.IsChecked = false;
        Click(value);
        visibility = Find(Header(grid), "Colunas visíveis");
        Assert.IsFalse(Find(visibility, "Nome").IsEnabled);
        Assert.IsTrue(Find(visibility, "Valor").IsEnabled);
        grid.DefaultSortMemberPath = "Name";
        grid.ApplySort(grid.Columns[0], ListSortDirection.Ascending);
        grid.SetFilter(grid.Columns[0], "a");
        grid.CanPinColumns = grid.CanPinRows = grid.CanUserToggleColumnVisibility = false;
        grid.CanUserSortColumns = false;
        grid.ShowFilterMenuItems = false;
        Assert.IsEmpty(Header(grid, grid.Columns[0]).Items);
        Assert.IsEmpty(RowMenu(grid, grid.Items[0], grid.Columns[0]).Items);
    }
}
