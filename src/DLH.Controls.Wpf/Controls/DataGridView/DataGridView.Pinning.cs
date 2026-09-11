using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace DLH.Controls.Wpf;

public sealed class DataGridViewRowPinChangedEventArgs(object item) : EventArgs
{
    public object Item { get; } = item;
}

public sealed class DataGridViewColumnPinChangedEventArgs(DataGridColumn column) : EventArgs
{
    public DataGridColumn Column { get; } = column;
}

public partial class DataGridView
{
    private readonly ObservableCollection<object> pinnedRows = [];
    private readonly ObservableCollection<DataGridColumn> pinnedColumns = [];
    private ReadOnlyObservableCollection<object>? readOnlyPinnedRows;
    private ReadOnlyObservableCollection<DataGridColumn>? readOnlyPinnedColumns;
    private INotifyCollectionChanged? observedItemsSource;

    public static readonly DependencyProperty CanPinRowsProperty = DependencyProperty.Register(
        nameof(CanPinRows), typeof(bool), typeof(DataGridView), new PropertyMetadata(false));
    public bool CanPinRows { get => (bool)GetValue(CanPinRowsProperty); set => SetValue(CanPinRowsProperty, value); }

    public static readonly DependencyProperty CanPinColumnsProperty = DependencyProperty.Register(
        nameof(CanPinColumns), typeof(bool), typeof(DataGridView), new PropertyMetadata(false));
    public bool CanPinColumns { get => (bool)GetValue(CanPinColumnsProperty); set => SetValue(CanPinColumnsProperty, value); }

    public static readonly DependencyProperty ShowRowPinButtonProperty = DependencyProperty.Register(
        nameof(ShowRowPinButton), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowRowPinButton { get => (bool)GetValue(ShowRowPinButtonProperty); set => SetValue(ShowRowPinButtonProperty, value); }

    public static readonly DependencyProperty ShowColumnPinButtonProperty = DependencyProperty.Register(
        nameof(ShowColumnPinButton), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowColumnPinButton { get => (bool)GetValue(ShowColumnPinButtonProperty); set => SetValue(ShowColumnPinButtonProperty, value); }

    public static readonly DependencyProperty MaxPinnedRowsProperty = DependencyProperty.Register(
        nameof(MaxPinnedRows), typeof(int), typeof(DataGridView), new PropertyMetadata(5),
        value => value is int count && count > 0);
    public int MaxPinnedRows { get => (int)GetValue(MaxPinnedRowsProperty); set => SetValue(MaxPinnedRowsProperty, value); }

    public static readonly DependencyProperty MaxPinnedColumnsProperty = DependencyProperty.Register(
        nameof(MaxPinnedColumns), typeof(int), typeof(DataGridView), new PropertyMetadata(4),
        value => value is int count && count > 0);
    public int MaxPinnedColumns { get => (int)GetValue(MaxPinnedColumnsProperty); set => SetValue(MaxPinnedColumnsProperty, value); }

    public ReadOnlyObservableCollection<object> PinnedRows =>
        readOnlyPinnedRows ??= new ReadOnlyObservableCollection<object>(pinnedRows);

    public ReadOnlyObservableCollection<DataGridColumn> PinnedColumns =>
        readOnlyPinnedColumns ??= new ReadOnlyObservableCollection<DataGridColumn>(pinnedColumns);

    public event EventHandler<DataGridViewRowPinChangedEventArgs>? RowPinned;
    public event EventHandler<DataGridViewRowPinChangedEventArgs>? RowUnpinned;
    public event EventHandler<DataGridViewColumnPinChangedEventArgs>? ColumnPinned;
    public event EventHandler<DataGridViewColumnPinChangedEventArgs>? ColumnUnpinned;

    public bool PinRow(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!CanPinRows || pinnedRows.Contains(item) || !Items.Contains(item) || pinnedRows.Count >= MaxPinnedRows)
            return false;
        pinnedRows.Add(item);
        RowPinned?.Invoke(this, new DataGridViewRowPinChangedEventArgs(item));
        OnPinnedItemsChanged();
        return true;
    }

    public bool UnpinRow(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!pinnedRows.Remove(item)) return false;
        RowUnpinned?.Invoke(this, new DataGridViewRowPinChangedEventArgs(item));
        OnPinnedItemsChanged();
        return true;
    }

    public bool ToggleRowPin(object item) => pinnedRows.Contains(item) ? UnpinRow(item) : PinRow(item);

    public bool PinColumn(DataGridColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!CanPinColumns || pinnedColumns.Contains(column) || !Columns.Contains(column) ||
            column.Visibility != Visibility.Visible || pinnedColumns.Count >= MaxPinnedColumns) return false;
        pinnedColumns.Add(column);
        ColumnPinned?.Invoke(this, new DataGridViewColumnPinChangedEventArgs(column));
        OnPinnedItemsChanged();
        return true;
    }

    public bool UnpinColumn(DataGridColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!pinnedColumns.Remove(column)) return false;
        ColumnUnpinned?.Invoke(this, new DataGridViewColumnPinChangedEventArgs(column));
        OnPinnedItemsChanged();
        return true;
    }

    public bool ToggleColumnPin(DataGridColumn column) => pinnedColumns.Contains(column) ? UnpinColumn(column) : PinColumn(column);

    public void UnpinAllRows()
    {
        foreach (var item in pinnedRows.ToArray()) UnpinRow(item);
    }

    public void UnpinAllColumns()
    {
        foreach (var column in pinnedColumns.ToArray()) UnpinColumn(column);
    }

    private void OnItemsSourceChangedForPinning(System.Collections.IEnumerable newValue)
    {
        if (observedItemsSource is not null) observedItemsSource.CollectionChanged -= OnItemsSourceCollectionChanged;
        observedItemsSource = newValue as INotifyCollectionChanged;
        if (observedItemsSource is not null) observedItemsSource.CollectionChanged += OnItemsSourceCollectionChanged;
        RemoveUnavailablePinnedRows();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => RemoveUnavailablePinnedRows();

    private void RemoveUnavailablePinnedRows()
    {
        foreach (var item in pinnedRows.Where(item => !Items.Contains(item)).ToArray()) UnpinRow(item);
    }

    private void InitializePinning() => Columns.CollectionChanged += (_, _) =>
    {
        foreach (var column in pinnedColumns.Where(column => !Columns.Contains(column)).ToArray()) UnpinColumn(column);
    };

    partial void OnPinnedItemsChanged();
}
