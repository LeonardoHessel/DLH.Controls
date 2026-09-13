# DLH Controls Manual

**🇺🇸 English** · [🇧🇷 Português](Manual.md)

This manual presents the **DLH Controls** library from the point of view of someone building an application. The implementation currently available is the `DLH.Controls.Wpf` package, for Windows with .NET 10 and WPF.

## Table of contents

1. [Installation](#1-installation)
2. [XAML setup](#2-xaml-setup)
3. [TabControl](#3-tabcontrol)
4. [DataGridView](#4-datagridview)
5. [ScrollBar and ContextMenu](#5-scrollbar-and-contextmenu)
6. [Themes and customization](#6-themes-and-customization)
7. [Accessibility and keyboard](#7-accessibility-and-keyboard)
8. [Performance](#8-performance)
9. [Troubleshooting](#9-troubleshooting)
10. [Demo application](#10-demo-application)
11. [Reference documents](#11-reference-documents)

## 1. Installation

Install the package into the WPF project:

```powershell
dotnet add package DLH.Controls.Wpf --version 0.4.0-preview.2
```

Or add the reference directly to the project file:

```xml
<ItemGroup>
    <PackageReference Include="DLH.Controls.Wpf" Version="0.4.0-preview.2" />
</ItemGroup>
```

Since the current versions are pre-release, check the option to show pre-release versions when searching for the package through the Visual Studio package manager.

## 2. XAML setup

Declare the library's namespace on the window or control that will host the components:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf">
</Window>
```

Default styles are loaded automatically. The application only needs to merge `Themes/Generic.xaml` when it wants to directly reference the styles or resources published by the library.

## 3. TabControl

`TabControl` organizes content into tabs and draws the selected tab and its body as a single surface. The control accepts both `TabControlItem` and the native `System.Windows.Controls.TabItem`.

![TabControl in different positions](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/custom-tab-control.png)

### 3.1 Basic usage

```xml
<dlh:TabControl CornerRadius="12"
                      HeaderIndent="0"
                      TabSpacing="0">
    <dlh:TabControlItem Header="Overview">
        <TextBlock Margin="24" Text="Overview content" />
    </dlh:TabControlItem>

    <dlh:TabControlItem Header="Editor">
        <TextBox Margin="24" AcceptsReturn="True" />
    </dlh:TabControlItem>
</dlh:TabControl>
```

- `CornerRadius` sets the same radius for every curve of the outline.
- `HeaderIndent="0"` pushes the first tab against the body's edge.
- `TabSpacing="0"` removes the gap between tabs.
- `TabStripPlacement` accepts `Top`, `Bottom`, `Left` and `Right`.

### 3.2 Title and icon

The header accepts any WPF content:

```xml
<dlh:TabControlItem>
    <dlh:TabControlItem.Header>
        <StackPanel Orientation="Horizontal">
            <Path Width="16"
                  Height="16"
                  Margin="0,0,8,0"
                  Stretch="Uniform"
                  Fill="Orange"
                  Data="M2,2 L14,2 14,14 2,14 Z" />
            <TextBlock VerticalAlignment="Center" Text="Editor" />
        </StackPanel>
    </dlh:TabControlItem.Header>

    <TextBlock Margin="24" Text="Tab content" />
</dlh:TabControlItem>
```

With `ItemsSource`, use `ItemTemplate` to build the header and `ContentTemplate` to present the page.

### 3.3 Usage with MVVM

```xml
<dlh:TabControl ItemsSource="{Binding Documents}"
                      SelectedItem="{Binding SelectedDocument, Mode=TwoWay}"
                      ItemKeyPath="Id"
                      TabHeaderPath="Title">
    <dlh:TabControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Title}" />
        </DataTemplate>
    </dlh:TabControl.ItemTemplate>

    <dlh:TabControl.ContentTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Text, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" />
        </DataTemplate>
    </dlh:TabControl.ContentTemplate>
</dlh:TabControl>
```

Use an `ObservableCollection<T>` so insertions, removals and position changes are reflected in the UI.

### 3.4 Reordering tabs

```xml
<dlh:TabControl CanReorderTabs="True"
                      MinimumDragDistance="5"
                      IsDragPreviewEnabled="True"
                      IsAnimationEnabled="True"
                      DragAnimationDuration="0:0:0.180"
                      DragPreviewOpacity="0.94" />
```

Dragging only starts once the minimum distance is crossed. Top and bottom tabs move along the horizontal axis; side tabs move along the vertical axis. Pressing `Esc` cancels the operation.

### 3.5 Adding tabs

The add action comes disabled. When enabled, it appears after the last tab and doesn't participate in indices or selection:

```xml
<dlh:TabControl CanAddTabs="True"
                      AddTabContent="+"
                      AddTabCommand="{Binding AddDocumentCommand}" />
```

The command receives `AddTabCommandParameter`, when set. Without a command, handle the `AddTabRequested` event. The application remains responsible for creating the object and adding it to the collection.

### 3.6 Renaming tabs

```xml
<dlh:TabControl CanRenameTabs="True"
                      TabHeaderPath="Title"
                      RenameActivation="F2AndDoubleClick" />
```

- `F2` or double-click starts editing, according to `RenameActivation`.
- `Enter` confirms.
- `Esc` cancels.
- `TabHeaderPath` must point to a writable `string` property.
- `RenameTabCommand` lets validation and the update be delegated to the ViewModel.

### 3.7 Closing tabs

```xml
<dlh:TabControl CanCloseTabs="True"
                      ShowCloseButtons="True"
                      CloseTabCommand="{Binding CloseDocumentCommand}" />
```

`CanCloseTabs` grants the global permission. `ShowCloseButtons` only controls appearance. Use the attached property `TabControl.CanCloseTab="False"` to protect a specific tab. The `TabClosing` event lets the operation be cancelled before removal.

### 3.8 Shadow and outline

```xml
<dlh:TabControl IsShadowEnabled="True"
                      ShadowColor="#494949"
                      ShadowOpacity="0.5"
                      ShadowBlurRadius="10"
                      ShadowDepth="0"
                      ShadowDirection="315"
                      CornerRadius="12" />
```

The shadow, border, selected tab and body use the same outline. Disabling the shadow preserves its parameters for a later re-activation.

### 3.9 Persisting order and selection

Each item must have a unique, stable text key, given by `ItemKeyPath` or by the attached `TabKey` property:

```csharp
using (var output = File.Create("tabs.json"))
    tabs.SaveState(output);

using (var input = File.OpenRead("tabs.json"))
    tabs.LoadState(input);
```

The state stores order and selection. Page content and data remain the application's responsibility.

## 4. DataGridView

`DataGridView` keeps the native `DataGrid`'s infrastructure and adds a consistent appearance, states, visual sorting, column menu and scrollbars.

![DataGridView with sorting and custom cells](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/data-grid-view.png)

### 4.1 Basic usage

```xml
<dlh:DataGridView ItemsSource="{Binding Products}"
                  SelectionBehavior="Row"
                  CornerRadius="10">
    <DataGridTextColumn Header="Code"
                        Binding="{Binding Code}"
                        SortMemberPath="Code"
                        MinWidth="120" />
    <DataGridTextColumn Header="Description"
                        Binding="{Binding Description}"
                        SortMemberPath="Description"
                        Width="*" />
    <DataGridTextColumn Header="Quantity"
                        Binding="{Binding Quantity}"
                        SortMemberPath="Quantity"
                        Width="Auto" />
</dlh:DataGridView>
```

`AutoGenerateColumns` is disabled by default. Declare native columns such as `DataGridTextColumn`, `DataGridCheckBoxColumn`, `DataGridComboBoxColumn`, `DataGridHyperlinkColumn` and `DataGridTemplateColumn`.

### 4.2 Selection

`SelectionBehavior` accepts:

Set `AllowMultipleSelection="True"` to select several rows or cells with Ctrl/Shift. Use `GetBatchSelection()` to read the current selection and `ExecuteBatchAction()` to forward it to `BatchActionCommand`.

| Value | Behavior |
|---|---|
| `None` | Disallows selection through the control |
| `Row` | Selects the full row |
| `Column` | Selects the column |
| `Cell` | Selects a single cell |

Set `SelectionChangedCommand` to receive a `DataGridViewSelection` in the ViewModel.

### 4.3 Sorting

```xml
<dlh:DataGridView CanUserSortColumns="True"
                  ShowSortIndicators="True"
                  DefaultSortMemberPath="Description"
                  DefaultSortDirection="Ascending"
                  ShowClearSortMenuItem="True"
                  ShowRestoreDefaultSortMenuItem="True" />
```

The header menu can clear the current sort or reapply the default sort. From code, use `ClearSorting()` and `ApplyDefaultSort()`.

#### Multi-column sorting

Enable `IsMultiColumnSortEnabled`. A click starts sorting, `Shift+click` appends criteria and `Ctrl+click` removes the targeted criterion. When there are two or more criteria, the header shows numbers indicating their priority.

```csharp
grid.ApplySort(nameColumn, ListSortDirection.Ascending);
grid.ApplySort(dateColumn, ListSortDirection.Descending, append: true);
grid.RemoveSort(nameColumn);
```

### 4.4 Per-column filters

Filters are combined with **AND** and also respect any filter already present on the WPF view:

```csharp
grid.SetFilter(statusColumn, "Pending", DataGridViewFilterOperator.Equals);
grid.SetFilter(quantityColumn, "10", DataGridViewFilterOperator.GreaterThan);
grid.ClearFilter(statusColumn);
grid.ClearFilters();
```

Set `dlh:DataGridView.FilterMemberPath="Customer.Name"` on the column when the filtered value isn't the same one used for sorting. Use `dlh:DataGridView.CanUserFilter="False"` to prevent filtering on that column. The header menu offers actions to clear the column's filter and all active filters.

### 4.5 Column organization

```xml
<dlh:DataGridView CanUserReorderColumns="True"
                  CanUserResizeColumns="True"
                  CanUserToggleColumnVisibility="True" />
```

The user can drag headers, resize dividers and use the context menu to show or hide columns. The control keeps at least one column visible.

### 4.6 Layout persistence

Each column needs a stable key. `SortMemberPath` is used automatically when it's unique; for columns without sorting or with repeated paths, set `DataGridView.ColumnKey`:

```xml
<DataGridTemplateColumn Header="Actions"
                        dlh:DataGridView.ColumnKey="actions">
    <!-- application template -->
</DataGridTemplateColumn>
```

```csharp
DataGridViewState state = grid.CaptureState();
grid.RestoreState(state);

using (var output = File.Create("grid-layout.json"))
    grid.SaveState(output);

using (var input = File.OpenRead("grid-layout.json"))
    grid.LoadState(input);

grid.ResetState();
```

The versioned format records order, width, width unit, visibility and sorting. The library doesn't choose the file path. Missing columns are ignored, new columns stay at the end, and invalid data is never applied partially.

### 4.7 Editing and validation

Enable editing explicitly with `IsCellEditingEnabled="True"`. The rules declared on the `Binding` remain responsible for validation. Invalid cells get a border and error hint; customize with `ShowValidationErrors` and `ValidationErrorBrush`. `CellEditEndingCommand` can inspect the item, column and action, and cancel the commit.

### 4.8 CSV export

Use `ExportCsv` to write only the visible columns, in their current order, and the rows produced by filters and sorting. For `DataGridTemplateColumn`, provide `DataGridViewCsvOptions.ValueSelector` when `SortMemberPath` doesn't represent the displayed content.

### 4.9 Custom cells

```xml
<DataGridTemplateColumn Header="Status" SortMemberPath="Status">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border Padding="8,3"
                    CornerRadius="8"
                    Background="#294A4034"
                    BorderBrush="#4B725A"
                    BorderThickness="1">
                <TextBlock Text="{Binding Status}"
                           Foreground="#8DD5A1"
                           FontWeight="SemiBold" />
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

The same mechanism lets you present icons, buttons, images, links or your own editors.

### 4.10 Grouping and details

Configure `GroupMemberPath` and `IsGroupingEnabled="True"` to group the view. Each group's header remains customizable through the native `GroupStyle` collection.

The control offers two ways to present details:

- **inline:** set `RowDetailsTemplate` and enable `ShowRowDetailsOnSelection`; the row's height grows to fit the content;
- **floating:** set `RowDetailsPopupTemplate` and enable `ShowRowDetailsPopupOnClick`; the content appears over the UI without shifting the other rows.

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  ShowRowDetailsPopupOnClick="True"
                  RowDetailsPopupPlacement="Bottom"
                  RowDetailsPopupVerticalOffset="4">
    <dlh:DataGridView.RowDetailsPopupTemplate>
        <DataTemplate>
            <Border MaxWidth="520"
                    Padding="16,12"
                    Background="#292B2F"
                    BorderBrush="#50545C"
                    BorderThickness="1"
                    CornerRadius="8">
                <StackPanel>
                    <TextBlock Text="{Binding ShipmentNumber}"
                               FontWeight="SemiBold" />
                    <TextBlock Margin="0,6,0,0"
                               Text="{Binding Notes}"
                               TextWrapping="Wrap" />
                </StackPanel>
            </Border>
        </DataTemplate>
    </dlh:DataGridView.RowDetailsPopupTemplate>
</dlh:DataGridView>
```

In floating mode, clicking a row opens the panel; clicking the same row again closes it; clicking another row swaps the content; and clicking outside closes the panel. `RowDetailsPopupContentStyle` customizes the container. `RowDetailsPopupPlacement`, `RowDetailsPopupHorizontalOffset` and `RowDetailsPopupVerticalOffset` control its position. `IsRowDetailsPopupOpen` and `RowDetailsPopupItem` expose the current state, and `CloseRowDetailsPopup()` closes the panel from code. When `RowDetailsPopupTemplate` is absent, the control reuses `RowDetailsTemplate`.

### 4.11 Sticky pinning

Enable `CanPinRows` and `CanPinColumns` to let the user pin records through the row menu and columns through the header menu. Both options are `False` by default. The pin command lives only in the options menu; cells and headers don't get a permanent button.

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  CanPinRows="True"
                  CanPinColumns="True"
                  MaxPinnedRows="5"
                  MaxPinnedColumns="4"
                  ShowPinnedBoundarySeparator="True"
                  PinnedBoundarySeparatorBrush="#42A5E8"
                  PinnedBoundarySeparatorThickness="2"
                  RowKeyMemberPath="Id" />
```

A pinned item follows scrolling while its natural position is still visible. Once it reaches an edge, it sticks to it and stays visible. Rows can stick to the top or bottom; columns can stick to the left or right. When there are several pinned items, they stack in the order they're found, without overlapping. The intersection between pinned rows and columns keeps the same content, background, height and alignment as the grid.

`MaxPinnedRows` and `MaxPinnedColumns` limit how many items can be pinned and default to 5 and 4. Lowering either one at runtime automatically unpins the most recently pinned items until the new limit is respected, raising `RowUnpinned`/`ColumnUnpinned` for each one. `ShowPinnedBoundarySeparator` controls the line that separates the pinned and scrollable regions. Its color comes from `PinnedBoundarySeparatorBrush`, and `PinnedBoundarySeparatorThickness` accepts a finite value greater than zero. The separator is applied automatically to whichever edge is in use.

The `PinRow`, `UnpinRow`, `ToggleRowPin`, `PinColumn`, `UnpinColumn` and `ToggleColumnPin` operations return `True` only when they change the state. Use `UnpinAllRows()` and `UnpinAllColumns()` to clear the groups. `PinnedRows` and `PinnedColumns` are read-only collections; the `RowPinned`, `RowUnpinned`, `ColumnPinned` and `ColumnUnpinned` events report each change. Hiding a pinned column (`Visibility` other than `Visible`) automatically unpins it and frees its slot in `MaxPinnedColumns`; it needs to be pinned again explicitly once it becomes visible again.

Pinned columns participate in `CaptureState`, `RestoreState`, `SaveState` and `LoadState` through their stable keys. To persist pinned rows, configure `RowKeyMemberPath` with a unique, stable, text-valued property. Nested paths use segments separated by a dot, such as `Identity.Key`. Rows without a configured key remain pinnable at runtime but aren't included in the saved state.

### 4.12 Collection states

```xml
<dlh:DataGridView EmptyMessage="No items found."
                  LoadingMessage="Loading..."
                  IsLoading="{Binding IsLoading}"
                  ErrorMessage="{Binding ErrorMessage}" />
```

The priority is: error, loading, empty collection, and finally the data.

### 4.13 Density and scrolling

```xml
<dlh:DataGridView Density="Compact"
                  ShowRowSeparators="True"
                  CellPadding="12,6"
                  ScrollBarThickness="10"
                  ScrollBarTrackBrush="#3D4046"
                  ScrollBarThumbBrush="#686D77"
                  ScrollBarThumbHoverBrush="#8B919D" />
```

The available densities are `Compact`, `Default` and `Comfortable`. The scrollbars reserve their own space so they never cover rows, headers or the last column's content.

## 5. ScrollBar and ContextMenu

`ScrollBar` can be used directly on any scrollable content. `Thickness` controls its thickness, and `CornerRadius` is visually clamped to half that measurement. Colors, opacity, shadow and directional buttons are optional.

```xml
<dlh:ScrollBar Orientation="Vertical"
               Minimum="0" Maximum="100" Value="30"
               Thickness="10" CornerRadius="5"
               ShowButtons="False" />
```

`ContextMenu` accepts the same `MenuItem`, commands, bindings, shortcuts, checkable items and submenus as WPF:

```xml
<Button Content="Options">
    <Button.ContextMenu>
        <dlh:ContextMenu CornerRadius="8" ItemPadding="12,8">
            <MenuItem Header="Refresh" InputGestureText="F5" />
            <dlh:ToggleMenuItem Header="Show details"
                                IsChecked="{Binding ShowDetails}"
                                CheckedIcon="●" UncheckedIcon="○" />
            <dlh:ChoiceMenuItem Header="Theme" SelectedValue="{Binding Theme}" SelectedIndex="0">
                <dlh:ChoiceMenuOption Content="Dark" Value="Dark" Icon="☾" />
                <dlh:ChoiceMenuOption Content="Light" Value="Light" Icon="☀" />
            </dlh:ChoiceMenuItem>
            <Separator />
            <MenuItem Header="Export">
                <MenuItem Header="CSV file" />
            </MenuItem>
        </dlh:ContextMenu>
    </Button.ContextMenu>
</Button>
```

`DataGridView` uses both components internally. They're also available for custom controls and consumer applications.

Items are organized into five columns: icon, title, value, shortcut and arrow. `MenuItemAssist.Value` lets you fill the value column of any item, while `MenuItemAssist.ValueTemplate` lets you customize its presentation. Submenus use the same structure.

`SubmenuPlacementDirection="Left"` moves only the arrow column to the start and makes the submenu open to the left. The icon, title, value and shortcut columns don't change order. The default value is `Right`.

Each menu has one active path. Clicking another branch closes the submenus that don't belong to the new path. An external component can call `ContextMenu.ActivatePath(item)`, `CollapseAfter(item)` or `CollapseAll()`. For fixed menus, mark the root with `MenuInteraction.IsScopeRoot="True"` and call the equivalent `MenuInteraction` methods, passing that root. This lets an outside click close the whole tree without interfering with other menus in the window.

On `ToggleMenuItem`, `IsChecked` is two-way by default and the icons can vary between `CheckedIcon` and `UncheckedIcon`. On `ChoiceMenuItem`, clicking the item cycles through the options and the arrow opens the submenu for direct selection. `SelectedContent` occupies the value column. Use `SelectedIndex`, `SelectedItem` or `SelectedValue` to keep the choice in the view model.

## 6. Themes and customization

The usual WPF properties, such as `Background`, `Foreground`, `BorderBrush`, `BorderThickness`, `FontFamily` and `FontSize`, remain available.

To replace global resources, declare keys in the application's dictionary:

```xml
<SolidColorBrush x:Key="Tabs.Surface" Color="#35373C" />
<SolidColorBrush x:Key="Tabs.Focus" Color="#9CC9FF" />
<SolidColorBrush x:Key="DataGridView.Surface" Color="#35373C" />
<SolidColorBrush x:Key="DataGridView.Header" Color="#2F3136" />
<SolidColorBrush x:Key="DataGridView.Focus" Color="#9CC9FF" />
```

The resources are dynamic and can be swapped at runtime. See the README for the full list of the main keys.

## 7. Accessibility and keyboard

- Preserve sufficient contrast between text, background, focus and selection.
- Provide accessible text for headers made up of icons only.
- Don't remove focus indicators without offering a visible alternative.
- Test navigation with `Tab`, the arrow keys and `Ctrl+Tab`.
- In `TabControl`, `Ctrl+Shift` with the arrow keys reorders tabs when allowed.
- In `DataGridView`, `DataGrid`'s keyboard behaviors remain available.

See the [accessibility checklist](AccessibilityChecklist.md) before shipping an application.

## 8. Performance

`DataGridView` keeps row and column virtualization enabled. To preserve that behavior:

- avoid placing the grid inside another `ScrollViewer`;
- prefer observable collections and incremental updates;
- keep cell templates simple in very large tables;
- avoid measuring wide columns with `Width="Auto"` when not necessary.

## 9. Troubleshooting

### The style wasn't applied

Check the package reference, the XAML namespace and compatibility with `net10.0-windows`. Clean and rebuild the solution after updating the package.

### The tab can't be dragged

Check `CanReorderTabs`, exceed `MinimumDragDistance`, and use a mutable collection with no active sorting, grouping or filter.

### The title can't be edited

Enable `CanRenameTabs` and confirm that `TabHeaderPath` ends at a writable `string` property. When there's a `RenameTabCommand`, the command is responsible for updating the model.

### The tab won't close

Check `CanCloseTabs`, the individual `CanCloseTab` property, the command's `CanExecute` result, and a possible cancellation in `TabClosing`.

### A column won't sort

Check `CanUserSortColumns`, `CanUserSort` on the column, and a `SortMemberPath` matching a property on the item.

### The grid lost virtualization

Remove the outer `ScrollViewer` and confirm that `EnableRowVirtualization`, `EnableColumnVirtualization` and `ScrollViewer.CanContentScroll` are still enabled.

## 10. Demo application

Clone the repository and run:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
```

The demo lets you try out the controls, change settings, and observe different positions, themes, densities, selection modes and behaviors.

## 11. Reference documents

- [API reference](ApiReference.en.md)
- [Detailed TabControl guide](TabControl.md)
- [Data, MVVM and theme integration](Integration.md)
- [Accessibility checklist](AccessibilityChecklist.md)
- [Version history](../CHANGELOG.md)
- [How to contribute](../CONTRIBUTING.md)
- [Security policy](../SECURITY.md)
