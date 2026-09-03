using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CustomTabControl.Demo;
using CustomTabControl.Controls;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var app = new Application();
        var window = new MainWindow();
        var vm = (DemoViewModel)window.DataContext;
        var tabs = (CustomTabControl.Controls.CustomTabControl)window.FindName("DynamicTabs");
        tabs.HeaderIndent = double.NaN;
        void Layout()
        {
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(1080, 760));
            root.Arrange(new Rect(0, 0, 1080, 760));
            root.UpdateLayout();
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            root.UpdateLayout();
        }
        void Check(bool condition, string label)
        {
            if (!condition) throw new Exception(label);
            Console.WriteLine("PASS: " + label);
        }
        Layout();
        void CheckUnifiedSurface()
        {
            var path = (System.Windows.Shapes.Path)tabs.Template.FindName("PART_Surface", tabs);
            var body = (FrameworkElement)tabs.Template.FindName("PART_Body", tabs);
            var selected = (TabItem)tabs.ItemContainerGenerator.ContainerFromItem(tabs.SelectedItem);
            var header = (FrameworkElement)selected.Template.FindName("Surface", selected);
            var headerBounds = header.TransformToVisual(path).TransformBounds(new Rect(header.RenderSize));
            var bodyBounds = body.TransformToVisual(path).TransformBounds(new Rect(body.RenderSize));
            var junction = new Point(headerBounds.Left + headerBounds.Width / 2, bodyBounds.Top);
            Check(path.Data.FillContains(junction) && !path.Data.StrokeContains(new Pen(Brushes.Black, 2), junction), "Unified outline has no internal border at selected tab junction");
            Check(path.Data.GetFlattenedPathGeometry().Figures.Count == 1, "Tab and body form one closed contour");
            var radius = tabs.CornerRadius.TopLeft;
            if (radius > 0)
            {
                var inset = radius * (1 - Math.Sqrt(0.5));
                var leftArc = new Point(headerBounds.Left - inset, bodyBounds.Top - inset);
                var rightArc = new Point(headerBounds.Right + inset, bodyBounds.Top - inset);
                Check(path.Data.StrokeContains(new Pen(Brushes.Black, 0.2), leftArc) &&
                      path.Data.StrokeContains(new Pen(Brushes.Black, 0.2), rightArc), "Concave joins use the configured radius");
            }
        }
        CheckUnifiedSurface();
        vm.SelectedTab = vm.Tabs[1];
        Layout();
        CheckUnifiedSurface();
        vm.SelectedTab = vm.Tabs[0];
        Layout();
        foreach (var radius in new[] { 0d, 8d, 16d })
        {
            tabs.CornerRadius = new CornerRadius(radius);
            Layout();
            Layout();
            CheckUnifiedSurface();
        }
        tabs.CornerRadius = new CornerRadius(12);
        Layout();
        Layout();
        foreach (var indent in new[] { 0d, 4d, 24d })
        {
            tabs.HeaderIndent = indent;
            Layout();
            Layout();
            var outline = (System.Windows.Shapes.Path)tabs.Template.FindName("PART_Surface", tabs);
            var panel = (FrameworkElement)tabs.Template.FindName("PART_Body", tabs);
            var item = (TabItem)tabs.ItemContainerGenerator.ContainerFromIndex(0);
            var face = (FrameworkElement)item.Template.FindName("Surface", item);
            var tabBounds = face.TransformToVisual(outline).TransformBounds(new Rect(face.RenderSize));
            var panelBounds = panel.TransformToVisual(outline).TransformBounds(new Rect(panel.RenderSize));
            Check(Math.Abs(tabBounds.Left - panelBounds.Left - indent) < 0.01, "Configurable first-tab indent");
            Check(outline.Data.GetFlattenedPathGeometry().Figures.Count == 1, "Inset keeps one contour");
            if (indent == 0)
                Check(outline.Data.FillContains(new Point(panelBounds.Left + 0.5, panelBounds.Top - 0.5)) &&
                      outline.Data.FillContains(new Point(panelBounds.Left + 0.5, panelBounds.Top + 0.5)), "Flush left edge remains continuous across junction");
        }
        tabs.HeaderIndent = double.NaN;
        Layout();
        Layout();
        Check(tabs.ItemContainerGenerator.ContainerFromIndex(0) is CustomTabItem, "Custom container generation");
        Check(tabs.Template.FindName("PART_SelectedContentHost", tabs) is ContentPresenter { Content: TabDocument }, "Content template loaded");
        Check(tabs.ItemContainerGenerator.ContainerFromIndex(2) is TabItem { IsEnabled: false }, "Disabled item binding");
        var first = vm.Tabs[0];
        first.Notes = "Preserved note";
        for (int i = 0; i < 15; i++) vm.AddTabCommand.Execute(null);
        Layout();
        Check(tabs.SelectedItem == vm.SelectedTab && tabs.Items.Count == 18, "Dynamic addition and selection binding");
        var scroll = (ScrollViewer)tabs.Template.FindName("PART_HeaderScrollViewer", tabs);
        Check(scroll.ScrollableWidth > 0 && scroll.HorizontalOffset > 0, "Selected tab revealed in overflow");
        vm.RemoveTabCommand.Execute(null);
        Layout();
        Check(tabs.Items.Count == 17 && tabs.SelectedItem == vm.SelectedTab, "Removal updates selection");
        vm.SelectedTab = first;
        Layout();
        Check(first.Notes == "Preserved note" && scroll.HorizontalOffset < 1, "Model content preserved and first tab revealed");
        while (vm.Tabs.Count > 3) vm.Tabs.RemoveAt(vm.Tabs.Count - 1);
        Layout();
        var firstHeader = (TabItem)tabs.ItemContainerGenerator.ContainerFromIndex(0);
        var secondHeader = (TabItem)tabs.ItemContainerGenerator.ContainerFromIndex(1);
        var firstFace = (FrameworkElement)firstHeader.Template.FindName("Surface", firstHeader);
        var secondFace = (FrameworkElement)secondHeader.Template.FindName("Surface", secondHeader);
        var firstBounds = firstFace.TransformToVisual(tabs).TransformBounds(new Rect(firstFace.RenderSize));
        var secondBounds = secondFace.TransformToVisual(tabs).TransformBounds(new Rect(secondFace.RenderSize));
        Check(Math.Abs(firstBounds.Right - secondBounds.Left) < 0.01, "Headers have no gap");
        vm.SelectedTab = vm.Tabs[1];
        Layout();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(CustomTabControl.Controls.CustomTabControl).GetField("hoveredItem", flags)!.SetValue(tabs, firstHeader);
        typeof(CustomTabControl.Controls.CustomTabControl).GetMethod("UpdateSurface", flags)!.Invoke(tabs, null);
        Layout();
        var hoverPath = (System.Windows.Shapes.Path)tabs.Template.FindName("PART_HoverSurface", tabs);
        Check(!hoverPath.Data.IsEmpty() && hoverPath.Data.GetFlattenedPathGeometry().Figures.Count == 1, "Hover uses one continuous curved silhouette");
        if (args.Length > 0)
        {
            var bitmap = new RenderTargetBitmap(1080, 760, 96, 96, PixelFormats.Pbgra32);
            var backdrop = new DrawingVisual();
            using (var drawing = backdrop.RenderOpen()) drawing.DrawRectangle(window.Background, null, new Rect(0, 0, 1080, 760));
            bitmap.Render(backdrop);
            bitmap.Render((Visual)window.Content);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(args[0]);
            encoder.Save(file);
        }
                IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match) yield return match;
                foreach (var descendant in Descendants<T>(child)) yield return descendant;
            }
        }
        var themeButton = Descendants<Button>((Visual)window.Content).First(button => Equals(button.Content, "Alternar tema"));
        themeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout();
        Check(((SolidColorBrush)tabs.Background).Color == (Color)ColorConverter.ConvertFromString("#FFFFFF"), "Live theme replacement");
        var rootElement = (FrameworkElement)window.Content;
        rootElement.Measure(new Size(600, 560));
        rootElement.Arrange(new Rect(0, 0, 600, 560));
        rootElement.UpdateLayout();
        Check(tabs.ActualWidth <= 600 && tabs.ActualHeight > 0, "Compact layout");
        vm.Tabs.Clear();
        Layout();
        Check(tabs.SelectedIndex == -1, "Empty collection");
        window.Close();
        app.Shutdown();
    }
}







