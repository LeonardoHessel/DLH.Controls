using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

public enum DataGridViewFilterOperator
{
    Contains,
    Equals,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan
}

public sealed class DataGridViewFilter
{
    public string ColumnKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DataGridViewFilterOperator Operator { get; set; } = DataGridViewFilterOperator.Contains;
    public bool IsCaseSensitive { get; set; }
}

public partial class DataGridView
{
    private Predicate<object>? externalFilter;
    private Predicate<object>? componentFilter;
    private ICollectionView? filteredView;

    public ObservableCollection<DataGridViewFilter> Filters { get; } = new();

    public static readonly DependencyProperty CanUserFilterProperty = DependencyProperty.RegisterAttached(
        "CanUserFilter", typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public static bool GetCanUserFilter(DependencyObject column) => (bool)column.GetValue(CanUserFilterProperty);
    public static void SetCanUserFilter(DependencyObject column, bool value) => column.SetValue(CanUserFilterProperty, value);

    public static readonly DependencyProperty FilterMemberPathProperty = DependencyProperty.RegisterAttached(
        "FilterMemberPath", typeof(string), typeof(DataGridView), new PropertyMetadata(null));
    public static string? GetFilterMemberPath(DependencyObject column) => (string?)column.GetValue(FilterMemberPathProperty);
    public static void SetFilterMemberPath(DependencyObject column, string? value) => column.SetValue(FilterMemberPathProperty, value);

    public static readonly DependencyProperty ShowFilterMenuItemsProperty = DependencyProperty.Register(
        nameof(ShowFilterMenuItems), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowFilterMenuItems
    {
        get => (bool)GetValue(ShowFilterMenuItemsProperty);
        set => SetValue(ShowFilterMenuItemsProperty, value);
    }

    private void InitializeFiltering()
    {
        componentFilter = MatchesFilters;
        Filters.CollectionChanged += (_, _) => ApplyFilters();
    }

    public bool SetFilter(DataGridColumn column, string? value,
        DataGridViewFilterOperator filterOperator = DataGridViewFilterOperator.Contains, bool isCaseSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!Columns.Contains(column) || !GetCanUserFilter(column) || !Enum.IsDefined(filterOperator)) return false;
        var key = GetStableColumnKey(column);
        ClearFilterCore(key);
        if (!string.IsNullOrWhiteSpace(value))
            Filters.Add(new DataGridViewFilter
            {
                ColumnKey = key,
                Value = value,
                Operator = filterOperator,
                IsCaseSensitive = isCaseSensitive
            });
        return true;
    }

    public bool ClearFilter(DataGridColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!Columns.Contains(column)) return false;
        return ClearFilterCore(GetStableColumnKey(column));
    }

    public void ClearFilters() => Filters.Clear();

    public void RefreshFilters() => ApplyFilters();

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        if (filteredView is not null && ReferenceEquals(filteredView.Filter, componentFilter))
            filteredView.Filter = externalFilter;
        filteredView = null;
        externalFilter = null;
        base.OnItemsSourceChanged(oldValue, newValue);
        OnItemsSourceChangedForPinning(newValue);
        ApplyFilters();
        ApplyGrouping();
    }

    private bool ClearFilterCore(string key)
    {
        var matches = Filters.Where(filter => string.Equals(filter.ColumnKey, key, StringComparison.Ordinal)).ToList();
        foreach (var filter in matches) Filters.Remove(filter);
        return matches.Count > 0;
    }

    private void ApplyFilters()
    {
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view?.CanFilter != true) return;
        if (!ReferenceEquals(filteredView, view))
        {
            filteredView = view;
            externalFilter = view.Filter;
        }
        else if (!ReferenceEquals(view.Filter, componentFilter))
        {
            externalFilter = view.Filter;
        }
        view.Filter = Filters.Count == 0 ? externalFilter : componentFilter;
        view.Refresh();
    }

    private bool MatchesFilters(object item) =>
        (externalFilter?.Invoke(item) ?? true) && Filters.All(filter => MatchesFilter(item, filter));

    private bool MatchesFilter(object item, DataGridViewFilter filter)
    {
        var column = Columns.FirstOrDefault(candidate =>
            string.Equals(GetStableColumnKey(candidate), filter.ColumnKey, StringComparison.Ordinal));
        if (column is null || !GetCanUserFilter(column)) return true;
        var path = GetFilterMemberPath(column);
        if (string.IsNullOrWhiteSpace(path)) path = column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(path)) return true;
        var value = ReadMemberPath(item, path);
        if (filter.Operator is DataGridViewFilterOperator.GreaterThan or DataGridViewFilterOperator.LessThan)
            return Compare(value, filter.Value, filter.Operator);
        var actual = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        var comparison = filter.IsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        return filter.Operator switch
        {
            DataGridViewFilterOperator.Equals => string.Equals(actual, filter.Value, comparison),
            DataGridViewFilterOperator.StartsWith => actual.StartsWith(filter.Value, comparison),
            DataGridViewFilterOperator.EndsWith => actual.EndsWith(filter.Value, comparison),
            _ => actual.Contains(filter.Value, comparison)
        };
    }

    private static bool Compare(object? actual, string expected, DataGridViewFilterOperator filterOperator)
    {
        if (actual is null) return false;
        try
        {
            var converter = TypeDescriptor.GetConverter(actual.GetType());
            var converted = converter.ConvertFromString(null, CultureInfo.CurrentCulture, expected);
            if (actual is not IComparable comparable || converted is null) return false;
            var result = comparable.CompareTo(converted);
            return filterOperator == DataGridViewFilterOperator.GreaterThan ? result > 0 : result < 0;
        }
        catch (Exception error) when (error is FormatException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    private static object? ReadMemberPath(object? item, string path)
    {
        foreach (var member in path.Split('.'))
        {
            if (item is null) return null;
            item = TypeDescriptor.GetProperties(item)[member]?.GetValue(item);
        }
        return item;
    }
}
