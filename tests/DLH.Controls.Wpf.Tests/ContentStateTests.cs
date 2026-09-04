using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

internal static partial class DragDropTests
{
    private static void RunContentStateTests(Action<string, Action> test)
    {
        test("content/model state persists while visual state is presenter-owned", () =>
        {
            using var f = new Fixture(); f.LayoutSize = new Size(500, 220);
            var panel = new FrameworkElementFactory(typeof(StackPanel));
            var editor = new FrameworkElementFactory(typeof(TextBox));
            editor.SetBinding(TextBox.TextProperty, new Binding(nameof(Document.Notes)) { Mode = BindingMode.TwoWay });
            var filler = new FrameworkElementFactory(typeof(Border)); filler.SetValue(FrameworkElement.HeightProperty, 900d);
            panel.AppendChild(editor); panel.AppendChild(filler);
            var scrollFactory = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollFactory.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scrollFactory.AppendChild(panel);
            var template = new DataTemplate { VisualTree = scrollFactory };
            f.Tabs.ContentTemplate = template;
            for (var i = 0; i < f.Tabs.Items.Count; i++) f.Item(i).ContentTemplate = template;
            f.Model.Selected = f.Model.Items[0]; f.Tabs.SelectedIndex = 0; f.Tabs.ApplyTemplate(); f.Tabs.InvalidateMeasure(); f.Layout();
            T? Find<T>(DependencyObject root) where T : DependencyObject
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                {
                    var child = VisualTreeHelper.GetChild(root, i);
                    if (child is T match) return match;
                    if (Find<T>(child) is { } nested) return nested;
                }
                return null;
            }
            var presenter = f.Tabs.Template.FindName("PART_SelectedContentHost", f.Tabs) as ContentPresenter ?? throw new InvalidOperationException("Selected content presenter was not created");
            var firstEditor = Find<TextBox>(presenter) ?? throw new InvalidOperationException("Content editor was not created");
            var firstScroll = Find<ScrollViewer>(presenter) ?? throw new InvalidOperationException("Content scroll viewer was not created");
            firstEditor.Text = "Estado no modelo"; firstEditor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            firstScroll.ScrollToVerticalOffset(120); f.Layout();
            f.Model.Selected = f.Model.Items[1]; f.Layout();
            var secondScroll = Find<ScrollViewer>(presenter)!;
            secondScroll.ScrollToVerticalOffset(300); f.Layout();
            f.Model.Selected = f.Model.Items[0]; f.Layout();
            var returnedEditor = Find<TextBox>(presenter)!; var returnedScroll = Find<ScrollViewer>(presenter)!;
            Assert(f.Model.Items[0].Notes == "Estado no modelo" && returnedEditor.Text == "Estado no modelo", "Model-backed content was lost");
            Assert(ReferenceEquals(firstScroll, secondScroll) && ReferenceEquals(secondScroll, returnedScroll) && returnedScroll.VerticalOffset > 250,
                "Scroll state did not follow the shared selected-content presenter");
        });
    }
}
