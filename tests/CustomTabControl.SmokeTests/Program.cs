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
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        if (args.Contains("--drag-only"))
        {
            var reportIndex = Array.IndexOf(args, "--report");
            Environment.ExitCode = DragDropTests.Run(reportIndex >= 0 ? args[reportIndex + 1] : null) ? 0 : 1;
            app.Shutdown();
            return;
        }
        if (args.Contains("--settings-only"))
        {
            var demo = new MainWindow();
            var settingsRoot = (FrameworkElement)demo.Content;
            settingsRoot.Measure(new Size(1320, 820)); settingsRoot.Arrange(new Rect(0, 0, 1320, 820)); settingsRoot.UpdateLayout();
            var settingsFrame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => settingsFrame.Continue = false));
            Dispatcher.PushFrame(settingsFrame); settingsRoot.UpdateLayout();
            var models = new[] { "DynamicTabs", "SideTabs", "SimpleTabs" }.Select(name => (CustomTabControl.Controls.CustomTabControl)demo.FindName(name)).ToArray();
            foreach (var model in models)
            {
                if (!model.ShowCloseButtons || model.CaptureState().Order.Count != model.Items.Count) throw new Exception("Configuração/chaves ausentes");
            }
            IEnumerable<DependencyObject> SettingsDescendants(DependencyObject parent)
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    yield return child;
                    foreach (var descendant in SettingsDescendants(child)) yield return descendant;
                }
            }
            var closingToggle = SettingsDescendants(settingsRoot).OfType<CheckBox>().Single(box => Equals(box.Content, "Permitir exclusão de abas"));
            var removeButton = SettingsDescendants(settingsRoot).OfType<Button>().Single(button => Equals(button.Content, "Remover selecionada"));
            foreach (var enabled in new[] { false, true, false, true })
            {
                closingToggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, enabled);
                closingToggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                var toggleFrame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => toggleFrame.Continue = false));
                Dispatcher.PushFrame(toggleFrame); settingsRoot.UpdateLayout();
                if (models.Any(model => model.CanCloseTabs != enabled) || removeButton.IsEnabled != enabled) throw new Exception("Checkbox não propagou o estado");
                foreach (var model in models)
                {
                    var container = (TabItem)model.ItemContainerGenerator.ContainerFromIndex(0);
                    var closeButton = (Button)container.Template.FindName("CloseButton", container);
                    if ((closeButton.Visibility == Visibility.Visible) != enabled) throw new Exception("Visibilidade do botão de fechar incorreta");
                }
            }
            Console.WriteLine("PASS: checkbox updates all models, close buttons and toolbar through repeated toggles");
            foreach (var model in models)
            {
                using var stateStream = new MemoryStream();
                model.SaveState(stateStream);
                var previous = model.Items.Cast<object>().ToArray();
                model.MoveTab(0, 1);
                stateStream.Position = 0; model.LoadState(stateStream);
                if (!model.Items.Cast<object>().SequenceEqual(previous)) throw new Exception("Organização não restaurada: " + model.Name);
            }
            Console.WriteLine("PASS: organization roundtrip for all three models");
            foreach (var model in models)
            {
                if (model.Name == "DynamicTabs") { model.ItemsSource = ((DemoViewModel)demo.DataContext).Tabs; model.CloseTabCommand = ((DemoViewModel)demo.DataContext).CloseTabCommand; }
                var item = model.Items[0];
                var count = model.Items.Count;
                model.CanCloseTabs = false;
                if (model.RequestCloseTab(item) || model.Items.Count != count || CustomTabControl.Controls.CustomTabControl.CloseTab.CanExecute(item, model)) throw new Exception("Exclusão não bloqueada");
                model.CanCloseTabs = true;
                if (!model.RequestCloseTab(item) || model.Items.Count != count - 1) throw new Exception("Exclusão não reabilitada: " + model.Name);
            }
            Console.WriteLine("PASS: closing checkbox gate blocks API and command for all models and can be re-enabled");
            var settings = new TabSettingsWindow(models);
            var settingsFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var fields = (List<(DependencyProperty Property, FrameworkElement Editor)>)typeof(TabSettingsWindow).GetField("editors", settingsFlags)!.GetValue(settings)!;
            var radius = (TextBox)fields.Single(field => field.Property.Name == "CornerRadius").Editor;
            var opacity = (TextBox)fields.Single(field => field.Property.Name == "ShadowOpacity").Editor;
            radius.Text = "20"; opacity.Text = "2";
            void ApplySettings() => typeof(TabSettingsWindow).GetMethod("Apply", settingsFlags)!.Invoke(settings, null);
            ApplySettings();
            if (models.Any(model => model.CornerRadius.TopLeft == 20)) throw new Exception("Aplicação parcial de valores inválidos");
            opacity.Text = "0"; ApplySettings();
            if (models.Any(model => model.CornerRadius.TopLeft != 20 || model.ShadowOpacity != 0)) throw new Exception("Não aplicou a todos");
            if (models[1].TabStripPlacement != Dock.Left) throw new Exception("Posição lateral perdida");
            ((ComboBox)typeof(TabSettingsWindow).GetField("scope", settingsFlags)!.GetValue(settings)!).SelectedIndex = 2;
            radius.Text = "8"; ApplySettings();
            if (models[1].CornerRadius.TopLeft != 8 || models[0].CornerRadius.TopLeft != 20 || models[2].CornerRadius.TopLeft != 20) throw new Exception("Escopo individual incorreto");
            Console.WriteLine("PASS: settings defaults, keys for all models, validation before apply, all-model propagation, preserved placement and individual scope");
            settings.Close(); demo.Close(); app.Shutdown(); return;
        }
        var window = new MainWindow();
        var vm = (DemoViewModel)window.DataContext;
        var tabs = (CustomTabControl.Controls.CustomTabControl)window.FindName("DynamicTabs");
        tabs.HeaderIndent = double.NaN;
        void Layout()
        {
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(1320, 820));
            root.Arrange(new Rect(0, 0, 1320, 820));
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
            var bitmap = new RenderTargetBitmap(1320, 820, 96, 96, PixelFormats.Pbgra32);
            var backdrop = new DrawingVisual();
            using (var drawing = backdrop.RenderOpen()) drawing.DrawRectangle(window.Background, null, new Rect(0, 0, 1320, 820));
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
        var themeSelection = tabs.SelectedItem;
        themeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout();
        Check(((SolidColorBrush)tabs.Background).Color == (Color)ColorConverter.ConvertFromString("#5D5D5D") &&
            ((SolidColorBrush)window.Resources["Demo.Accent"]).Color == (Color)ColorConverter.ConvertFromString("#FF8A00"), "Third theme uses reference gray and orange palette");
        themeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout();
        Check(((SolidColorBrush)tabs.Background).Color == (Color)ColorConverter.ConvertFromString("#35373C") && tabs.SelectedItem == themeSelection,
            "Three-theme cycle returns to dark and preserves selected tab");
        var rootElement = (FrameworkElement)window.Content;
        rootElement.Measure(new Size(600, 560));
        rootElement.Arrange(new Rect(0, 0, 600, 560));
        rootElement.UpdateLayout();
        Check(tabs.ActualWidth <= 600 && tabs.ActualHeight > 0, "Compact layout");
        var sideTabs = (CustomTabControl.Controls.CustomTabControl)window.FindName("SideTabs");
        foreach (var side in new[] { Dock.Left, Dock.Right })
        {
            sideTabs.TabStripPlacement = side;
            Layout(); Layout();
            var sideBody = (FrameworkElement)sideTabs.Template.FindName("PART_Body", sideTabs);
            var sideSurface = (System.Windows.Shapes.Path)sideTabs.Template.FindName("PART_Surface", sideTabs);
            var sideItem = (TabItem)sideTabs.ItemContainerGenerator.ContainerFromIndex(0);
            var sideHeader = (FrameworkElement)sideItem.Template.FindName("Surface", sideItem);
            var headerBounds = sideHeader.TransformToVisual(sideTabs).TransformBounds(new Rect(sideHeader.RenderSize));
            var bodyBounds = sideBody.TransformToVisual(sideTabs).TransformBounds(new Rect(sideBody.RenderSize));
            var joint = new Point(side == Dock.Left ? bodyBounds.Left : bodyBounds.Right, headerBounds.Top + headerBounds.Height / 2);
            Check(((Image)sideItem.Header).Source is BitmapSource { PixelWidth: > 0 } &&
                sideSurface.Data.FillContains(joint) && !sideSurface.Data.StrokeContains(new Pen(Brushes.Black, 1), joint),
                $"{side} icons load and connect to one continuous surface");
        }
        sideTabs.TabStripPlacement = Dock.Left;
        Layout(); Layout();
        if (!DragDropTests.Run()) Environment.ExitCode = 1;
        var rejectedNonUniformRadius = false;
        try { tabs.CornerRadius = new CornerRadius(4, 8, 12, 16); } catch (ArgumentException) { rejectedNonUniformRadius = true; }
        Check(rejectedNonUniformRadius, "Corner radius remains uniform across the component");
        var rejectedNegativeRadius = false;
        try { tabs.CornerRadius = new CornerRadius(-1); } catch (ArgumentException) { rejectedNegativeRadius = true; }
        Check(rejectedNegativeRadius, "Negative corner radius is rejected");
        var shadowSurface = (System.Windows.Shapes.Path)tabs.Template.FindName("PART_Surface", tabs);
        var shadowEffect = (System.Windows.Media.Effects.DropShadowEffect)shadowSurface.Effect;
        Check(shadowEffect.Color == Color.FromRgb(73, 73, 73) && shadowEffect.Opacity == 0.5 && shadowEffect.BlurRadius == 10 && shadowEffect.ShadowDepth == 0,
            "Shadow defaults use reference settings with fifty percent opacity");
        tabs.ShadowColor = Colors.Black;
        tabs.ShadowOpacity = 0.4;
        tabs.ShadowBlurRadius = 18;
        tabs.ShadowDepth = 4;
        tabs.ShadowDirection = 270;
        Layout();
        shadowEffect = (System.Windows.Media.Effects.DropShadowEffect)shadowSurface.Effect;
        Check(shadowEffect.Color == Colors.Black && shadowEffect.Opacity == 0.4 && shadowEffect.BlurRadius == 18 && shadowEffect.ShadowDepth == 4 && shadowEffect.Direction == 270,
            "Shadow properties update the rendered effect");
        tabs.IsShadowEnabled = false;
        Layout();
        Check(shadowSurface.Effect is null, "Disabling shadow removes the effect");
        tabs.ShadowOpacity = 0.6;
        tabs.IsShadowEnabled = true;
        Layout();
        Check(shadowSurface.Effect is System.Windows.Media.Effects.DropShadowEffect { Opacity: 0.6 }, "Re-enabling shadow preserves current configuration");
        var rejectedOpacity = false;
        try { tabs.ShadowOpacity = 1.5; } catch (ArgumentException) { rejectedOpacity = true; }
        Check(rejectedOpacity, "Invalid shadow opacity is rejected");
        vm.Tabs.Clear();
        Layout();
        Check(tabs.SelectedIndex == -1, "Empty collection");
        window.Close();
        app.Shutdown();
    }
}
