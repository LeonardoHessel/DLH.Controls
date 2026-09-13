using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.CompilerServices;

namespace DLH.Controls.Wpf;

public partial class DataGridView
{
    private Canvas? pinningLayer;
    private ScrollViewer? pinningScrollViewer;
    private FrameworkElement? pinningViewport;
    private readonly Dictionary<object, (double Offset, double Height)> pinnedRowMetrics = [];
    private readonly Dictionary<object, Brush> pinnedRowBackgrounds = [];
    private readonly Dictionary<object, int> pinnedRowIndices = [];
    private readonly Dictionary<object, double> renderedRowHeights = [];
    private readonly Dictionary<DataGridColumn, (double Offset, double Width)> pinnedColumnMetrics = [];
    private bool pinningUpdatePending;
    private string pinningLayoutSignature = string.Empty;
    private readonly List<(DataGridColumn Column, Visibility Visibility, double Width, int DisplayIndex)> pinningColumnLayout = [];
    private static readonly DependencyProperty OriginalPinnedColumnProperty = DependencyProperty.RegisterAttached(
        "OriginalPinnedColumn", typeof(DataGridColumn), typeof(DataGridView));
    private readonly List<DataGridView> pinnedRowOverlays = [];
    private readonly List<DataGridView> pinnedColumnOverlays = [];
    private double lastOverlayVerticalOffset = double.NaN;
    private bool overlayNeedsAlignment;
    private (object? Item, double Top, double Height) pinningLayoutAnchor;
    private double? pinningUniformRowHeight;
    private bool pinningHasVariableRowHeights;

    private void InitializePinningVisuals()
    {
        if (pinningScrollViewer is not null) pinningScrollViewer.ScrollChanged -= OnPinningScrollChanged;
        if (pinningLayer is not null) DetachDiscardedPinningOverlays(pinningLayer);
        pinningLayer = GetTemplateChild("PART_PinningLayer") as Canvas;
        pinningScrollViewer = GetTemplateChild("DG_ScrollViewer") as ScrollViewer;
        if (pinningScrollViewer is null || pinningLayer is null) return;
        pinningLayoutSignature = string.Empty;
        pinningScrollViewer.ApplyTemplate();
        pinningViewport = pinningScrollViewer.Template.FindName("PART_ScrollContentPresenter", pinningScrollViewer) as FrameworkElement;
        pinningScrollViewer.ScrollChanged += OnPinningScrollChanged;
        QueuePinningVisualUpdate();
    }

    private void OnPinningScrollChanged(object sender, ScrollChangedEventArgs e) => QueuePinningVisualUpdate();

    private void OnPinningLayoutUpdated(object? sender, EventArgs args)
    {
        if (pinnedColumns.Count == 0 && pinnedRows.Count == 0) return;
        // Variable-height virtualization can adjust the visual anchor without
        // changing ScrollViewer offsets. Uniform rows need no extra layout probe.
        if (pinningHasVariableRowHeights && pinnedColumnOverlays.Count > 0 && pinningViewport is not null)
        {
            foreach (var row in VisualChildren<DataGridRow>(pinningViewport))
            {
                var top = row.TranslatePoint(new Point(), pinningViewport).Y;
                if (top + row.ActualHeight <= 0 || top >= pinningViewport.ActualHeight) continue;
                var anchor = (row.Item, top, row.ActualHeight);
                if (pinningLayoutAnchor != anchor)
                {
                    pinningLayoutAnchor = anchor;
                    QueuePinningVisualUpdate();
                }
                break;
            }
        }
        var changed = pinningColumnLayout.Count != Columns.Count;
        for (var index = 0; !changed && index < Columns.Count; index++)
        {
            var column = Columns[index];
            changed = pinningColumnLayout[index] != (column, column.Visibility, column.ActualWidth, column.DisplayIndex);
        }
        if (!changed) return;
        pinningColumnLayout.Clear();
        foreach (var column in Columns)
            pinningColumnLayout.Add((column, column.Visibility, column.ActualWidth, column.DisplayIndex));
        pinningLayoutSignature = string.Empty;
        QueuePinningVisualUpdate();
    }

    partial void OnPinnedItemsChanged()
    {
        CapturePinnedMetrics();
        QueuePinningVisualUpdate();
    }

    private void QueuePinningVisualUpdate()
    {
        if (pinningUpdatePending || pinningLayer is null) return;
        pinningUpdatePending = true;
        Dispatcher.BeginInvoke(() =>
        {
            pinningUpdatePending = false;
            UpdatePinningVisuals();
        }, DispatcherPriority.Render);
    }

