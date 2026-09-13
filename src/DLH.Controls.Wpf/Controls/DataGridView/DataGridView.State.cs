using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

/// <summary>Estado persistível da organização visual de um DataGridView.</summary>
public sealed class DataGridViewState
{
    public int Version { get; set; } = 1;
    public List<DataGridViewColumnState> Columns { get; set; } = new();
    public List<DataGridViewSortState> Sorting { get; set; } = new();
    public List<string> PinnedColumnKeys { get; set; } = new();
    public List<string> PinnedRowKeys { get; set; } = new();
}

public sealed class DataGridViewColumnState
{
    public string Key { get; set; } = string.Empty;
    public int DisplayIndex { get; set; }
    public double WidthValue { get; set; }
    public DataGridLengthUnitType WidthUnitType { get; set; }
    public Visibility Visibility { get; set; }
}

public sealed class DataGridViewSortState
{
    public string ColumnKey { get; set; } = string.Empty;
    public ListSortDirection Direction { get; set; }
}

public sealed class DataGridViewStateRestoredEventArgs(DataGridViewState state) : EventArgs
{
    public DataGridViewState State { get; } = state;
}

public partial class DataGridView
{
    private static readonly JsonSerializerOptions StateJsonOptions = new() { WriteIndented = true };
    private DataGridViewState? initialState;

    public static readonly DependencyProperty ColumnKeyProperty = DependencyProperty.RegisterAttached(
        "ColumnKey", typeof(string), typeof(DataGridView), new PropertyMetadata(null));

    public static string? GetColumnKey(DependencyObject column) => (string?)column.GetValue(ColumnKeyProperty);
    public static void SetColumnKey(DependencyObject column, string? value) => column.SetValue(ColumnKeyProperty, value);

    public event EventHandler<DataGridViewStateRestoredEventArgs>? StateRestored;

    public static readonly DependencyProperty RowKeyMemberPathProperty = DependencyProperty.Register(
        nameof(RowKeyMemberPath), typeof(string), typeof(DataGridView), new PropertyMetadata(null));
    public string? RowKeyMemberPath
    {
        get => (string?)GetValue(RowKeyMemberPathProperty);
        set => SetValue(RowKeyMemberPathProperty, value);
    }

    public DataGridViewState CaptureState() => CaptureStateCore();

    public void RestoreState(DataGridViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var keyed = KeyedColumns();
        ValidateState(state, keyed);
        var previous = CaptureStateCore();
        try
        {
            ApplyState(state, keyed);
        }
        catch
        {
            ApplyState(previous, KeyedColumns());
            throw;
        }
        StateRestored?.Invoke(this, new DataGridViewStateRestoredEventArgs(CloneState(state)));
    }

    public void SaveState(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        JsonSerializer.Serialize(destination, CaptureState(), StateJsonOptions);
    }

    public void LoadState(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        RestoreState(JsonSerializer.Deserialize<DataGridViewState>(source) ??
            throw new InvalidDataException("Estado vazio."));
    }

    public bool ResetState()
    {
        if (initialState is null) return false;
        RestoreState(CloneState(initialState));
        return true;
    }

    private void CaptureInitialState()
    {
        if (initialState is not null) return;
        try { initialState = CaptureStateCore(); }
        catch (InvalidOperationException) { }
    }

    private DataGridViewState CaptureStateCore()
    {
        var keyed = KeyedColumns();
        var byPath = keyed.Where(pair => !string.IsNullOrWhiteSpace(pair.Column.SortMemberPath))
            .GroupBy(pair => pair.Column.SortMemberPath!, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Key, StringComparer.Ordinal);
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        return new DataGridViewState
        {
            Columns = keyed.Select(pair => new DataGridViewColumnState
            {
                Key = pair.Key,
                DisplayIndex = pair.Column.DisplayIndex,
                WidthValue = pair.Column.Width.Value,
                WidthUnitType = pair.Column.Width.UnitType,
                Visibility = pair.Column.Visibility
            }).ToList(),
            Sorting = view?.SortDescriptions
                .Where(sort => byPath.ContainsKey(sort.PropertyName))
                .Select(sort => new DataGridViewSortState
                {
                    ColumnKey = byPath[sort.PropertyName],
                    Direction = sort.Direction
                }).ToList() ?? new List<DataGridViewSortState>(),
            PinnedColumnKeys = pinnedColumns.Select(GetStableColumnKey)
                .Where(key => !string.IsNullOrWhiteSpace(key)).ToList(),
            PinnedRowKeys = string.IsNullOrWhiteSpace(RowKeyMemberPath)
                ? []
                : pinnedRows.Select(GetRowKey).Where(key => key is not null).Cast<string>().ToList()
        };
    }

