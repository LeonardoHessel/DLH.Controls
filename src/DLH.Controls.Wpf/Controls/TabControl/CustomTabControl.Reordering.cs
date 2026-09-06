using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DLH.Controls.Wpf;

public partial class CustomTabControl
{
    public static readonly DependencyProperty CanReorderTabsProperty = DependencyProperty.Register(
        nameof(CanReorderTabs), typeof(bool), typeof(CustomTabControl),
        new PropertyMetadata(true, (owner, _) => ((CustomTabControl)owner).CancelTabDrag()));
    public bool CanReorderTabs { get => (bool)GetValue(CanReorderTabsProperty); set => SetValue(CanReorderTabsProperty, value); }

    /// <summary>Cursor displayed after a header drag exceeds five physical pixels.</summary>
    public static readonly DependencyProperty TabDragCursorProperty = DependencyProperty.Register(
        nameof(TabDragCursor), typeof(Cursor), typeof(CustomTabControl),
        new PropertyMetadata(Cursors.ScrollWE, (_, _) => Mouse.UpdateCursor()), value => value is Cursor);
    public Cursor TabDragCursor { get => (Cursor)GetValue(TabDragCursorProperty); set => SetValue(TabDragCursorProperty, value); }

    private TabItem? dragCandidate;
    private Point dragOrigin;
    private bool dragging;
    private int dropIndex = -1;
    private DispatcherTimer? dragTimer;
    private Window? dragWindow;

