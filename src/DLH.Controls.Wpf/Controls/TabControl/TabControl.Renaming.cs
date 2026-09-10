using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DLH.Controls.Wpf;

[Flags]
public enum TabRenameActivation
{
    None = 0,
    F2 = 1,
    DoubleClick = 2,
    F2AndDoubleClick = F2 | DoubleClick
}

public sealed class TabRenamingEventArgs(object item, string oldHeader) : EventArgs
{
    public object Item { get; } = item;
    public string OldHeader { get; } = oldHeader;
    public bool Cancel { get; set; }
}

public sealed class TabRenameRequest(object item, string oldHeader, string newHeader)
{
    public object Item { get; } = item;
    public string OldHeader { get; } = oldHeader;
    public string NewHeader { get; } = newHeader;
}

public sealed class TabRenameRequestedEventArgs(object item, string oldHeader, string newHeader) : EventArgs
{
    public object Item { get; } = item;
    public string OldHeader { get; } = oldHeader;
    public string NewHeader { get; } = newHeader;
    public bool Cancel { get; set; }
}

public sealed class TabRenamedEventArgs(object item, string oldHeader, string newHeader) : EventArgs
{
    public object Item { get; } = item;
    public string OldHeader { get; } = oldHeader;
    public string NewHeader { get; } = newHeader;
}

public partial class TabControl
{
    private TabItem? editingTab;
    private object? editingItem;
    private TextBox? headerEditor;
    private FrameworkElement? headerPresenter;
    private string originalHeader = string.Empty;
    private bool endingRename;

    public event EventHandler<TabRenamingEventArgs>? TabRenaming;
    public event EventHandler<TabRenameRequestedEventArgs>? TabRenameRequested;
    public event EventHandler<TabRenamedEventArgs>? TabRenamed;

    public static readonly DependencyProperty CanRenameTabsProperty = DependencyProperty.Register(
        nameof(CanRenameTabs), typeof(bool), typeof(TabControl),
        new PropertyMetadata(false, (owner, args) => { if (!(bool)args.NewValue) ((TabControl)owner).CancelTabRename(); }));
    public bool CanRenameTabs { get => (bool)GetValue(CanRenameTabsProperty); set => SetValue(CanRenameTabsProperty, value); }

    public static readonly DependencyProperty TabHeaderPathProperty = DependencyProperty.Register(
        nameof(TabHeaderPath), typeof(string), typeof(TabControl),
        new PropertyMetadata(null, (owner, _) => ((TabControl)owner).CancelTabRename()),
        value => value is null || value is string path && !string.IsNullOrWhiteSpace(path));
    public string? TabHeaderPath { get => (string?)GetValue(TabHeaderPathProperty); set => SetValue(TabHeaderPathProperty, value); }

    public static readonly DependencyProperty RenameTabCommandProperty = DependencyProperty.Register(
        nameof(RenameTabCommand), typeof(ICommand), typeof(TabControl), new PropertyMetadata(null));
    public ICommand? RenameTabCommand { get => (ICommand?)GetValue(RenameTabCommandProperty); set => SetValue(RenameTabCommandProperty, value); }

    public static readonly DependencyProperty RenameActivationProperty = DependencyProperty.Register(
        nameof(RenameActivation), typeof(TabRenameActivation), typeof(TabControl),
        new PropertyMetadata(TabRenameActivation.F2AndDoubleClick),
        value => value is TabRenameActivation activation && (activation & ~TabRenameActivation.F2AndDoubleClick) == 0);
    public TabRenameActivation RenameActivation { get => (TabRenameActivation)GetValue(RenameActivationProperty); set => SetValue(RenameActivationProperty, value); }

    public bool IsRenamingTab => editingTab is not null;

    private object ItemFromContainer(TabItem container)
    {
        var item = ItemContainerGenerator.ItemFromContainer(container);
        return ReferenceEquals(item, DependencyProperty.UnsetValue) ? container : item;
    }

    private bool TryGetHeader(object item, out string header, out object? owner, out PropertyDescriptor? property)
    {
        owner = null; property = null;
        if (item is TabItem tab && string.IsNullOrWhiteSpace(TabHeaderPath))
        {
            header = tab.Header?.ToString() ?? string.Empty;
            owner = tab;
            return tab.Header is null or string;
        }
        object? current = item;
        var members = TabHeaderPath?.Split('.') ?? [];
        if (members.Length == 0)
        {
            header = item.ToString() ?? string.Empty;
            return RenameTabCommand is not null;
        }
        for (var index = 0; index < members.Length; index++)
        {
            if (current is null) { header = string.Empty; return false; }
            var descriptor = TypeDescriptor.GetProperties(current)[members[index]];
            if (descriptor is null) { header = string.Empty; return false; }
            if (index == members.Length - 1)
            {
                var value = descriptor.GetValue(current);
                if (value is not null and not string) { header = string.Empty; return false; }
                header = (string?)value ?? string.Empty;
                owner = current; property = descriptor;
                return RenameTabCommand is not null || !descriptor.IsReadOnly;
            }
            current = descriptor.GetValue(current);
        }
        header = string.Empty;
        return false;
    }

