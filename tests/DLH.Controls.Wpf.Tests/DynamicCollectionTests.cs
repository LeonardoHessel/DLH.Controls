using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Tabs = DLH.Controls.Wpf.TabControl;

internal static partial class DragDropTests
{
    private static void RunDynamicCollectionTests(Action<string, Action> test)
    {
        test("collections/add, move, remove and clear", () =>
        {
            using var f = new Fixture();
            var selected = f.Model.Items[1]; f.Model.Selected = selected; f.Layout();
            var added = new Document("Adicionado"); f.Model.Items.Add(added); f.Layout();
            Assert(f.Tabs.Items.Contains(added), "Added document did not appear");
            f.Model.Items.Move(4, 0); f.Layout();
            Assert(ReferenceEquals(f.Tabs.Items[0], added), "External move was not reflected");
            // Raw source moves follow WPF's selection policy. Component-initiated moves preserve identity.
            f.Model.Selected = selected; f.Layout();
            Assert(f.Tabs.MoveTab(f.Model.Items.IndexOf(selected), 0) && ReferenceEquals(f.Model.Selected, selected), "Component move changed selection");
            f.Model.Items.Remove(selected); f.Layout();
            Assert(!f.Tabs.Items.Contains(selected) && !ReferenceEquals(f.Model.Selected, selected), "External removal kept obsolete selection");
            Assert(f.Model.Selected is null || f.Model.Items.Contains(f.Model.Selected), "External removal selected an object outside the source");
            f.Model.Items.Clear(); f.Layout();
            Assert(f.Tabs.Items.Count == 0 && f.Tabs.SelectedItem is null, "Clear left items or selection");
        });
        test("collections/replace source while dragging", () =>
        {
            using var f = new Fixture(); var old = f.Model.Items.ToArray(); f.Start();
            var replacement = new ObservableCollection<Document> { new("Nova A"), new("Nova B") };
            f.Tabs.ItemsSource = replacement; f.Layout(); f.Clean();
            Assert(f.Tabs.Items.Count == 2 && ReferenceEquals(f.Tabs.Items[0], replacement[0]), "Replacement source was not applied");
            Assert(old.Length == 4, "Old source was mutated");
        });
        test("collections/mutation while dragging cancels session", () =>
        {
            foreach (var action in new[] { NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Move, NotifyCollectionChangedAction.Reset })
            {
                using var f = new Fixture(); f.Start();
                if (action == NotifyCollectionChangedAction.Add) f.Model.Items.Add(new("Novo"));
                else if (action == NotifyCollectionChangedAction.Remove) f.Model.Items.RemoveAt(2);
                else if (action == NotifyCollectionChangedAction.Move) f.Model.Items.Move(2, 3);
                else f.Model.Items.Clear();
                f.Layout(); f.Clean();
            }
        });
        test("collections/same persistent key is a different object", () =>
        {
            using var f = new Fixture();
            var selected = f.Model.Items[1]; f.Model.Selected = selected; f.Layout();
            var replacement = new Document(selected.Title);
            f.Model.Items[1] = replacement; f.Layout();
            Assert(!ReferenceEquals(f.Tabs.SelectedItem, replacement), "A matching title incorrectly preserved object selection");
            Assert(!f.Model.Items.Contains(selected), "Old object remained in source");
        });
        test("collections/shared source has independent selections", () =>
        {
            var documents = new ObservableCollection<Document>(Enumerable.Range(0, 4).Select(i => new Document($"Documento {i}")));
            var first = new Tabs { ItemsSource = documents, SelectedItem = documents[0] };
            var second = new Tabs { ItemsSource = documents, SelectedItem = documents[2], TabStripPlacement = Dock.Left };
            var root = new Grid(); root.ColumnDefinitions.Add(new()); root.ColumnDefinitions.Add(new());
            root.Children.Add(first); Grid.SetColumn(second, 1); root.Children.Add(second);
            var window = new Window { Content = root };
            try
            {
                root.Measure(new Size(900, 400)); root.Arrange(new Rect(0, 0, 900, 400)); root.UpdateLayout(); Pump();
                first.SelectedItem = documents[1];
                Assert(ReferenceEquals(second.SelectedItem, documents[2]), "Selection leaked between controls");
                var selectedFirst = first.SelectedItem; var selectedSecond = second.SelectedItem;
                documents.Move(3, 0); root.UpdateLayout(); Pump();
                Assert(ReferenceEquals(first.SelectedItem, selectedFirst) && ReferenceEquals(second.SelectedItem, selectedSecond), "Shared move lost selections");
                Assert(ReferenceEquals(first.Items[0], documents[0]) && ReferenceEquals(second.Items[0], documents[0]), "Shared order diverged");
                documents.Remove((Document)selectedFirst); root.UpdateLayout(); Pump();
                Assert(!ReferenceEquals(first.SelectedItem, selectedFirst) && ReferenceEquals(second.SelectedItem, selectedSecond), "Shared removal affected unrelated selection");
                Assert(!ReferenceEquals(first.ItemContainerGenerator.ContainerFromIndex(0), second.ItemContainerGenerator.ContainerFromIndex(0)), "Controls shared a visual container");
            }
            finally { window.Close(); Pump(); }
        });
        test("collections/external changes do not raise close events", () =>
        {
            using var f = new Fixture(); var closing = 0; var closed = 0;
            f.Tabs.TabClosing += (_, _) => closing++; f.Tabs.TabClosed += (_, _) => closed++;
            f.Model.Items.RemoveAt(0); f.Model.Items.Clear(); f.Layout();
            Assert(closing == 0 && closed == 0, "External mutation raised component close events");
        });
    }
}

