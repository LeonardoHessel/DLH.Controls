using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DLH.Controls.Wpf.Demo;
using ControlsContextMenu = DLH.Controls.Wpf.ContextMenu;
using ControlsScrollBar = DLH.Controls.Wpf.ScrollBar;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("WPF")]
[TestCategory("Demo")]
[TestCategory("SharedControls")]
public sealed class SharedControlsDemoTests
{
    [STATestMethod]
    public void EveryColorOptionHasAVisualPicker()
    {
        var window = new SharedControlsDemoWindow();
        try
        {
            window.Show();
            window.UpdateLayout();
            var pickers = Descendants((DependencyObject)window.Content).OfType<Button>().Where(button => button.Tag is TextBox).ToList();
            Assert.HasCount(15, pickers);
            Assert.HasCount(15, pickers.Select(button => (TextBox)button.Tag).Distinct().ToList());
            Assert.IsFalse(pickers.Any(button => !Equals(button.ToolTip, "Escolher cor")));
        }
        finally { window.Close(); }
    }

    [STATestMethod]
    public void ConfigurationScreenAppliesEveryCustomOption()
    {
        var window = new SharedControlsDemoWindow();
        try
        {
            ComboBox("ScrollOrientation").SelectedIndex = 1;
            Slider("ScrollThickness").Value = 14;
            Slider("ThumbOpacity").Value = .6;
            Slider("ScrollValue").Value = 42;
            CheckBox("ScrollButtons").IsChecked = true;
            CheckBox("ScrollShadow").IsChecked = true;
            TextBox("ScrollCorner").Text = "7";
            TextBox("ScrollTrackPadding").Text = "1,2,1,2";
            TextBox("ScrollTrack").Text = "#111111";
            TextBox("ScrollThumb").Text = "#222222";
            TextBox("ScrollThumbHover").Text = "#333333";
            TextBox("ScrollThumbPressed").Text = "#444444";
            TextBox("ScrollShadowColor").Text = "#555555";
            Slider("ScrollShadowOpacity").Value = .4;
            Slider("ScrollShadowBlur").Value = 12;
            Slider("ScrollShadowDepth").Value = 2;

            TextBox("MenuCorner").Text = "9";
            TextBox("MenuPadding").Text = "5";
            TextBox("MenuItemPadding").Text = "11,8,11,8";
            TextBox("MenuBorderThickness").Text = "2";
            TextBox("MenuItemCorner").Text = "6";
            TextBox("MenuItemBorderThickness").Text = "1";
            TextBox("MenuItemHoverBorderThickness").Text = "2";
            TextBox("MenuItemCheckedBorderThickness").Text = "3";
            TextBox("MenuTitleColumn").Text = "2*";
            TextBox("MenuValueColumn").Text = "96";
            TextBox("MenuInputGestureColumn").Text = "48";
            Slider("MenuSubmenuIconColumn").Value = 28;
            TextBox("MenuSubmenuTitleColumn").Text = "128";
            TextBox("MenuSubmenuValueColumn").Text = "72";
            TextBox("MenuSubmenuInputGestureColumn").Text = "36";
            Slider("MenuSubmenuArrowColumn").Value = 22;
            Slider("MenuSubmenuHorizontalOffset").Value = 7;
            Slider("MenuSubmenuVerticalOffset").Value = -4;
            Slider("MenuIconSize").Value = 18;
            Slider("MenuIconColumn").Value = 30;
            Slider("MenuDisabledOpacity").Value = .35;
            TextBox("MenuBackground").Text = "#121212";
            TextBox("MenuForeground").Text = "#FAFAFA";
            TextBox("MenuBorder").Text = "#343434";
            TextBox("MenuHover").Text = "#454545";
            TextBox("MenuChecked").Text = "#565656";
            TextBox("MenuSeparator").Text = "#676767";
            TextBox("MenuItemBorder").Text = "#686868";
            TextBox("MenuItemHoverBorder").Text = "#696969";
            TextBox("MenuItemCheckedBorder").Text = "#6A6A6A";
            TextBox("MenuShadowColor").Text = "#787878";
            CheckBox("MenuShadow").IsChecked = false;
            Slider("MenuShadowOpacity").Value = .3;
            Slider("MenuShadowBlur").Value = 14;
            Slider("MenuShadowDepth").Value = 3;
            CheckBox("MenuDetailsValue").IsChecked = false;
            ComboBox("MenuChoiceDirection").SelectedIndex = 1;
            CheckBox("MenuChoiceWrap").IsChecked = false;
            Slider("MenuChoiceArrowWidth").Value = 32;
            Slider("MenuChoiceIndex").Value = 2;
            ComboBox("MenuSubmenuDirection").SelectedIndex = 1;

            Assert.IsTrue(window.ApplyOptions(includeTextFields: true));

            var bar = Find<ControlsScrollBar>("DemoScrollBar");
            Assert.AreEqual(System.Windows.Controls.Orientation.Horizontal, bar.Orientation);
            Assert.AreEqual(14d, bar.Thickness);
            Assert.AreEqual(new CornerRadius(7), bar.CornerRadius);
            Assert.AreEqual(new Thickness(1, 2, 1, 2), bar.TrackPadding);
            Assert.AreEqual(.6d, bar.ThumbOpacity);
            Assert.IsTrue(bar.ShowButtons && bar.IsShadowEnabled);
            Assert.AreEqual(12d, bar.ShadowBlurRadius);
            Assert.AreEqual(Color.FromRgb(0x55, 0x55, 0x55), bar.ShadowColor);

            var menu = Find<ControlsContextMenu>("DemoMenu");
            Assert.AreEqual(new CornerRadius(9), menu.CornerRadius);
            Assert.AreEqual(new Thickness(11, 8, 11, 8), menu.ItemPadding);
            Assert.AreEqual(new CornerRadius(6), menu.ItemCornerRadius);
            Assert.AreEqual(new Thickness(1), menu.ItemBorderThickness);
            Assert.AreEqual(new Thickness(2), menu.ItemHoverBorderThickness);
            Assert.AreEqual(new Thickness(3), menu.ItemCheckedBorderThickness);
            Assert.AreEqual(new GridLength(2, GridUnitType.Star), menu.TitleColumnWidth);
            Assert.AreEqual(new GridLength(96), menu.ValueColumnWidth);
            Assert.AreEqual(new GridLength(48), menu.InputGestureColumnWidth);
            Assert.AreEqual(28d, menu.SubmenuIconColumnWidth);
            Assert.AreEqual(new GridLength(128), menu.SubmenuTitleColumnWidth);
            Assert.AreEqual(new GridLength(72), menu.SubmenuValueColumnWidth);
            Assert.AreEqual(new GridLength(36), menu.SubmenuInputGestureColumnWidth);
            Assert.AreEqual(22d, menu.SubmenuArrowColumnWidth);
            Assert.AreEqual(7d, menu.SubmenuHorizontalOffset);
            Assert.AreEqual(-4d, menu.SubmenuVerticalOffset);
            Assert.AreEqual(Color.FromRgb(0x69, 0x69, 0x69), Assert.IsInstanceOfType<SolidColorBrush>(menu.ItemHoverBorderBrush).Color);
            Assert.AreEqual(18d, menu.IconSize);
            Assert.AreEqual(30d, menu.IconColumnWidth);
            Assert.AreEqual(.35d, menu.DisabledOpacity);
            Assert.IsFalse(menu.IsShadowEnabled);
            Assert.AreEqual(14d, menu.ShadowBlurRadius);
            Assert.AreEqual(Color.FromRgb(0x78, 0x78, 0x78), menu.ShadowColor);

            var fixedSurface = Find<Border>("PersistentMenuSurface");
            var fixedToggle = Find<DLH.Controls.Wpf.ToggleMenuItem>("PersistentDetailsToggleItem");
            var fixedChoice = Find<DLH.Controls.Wpf.ChoiceMenuItem>("PersistentThemeChoiceItem");
            Assert.AreEqual(new CornerRadius(9), fixedSurface.CornerRadius);
            Assert.AreEqual(Color.FromRgb(0x12, 0x12, 0x12), Assert.IsInstanceOfType<SolidColorBrush>(fixedSurface.Background).Color);
            Assert.AreEqual(Color.FromRgb(0x67, 0x67, 0x67), Assert.IsInstanceOfType<SolidColorBrush>(fixedSurface.Resources["ContextMenu.Separator"]).Color);
            Assert.AreEqual(new CornerRadius(6), fixedSurface.Resources["ContextMenu.ItemCornerRadius"]);
            Assert.AreEqual(new Thickness(2), fixedSurface.Resources["ContextMenu.ItemHoverBorderThickness"]);
            Assert.AreEqual(new GridLength(96), fixedSurface.Resources["ContextMenu.ValueColumnWidth"]);
            Assert.AreEqual(new GridLength(128), fixedSurface.Resources["ContextMenu.SubmenuTitleColumnWidth"]);
            Assert.AreEqual(7d, fixedSurface.Resources["ContextMenu.SubmenuHorizontalOffset"]);
            Assert.IsNull(fixedSurface.Effect);
            Assert.IsFalse(fixedToggle.IsChecked);
            Assert.AreEqual(DLH.Controls.Wpf.ChoiceCycleDirection.Backward, fixedChoice.CycleDirection);
            Assert.IsFalse(fixedChoice.IsCycleWrappingEnabled);
            Assert.AreEqual(32d, fixedChoice.DropDownButtonWidth);
            Assert.AreEqual(2, fixedChoice.SelectedIndex);
            Assert.AreEqual("System", fixedChoice.SelectedValue);
            Assert.AreEqual(FlowDirection.LeftToRight, fixedSurface.FlowDirection);
            Assert.AreEqual(FlowDirection.LeftToRight, menu.FlowDirection);
            Assert.AreEqual(SubmenuPlacementDirection.Left, menu.SubmenuPlacementDirection);
            Assert.AreEqual(SubmenuPlacementDirection.Left, MenuItemAssist.GetSubmenuPlacementDirection(fixedSurface));
        }
        finally { window.Close(); }

        T Find<T>(string name) where T : class => (T)window.FindName(name);
        Slider Slider(string name) => Find<Slider>(name);
        TextBox TextBox(string name) => Find<TextBox>(name);
        CheckBox CheckBox(string name) => Find<CheckBox>(name);
        ComboBox ComboBox(string name) => Find<ComboBox>(name);
    }

    [STATestMethod]
    public void InvalidTextIsReportedWithoutReplacingTheCurrentConfiguration()
    {
        var window = new SharedControlsDemoWindow();
        try
        {
            var bar = (ControlsScrollBar)window.FindName("DemoScrollBar");
            var original = bar.CornerRadius;
            ((TextBox)window.FindName("ScrollCorner")).Text = "inválido";
            Assert.IsFalse(window.ApplyOptions(includeTextFields: true));
            Assert.AreEqual(original, bar.CornerRadius);
            Assert.StartsWith("Valor inválido:", ((TextBlock)window.FindName("ValidationMessage")).Text);
        }
        finally { window.Close(); }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
