using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

public enum DataGridViewSelectionBehavior
{
    None,
    Row,
    Column,
    Cell
}

public enum DataGridViewDensity
{
    Compact,
    Default,
    Comfortable
}

public sealed record DataGridViewSelection(object? Item, DataGridColumn? Column);

[ContentProperty(nameof(Columns))]
public partial class DataGridView : DataGrid
{
    private object? lastNotifiedItem;
    private DataGridColumn? lastNotifiedColumn;
    private bool applyingSelectionBehavior;
    private ScrollViewer? animatedScrollViewer;
    private double horizontalAnimationTarget;
    private long horizontalAnimationLastFrame;
    private bool isHorizontalAnimationActive;
    private ScrollViewer? verticallyAnimatedScrollViewer;
    private double verticalAnimationTarget;
    private long verticalAnimationLastFrame;
    private bool isVerticalAnimationActive;
    private ScrollViewer? mousePanningScrollViewer;
    private Point mousePanningOrigin;
    private double mousePanningHorizontalOrigin;
    private double mousePanningVerticalOrigin;
    private bool canMousePanHorizontally;
    private bool canMousePanVertically;
    private Cursor? cursorBeforeMousePanning;

    static DataGridView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridView), new FrameworkPropertyMetadata(typeof(DataGridView)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateRoundedContentClip();
        InitializePinningVisuals();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateRoundedContentClip();
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (TryScrollHorizontally(e.Delta, Keyboard.Modifiers) ||
            TryScrollVertically(e.Delta, Keyboard.Modifiers))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseWheel(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (mousePanningScrollViewer is not null)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
                UpdateMousePanning(e.GetPosition(this));
            else
                EndMousePanning();
            e.Handled = true;
            return;
        }
        base.OnMouseMove(e);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && mousePanningScrollViewer is not null)
        {
            EndMousePanning();
            e.Handled = true;
            return;
        }
        base.OnPreviewMouseUp(e);
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        EndMousePanning(false);
        base.OnLostMouseCapture(e);
    }

    internal bool TryScrollHorizontally(int wheelDelta, ModifierKeys modifiers)
    {
        if ((modifiers & ModifierKeys.Shift) == 0 || wheelDelta == 0 ||
            GetTemplateChild("DG_ScrollViewer") is not ScrollViewer viewer || viewer.ScrollableWidth <= 0)
            return false;

        var detents = wheelDelta / (double)Mouse.MouseWheelDeltaForOneLine;
        var currentTarget = isHorizontalAnimationActive && ReferenceEquals(animatedScrollViewer, viewer)
            ? horizontalAnimationTarget
            : viewer.HorizontalOffset;
        var target = Math.Clamp(currentTarget - HorizontalMouseWheelScrollAmount * detents, 0, viewer.ScrollableWidth);

        if (!IsSmoothHorizontalScrollingEnabled || HorizontalScrollAnimationDuration <= TimeSpan.Zero)
        {
            StopHorizontalScrollAnimation();
            viewer.ScrollToHorizontalOffset(target);
            return true;
        }

        animatedScrollViewer = viewer;
        horizontalAnimationTarget = target;
        if (!isHorizontalAnimationActive)
        {
            isHorizontalAnimationActive = true;
            horizontalAnimationLastFrame = Stopwatch.GetTimestamp();
            CompositionTarget.Rendering += OnHorizontalScrollAnimationFrame;
        }
        return true;
    }

    private void OnHorizontalScrollAnimationFrame(object? sender, EventArgs args)
    {
        if (animatedScrollViewer is null)
        {
            StopHorizontalScrollAnimation();
            return;
        }

        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(horizontalAnimationLastFrame, now);
        horizontalAnimationLastFrame = now;
        var next = ApproachTarget(animatedScrollViewer.HorizontalOffset, horizontalAnimationTarget,
            elapsed, HorizontalScrollAnimationDuration);
        animatedScrollViewer.ScrollToHorizontalOffset(next);
        if (Math.Abs(next - horizontalAnimationTarget) <= 0.25)
        {
            animatedScrollViewer.ScrollToHorizontalOffset(horizontalAnimationTarget);
            StopHorizontalScrollAnimation();
        }
    }

    private void StopHorizontalScrollAnimation()
    {
        if (isHorizontalAnimationActive) CompositionTarget.Rendering -= OnHorizontalScrollAnimationFrame;
        isHorizontalAnimationActive = false;
        animatedScrollViewer = null;
    }

    internal bool TryScrollVertically(int wheelDelta, ModifierKeys modifiers)
    {
        if ((modifiers & ModifierKeys.Shift) != 0 || wheelDelta == 0 ||
            GetTemplateChild("DG_ScrollViewer") is not ScrollViewer viewer || viewer.ScrollableHeight <= 0)
            return false;

        var detents = wheelDelta / (double)Mouse.MouseWheelDeltaForOneLine;
        var currentTarget = isVerticalAnimationActive && ReferenceEquals(verticallyAnimatedScrollViewer, viewer)
            ? verticalAnimationTarget
            : viewer.VerticalOffset;
        var target = Math.Clamp(currentTarget - VerticalMouseWheelScrollAmount * detents, 0, viewer.ScrollableHeight);

        if (!IsSmoothVerticalScrollingEnabled || VerticalScrollAnimationDuration <= TimeSpan.Zero)
        {
            StopVerticalScrollAnimation();
            viewer.ScrollToVerticalOffset(target);
            return true;
        }

        verticallyAnimatedScrollViewer = viewer;
        verticalAnimationTarget = target;
        if (!isVerticalAnimationActive)
        {
            isVerticalAnimationActive = true;
            verticalAnimationLastFrame = Stopwatch.GetTimestamp();
            CompositionTarget.Rendering += OnVerticalScrollAnimationFrame;
        }
        return true;
    }

    private void OnVerticalScrollAnimationFrame(object? sender, EventArgs args)
    {
        if (verticallyAnimatedScrollViewer is null)
        {
            StopVerticalScrollAnimation();
            return;
        }

        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(verticalAnimationLastFrame, now);
        verticalAnimationLastFrame = now;
        var next = ApproachTarget(verticallyAnimatedScrollViewer.VerticalOffset, verticalAnimationTarget,
            elapsed, VerticalScrollAnimationDuration);
        verticallyAnimatedScrollViewer.ScrollToVerticalOffset(next);
        if (Math.Abs(next - verticalAnimationTarget) <= 0.25)
        {
            verticallyAnimatedScrollViewer.ScrollToVerticalOffset(verticalAnimationTarget);
            StopVerticalScrollAnimation();
        }
    }

    private void StopVerticalScrollAnimation()
    {
        if (isVerticalAnimationActive) CompositionTarget.Rendering -= OnVerticalScrollAnimationFrame;
        isVerticalAnimationActive = false;
        verticallyAnimatedScrollViewer = null;
    }

    private static double ApproachTarget(double current, double target, TimeSpan elapsed, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return target;
        var progress = Math.Max(0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);
        var factor = 1 - Math.Pow(0.01, progress);
        return current + (target - current) * factor;
    }

    internal bool TryBeginMousePanning(Point position, bool captureMouse = true)
    {
        if (!IsMiddleButtonPanningEnabled ||
            GetTemplateChild("DG_ScrollViewer") is not ScrollViewer viewer) return false;

        var horizontal = viewer.ScrollableWidth > 0;
        var vertical = viewer.ScrollableHeight > 0;
        if (!horizontal && !vertical) return false;
        if (captureMouse && !CaptureMouse()) return false;

        StopHorizontalScrollAnimation();
        StopVerticalScrollAnimation();
        mousePanningScrollViewer = viewer;
        mousePanningOrigin = position;
        mousePanningHorizontalOrigin = viewer.HorizontalOffset;
        mousePanningVerticalOrigin = viewer.VerticalOffset;
        canMousePanHorizontally = horizontal;
        canMousePanVertically = vertical;
        cursorBeforeMousePanning = Cursor;
        Cursor = GetMousePanningCursor(horizontal, vertical);
        return true;
    }

    internal void UpdateMousePanning(Point position)
    {
        if (mousePanningScrollViewer is null) return;
        var movement = position - mousePanningOrigin;
        if (canMousePanHorizontally)
            mousePanningScrollViewer.ScrollToHorizontalOffset(
                Math.Clamp(mousePanningHorizontalOrigin - movement.X * MousePanningSpeed, 0,
                    mousePanningScrollViewer.ScrollableWidth));
        if (canMousePanVertically)
            mousePanningScrollViewer.ScrollToVerticalOffset(
                Math.Clamp(mousePanningVerticalOrigin - movement.Y * MousePanningSpeed, 0,
                    mousePanningScrollViewer.ScrollableHeight));
    }

    internal void EndMousePanning(bool releaseCapture = true)
    {
        if (mousePanningScrollViewer is null) return;
        mousePanningScrollViewer = null;
        canMousePanHorizontally = false;
        canMousePanVertically = false;
        Cursor = cursorBeforeMousePanning;
        cursorBeforeMousePanning = null;
        if (releaseCapture && IsMouseCaptured) ReleaseMouseCapture();
    }

    internal static Cursor GetMousePanningCursor(bool horizontal, bool vertical) =>
        horizontal && vertical ? Cursors.ScrollAll :
        horizontal ? Cursors.ScrollWE :
        Cursors.ScrollNS;

    private void UpdateRoundedContentClip()
    {
        if (GetTemplateChild("PART_ClipRoot") is not FrameworkElement root ||
            root.ActualWidth <= 0 || root.ActualHeight <= 0) return;

        var radius = Math.Max(0, Math.Min(
            Math.Min(CornerRadius.TopLeft, CornerRadius.TopRight),
            Math.Min(CornerRadius.BottomRight, CornerRadius.BottomLeft)) -
            Math.Max(BorderThickness.Left, BorderThickness.Top));
        root.Clip = new RectangleGeometry(new Rect(0, 0, root.ActualWidth, root.ActualHeight), radius, radius);
    }

    public DataGridView()
    {
        SetCurrentValue(AutoGenerateColumnsProperty, false);
        SetCurrentValue(CanUserAddRowsProperty, false);
        SetCurrentValue(CanUserDeleteRowsProperty, false);
        SetCurrentValue(IsReadOnlyProperty, true);
        SetCurrentValue(HeadersVisibilityProperty, DataGridHeadersVisibility.Column);
        SetCurrentValue(EnableRowVirtualizationProperty, true);
        SetCurrentValue(EnableColumnVirtualizationProperty, true);
        SetCurrentValue(VirtualizingPanel.ScrollUnitProperty, ScrollUnit.Pixel);
        SetCurrentValue(CanUserReorderColumnsProperty, true);
        InitializePinning();
        Sorting += OnGridSorting;
        InitializeFiltering();
        ApplySelectionMode();
        Loaded += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(DefaultSortMemberPath) &&
                Columns.All(column => column.SortDirection is null)) ApplyDefaultSort();
            CaptureInitialState();
        };
        Unloaded += (_, _) =>
        {
            StopHorizontalScrollAnimation();
            StopVerticalScrollAnimation();
            EndMousePanning();
        };
        ApplySelectionBehavior();
    }

    public static readonly DependencyProperty IsSmoothHorizontalScrollingEnabledProperty = DependencyProperty.Register(
        nameof(IsSmoothHorizontalScrollingEnabled), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool IsSmoothHorizontalScrollingEnabled
    {
        get => (bool)GetValue(IsSmoothHorizontalScrollingEnabledProperty);
        set => SetValue(IsSmoothHorizontalScrollingEnabledProperty, value);
    }

    public static readonly DependencyProperty HorizontalMouseWheelScrollAmountProperty = DependencyProperty.Register(
        nameof(HorizontalMouseWheelScrollAmount), typeof(double), typeof(DataGridView), new PropertyMetadata(48d),
        value => value is double amount && double.IsFinite(amount) && amount > 0);
    public double HorizontalMouseWheelScrollAmount
    {
        get => (double)GetValue(HorizontalMouseWheelScrollAmountProperty);
        set => SetValue(HorizontalMouseWheelScrollAmountProperty, value);
    }

    public static readonly DependencyProperty HorizontalScrollAnimationDurationProperty = DependencyProperty.Register(
        nameof(HorizontalScrollAnimationDuration), typeof(TimeSpan), typeof(DataGridView), new PropertyMetadata(TimeSpan.FromMilliseconds(260)),
        value => value is TimeSpan duration && duration >= TimeSpan.Zero);
    public TimeSpan HorizontalScrollAnimationDuration
    {
        get => (TimeSpan)GetValue(HorizontalScrollAnimationDurationProperty);
        set => SetValue(HorizontalScrollAnimationDurationProperty, value);
    }

    public static readonly DependencyProperty IsSmoothVerticalScrollingEnabledProperty = DependencyProperty.Register(
        nameof(IsSmoothVerticalScrollingEnabled), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool IsSmoothVerticalScrollingEnabled
    {
        get => (bool)GetValue(IsSmoothVerticalScrollingEnabledProperty);
        set => SetValue(IsSmoothVerticalScrollingEnabledProperty, value);
    }

    public static readonly DependencyProperty VerticalMouseWheelScrollAmountProperty = DependencyProperty.Register(
        nameof(VerticalMouseWheelScrollAmount), typeof(double), typeof(DataGridView), new PropertyMetadata(72d),
        value => value is double amount && double.IsFinite(amount) && amount > 0);
    public double VerticalMouseWheelScrollAmount
    {
        get => (double)GetValue(VerticalMouseWheelScrollAmountProperty);
        set => SetValue(VerticalMouseWheelScrollAmountProperty, value);
    }

    public static readonly DependencyProperty VerticalScrollAnimationDurationProperty = DependencyProperty.Register(
        nameof(VerticalScrollAnimationDuration), typeof(TimeSpan), typeof(DataGridView), new PropertyMetadata(TimeSpan.FromMilliseconds(260)),
        value => value is TimeSpan duration && duration >= TimeSpan.Zero);
    public TimeSpan VerticalScrollAnimationDuration
    {
        get => (TimeSpan)GetValue(VerticalScrollAnimationDurationProperty);
        set => SetValue(VerticalScrollAnimationDurationProperty, value);
    }

    public static readonly DependencyProperty IsMiddleButtonPanningEnabledProperty = DependencyProperty.Register(
        nameof(IsMiddleButtonPanningEnabled), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool IsMiddleButtonPanningEnabled
    {
        get => (bool)GetValue(IsMiddleButtonPanningEnabledProperty);
        set => SetValue(IsMiddleButtonPanningEnabledProperty, value);
    }

    public static readonly DependencyProperty MousePanningSpeedProperty = DependencyProperty.Register(
        nameof(MousePanningSpeed), typeof(double), typeof(DataGridView), new PropertyMetadata(1d),
        value => value is double speed && double.IsFinite(speed) && speed > 0);
    public double MousePanningSpeed
    {
        get => (double)GetValue(MousePanningSpeedProperty);
        set => SetValue(MousePanningSpeedProperty, value);
    }

    public static readonly DependencyProperty SelectionBehaviorProperty = DependencyProperty.Register(
        nameof(SelectionBehavior), typeof(DataGridViewSelectionBehavior), typeof(DataGridView),
        new FrameworkPropertyMetadata(DataGridViewSelectionBehavior.None, OnSelectionBehaviorChanged),
        value => value is DataGridViewSelectionBehavior behavior && Enum.IsDefined(behavior));
    public DataGridViewSelectionBehavior SelectionBehavior
    {
        get => (DataGridViewSelectionBehavior)GetValue(SelectionBehaviorProperty);
        set => SetValue(SelectionBehaviorProperty, value);
    }

    public static readonly DependencyProperty SelectionChangedCommandProperty = DependencyProperty.Register(
        nameof(SelectionChangedCommand), typeof(ICommand), typeof(DataGridView), new PropertyMetadata(null));
    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public static readonly DependencyProperty SelectedColumnProperty = DependencyProperty.Register(
        nameof(SelectedColumn), typeof(DataGridColumn), typeof(DataGridView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColumnChanged));
    public DataGridColumn? SelectedColumn
    {
        get => (DataGridColumn?)GetValue(SelectedColumnProperty);
        set => SetValue(SelectedColumnProperty, value);
    }

    public static readonly DependencyProperty CanUserToggleColumnVisibilityProperty = DependencyProperty.Register(
        nameof(CanUserToggleColumnVisibility), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool CanUserToggleColumnVisibility
    {
        get => (bool)GetValue(CanUserToggleColumnVisibilityProperty);
        set => SetValue(CanUserToggleColumnVisibilityProperty, value);
    }

    public static readonly DependencyProperty ShowRowSeparatorsProperty = DependencyProperty.Register(
        nameof(ShowRowSeparators), typeof(bool), typeof(DataGridView),
        new FrameworkPropertyMetadata(true, OnShowRowSeparatorsChanged));
    public bool ShowRowSeparators
    {
        get => (bool)GetValue(ShowRowSeparatorsProperty);
        set => SetValue(ShowRowSeparatorsProperty, value);
    }

    public static readonly DependencyProperty CellPaddingProperty = DependencyProperty.Register(
        nameof(CellPadding), typeof(Thickness), typeof(DataGridView),
        new FrameworkPropertyMetadata(new Thickness(10, 7, 10, 7), FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => value is Thickness thickness && IsValidThickness(thickness));
    public Thickness CellPadding
    {
        get => (Thickness)GetValue(CellPaddingProperty);
        set => SetValue(CellPaddingProperty, value);
    }

    public static readonly DependencyProperty DensityProperty = DependencyProperty.Register(
        nameof(Density), typeof(DataGridViewDensity), typeof(DataGridView),
        new FrameworkPropertyMetadata(DataGridViewDensity.Default, OnDensityChanged),
        value => value is DataGridViewDensity density && Enum.IsDefined(density));
    public DataGridViewDensity Density
    {
        get => (DataGridViewDensity)GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(DataGridView),
        new FrameworkPropertyMetadata(new CornerRadius(10), FrameworkPropertyMetadataOptions.AffectsRender,
            (owner, _) => ((DataGridView)owner).UpdateRoundedContentClip()));
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(DataGridView), new PropertyMetadata(false));
    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public static readonly DependencyProperty LoadingMessageProperty = DependencyProperty.Register(
        nameof(LoadingMessage), typeof(string), typeof(DataGridView), new PropertyMetadata("Carregando..."));
    public string LoadingMessage
    {
        get => (string)GetValue(LoadingMessageProperty);
        set => SetValue(LoadingMessageProperty, value);
    }

    public static readonly DependencyProperty EmptyMessageProperty = DependencyProperty.Register(
        nameof(EmptyMessage), typeof(string), typeof(DataGridView), new PropertyMetadata("Nenhum registro encontrado."));
    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.Register(
        nameof(ErrorMessage), typeof(string), typeof(DataGridView), new PropertyMetadata(null));
    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public static readonly DependencyProperty ShowSortIndicatorsProperty = DependencyProperty.Register(
        nameof(ShowSortIndicators), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowSortIndicators
    {
        get => (bool)GetValue(ShowSortIndicatorsProperty);
        set => SetValue(ShowSortIndicatorsProperty, value);
    }

    public static readonly DependencyProperty SortIconSizeProperty = DependencyProperty.Register(
        nameof(SortIconSize), typeof(double), typeof(DataGridView),
        new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => value is double size && double.IsFinite(size) && size > 0);
    public double SortIconSize
    {
        get => (double)GetValue(SortIconSizeProperty);
        set => SetValue(SortIconSizeProperty, value);
    }

    public static readonly DependencyProperty SortIconBrushProperty = DependencyProperty.Register(
        nameof(SortIconBrush), typeof(Brush), typeof(DataGridView),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(157, 163, 174))));
    public Brush SortIconBrush
    {
        get => (Brush)GetValue(SortIconBrushProperty);
        set => SetValue(SortIconBrushProperty, value);
    }

    public static readonly DependencyProperty ActiveSortIconBrushProperty = DependencyProperty.Register(
        nameof(ActiveSortIconBrush), typeof(Brush), typeof(DataGridView),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(66, 165, 232))));
    public Brush ActiveSortIconBrush
    {
        get => (Brush)GetValue(ActiveSortIconBrushProperty);
        set => SetValue(ActiveSortIconBrushProperty, value);
    }

    public static readonly DependencyProperty UnsortedIconProperty = DependencyProperty.Register(
        nameof(UnsortedIcon), typeof(Geometry), typeof(DataGridView),
        new PropertyMetadata(Geometry.Parse("M 2,5 L 7,1 L 12,5 M 2,9 L 7,13 L 12,9")));
    public Geometry UnsortedIcon
    {
        get => (Geometry)GetValue(UnsortedIconProperty);
        set => SetValue(UnsortedIconProperty, value);
    }

    public static readonly DependencyProperty AscendingSortIconProperty = DependencyProperty.Register(
        nameof(AscendingSortIcon), typeof(Geometry), typeof(DataGridView),
        new PropertyMetadata(Geometry.Parse("M 2,10 L 7,5 L 12,10")));
    public Geometry AscendingSortIcon
    {
        get => (Geometry)GetValue(AscendingSortIconProperty);
        set => SetValue(AscendingSortIconProperty, value);
    }

    public static readonly DependencyProperty DescendingSortIconProperty = DependencyProperty.Register(
        nameof(DescendingSortIcon), typeof(Geometry), typeof(DataGridView),
        new PropertyMetadata(Geometry.Parse("M 2,5 L 7,10 L 12,5")));
    public Geometry DescendingSortIcon
    {
        get => (Geometry)GetValue(DescendingSortIconProperty);
        set => SetValue(DescendingSortIconProperty, value);
    }

    public static readonly DependencyProperty DefaultSortMemberPathProperty = DependencyProperty.Register(
        nameof(DefaultSortMemberPath), typeof(string), typeof(DataGridView),
        new PropertyMetadata(null, OnDefaultSortChanged));
    public string? DefaultSortMemberPath
    {
        get => (string?)GetValue(DefaultSortMemberPathProperty);
        set => SetValue(DefaultSortMemberPathProperty, value);
    }

    public static readonly DependencyProperty DefaultSortDirectionProperty = DependencyProperty.Register(
        nameof(DefaultSortDirection), typeof(ListSortDirection), typeof(DataGridView),
        new PropertyMetadata(ListSortDirection.Ascending, OnDefaultSortChanged),
        value => value is ListSortDirection direction && Enum.IsDefined(direction));
    public ListSortDirection DefaultSortDirection
    {
        get => (ListSortDirection)GetValue(DefaultSortDirectionProperty);
        set => SetValue(DefaultSortDirectionProperty, value);
    }

    public static readonly DependencyProperty ShowClearSortMenuItemProperty = DependencyProperty.Register(
        nameof(ShowClearSortMenuItem), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowClearSortMenuItem
    {
        get => (bool)GetValue(ShowClearSortMenuItemProperty);
        set => SetValue(ShowClearSortMenuItemProperty, value);
    }

    public static readonly DependencyProperty ShowRestoreDefaultSortMenuItemProperty = DependencyProperty.Register(
        nameof(ShowRestoreDefaultSortMenuItem), typeof(bool), typeof(DataGridView), new PropertyMetadata(true));
    public bool ShowRestoreDefaultSortMenuItem
    {
        get => (bool)GetValue(ShowRestoreDefaultSortMenuItemProperty);
        set => SetValue(ShowRestoreDefaultSortMenuItemProperty, value);
    }

    public static readonly DependencyProperty ScrollBarThicknessProperty = DependencyProperty.Register(
        nameof(ScrollBarThickness), typeof(double), typeof(DataGridView),
        new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => value is double thickness && double.IsFinite(thickness) && thickness > 0);
    public double ScrollBarThickness
    {
        get => (double)GetValue(ScrollBarThicknessProperty);
        set => SetValue(ScrollBarThicknessProperty, value);
    }

    public static readonly DependencyProperty ScrollBarTrackBrushProperty = DependencyProperty.Register(
        nameof(ScrollBarTrackBrush), typeof(Brush), typeof(DataGridView),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x3D, 0x40, 0x46))));
    public Brush ScrollBarTrackBrush
    {
        get => (Brush)GetValue(ScrollBarTrackBrushProperty);
        set => SetValue(ScrollBarTrackBrushProperty, value);
    }

    public static readonly DependencyProperty ScrollBarThumbBrushProperty = DependencyProperty.Register(
        nameof(ScrollBarThumbBrush), typeof(Brush), typeof(DataGridView),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x68, 0x6D, 0x77))));
    public Brush ScrollBarThumbBrush
    {
        get => (Brush)GetValue(ScrollBarThumbBrushProperty);
        set => SetValue(ScrollBarThumbBrushProperty, value);
    }

    public static readonly DependencyProperty ScrollBarThumbHoverBrushProperty = DependencyProperty.Register(
        nameof(ScrollBarThumbHoverBrush), typeof(Brush), typeof(DataGridView),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x8B, 0x91, 0x9D))));
    public Brush ScrollBarThumbHoverBrush
    {
        get => (Brush)GetValue(ScrollBarThumbHoverBrushProperty);
        set => SetValue(ScrollBarThumbHoverBrushProperty, value);
    }

    private static void OnDefaultSortChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var grid = (DataGridView)owner;
        if (grid.IsLoaded && !string.IsNullOrWhiteSpace(grid.DefaultSortMemberPath)) grid.ApplyDefaultSort();
    }

    public void ClearSorting()
    {
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view?.CanSort == true) view.SortDescriptions.Clear();
        foreach (var column in Columns) column.SortDirection = null;
        UpdateSortPriorities();
        QueuePinningVisualUpdate();
    }

    public bool ApplyDefaultSort()
    {
        if (string.IsNullOrWhiteSpace(DefaultSortMemberPath))
        {
            ClearSorting();
            return false;
        }

        var column = Columns.FirstOrDefault(candidate =>
            string.Equals(candidate.SortMemberPath, DefaultSortMemberPath, StringComparison.Ordinal));
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (column is null || view?.CanSort != true) return false;

        return ApplySort(column, DefaultSortDirection);
    }

    private static void OnDensityChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var grid = (DataGridView)owner;
        grid.SetCurrentValue(CellPaddingProperty, (DataGridViewDensity)args.NewValue switch
        {
            DataGridViewDensity.Compact => new Thickness(12, 6, 12, 6),
            DataGridViewDensity.Comfortable => new Thickness(18, 12, 18, 12),
            _ => new Thickness(14, 9, 14, 9)
        });
    }

    private static bool IsValidThickness(Thickness value) =>
        new[] { value.Left, value.Top, value.Right, value.Bottom }.All(number => double.IsFinite(number) && number >= 0);

    private static void OnSelectionBehaviorChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((DataGridView)owner).ApplySelectionBehavior();

    private void ApplySelectionBehavior()
    {
        applyingSelectionBehavior = true;
        try
        {
            UnselectAll();
            CurrentCell = new DataGridCellInfo();
            SetCurrentValue(SelectionUnitProperty, SelectionBehavior == DataGridViewSelectionBehavior.Cell
                ? DataGridSelectionUnit.Cell : DataGridSelectionUnit.FullRow);
            if (SelectionBehavior is not DataGridViewSelectionBehavior.Column and not DataGridViewSelectionBehavior.Cell)
                SetCurrentValue(SelectedColumnProperty, null);
            lastNotifiedItem = null;
            lastNotifiedColumn = null;
        }
        finally { applyingSelectionBehavior = false; }
    }

    private static void OnShowRowSeparatorsChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) =>
        ((DataGridView)owner).SetCurrentValue(GridLinesVisibilityProperty,
            (bool)args.NewValue ? DataGridGridLinesVisibility.Horizontal : DataGridGridLinesVisibility.None);

    private static void OnSelectedColumnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var grid = (DataGridView)owner;
        if (args.NewValue is DataGridColumn column && !grid.Columns.Contains(column))
        {
            grid.SetCurrentValue(SelectedColumnProperty, null);
            return;
        }
        if (!grid.applyingSelectionBehavior && grid.SelectionBehavior == DataGridViewSelectionBehavior.Column)
            grid.NotifySelectionChanged();
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && TryBeginMousePanning(e.GetPosition(this)))
        {
            e.Handled = true;
            return;
        }
        if (e.ChangedButton == MouseButton.Left && SelectionBehavior == DataGridViewSelectionBehavior.Column &&
            FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject) is { Column: { } column })
        {
            SetCurrentValue(SelectedColumnProperty, column);
            UnselectAll();
            CurrentCell = new DataGridCellInfo();
        }
        if (SelectionBehavior == DataGridViewSelectionBehavior.None &&
            FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) is not null)
        {
            Focus();
            e.Handled = true;
            return;
        }
        base.OnPreviewMouseDown(e);
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        if (!applyingSelectionBehavior) { NotifySelectionChanged(); NotifyBatchSelectionChanged(); }
    }

    protected override void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
    {
        base.OnSelectedCellsChanged(e);
        if (!applyingSelectionBehavior) { NotifySelectionChanged(); NotifyBatchSelectionChanged(); }
    }

    private void NotifySelectionChanged()
    {
        if (SelectionBehavior == DataGridViewSelectionBehavior.None) return;
        var item = SelectionBehavior switch
        {
            DataGridViewSelectionBehavior.Column => null,
            DataGridViewSelectionBehavior.Cell when CurrentCell.IsValid => CurrentCell.Item,
            _ => SelectedItem
        };
        var column = SelectionBehavior switch
        {
            DataGridViewSelectionBehavior.Column => SelectedColumn,
            DataGridViewSelectionBehavior.Cell when CurrentCell.IsValid => CurrentCell.Column,
            _ => null
        };
        if (SelectionBehavior != DataGridViewSelectionBehavior.Column && item is null ||
            SelectionBehavior == DataGridViewSelectionBehavior.Column && column is null) return;
        if (ReferenceEquals(lastNotifiedItem, item) && ReferenceEquals(lastNotifiedColumn, column)) return;
        var selection = new DataGridViewSelection(item, column);
        if (SelectionChangedCommand?.CanExecute(selection) == true)
        {
            lastNotifiedItem = item;
            lastNotifiedColumn = column;
            SelectionChangedCommand.Execute(selection);
        }
    }

    protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject) is { } header)
        {
            var menu = CreateColumnHeaderMenu(header.Column);
            if (menu.Items.Count == 0)
            {
                base.OnPreviewMouseRightButtonDown(e);
                return;
            }
            menu.PlacementTarget = header;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;
            e.Handled = true;
            return;
        }
        if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) is { Item: { } rowItem } row &&
            (CanPinRows || CanPinColumns))
        {
            var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            var menu = CreateRowPinMenu(rowItem, cell?.Column);
            if (menu.Items.Count > 0)
            {
                menu.PlacementTarget = row;
                menu.Placement = PlacementMode.MousePoint;
                menu.IsOpen = true;
                e.Handled = true;
                return;
            }
        }
        base.OnPreviewMouseRightButtonDown(e);
    }

    internal ContextMenu CreateColumnVisibilityMenu()
        => CreateColumnHeaderMenu();

    internal ContextMenu CreateColumnHeaderMenu(DataGridColumn? contextColumn = null)
    {
        var menu = new ContextMenu();
        if (CanPinColumns && contextColumn is not null)
        {
            var isPinned = pinnedColumns.Contains(contextColumn);
            var pin = new MenuItem
            {
                Header = isPinned ? "Desafixar coluna" : "Fixar coluna",
                Icon = CreatePinIcon(isPinned),
                IsEnabled = isPinned || pinnedColumns.Count < MaxPinnedColumns
            };
            pin.Click += (_, _) => ToggleColumnPin(contextColumn);
            menu.Items.Add(pin);
            if (pinnedColumns.Count > 0)
            {
                var unpinAll = new MenuItem { Header = "Desafixar todas as colunas", Icon = CreatePinIcon(true) };
                unpinAll.Click += (_, _) => UnpinAllColumns();
                menu.Items.Add(unpinAll);
            }
            menu.Items.Add(new Separator());
        }
        if (ShowClearSortMenuItem)
        {
            var clearSort = new MenuItem { Header = "Limpar ordenação", IsEnabled = HasActiveSorting() };
            clearSort.Click += (_, _) => ClearSorting();
            menu.Items.Add(clearSort);
        }

        if (ShowRestoreDefaultSortMenuItem)
        {
            var restoreDefault = new MenuItem
            {
                Header = "Restaurar ordenação padrão",
                IsEnabled = !string.IsNullOrWhiteSpace(DefaultSortMemberPath)
            };
            restoreDefault.Click += (_, _) => ApplyDefaultSort();
            menu.Items.Add(restoreDefault);
        }

        if (ShowFilterMenuItems && Filters.Count > 0)
        {
            if (menu.Items.Count > 0) menu.Items.Add(new Separator());
            if (contextColumn is not null)
            {
                var clearColumnFilter = new MenuItem
                {
                    Header = "Limpar filtro desta coluna",
                    IsEnabled = Filters.Any(filter => string.Equals(filter.ColumnKey, GetStableColumnKey(contextColumn), StringComparison.Ordinal))
                };
                clearColumnFilter.Click += (_, _) => ClearFilter(contextColumn);
                menu.Items.Add(clearColumnFilter);
            }
            var clearFilters = new MenuItem { Header = "Limpar todos os filtros" };
            clearFilters.Click += (_, _) => ClearFilters();
            menu.Items.Add(clearFilters);
        }

        if (CanUserToggleColumnVisibility)
        {
            if (menu.Items.Count > 0) menu.Items.Add(new Separator());
            var visibleCount = Columns.Count(column => column.Visibility == Visibility.Visible);
            foreach (var column in Columns.OrderBy(column => column.DisplayIndex))
            {
                var item = new MenuItem
                {
                    Header = column.Header?.ToString() ?? $"Coluna {column.DisplayIndex + 1}",
                    IsCheckable = true,
                    IsChecked = column.Visibility == Visibility.Visible,
                    IsEnabled = column.Visibility != Visibility.Visible || visibleCount > 1,
                    Tag = column
                };
                item.Click += (_, _) =>
                {
                    var target = (DataGridColumn)item.Tag;
                    target.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                    if (ReferenceEquals(SelectedColumn, target) && target.Visibility != Visibility.Visible)
                        SetCurrentValue(SelectedColumnProperty, null);
                };
                menu.Items.Add(item);
            }
        }
        return menu;
    }

    private ContextMenu CreateRowPinMenu(object rowItem, DataGridColumn? column)
    {
        var menu = new ContextMenu();
        if (CanPinRows)
        {
            var isPinned = pinnedRows.Contains(rowItem);
            var pinRow = new MenuItem
            {
                Header = isPinned ? "Desafixar linha" : "Fixar linha",
                Icon = CreatePinIcon(isPinned),
                IsEnabled = isPinned || pinnedRows.Count < MaxPinnedRows
            };
            pinRow.Click += (_, _) => ToggleRowPin(rowItem);
            menu.Items.Add(pinRow);
            if (pinnedRows.Count > 0)
            {
                var unpinRows = new MenuItem { Header = "Desafixar todas as linhas", Icon = CreatePinIcon(true) };
                unpinRows.Click += (_, _) => UnpinAllRows();
                menu.Items.Add(unpinRows);
            }
        }
        if (CanPinColumns && column is not null)
        {
            if (menu.Items.Count > 0) menu.Items.Add(new Separator());
            var isPinned = pinnedColumns.Contains(column);
            var pinColumn = new MenuItem
            {
                Header = isPinned ? "Desafixar esta coluna" : "Fixar esta coluna",
                Icon = CreatePinIcon(isPinned),
                IsEnabled = isPinned || pinnedColumns.Count < MaxPinnedColumns
            };
            pinColumn.Click += (_, _) => ToggleColumnPin(column);
            menu.Items.Add(pinColumn);
        }
        return menu;
    }

    private bool HasActiveSorting() =>
        Columns.Any(column => column.SortDirection is not null) ||
        CollectionViewSource.GetDefaultView(ItemsSource)?.SortDescriptions.Count > 0;

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T result) return result;
            current = current is FrameworkContentElement content ? content.Parent : VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
