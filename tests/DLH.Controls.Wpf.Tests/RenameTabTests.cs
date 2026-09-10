using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DLH.Controls.Wpf;

internal static partial class DragDropTests
{
    private static void RunRenameTabTests(Action<string, Action> test)
    {
        test("rename/default disabled", () =>
        {
            using var f = new Fixture();
            f.Tabs.TabHeaderPath = "Title";
            Assert(!f.Tabs.CanRenameTabs && !f.Tabs.BeginRenameTab(f.Model.Items[0]) && !f.Tabs.IsRenamingTab, "Rename is enabled by default");
        });

        foreach (var side in new[] { Dock.Top, Dock.Bottom, Dock.Left, Dock.Right })
        {
            test($"{side}/rename commit and cancel", () =>
            {
                using var f = new Fixture(side); var item = f.Model.Items[0]; var renamed = 0;
                f.Tabs.CanRenameTabs = true; f.Tabs.TabHeaderPath = "Title";
                f.Tabs.TabRenamed += (_, e) => { Assert(ReferenceEquals(e.Item, item) && e.OldHeader.StartsWith("Aba") && e.NewHeader == "Editada", "Rename event data is invalid"); renamed++; };
                Assert(f.Tabs.BeginRenameTab(item), "Could not begin rename");
                var editor = (TextBox)f.Item(0).Template.FindName("HeaderEditor", f.Item(0));
                editor.Text = "Editada";
                Assert(f.Tabs.CommitTabRename() && item.Title == "Editada" && renamed == 1 && !f.Tabs.IsRenamingTab, "Rename was not committed");
                Assert(f.Tabs.BeginRenameTab(item), "Could not restart rename"); editor.Text = "Descartar";
                f.Tabs.CancelTabRename();
                Assert(item.Title == "Editada" && !f.Tabs.IsRenamingTab, "Cancel changed title or left editor active");
            });
        }

        test("rename/command owns update and veto preserves editor", () =>
        {
            using var f = new Fixture(); var item = f.Model.Items[0]; var allow = false; TabRenameRequest? received = null;
            f.Tabs.CanRenameTabs = true; f.Tabs.TabHeaderPath = "Title";
            f.Tabs.RenameTabCommand = new AddCommand(value => { received = (TabRenameRequest)value!; item.Title = received.NewHeader; }, _ => allow);
            Assert(f.Tabs.BeginRenameTab(item), "Command rename did not start");
            var editor = (TextBox)f.Item(0).Template.FindName("HeaderEditor", f.Item(0)); editor.Text = "Comando";
            Assert(!f.Tabs.CommitTabRename() && f.Tabs.IsRenamingTab && item.Title != "Comando", "CanExecute veto was ignored");
            allow = true;
            Assert(f.Tabs.CommitTabRename() && received is { NewHeader: "Comando" } && item.Title == "Comando", "Command did not receive rename request");
        });

        test("rename/events, unchanged text and structural cancellation", () =>
        {
            using var f = new Fixture(); var item = f.Model.Items[0]; var requested = 0; var renamed = 0;
            f.Tabs.CanRenameTabs = true; f.Tabs.TabHeaderPath = "Title";
            f.Tabs.TabRenameRequested += (_, e) => { requested++; if (e.NewHeader == "Bloqueada") e.Cancel = true; };
            f.Tabs.TabRenamed += (_, _) => renamed++;
            Assert(f.Tabs.BeginRenameTab(item) && f.Tabs.CommitTabRename() && requested == 0 && renamed == 0, "Unchanged title raised events");
            Assert(f.Tabs.BeginRenameTab(item), "Rename did not restart");
            var editor = (TextBox)f.Item(0).Template.FindName("HeaderEditor", f.Item(0)); editor.Text = "Bloqueada";
            Assert(!f.Tabs.CommitTabRename() && f.Tabs.IsRenamingTab && item.Title != "Bloqueada", "Rename veto was ignored");
            f.Model.Items.Remove(item); f.Layout();
            Assert(!f.Tabs.IsRenamingTab && f.Model.Items.Count == 3, "Collection mutation left rename active");
        });

        test("rename/explicit TabItem and activation options", () =>
        {
            var tabs = new DLH.Controls.Wpf.TabControl { CanRenameTabs = true };
            var item = new DLH.Controls.Wpf.TabControlItem { Header = "Original" }; tabs.Items.Add(item);
            var window = new Window { Content = tabs };
            try
            {
                tabs.Measure(new Size(500, 240)); tabs.Arrange(new Rect(0, 0, 500, 240)); tabs.UpdateLayout(); Pump();
                Assert(tabs.BeginRenameTab(item), "Explicit item could not be edited");
                var editor = (TextBox)item.Template.FindName("HeaderEditor", item); editor.Text = "Direta";
                Assert(tabs.CommitTabRename() && Equals(item.Header, "Direta"), "Explicit header was not updated");
                tabs.RenameActivation = TabRenameActivation.None;
                using var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("RenameTabTestKeyboard")
                    { Width = 1, Height = 1, WindowStyle = 0 });
                var key = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.F2) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                tabs.RaiseEvent(key);
                Assert(!tabs.IsRenamingTab, "Disabled activation accepted F2");
                tabs.RenameActivation = TabRenameActivation.F2;
                key = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.F2) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                tabs.RaiseEvent(key);
                Assert(key.Handled && tabs.IsRenamingTab, "F2 did not start rename");
                editor.Text = "Por teclado";
                key = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                editor.RaiseEvent(key);
                Assert(key.Handled && !tabs.IsRenamingTab && Equals(item.Header, "Por teclado"), "Enter did not commit rename");
                Assert(tabs.BeginRenameTab(item), "Rename did not restart for Escape"); editor.Text = "Descartar";
                key = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                editor.RaiseEvent(key);
                Assert(key.Handled && !tabs.IsRenamingTab && Equals(item.Header, "Por teclado"), "Escape did not cancel rename");
            }
            finally { window.Close(); Pump(); }
        });
    }
}

