using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.Demo;

public partial class DataGridViewDemoWindow : Window
{
    private int nextRow = 19;
    private readonly List<ShipmentRow> emptyStateBackup = [];
    private DataGridViewState? savedLayout;

    public ObservableCollection<ShipmentRow> Rows { get; } =
    [
        new("EXP-2026-001", "SP", "Caminhão 12", 120, 120, "Concluído"),
        new("EXP-2026-002", "MG", "Caminhão 07", 85, 62, "Em inspeção"),
        new("EXP-2026-003", "PR", "Van 03", 34, 0, "Pendente"),
        new("EXP-2026-004", "RJ", "Caminhão 18", 210, 180, "Em inspeção"),
        new("EXP-2026-005", "BA", "Caminhão 04", 98, 98, "Concluído"),
        new("EXP-2026-006", "SC", "Van 09", 42, 11, "Em inspeção")
    ];

    public ICommand SelectionCommand { get; }

    public DataGridViewDemoWindow()
    {
        SelectionCommand = new ParameterCommand(OnSelection, _ => true);
        InitializeComponent();
        var origins = new[] { "SP", "MG", "PR", "RJ", "BA", "SC" };
        for (var number = 7; number <= 18; number++)
            Rows.Add(new($"EXP-2026-{number:000}", origins[number % origins.Length], $"Veículo {number:00}",
                30 + number * 3, number % 3 == 0 ? 0 : 20 + number, number % 3 == 0 ? "Pendente" : "Em inspeção"));
        DataContext = this;
    }