    private void CapturePinnedMetrics()
    {
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null ||
            (pinnedRows.Count == 0 && pinnedColumns.Count == 0)) return;
        foreach (var row in VisualChildren<DataGridRow>(pinningViewport))
            if (ItemsControl.ItemsControlFromItemContainer(row) == this && row.Item is not null && row.ActualHeight > 0)
            {
                renderedRowHeights[row.Item] = row.ActualHeight;
                pinningUniformRowHeight ??= row.ActualHeight;
                pinningHasVariableRowHeights |= Math.Abs(row.ActualHeight - pinningUniformRowHeight.Value) > 0.1;
            }
        var viewportOrigin = pinningViewport.TranslatePoint(new Point(), pinningLayer);
        foreach (var item in pinnedRows)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row || row.ActualHeight <= 0)
            {
                // No realized container: could be virtualized off-screen (still at the same
                // Items position, cached metrics stay valid) or genuinely filtered/reordered
                // away (Items position changed or gone, cached metrics are stale). Only
                // Items.IndexOf — not this dictionary alone — reflects filtering/sorting, so
                // it has to be checked here; this branch only runs for pinned rows without a
                // realized container, not on every capture for every visible pinned row.
                var currentIndex = Items.IndexOf(item);
                if (currentIndex < 0 || !pinnedRowIndices.TryGetValue(item, out var capturedIndex) ||
                    currentIndex != capturedIndex)
                {
                    pinnedRowMetrics.Remove(item);
                    pinnedRowBackgrounds.Remove(item);
                    pinnedRowIndices.Remove(item);
                }
                continue;
            }
            var position = row.TranslatePoint(new Point(), pinningLayer);
            pinnedRowMetrics[item] = (position.Y - viewportOrigin.Y + pinningScrollViewer.VerticalOffset, row.ActualHeight);
            pinnedRowBackgrounds[item] = ResolvePinnedRowBackground(row.Background);
            pinnedRowIndices[item] = Items.IndexOf(item);
        }
        if (pinnedColumns.Count == 0 ||
            pinningScrollViewer.Template.FindName("PART_ColumnHeadersPresenter", pinningScrollViewer) is not DependencyObject headers) return;
        foreach (var header in VisualChildren<DataGridColumnHeader>(headers))
        {
            if (!pinnedColumns.Contains(header.Column) || header.Column.Visibility != Visibility.Visible || header.ActualWidth <= 0) continue;
            var position = header.TranslatePoint(new Point(), pinningLayer);
            pinnedColumnMetrics[header.Column] = (position.X - viewportOrigin.X + pinningScrollViewer.HorizontalOffset, header.ActualWidth);
        }
    }

    private void UpdatePinningVisuals()
    {
        if (pinnedRows.Count == 0 && pinnedColumns.Count == 0 &&
            (pinningLayer is null || pinningLayer.Children.Count == 0)) return;
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null ||
            pinningViewport.ActualWidth <= 0 || pinningViewport.ActualHeight <= 0) return;
        CapturePinnedMetrics();
        var origin = pinningViewport.TranslatePoint(new Point(), pinningLayer);
        var signature = CreatePinningLayoutSignature(origin);
        if (string.Equals(signature, pinningLayoutSignature, StringComparison.Ordinal))
        {
            SyncActiveOverlayScrolls();
            return;
        }
        pinningLayoutSignature = signature;
        DetachDiscardedPinningOverlays(pinningLayer);
        pinningLayer.Children.Clear();
        pinnedRowOverlays.Clear();
        pinnedColumnOverlays.Clear();
        AddPinnedRows(origin);
        AddPinnedColumns(origin);
        AddPinnedIntersections(origin);
    }

    private static void DetachDiscardedPinningOverlays(Canvas layer)
    {
        foreach (var overlay in layer.Children.OfType<DataGridView>())
            overlay.DetachItemsSourceObserverForPinning();
    }

    private string CreatePinningLayoutSignature(Point origin)
    {
        if (pinningScrollViewer is null || pinningViewport is null) return string.Empty;
        var parts = new List<string> { $"V{pinningViewport.ActualWidth:F2},{pinningViewport.ActualHeight:F2}" };
        var (startRows, endRows) = ClassifyPinnedRows(origin);
        var startRowItems = startRows.Select(entry => entry.Item).ToHashSet();
        var endRowItems = endRows.Select(entry => entry.Item).ToHashSet();
        foreach (var item in pinnedRows.Where(pinnedRowMetrics.ContainsKey))
        {
            var metric = pinnedRowMetrics[item];
            var edge = startRowItems.Contains(item) ? 'S' : endRowItems.Contains(item) ? 'E' : 'N';
            parts.Add($"R{RuntimeHelpers.GetHashCode(item)}{edge}{metric.Height:F2}");
        }
        var (startColumns, endColumns) = ClassifyPinnedColumns(origin);
        var startColumnItems = startColumns.Select(entry => entry.Column).ToHashSet();
        var endColumnItems = endColumns.Select(entry => entry.Column).ToHashSet();
        foreach (var column in pinnedColumns.Where(column => column.Visibility == Visibility.Visible && pinnedColumnMetrics.ContainsKey(column)))
        {
            var metric = pinnedColumnMetrics[column];
            var edge = startColumnItems.Contains(column) ? 'S' : endColumnItems.Contains(column) ? 'E' : 'N';
            parts.Add($"C{RuntimeHelpers.GetHashCode(column)}{edge}{metric.Width:F2}" +
                $"{column.SortDirection?.ToString() ?? "None"}P{GetSortPriority(column)}");
        }
        return string.Join('|', parts);
    }

    private (List<(object Item, (double Offset, double Height) Metric)> Start,
        List<(object Item, (double Offset, double Height) Metric)> End) ClassifyPinnedRows(Point origin)
    {
        if (pinningScrollViewer is null || pinningViewport is null) return ([], []);
        var metrics = pinnedRows.Where(pinnedRowMetrics.ContainsKey)
            .Select(item => (Item: item, Metric: pinnedRowMetrics[item]))
            .OrderBy(entry => entry.Metric.Offset).ToList();
        var start = new List<(object Item, (double Offset, double Height) Metric)>();
        var occupiedStart = 0d;
        foreach (var entry in metrics)
        {
            var naturalStart = origin.Y + entry.Metric.Offset - pinningScrollViewer.VerticalOffset;
            if (naturalStart >= origin.Y + occupiedStart) continue;
            start.Add(entry);
            occupiedStart += entry.Metric.Height;
        }

        var startItems = start.Select(entry => entry.Item).ToHashSet();
        var end = new List<(object Item, (double Offset, double Height) Metric)>();
        var occupiedEnd = 0d;
        foreach (var entry in metrics.AsEnumerable().Reverse())
        {
            if (startItems.Contains(entry.Item)) continue;
            var naturalEnd = origin.Y + entry.Metric.Offset - pinningScrollViewer.VerticalOffset + entry.Metric.Height;
            if (naturalEnd <= origin.Y + pinningViewport.ActualHeight - occupiedEnd) continue;
            end.Add(entry);
            occupiedEnd += entry.Metric.Height;
        }
        return (start, end);
    }

    private (List<(DataGridColumn Column, (double Offset, double Width) Metric)> Start,
        List<(DataGridColumn Column, (double Offset, double Width) Metric)> End) ClassifyPinnedColumns(Point origin)
    {
        if (pinningScrollViewer is null || pinningViewport is null) return ([], []);
        var metrics = pinnedColumns.Where(column => column.Visibility == Visibility.Visible && pinnedColumnMetrics.ContainsKey(column))
            .Select(column => (Column: column, Metric: pinnedColumnMetrics[column]))
            .OrderBy(entry => entry.Metric.Offset).ToList();
        var start = new List<(DataGridColumn Column, (double Offset, double Width) Metric)>();
        var occupiedStart = 0d;
        foreach (var entry in metrics)
        {
            var naturalStart = origin.X + entry.Metric.Offset - pinningScrollViewer.HorizontalOffset;
            if (naturalStart >= origin.X + occupiedStart) continue;
            start.Add(entry);
            occupiedStart += entry.Metric.Width;
        }

        var startItems = start.Select(entry => entry.Column).ToHashSet();
        var end = new List<(DataGridColumn Column, (double Offset, double Width) Metric)>();
        var occupiedEnd = 0d;
        foreach (var entry in metrics.AsEnumerable().Reverse())
        {
            if (startItems.Contains(entry.Column)) continue;
            var naturalEnd = origin.X + entry.Metric.Offset - pinningScrollViewer.HorizontalOffset + entry.Metric.Width;
            if (naturalEnd <= origin.X + pinningViewport.ActualWidth - occupiedEnd) continue;
            end.Add(entry);
            occupiedEnd += entry.Metric.Width;
        }
        return (start, end);
    }

    private void SyncActiveOverlayScrolls()
    {
        if (pinningScrollViewer is null) return;
        foreach (var overlay in pinnedRowOverlays)
        {
            SyncOverlayRowHeights(overlay);
            SyncOverlayScrollNow(overlay, pinningScrollViewer.HorizontalOffset, 0);
        }
        foreach (var overlay in pinnedColumnOverlays)
        {
            var heightsChanged = SyncOverlayRowHeights(overlay);
            overlay.overlayNeedsAlignment = SyncOverlayScrollNow(overlay, 0, pinningScrollViewer.VerticalOffset) || heightsChanged;
        }
        // Settle all moving overlays together, rather than forcing a layout for each
        // column group and again for every individual alignment correction.
        if (pinnedColumnOverlays.Any(overlay => overlay.overlayNeedsAlignment)) UpdateLayout();
        var corrected = false;
        foreach (var overlay in pinnedColumnOverlays)
            corrected |= AlignPinnedColumnRows(overlay);
        if (corrected) UpdateLayout();
    }

    private void AddPinnedRows(Point origin)
    {
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null) return;
        var (start, end) = ClassifyPinnedRows(origin);

        var startHeight = start.Sum(entry => entry.Metric.Height);
        if (startHeight > 0)
        {
            var pixel = 1d / VisualTreeHelper.GetDpi(this).DpiScaleY;
            var boundaryTop = Math.Floor(origin.Y / pixel) * pixel - pixel;
            AddPinnedRowBackdrop(origin.X, boundaryTop, pinningViewport.ActualWidth,
                origin.Y + startHeight - boundaryTop + pixel, "Start");
            var boundary = new Border
            {
                Background = ResolveOpaqueBrush(HorizontalGridLinesBrush),
                IsHitTestVisible = false,
                Tag = "PinnedHeaderBoundary"
            };
            PlaceOverlay(boundary, origin.X, boundaryTop, pinningViewport.ActualWidth, pixel);
            Panel.SetZIndex(boundary, 3);
        }
        var endHeight = end.Sum(entry => entry.Metric.Height);
        if (endHeight > 0)
            AddPinnedRowBackdrop(origin.X, origin.Y + pinningViewport.ActualHeight - endHeight - 1,
                pinningViewport.ActualWidth, endHeight + 1, "End");

        var occupied = 0d;
        foreach (var entry in start)
        {
            AddPinnedRow(entry.Item, origin.X, origin.Y + occupied, pinningViewport.ActualWidth, entry.Metric.Height);
            AddPinnedRowSeparator(origin.X, origin.Y + occupied + entry.Metric.Height,
                pinningViewport.ActualWidth, entry.Item);
            occupied += entry.Metric.Height;
        }
        occupied = 0;
        foreach (var entry in end)
        {
            occupied += entry.Metric.Height;
            var top = origin.Y + pinningViewport.ActualHeight - occupied;
            AddPinnedRow(entry.Item, origin.X, top,
                pinningViewport.ActualWidth, entry.Metric.Height);
            AddPinnedRowSeparator(origin.X, top + entry.Metric.Height,
                pinningViewport.ActualWidth, entry.Item);
        }
        if (ShowPinnedBoundarySeparator && startHeight > 0)
            AddPinnedBoundarySeparator(origin.X, origin.Y + startHeight - PinnedBoundarySeparatorThickness,
                pinningViewport.ActualWidth, PinnedBoundarySeparatorThickness, "RowStart");
        if (ShowPinnedBoundarySeparator && endHeight > 0)
            AddPinnedBoundarySeparator(origin.X, origin.Y + pinningViewport.ActualHeight - endHeight,
                pinningViewport.ActualWidth, PinnedBoundarySeparatorThickness, "RowEnd");
    }

    private void AddPinnedRowSeparator(double left, double bottom, double width, object item)
    {
        if (pinningLayer is null || GridLinesVisibility is DataGridGridLinesVisibility.None or DataGridGridLinesVisibility.Vertical)
            return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var thickness = Math.Max(1d, 1d / dpi.DpiScaleY);
        var separator = new Border
        {
            Background = ResolveOpaqueBrush(HorizontalGridLinesBrush),
            IsHitTestVisible = false,
            Tag = $"PinnedRowSeparator:{RuntimeHelpers.GetHashCode(item)}"
        };
        PlaceOverlay(separator, left, bottom - thickness, width, thickness);
        Panel.SetZIndex(separator, 3);
    }

    private void AddPinnedRowBackdrop(double left, double top, double width, double height, string edge)
    {
        if (pinningLayer is null) return;
        var backdrop = new Border
        {
            Background = ResolvePinnedRowBackground(null),
            IsHitTestVisible = false,
            Tag = $"PinnedRowBackdrop:{edge}"
        };
        PlaceOverlay(backdrop, left, top, width, height);
        // Cover moving column cells as well as the source rows. Fixed rows are
        // drawn above this surface, so antialiasing cannot expose moving text.
        Panel.SetZIndex(backdrop, 1);
    }

    private void AddPinnedColumns(Point origin)
    {
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null) return;
        var (start, end) = ClassifyPinnedColumns(origin);

        var startWidth = start.Sum(entry => entry.Metric.Width);
        if (startWidth > 0)
            AddPinnedColumnBackdrop(origin.X, 0, startWidth + 1,
                origin.Y + pinningViewport.ActualHeight, "Start");
        var endWidth = end.Sum(entry => entry.Metric.Width);
        if (endWidth > 0)
            AddPinnedColumnBackdrop(origin.X + pinningViewport.ActualWidth - endWidth - 1, 0,
                endWidth + 1, origin.Y + pinningViewport.ActualHeight, "End");

        if (start.Count > 0)
        {
            AddPinnedColumnGroup(start, origin.X, startWidth, origin.Y + pinningViewport.ActualHeight);
        }
        if (end.Count > 0)
        {
            AddPinnedColumnGroup(end.OrderBy(entry => entry.Metric.Offset).ToList(),
                origin.X + pinningViewport.ActualWidth - endWidth, endWidth,
                origin.Y + pinningViewport.ActualHeight);
        }
        if (ShowPinnedBoundarySeparator && startWidth > 0)
            AddPinnedBoundarySeparator(origin.X + startWidth - PinnedBoundarySeparatorThickness, 0,
                PinnedBoundarySeparatorThickness, origin.Y + pinningViewport.ActualHeight, "ColumnStart");
        if (ShowPinnedBoundarySeparator && endWidth > 0)
            AddPinnedBoundarySeparator(origin.X + pinningViewport.ActualWidth - endWidth, 0,
                PinnedBoundarySeparatorThickness, origin.Y + pinningViewport.ActualHeight, "ColumnEnd");
    }

    private void AddPinnedBoundarySeparator(double left, double top, double width, double height, string edge)
    {
        if (pinningLayer is null) return;
        var separator = new Border
        {
            Background = PinnedBoundarySeparatorBrush,
            IsHitTestVisible = false,
            Tag = $"PinnedBoundarySeparator:{edge}"
        };
        PlaceOverlay(separator, left, top, width, height);
        Panel.SetZIndex(separator, 4);
    }

    private void AddPinnedColumnBackdrop(double left, double top, double width, double height, string edge)
    {
        if (pinningLayer is null) return;
        var backdrop = new Border
        {
            Background = ResolvePinnedRowBackground(null),
            IsHitTestVisible = false,
            Tag = $"PinnedColumnBackdrop:{edge}"
        };
        PlaceOverlay(backdrop, left, top, width, height);
        Panel.SetZIndex(backdrop, -1);
    }

    private void AddPinnedRow(object item, double left, double top, double width, double height)
    {
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null) return;
        var overlay = CreateOverlayGrid(DataGridHeadersVisibility.None, new[] { item });
        overlay.GridLinesVisibility = DataGridGridLinesVisibility.None;
        ApplyPinnedRowBackground(overlay, item);
        foreach (var column in Columns.Where(column => column.Visibility == Visibility.Visible).OrderBy(column => column.DisplayIndex))
            overlay.Columns.Add(CloneColumn(column));
        PlaceOverlay(overlay, left, top, width, height);
        Panel.SetZIndex(overlay, 2);
        pinnedRowOverlays.Add(overlay);
        SyncOverlayScroll(overlay, pinningScrollViewer.HorizontalOffset, 0);
    }

    private void AddPinnedIntersections(Point origin)
    {
        if (pinningViewport is null) return;
        var (startRows, endRows) = ClassifyPinnedRows(origin);
        var (startColumns, endColumns) = ClassifyPinnedColumns(origin);
        var rows = new List<(object Item, double Top, double Height)>();
        var occupied = 0d;
        foreach (var entry in startRows)
        {
            rows.Add((entry.Item, origin.Y + occupied, entry.Metric.Height));
            occupied += entry.Metric.Height;
        }
        occupied = 0;
        foreach (var entry in endRows)
        {
            occupied += entry.Metric.Height;
            rows.Add((entry.Item, origin.Y + pinningViewport.ActualHeight - occupied, entry.Metric.Height));
        }

        foreach (var row in rows)
        {
            var startWidth = startColumns.Sum(entry => entry.Metric.Width);
            if (startColumns.Count > 0)
                AddPinnedIntersectionGroup(row.Item, startColumns, origin.X, row.Top, startWidth, row.Height);
            var orderedEnd = endColumns.OrderBy(entry => entry.Metric.Offset).ToList();
            var endWidth = orderedEnd.Sum(entry => entry.Metric.Width);
            if (orderedEnd.Count > 0)
                AddPinnedIntersectionGroup(row.Item, orderedEnd,
                    origin.X + pinningViewport.ActualWidth - endWidth, row.Top, endWidth, row.Height);
        }
    }

    private void AddPinnedIntersectionGroup(object item,
        IReadOnlyList<(DataGridColumn Column, (double Offset, double Width) Metric)> columns,
        double left, double top, double width, double height)
    {
        var overlay = CreateOverlayGrid(DataGridHeadersVisibility.None, new[] { item });
        overlay.GridLinesVisibility = DataGridGridLinesVisibility.None;
        ApplyPinnedRowBackground(overlay, item);
        foreach (var entry in columns) overlay.Columns.Add(CloneColumn(entry.Column));
        PlaceOverlay(overlay, left, top, width, height);
        Panel.SetZIndex(overlay, 2);
    }

    private void ApplyPinnedRowBackground(DataGridView overlay, object item)
    {
        var brush = pinnedRowBackgrounds.TryGetValue(item, out var captured) && IsOpaqueBrush(captured)
            ? captured
            : ResolvePinnedRowBackground(null);
        overlay.Background = brush;
        overlay.RowBackground = brush;
        overlay.AlternatingRowBackground = brush;
        overlay.RowStyle = CreateOpaqueStyle(typeof(DataGridRow), RowStyle, brush);
        overlay.CellStyle = CreateOpaqueStyle(typeof(DataGridCell), CellStyle, brush);
    }

    private Brush ResolvePinnedRowBackground(Brush? rowBrush)
    {
        var surface = IsOpaqueBrush(Background)
            ? Background
            : TryFindResource("DataGridView.Surface") as Brush ?? new SolidColorBrush(Color.FromRgb(0x35, 0x37, 0x3C));
        if (rowBrush is not SolidColorBrush rowColor || rowColor.Opacity <= 0 || rowColor.Color.A == 0)
            return surface;
        if (IsOpaqueBrush(rowColor)) return rowColor;
        if (surface is not SolidColorBrush surfaceColor) return surface;

        var alpha = rowColor.Color.A / 255d * rowColor.Opacity;
        byte Blend(byte foreground, byte background) => (byte)Math.Round(foreground * alpha + background * (1 - alpha));
        return new SolidColorBrush(Color.FromRgb(
            Blend(rowColor.Color.R, surfaceColor.Color.R),
            Blend(rowColor.Color.G, surfaceColor.Color.G),
            Blend(rowColor.Color.B, surfaceColor.Color.B)));
    }

    private Brush ResolveOpaqueBrush(Brush? brush)
    {
        if (IsOpaqueBrush(brush)) return brush!;
        return ResolvePinnedRowBackground(brush);
    }

    private double ResolveOverlayRowHeight()
    {
        if (double.IsFinite(RowHeight) && RowHeight > 0) return RowHeight;
        return renderedRowHeights.Count > 0 ? renderedRowHeights.Values.Max() : double.NaN;
    }

    private static Style CreateOpaqueStyle(Type targetType, Style? basedOn, Brush background)
    {
        var style = new Style(targetType, basedOn);
        style.Setters.Add(new Setter(Control.BackgroundProperty, background));
        return style;
    }

    private static bool IsOpaqueBrush(Brush? brush) => brush is not null && brush.Opacity >= 1 &&
        (brush is not SolidColorBrush solid || solid.Color.A == byte.MaxValue);

    private void AddPinnedColumnGroup(
        IReadOnlyList<(DataGridColumn Column, (double Offset, double Width) Metric)> columns,
        double left, double width, double height)
    {
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null) return;
        var overlay = CreateOverlayGrid(DataGridHeadersVisibility.Column, ItemsSource ?? Items);
        foreach (var entry in columns) overlay.Columns.Add(CloneColumn(entry.Column));
        PlaceOverlay(overlay, left, 0, width, height);
        pinnedColumnOverlays.Add(overlay);
        SyncOverlayScroll(overlay, 0, pinningScrollViewer.VerticalOffset);
        var headerHeight = Math.Max(0, pinningViewport.TranslatePoint(new Point(), pinningLayer).Y);
        var occupied = 0d;
        foreach (var entry in columns)
        {
            AddPinnedColumnHeaderHitTarget(entry.Column, left + occupied, entry.Metric.Width, headerHeight);
            occupied += entry.Metric.Width;
        }
    }

    private void AddPinnedColumnHeaderHitTarget(DataGridColumn column, double left, double width, double height)
    {
        if (pinningLayer is null || height <= 0) return;
        var target = new Border
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent,
            Tag = column
        };
        target.PreviewMouseDown += (_, args) => args.Handled = true;
        target.MouseLeftButtonUp += (_, args) =>
        {
            args.Handled = true;
            SortFromPinnedColumnHeader(column);
        };
        target.MouseRightButtonUp += (_, args) =>
        {
            args.Handled = true;
            var menu = CreateColumnHeaderMenu(column);
            menu.PlacementTarget = target;
            menu.IsOpen = true;
        };
        Canvas.SetLeft(target, left);
        Canvas.SetTop(target, 0);
        Panel.SetZIndex(target, 1);
        pinningLayer.Children.Add(target);
    }

    private void SortFromPinnedColumnHeader(DataGridColumn column)
    {
        if (!CanUserSortColumns || !column.CanUserSort || string.IsNullOrWhiteSpace(column.SortMemberPath)) return;
        if (IsMultiColumnSortEnabled && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            RemoveSort(column);
            return;
        }
        var direction = column.SortDirection == System.ComponentModel.ListSortDirection.Ascending
            ? System.ComponentModel.ListSortDirection.Descending
            : System.ComponentModel.ListSortDirection.Ascending;
        ApplySort(column, direction, IsMultiColumnSortEnabled && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    private DataGridView CreateOverlayGrid(DataGridHeadersVisibility headers, System.Collections.IEnumerable source)
    {
        var overlay = new DataGridView
        {
            ItemsSource = source,
            HeadersVisibility = headers,
            // Read-only at the grid level so the overlay can never start its own edit
            // (defense in depth for any input path, e.g. keyboard, that isn't a pointer
            // press). CloneColumn separately keeps checkbox/combo columns' own IsReadOnly
            // false so they don't render as visually disabled — HandlePinnedCellMouseDown
            // intercepts every pointer press on a pinned cell during the tunneling
            // PreviewMouseDown pass (fired on the real grid, an ancestor of this overlay)
            // and marks it Handled before it can ever reach those elements' own click
            // handling, regardless of their own IsReadOnly/IsEnabled state.
            IsReadOnly = true,
            IsHitTestVisible = true,
            Background = Background,
            Foreground = Foreground,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(0),
            RowBackground = RowBackground,
            AlternatingRowBackground = AlternatingRowBackground,
            AlternationCount = AlternationCount,
            HorizontalGridLinesBrush = HorizontalGridLinesBrush,
            GridLinesVisibility = GridLinesVisibility,
            CellPadding = CellPadding,
            Density = Density,
            ColumnHeaderHeight = headers.HasFlag(DataGridHeadersVisibility.Column) ? ColumnHeaderHeight : 0,
            RowHeight = ResolveOverlayRowHeight(),
            CanUserReorderColumns = false,
            CanUserResizeColumns = false,
            CanUserSortColumns = CanUserSortColumns,
            ShowSortIndicators = ShowSortIndicators,
            SortIconSize = SortIconSize,
            SortIconBrush = SortIconBrush,
            ActiveSortIconBrush = ActiveSortIconBrush,
            UnsortedIcon = UnsortedIcon,
            AscendingSortIcon = AscendingSortIcon,
            DescendingSortIcon = DescendingSortIcon,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            CornerRadius = new CornerRadius(0)
        };
        overlay.LoadingRow += (_, args) =>
        {
            if (args.Row.Item is not null && renderedRowHeights.TryGetValue(args.Row.Item, out var height))
                args.Row.Height = height;
        };
        return overlay;
    }

    private bool SyncOverlayRowHeights(DataGridView overlay)
    {
        var changed = false;
        foreach (var row in VisualChildren<DataGridRow>(overlay))
            if (row.Item is not null && renderedRowHeights.TryGetValue(row.Item, out var height) && row.Height != height)
            {
                row.Height = height;
                changed = true;
            }
        return changed;
    }

    private bool AlignPinnedColumnRows(DataGridView overlay)
    {
        if (pinningLayer is null || overlay.Template.FindName("DG_ScrollViewer", overlay) is not ScrollViewer viewer)
            return false;
        if (pinningViewport is null) return false;
        var viewportTop = pinningViewport.TranslatePoint(new Point(), pinningLayer).Y;
        foreach (var sourceRow in VisualChildren<DataGridRow>(pinningViewport))
        {
            var sourceTop = sourceRow.TranslatePoint(new Point(), pinningLayer).Y;
            if (sourceTop + sourceRow.ActualHeight <= viewportTop ||
                sourceTop >= viewportTop + pinningViewport.ActualHeight) continue;
            if (overlay.ItemContainerGenerator.ContainerFromItem(sourceRow.Item) is not DataGridRow overlayRow) continue;
            var overlayTop = overlayRow.TranslatePoint(new Point(), pinningLayer).Y;
            var correction = overlayTop - sourceTop;
            var target = Math.Clamp(viewer.VerticalOffset + correction, 0, viewer.ScrollableHeight);
            if (Math.Abs(target - viewer.VerticalOffset) <= 0.5 / VisualTreeHelper.GetDpi(overlay).DpiScaleY) return false;
            viewer.ScrollToVerticalOffset(target);
            return true;
        }
        return false;
    }

    private void PlaceOverlay(FrameworkElement overlay, double left, double top, double width, double height)
    {
        if (pinningLayer is null) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        static double Round(double value, double scale) => Math.Round(value * scale) / scale;
        // Keep cell layout consistent with the source grid. Rounding only the
        // copied rows changes fractional heights and accumulates vertical drift.
        // Backdrops and boundary separators still use physical pixel rounding.
        overlay.UseLayoutRounding = overlay is DataGridView ? UseLayoutRounding : true;
        overlay.SnapsToDevicePixels = overlay is DataGridView ? SnapsToDevicePixels : true;
        var preserveBounds = overlay is DataGridView && !UseLayoutRounding;
        overlay.Width = Math.Max(0, preserveBounds ? width : Round(width, dpi.DpiScaleX));
        overlay.Height = Math.Max(0, preserveBounds ? height : Round(height, dpi.DpiScaleY));
        Canvas.SetLeft(overlay, preserveBounds ? left : Round(left, dpi.DpiScaleX));
        Canvas.SetTop(overlay, preserveBounds ? top : Round(top, dpi.DpiScaleY));
        pinningLayer.Children.Add(overlay);
    }

    private void SyncOverlayScroll(DataGridView overlay, double horizontalOffset, double verticalOffset)
    {
        overlay.Loaded += (_, _) =>
        {
            if (overlay.Template.FindName("DG_ScrollViewer", overlay) is not ScrollViewer viewer) return;
            SyncOverlayRowHeights(overlay);
            overlay.UpdateLayout();
            SyncOverlayScrollNow(overlay, horizontalOffset, verticalOffset);
            overlay.UpdateLayout();
            if (overlay.HeadersVisibility.HasFlag(DataGridHeadersVisibility.Column) && AlignPinnedColumnRows(overlay))
                overlay.UpdateLayout();
        };
    }

    private static bool SyncOverlayScrollNow(DataGridView overlay, double horizontalOffset, double verticalOffset)
    {
        if (overlay.Template.FindName("DG_ScrollViewer", overlay) is not ScrollViewer viewer) return false;
        var changed = false;
        if (viewer.HorizontalOffset != horizontalOffset)
        {
            viewer.ScrollToHorizontalOffset(horizontalOffset);
            changed = true;
        }
        // Preserve the alignment offset already established for variable-height
        // rows; resetting to the source's absolute offset would undo it each frame.
        if (overlay.lastOverlayVerticalOffset != verticalOffset)
        {
            var target = double.IsNaN(overlay.lastOverlayVerticalOffset)
                ? verticalOffset
                : viewer.VerticalOffset + verticalOffset - overlay.lastOverlayVerticalOffset;
            overlay.lastOverlayVerticalOffset = verticalOffset;
            target = Math.Clamp(target, 0, viewer.ScrollableHeight);
            if (viewer.VerticalOffset != target)
            {
                viewer.ScrollToVerticalOffset(target);
                changed = true;
            }
        }
        return changed;
    }

    private static DataGridColumn CloneColumn(DataGridColumn source)
    {
        DataGridColumn clone = source switch
        {
            DataGridTextColumn text => new DataGridTextColumn
            {
                Binding = text.Binding,
                ElementStyle = text.ElementStyle,
                EditingElementStyle = text.EditingElementStyle
            },
            DataGridCheckBoxColumn checkBox => new DataGridCheckBoxColumn
            {
                Binding = checkBox.Binding,
                ElementStyle = checkBox.ElementStyle,
                EditingElementStyle = checkBox.EditingElementStyle
            },
            DataGridComboBoxColumn comboBox => new DataGridComboBoxColumn
            {
                SelectedItemBinding = comboBox.SelectedItemBinding,
                SelectedValueBinding = comboBox.SelectedValueBinding,
                TextBinding = comboBox.TextBinding,
                ItemsSource = comboBox.ItemsSource,
                SelectedValuePath = comboBox.SelectedValuePath,
                DisplayMemberPath = comboBox.DisplayMemberPath
            },
            DataGridTemplateColumn template => new DataGridTemplateColumn
            {
                CellTemplate = template.CellTemplate,
                CellEditingTemplate = template.CellEditingTemplate,
                CellTemplateSelector = template.CellTemplateSelector,
                CellEditingTemplateSelector = template.CellEditingTemplateSelector
            },
            DataGridHyperlinkColumn hyperlink => new DataGridHyperlinkColumn
            {
                Binding = hyperlink.Binding,
                ContentBinding = hyperlink.ContentBinding,
                TargetName = hyperlink.TargetName
            },
            _ => new DataGridTextColumn { Binding = new Binding() }
        };
        clone.Header = source.Header;
        clone.HeaderTemplate = source.HeaderTemplate;
        clone.HeaderTemplateSelector = source.HeaderTemplateSelector;
        clone.HeaderStyle = source.HeaderStyle;
        clone.CellStyle = source.CellStyle;
        clone.Width = new DataGridLength(Math.Max(1, source.ActualWidth));
        clone.MinWidth = 0;
        clone.MaxWidth = double.PositiveInfinity;
        // DataGridCheckBoxColumn/DataGridComboBoxColumn tie their display element's
        // IsEnabled directly to the column's own IsReadOnly, unlike text/template columns
        // which just stop offering double-click-to-edit when read-only. Give those two an
        // explicit IsReadOnly matching the real column, so they look exactly as
        // enabled/disabled as the real column instead of always inheriting the overlay
        // grid's IsReadOnly=true and rendering visually disabled. Every other column type
        // is left unset, so it inherits the overlay grid's IsReadOnly=true as before.
        // HandlePinnedCellMouseDown's click interception blocks direct interaction on any
        // pinned cell regardless of IsReadOnly, so this stays safe either way.
        if (clone is DataGridCheckBoxColumn or DataGridComboBoxColumn) clone.IsReadOnly = source.IsReadOnly;
        clone.CanUserSort = source.CanUserSort;
        clone.SortMemberPath = source.SortMemberPath;
        clone.SortDirection = source.SortDirection;
        clone.SetValue(SortPriorityPropertyKey, GetSortPriority(source));
        clone.SetValue(OriginalPinnedColumnProperty, source);
        return clone;
    }

    // Bring the real cell into view before dispatching input so selection, editing,
    // validation and keyboard navigation continue to be owned by the original grid.
    private bool HandlePinnedCellMouseDown(MouseButtonEventArgs args)
    {
        var cell = FindAncestor<DataGridCell>(args.OriginalSource as DependencyObject);
        if (cell?.Column.GetValue(OriginalPinnedColumnProperty) is not DataGridColumn column ||
            !Columns.Contains(column)) return false;
        var row = FindAncestor<DataGridRow>(cell);
        if (row is null || !Items.Contains(row.Item)) return false;
        args.Handled = true;
        if (args.ChangedButton == MouseButton.Right)
        {
            var menu = CreateRowPinMenu(row.Item, column);
            if (menu.Items.Count > 0)
            {
                menu.PlacementTarget = cell;
                menu.Placement = PlacementMode.MousePoint;
                cell.ContextMenu = menu;
                menu.IsOpen = true;
            }
            return true;
        }
        if (args.ChangedButton != MouseButton.Left) return true;
        if (SelectionBehavior == DataGridViewSelectionBehavior.None)
        {
            Focus();
            return true;
        }
        ScrollIntoView(row.Item, column);
        UpdateLayout();
        if (column.GetCellContent(row.Item)?.Parent is not DataGridCell originalCell) return true;
        originalCell.RaiseEvent(new MouseButtonEventArgs(args.MouseDevice, args.Timestamp, args.ChangedButton)
        {
            RoutedEvent = Mouse.MouseDownEvent,
            Source = originalCell
        });
        if (args.ClickCount == 2 && !IsReadOnly && !column.IsReadOnly) BeginEdit(args);
        QueuePinningVisualUpdate();
        return true;
    }

    private static IEnumerable<T> VisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            // Containers are leaves for this search: their cell templates can contain
            // arbitrarily large visual trees, including other grids.
            if (child is T match) yield return match;
            else if (child is not DataGridView)
                foreach (var descendant in VisualChildren<T>(child)) yield return descendant;
        }
    }
}
