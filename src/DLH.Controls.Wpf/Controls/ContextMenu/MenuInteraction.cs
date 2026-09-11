using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

/// <summary>
/// Coordinates the active branch of a menu tree and allows other components
/// to close all or part of that tree.
/// </summary>
public static class MenuInteraction
{
    static MenuInteraction()
    {
        EventManager.RegisterClassHandler(typeof(MenuItem), UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnAnyMenuItemPreviewMouseDown), true);
    }

    public static readonly DependencyProperty IsScopeRootProperty = DependencyProperty.RegisterAttached(
        "IsScopeRoot", typeof(bool), typeof(MenuInteraction), new FrameworkPropertyMetadata(false));

    public static bool GetIsScopeRoot(DependencyObject element) => (bool)element.GetValue(IsScopeRootProperty);

    public static void SetIsScopeRoot(DependencyObject element, bool value) => element.SetValue(IsScopeRootProperty, value);

    /// <summary>Keeps the anchor and its ancestors active and closes every other branch.</summary>
    public static void ActivatePath(DependencyObject scopeRoot, MenuItem anchor) =>
        CloseExcept(scopeRoot, BuildPath(anchor, includeAnchor: true));

    /// <summary>Keeps only the anchor's ancestors and closes the anchor and all later branches.</summary>
    public static void CollapseAfter(DependencyObject scopeRoot, MenuItem anchor) =>
        CloseExcept(scopeRoot, BuildPath(anchor, includeAnchor: false));

    /// <summary>Closes every submenu in the supplied scope.</summary>
    public static void CollapseAll(DependencyObject scopeRoot) => CloseExcept(scopeRoot, []);

    internal static void ActivatePathFor(MenuItem anchor)
    {
        if (FindScopeRoot(anchor) is { } scopeRoot) ActivatePath(scopeRoot, anchor);
    }

    private static void OnAnyMenuItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && sender is MenuItem clickedItem)
            ActivatePathFor(clickedItem);
    }

    private static HashSet<MenuItem> BuildPath(MenuItem anchor, bool includeAnchor)
    {
        var path = new HashSet<MenuItem>();
        MenuItem? current = includeAnchor ? anchor : ParentMenuItem(anchor);
        while (current is not null)
        {
            path.Add(current);
            current = ParentMenuItem(current);
        }
        return path;
    }

    private static MenuItem? ParentMenuItem(MenuItem item) =>
        ItemsControl.ItemsControlFromItemContainer(item) as MenuItem;

    private static void CloseExcept(DependencyObject scopeRoot, HashSet<MenuItem> retainedPath)
    {
        foreach (var item in EnumerateMenuItems(scopeRoot).Distinct())
            if (item.IsSubmenuOpen && !retainedPath.Contains(item))
                item.SetCurrentValue(MenuItem.IsSubmenuOpenProperty, false);
    }

    private static IEnumerable<MenuItem> EnumerateMenuItems(DependencyObject scopeRoot)
    {
        if (scopeRoot is ItemsControl itemsControl)
        {
            foreach (var item in EnumerateItems(itemsControl)) yield return item;
            yield break;
        }

        foreach (var item in VisualDescendants(scopeRoot).OfType<MenuItem>()
                     .Where(item => FindScopeRoot(item) == scopeRoot))
        {
            yield return item;
            foreach (var descendant in EnumerateItems(item)) yield return descendant;
        }
    }

    private static IEnumerable<MenuItem> EnumerateItems(ItemsControl owner)
    {
        for (var index = 0; index < owner.Items.Count; index++)
        {
            var item = owner.Items[index] as MenuItem ?? owner.ItemContainerGenerator.ContainerFromIndex(index) as MenuItem;
            if (item is null) continue;
            yield return item;
            foreach (var descendant in EnumerateItems(item)) yield return descendant;
        }
    }

    private static DependencyObject? FindScopeRoot(MenuItem item)
    {
        DependencyObject? current = item;
        while (current is not null)
        {
            if (GetIsScopeRoot(current)) return current;
            if (current is MenuItem menuItem && ItemsControl.ItemsControlFromItemContainer(menuItem) is { } owner)
            {
                current = owner;
                continue;
            }
            current = GetParent(current);
        }
        return null;
    }

    private static DependencyObject? GetParent(DependencyObject source)
    {
        if (source is Visual or System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(source) ?? LogicalTreeHelper.GetParent(source);
        return LogicalTreeHelper.GetParent(source);
    }

    private static IEnumerable<DependencyObject> VisualDescendants(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in VisualDescendants(child)) yield return descendant;
        }
    }
}