    private void SelectionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShipmentsGrid is null || SelectionSelector.SelectedItem is not ComboBoxItem { Tag: string value }) return;
        ShipmentsGrid.SelectionBehavior = Enum.Parse<DataGridViewSelectionBehavior>(value);
        StatusText.Text = value == "None" ? "Seleção desativada." : $"Modo de seleção: {value}.";
    }

    private void OptionsChanged(object sender, RoutedEventArgs e)
    {
        if (ShipmentsGrid is null) return;
        ShipmentsGrid.CanUserSortColumns = EnableSorting.IsChecked == true;
        ShipmentsGrid.AllowMultipleSelection = MultipleSelection.IsChecked == true;
        ShipmentsGrid.IsCellEditingEnabled = EnableEditing.IsChecked == true;
        ShipmentsGrid.IsMultiColumnSortEnabled = MultiColumnSort.IsChecked == true;
        ShipmentsGrid.CanUserReorderColumns = ReorderColumns.IsChecked == true;
        ShipmentsGrid.CanUserToggleColumnVisibility = ColumnMenu.IsChecked == true;
        ShipmentsGrid.ShowClearSortMenuItem = ShowClearSort.IsChecked == true;
        ShipmentsGrid.ShowRestoreDefaultSortMenuItem = ShowRestoreSort.IsChecked == true;
        ShipmentsGrid.AlternatingRowBackground = StripedRows.IsChecked == true
            ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#24373A40")!
            : System.Windows.Media.Brushes.Transparent;
    }

    private void DensitySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShipmentsGrid is null || DensitySelector.SelectedItem is not ComboBoxItem { Tag: string value }) return;
        ShipmentsGrid.Density = Enum.Parse<DataGridViewDensity>(value);
    }

    private void ScrollBarThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ShipmentsGrid is not null) ShipmentsGrid.ScrollBarThickness = e.NewValue;
    }

    private void FilterText_Changed(object sender, TextChangedEventArgs e)
    {
        if (ShipmentsGrid?.Columns.Count > 0)
            ShipmentsGrid.SetFilter(ShipmentsGrid.Columns[0], FilterText.Text);
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        FilterText.Clear();
        ShipmentsGrid.ClearFilters();
        StatusText.Text = "Filtros removidos.";
    }

    private void OnSelection(object? value)
    {
        if (value is not DataGridViewSelection selection) return;
        var item = selection.Item is ShipmentRow row ? row.Shipment : "todas as linhas";
        StatusText.Text = selection.Column is null
            ? $"Linha selecionada: {item}."
            : $"Seleção: {item}, coluna {selection.Column.Header}.";
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        Rows.Add(new($"EXP-2026-{nextRow:000}", "SP", $"Veículo {nextRow:00}", 20 + nextRow, 0, "Pendente"));
        nextRow++;
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ShipmentsGrid.SelectedItem is ShipmentRow row) Rows.Remove(row);
    }

    private void ProcessSelected_Click(object sender, RoutedEventArgs e)
    {
        var selection = ShipmentsGrid.GetBatchSelection();
        StatusText.Text = $"Ação aplicada a {selection.Items.Count} registro(s).";
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Arquivo CSV (*.csv)|*.csv", FileName = "inspecoes.csv" };
        if (dialog.ShowDialog(this) != true) return;
        using var stream = File.Create(dialog.FileName);
        ShipmentsGrid.ExportCsv(stream);
        StatusText.Text = "Dados visíveis exportados para CSV.";
    }

    private void RestoreColumns_Click(object sender, RoutedEventArgs e)
    {
        foreach (var column in ShipmentsGrid.Columns) column.Visibility = Visibility.Visible;
    }

    private void SaveLayout_Click(object sender, RoutedEventArgs e)
    {
        savedLayout = ShipmentsGrid.CaptureState();
        StatusText.Text = "Layout das colunas salvo na sessão.";
    }

    private void LoadLayout_Click(object sender, RoutedEventArgs e)
    {
        if (savedLayout is null)
        {
            StatusText.Text = "Salve um layout antes de restaurá-lo.";
            return;
        }
        ShipmentsGrid.RestoreState(savedLayout);
        StatusText.Text = "Layout salvo restaurado.";
    }

    private void ResetLayout_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = ShipmentsGrid.ResetState()
            ? "Layout inicial restaurado."
            : "O layout inicial ainda não está disponível.";
    }

    private void ToggleLoading_Click(object sender, RoutedEventArgs e)
    {
        ShipmentsGrid.IsLoading = !ShipmentsGrid.IsLoading;
        StatusText.Text = ShipmentsGrid.IsLoading ? "Estado de carregamento ativo." : "Carregamento concluído.";
    }

    private void ToggleEmpty_Click(object sender, RoutedEventArgs e)
    {
        if (Rows.Count > 0)
        {
            emptyStateBackup.Clear();
            emptyStateBackup.AddRange(Rows);
            Rows.Clear();
        }
        else
        {
            foreach (var row in emptyStateBackup) Rows.Add(row);
            emptyStateBackup.Clear();
        }
    }

    private void ToggleError_Click(object sender, RoutedEventArgs e)
    {
        ShipmentsGrid.ErrorMessage = string.IsNullOrEmpty(ShipmentsGrid.ErrorMessage)
            ? "Não foi possível carregar as inspeções. Tente novamente."
            : null;
    }

    private void OpenTabsDemo_Click(object sender, RoutedEventArgs e) => new MainWindow { Owner = this }.Show();
}

public sealed record ShipmentRow(string Shipment, string Origin, string Vehicle, int Quantity, int Inspected, string Status)
{
    public string Destination => Origin switch
    {
        "SP" => "Centro de Distribuição SP",
        "MG" => "Unidade Contagem",
        "PR" => "Terminal Curitiba",
        "RJ" => "Porto do Rio",
        "BA" => "Unidade Salvador",
        _ => "Terminal Regional"
    };

    public string Dock => $"D-{Quantity % 12 + 1:00}";
    public string Inspector => $"Inspetor {Inspected % 7 + 1:00}";
    public DateTime UpdatedAt => new(2026, 9, Quantity % 28 + 1, 8 + Quantity % 10, Inspected % 60, 0);
    public string Notes => Status == "Pendente" ? "Aguardando início da inspeção" : "Conferência registrada no sistema";
}
