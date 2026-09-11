using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DLH.Controls.Wpf;

public partial class DataGridView
{
    private Popup? rowDetailsPopup;
    private object? rowDetailsPopupItem;

    public static readonly DependencyProperty ShowRowDetailsPopupOnClickProperty = DependencyProperty.Register(
        nameof(ShowRowDetailsPopupOnClick), typeof(bool), typeof(DataGridView),
        new PropertyMetadata(false, OnRowDetailsPopupConfigurationChanged));
    public bool ShowRowDetailsPopupOnClick
    {
        get => (bool)GetValue(ShowRowDetailsPopupOnClickProperty);
        set => SetValue(ShowRowDetailsPopupOnClickProperty, value);
    }

    public static readonly DependencyProperty RowDetailsPopupTemplateProperty = DependencyProperty.Register(
        nameof(RowDetailsPopupTemplate), typeof(DataTemplate), typeof(DataGridView),
        new PropertyMetadata(null, OnRowDetailsPopupConfigurationChanged));
    public DataTemplate? RowDetailsPopupTemplate
    {
        get => (DataTemplate?)GetValue(RowDetailsPopupTemplateProperty);
        set => SetValue(RowDetailsPopupTemplateProperty, value);
    }

    public static readonly DependencyProperty RowDetailsPopupContentStyleProperty = DependencyProperty.Register(
        nameof(RowDetailsPopupContentStyle), typeof(Style), typeof(DataGridView),
        new PropertyMetadata(null, OnRowDetailsPopupConfigurationChanged));
    public Style? RowDetailsPopupContentStyle
    {
        get => (Style?)GetValue(RowDetailsPopupContentStyleProperty);
        set => SetValue(RowDetailsPopupContentStyleProperty, value);
    }

    public static readonly DependencyProperty RowDetailsPopupPlacementProperty = DependencyProperty.Register(
        nameof(RowDetailsPopupPlacement), typeof(PlacementMode), typeof(DataGridView),
        new PropertyMetadata(PlacementMode.Bottom, OnRowDetailsPopupConfigurationChanged));
    public PlacementMode RowDetailsPopupPlacement
    {
        get => (PlacementMode)GetValue(RowDetailsPopupPlacementProperty);
        set => SetValue(RowDetailsPopupPlacementProperty, value);
    }

    public static readonly DependencyProperty RowDetailsPopupHorizontalOffsetProperty = DependencyProperty.Register(
        nameof(RowDetailsPopupHorizontalOffset), typeof(double), typeof(DataGridView),
        new PropertyMetadata(0d, OnRowDetailsPopupConfigurationChanged), IsFiniteNumber);
    public double RowDetailsPopupHorizontalOffset
    {
        get => (double)GetValue(RowDetailsPopupHorizontalOffsetProperty);
        set => SetValue(RowDetailsPopupHorizontalOffsetProperty, value);
    }

    public static readonly DependencyProperty RowDetailsPopupVerticalOffsetProperty = DependencyProperty.Register(
        nameof(RowDetailsPopupVerticalOffset), typeof(double), typeof(DataGridView),
        new PropertyMetadata(4d, OnRowDetailsPopupConfigurationChanged), IsFiniteNumber);
    public double RowDetailsPopupVerticalOffset
    {
        get => (double)GetValue(RowDetailsPopupVerticalOffsetProperty);
        set => SetValue(RowDetailsPopupVerticalOffsetProperty, value);
    }

    public bool IsRowDetailsPopupOpen => rowDetailsPopup?.IsOpen == true;
    public object? RowDetailsPopupItem => rowDetailsPopupItem;

    public void CloseRowDetailsPopup()
    {
        if (rowDetailsPopup is not null) rowDetailsPopup.IsOpen = false;
        rowDetailsPopupItem = null;
    }

    private static bool IsFiniteNumber(object value) => value is double number && double.IsFinite(number);

    private static void OnRowDetailsPopupConfigurationChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((DataGridView)owner).CloseRowDetailsPopup();

    private void HandleRowDetailsPopupClick(MouseButtonEventArgs args)
    {
        if (args.ChangedButton != MouseButton.Left) return;
        var row = FindAncestor<DataGridRow>(args.OriginalSource as DependencyObject);
        if (!ShowRowDetailsPopupOnClick || row is null)
        {
            CloseRowDetailsPopup();
            return;
        }
        if (IsRowDetailsPopupOpen && ReferenceEquals(rowDetailsPopupItem, row.Item))
        {
            CloseRowDetailsPopup();
            return;
        }

        var item = row.Item;
        Dispatcher.BeginInvoke(() => OpenRowDetailsPopup(row, item),
            System.Windows.Threading.DispatcherPriority.Input);
    }

    private void OpenRowDetailsPopup(DataGridRow row, object item)
    {
        if (!ShowRowDetailsPopupOnClick || !row.IsVisible) return;
        CloseRowDetailsPopup();
        var content = new ContentControl
        {
            Content = item,
            ContentTemplate = RowDetailsPopupTemplate ?? RowDetailsTemplate,
            Style = RowDetailsPopupContentStyle
        };
        rowDetailsPopup = new Popup
        {
            AllowsTransparency = true,
            StaysOpen = false,
            PlacementTarget = row,
            Placement = RowDetailsPopupPlacement,
            HorizontalOffset = RowDetailsPopupHorizontalOffset,
            VerticalOffset = RowDetailsPopupVerticalOffset,
            Child = content
        };
        rowDetailsPopup.Closed += (_, _) => rowDetailsPopupItem = null;
        rowDetailsPopupItem = item;
        rowDetailsPopup.IsOpen = true;
    }
}
