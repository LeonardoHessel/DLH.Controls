using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

internal static partial class DragDropTests
{
    private static void RunDynamicHeaderTests(Action<string, Action> test)
    {
        foreach (var side in new[] { Dock.Top, Dock.Bottom, Dock.Left, Dock.Right })
        {
            Fixture Create(int count = 4)
            {
                var f = new Fixture(side, count);
                var text = new FrameworkElementFactory(typeof(TextBlock));
                text.SetBinding(TextBlock.TextProperty, new Binding(nameof(Document.Title)));
                f.Tabs.ItemTemplate = new DataTemplate { VisualTree = text };
                f.Layout();
                return f;
            }
            test($"{side}/dynamic title geometry and identity", () =>
            {
                using var f = Create();
                var order = f.Model.Items.ToArray();
                f.Model.Selected = order[1]; f.Layout();
                var notifications = 0;
                f.Model.Items.CollectionChanged += (_, _) => notifications++;
                var surface = (Path)f.Tabs.Template.FindName("PART_Surface", f.Tabs);
                var previous = surface.Data;
                order[0].Title = "Documento anterior traduzido com título maior";
                order[1].Title = "Selected document with a longer translated title";
                f.Layout();
                Assert(!ReferenceEquals(previous, surface.Data), "Outline did not follow changed header bounds");
                Assert(surface.Data.IsFrozen && !surface.Data.Bounds.IsEmpty, "Invalid outline");
                order[0].Title = "A"; order[1].Title = "B";
                f.Tabs.FontSize = 22; f.Layout();
                Assert(ReferenceEquals(f.Model.Selected, order[1]) && ReferenceEquals(f.Tabs.SelectedItem, order[1]), "Selection identity changed");
                Assert(f.Model.Items.SequenceEqual(order) && notifications == 0, "Header change mutated collection");
                Assert(order[1].Notes == "Texto preservado", "Document data changed");
            });
            test($"{side}/header resize cancels stale drag preview", () =>
            {
                using var f = Create();
                var order = f.Model.Items.ToArray();
                f.Start(); f.Update(f.Position(3, true));
                order[0].Title = "A completely different and much larger header";
                f.Tabs.FontSize = 25; f.Layout();
                f.Clean();
                Assert(f.Model.Items.SequenceEqual(order), "Resize committed a reorder");
                f.Start(); f.Cancel(); f.Clean();
            });
            test($"{side}/overflow title remains reachable", () =>
            {
                using var f = Create(20);
                f.Model.Selected = f.Model.Items[18]; f.Layout();
                f.Model.Items[18].Title = "Translated selected document with a much longer title";
                f.Layout();
                f.Item(18).BringIntoView(); f.Layout();
                var bounds = f.Item(18).TransformToVisual(f.Tabs).TransformBounds(new Rect(f.Item(18).RenderSize));
                Assert(bounds.IntersectsWith(f.Viewport), "Selected header cannot be reached after translation");
                Assert(ReferenceEquals(f.Model.Selected, f.Model.Items[18]), "Overflow lost selection");
            });
        }
    }
}
