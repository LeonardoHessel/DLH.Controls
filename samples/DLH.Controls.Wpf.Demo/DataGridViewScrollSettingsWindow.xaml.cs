using System.Windows;

namespace DLH.Controls.Wpf.Demo;

public partial class DataGridViewScrollSettingsWindow : Window
{
    private readonly DLH.Controls.Wpf.DataGridView grid;
    private bool initializing;

    public DataGridViewScrollSettingsWindow(DLH.Controls.Wpf.DataGridView grid)
    {
        this.grid = grid;
        initializing = true;
        InitializeComponent();
        ReadFromGrid();
        initializing = false;
    }

    private void ReadFromGrid()
    {
        SmoothHorizontal.IsChecked = grid.IsSmoothHorizontalScrollingEnabled;
        SmoothVertical.IsChecked = grid.IsSmoothVerticalScrollingEnabled;
        HorizontalAmount.Value = grid.HorizontalMouseWheelScrollAmount;
        VerticalAmount.Value = grid.VerticalMouseWheelScrollAmount;
        HorizontalDuration.Value = grid.HorizontalScrollAnimationDuration.TotalMilliseconds;
        VerticalDuration.Value = grid.VerticalScrollAnimationDuration.TotalMilliseconds;
    }

    private void OptionChanged(object sender, RoutedEventArgs e)
    {
        if (initializing || !IsInitialized) return;
        grid.IsSmoothHorizontalScrollingEnabled = SmoothHorizontal.IsChecked == true;
        grid.IsSmoothVerticalScrollingEnabled = SmoothVertical.IsChecked == true;
        grid.HorizontalMouseWheelScrollAmount = HorizontalAmount.Value;
        grid.VerticalMouseWheelScrollAmount = VerticalAmount.Value;
        grid.HorizontalScrollAnimationDuration = TimeSpan.FromMilliseconds(HorizontalDuration.Value);
        grid.VerticalScrollAnimationDuration = TimeSpan.FromMilliseconds(VerticalDuration.Value);
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        initializing = true;
        SmoothHorizontal.IsChecked = true;
        SmoothVertical.IsChecked = true;
        HorizontalAmount.Value = 48;
        VerticalAmount.Value = 72;
        HorizontalDuration.Value = 260;
        VerticalDuration.Value = 260;
        initializing = false;
        OptionChanged(sender, e);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