    private IList? ReorderableList() => CanReorderTabs ? EditableList() : null;
    private IList? EditableList()
    {
        if (ItemsSource is null) return Items;
        if (ItemsSource is not IList list || list.IsReadOnly || list.IsFixedSize) return null;
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view.Filter is not null || view.SortDescriptions.Count > 0 || view.GroupDescriptions?.Count > 0 || list.Count != Items.Count) return null;
        for (var i = 0; i < list.Count; i++)
            if (!Equals(list[i], Items[i])) return null;
        return list;
    }

    /// <summary>Moves an item to a final zero-based index, preserving selection. Returns false for unsupported sources.</summary>
    public bool MoveTab(int oldIndex, int newIndex) => MoveTabCore(oldIndex, newIndex, TabReorderReason.Programmatic);

    private bool MoveTabCore(int oldIndex, int newIndex, TabReorderReason reason)
    {
        var list = ReorderableList();
        if (list is null || oldIndex < 0 || newIndex < 0 || oldIndex >= list.Count || newIndex >= list.Count) return false;
        if (ItemContainerGenerator.ContainerFromIndex(oldIndex) is TabItem { IsEnabled: false }) return false;
        if (oldIndex == newIndex) return true;
        var selected = SelectedItem;
        var item = list[oldIndex];
        // ObservableCollection.Move emits a single Move notification, preserving MVVM identity.
        MoveListItem(list, oldIndex, newIndex);
        SetCurrentValue(SelectedItemProperty, selected);
        RevealSelectedTab();
        TabReordered?.Invoke(this, new TabReorderedEventArgs(item!, oldIndex, newIndex, reason));
        return true;
    }

    private void MoveListItem(IList list, int oldIndex, int newIndex)
    {
        var item = list[oldIndex];
        var type = list.GetType();
        while (type is not null && (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ObservableCollection<>))) type = type.BaseType;
        if (type is not null) type.GetMethod("Move")!.Invoke(list, new object[] { oldIndex, newIndex });
        else
        {
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            if (ItemsSource is not null && list is not System.Collections.Specialized.INotifyCollectionChanged) Items.Refresh();
        }
    }

    private Rect HeaderViewport()
    {
        if (headers?.Template.FindName("PART_ScrollContentPresenter", headers) is not FrameworkElement viewport) return Rect.Empty;
        return viewport.TransformToVisual(this).TransformBounds(new Rect(viewport.RenderSize));
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        CancelTabDrag();
        if (TryBeginRenameFromDoubleClick(e)) { e.Handled = true; return; }
        if (ReorderableList() is null || !HeaderViewport().Contains(e.GetPosition(this))) return;
        // Buttons, text fields and other interactive header content keep their normal input behavior.
        var source = e.OriginalSource as DependencyObject;
        for (var node = source; node is not null && node is not TabItem; node = node is FrameworkContentElement content ? content.Parent : VisualTreeHelper.GetParent(node))
            if (node is ButtonBase or TextBoxBase or PasswordBox or Selector) return;
        if (source is null || ItemsControl.ContainerFromElement(this, source) is not TabItem { IsEnabled: true } item) return;
        dragCandidate = item;
        dragOrigin = e.GetPosition(this);
        Mouse.UpdateCursor();
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (dragCandidate is null) return;
        if (e.LeftButton != MouseButtonState.Pressed) { CancelTabDrag(); return; }
        var point = e.GetPosition(this);
        if (!dragging)
        {
            if (!HasPassedDragThreshold(point)) return;
            // Capture can dispatch nested input events: establish the state first.
            dragging = true;
            if (!CaptureMouse() || !dragging || dragCandidate is null) { CancelTabDrag(); return; }
            BeginDragPreview();
            dragWindow = Window.GetWindow(this);
            if (dragWindow is not null) dragWindow.Deactivated += DragWindowDeactivated;
            dragTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(45), DispatcherPriority.Input,
                (_, _) => PollTabDrag(), Dispatcher);
            Mouse.UpdateCursor();
        }
        UpdateDropTarget(point, false);
        e.Handled = true;
    }

    private bool HasPassedDragThreshold(Point point)
    {
        // Convert WPF units to physical pixels so the threshold stays 5px at any DPI.
        var delta = point - dragOrigin;
        var dpi = VisualTreeHelper.GetDpi(this);
        var x = delta.X * dpi.DpiScaleX;
        var y = delta.Y * dpi.DpiScaleY;
        return Math.Abs(IsVerticalTabStrip ? y : x) > MinimumDragDistance;
    }

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        base.OnQueryCursor(e);
        // Never change CursorProperty: its original value/binding survives every exit path.
        if (dragging && dragCandidate is not null && CanReorderTabs && IsMouseCaptured && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            e.Cursor = TabDragCursor;
            e.Handled = true;
        }
    }

    private void DragWindowDeactivated(object? sender, EventArgs e) => CancelTabDrag();

    private void PollTabDrag()
    {
        // Recover even if mouse-up was consumed elsewhere or occurred outside the window.
        if (!dragging || Mouse.LeftButton != MouseButtonState.Pressed || !IsMouseCaptured ||
            dragWindow is { IsActive: false })
        {
            CancelTabDrag();
            return;
        }
        UpdateDropTarget(Mouse.GetPosition(this), true);
    }

    private Point ProjectToHeaderAxis(Point point, Rect viewport) => IsVerticalTabStrip
        ? new Point(viewport.Left + viewport.Width / 2, point.Y)
        : new Point(point.X, viewport.Top + viewport.Height / 2);

    private void UpdateDropTarget(Point point, bool scroll)
    {
        var marker = GetTemplateChild("PART_DropIndicator") as Line;
        var viewport = HeaderViewport();
        if (!viewport.IsEmpty) point = ProjectToHeaderAxis(point, viewport);
        dropIndex = -1;
        if (marker is not null) marker.Visibility = Visibility.Collapsed;
        if (!dragging || viewport.IsEmpty || !viewport.Contains(point) || ReorderableList() is null)
        {
            if (dragging) UpdateDragPreview(point, -1);
            return;
        }
        if (scroll && headers is not null)
        {
            var coordinate = IsVerticalTabStrip ? point.Y : point.X;
            var min = IsVerticalTabStrip ? viewport.Top : viewport.Left;
            var max = IsVerticalTabStrip ? viewport.Bottom : viewport.Right;
            var delta = coordinate < min + 28 ? -18 : coordinate > max - 28 ? 18 : 0;
            if (delta != 0) { if (IsVerticalTabStrip) headers.ScrollToVerticalOffset(headers.VerticalOffset + delta); else headers.ScrollToHorizontalOffset(headers.HorizontalOffset + delta); headers.UpdateLayout(); }
        }
        var slot = Items.Count;
        var x = IsVerticalTabStrip ? viewport.Bottom : viewport.Right;
        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is not TabItem tab) continue;
            var bounds = tab.TransformToVisual(this).TransformBounds(new Rect(tab.RenderSize));
            var start = IsVerticalTabStrip ? bounds.Top : bounds.Left;
            var length = IsVerticalTabStrip ? bounds.Height : bounds.Width;
            x = start + length;
            if ((IsVerticalTabStrip ? point.Y : point.X) < start + length / 2) { slot = i; x = start; break; }
        }
        var oldIndex = ItemContainerGenerator.IndexFromContainer(dragCandidate!);
        if (oldIndex < 0) { CancelTabDrag(); return; }
        dropIndex = Math.Clamp(slot > oldIndex ? slot - 1 : slot, 0, Items.Count - 1);
        UpdateDragPreview(point, dropIndex);
        if (marker is not null && dropIndex != oldIndex)
        {
            // Indicator coordinates are relative to the template canvas, not the control border.
            var origin = TranslatePoint(new Point(Math.Clamp(x, viewport.Left + 1, viewport.Right - 1), viewport.Top + 3), (UIElement)VisualTreeHelper.GetParent(marker));
            if (IsVerticalTabStrip)
            {
                origin = TranslatePoint(new Point(viewport.Left + 3, Math.Clamp(x, viewport.Top + 1, viewport.Bottom - 1)), (UIElement)VisualTreeHelper.GetParent(marker));
                marker.X1 = origin.X;
                marker.X2 = origin.X + Math.Max(0, viewport.Width - 6);
                marker.Y1 = marker.Y2 = origin.Y;
                marker.Visibility = Visibility.Visible;
                return;
            }
            marker.X1 = marker.X2 = origin.X;
            marker.Y1 = origin.Y;
            marker.Y2 = origin.Y + Math.Max(0, viewport.Height - 6);
            marker.Visibility = Visibility.Visible;
        }
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!dragging) { CancelTabDrag(); return; }
        CompleteTabDrag(e.GetPosition(this));
        e.Handled = true;
    }

    private void CompleteTabDrag(Point point)
    {
        var oldIndex = -1;
        var target = -1;
        try
        {
            UpdateDropTarget(point, false);
            oldIndex = dragCandidate is null ? -1 : ItemContainerGenerator.IndexFromContainer(dragCandidate);
            target = dropIndex;
        }
        finally
        {
            committingDrop = target >= 0;
            CancelTabDrag();
            committingDrop = false;
        }
        if (target >= 0) MoveTabCore(oldIndex, target, TabReorderReason.Drag);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (TryBeginRenameFromKeyboard(e)) { e.Handled = true; return; }
        if (e.Key == Key.Escape && dragging) { CancelTabDrag(); e.Handled = true; return; }
        if (TryReorderFromKeyboard(e.Key, Keyboard.Modifiers, e.OriginalSource as DependencyObject)) { e.Handled = true; return; }
        base.OnPreviewKeyDown(e);
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (dragging) CancelTabDrag();
    }

    private void CancelTabDrag()
    {
        var hadPointerFeedback = dragging || dragCandidate is not null;
        dragTimer?.Stop();
        dragTimer = null;
        EndDragPreview();
        dragCandidate = null;
        dropIndex = -1;
        var wasDragging = dragging;
        dragging = false;
        UpdateSurface();
        if (dragWindow is not null) dragWindow.Deactivated -= DragWindowDeactivated;
        dragWindow = null;
        if (GetTemplateChild("PART_DropIndicator") is Line marker) marker.Visibility = Visibility.Collapsed;
        if (wasDragging && IsMouseCaptured) ReleaseMouseCapture();
        if (hadPointerFeedback) Mouse.UpdateCursor();
    }
}
