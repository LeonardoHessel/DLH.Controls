using System.Windows;
using System.Windows.Input;

namespace DLH.Controls.Wpf;

public sealed class AddTabRequestedEventArgs(object? parameter) : EventArgs
{
    public object? Parameter { get; } = parameter;
}

public partial class TabControl
{
    private EventHandler<AddTabRequestedEventArgs>? addTabRequested;

    public static readonly RoutedUICommand AddTab = new("Adicionar aba", nameof(AddTab), typeof(TabControl));

    public event EventHandler<AddTabRequestedEventArgs>? AddTabRequested
    {
        add { addTabRequested += value; CommandManager.InvalidateRequerySuggested(); }
        remove { addTabRequested -= value; CommandManager.InvalidateRequerySuggested(); }
    }

    public static readonly DependencyProperty CanAddTabsProperty = DependencyProperty.Register(
        nameof(CanAddTabs), typeof(bool), typeof(TabControl),
        new PropertyMetadata(false, (_, _) => CommandManager.InvalidateRequerySuggested()));
    public bool CanAddTabs { get => (bool)GetValue(CanAddTabsProperty); set => SetValue(CanAddTabsProperty, value); }

    public static readonly DependencyProperty AddTabCommandProperty = DependencyProperty.Register(
        nameof(AddTabCommand), typeof(ICommand), typeof(TabControl),
        new PropertyMetadata(null, OnAddTabCommandChanged));
    public ICommand? AddTabCommand { get => (ICommand?)GetValue(AddTabCommandProperty); set => SetValue(AddTabCommandProperty, value); }

    public static readonly DependencyProperty AddTabCommandParameterProperty = DependencyProperty.Register(
        nameof(AddTabCommandParameter), typeof(object), typeof(TabControl), new PropertyMetadata(null));
    public object? AddTabCommandParameter { get => GetValue(AddTabCommandParameterProperty); set => SetValue(AddTabCommandParameterProperty, value); }

    public static readonly DependencyProperty AddTabContentProperty = DependencyProperty.Register(
        nameof(AddTabContent), typeof(object), typeof(TabControl), new PropertyMetadata("+"));
    public object? AddTabContent { get => GetValue(AddTabContentProperty); set => SetValue(AddTabContentProperty, value); }

    public static readonly DependencyProperty AddTabContentTemplateProperty = DependencyProperty.Register(
        nameof(AddTabContentTemplate), typeof(DataTemplate), typeof(TabControl), new PropertyMetadata(null));
    public DataTemplate? AddTabContentTemplate { get => (DataTemplate?)GetValue(AddTabContentTemplateProperty); set => SetValue(AddTabContentTemplateProperty, value); }

    private static void OnAddTabCommandChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var control = (TabControl)owner;
        if (args.OldValue is ICommand previous) previous.CanExecuteChanged -= control.OnAddTabCanExecuteChanged;
        if (args.NewValue is ICommand current) current.CanExecuteChanged += control.OnAddTabCanExecuteChanged;
        CommandManager.InvalidateRequerySuggested();
    }

    private void OnAddTabCanExecuteChanged(object? sender, EventArgs e) => CommandManager.InvalidateRequerySuggested();

    private bool CanRequestAddTab(object? parameter) =>
        CanAddTabs && (AddTabCommand?.CanExecute(parameter) ?? addTabRequested is not null);

    public bool RequestAddTab(object? parameter = null)
    {
        if (!CanRequestAddTab(parameter)) return false;
        if (AddTabCommand is { } command) command.Execute(parameter);
        else addTabRequested?.Invoke(this, new AddTabRequestedEventArgs(parameter));
        return true;
    }
}

