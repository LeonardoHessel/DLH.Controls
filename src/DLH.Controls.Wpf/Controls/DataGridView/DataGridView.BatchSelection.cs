using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DLH.Controls.Wpf;

public sealed record DataGridViewBatchSelection(IReadOnlyList<object> Items,
    IReadOnlyList<DataGridCellInfo> Cells, DataGridColumn? Column);

public partial class DataGridView
{
    public static readonly DependencyProperty AllowMultipleSelectionProperty = DependencyProperty.Register(
        nameof(AllowMultipleSelection), typeof(bool), typeof(DataGridView),
        new FrameworkPropertyMetadata(false, (owner, _) => ((DataGridView)owner).ApplySelectionMode()));
    public bool AllowMultipleSelection { get => (bool)GetValue(AllowMultipleSelectionProperty); set => SetValue(AllowMultipleSelectionProperty, value); }

    public static readonly DependencyProperty BatchSelectionChangedCommandProperty = DependencyProperty.Register(
        nameof(BatchSelectionChangedCommand), typeof(ICommand), typeof(DataGridView), new PropertyMetadata(null));
    public ICommand? BatchSelectionChangedCommand { get => (ICommand?)GetValue(BatchSelectionChangedCommandProperty); set => SetValue(BatchSelectionChangedCommandProperty, value); }

    public static readonly DependencyProperty BatchActionCommandProperty = DependencyProperty.Register(
        nameof(BatchActionCommand), typeof(ICommand), typeof(DataGridView), new PropertyMetadata(null));
    public ICommand? BatchActionCommand { get => (ICommand?)GetValue(BatchActionCommandProperty); set => SetValue(BatchActionCommandProperty, value); }

    public DataGridViewBatchSelection GetBatchSelection()
    {
        var items = SelectionBehavior == DataGridViewSelectionBehavior.Cell
            ? SelectedCells.Select(cell => cell.Item).Distinct().ToArray()
            : SelectedItems.Cast<object>().ToArray();
        var cells = SelectionBehavior == DataGridViewSelectionBehavior.Cell ? SelectedCells.ToArray() : [];
        return new(items, cells, SelectionBehavior == DataGridViewSelectionBehavior.Column ? SelectedColumn : null);
    }

    public bool ExecuteBatchAction()
    {
        var selection = GetBatchSelection();
        if (BatchActionCommand?.CanExecute(selection) != true) return false;
        BatchActionCommand.Execute(selection);
        return true;
    }

    private void ApplySelectionMode() => SetCurrentValue(SelectionModeProperty,
        AllowMultipleSelection ? DataGridSelectionMode.Extended : DataGridSelectionMode.Single);

    private void NotifyBatchSelectionChanged()
    {
        var selection = GetBatchSelection();
        if (BatchSelectionChangedCommand?.CanExecute(selection) == true) BatchSelectionChangedCommand.Execute(selection);
    }
}