    public bool BeginRenameTab(object item)
    {
        if (!CanRenameTabs || dragging) return false;
        var container = item as TabItem;
        if (container is null || ItemsControl.ItemsControlFromItemContainer(container) != this)
            container = ItemContainerGenerator.ContainerFromItem(item) as TabItem;
        if (container is null || !container.IsEnabled) return false;
        var data = ItemFromContainer(container);
        if (!TryGetHeader(data, out var header, out _, out _)) return false;
        var starting = new TabRenamingEventArgs(data, header);
        TabRenaming?.Invoke(this, starting);
        if (starting.Cancel) return false;

        CancelTabDrag();
        CancelTabRename();
        container.ApplyTemplate();
        if (container.Template.FindName("HeaderEditor", container) is not TextBox editor ||
            container.Template.FindName("HeaderPresenter", container) is not FrameworkElement presenter) return false;
        editingTab = container; editingItem = data; headerEditor = editor; headerPresenter = presenter; originalHeader = header;
        SetCurrentValue(SelectedItemProperty, data);
        presenter.Visibility = Visibility.Hidden;
        editor.Text = header;
        editor.Visibility = Visibility.Visible;
        editor.PreviewKeyDown += HeaderEditorPreviewKeyDown;
        editor.LostKeyboardFocus += HeaderEditorLostKeyboardFocus;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (!ReferenceEquals(headerEditor, editor)) return;
            editor.Focus(); editor.SelectAll();
        }));
        return true;
    }

    public bool CommitTabRename()
    {
        if (editingTab is null || editingItem is null || headerEditor is null) return false;
        var item = editingItem; var oldHeader = originalHeader; var newHeader = headerEditor.Text;
        if (newHeader == oldHeader) { EndTabRename(); return true; }
        var requested = new TabRenameRequestedEventArgs(item, oldHeader, newHeader);
        TabRenameRequested?.Invoke(this, requested);
        if (requested.Cancel) return false;
        var request = new TabRenameRequest(item, oldHeader, newHeader);
        if (RenameTabCommand is { } command)
        {
            if (!command.CanExecute(request)) return false;
            command.Execute(request);
        }
        else if (item is TabItem tab && string.IsNullOrWhiteSpace(TabHeaderPath)) tab.SetCurrentValue(HeaderedContentControl.HeaderProperty, newHeader);
        else
        {
            if (!TryGetHeader(item, out _, out var owner, out var property) || owner is null || property is null || property.IsReadOnly) return false;
            property.SetValue(owner, newHeader);
        }
        EndTabRename();
        TabRenamed?.Invoke(this, new TabRenamedEventArgs(item, oldHeader, newHeader));
        return true;
    }

    public void CancelTabRename() => EndTabRename();

    private void EndTabRename()
    {
        if (endingRename) return;
        endingRename = true;
        try
        {
            if (headerEditor is not null)
            {
                headerEditor.PreviewKeyDown -= HeaderEditorPreviewKeyDown;
                headerEditor.LostKeyboardFocus -= HeaderEditorLostKeyboardFocus;
                headerEditor.Visibility = Visibility.Collapsed;
            }
            if (headerPresenter is not null) headerPresenter.Visibility = Visibility.Visible;
            editingTab = null; editingItem = null; headerEditor = null; headerPresenter = null; originalHeader = string.Empty;
        }
        finally { endingRename = false; }
    }

    private void HeaderEditorPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { if (CommitTabRename()) e.Handled = true; }
        else if (e.Key == Key.Escape) { CancelTabRename(); e.Handled = true; }
    }

    private void HeaderEditorLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!endingRename && ReferenceEquals(sender, headerEditor) && !CommitTabRename()) CancelTabRename();
    }

    private bool TryBeginRenameFromDoubleClick(MouseButtonEventArgs e)
    {
        if (!CanRenameTabs || !RenameActivation.HasFlag(TabRenameActivation.DoubleClick) || e.ClickCount != 2 ||
            e.OriginalSource is not DependencyObject source) return false;
        for (var node = source; node is not null && node is not TabItem; node = node is FrameworkContentElement content ? content.Parent : System.Windows.Media.VisualTreeHelper.GetParent(node))
            if (node is System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.Primitives.TextBoxBase or PasswordBox or System.Windows.Controls.Primitives.Selector) return false;
        return ItemsControl.ContainerFromElement(this, source) is TabItem tab && BeginRenameTab(tab);
    }

    private bool TryBeginRenameFromKeyboard(KeyEventArgs e)
    {
        if (!CanRenameTabs || !RenameActivation.HasFlag(TabRenameActivation.F2) || e.Key != Key.F2 || Keyboard.Modifiers != ModifierKeys.None) return false;
        var tab = e.OriginalSource is DependencyObject source ? ItemsControl.ContainerFromElement(this, source) as TabItem : null;
        tab ??= ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as TabItem;
        return tab is not null && BeginRenameTab(tab);
    }
}

