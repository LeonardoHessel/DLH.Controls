using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DLH.Controls.Wpf;

public partial class DataGridView
{
    private static readonly DependencyPropertyKey SortPriorityPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "SortPriority", typeof(int), typeof(DataGridView), new PropertyMetadata(0));
    public static readonly DependencyProperty SortPriorityProperty = SortPriorityPropertyKey.DependencyProperty;

    public static int GetSortPriority(DependencyObject column) => (int)column.GetValue(SortPriorityProperty);

    public static readonly DependencyProperty IsMultiColumnSortEnabledProperty = DependencyProperty.Register(
        nameof(IsMultiColumnSortEnabled), typeof(bool), typeof(DataGridView), new PropertyMetadata(false));
    public bool IsMultiColumnSortEnabled
    {
        get => (bool)GetValue(IsMultiColumnSortEnabledProperty);
        set => SetValue(IsMultiColumnSortEnabledProperty, value);
    }

    private void OnGridSorting(object sender, DataGridSortingEventArgs e)
    {
        if (!IsMultiColumnSortEnabled || !CanUserSortColumns || !e.Column.CanUserSort ||
            string.IsNullOrWhiteSpace(e.Column.SortMemberPath)) return;

        e.Handled = true;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            RemoveSort(e.Column);
            return;
        }

        var direction = e.Column.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
        ApplySort(e.Column, direction, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    public bool ApplySort(DataGridColumn column, ListSortDirection direction, bool append = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!Columns.Contains(column) || string.IsNullOrWhiteSpace(column.SortMemberPath) ||
            !Enum.IsDefined(direction)) return false;
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view?.CanSort != true) return false;

        if (!append) ClearSorting();
        else
        {
            for (var index = view.SortDescriptions.Count - 1; index >= 0; index--)
                if (string.Equals(view.SortDescriptions[index].PropertyName, column.SortMemberPath, StringComparison.Ordinal))
                    view.SortDescriptions.RemoveAt(index);
            foreach (var candidate in Columns.Where(candidate =>
                string.Equals(candidate.SortMemberPath, column.SortMemberPath, StringComparison.Ordinal)))
                candidate.SortDirection = null;
        }

        view.SortDescriptions.Add(new SortDescription(column.SortMemberPath, direction));
        column.SortDirection = direction;
        UpdateSortPriorities();
        QueuePinningVisualUpdate();
        return true;
    }

    public bool RemoveSort(DataGridColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!Columns.Contains(column) || string.IsNullOrWhiteSpace(column.SortMemberPath)) return false;
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view?.CanSort != true) return false;
        var removed = false;
        for (var index = view.SortDescriptions.Count - 1; index >= 0; index--)
        {
            if (!string.Equals(view.SortDescriptions[index].PropertyName, column.SortMemberPath, StringComparison.Ordinal)) continue;
            view.SortDescriptions.RemoveAt(index);
            removed = true;
        }
        column.SortDirection = null;
        UpdateSortPriorities();
        QueuePinningVisualUpdate();
        return removed;
    }

    private void UpdateSortPriorities()
    {
        foreach (var column in Columns) column.SetValue(SortPriorityPropertyKey, 0);
        var sorts = CollectionViewSource.GetDefaultView(ItemsSource)?.SortDescriptions;
        if (sorts is null || sorts.Count < 2) return;
        for (var index = 0; index < sorts.Count; index++)
        {
            var column = Columns.FirstOrDefault(candidate =>
                string.Equals(candidate.SortMemberPath, sorts[index].PropertyName, StringComparison.Ordinal));
            column?.SetValue(SortPriorityPropertyKey, index + 1);
        }
    }
}
