using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DLH.Controls.Wpf;

/// <summary>
/// A menu item whose primary area cycles through values and whose arrow opens
/// a submenu for direct selection.
/// </summary>
public class ChoiceMenuItem : MenuItem
{
    private bool isSynchronizing;

    public ChoiceMenuItem()
    {
        SetCurrentValue(StaysOpenOnClickProperty, true);
        AddHandler(ClickEvent, new RoutedEventHandler(OnDescendantClick));
    }

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(ChoiceMenuItem),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionPropertyChanged),
        value => value is int index && index >= -1);

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty) is int value ? value : -1;
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(ChoiceMenuItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(
        nameof(SelectedValue), typeof(object), typeof(ChoiceMenuItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty.Register(
        nameof(SelectedValuePath), typeof(string), typeof(ChoiceMenuItem),
        new PropertyMetadata(string.Empty, OnPresentationPropertyChanged));

    public string SelectedValuePath
    {
        get => GetValue(SelectedValuePathProperty) as string ?? string.Empty;
        set => SetValue(SelectedValuePathProperty, value);
    }

    public static readonly DependencyProperty IconMemberPathProperty = DependencyProperty.Register(
        nameof(IconMemberPath), typeof(string), typeof(ChoiceMenuItem),
        new PropertyMetadata(string.Empty, OnPresentationPropertyChanged));

    public string IconMemberPath
    {
        get => GetValue(IconMemberPathProperty) as string ?? string.Empty;
        set => SetValue(IconMemberPathProperty, value);
    }

    public static readonly DependencyProperty CycleDirectionProperty = DependencyProperty.Register(
        nameof(CycleDirection), typeof(ChoiceCycleDirection), typeof(ChoiceMenuItem),
        new PropertyMetadata(ChoiceCycleDirection.Forward));

    public ChoiceCycleDirection CycleDirection
    {
        get => GetValue(CycleDirectionProperty) is ChoiceCycleDirection value ? value : ChoiceCycleDirection.Forward;
        set => SetValue(CycleDirectionProperty, value);
    }

    public static readonly DependencyProperty IsCycleWrappingEnabledProperty = DependencyProperty.Register(
        nameof(IsCycleWrappingEnabled), typeof(bool), typeof(ChoiceMenuItem), new PropertyMetadata(true));

    public bool IsCycleWrappingEnabled
    {
        get => GetValue(IsCycleWrappingEnabledProperty) is true;
        set => SetValue(IsCycleWrappingEnabledProperty, value);
    }

    public static readonly DependencyProperty DropDownButtonWidthProperty = DependencyProperty.Register(
        nameof(DropDownButtonWidth), typeof(double), typeof(ChoiceMenuItem), new PropertyMetadata(24d),
        value => value is double width && double.IsFinite(width) && width >= 0);

    public double DropDownButtonWidth
    {
        get => GetValue(DropDownButtonWidthProperty) is double value ? value : 24d;
        set => SetValue(DropDownButtonWidthProperty, value);
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MenuItem;

    protected override DependencyObject GetContainerForItemOverride() => new MenuItem();

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is not MenuItem container || item is MenuItem) return;
        container.Header = GetContent(item);
        container.Icon = GetIcon(item);
        if (item is ChoiceMenuOption option) container.IsEnabled = option.IsEnabled;
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        if (SelectedIndex >= Items.Count) SetCurrentValue(SelectedIndexProperty, Items.Count - 1);
        else SynchronizeFromIndex();
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (FindMenuItem(e.OriginalSource as DependencyObject) != this)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            return;
        }

        if (HasItems && e.GetPosition(this).X >= Math.Max(0, ActualWidth - DropDownButtonWidth))
            SetCurrentValue(IsSubmenuOpenProperty, true);
        else
            OnClick();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            OnClick();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Right && HasItems)
        {
            SetCurrentValue(IsSubmenuOpenProperty, true);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnClick()
    {
        CycleSelection();
        base.OnClick();
    }

    public void CycleSelection()
    {
        if (Items.Count == 0) return;
        var step = CycleDirection == ChoiceCycleDirection.Forward ? 1 : -1;
        var candidate = SelectedIndex < 0 ? (step > 0 ? 0 : Items.Count - 1) : SelectedIndex + step;
        if (IsCycleWrappingEnabled)
            candidate = (candidate % Items.Count + Items.Count) % Items.Count;
        else
            candidate = Math.Clamp(candidate, 0, Items.Count - 1);

        var start = candidate;
        while (!IsSelectable(Items[candidate]))
        {
            candidate += step;
            if (candidate < 0 || candidate >= Items.Count)
            {
                if (!IsCycleWrappingEnabled) return;
                candidate = candidate < 0 ? Items.Count - 1 : 0;
            }
            if (candidate == start) return;
        }
        SetCurrentValue(SelectedIndexProperty, candidate);
    }

    private void OnDescendantClick(object sender, RoutedEventArgs e)
    {
        var container = FindMenuItem(e.OriginalSource as DependencyObject);
        if (container is null || container == this || ItemsControl.ItemsControlFromItemContainer(container) != this) return;
        var item = ItemContainerGenerator.ItemFromContainer(container);
        var index = Items.IndexOf(item == DependencyProperty.UnsetValue ? container : item);
        if (index >= 0) SetCurrentValue(SelectedIndexProperty, index);
    }

    private static void OnSelectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs _) =>
        ((ChoiceMenuItem)d).SynchronizeFromIndex();

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var owner = (ChoiceMenuItem)d;
        if (owner.isSynchronizing) return;
        owner.SetCurrentValue(SelectedIndexProperty, owner.Items.IndexOf(e.NewValue));
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var owner = (ChoiceMenuItem)d;
        if (owner.isSynchronizing) return;
        for (var index = 0; index < owner.Items.Count; index++)
            if (Equals(owner.ResolveValue(owner.Items[index]), e.NewValue))
            {
                owner.SetCurrentValue(SelectedIndexProperty, index);
                return;
            }
    }

    private static void OnPresentationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs _) =>
        ((ChoiceMenuItem)d).SynchronizeFromIndex();

    private void SynchronizeFromIndex()
    {
        if (isSynchronizing) return;
        isSynchronizing = true;
        try
        {
            var item = SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;
            SetCurrentValue(SelectedItemProperty, item);
            SetCurrentValue(SelectedValueProperty, item is null ? null : ResolveValue(item));
            SetCurrentValue(InputGestureTextProperty, item is null ? string.Empty : GetContent(item)?.ToString() ?? string.Empty);
            SetCurrentValue(IconProperty, item is null ? null : GetIcon(item));
        }
        finally { isSynchronizing = false; }
    }

    private object? GetContent(object item) => item switch
    {
        ChoiceMenuOption option => option.Content,
        MenuItem menuItem => menuItem.Header,
        _ when !string.IsNullOrWhiteSpace(DisplayMemberPath) => ResolvePath(item, DisplayMemberPath),
        _ => item
    };

    private object? GetIcon(object item) => item switch
    {
        ChoiceMenuOption option => option.Icon,
        MenuItem menuItem => menuItem.Icon is FrameworkElement ? null : menuItem.Icon,
        _ when !string.IsNullOrWhiteSpace(IconMemberPath) => ResolvePath(item, IconMemberPath),
        _ => null
    };

    private object? ResolveValue(object item) => item switch
    {
        ChoiceMenuOption option when string.IsNullOrWhiteSpace(SelectedValuePath) => option.Value,
        _ when !string.IsNullOrWhiteSpace(SelectedValuePath) => ResolvePath(item, SelectedValuePath),
        _ => item
    };

    private static bool IsSelectable(object item) => item switch
    {
        ChoiceMenuOption option => option.IsEnabled,
        MenuItem menuItem => menuItem.IsEnabled,
        _ => true
    };

    private static object? ResolvePath(object? source, string path)
    {
        foreach (var memberName in path.Split('.'))
        {
            if (source is null) return null;
            var descriptor = TypeDescriptor.GetProperties(source)[memberName];
            source = descriptor is not null
                ? descriptor.GetValue(source)
                : source.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
        }
        return source;
    }

    private static MenuItem? FindMenuItem(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is MenuItem item) return item;
            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }
}
