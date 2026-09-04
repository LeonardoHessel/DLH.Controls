using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

internal static partial class DragDropTests
{
    private static void RunAccessibilityTests(Action<string, Action> test)
    {
        test("accessibility/inherited tab and selection peers", () =>
        {
            using var f = new Fixture(); f.Model.Selected = f.Model.Items[1]; f.Layout();
            var controlPeer = UIElementAutomationPeer.CreatePeerForElement(f.Tabs) ?? new TabControlAutomationPeer(f.Tabs);
            var itemPeer = controlPeer.GetChildren()![1];
            Assert(controlPeer.GetAutomationControlType() == AutomationControlType.Tab, "Control is not exposed as a tab control");
            Assert(itemPeer.GetAutomationControlType() == AutomationControlType.TabItem, $"Header is exposed as {itemPeer.GetAutomationControlType()}");
            Assert(itemPeer.GetPattern(PatternInterface.SelectionItem) is System.Windows.Automation.Provider.ISelectionItemProvider { IsSelected: true }, "Selected state is not exposed");
        });
        test("accessibility/icon header accepts consumer name", () =>
        {
            using var f = new Fixture();
            AutomationProperties.SetName(f.Item(0), "Banco de dados");
            var controlPeer = UIElementAutomationPeer.CreatePeerForElement(f.Tabs) ?? new TabControlAutomationPeer(f.Tabs);
            var peer = controlPeer.GetChildren()![0];
            Assert(peer.GetName() == "Banco de dados", "Consumer-provided accessible name was lost");
        });
        test("accessibility/close action is contextual and unavailable when blocked", () =>
        {
            using var f = new Fixture(); f.Tabs.ShowCloseButtons = true; f.Layout();
            var item = f.Item(0); var button = (Button)item.Template.FindName("CloseButton", item);
            var peer = UIElementAutomationPeer.CreatePeerForElement(button) ?? new ButtonAutomationPeer(button);
            Assert(peer.GetName().Contains(f.Model.Items[0].Title), "Close action does not identify its tab");
            f.Tabs.CanCloseTabs = false; f.Layout();
            Assert(button.Visibility != Visibility.Visible && !DLH.Controls.Wpf.CustomTabControl.CloseTab.CanExecute(f.Model.Items[0], f.Tabs), "Blocked close remains executable");
        });
    }
}
