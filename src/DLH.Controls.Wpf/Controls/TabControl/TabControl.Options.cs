using System.Windows;

namespace DLH.Controls.Wpf;

public enum TabReorderReason { Programmatic, Drag, Keyboard }

public sealed class TabReorderedEventArgs(object item, int oldIndex, int newIndex, TabReorderReason reason) : EventArgs
{
    public object Item { get; } = item;
    public int OldIndex { get; } = oldIndex;
    public int NewIndex { get; } = newIndex;
    public TabReorderReason Reason { get; } = reason;
}

public sealed class TabClosingEventArgs(object item) : EventArgs
{
    public object Item { get; } = item;
    public bool Cancel { get; set; }
}

public sealed class TabClosedEventArgs(object item) : EventArgs
{
    public object Item { get; } = item;
}

public partial class TabControl
{
    public event EventHandler<TabReorderedEventArgs>? TabReordered;
    public event EventHandler<TabClosingEventArgs>? TabClosing;
    public event EventHandler<TabClosedEventArgs>? TabClosed;
    public event EventHandler? StateRestored;

    public static readonly DependencyProperty IsDragPreviewEnabledProperty = DependencyProperty.Register(
        nameof(IsDragPreviewEnabled), typeof(bool), typeof(TabControl), new PropertyMetadata(true, CancelOnConfigurationChange));
    public bool IsDragPreviewEnabled { get => (bool)GetValue(IsDragPreviewEnabledProperty); set => SetValue(IsDragPreviewEnabledProperty, value); }

    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(TabControl), new PropertyMetadata(true, CancelOnConfigurationChange));
    public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    public static readonly DependencyProperty DragAnimationDurationProperty = DependencyProperty.Register(
        nameof(DragAnimationDuration), typeof(TimeSpan), typeof(TabControl), new PropertyMetadata(TimeSpan.FromMilliseconds(180), CancelOnConfigurationChange),
        value => value is TimeSpan time && time >= TimeSpan.Zero && time <= TimeSpan.FromSeconds(10));
    public TimeSpan DragAnimationDuration { get => (TimeSpan)GetValue(DragAnimationDurationProperty); set => SetValue(DragAnimationDurationProperty, value); }

    public static readonly DependencyProperty DragPreviewOpacityProperty = DependencyProperty.Register(
        nameof(DragPreviewOpacity), typeof(double), typeof(TabControl), new PropertyMetadata(0.94d, CancelOnConfigurationChange),
        value => value is double number && double.IsFinite(number) && number >= 0 && number <= 1);
    public double DragPreviewOpacity { get => (double)GetValue(DragPreviewOpacityProperty); set => SetValue(DragPreviewOpacityProperty, value); }

    public static readonly DependencyProperty MinimumDragDistanceProperty = DependencyProperty.Register(
        nameof(MinimumDragDistance), typeof(double), typeof(TabControl), new PropertyMetadata(5d, CancelOnConfigurationChange),
        value => value is double number && double.IsFinite(number) && number >= 0);
    public double MinimumDragDistance { get => (double)GetValue(MinimumDragDistanceProperty); set => SetValue(MinimumDragDistanceProperty, value); }

    private static void CancelOnConfigurationChange(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var control = (TabControl)owner;
        control.CancelTabDrag();
        control.ResetDragPreview();
    }
}

