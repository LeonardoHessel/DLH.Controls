using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace DLH.Controls.Wpf.Demo.Screenshots;

public sealed record ShipmentRow(string Shipment, string Origin, string Status, string Vehicle, int Quantity,
    string Destination, string Dock, string Inspector, DateTime UpdatedAt, string Notes);

public partial class DataGridViewScreenshotWindow : Window
{
    public ObservableCollection<ShipmentRow> Rows { get; } = new(
    [
        new("EMB-2201", "Guarulhos", "Concluído", "Carreta", 420, "Curitiba", "D3", "Ana Ferreira", new DateTime(2026, 9, 10, 8, 12, 0), "Carga conferida sem divergências."),
        new("EMB-2202", "Osasco", "Pendente", "Truck", 180, "Joinville", "D1", "Bruno Alves", new DateTime(2026, 9, 10, 9, 5, 0), "Aguardando liberação fiscal."),
        new("EMB-2203", "Guarulhos", "Concluído", "Carreta", 512, "Blumenau", "D5", "Ana Ferreira", new DateTime(2026, 9, 10, 9, 40, 0), "—"),
        new("EMB-2204", "Barueri", "Em trânsito", "Van", 64, "São José", "D2", "Camila Rocha", new DateTime(2026, 9, 10, 10, 2, 0), "Rota alternativa por obras."),
        new("EMB-2205", "Osasco", "Concluído", "Truck", 233, "Curitiba", "D4", "Diego Martins", new DateTime(2026, 9, 10, 10, 30, 0), "—"),
        new("EMB-2206", "Guarulhos", "Pendente", "Carreta", 388, "Blumenau", "D3", "Bruno Alves", new DateTime(2026, 9, 10, 11, 8, 0), "Divergência de peso em conferência."),
        new("EMB-2207", "Barueri", "Em trânsito", "Van", 91, "Joinville", "D1", "Camila Rocha", new DateTime(2026, 9, 10, 11, 45, 0), "—"),
        new("EMB-2208", "Osasco", "Concluído", "Truck", 276, "São José", "D5", "Diego Martins", new DateTime(2026, 9, 10, 12, 15, 0), "Descarregamento antecipado."),
        new("EMB-2209", "Guarulhos", "Concluído", "Carreta", 455, "Curitiba", "D2", "Ana Ferreira", new DateTime(2026, 9, 10, 12, 50, 0), "—"),
        new("EMB-2210", "Barueri", "Em trânsito", "Van", 58, "Blumenau", "D4", "Bruno Alves", new DateTime(2026, 9, 10, 13, 20, 0), "—"),
        new("EMB-2211", "Osasco", "Pendente", "Truck", 199, "Joinville", "D3", "Camila Rocha", new DateTime(2026, 9, 10, 13, 55, 0), "Aguardando vaga na doca."),
        new("EMB-2212", "Guarulhos", "Concluído", "Carreta", 341, "São José", "D1", "Diego Martins", new DateTime(2026, 9, 10, 14, 30, 0), "—"),
        new("EMB-2213", "Barueri", "Concluído", "Van", 77, "Curitiba", "D5", "Ana Ferreira", new DateTime(2026, 9, 10, 15, 5, 0), "—"),
        new("EMB-2214", "Osasco", "Em trânsito", "Truck", 264, "Blumenau", "D2", "Bruno Alves", new DateTime(2026, 9, 10, 15, 40, 0), "—"),
        new("EMB-2215", "Guarulhos", "Concluído", "Carreta", 402, "Joinville", "D4", "Camila Rocha", new DateTime(2026, 9, 10, 16, 10, 0), "—"),
    ]);

    public DataGridViewScreenshotWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            Grid.PinColumn(Grid.Columns[0]);
            Grid.PinColumn(Grid.Columns[1]);
            Grid.PinRow(Rows[0]);
            var scrollViewer = (System.Windows.Controls.ScrollViewer)Grid.Template.FindName("DG_ScrollViewer", Grid)!;
            scrollViewer.ScrollToHorizontalOffset(220);
            scrollViewer.ScrollToVerticalOffset(90);
        }, DispatcherPriority.Loaded);
    }
}
