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
    private readonly Dictionary<object, double> renderedRowHeights = [];
    private readonly Dictionary<DataGridColumn, (double Offset, double Width)> pinnedColumnMetrics = [];
    private bool pinningUpdatePending;
    private string pinningLayoutSignature = string.Empty;
    private readonly List<DataGridView> pinnedRowOverlays = [];
    private readonly List<DataGridView> pinnedColumnOverlays = [];

    private void InitializePinningVisuals()
    {
        if (pinningScrollViewer is not null) pinningScrollViewer.ScrollChanged -= OnPinningScrollChanged;
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
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null) return;
        foreach (var row in VisualChildren<DataGridRow>(this))
            if (ItemsControl.ItemsControlFromItemContainer(row) == this && row.Item is not null && row.ActualHeight > 0)
                renderedRowHeights[row.Item] = row.ActualHeight;
        var viewportOrigin = pinningViewport.TranslatePoint(new Point(), pinningLayer);
        foreach (var item in pinnedRows)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row || row.ActualHeight <= 0) continue;
            var position = row.TranslatePoint(new Point(), pinningLayer);
            pinnedRowMetrics[item] = (position.Y - viewportOrigin.Y + pinningScrollViewer.VerticalOffset, row.ActualHeight);
            pinnedRowBackgrounds[item] = ResolvePinnedRowBackground(row.Background);
        }
        foreach (var header in VisualChildren<DataGridColumnHeader>(this))
        {
            if (!pinnedColumns.Contains(header.Column) || header.ActualWidth <= 0) continue;
            var position = header.TranslatePoint(new Point(), pinningLayer);
            pinnedColumnMetrics[header.Column] = (position.X - viewportOrigin.X + pinningScrollViewer.HorizontalOffset, header.ActualWidth);
        }
    }

    private void UpdatePinningVisuals()
    {
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
        pinningLayer.Children.Clear();
        pinnedRowOverlays.Clear();
        pinnedColumnOverlays.Clear();
        AddPinnedRows(origin);
        AddPinnedColumns(origin);
        AddPinnedIntersections(origin);
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
        foreach (var column in pinnedColumns.Where(pinnedColumnMetrics.ContainsKey))
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
        var metrics = pinnedColumns.Where(pinnedColumnMetrics.ContainsKey)
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
            SyncOverlayRowHeights(overlay);
            SyncOverlayScrollNow(overlay, 0, pinningScrollViewer.VerticalOffset);
            overlay.UpdateLayout();
            AlignPinnedColumnRows(overlay);
        }
    }

    private void AddPinnedRows(Point origin)
    {
        if (pinningLayer is null || pinningScrollViewer is null || pinningViewport is null) return;
        var (start, end) = ClassifyPinnedRows(origin);

        var startHeight = start.Sum(entry => entry.Metric.Height);
        if (startHeight > 0)
            AddPinnedRowBackdrop(origin.X, origin.Y, pinningViewport.ActualWidth, startHeight + 1, "Start");
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
        Panel.SetZIndex(backdrop, -1);
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
            AddPinnedColumnGroup(start, origin.X, startWidth, origin.Y + pinningViewport.ActualHeight);
        if (end.Count > 0)
            AddPinnedColumnGroup(end.OrderBy(entry => entry.Metric.Offset).ToList(),
                origin.X + pinningViewport.ActualWidth - endWidth, endWidth,
                origin.Y + pinningViewport.ActualHeight);
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
            IsReadOnly = IsReadOnly,
            IsHitTestVisible = false,
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

    private void SyncOverlayRowHeights(DataGridView overlay)
    {
        foreach (var entry in renderedRowHeights)
            if (overlay.ItemContainerGenerator.ContainerFromItem(entry.Key) is DataGridRow row)
                row.Height = entry.Value;
    }

    private void AlignPinnedColumnRows(DataGridView overlay)
    {
        if (pinningLayer is null || overlay.Template.FindName("DG_ScrollViewer", overlay) is not ScrollViewer viewer)
            return;
        foreach (var item in renderedRowHeights.Keys)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow sourceRow ||
                overlay.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow overlayRow) continue;
            var sourceTop = sourceRow.TranslatePoint(new Point(), pinningLayer).Y;
            var overlayTop = overlayRow.TranslatePoint(new Point(), pinningLayer).Y;
            var correction = overlayTop - sourceTop;
            if (Math.Abs(correction) <= 0.1) return;
            viewer.ScrollToVerticalOffset(Math.Clamp(viewer.VerticalOffset + correction, 0, viewer.ScrollableHeight));
            overlay.UpdateLayout();
            return;
        }
    }

    private void PlaceOverlay(FrameworkElement overlay, double left, double top, double width, double height)
    {
        if (pinningLayer is null) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        static double Round(double value, double scale) => Math.Round(value * scale) / scale;
        overlay.UseLayoutRounding = true;
        overlay.SnapsToDevicePixels = true;
        overlay.Width = Math.Max(0, Round(width, dpi.DpiScaleX));
        overlay.Height = Math.Max(0, Round(height, dpi.DpiScaleY));
        Canvas.SetLeft(overlay, Round(left, dpi.DpiScaleX));
        Canvas.SetTop(overlay, Round(top, dpi.DpiScaleY));
        pinningLayer.Children.Add(overlay);
    }

    private void SyncOverlayScroll(DataGridView overlay, double horizontalOffset, double verticalOffset)
    {
        overlay.Loaded += (_, _) =>
        {
            if (overlay.Template.FindName("DG_ScrollViewer", overlay) is not ScrollViewer viewer) return;
            SyncOverlayRowHeights(overlay);
            overlay.UpdateLayout();
            viewer.ScrollToHorizontalOffset(horizontalOffset);
            viewer.ScrollToVerticalOffset(verticalOffset);
            overlay.UpdateLayout();
            if (overlay.HeadersVisibility.HasFlag(DataGridHeadersVisibility.Column))
                AlignPinnedColumnRows(overlay);
        };
    }

    private static void SyncOverlayScrollNow(DataGridView overlay, double horizontalOffset, double verticalOffset)
    {
        if (overlay.Template.FindName("DG_ScrollViewer", overlay) is not ScrollViewer viewer) return;
        viewer.ScrollToHorizontalOffset(horizontalOffset);
        viewer.ScrollToVerticalOffset(verticalOffset);
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
        clone.IsReadOnly = source.IsReadOnly;
        clone.CanUserSort = source.CanUserSort;
        clone.SortMemberPath = source.SortMemberPath;
        clone.SortDirection = source.SortDirection;
        clone.SetValue(SortPriorityPropertyKey, GetSortPriority(source));
        return clone;
    }

    private static IEnumerable<T> VisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in VisualChildren<T>(child)) yield return descendant;
        }
    }
}
