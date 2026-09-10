using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

public sealed class DataGridViewCellEditContext
{
    public required object Item { get; init; }
    public required DataGridColumn Column { get; init; }
    public required DataGridEditAction Action { get; init; }
    public bool Cancel { get; set; }
}

public partial class DataGridView
{
    public static readonly DependencyProperty IsCellEditingEnabledProperty = DependencyProperty.Register(
        nameof(IsCellEditingEnabled), typeof(bool), typeof(DataGridView),
        new FrameworkPropertyMetadata(false, (owner, args) =>
            ((DataGridView)owner).SetCurrentValue(IsReadOnlyProperty, !(bool)args.NewValue)));
    public bool IsCellEditingEnabled { get => (bool)GetValue(IsCellEditingEnabledProperty); set => SetValue(IsCellEditingEnabledProperty, value); }

    public static readonly DependencyProperty ShowValidationErrorsProperty = DependencyProperty.Register(
        nameof(ShowValidationErrors), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowValidationErrors { get => (bool)GetValue(ShowValidationErrorsProperty); set => SetValue(ShowValidationErrorsProperty, value); }

    public static readonly DependencyProperty ValidationErrorBrushProperty = DependencyProperty.Register(
        nameof(ValidationErrorBrush), typeof(Brush), typeof(DataGridView),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 112, 122))));
    public Brush ValidationErrorBrush { get => (Brush)GetValue(ValidationErrorBrushProperty); set => SetValue(ValidationErrorBrushProperty, value); }

    public static readonly DependencyProperty CellEditEndingCommandProperty = DependencyProperty.Register(
        nameof(CellEditEndingCommand), typeof(ICommand), typeof(DataGridView), new PropertyMetadata(null));
    public ICommand? CellEditEndingCommand { get => (ICommand?)GetValue(CellEditEndingCommandProperty); set => SetValue(CellEditEndingCommandProperty, value); }

    protected override void OnCellEditEnding(DataGridCellEditEndingEventArgs e)
    {
        var context = new DataGridViewCellEditContext { Item = e.Row.Item, Column = e.Column, Action = e.EditAction };
        if (CellEditEndingCommand?.CanExecute(context) == true) CellEditEndingCommand.Execute(context);
        if (context.Cancel) e.Cancel = true;
        base.OnCellEditEnding(e);
    }
}
