using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DLH.Controls.Wpf.Demo;

public partial class SharedControlsDemoWindow : Window
{
    public SharedControlsDemoWindow()
    {
        InitializeComponent();
        ApplyOptions(includeTextFields: true);
    }

    private void OptionsChanged(object sender, RoutedEventArgs e) => ApplyOptions(includeTextFields: false);

    private void ApplyTextOptions_Click(object sender, RoutedEventArgs e) => ApplyOptions(includeTextFields: true);

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TextBox target }) return;
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true };
        try
        {
            var current = ParseColor(target.Text);
            dialog.Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B);
        }
        catch { }
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        target.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        ApplyOptions(includeTextFields: true);
    }

    private void OpenMenu_Click(object sender, RoutedEventArgs e)
    {
        ApplyOptions(includeTextFields: true);
        DemoMenu.PlacementTarget = OpenMenuButton;
        DemoMenu.Placement = PlacementMode.Bottom;
        DemoMenu.IsOpen = true;
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        ScrollOrientation.SelectedIndex = 0; ScrollThickness.Value = 10; ThumbOpacity.Value = .72; ScrollValue.Value = 35;
        ScrollButtons.IsChecked = false; ScrollShadow.IsChecked = false; ScrollCorner.Text = "5"; ScrollTrackPadding.Text = "0";
        ScrollTrack.Text = "#3D4046"; ScrollThumb.Text = "#686D77"; ScrollThumbHover.Text = "#8B919D"; ScrollThumbPressed.Text = "#AEB4BF";
        ScrollShadowColor.Text = "#494949"; ScrollShadowOpacity.Value = .5; ScrollShadowBlur.Value = 10; ScrollShadowDepth.Value = 0;
        MenuCorner.Text = "8"; MenuPadding.Text = "4"; MenuItemPadding.Text = "10,7,10,7"; MenuBorderThickness.Text = "1";
        MenuIconSize.Value = 16; MenuIconColumn.Value = 26; MenuDisabledOpacity.Value = .45;
        MenuBackground.Text = "#35373C"; MenuForeground.Text = "#F2F3F5"; MenuBorder.Text = "#4C5058";
        MenuHover.Text = "#454850"; MenuChecked.Text = "#9CC9FF"; MenuSeparator.Text = "#4C5058"; MenuShadowColor.Text = "#494949";
        MenuShadow.IsChecked = true; MenuShadowOpacity.Value = .5; MenuShadowBlur.Value = 10; MenuShadowDepth.Value = 0;
        ApplyOptions(includeTextFields: true);
    }

    public bool ApplyOptions(bool includeTextFields)
    {
        if (!IsInitialized || DemoScrollBar is null || DemoMenu is null) return false;
        try
        {
            DemoScrollBar.Orientation = ScrollOrientation.SelectedIndex == 1 ? Orientation.Horizontal : Orientation.Vertical;
            DemoScrollBar.Thickness = ScrollThickness.Value;
            DemoScrollBar.ThumbOpacity = ThumbOpacity.Value;
            DemoScrollBar.Value = ScrollValue.Value;
            DemoScrollBar.ShowButtons = ScrollButtons.IsChecked == true;
            DemoScrollBar.IsShadowEnabled = ScrollShadow.IsChecked == true;
            DemoScrollBar.ShadowOpacity = ScrollShadowOpacity.Value;
            DemoScrollBar.ShadowBlurRadius = ScrollShadowBlur.Value;
            DemoScrollBar.ShadowDepth = ScrollShadowDepth.Value;
            DemoScrollBar.HorizontalAlignment = DemoScrollBar.Orientation == Orientation.Vertical ? HorizontalAlignment.Right : HorizontalAlignment.Stretch;
            DemoScrollBar.VerticalAlignment = DemoScrollBar.Orientation == Orientation.Vertical ? VerticalAlignment.Stretch : VerticalAlignment.Bottom;

            DemoMenu.IconSize = MenuIconSize.Value;
            DemoMenu.IconColumnWidth = MenuIconColumn.Value;
            DemoMenu.DisabledOpacity = MenuDisabledOpacity.Value;
            DemoMenu.IsShadowEnabled = MenuShadow.IsChecked == true;
            DemoMenu.ShadowOpacity = MenuShadowOpacity.Value;
            DemoMenu.ShadowBlurRadius = MenuShadowBlur.Value;
            DemoMenu.ShadowDepth = MenuShadowDepth.Value;

            if (includeTextFields)
            {
                DemoScrollBar.CornerRadius = ParseCornerRadius(ScrollCorner.Text);
                DemoScrollBar.TrackPadding = ParseThickness(ScrollTrackPadding.Text);
                DemoScrollBar.TrackBrush = ParseBrush(ScrollTrack.Text);
                DemoScrollBar.ThumbBrush = ParseBrush(ScrollThumb.Text);
                DemoScrollBar.ThumbHoverBrush = ParseBrush(ScrollThumbHover.Text);
                DemoScrollBar.ThumbPressedBrush = ParseBrush(ScrollThumbPressed.Text);
                DemoScrollBar.ShadowColor = ParseColor(ScrollShadowColor.Text);

                DemoMenu.CornerRadius = ParseCornerRadius(MenuCorner.Text);
                DemoMenu.Padding = ParseThickness(MenuPadding.Text);
                DemoMenu.ItemPadding = ParseThickness(MenuItemPadding.Text);
                DemoMenu.BorderThickness = ParseThickness(MenuBorderThickness.Text);
                DemoMenu.Background = ParseBrush(MenuBackground.Text);
                DemoMenu.Foreground = ParseBrush(MenuForeground.Text);
                DemoMenu.BorderBrush = ParseBrush(MenuBorder.Text);
                DemoMenu.HoverBrush = ParseBrush(MenuHover.Text);
                DemoMenu.CheckedBrush = ParseBrush(MenuChecked.Text);
                DemoMenu.SeparatorBrush = ParseBrush(MenuSeparator.Text);
                DemoMenu.ShadowColor = ParseColor(MenuShadowColor.Text);
            }

            ValidationMessage.Text = $"Aplicado: {DemoScrollBar.Orientation}, {DemoScrollBar.Thickness:0}px; menu com raio {DemoMenu.CornerRadius.TopLeft:0}.";
            ValidationMessage.Foreground = Brushes.LightSkyBlue;
            return true;
        }
        catch (Exception exception)
        {
            ValidationMessage.Text = $"Valor inválido: {exception.Message}";
            ValidationMessage.Foreground = Brushes.LightCoral;
            return false;
        }
    }

    private static System.Windows.Media.Brush ParseBrush(string value) => (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(value)!;
    private static System.Windows.Media.Color ParseColor(string value) => (System.Windows.Media.Color)ColorConverter.ConvertFromString(value);
    private static Thickness ParseThickness(string value) => (Thickness)new ThicknessConverter().ConvertFromString(null, CultureInfo.InvariantCulture, value)!;
    private static CornerRadius ParseCornerRadius(string value) => (CornerRadius)new CornerRadiusConverter().ConvertFromString(null, CultureInfo.InvariantCulture, value)!;
}
