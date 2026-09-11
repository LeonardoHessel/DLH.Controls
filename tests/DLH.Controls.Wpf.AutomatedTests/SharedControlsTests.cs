using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ControlsScrollBar = DLH.Controls.Wpf.ScrollBar;
using ControlsDataGridView = DLH.Controls.Wpf.DataGridView;
using ControlsContextMenu = DLH.Controls.Wpf.ContextMenu;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("SharedControls")]
public sealed class SharedControlsTests
{
    [STATestMethod]
    public void ContextMenuDefaultsAndValidationAreStable()
    {
        var menu = new ControlsContextMenu();
        Assert.AreEqual(new CornerRadius(8), menu.CornerRadius);
        Assert.AreEqual(new Thickness(10, 7, 10, 7), menu.ItemPadding);
        Assert.AreEqual(16d, menu.IconSize);
        Assert.AreEqual(26d, menu.IconColumnWidth);
        Assert.AreEqual(24d, menu.ArrowColumnWidth);
        Assert.IsTrue(menu.IsShadowEnabled);
        Assert.AreEqual(.5d, menu.ShadowOpacity);
        Assert.ThrowsExactly<ArgumentException>(() => menu.IconSize = -1);
        Assert.ThrowsExactly<ArgumentException>(() => menu.ItemPadding = new Thickness(-1));
        Assert.ThrowsExactly<ArgumentException>(() => menu.ShadowOpacity = 2);
    }

    [STATestMethod]
    public void ToggleMenuItemSynchronizesBooleanStateAndStateIcons()
    {
        var checkedIcon = new TextBlock { Text = "Ligado" };
        var uncheckedIcon = new TextBlock { Text = "Desligado" };
        var item = new ToggleMenuItem
        {
            Header = "Exibir detalhes",
            CheckedIcon = checkedIcon,
            UncheckedIcon = uncheckedIcon
        };

        Assert.IsTrue(item.IsCheckable);
        Assert.IsFalse(item.IsChecked);
        Assert.AreSame(uncheckedIcon, item.Icon);

        item.IsChecked = true;

        Assert.AreSame(checkedIcon, item.Icon);
        var metadata = (FrameworkPropertyMetadata)ToggleMenuItem.IsCheckedProperty.GetMetadata(typeof(ToggleMenuItem));
        Assert.IsTrue(metadata.BindsTwoWayByDefault);
    }

    [STATestMethod]
    public void ChoiceMenuItemCyclesWrapsAndSkipsDisabledOptions()
    {
        var item = new ChoiceMenuItem { Header = "Tema", SelectedIndex = 0 };
        item.Items.Add(new ChoiceMenuOption { Content = "Escuro", Value = "Dark", Icon = "☾" });
        item.Items.Add(new ChoiceMenuOption { Content = "Indisponível", Value = "Disabled", IsEnabled = false });
        item.Items.Add(new ChoiceMenuOption { Content = "Claro", Value = "Light", Icon = "☀" });

        item.CycleSelection();
        Assert.AreEqual(2, item.SelectedIndex);
        Assert.AreEqual("Light", item.SelectedValue);
        Assert.AreEqual("Claro", item.SelectedContent);
        Assert.AreEqual("Claro", MenuItemAssist.GetValue(item));
        Assert.AreEqual(string.Empty, item.InputGestureText);
        Assert.AreEqual("☀", item.Icon);

        item.CycleSelection();
        Assert.AreEqual(0, item.SelectedIndex);
        item.CycleDirection = ChoiceCycleDirection.Backward;
        item.CycleSelection();
        Assert.AreEqual(2, item.SelectedIndex);
    }

    [STATestMethod]
    public void ChoiceMenuItemSupportsObjectPathsAndTwoWaySelectionMetadata()
    {
        var first = new ChoiceTestOption("Escuro", "Dark", "☾");
        var second = new ChoiceTestOption("Claro", "Light", "☀");
        var item = new ChoiceMenuItem
        {
            DisplayMemberPath = nameof(ChoiceTestOption.Title),
            SelectedValuePath = nameof(ChoiceTestOption.Code),
            IconMemberPath = nameof(ChoiceTestOption.Symbol)
        };
        item.Items.Add(first);
        item.Items.Add(second);
        item.SelectedValue = "Light";

        Assert.AreEqual(1, item.SelectedIndex);
        Assert.AreSame(second, item.SelectedItem);
        Assert.AreEqual("Claro", item.SelectedContent);
        Assert.AreEqual("Claro", MenuItemAssist.GetValue(item));
        Assert.AreEqual("☀", item.Icon);
        Assert.IsTrue(((FrameworkPropertyMetadata)ChoiceMenuItem.SelectedIndexProperty.GetMetadata(typeof(ChoiceMenuItem))).BindsTwoWayByDefault);
        Assert.IsTrue(((FrameworkPropertyMetadata)ChoiceMenuItem.SelectedItemProperty.GetMetadata(typeof(ChoiceMenuItem))).BindsTwoWayByDefault);
        Assert.IsTrue(((FrameworkPropertyMetadata)ChoiceMenuItem.SelectedValueProperty.GetMetadata(typeof(ChoiceMenuItem))).BindsTwoWayByDefault);
    }

