using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

public partial class DataGridView
{
    private ICollectionView? groupedView;
    private PropertyGroupDescription? activeGroup;

    public static readonly DependencyProperty IsGroupingEnabledProperty = DependencyProperty.Register(
        nameof(IsGroupingEnabled), typeof(bool), typeof(DataGridView),
        new FrameworkPropertyMetadata(false, (owner, _) => ((DataGridView)owner).ApplyGrouping()));
    public bool IsGroupingEnabled { get => (bool)GetValue(IsGroupingEnabledProperty); set => SetValue(IsGroupingEnabledProperty, value); }

    public static readonly DependencyProperty GroupMemberPathProperty = DependencyProperty.Register(
        nameof(GroupMemberPath), typeof(string), typeof(DataGridView),
        new FrameworkPropertyMetadata(null, (owner, _) => ((DataGridView)owner).ApplyGrouping()));
    public string? GroupMemberPath { get => (string?)GetValue(GroupMemberPathProperty); set => SetValue(GroupMemberPathProperty, value); }

    public static readonly DependencyProperty ShowRowDetailsOnSelectionProperty = DependencyProperty.Register(
        nameof(ShowRowDetailsOnSelection), typeof(bool), typeof(DataGridView),
        new FrameworkPropertyMetadata(false, (owner, args) => ((DataGridView)owner).SetCurrentValue(
            RowDetailsVisibilityModeProperty, (bool)args.NewValue
                ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
                : DataGridRowDetailsVisibilityMode.Collapsed)));
    public bool ShowRowDetailsOnSelection { get => (bool)GetValue(ShowRowDetailsOnSelectionProperty); set => SetValue(ShowRowDetailsOnSelectionProperty, value); }

    public bool SetRowDetailsVisibility(object item, bool visible)
    {
        if (ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row) return false;
        row.DetailsVisibility = visible ? Visibility.Visible : Visibility.Collapsed;
        return true;
    }

    private void ApplyGrouping()
    {
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (groupedView is not null && activeGroup is not null && groupedView.GroupDescriptions.Contains(activeGroup))
            groupedView.GroupDescriptions.Remove(activeGroup);
        groupedView = view;
        activeGroup = null;
        if (!IsGroupingEnabled || string.IsNullOrWhiteSpace(GroupMemberPath) || view?.CanGroup != true) return;
        activeGroup = new PropertyGroupDescription(GroupMemberPath);
        view.GroupDescriptions.Add(activeGroup);
        view.Refresh();
    }
}
