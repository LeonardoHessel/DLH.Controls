using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

public enum DataGridViewSelectionBehavior
{
    None,
    Row,
    Column,
    Cell
}

public sealed record DataGridViewSelection(object? Item, DataGridColumn? Column);

public class DataGridView : DataGrid
{
    private object? lastNotifiedItem;
    private DataGridColumn? lastNotifiedColumn;
    private bool applyingSelectionBehavior;

    static DataGridView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridView), new FrameworkPropertyMetadata(typeof(DataGridView)));
    }

    public DataGridView()
    {
        SetCurrentValue(AutoGenerateColumnsProperty, false);
        SetCurrentValue(CanUserAddRowsProperty, false);
        SetCurrentValue(CanUserDeleteRowsProperty, false);
        SetCurrentValue(IsReadOnlyProperty, true);
        SetCurrentValue(HeadersVisibilityProperty, DataGridHeadersVisibility.Column);
        SetCurrentValue(EnableRowVirtualizationProperty, true);
        SetCurrentValue(EnableColumnVirtualizationProperty, true);
        SetCurrentValue(CanUserReorderColumnsProperty, true);
        ApplySelectionBehavior();
    }

    public static readonly DependencyProperty SelectionBehaviorProperty = DependencyProperty.Register(
        nameof(SelectionBehavior), typeof(DataGridViewSelectionBehavior), typeof(DataGridView),
        new FrameworkPropertyMetadata(DataGridViewSelectionBehavior.None, OnSelectionBehaviorChanged),
        value => value is DataGridViewSelectionBehavior behavior && Enum.IsDefined(behavior));
    public DataGridViewSelectionBehavior SelectionBehavior
    {
        get => (DataGridViewSelectionBehavior)GetValue(SelectionBehaviorProperty);
        set => SetValue(SelectionBehaviorProperty, value);
    }

    public static readonly DependencyProperty SelectionChangedCommandProperty = DependencyProperty.Register(
        nameof(SelectionChangedCommand), typeof(ICommand), typeof(DataGridView), new PropertyMetadata(null));
    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly DependencyProperty SelectedColumnProperty = DependencyProperty.Register(
        nameof(SelectedColumn), typeof(DataGridColumn), typeof(DataGridView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColumnChanged));
    public DataGridColumn? SelectedColumn
    {
        get => (DataGridColumn?)GetValue(SelectedColumnProperty);
        set => SetValue(SelectedColumnProperty, value);
    }

    public static readonly DependencyProperty CanUserToggleColumnVisibilityProperty = DependencyProperty.Register(
        nameof(CanUserToggleColumnVisibility), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool CanUserToggleColumnVisibility
    {
        get => (bool)GetValue(CanUserToggleColumnVisibilityProperty);
        set => SetValue(CanUserToggleColumnVisibilityProperty, value);
    }

    public static readonly DependencyProperty ShowRowSeparatorsProperty = DependencyProperty.Register(
        nameof(ShowRowSeparators), typeof(bool), typeof(DataGridView),
        new FrameworkPropertyMetadata(true, OnShowRowSeparatorsChanged));
    public bool ShowRowSeparators
    {
        get => (bool)GetValue(ShowRowSeparatorsProperty);
        set => SetValue(ShowRowSeparatorsProperty, value);
    }

    public static readonly DependencyProperty CellPaddingProperty = DependencyProperty.Register(
        nameof(CellPadding), typeof(Thickness), typeof(DataGridView),
        new FrameworkPropertyMetadata(new Thickness(10, 7, 10, 7), FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => value is Thickness thickness && IsValidThickness(thickness));
    public Thickness CellPadding
    {
        get => (Thickness)GetValue(CellPaddingProperty);
        set => SetValue(CellPaddingProperty, value);
    }

    private static bool IsValidThickness(Thickness value) =>
        new[] { value.Left, value.Top, value.Right, value.Bottom }.All(number => double.IsFinite(number) && number >= 0);

    private static void OnSelectionBehaviorChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((DataGridView)owner).ApplySelectionBehavior();

    private void ApplySelectionBehavior()
    {
        applyingSelectionBehavior = true;
        try
        {
            SetCurrentValue(SelectionUnitProperty, SelectionBehavior == DataGridViewSelectionBehavior.Cell
                ? DataGridSelectionUnit.Cell : DataGridSelectionUnit.FullRow);
            if (SelectionBehavior == DataGridViewSelectionBehavior.None)
            {
                UnselectAll();
                CurrentCell = new DataGridCellInfo();
            }
            if (SelectionBehavior is not DataGridViewSelectionBehavior.Column and not DataGridViewSelectionBehavior.Cell)
                SetCurrentValue(SelectedColumnProperty, null);
            lastNotifiedItem = null;
            lastNotifiedColumn = null;
        }
        finally { applyingSelectionBehavior = false; }
    }

    private static void OnShowRowSeparatorsChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((DataGridView)owner).SetCurrentValue(GridLinesVisibilityProperty,
            (bool)args.NewValue ? DataGridGridLinesVisibility.Horizontal : DataGridGridLinesVisibility.None);

    private static void OnSelectedColumnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var grid = (DataGridView)owner;
        if (args.NewValue is DataGridColumn column && !grid.Columns.Contains(column))
        {
            grid.SetCurrentValue(SelectedColumnProperty, null);
            return;
        }
        if (!grid.applyingSelectionBehavior && grid.SelectionBehavior == DataGridViewSelectionBehavior.Column)
            grid.NotifySelectionChanged();
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && SelectionBehavior == DataGridViewSelectionBehavior.Column &&
            FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject) is { Column: { } column })
        {
            SetCurrentValue(SelectedColumnProperty, column);
            UnselectAll();
            CurrentCell = new DataGridCellInfo();
        }
        if (SelectionBehavior == DataGridViewSelectionBehavior.None &&
            FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) is not null)
        {
            Focus();
            e.Handled = true;
            return;
        }
        base.OnPreviewMouseDown(e);
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        if (!applyingSelectionBehavior) NotifySelectionChanged();
    }

    protected override void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
    {
        base.OnSelectedCellsChanged(e);
        if (!applyingSelectionBehavior) NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        if (SelectionBehavior == DataGridViewSelectionBehavior.None) return;
        var item = SelectionBehavior == DataGridViewSelectionBehavior.Column ? null : SelectedItem;
        var column = SelectionBehavior switch
        {
            DataGridViewSelectionBehavior.Column => SelectedColumn,
            DataGridViewSelectionBehavior.Cell when CurrentCell.IsValid => CurrentCell.Column,
            _ => null
        };
        if (SelectionBehavior != DataGridViewSelectionBehavior.Column && item is null ||
            SelectionBehavior == DataGridViewSelectionBehavior.Column && column is null) return;
        if (ReferenceEquals(lastNotifiedItem, item) && ReferenceEquals(lastNotifiedColumn, column)) return;
        var selection = new DataGridViewSelection(item, column);
        if (SelectionChangedCommand?.CanExecute(selection) == true)
        {
            lastNotifiedItem = item;
            lastNotifiedColumn = column;
            SelectionChangedCommand.Execute(selection);
        }
    }

    protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
    {
        if (CanUserToggleColumnVisibility && FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject) is { } header)
        {
            var menu = CreateColumnVisibilityMenu();
            menu.PlacementTarget = header;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;
            e.Handled = true;
            return;
        }
        base.OnPreviewMouseRightButtonDown(e);
    }

    internal ContextMenu CreateColumnVisibilityMenu()
    {
        var menu = new ContextMenu();
        var visibleCount = Columns.Count(column => column.Visibility == Visibility.Visible);
        foreach (var column in Columns.OrderBy(column => column.DisplayIndex))
        {
            var item = new MenuItem
            {
                Header = column.Header?.ToString() ?? $"Coluna {column.DisplayIndex + 1}",
                IsCheckable = true,
                IsChecked = column.Visibility == Visibility.Visible,
                IsEnabled = column.Visibility != Visibility.Visible || visibleCount > 1,
                Tag = column
            };
            item.Click += (_, _) =>
            {
                var target = (DataGridColumn)item.Tag;
                target.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                if (ReferenceEquals(SelectedColumn, target) && target.Visibility != Visibility.Visible)
                    SetCurrentValue(SelectedColumnProperty, null);
            };
            menu.Items.Add(item);
        }
        return menu;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T result) return result;
            current = current is FrameworkContentElement content ? content.Parent : VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
