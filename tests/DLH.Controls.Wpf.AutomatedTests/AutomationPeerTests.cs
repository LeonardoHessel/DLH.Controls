using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ControlsContextMenu = DLH.Controls.Wpf.ContextMenu;
using ControlsDataGridView = DLH.Controls.Wpf.DataGridView;
using ControlsScrollBar = DLH.Controls.Wpf.ScrollBar;

namespace DLH.Controls.Wpf.AutomatedTests;

/// <summary>
/// Confirma o que os controles expõem às tecnologias assistivas pela árvore de automação.
/// Não substitui a validação com leitor de tela e alto contraste descrita em docs/guides/accessibility.md.
/// </summary>
[TestClass]
[TestCategory("Accessibility")]
[TestCategory("Unit")]
public sealed class AutomationPeerTests
{
    private sealed record Row(string Name, int Quantity);

    private static AutomationPeer PeerOf(UIElement element) =>
        UIElementAutomationPeer.CreatePeerForElement(element)
        ?? throw new AssertFailedException($"{element.GetType().Name} não expõe um peer de automação.");

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var nested in FindVisualChildren<T>(child)) yield return nested;
        }
    }

    private static void WithWindow(FrameworkElement content, Action<Window> action)
    {
        var window = new Window
        {
            Content = content,
            Width = 360,
            Height = 240,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false
        };
        window.Show();
        try
        {
            content.UpdateLayout();
            action(window);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ScrollBarIsExposedAsScrollBarWithRangeValueAndOrientation()
    {
        var bar = new ControlsScrollBar
        {
            Orientation = Orientation.Vertical,
            Minimum = 0,
            Maximum = 100,
            Value = 35,
            Height = 160
        };

        WithWindow(bar, _ =>
        {
            var peer = PeerOf(bar);
            Assert.AreEqual(AutomationControlType.ScrollBar, peer.GetAutomationControlType());
            Assert.AreEqual(AutomationOrientation.Vertical, peer.GetOrientation());

            var range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;
            Assert.AreEqual(0d, range.Minimum);
            Assert.AreEqual(100d, range.Maximum);
            Assert.AreEqual(35d, range.Value);

            bar.Orientation = Orientation.Horizontal;
            Assert.AreEqual(AutomationOrientation.Horizontal, peer.GetOrientation());
        });
    }

    [STATestMethod]
    public void ScrollBarKeepsConsumerNameAndDisabledState()
    {
        var bar = new ControlsScrollBar { Maximum = 10, Height = 120 };
        AutomationProperties.SetName(bar, "Rolagem da lista de pedidos");

        WithWindow(bar, _ =>
        {
            var peer = PeerOf(bar);
            Assert.AreEqual("Rolagem da lista de pedidos", peer.GetName());
            Assert.IsTrue(peer.IsEnabled());

            bar.IsEnabled = false;
            Assert.IsFalse(peer.IsEnabled());
        });
    }

    [STATestMethod]
    public void DataGridViewIsExposedAsGridWithTableHeaders()
    {
        var rows = new ObservableCollection<Row> { new("Primeira", 1), new("Segunda", 2) };
        var grid = new ControlsDataGridView { ItemsSource = rows, Width = 320, Height = 180 };
        grid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new Binding(nameof(Row.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Quantidade", Binding = new Binding(nameof(Row.Quantity)) });

        WithWindow(grid, _ =>
        {
            var peer = PeerOf(grid);
            Assert.AreEqual(AutomationControlType.DataGrid, peer.GetAutomationControlType());

            var generated = peer.GetChildren();
            Assert.IsNotNull(generated);
            var table = (ITableProvider)peer.GetPattern(PatternInterface.Table)!;
            Assert.AreEqual(2, table.RowCount);
            Assert.AreEqual(2, table.ColumnCount);

            Assert.AreEqual(2, table.GetColumnHeaders()?.Length);

            var headerNames = FindVisualChildren<DataGridColumnHeader>(grid)
                .Where(header => header.Column is not null)
                .Select(header => PeerOf(header).GetName())
                .ToArray();
            CollectionAssert.AreEqual(new[] { "Nome", "Quantidade" }, headerNames);
        });
    }

    [STATestMethod]
    public void DataGridViewExposesRowsAsDataItemsWithSelectionState()
    {
        var rows = new ObservableCollection<Row> { new("Primeira", 1), new("Segunda", 2) };
        var grid = new ControlsDataGridView
        {
            ItemsSource = rows,
            Width = 320,
            Height = 180,
            SelectionBehavior = DataGridViewSelectionBehavior.Row
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new Binding(nameof(Row.Name)) });

        WithWindow(grid, _ =>
        {
            var peer = PeerOf(grid);
            var items = peer.GetChildren()!
                .Where(child => child.GetAutomationControlType() == AutomationControlType.DataItem)
                .ToArray();
            Assert.AreEqual(2, items.Length);

            grid.SelectedItem = rows[1];

            Assert.IsTrue(((ISelectionItemProvider)items[1].GetPattern(PatternInterface.SelectionItem)!).IsSelected);
            Assert.IsFalse(((ISelectionItemProvider)items[0].GetPattern(PatternInterface.SelectionItem)!).IsSelected);
        });
    }

    [STATestMethod]
    public void ToggleMenuItemExposesToggleStateAndConsumerName()
    {
        var item = new ToggleMenuItem { Header = "Exibir detalhes", IsChecked = false };
        AutomationProperties.SetName(item, "Mostrar painel de detalhes");

        var peer = PeerOf(item);
        Assert.AreEqual(AutomationControlType.MenuItem, peer.GetAutomationControlType());
        Assert.AreEqual("Mostrar painel de detalhes", peer.GetName());

        var toggle = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle)!;
        Assert.AreEqual(ToggleState.Off, toggle.ToggleState);

        item.IsChecked = true;
        Assert.AreEqual(ToggleState.On, toggle.ToggleState);

        item.IsEnabled = false;
        Assert.IsFalse(peer.IsEnabled());
    }

    [STATestMethod]
    public void ChoiceMenuItemIsNamedByHeaderAndExposesItsOptions()
    {
        var item = new ChoiceMenuItem { Header = "Tema", SelectedIndex = 0 };
        item.Items.Add(new ChoiceMenuOption { Content = "Escuro", Value = "Dark" });
        item.Items.Add(new ChoiceMenuOption { Content = "Claro", Value = "Light" });

        var peer = PeerOf(item);
        Assert.AreEqual(AutomationControlType.MenuItem, peer.GetAutomationControlType());
        Assert.AreEqual("Tema", peer.GetName());
        Assert.IsNotNull(peer.GetPattern(PatternInterface.ExpandCollapse),
            "A seta que abre a lista completa precisa estar disponível como ação de expandir.");

        item.CycleSelection();
        Assert.AreEqual("Claro", item.SelectedContent);
        Assert.AreEqual("Tema", peer.GetName(), "O nome acessível não deve depender do valor selecionado.");
    }

    [STATestMethod]
    public void ContextMenuItemsKeepNativeMenuStructure()
    {
        var menu = new ControlsContextMenu();
        var submenu = new MenuItem { Header = "Exportar" };
        submenu.Items.Add(new MenuItem { Header = "CSV" });
        menu.Items.Add(new MenuItem { Header = "Copiar" });
        menu.Items.Add(new Separator());
        menu.Items.Add(submenu);

        var peer = PeerOf(menu);
        Assert.AreEqual(AutomationControlType.Menu, peer.GetAutomationControlType());

        var itemPeers = menu.Items.OfType<MenuItem>().Select(PeerOf).ToArray();
        CollectionAssert.AreEqual(new[] { "Copiar", "Exportar" }, itemPeers.Select(item => item.GetName()).ToArray());
        Assert.IsNull(itemPeers[0].GetPattern(PatternInterface.ExpandCollapse),
            "Um item sem filhos nÃ£o deve ser anunciado como expansÃ­vel.");
        Assert.IsNotNull(itemPeers[1].GetPattern(PatternInterface.ExpandCollapse),
            "Um item com submenu precisa expor a aÃ§Ã£o de expandir.");
    }
}

