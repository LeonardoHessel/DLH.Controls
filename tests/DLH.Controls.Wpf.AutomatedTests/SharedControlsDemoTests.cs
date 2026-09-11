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
            Assert.HasCount(12, pickers);
            Assert.HasCount(12, pickers.Select(button => (TextBox)button.Tag).Distinct().ToList());
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
            Slider("MenuIconSize").Value = 18;
            Slider("MenuIconColumn").Value = 30;
            Slider("MenuDisabledOpacity").Value = .35;
            TextBox("MenuBackground").Text = "#121212";
            TextBox("MenuForeground").Text = "#FAFAFA";
            TextBox("MenuBorder").Text = "#343434";
            TextBox("MenuHover").Text = "#454545";
            TextBox("MenuChecked").Text = "#565656";
            TextBox("MenuSeparator").Text = "#676767";
            TextBox("MenuShadowColor").Text = "#787878";
            CheckBox("MenuShadow").IsChecked = false;
            Slider("MenuShadowOpacity").Value = .3;
            Slider("MenuShadowBlur").Value = 14;
            Slider("MenuShadowDepth").Value = 3;

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
            Assert.AreEqual(18d, menu.IconSize);
            Assert.AreEqual(30d, menu.IconColumnWidth);
            Assert.AreEqual(.35d, menu.DisabledOpacity);
            Assert.IsFalse(menu.IsShadowEnabled);
            Assert.AreEqual(14d, menu.ShadowBlurRadius);
            Assert.AreEqual(Color.FromRgb(0x78, 0x78, 0x78), menu.ShadowColor);
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