    [STATestMethod]
    public void ChoiceMenuItemCanStopAtEitherEnd()
    {
        var item = new ChoiceMenuItem { IsCycleWrappingEnabled = false, SelectedIndex = 1 };
        item.Items.Add("Primeiro");
        item.Items.Add("Último");
        item.CycleSelection();
        Assert.AreEqual(1, item.SelectedIndex);
        item.CycleDirection = ChoiceCycleDirection.Backward;
        item.CycleSelection();
        Assert.AreEqual(0, item.SelectedIndex);
        item.CycleSelection();
        Assert.AreEqual(0, item.SelectedIndex);
    }

    [STATestMethod]
    public void ChoiceSubmenuUsesTheSameFiveColumnTemplateAndCanOpenToTheLeft()
    {
        var choice = new TestableChoiceMenuItem
        {
            Header = "Tema",
            SelectedIndex = 0,
            FlowDirection = FlowDirection.RightToLeft
        };
        choice.Items.Add(new ChoiceMenuOption { Content = "Escuro", Value = "Dark" });
        choice.Items.Add(new ChoiceMenuOption { Content = "Claro", Value = "Light" });
        var menu = new ControlsContextMenu();
        menu.Items.Add(choice);
        var target = new Button { Content = "Abrir" };
        var window = Arrange(target, 180, 80);
        try
        {
            menu.PlacementTarget = target;
            menu.IsOpen = true;
            menu.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            choice.ApplyTemplate();
            var popup = (System.Windows.Controls.Primitives.Popup)choice.Template.FindName("PART_Popup", choice)!;
            var generated = choice.CreatePreparedContainer(choice.Items[0]);
            generated.ApplyTemplate();

            Assert.AreSame(choice.Template, generated.Template);
            Assert.HasCount(5, ((Grid)generated.Template.FindName("ItemLayout", generated)!).ColumnDefinitions);
            Assert.AreEqual(System.Windows.Controls.Primitives.PlacementMode.Left,
                popup.Placement);
            Assert.AreEqual("‹", ((TextBlock)choice.Template.FindName("SubmenuArrow", choice)!).Text);
        }
        finally { menu.IsOpen = false; window.Close(); }
    }

    [STATestMethod]
    public void ContextMenuTemplateSupportsItemsSeparatorsAndOptionalShadow()
    {
        var menu = new ControlsContextMenu { IsShadowEnabled = false };
        var item = new MenuItem { Header = "Opção", IsCheckable = true, IsChecked = true, InputGestureText = "Ctrl+O" };
        MenuItemAssist.SetValue(item, "Ativo");
        var submenu = new MenuItem { Header = "Submenu" };
        var child = new MenuItem { Header = "Filho" };
        submenu.Items.Add(child);
        var customStyle = new Style(typeof(MenuItem));
        var customItem = new MenuItem { Header = "Personalizado", Style = customStyle };
        menu.Items.Add(item);
        menu.Items.Add(submenu);
        menu.Items.Add(customItem);
        menu.Items.Add(new Separator());
        var target = new Button { Content = "Abrir" };
        var window = Arrange(target, 180, 80);
        try
        {
            menu.PlacementTarget = target;
            menu.IsOpen = true;
            menu.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            menu.ApplyTemplate();
            item.ApplyTemplate();
            submenu.ApplyTemplate();

            var surface = (Border)menu.Template.FindName("MenuSurface", menu)!;
            Assert.IsNull(surface.Effect);
            Assert.AreEqual(typeof(MenuItem), item.Style.TargetType);
            Assert.AreSame(item.Style, child.Style);
            Assert.AreSame(customStyle, customItem.Style);
            Assert.IsNotNull(item.Template.FindName("ItemSurface", item));
            Assert.AreEqual(HorizontalAlignment.Center, ((FrameworkElement)item.Template.FindName("IconHost", item)!).HorizontalAlignment);
            Assert.AreEqual(HorizontalAlignment.Center, ((FrameworkElement)item.Template.FindName("IconPresenter", item)!).HorizontalAlignment);
            Assert.AreEqual(HorizontalAlignment.Center, ((FrameworkElement)item.Template.FindName("CheckMark", item)!).HorizontalAlignment);
            Assert.AreEqual(HorizontalAlignment.Center, ((FrameworkElement)submenu.Template.FindName("SubmenuArrow", submenu)!).HorizontalAlignment);
            Assert.HasCount(5, ((Grid)item.Template.FindName("ItemLayout", item)!).ColumnDefinitions);
            Assert.AreEqual("Ativo", ((ContentPresenter)item.Template.FindName("ValuePresenter", item)!).Content);
            Assert.AreEqual("Ctrl+O", ((TextBlock)item.Template.FindName("GesturePresenter", item)!).Text);
            Assert.AreEqual(Visibility.Visible, ((FrameworkElement)item.Template.FindName("CheckMark", item)!).Visibility);
            menu.IsShadowEnabled = true;
            menu.UpdateLayout();
            Assert.IsInstanceOfType<DropShadowEffect>(surface.Effect);
        }
        finally { menu.IsOpen = false; window.Close(); }
    }

