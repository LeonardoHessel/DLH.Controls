using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

internal static partial class DragDropTests
{
    private sealed class AddCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
    {
        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged;
        public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void RunAddTabTests(Action<string, Action> test)
    {
        test("add/default is disabled and absent from documents", () =>
        {
            using var f = new Fixture();
            var button = (Button)f.Tabs.Template.FindName("PART_AddTabButton", f.Tabs);
            Assert(!f.Tabs.CanAddTabs && button.Visibility == Visibility.Collapsed, "Add action is enabled by default");
            Assert(f.Tabs.Items.Count == f.Model.Items.Count && !f.Tabs.RequestAddTab(), "Hidden add action changed documents");
        });

        foreach (var side in new[] { Dock.Top, Dock.Bottom, Dock.Left, Dock.Right })
        {
            test($"{side}/add command, parameter and placement", () =>
            {
                using var f = new Fixture(side);
                var parameter = new object(); var executions = 0;
                f.Tabs.CanAddTabs = true;
                f.Tabs.AddTabCommandParameter = parameter;
                f.Tabs.AddTabCommand = new AddCommand(value =>
                {
                    Assert(ReferenceEquals(value, parameter), "Add parameter was replaced");
                    executions++;
                    f.Model.Items.Add(new Document("Nova aba"));
                });
                f.Layout();
                var button = (Button)f.Tabs.Template.FindName("PART_AddTabButton", f.Tabs);
                var flow = (StackPanel)f.Tabs.Template.FindName("PART_HeaderFlow", f.Tabs);
                Assert(button.Visibility == Visibility.Visible && button.IsEnabled, "Add action is not available");
                Assert(flow.Orientation == (f.Vertical ? Orientation.Vertical : Orientation.Horizontal), "Add action uses wrong axis");
                var peer = UIElementAutomationPeer.CreatePeerForElement(button) ?? new ButtonAutomationPeer(button);
                ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke();
                Pump(); f.Layout();
                Assert(executions == 1 && f.Model.Items.Count == 5 && f.Tabs.Items.Count == 5, "Add command did not create exactly one document");
                Assert(!f.Tabs.Items.Cast<object>().Contains(button), "Add action became a tab item");
            });
        }

        test("add/event fallback and command precedence", () =>
        {
            using var f = new Fixture(); var events = 0; var commands = 0; var received = "";
            f.Tabs.CanAddTabs = true; f.Tabs.AddTabCommandParameter = "context";
            f.Tabs.AddTabRequested += (_, e) => { events++; received = (string)e.Parameter!; };
            Assert(f.Tabs.RequestAddTab(f.Tabs.AddTabCommandParameter) && events == 1 && received == "context", "Add request event failed");
            f.Tabs.AddTabCommand = new AddCommand(_ => commands++);
            Assert(f.Tabs.RequestAddTab("command") && commands == 1 && events == 1, "Command and event both handled one request");
        });

        test("add/can execute and accessibility", () =>
        {
            using var f = new Fixture(); var allowed = false; var executions = 0;
            var command = new AddCommand(_ => executions++, _ => allowed);
            f.Tabs.CanAddTabs = true; f.Tabs.AddTabCommand = command; f.Layout();
            var button = (Button)f.Tabs.Template.FindName("PART_AddTabButton", f.Tabs);
            Assert(!f.Tabs.RequestAddTab() && !button.IsEnabled, "CanExecute veto was ignored");
            allowed = true; command.Refresh(); Pump();
            Assert(button.IsEnabled && f.Tabs.RequestAddTab() && executions == 1, "Enabled add command was not executed");
            var peer = UIElementAutomationPeer.CreatePeerForElement(button) ?? new ButtonAutomationPeer(button);
            Assert(peer.GetName() == "Adicionar aba" && peer.GetAutomationControlType() == AutomationControlType.Button, "Add action is not accessible");
        });
    }
}
