using System.Windows;
using System.Windows.Controls;
using DLH.Controls.Wpf.Demo;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("DataGridView")]
[TestCategory("Demo")]
public sealed class DataGridViewScrollSettingsTests
{
    [STATestMethod]
    public void SettingsApplyImmediatelyAndRestoreComponentDefaults()
    {
        var grid = new DLH.Controls.Wpf.DataGridView
        {
            HorizontalMouseWheelScrollAmount = 60,
            VerticalMouseWheelScrollAmount = 84,
            HorizontalScrollAnimationDuration = TimeSpan.FromMilliseconds(210),
            VerticalScrollAnimationDuration = TimeSpan.FromMilliseconds(230)
        };
        var window = new DataGridViewScrollSettingsWindow(grid);
        window.Show();
        try
        {
            var smoothVertical = (CheckBox)window.FindName("SmoothVertical");
            var verticalAmount = (Slider)window.FindName("VerticalAmount");
            var horizontalDuration = (Slider)window.FindName("HorizontalDuration");
            var verticalDuration = (Slider)window.FindName("VerticalDuration");
            Assert.AreEqual(84d, verticalAmount.Value);
            Assert.AreEqual(210d, horizontalDuration.Value);
            Assert.AreEqual(230d, verticalDuration.Value);

            smoothVertical.IsChecked = false;
            verticalAmount.Value = 96;
            verticalDuration.Value = 180;
            Assert.IsFalse(grid.IsSmoothVerticalScrollingEnabled);
            Assert.AreEqual(96d, grid.VerticalMouseWheelScrollAmount);
            Assert.AreEqual(TimeSpan.FromMilliseconds(180), grid.VerticalScrollAnimationDuration);

            ((Button)window.FindName("RestoreDefaults")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.IsTrue(grid.IsSmoothHorizontalScrollingEnabled);
            Assert.IsTrue(grid.IsSmoothVerticalScrollingEnabled);
            Assert.AreEqual(48d, grid.HorizontalMouseWheelScrollAmount);
            Assert.AreEqual(72d, grid.VerticalMouseWheelScrollAmount);
            Assert.AreEqual(TimeSpan.FromMilliseconds(260), grid.HorizontalScrollAnimationDuration);
            Assert.AreEqual(TimeSpan.FromMilliseconds(260), grid.VerticalScrollAnimationDuration);
        }
        finally { window.Close(); }
    }
}