    private List<(string Key, DataGridColumn Column)> KeyedColumns()
    {
        var result = new List<(string, DataGridColumn)>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in Columns)
        {
            var key = GetStableColumnKey(column);
            if (string.IsNullOrWhiteSpace(key) || !used.Add(key))
                throw new InvalidOperationException("Cada coluna deve ter uma chave estável, não vazia e única (ColumnKey ou SortMemberPath).");
            result.Add((key, column));
        }
        return result;
    }

    private static string GetStableColumnKey(DataGridColumn column)
    {
        var key = GetColumnKey(column);
        if (string.IsNullOrWhiteSpace(key)) key = column.SortMemberPath;
        return key ?? string.Empty;
    }

    private void ValidateState(DataGridViewState state, List<(string Key, DataGridColumn Column)> current)
    {
        if (state.Version != 1 || state.Columns is null || state.Sorting is null ||
            state.PinnedColumnKeys is null || state.PinnedRowKeys is null ||
            state.Columns.Any(column => string.IsNullOrWhiteSpace(column.Key) || column.DisplayIndex < 0 ||
                !double.IsFinite(column.WidthValue) || column.WidthValue <= 0 ||
                !Enum.IsDefined(column.WidthUnitType) || !Enum.IsDefined(column.Visibility)) ||
            state.Columns.Select(column => column.Key).Distinct(StringComparer.Ordinal).Count() != state.Columns.Count ||
            state.Columns.Select(column => column.DisplayIndex).Distinct().Count() != state.Columns.Count ||
            state.Sorting.Any(sort => string.IsNullOrWhiteSpace(sort.ColumnKey) || !Enum.IsDefined(sort.Direction)) ||
            state.Sorting.Select(sort => sort.ColumnKey).Distinct(StringComparer.Ordinal).Count() != state.Sorting.Count ||
            state.PinnedColumnKeys.Any(string.IsNullOrWhiteSpace) ||
            state.PinnedColumnKeys.Distinct(StringComparer.Ordinal).Count() != state.PinnedColumnKeys.Count ||
            state.PinnedRowKeys.Any(string.IsNullOrWhiteSpace) ||
            state.PinnedRowKeys.Distinct(StringComparer.Ordinal).Count() != state.PinnedRowKeys.Count)
            throw new ArgumentException("Estado do DataGridView inválido ou versão não suportada.", nameof(state));

        var states = state.Columns.ToDictionary(column => column.Key, StringComparer.Ordinal);
        if (current.Count > 0 && !current.Any(pair => !states.TryGetValue(pair.Key, out var saved) || saved.Visibility == Visibility.Visible))
            throw new ArgumentException("O estado precisa manter pelo menos uma coluna visível.", nameof(state));

        var known = current.Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal);
        var knownSorts = state.Sorting.Where(sort => known.Contains(sort.ColumnKey)).ToList();
        if (knownSorts.Count > 0 && CollectionViewSource.GetDefaultView(ItemsSource)?.CanSort != true)
            throw new InvalidOperationException("A fonte de dados não oferece suporte à ordenação restaurada.");
    }

    private void ApplyState(DataGridViewState state, List<(string Key, DataGridColumn Column)> current)
    {
        applyingPinnedState = true;
        try
        {
            ApplyStateCore(state, current);
        }
        finally
        {
            applyingPinnedState = false;
        }
    }

    private void ApplyStateCore(DataGridViewState state, List<(string Key, DataGridColumn Column)> current)
    {
        var byKey = current.ToDictionary(pair => pair.Key, pair => pair.Column, StringComparer.Ordinal);
        var savedByKey = state.Columns.ToDictionary(column => column.Key, StringComparer.Ordinal);
        var ordered = state.Columns.Where(saved => byKey.ContainsKey(saved.Key)).OrderBy(saved => saved.DisplayIndex)
            .Select(saved => byKey[saved.Key])
            .Concat(current.Where(pair => !savedByKey.ContainsKey(pair.Key)).OrderBy(pair => pair.Column.DisplayIndex).Select(pair => pair.Column))
            .ToList();

        foreach (var saved in state.Columns.Where(saved => byKey.ContainsKey(saved.Key)))
        {
            var column = byKey[saved.Key];
            column.Width = new DataGridLength(saved.WidthValue, saved.WidthUnitType);
            column.Visibility = saved.Visibility;
        }
        for (var index = 0; index < ordered.Count; index++) ordered[index].DisplayIndex = index;

        ClearSorting();
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        foreach (var sort in state.Sorting.Where(sort => byKey.ContainsKey(sort.ColumnKey)))
        {
            var column = byKey[sort.ColumnKey];
            if (string.IsNullOrWhiteSpace(column.SortMemberPath)) continue;
            view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sort.Direction));
            column.SortDirection = sort.Direction;
        }
        UpdateSortPriorities();

        var targetColumns = CanPinColumns
            ? state.PinnedColumnKeys.Where(byKey.ContainsKey).Select(key => byKey[key])
                .Where(column => column.Visibility == Visibility.Visible).Take(MaxPinnedColumns).ToList()
            : [];
        ReconcilePins(pinnedColumns, targetColumns, UnpinColumn, PinColumn);

        var targetRows = new List<object>();
        if (CanPinRows && !string.IsNullOrWhiteSpace(RowKeyMemberPath))
        {
            var rowsByKey = Items.Cast<object>().Select(item => (Key: GetRowKey(item), Item: item))
                .Where(pair => pair.Key is not null)
                .GroupBy(pair => pair.Key!, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single().Item, StringComparer.Ordinal);
            targetRows.AddRange(state.PinnedRowKeys.Where(rowsByKey.ContainsKey).Select(key => rowsByKey[key])
                .Take(MaxPinnedRows));
        }
        ReconcilePins(pinnedRows, targetRows, UnpinRow, PinRow);
    }

    /// <summary>
    /// Makes <paramref name="pinned"/> hold exactly <paramref name="target"/>, by reference identity
    /// (not <see cref="object.Equals(object)"/>, which record-typed row models override), then reorders
    /// it to match <paramref name="target"/> exactly — without unpinning/re-pinning (and so without
    /// firing pin-changed events for) items whose pinned status didn't actually change.
    /// </summary>
    private static void ReconcilePins<T>(ObservableCollection<T> pinned, List<T> target, Func<T, bool> unpin, Func<T, bool> pin)
        where T : class
    {
        var referenceComparer = (IEqualityComparer<T>)(object)ReferenceEqualityComparer.Instance;
        var targetSet = new HashSet<T>(target, referenceComparer);
        var pinnedSet = new HashSet<T>(pinned, referenceComparer);
        foreach (var item in pinned.Where(item => !targetSet.Contains(item)).ToArray()) unpin(item);
        foreach (var item in target.Where(item => !pinnedSet.Contains(item))) pin(item);
        for (var index = 0; index < target.Count; index++)
        {
            var currentIndex = -1;
            for (var search = index; search < pinned.Count; search++)
                if (ReferenceEquals(pinned[search], target[index])) { currentIndex = search; break; }
            if (currentIndex >= 0 && currentIndex != index) pinned.Move(currentIndex, index);
        }
    }

    private static DataGridViewState CloneState(DataGridViewState state) => new()
    {
        Version = state.Version,
        Columns = state.Columns.Select(column => new DataGridViewColumnState
        {
            Key = column.Key,
            DisplayIndex = column.DisplayIndex,
            WidthValue = column.WidthValue,
            WidthUnitType = column.WidthUnitType,
            Visibility = column.Visibility
        }).ToList(),
        Sorting = state.Sorting.Select(sort => new DataGridViewSortState
        {
            ColumnKey = sort.ColumnKey,
            Direction = sort.Direction
        }).ToList(),
        PinnedColumnKeys = state.PinnedColumnKeys.ToList(),
        PinnedRowKeys = state.PinnedRowKeys.ToList()
    };

    private string? GetRowKey(object item)
    {
        if (string.IsNullOrWhiteSpace(RowKeyMemberPath)) return null;
        return ReadMemberPath(item, RowKeyMemberPath)?.ToString();
    }
}