    [STATestMethod]
    public void DataGridViewCreatesTheSharedContextMenu()
    {
        var grid = new ControlsDataGridView();
        grid.Columns.Add(new DataGridTextColumn { Header = "Valor" });
        var method = typeof(ControlsDataGridView).GetMethod("CreateColumnHeaderMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var menu = method.Invoke(grid, new object?[] { null });
        Assert.IsInstanceOfType<ControlsContextMenu>(menu);
        Assert.IsTrue(((ControlsContextMenu)menu!).Items.OfType<MenuItem>().Any());
    }

    [STATestMethod]
    public void ContextMenuRefreshesEveryColorAfterItsFirstApplication()
    {
        var item = new MenuItem { Header = "Item" };
        var separator = new Separator();
        var child = new MenuItem { Header = "Filho" };
        item.Items.Add(child);
        var menu = new ControlsContextMenu();
        menu.Items.Add(item);
        menu.Items.Add(separator);
        var target = new Button { Content = "Abrir" };
        var window = Arrange(target, 180, 80);
        menu.PlacementTarget = target;
        menu.IsOpen = true;
        menu.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
        menu.ApplyTemplate();
        separator.ApplyTemplate();

        var surfaceColor = Color.FromRgb(10, 20, 30);
        var textColor = Color.FromRgb(40, 50, 60);
        var edgeColor = Color.FromRgb(70, 80, 90);
        var hoverColor = Color.FromRgb(100, 110, 120);
        var checkedColor = Color.FromRgb(130, 140, 150);
        var separatorColor = Color.FromRgb(0x18, 0xA4, 0x00);
        menu.Background = new SolidColorBrush(surfaceColor);
        menu.Foreground = new SolidColorBrush(textColor);
        menu.BorderBrush = new SolidColorBrush(edgeColor);
        menu.HoverBrush = new SolidColorBrush(hoverColor);
        menu.CheckedBrush = new SolidColorBrush(checkedColor);
        menu.SeparatorBrush = new SolidColorBrush(separatorColor);

        AssertBrush(item.Resources["ContextMenu.Surface"], surfaceColor);
        AssertBrush(item.Resources["ContextMenu.Text"], textColor);
        AssertBrush(item.Resources["ContextMenu.Edge"], edgeColor);
        AssertBrush(item.Resources["ContextMenu.Hover"], hoverColor);
        AssertBrush(item.Resources["ContextMenu.Checked"], checkedColor);
        AssertBrush(child.Resources["ContextMenu.Hover"], hoverColor);
        AssertBrush(separator.Resources["ContextMenu.Separator"], separatorColor);
        Assert.AreEqual(typeof(Separator), separator.Style.TargetType);
        Assert.AreSame(menu.Background, ((Border)menu.Template.FindName("MenuSurface", menu)!).Background);
        var separatorLine = (Border)separator.Template.FindName("SeparatorLine", separator)!;
        Assert.AreEqual(separatorColor, Assert.IsInstanceOfType<SolidColorBrush>(separatorLine.Background).Color);
        var renderedLine = VisualDescendants(separator).OfType<Border>().Single(border => border.Name == "SeparatorLine");
        Assert.AreEqual(separatorColor, Assert.IsInstanceOfType<SolidColorBrush>(renderedLine.Background).Color);
        menu.IsOpen = false;
        window.Close();
    }

    [STATestMethod]
    public void ScrollBarDefaultsAndValidationAreStable()
    {
        var bar = new ControlsScrollBar();
        Assert.AreEqual(10d, bar.Thickness);
        Assert.AreEqual(new CornerRadius(5), bar.CornerRadius);
        Assert.IsFalse(bar.ShowButtons);
        Assert.IsFalse(bar.IsShadowEnabled);
        Assert.ThrowsExactly<ArgumentException>(() => bar.Thickness = 0);
        Assert.ThrowsExactly<ArgumentException>(() => bar.CornerRadius = new CornerRadius(2, 3, 2, 3));
        Assert.ThrowsExactly<ArgumentException>(() => bar.ShadowOpacity = 2);
    }

    [STATestMethod]
    public void ScrollBarUsesOrientationThicknessAndLimitsRenderedRadius()
    {
        var bar = new ControlsScrollBar { Thickness = 8, CornerRadius = new CornerRadius(20), Height = 160 };
        var window = Arrange(bar, 8, 160);
        try
        {
            var surface = (Border)bar.Template.FindName("TrackSurface", bar)!;
            Assert.AreEqual(8d, bar.ActualWidth);
            Assert.AreEqual(new CornerRadius(4), surface.CornerRadius);
            bar.Orientation = Orientation.Horizontal;
            bar.Thickness = 12;
            bar.Width = 180;
            bar.Measure(new Size(180, 12)); bar.Arrange(new Rect(0, 0, 180, 12)); bar.UpdateLayout();
            surface = (Border)bar.Template.FindName("TrackSurface", bar)!;
            Assert.AreEqual(12d, bar.ActualHeight);
            Assert.AreEqual(new CornerRadius(6), surface.CornerRadius);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ScrollBarButtonsAndShadowAreOptional()
    {
        var bar = new ControlsScrollBar { Height = 160, ShowButtons = true, IsShadowEnabled = true };
        var window = Arrange(bar, 10, 160);
        try
        {
            Assert.AreEqual(Visibility.Visible, ((FrameworkElement)bar.Template.FindName("DecreaseButton", bar)!).Visibility);
            Assert.IsInstanceOfType<DropShadowEffect>(((Border)bar.Template.FindName("TrackSurface", bar)!).Effect);
            bar.ShowButtons = false; bar.IsShadowEnabled = false; bar.UpdateLayout();
            Assert.AreEqual(Visibility.Collapsed, ((FrameworkElement)bar.Template.FindName("DecreaseButton", bar)!).Visibility);
            Assert.IsNull(((Border)bar.Template.FindName("TrackSurface", bar)!).Effect);
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void DataGridViewUsesTheSharedScrollBars()
    {
        var grid = new ControlsDataGridView { Width = 300, Height = 180, ScrollBarThickness = 9 };
        grid.Columns.Add(new DataGridTextColumn { Header = "Valor", Width = 500 });
        grid.ItemsSource = Enumerable.Range(1, 30).Select(number => new { Valor = number });
        var window = Arrange(grid, 300, 180);
        try
        {
            var viewer = (System.Windows.Controls.ScrollViewer)grid.Template.FindName("DG_ScrollViewer", grid)!;
            viewer.ApplyTemplate();
            var vertical = (ControlsScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer)!;
            var horizontal = (ControlsScrollBar)viewer.Template.FindName("PART_HorizontalScrollBar", viewer)!;
            Assert.AreEqual(9d, vertical.Thickness);
            Assert.AreEqual(9d, horizontal.Thickness);
            Assert.AreSame(grid.ScrollBarThumbBrush, vertical.ThumbBrush);
        }
        finally { window.Close(); }
    }

    private static Window Arrange(FrameworkElement element, double width, double height)
    {
        var window = new Window { Content = element, Width = width, Height = height };
        element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height));
        element.ApplyTemplate(); element.UpdateLayout();
        return window;
    }

    private static void AssertBrush(object value, Color expected) =>
        Assert.AreEqual(expected, Assert.IsInstanceOfType<SolidColorBrush>(value).Color);

    private static IEnumerable<DependencyObject> VisualDescendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in VisualDescendants(child)) yield return descendant;
        }
    }

    private sealed record ChoiceTestOption(string Title, string Code, string Symbol);

    private sealed class TestableChoiceMenuItem : ChoiceMenuItem
    {
        public MenuItem CreatePreparedContainer(object item)
        {
            var container = (MenuItem)GetContainerForItemOverride();
            PrepareContainerForItemOverride(container, item);
            return container;
        }
    }
}

