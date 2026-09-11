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
        MenuDetailsValue.IsChecked = true; MenuChoiceDirection.SelectedIndex = 0; MenuChoiceWrap.IsChecked = true;
        MenuChoiceArrowWidth.Value = 24; MenuChoiceIndex.Value = 0; MenuSubmenuDirection.SelectedIndex = 0;
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
            DemoMenu.ArrowColumnWidth = MenuChoiceArrowWidth.Value;
            DemoMenu.DisabledOpacity = MenuDisabledOpacity.Value;
            DemoMenu.IsShadowEnabled = MenuShadow.IsChecked == true;
            DemoMenu.ShadowOpacity = MenuShadowOpacity.Value;
            DemoMenu.ShadowBlurRadius = MenuShadowBlur.Value;
            DemoMenu.ShadowDepth = MenuShadowDepth.Value;
            DemoMenu.SubmenuPlacementDirection = MenuSubmenuDirection.SelectedIndex == 1
                ? SubmenuPlacementDirection.Left
                : SubmenuPlacementDirection.Right;
            DetailsToggleItem.IsChecked = MenuDetailsValue.IsChecked == true;
            ThemeChoiceItem.CycleDirection = MenuChoiceDirection.SelectedIndex == 1 ? ChoiceCycleDirection.Backward : ChoiceCycleDirection.Forward;
            ThemeChoiceItem.IsCycleWrappingEnabled = MenuChoiceWrap.IsChecked == true;
            ThemeChoiceItem.DropDownButtonWidth = MenuChoiceArrowWidth.Value;
            ThemeChoiceItem.SelectedIndex = (int)MenuChoiceIndex.Value;
            PersistentDetailsToggleItem.IsChecked = MenuDetailsValue.IsChecked == true;
            PersistentThemeChoiceItem.CycleDirection = ThemeChoiceItem.CycleDirection;
            PersistentThemeChoiceItem.IsCycleWrappingEnabled = ThemeChoiceItem.IsCycleWrappingEnabled;
            PersistentThemeChoiceItem.DropDownButtonWidth = ThemeChoiceItem.DropDownButtonWidth;
            PersistentThemeChoiceItem.SelectedIndex = ThemeChoiceItem.SelectedIndex;
            MenuItemAssist.SetSubmenuPlacementDirection(PersistentMenuSurface, DemoMenu.SubmenuPlacementDirection);

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

            ApplyPersistentMenuAppearance();

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

    private void ApplyPersistentMenuAppearance()
    {
        PersistentMenuSurface.Background = DemoMenu.Background;
        PersistentMenuSurface.BorderBrush = DemoMenu.BorderBrush;
        PersistentMenuSurface.BorderThickness = DemoMenu.BorderThickness;
        PersistentMenuSurface.CornerRadius = DemoMenu.CornerRadius;
        PersistentMenuSurface.Padding = DemoMenu.Padding;
        PersistentMenuSurface.Resources["ContextMenu.Surface"] = DemoMenu.Background;
        PersistentMenuSurface.Resources["ContextMenu.Text"] = DemoMenu.Foreground;
        PersistentMenuSurface.Resources["ContextMenu.Edge"] = DemoMenu.BorderBrush;
        PersistentMenuSurface.Resources["ContextMenu.Hover"] = DemoMenu.HoverBrush;
        PersistentMenuSurface.Resources["ContextMenu.Checked"] = DemoMenu.CheckedBrush;
        PersistentMenuSurface.Resources["ContextMenu.Separator"] = DemoMenu.SeparatorBrush;
        PersistentMenuSurface.Resources["ContextMenu.DisabledOpacity"] = DemoMenu.DisabledOpacity;
        PersistentMenuSurface.Resources["ContextMenu.ItemPadding"] = DemoMenu.ItemPadding;
        PersistentMenuSurface.Resources["ContextMenu.IconSize"] = DemoMenu.IconSize;
        PersistentMenuSurface.Resources["ContextMenu.IconColumnWidth"] = new GridLength(DemoMenu.IconColumnWidth);
        PersistentMenuSurface.Resources["ContextMenu.ArrowColumnWidth"] = new GridLength(DemoMenu.ArrowColumnWidth);
        PersistentMenuSurface.Resources["ContextMenu.IconAreaWidth"] = new GridLength(DemoMenu.IconColumnWidth + DemoMenu.ItemPadding.Left);
        PersistentMenuSurface.Resources["ContextMenu.ArrowAreaWidth"] = DemoMenu.ArrowColumnWidth + DemoMenu.ItemPadding.Right;
        PersistentMenuSurface.Resources["ContextMenu.ItemVerticalMargin"] = new Thickness(0, DemoMenu.ItemPadding.Top, 0, DemoMenu.ItemPadding.Bottom);
        PersistentMenuSurface.Resources["ContextMenu.CornerRadius"] = DemoMenu.CornerRadius;
        PersistentMenuSurface.Resources["ContextMenu.Padding"] = DemoMenu.Padding;
        PersistentMenuSurface.Resources["ContextMenu.BorderThickness"] = DemoMenu.BorderThickness;
        PersistentMenuSurface.Resources["ContextMenu.ShadowColor"] = DemoMenu.ShadowColor;
        PersistentMenuSurface.Resources["ContextMenu.ShadowOpacity"] = DemoMenu.IsShadowEnabled ? DemoMenu.ShadowOpacity : 0d;
        PersistentMenuSurface.Resources["ContextMenu.ShadowBlurRadius"] = DemoMenu.ShadowBlurRadius;
        PersistentMenuSurface.Resources["ContextMenu.ShadowDepth"] = DemoMenu.ShadowDepth;
        PersistentMenuSurface.Effect = DemoMenu.IsShadowEnabled
            ? new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = DemoMenu.ShadowColor,
                Opacity = DemoMenu.ShadowOpacity,
                BlurRadius = DemoMenu.ShadowBlurRadius,
                ShadowDepth = DemoMenu.ShadowDepth
            }
            : null;
    }

    private static System.Windows.Media.Brush ParseBrush(string value) => (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(value)!;
    private static System.Windows.Media.Color ParseColor(string value) => (System.Windows.Media.Color)ColorConverter.ConvertFromString(value);
    private static Thickness ParseThickness(string value) => (Thickness)new ThicknessConverter().ConvertFromString(null, CultureInfo.InvariantCulture, value)!;
    private static CornerRadius ParseCornerRadius(string value) => (CornerRadius)new CornerRadiusConverter().ConvertFromString(null, CultureInfo.InvariantCulture, value)!;
}
