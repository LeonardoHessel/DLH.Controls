using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CustomTabControl.Controls;

public partial class CustomTabControl
{
    public static readonly RoutedUICommand CloseTab = new("Fechar aba", nameof(CloseTab), typeof(CustomTabControl));
    public static readonly DependencyProperty CanCloseTabsProperty = DependencyProperty.Register(
        nameof(CanCloseTabs), typeof(bool), typeof(CustomTabControl), new PropertyMetadata(true, (_, _) => CommandManager.InvalidateRequerySuggested()));
    public bool CanCloseTabs { get => (bool)GetValue(CanCloseTabsProperty); set => SetValue(CanCloseTabsProperty, value); }

    public static readonly DependencyProperty ShowCloseButtonsProperty = DependencyProperty.Register(
        nameof(ShowCloseButtons), typeof(bool), typeof(CustomTabControl), new PropertyMetadata(false));
    public bool ShowCloseButtons { get => (bool)GetValue(ShowCloseButtonsProperty); set => SetValue(ShowCloseButtonsProperty, value); }

    public static readonly DependencyProperty CloseTabCommandProperty = DependencyProperty.Register(
        nameof(CloseTabCommand), typeof(ICommand), typeof(CustomTabControl), new PropertyMetadata(null, (_, _) => CommandManager.InvalidateRequerySuggested()));
    public ICommand? CloseTabCommand { get => (ICommand?)GetValue(CloseTabCommandProperty); set => SetValue(CloseTabCommandProperty, value); }

    public static readonly DependencyProperty CanCloseTabProperty = DependencyProperty.RegisterAttached(
        "CanCloseTab", typeof(bool), typeof(CustomTabControl), new PropertyMetadata(true, (_, _) => CommandManager.InvalidateRequerySuggested()));
    public static bool GetCanCloseTab(DependencyObject item) => (bool)item.GetValue(CanCloseTabProperty);
    public static void SetCanCloseTab(DependencyObject item, bool value) => item.SetValue(CanCloseTabProperty, value);

    private object? ResolveCloseItem(object? parameter)
    {
        if (parameter is TabItem container && ItemsControlFromItemContainer(container) == this)
            return ItemContainerGenerator.ItemFromContainer(container);
        return parameter;
    }

    private bool CanRequestClose(object? item)
    {
        if (!CanCloseTabs || item is null || !Items.Contains(item)) return false;
        if (ItemContainerGenerator.ContainerFromItem(item) is TabItem container && (!container.IsEnabled || !GetCanCloseTab(container))) return false;
        if (item is TabItem own && (!own.IsEnabled || !GetCanCloseTab(own))) return false;
        return CloseTabCommand is { } command ? command.CanExecute(item) : EditableList() is not null;
    }

    /// <summary>Requests synchronous closing; commands own removal when supplied. The cancellable event runs first.</summary>
    public bool RequestCloseTab(object item)
    {
        item = ResolveCloseItem(item)!;
        if (!CanRequestClose(item)) return false;
        CancelTabDrag(); ResetDragPreview();
        var closing = new TabClosingEventArgs(item);
        TabClosing?.Invoke(this, closing);
        if (closing.Cancel || !CanRequestClose(item)) return false;
        var selected = SelectedItem;
        var index = Items.IndexOf(item);
        object? next = null;
        if (Equals(selected, item))
            next = Items.Cast<object>().Skip(index + 1).Concat(Items.Cast<object>().Take(index).Reverse())
                .FirstOrDefault(candidate => ItemContainerGenerator.ContainerFromItem(candidate) is not TabItem { IsEnabled: false });
        if (CloseTabCommand is { } command) command.Execute(item);
        else
        {
            var list = EditableList()!;
            list.Remove(item);
            if (ItemsSource is not null && list is not System.Collections.Specialized.INotifyCollectionChanged) Items.Refresh();
        }
        if (Items.Contains(item)) return false;
        if (!Equals(selected, item) && Items.Contains(selected)) SetCurrentValue(SelectedItemProperty, selected);
        else if (Equals(selected, item)) SetCurrentValue(SelectedItemProperty, next is not null && Items.Contains(next) ? next : null);
        TabClosed?.Invoke(this, new TabClosedEventArgs(item));
        CommandManager.InvalidateRequerySuggested();
        return true;
    }

    private bool TryReorderFromKeyboard(Key key, ModifierKeys modifiers, DependencyObject? source)
    {
        if (modifiers != (ModifierKeys.Control | ModifierKeys.Shift) || !CanReorderTabs) return false;
        var delta = IsVerticalTabStrip ? key == Key.Up ? -1 : key == Key.Down ? 1 : 0
            : key == Key.Left ? -1 : key == Key.Right ? 1 : 0;
        if (delta == 0 || source is null || ContainerFromElement(this, source) is not TabItem tab) return false;
        // Do not steal Ctrl+Shift+arrows from an editor or another interactive element.
        for (var node = source; node is not null && node != tab; node = node is FrameworkContentElement text ? text.Parent : VisualTreeHelper.GetParent(node))
            if (node is TextBoxBase or PasswordBox or ButtonBase or Selector) return false;
        if (source != tab && (source is not Visual visual || tab.Template.FindName("Surface", tab) is not FrameworkElement header || !header.IsAncestorOf(visual))) return false;
        var oldIndex = ItemContainerGenerator.IndexFromContainer(tab);
        var newIndex = oldIndex + delta;
        if (newIndex < 0 || newIndex >= Items.Count) return true;
        CancelTabDrag(); ResetDragPreview();
        if (!MoveTabCore(oldIndex, newIndex, TabReorderReason.Keyboard)) return false;
        UpdateLayout();
        (ItemContainerGenerator.ContainerFromIndex(newIndex) as TabItem)?.Focus();
        return true;
    }
}
