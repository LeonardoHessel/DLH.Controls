using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Tabs = CustomTabControl.Controls.CustomTabControl;

internal static class DragDropTests
{
    private sealed record Result(string Name, bool Passed, double Milliseconds, string? Error);
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object? Call(Tabs tabs, string name, params object?[] arguments)
    {
        try { return typeof(Tabs).GetMethod(name, Private)!.Invoke(tabs, arguments); }
        catch (TargetInvocationException e) { throw e.InnerException ?? e; }
    }
    private static T Field<T>(Tabs tabs, string name) => (T)typeof(Tabs).GetField(name, Private)!.GetValue(tabs)!;
    private static void Set(Tabs tabs, string name, object? value) => typeof(Tabs).GetField(name, Private)!.SetValue(tabs, value);
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Pump(int milliseconds = 0)
    {
        var frame = new DispatcherFrame();
        if (milliseconds == 0) Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
        else
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
        }
        Dispatcher.PushFrame(frame);
    }

    private sealed class Document(string title)
    {
        public string Title { get; } = title;
        public string Notes { get; set; } = "Texto preservado";
        public override string ToString() => Title;
    }
    private sealed class Model : INotifyPropertyChanged
    {
        public ObservableCollection<Document> Items { get; } = new();
        private Document? selected;
        public Document? Selected { get => selected; set { selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Fixture : IDisposable
    {
        public Tabs Tabs { get; }
        public Model Model { get; } = new();
        public Window Window { get; }
        public Grid Root { get; } = new();
        public Size LayoutSize { get; set; } = new Size(760, 360);
        public bool Vertical => Tabs.TabStripPlacement is Dock.Left or Dock.Right;
        public ScrollViewer Scroll => (ScrollViewer)Tabs.Template.FindName("PART_HeaderScrollViewer", Tabs);
        public Canvas Layer => (Canvas)Tabs.Template.FindName("PART_DragPreviewLayer", Tabs);
        public Rect Viewport => (Rect)Call(Tabs, "HeaderViewport")!;
        public int Target => Field<int>(Tabs, "dropIndex");
        public bool Dragging => Field<bool>(Tabs, "dragging");
        public TabItem Item(int i) => (TabItem)Tabs.ItemContainerGenerator.ContainerFromIndex(i);
        public FrameworkElement HeaderRoot(int i) => (FrameworkElement)Item(i).Template.FindName("Root", Item(i));
        public Fixture(Dock side = Dock.Top, int count = 4)
        {
            Tabs = new Tabs { TabStripPlacement = side, HeaderIndent = 0, TabSpacing = 0 };
            for (var i = 0; i < count; i++) Model.Items.Add(new Document(i % 2 == 0 ? $"Aba {i}" : $"Documento mais longo {i}"));
            Model.Selected = Model.Items.FirstOrDefault();
            Tabs.ItemsSource = Model.Items;
            Tabs.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, new Binding(nameof(Model.Selected)) { Source = Model, Mode = BindingMode.TwoWay });
            Root.Children.Add(Tabs);
            Window = new Window { Content = Root };
            Layout();
        }
        public void Layout()
        {
            for (var i = 0; i < 3; i++)
            {
                Root.Measure(LayoutSize);
                Root.Arrange(new Rect(new Point(), LayoutSize));
                Root.UpdateLayout(); Pump();
            }
        }
        public Point Position(int index, bool after = false)
        {
            var item = Item(index);
            return item.TranslatePoint(new Point(Vertical ? item.ActualWidth / 2 : after ? item.ActualWidth - 1 : 1,
                Vertical ? after ? item.ActualHeight - 1 : 1 : item.ActualHeight / 2), Tabs);
        }
        public void Start(int index = 0)
        {
            Call(Tabs, "CancelTabDrag"); Call(Tabs, "ResetDragPreview");
            Model.Selected = Model.Items[index]; Layout();
            Set(Tabs, "dragCandidate", Item(index));
            Set(Tabs, "dragOrigin", Position(index));
            Set(Tabs, "dragging", true);
            Call(Tabs, "BeginDragPreview");
        }
        public void Update(Point point, bool scroll = false) => Call(Tabs, "UpdateDropTarget", point, scroll);
        public void Drop(Point point) { Call(Tabs, "CompleteTabDrag", point); Layout(); }
        public void Cancel() { Call(Tabs, "CancelTabDrag"); Pump(240); Layout(); }
        public void Clean()
        {
            Assert(!Dragging && Field<TabItem?>(Tabs, "dragCandidate") is null, "Drag state was not cleared");
            Assert(Layer.Children.OfType<Border>().Count() == 0, "Floating preview remained");
            Assert(((Line)Tabs.Template.FindName("PART_DropIndicator", Tabs)).Visibility != Visibility.Visible, "Drop indicator remained");
            for (var i = 0; i < Tabs.Items.Count; i++)
            {
                var root = HeaderRoot(i);
                Assert(Math.Abs(root.RenderTransform.Value.OffsetX) < 0.01 && Math.Abs(root.RenderTransform.Value.OffsetY) < 0.01, "Header offset remained");
                Assert(Math.Abs(root.Opacity - 1) < 0.01, "Header opacity remained");
            }
        }
        public void Dispose()
        {
            Call(Tabs, "CancelTabDrag"); Call(Tabs, "ResetDragPreview");
            Window.Close(); Pump();
        }
    }

    public static bool Run(string? reportPath = null)
    {
        var results = new List<Result>();
        void Test(string name, Action action)
        {
            var timer = Stopwatch.StartNew();
            try { action(); results.Add(new(name, true, timer.Elapsed.TotalMilliseconds, null)); Console.WriteLine("PASS: " + name); }
            catch (Exception e) { results.Add(new(name, false, timer.Elapsed.TotalMilliseconds, e.ToString())); Console.WriteLine("FAIL: " + name + " — " + e.Message); }
        }
        foreach (var side in new[] { Dock.Top, Dock.Bottom, Dock.Left, Dock.Right })
        {
            Test($"{side}/drop both directions + MVVM", () =>
            {
                using var f = new Fixture(side);
                var first = f.Model.Items[0]; var events = new List<NotifyCollectionChangedAction>();
                f.Model.Items.CollectionChanged += (_, e) => events.Add(e.Action);
                f.Start(); f.Drop(f.Position(3, true)); f.Clean();
                Assert(ReferenceEquals(f.Model.Items[3], first) && ReferenceEquals(f.Model.Selected, first), "Forward drop lost order or selection");
                f.Start(3); f.Drop(f.Position(0)); f.Clean();
                Assert(ReferenceEquals(f.Model.Items[0], first) && first.Notes == "Texto preservado", "Reverse drop lost document state");
                Assert(events.SequenceEqual(new[] { NotifyCollectionChangedAction.Move, NotifyCollectionChangedAction.Move }), "Expected exactly one Move notification per drop");
            });
            Test($"{side}/5px threshold", () =>
            {
                using var f = new Fixture(side); Set(f.Tabs, "dragOrigin", new Point(0, 0));
                var dpi = VisualTreeHelper.GetDpi(f.Tabs);
                foreach (var distance in new[] { -5.1, -5d, 0, 5, 5.1 })
                {
                    var p = f.Vertical ? new Point(100, distance / dpi.DpiScaleY) : new Point(distance / dpi.DpiScaleX, 100);
                    Assert((bool)Call(f.Tabs, "HasPassedDragThreshold", p)! == (Math.Abs(distance) > 5), "Incorrect physical pixel threshold");
                }
            });
            Test($"{side}/axis + stable target + cancel", () =>
            {
                using var f = new Fixture(side); var order = f.Model.Items.ToArray(); f.Start();
                var target = f.Position(3, true);
                for (var i = 0; i < 8; i++)
                {
                    var p = f.Vertical ? new Point(target.X + (i % 2 == 0 ? 500 : -500), target.Y) : new Point(target.X, target.Y + (i % 2 == 0 ? 500 : -500));
                    f.Update(p); Pump(30);
                    Assert(f.Target == 3, "Target oscillated during animation or perpendicular movement");
                }
                Assert(f.Model.Items.SequenceEqual(order), "Preview modified source before drop");
                var preview = f.Layer.Children.OfType<Border>().Single();
                var offset = f.HeaderRoot(1).RenderTransform.Value;
                Assert(f.Vertical ? offset.OffsetY < 0 && offset.OffsetX == 0 : offset.OffsetX < 0 && offset.OffsetY == 0, "Animation used wrong axis");
                Assert(preview.CornerRadius == f.Tabs.CornerRadius, "Preview radius differs");
                f.Cancel(); f.Clean(); Assert(f.Model.Items.SequenceEqual(order), "Cancel changed order");
            });
            Test($"{side}/outside-axis drop", () =>
            {
                using var f = new Fixture(side); var order = f.Model.Items.ToArray(); f.Start();
                f.Drop(new Point(-100, -100)); Pump(240); f.Clean();
                Assert(f.Model.Items.SequenceEqual(order), "Invalid drop changed order");
            });
            Test($"{side}/scroll in both directions", () =>
            {
                using var f = new Fixture(side, 20); f.Start();
                var v = f.Viewport;
                var end = new Point(v.Right - 2, v.Bottom - 2);
                for (var i = 0; i < 4; i++) { f.Update(end, true); f.Layout(); }
                var maximum = f.Vertical ? f.Scroll.VerticalOffset : f.Scroll.HorizontalOffset;
                Assert(maximum > 0, "Forward auto-scroll failed");
                var start = new Point(v.Left + 2, v.Top + 2);
                f.Update(start, true); f.Layout();
                Assert((f.Vertical ? f.Scroll.VerticalOffset : f.Scroll.HorizontalOffset) < maximum, "Reverse auto-scroll failed");
                f.Cancel(); f.Clean();
            });
            Test($"{side}/resize during drag", () =>
            {
                using var f = new Fixture(side, 12); var original = f.Model.Items.ToArray(); f.Start();
                f.Update(f.Position(2, true));
                foreach (var size in new[] { new Size(480, 240), new Size(920, 500), new Size(560, 300) })
                {
                    f.LayoutSize = size; f.Layout();
                    var viewport = f.Viewport;
                    var point = new Point(viewport.Left + viewport.Width / 2, viewport.Top + viewport.Height / 2);
                    f.Update(point); Pump(40);
                    var preview = f.Layer.Children.OfType<Border>().Single();
                    var origin = f.Layer.TranslatePoint(new Point(Canvas.GetLeft(preview), Canvas.GetTop(preview)), f.Tabs);
                    Assert(f.Vertical ? Math.Abs(origin.X - viewport.Left) < 1 : Math.Abs(origin.Y - viewport.Top) < 1,
                        "Preview detached from header strip after resize");
                    Assert(f.Target >= 0 && f.Model.Items.SequenceEqual(original), "Resize corrupted destination or collection");
                }
                f.Cancel(); f.Clean();
            });
            Test($"{side}/placement change during drag cancels cleanly", () =>
            {
                using var f = new Fixture(side); var order = f.Model.Items.ToArray(); f.Start(); f.Update(f.Position(3, true)); Pump(50);
                f.Tabs.TabStripPlacement = side is Dock.Top or Dock.Bottom ? Dock.Left : Dock.Top;
                f.Layout(); Pump(250); f.Clean();
                Assert(f.Model.Items.SequenceEqual(order), "Changing placement changed order");
                f.Start(); f.Drop(f.Position(3, true)); f.Clean();
                Assert(ReferenceEquals(f.Model.Items[3], order[0]), "Drag could not restart after placement change");
            });
            Test($"{side}/source removal during drag", () =>
            {
                using var f = new Fixture(side); f.Start(); f.Model.Items.RemoveAt(0); f.Layout();
                f.Update(f.Position(1)); f.Cancel(); f.Clean();
                Assert(f.Model.Items.Count == 3, "Removing dragged item changed additional items");
            });
            Test($"{side}/rapid cancel and restart", () =>
            {
                using var f = new Fixture(side);
                for (var i = 0; i < 4; i++)
                {
                    f.Start(); f.Update(f.Position(3, true));
                    Call(f.Tabs, "CancelTabDrag"); // Restart before the return animation finishes.
                }
                f.Start(); f.Drop(f.Position(2, true)); Pump(260); f.Clean();
            });
        }
        Test("sources/explicit, mutable, readonly, filtered and sorted", () =>
        {
            using var f = new Fixture();
            var tabs = new Tabs(); var a = new TabItem { Header = "A" }; var b = new TabItem { Header = "B" };
            tabs.Items.Add(a); tabs.Items.Add(b); tabs.SelectedItem = a;
            Assert(tabs.MoveTab(0, 1) && tabs.Items[1] == a && tabs.SelectedItem == a, "Explicit items failed");
            var list = new List<string> { "A", "B" }; tabs = new Tabs { ItemsSource = list };
            Assert(tabs.MoveTab(0, 1) && Equals(tabs.Items[1], "A"), "Mutable list failed");
            tabs = new Tabs { ItemsSource = new[] { "A", "B" } }; Assert(!tabs.MoveTab(0, 1), "Array mutated");
            tabs = new Tabs { ItemsSource = new ReadOnlyObservableCollection<Document>(f.Model.Items) }; Assert(!tabs.MoveTab(0, 1), "Read-only source mutated");
            var view = CollectionViewSource.GetDefaultView(f.Model.Items);
            view.Filter = _ => true; Assert(!f.Tabs.MoveTab(0, 1), "Filtered source accepted"); view.Filter = null;
            view.SortDescriptions.Add(new SortDescription(nameof(Document.Title), ListSortDirection.Ascending));
            Assert(!f.Tabs.MoveTab(0, 1), "Sorted source accepted");
            view.SortDescriptions.Clear(); view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Document.Title)));
            Assert(!f.Tabs.MoveTab(0, 1), "Grouped source accepted");
        });
        Test("guards/disabled, invalid and no-op", () =>
        {
            using var f = new Fixture(); f.Item(0).IsEnabled = false;
            Assert(!f.Tabs.MoveTab(0, 1), "Disabled item moved"); f.Item(0).IsEnabled = true;
            Assert(!f.Tabs.MoveTab(-1, 1) && !f.Tabs.MoveTab(0, 99) && f.Tabs.MoveTab(0, 0), "Index guard failed");
            f.Start(); f.Tabs.CanReorderTabs = false; Pump(240); f.Clean();
            Assert(!f.Tabs.MoveTab(0, 1), "Disabled reorder accepted");
        });
        Test("lifecycle/cursor binding, watchdog and window deactivation", () =>
        {
            using var f = new Fixture(); var owner = new TextBlock { Tag = Cursors.Cross };
            f.Tabs.SetBinding(FrameworkElement.CursorProperty, new Binding("Tag") { Source = owner });
            f.Start(); var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; timer.Start(); Set(f.Tabs, "dragTimer", timer);
            Call(f.Tabs, "PollTabDrag"); Pump(240); f.Clean(); Assert(!timer.IsEnabled, "Timer survived lost capture");
            f.Start(); Call(f.Tabs, "DragWindowDeactivated", f.Window, EventArgs.Empty); Pump(240); f.Clean();
            Assert(BindingOperations.IsDataBound(f.Tabs, FrameworkElement.CursorProperty) && f.Tabs.Cursor == Cursors.Cross, "Original cursor binding was overwritten");
        });
        Test("lifecycle/Escape routed event", () =>
        {
            using var f = new Fixture(); var order = f.Model.Items.ToArray(); f.Start(); f.Update(f.Position(3, true));
            // Hidden presentation source only: no foreground window or system input is changed.
            using var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("DragDropTestKeyboard")
                { Width = 1, Height = 1, WindowStyle = 0 });
            var key = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Escape)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            f.Tabs.RaiseEvent(key); Pump(240); f.Clean();
            Assert(key.Handled && f.Model.Items.SequenceEqual(order), "Escape did not cancel without mutation");
        });
        Test("lifecycle/capture lost and unload routed events", () =>
        {
            using var f = new Fixture(); f.Start();
            f.Tabs.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.LostMouseCaptureEvent });
            Pump(240); f.Clean();
            f.Start(); f.Tabs.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); Pump(240); f.Clean();
        });
        Test("mutation/clear collection during animated drag", () =>
        {
            using var f = new Fixture(); f.Start(); f.Update(f.Position(3, true));
            f.Model.Items.Clear(); f.Layout();
            f.Update(new Point(50, 25)); f.Cancel(); f.Clean();
            Assert(f.Model.Items.Count == 0, "Clear was not preserved");
        });
        Test("preview/preserve existing transform and opacity", () =>
        {
            using var f = new Fixture();
            var root = f.HeaderRoot(1); var original = new ScaleTransform(0.95, 0.95);
            root.RenderTransform = original; root.Opacity = 0.7;
            f.Start(); f.Update(f.Position(3, true)); f.Cancel();
            Assert(ReferenceEquals(root.RenderTransform, original) && root.Opacity == 0.7, "Existing header appearance was lost");
        });
        var failed = results.Count(r => !r.Passed);
        Console.WriteLine($"DRAG-DROP: {results.Count - failed}/{results.Count} passed; {failed} failed.");
        if (reportPath is not null)
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new { CreatedUtc = DateTime.UtcNow, Total = results.Count, Failed = failed,
                Scope = "STA WPF integration, rendered layout and animation; simulated drag state and pointer coordinates. No physical mouse or OS input injection.", Results = results }, new JsonSerializerOptions { WriteIndented = true }));
        return failed == 0;
    }
}


