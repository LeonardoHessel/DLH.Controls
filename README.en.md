# DLH Controls

**🇺🇸 English** · [🇧🇷 Português](README.md)

[![NuGet](https://img.shields.io/nuget/vpre/DLH.Controls.Wpf?label=NuGet)](https://www.nuget.org/packages/DLH.Controls.Wpf)
[![Downloads](https://img.shields.io/nuget/dt/DLH.Controls.Wpf?label=Downloads)](https://www.nuget.org/packages/DLH.Controls.Wpf)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)

**DLH Controls** is a library of reusable, customizable UI controls for .NET applications. The project brings together components, behaviors, themes, accessibility, documentation and samples in a base designed to grow with new controls and platforms.

The first package available is **DLH.Controls.Wpf**, targeting WPF applications. It offers a modern look, MVVM support, keyboard and accessibility support, and conventional WPF properties, without requiring an additional visual framework.

The package currently contains:

| Component | Purpose | Key features |
|---|---|---|
| `TabControl` | Organize pages and documents into tabs | continuous surface, four positions, animated drag and drop, creation, renaming, closing, persistence, shadow and themes |
| `DataGridView` | Display collections in a rich table | configurable selection, visual sorting, reorderable/hideable columns, data states, densities, badges/templates and customized scrollbars |
| `ScrollBar` | Standardize scrolling in any content | orientation, thickness, colors, safe corner radius, shadow and optional directional buttons |
| `ContextMenu` | Present consistent contextual actions | icons, checkable items, shortcuts, submenus, separators, states and configurable shadow |

> Requires **Windows** and **.NET 10** with WPF. The current package is a pre-release version.

## Component overview

### TabControl

`TabControl` draws the selected tab and the body as a single surface. The same corner radius is used on the outer edges and on the joins between the tab and the content.

![TabControl with top, bottom and side tabs](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/custom-tab-control.png)

It can be used with tabs declared directly in XAML or with a collection bound to `ItemsSource`. Creation, renaming, removal and reordering are optional and come disabled or protected by their own settings.

### DataGridView

`DataGridView` specializes WPF's native `DataGrid`. It preserves virtualization, bindings, templates, sorting, keyboard support and native column types, adding an appearance and behaviors consistent with the library.

![DataGridView with sorting, status, multiple columns and custom scrollbars](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/data-grid-view.png)

The example shows sorting with indicators, status rendered by template, column menu, compact density and scrolling on both axes.

## Installation

### .NET CLI

```powershell
dotnet add package DLH.Controls.Wpf --version 0.4.0-preview.2
```

### Visual Studio Package Manager

```powershell
Install-Package DLH.Controls.Wpf -Version 0.4.0-preview.2
```

### PackageReference

```xml
<ItemGroup>
    <PackageReference Include="DLH.Controls.Wpf" Version="0.4.0-preview.2" />
</ItemGroup>
```

### Paket CLI

Specify the project that will receive the reference:

```powershell
paket add DLH.Controls.Wpf --version 0.4.0-preview.2 --project path/YourProject.csproj
```

Or declare the package in the `paket.dependencies` file:

```text
source https://api.nuget.org/v3/index.json
nuget DLH.Controls.Wpf 0.4.0-preview.2
```

Add this line to the WPF project's `paket.references`:

```text
DLH.Controls.Wpf
```

Then restore normally:

```powershell
paket install
```

NuGet, `PackageReference` and Paket all consume the same `.nupkg` file.

## XAML setup

Add the library's namespace to the window or control that will use the components:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf">
</Window>
```

Default styles live in `Themes/Generic.xaml` and are loaded by WPF's own theme system. There's no need to copy templates into the consuming application.

# TabControl

## Minimal example

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

`HeaderIndent="0"` aligns the first tab with the body's edge. `TabSpacing="0"` removes the gaps between tabs. `CornerRadius` controls every curve of the outline.

## Header with text and icon

`Header` accepts any WPF content:

```xml
<dlh:TabControlItem>
    <dlh:TabControlItem.Header>
        <StackPanel Orientation="Horizontal">
            <Image Width="18"
                   Height="18"
                   Margin="0,0,8,0"
                   Source="/MyApplication;component/Assets/Edit.png" />
            <TextBlock VerticalAlignment="Center" Text="Editor" />
        </StackPanel>
    </dlh:TabControlItem.Header>

    <TextBlock Margin="24" Text="Editor page" />
</dlh:TabControlItem>
```

For vector icons, replace `Image` with `Path`, `Viewbox` or another visual element.

## Usage with ItemsSource and MVVM

```xml
<dlh:TabControl ItemsSource="{Binding Documents}"
                      SelectedItem="{Binding SelectedDocument}"
                      ItemKeyPath="Id"
                      TabHeaderPath="Title">
    <dlh:TabControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Margin="0,0,8,0" Text="◆" />
                <TextBlock Text="{Binding Title}" />
            </StackPanel>
        </DataTemplate>
    </dlh:TabControl.ItemTemplate>

    <dlh:TabControl.ContentTemplate>
        <DataTemplate>
            <TextBox Margin="24"
                     Text="{Binding Text, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" />
        </DataTemplate>
    </dlh:TabControl.ContentTemplate>
</dlh:TabControl>
```

A simple model can be defined like this:

```csharp
public sealed class DocumentViewModel
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public string Text { get; set; } = string.Empty;
}

public ObservableCollection<DocumentViewModel> Documents { get; } = [];
public DocumentViewModel? SelectedDocument { get; set; }
```

Data and editable state stay in the ViewModel. Switching tabs neither recreates nor loses the model's state.

## Top, bottom and side tabs

Use the WPF `TabStripPlacement` property:

```xml
<dlh:TabControl TabStripPlacement="Top" />
<dlh:TabControl TabStripPlacement="Bottom" />
<dlh:TabControl TabStripPlacement="Left" />
<dlh:TabControl TabStripPlacement="Right" />
```

The outline, scroll direction and drag-and-drop axis follow the position automatically.

## Drag and drop to reorder

```xml
<dlh:TabControl CanReorderTabs="True"
                      MinimumDragDistance="5"
                      IsDragPreviewEnabled="True"
                      IsAnimationEnabled="True"
                      DragAnimationDuration="0:0:0.180"
                      DragPreviewOpacity="0.94"
                      TabDragCursor="ScrollWE" />
```

The cursor only changes after the user crosses the drag threshold. On top or bottom tabs the movement is horizontal; on side tabs it's vertical. At the ends, the strip scrolls automatically.

Reordering is also available from the keyboard:

- `Ctrl+Shift+Left/Right` on horizontal tabs.
- `Ctrl+Shift+Up/Down` on side tabs.
- `Esc` cancels an in-progress drag.

From code:

```csharp
bool moved = tabs.MoveTab(oldIndex: 3, newIndex: 1);
```

## Adding a tab

The `+` action is optional and doesn't participate in selection, persistence or reordering:

```xml
<dlh:TabControl ItemsSource="{Binding Documents}"
                      CanAddTabs="True"
                      AddTabCommand="{Binding AddDocumentCommand}"
                      AddTabCommandParameter="{Binding SelectedWorkspace}" />
```

Without a command, the application can handle the event:

```csharp
tabs.AddTabRequested += (_, e) =>
{
    Documents.Add(CreateDocument());
};
```

## Renaming directly in the header

```xml
<dlh:TabControl ItemsSource="{Binding Documents}"
                      CanRenameTabs="True"
                      TabHeaderPath="Title"
                      RenameActivation="F2AndDoubleClick" />
```

- `F2` or double-click starts editing.
- `Enter` confirms.
- `Esc` cancels.
- `TabRenameRequested` can validate or reject the new title.
- `RenameTabCommand` lets the ViewModel take over the update.

## Closing tabs

```xml
<dlh:TabControl ItemsSource="{Binding Documents}"
                      CanCloseTabs="True"
                      ShowCloseButtons="True"
                      CloseTabCommand="{Binding CloseDocumentCommand}" />
```

A single tab can be protected:

```xml
<dlh:TabControlItem Header="Home"
                   dlh:TabControl.CanCloseTab="False" />
```

The application can cancel the close before removal:

```csharp
tabs.TabClosing += (_, e) =>
{
    if (HasUnsavedChanges(e.Item))
        e.Cancel = true;
};
```

The library never opens dialogs on its own; confirmation belongs to the application.

## Shadow and appearance

```xml
<dlh:TabControl Background="#35373C"
                      Foreground="#F2F3F5"
                      BorderBrush="#4C5058"
                      BorderThickness="1"
                      CornerRadius="12"
                      IsShadowEnabled="True"
                      ShadowColor="#494949"
                      ShadowOpacity="0.5"
                      ShadowBlurRadius="10"
                      ShadowDepth="0"
                      ShadowDirection="315" />
```

The background, border and shadow use the same unified outline. `ShadowOpacity` ranges from `0` to `1`; disabling `IsShadowEnabled` preserves the other values.

## Saving order and selection

Each item needs a unique, stable key. By default the control looks for the property named by `ItemKeyPath="Id"`.

```csharp
using (var output = File.Create("tabs.json"))
    tabs.SaveState(output);

using (var input = File.OpenRead("tabs.json"))
    tabs.LoadState(input);
```

It's also possible to work without files:

```csharp
TabControlState state = tabs.CaptureState();
tabs.RestoreState(state);
```

The state contains keys, order and selection. Page content stays the application's responsibility.

# ScrollBar

Use `controls:ScrollBar` directly or inside the template of any `ScrollViewer`, list or custom control. Commands, keyboard, automation and native properties remain available.

```xml
<controls:ScrollBar Orientation="Vertical"
                    Minimum="0"
                    Maximum="100"
                    Value="35"
                    Thickness="10"
                    CornerRadius="5"
                    TrackBrush="#3D4046"
                    ThumbBrush="#686D77"
                    ThumbHoverBrush="#8B919D"
                    ShowButtons="False" />
```

`CornerRadius` accepts a uniform value; when rendered it's automatically clamped to half the thickness. The shadow is optional via `IsShadowEnabled`, and its parameters can be changed with `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` and `ShadowDepth`.

`DataGridView` already uses this component internally. Its `ScrollBarThickness`, `ScrollBarTrackBrush`, `ScrollBarThumbBrush` and `ScrollBarThumbHoverBrush` properties were preserved.

# ContextMenu

`ContextMenu` keeps the commands, bindings, keyboard support, checkable items and submenus of WPF's native control, with the library's look.

```xml
<Button Content="Options">
    <Button.ContextMenu>
        <dlh:ContextMenu CornerRadius="8"
                         ItemPadding="12,8"
                         IconColumnWidth="28"
                         IsShadowEnabled="True">
            <MenuItem Header="Refresh" InputGestureText="F5"
                      dlh:MenuItemAssist.Value="Available">
                <MenuItem.Icon>
                    <TextBlock Text="↻" />
                </MenuItem.Icon>
            </MenuItem>
            <dlh:ToggleMenuItem Header="Show details"
                                IsChecked="{Binding ShowDetails, Mode=TwoWay}"
                                CheckedIcon="●"
                                UncheckedIcon="○" />
            <dlh:ChoiceMenuItem Header="Theme"
                                SelectedValue="{Binding Theme, Mode=TwoWay}"
                                SelectedIndex="0"
                                CycleDirection="Forward"
                                IsCycleWrappingEnabled="True">
                <dlh:ChoiceMenuOption Content="Dark" Value="Dark" Icon="☾" />
                <dlh:ChoiceMenuOption Content="Light" Value="Light" Icon="☀" />
                <dlh:ChoiceMenuOption Content="System" Value="System" Icon="◐" />
            </dlh:ChoiceMenuItem>
            <Separator />
            <MenuItem Header="Export">
                <MenuItem Header="CSV file" />
                <MenuItem Header="Spreadsheet" IsEnabled="False" />
            </MenuItem>
        </dlh:ContextMenu>
    </Button.ContextMenu>
</Button>
```

Use `Background`, `Foreground`, `BorderBrush`, `BorderThickness` and `Padding` for the surface. `HoverBrush`, `CheckedBrush`, `SeparatorBrush`, `DisabledOpacity`, `ItemPadding`, `IconSize` and `IconColumnWidth` control the items. The shadow uses `IsShadowEnabled`, `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` and `ShadowDepth`.

Each row uses five aligned columns: icon, title, value, shortcut and arrow. Use `MenuItemAssist.Value` and `MenuItemAssist.ValueTemplate` to fill the value column on any `MenuItem`; columns with no content stay empty. The same structure applies recursively to submenus.

Set `SubmenuPlacementDirection="Left"` to use the menu next to a window's right edge. Only the arrow column moves to the left side, the indicator points left and the submenu opens from that side. Icon, title, value and shortcut keep their order and orientation. The default is `Right`.

The menu keeps only one active path in the tree. Interacting with another branch automatically closes incompatible submenus. Other components can also control that state:

```csharp
menu.ActivatePath(item);  // keeps the item, its parents, and closes other branches
menu.CollapseAfter(item); // keeps the parents and closes the item and deeper levels
menu.CollapseAll();       // closes the whole tree
```

For a fixed composition that doesn't use `ContextMenu`, mark the container with `dlh:MenuInteraction.IsScopeRoot="True"` and use `MenuInteraction.ActivatePath`, `CollapseAfter` or `CollapseAll`, passing that container as the scope. Different scopes don't interfere with each other.

`ToggleMenuItem` provides a two-way boolean state and accepts `CheckedIcon` and `UncheckedIcon`. `ChoiceMenuItem` automatically fills the value column with `SelectedContent`, cycles through options on the main click and opens the full list from the arrow. Selection can be bound via `SelectedIndex`, `SelectedItem` or `SelectedValue`; all three properties use two-way binding by default. For collections of models, use `DisplayMemberPath`, `SelectedValuePath` and `IconMemberPath`. `CycleDirection`, `IsCycleWrappingEnabled` and `DropDownButtonWidth` customize the interaction.

The menu opened with a right click on `DataGridView` headers is already an instance of this component.

In `DataGridView`, the header menu groups the column's own actions first, then the `Sorting` and `Visible columns` submenus, and finally the general actions. The row menu shows pinning for the row and the clicked column, identified by name. Clear actions appear when applicable, and `Unpin all...` appears once there are two or more pins. When a pin limit is reached, the action stays disabled with an explanation in its tooltip. The same menus are used in both the pinned and scrollable areas.

# DataGridView

## Minimal example

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  SelectionBehavior="Row"
                  CornerRadius="10">
    <DataGridTextColumn Header="Shipment No."
                        Binding="{Binding Number}"
                        SortMemberPath="Number"
                        MinWidth="160" />

    <DataGridTextColumn Header="Origin"
                        Binding="{Binding Origin}"
                        SortMemberPath="Origin"
                        Width="*" />

    <DataGridTextColumn Header="Quantity"
                        Binding="{Binding Quantity}"
                        SortMemberPath="Quantity"
                        Width="Auto" />
</dlh:DataGridView>
```

Columns are WPF's native types: `DataGridTextColumn`, `DataGridCheckBoxColumn`, `DataGridComboBoxColumn`, `DataGridHyperlinkColumn` and `DataGridTemplateColumn`. `AutoGenerateColumns` is disabled by default.

## Model and collection

```csharp
public sealed record Shipment(
    string Number,
    string Origin,
    string Vehicle,
    int Quantity,
    string Status);

public ObservableCollection<Shipment> Shipments { get; } =
[
    new("EXP-2026-001", "SP", "Truck 12", 120, "Completed"),
    new("EXP-2026-002", "MG", "Truck 07", 85, "Under inspection"),
    new("EXP-2026-003", "PR", "Van 03", 34, "Pending")
];
```

Changes to `ObservableCollection<T>` are reflected automatically.

## Selection

```xml
<dlh:DataGridView SelectionBehavior="None" />
<dlh:DataGridView SelectionBehavior="Row" />
<dlh:DataGridView SelectionBehavior="Column" />
<dlh:DataGridView SelectionBehavior="Cell" />
```

To receive the selection in the ViewModel:

```xml
<dlh:DataGridView SelectionBehavior="Cell"
                  SelectionChangedCommand="{Binding SelectionChangedCommand}" />
```

The command receives a `DataGridViewSelection`, containing `Item` and `Column` according to the selected mode.

To allow multiple selection, set `AllowMultipleSelection="True"`. `GetBatchSelection()` returns a snapshot of the selected items and cells; `BatchSelectionChangedCommand` tracks changes and `BatchActionCommand` can be triggered by `ExecuteBatchAction()`.

```csharp
private void OnSelectionChanged(DataGridViewSelection selection)
{
    object? row = selection.Item;
    DataGridColumn? column = selection.Column;
}
```

## Sorting and indicators

Global sorting can be turned on or off:

```xml
<dlh:DataGridView CanUserSortColumns="True"
                  ShowSortIndicators="True" />
```

Each column can also control its own sorting:

```xml
<DataGridTextColumn Header="Notes"
                    Binding="{Binding Notes}"
                    CanUserSort="False" />
```

The indicators communicate three states:

| State | Representation |
|---|---|
| Sortable, inactive column | discreet up and down arrows |
| Ascending order | highlighted upward arrow |
| Descending order | highlighted downward arrow |

Size, colors and geometries are configurable:

```xml
<dlh:DataGridView SortIconSize="14"
                  SortIconBrush="#9DA3AE"
                  ActiveSortIconBrush="#42A5E8" />
```

## Default sort and clearing

```xml
<dlh:DataGridView DefaultSortMemberPath="Number"
                  DefaultSortDirection="Ascending"
                  ShowClearSortMenuItem="True"
                  ShowRestoreDefaultSortMenuItem="True" />
```

Right-clicking a header:

- **Clear sorting** restores the collection's original order.
- **Restore default sort** reapplies the configured property and direction.

The same actions are available from code:

```csharp
grid.ClearSorting();
bool applied = grid.ApplyDefaultSort();
```

## Multi-column sorting

```xml
<dlh:DataGridView IsMultiColumnSortEnabled="True" />
```

- A click starts or toggles sorting on a column.
- `Shift+click` appends the column as a new criterion.
- `Ctrl+click` removes the column from sorting.
- The number next to the arrow shows each criterion's priority.

From code, use `ApplySort(column, direction, append)` and `RemoveSort(column)`. The full order is also preserved by `CaptureState()`.

## Per-column filters

Use `SetFilter` to combine filters across different columns. The default path comes from `SortMemberPath` and can be overridden with `DataGridView.FilterMemberPath`.

```csharp
grid.SetFilter(grid.Columns[0], "EXP-2026", DataGridViewFilterOperator.StartsWith);
grid.SetFilter(grid.Columns[3], "50", DataGridViewFilterOperator.GreaterThan);
grid.ClearFilter(grid.Columns[3]);
grid.ClearFilters();
```

## Reordering, resizing and hiding columns

```xml
<dlh:DataGridView CanUserReorderColumns="True"
                  CanUserResizeColumns="True"
                  CanUserToggleColumnVisibility="True" />
```

- Drag the header to change its position.
- Drag the divider to change its width.
- Right-click to show or hide columns.
- The control prevents hiding the last visible column.

Features can be disabled on a specific column:

```xml
<DataGridTextColumn Header="Code"
                    Binding="{Binding Code}"
                    CanUserReorder="False"
                    CanUserResize="False"
                    CanUserSort="False" />
```

## Editing and validation

Cells remain read-only by default. Enable `IsCellEditingEnabled="True"` and use normal WPF binding validation rules. `ShowValidationErrors` controls the highlight, `ValidationErrorBrush` sets its color, and `CellEditEndingCommand` receives a `DataGridViewCellEditContext` that can cancel the commit.

## CSV export

`ExportCsv` writes the current view's rows and only the visible columns, in the order shown. Active filters and sorting are therefore respected. `DataGridViewCsvOptions` configures headers, delimiter, culture and a selector for templated columns.

```csharp
using var file = File.Create("data.csv");
grid.ExportCsv(file, new DataGridViewCsvOptions { Delimiter = ";" });
```

## Grouping and row details

Set `GroupMemberPath` and enable `IsGroupingEnabled` to create groups without replacing external grouping. The built-in mode remains available: `ShowRowDetailsOnSelection` presents the `RowDetailsTemplate` inside the grid when a row is selected, and `SetRowDetailsVisibility(item, visible)` controls a materialized row from code.

```xml
<dlh:DataGridView IsGroupingEnabled="True"
                  GroupMemberPath="Status"
                  ShowRowDetailsOnSelection="True">
    <dlh:DataGridView.RowDetailsTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Notes}" />
        </DataTemplate>
    </dlh:DataGridView.RowDetailsTemplate>
</dlh:DataGridView>
```

To present details without changing row height, enable the floating panel. A click opens the row's details, a second click on the same row closes the panel, and clicking another row swaps the content. Clicking outside the panel also closes it.

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

`RowDetailsPopupContentStyle` customizes the container, and the `RowDetailsPopupPlacement`, `RowDetailsPopupHorizontalOffset` and `RowDetailsPopupVerticalOffset` properties control its position. Check `IsRowDetailsPopupOpen` and `RowDetailsPopupItem` for the current state, or call `CloseRowDetailsPopup()` to close the panel from code. When `RowDetailsPopupTemplate` isn't provided, the control reuses `RowDetailsTemplate`.

## Sticky row and column pinning

Pinning is opt-in and stays disabled by default. Enable `CanPinRows` and `CanPinColumns` to make the pin action available in the row and header context menus. The pin icon appears in the options menu, without taking up permanent space in cells or headers.

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  CanPinRows="True"
                  CanPinColumns="True"
                  MaxPinnedRows="5"
                  MaxPinnedColumns="4"
                  ShowPinnedBoundarySeparator="True"
                  PinnedBoundarySeparatorBrush="#42A5E8"
                  PinnedBoundarySeparatorThickness="2" />
```

A pinned row or column stays in its natural position while it's visible. While scrolling, it only sticks to the edge once it leaves the visible area. Multiple pinned items stack without overlapping: rows stick to the top or bottom and columns stick to the left or right, depending on the scroll direction. Intersections between pinned rows and columns preserve the original content, height and alignment.

Clicking a sticky cell to select or edit it scrolls the grid to the original cell, preserving selection, editing and validation behavior. The sticky cell's context menu acts on the original column. Hiding a pinned column automatically unpins it and frees its slot in `MaxPinnedColumns`; to pin it again once it's shown, call `PinColumn` explicitly. Replacing `ItemsSource` also refreshes the content of sticky columns.

Lowering `MaxPinnedRows` or `MaxPinnedColumns` at runtime automatically unpins the most recently pinned items in excess until the new limit is respected, raising `RowUnpinned`/`ColumnUnpinned` for each one.

`ShowPinnedBoundarySeparator` displays the divider between the sticky area and the scrollable area. `PinnedBoundarySeparatorBrush` sets its color and `PinnedBoundarySeparatorThickness` accepts finite values greater than zero. The divider follows whichever of the four edges currently has sticky content.

From code, use `PinRow`, `UnpinRow`, `ToggleRowPin`, `PinColumn`, `UnpinColumn` and `ToggleColumnPin`. `UnpinAllRows()` and `UnpinAllColumns()` clear each group. The read-only `PinnedRows` and `PinnedColumns` collections represent the current state, and the `RowPinned`, `RowUnpinned`, `ColumnPinned` and `ColumnUnpinned` events notify changes.

Pinned column state is saved along with the grid's other settings. To also persist pinned rows, set `RowKeyMemberPath` to a stable, unique, text-valued property of each record. Nested paths are accepted, with segments separated by a dot, such as `Identity.Key`.

## Persisting the column layout

Set a stable key with `SortMemberPath` or with the attached `ColumnKey` property. The state includes order, width, visibility and sorting:

```xml
<DataGridTextColumn Header="Description"
                    dlh:DataGridView.ColumnKey="description"
                    Binding="{Binding Description}"
                    SortMemberPath="Description" />
```

```csharp
using (var output = File.Create("grid-layout.json"))
    grid.SaveState(output);

using (var input = File.OpenRead("grid-layout.json"))
    grid.LoadState(input);

grid.ResetState(); // returns to the layout captured when the control was loaded
```

Removed columns are ignored and new columns are placed after the known ones. An invalid state is rejected before it changes the control.

## Custom cells

Use `DataGridTemplateColumn` for status, icons, buttons or any WPF content:

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

## Empty, loading and error states

```xml
<dlh:DataGridView EmptyMessage="No shipments found."
                  LoadingMessage="Loading shipments..."
                  IsLoading="{Binding IsLoading}"
                  ErrorMessage="{Binding ErrorMessage}" />
```

Visual priority:

1. A filled `ErrorMessage` shows the failure.
2. `IsLoading="True"` shows the loading state.
3. An empty collection shows `EmptyMessage`.
4. Otherwise, the rows are displayed.

## Density and separators

```xml
<dlh:DataGridView Density="Compact"
                  ShowRowSeparators="True"
                  CellPadding="12,6" />
```

Available densities:

- `Compact`
- `Default`
- `Comfortable`

For alternating rows, use the native property:

```xml
<dlh:DataGridView AlternatingRowBackground="#24373A40"
                  AlternationCount="2" />
```

## Scrollbars

```xml
<dlh:DataGridView ScrollBarThickness="10"
                  ScrollBarTrackBrush="#3D4046"
                  ScrollBarThumbBrush="#686D77"
                  ScrollBarThumbHoverBrush="#8B919D" />
```

The vertical scrollbar reserves its own space, preventing the last column's text from sitting under it. The horizontal scrollbar spans the full bottom width and reserves its own strip, preventing the last row from being covered. The corner radius never exceeds half the actual thickness.

## Performance

The control keeps these enabled:

```xml
<dlh:DataGridView EnableRowVirtualization="True"
                  EnableColumnVirtualization="True"
                  ScrollViewer.CanContentScroll="True" />
```

For large collections, avoid placing the grid inside another `ScrollViewer`, as that can prevent row virtualization.

# Themes and resources

The conventional `Background`, `Foreground`, `BorderBrush`, `BorderThickness`, `Padding`, `FontFamily` and `FontSize` properties remain available.

`TabControl`'s main resources:

```xml
<SolidColorBrush x:Key="Tabs.Surface" Color="#35373C" />
<SolidColorBrush x:Key="Tabs.Hover" Color="#454850" />
<SolidColorBrush x:Key="Tabs.Text" Color="#F2F3F5" />
<SolidColorBrush x:Key="Tabs.Muted" Color="#BCC0CA" />
<SolidColorBrush x:Key="Tabs.Edge" Color="#4C5058" />
<SolidColorBrush x:Key="Tabs.Focus" Color="#9CC9FF" />
```

`DataGridView`'s main resources:

```xml
<SolidColorBrush x:Key="DataGridView.Surface" Color="#35373C" />
<SolidColorBrush x:Key="DataGridView.Header" Color="#2F3136" />
<SolidColorBrush x:Key="DataGridView.Hover" Color="#454850" />
<SolidColorBrush x:Key="DataGridView.Selection" Color="#334F8AC9" />
<SolidColorBrush x:Key="DataGridView.Text" Color="#F2F3F5" />
<SolidColorBrush x:Key="DataGridView.Edge" Color="#4C5058" />
<SolidColorBrush x:Key="DataGridView.Focus" Color="#9CC9FF" />
```

Since they're `DynamicResource`s, these values can be replaced at runtime to switch themes.

# Solution structure

- `src/DLH.Controls.Wpf`: the reusable, packable library project.
- `src/DLH.Controls.Wpf/Controls`: component implementations.
- `src/DLH.Controls.Wpf/Themes`: templates and visual resources.
- `samples/DLH.Controls.Wpf.Demo`: interactive viewer.
- `tests/DLH.Controls.Wpf.Tests`: WPF integration tests.
- `docs`: detailed documentation and technical plans.
- `eng`: package validation and preparation.

# Development

```powershell
dotnet restore DLH.Controls.sln
dotnet build DLH.Controls.sln -c Release
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release
dotnet pack src/DLH.Controls.Wpf -c Release -o artifacts/packages
```

Full validation before publishing:

```powershell
./eng/Validate.ps1
```

The routine builds the solution, runs the suites via `dotnet test`, saves a TRX report, generates the package and checks the DLL, README, license and Paket instructions. It then installs the `.nupkg` into a temporary WPF application, builds and runs both controls. Only the library is packaged; the demo and tests never go into the `.nupkg`.

To run just one family, use `--filter TestCategory=DataGridView` or `--filter TestCategory=TabControl` on the `tests/DLH.Controls.Wpf.AutomatedTests` project.

The demo preserves preferences under `%LOCALAPPDATA%\TabControl.Demo`.

# Additional documentation

- [DLH Controls usage manual](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Manual.en.md)
- [TabControl in detail](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/TabControl.md)
- [API reference](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/ApiReference.en.md)
- [Data, MVVM and theme integration](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Integration.md)
- [Package preparation and contents](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Packaging.md)
- [Automatic publishing](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Publishing.md)
- [Preparing for version 1.0.0](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/ReleaseReadiness.md)
- [Version history](https://github.com/LeonardoHessel/DLH.Controls/blob/main/CHANGELOG.md)
- [How to contribute](https://github.com/LeonardoHessel/DLH.Controls/blob/main/CONTRIBUTING.md)
- [Security policy](https://github.com/LeonardoHessel/DLH.Controls/blob/main/SECURITY.md)

# Support the project

If **DLH Controls** is helping your application, you can support the library's maintenance, bug fixes and the development of new components.

## International support — GitHub Sponsors

One-time or monthly contributions can be made through GitHub Sponsors:

[![Sponsor on GitHub Sponsors](https://img.shields.io/badge/Sponsor-GitHub%20Sponsors-EA4AAA?logo=githubsponsors&logoColor=white)](https://github.com/sponsors/LeonardoHessel)

[Open LeonardoHessel's sponsor profile](https://github.com/sponsors/LeonardoHessel)

### Support via Pix (Brazil)

Pix is the primary way to support DLH Controls from Brazil. Scan the QR code with your bank's app and choose the contribution amount.

![QR code to support DLH Controls via Pix](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/pix-qrcode.png)

**Random Pix key:** `65e95283-e2bd-46c3-b045-8773e8157df8`

# License and attribution

Distributed under the **DLH Controls — Visible Attribution License, version 1.0**. See the [full license](https://github.com/LeonardoHessel/DLH.Controls/blob/main/LICENSE.txt).

Commercial use, modification and redistribution are permitted under the license's terms, including accessible credit to users:

> This product uses DLH Controls, developed by Leonardo D. de L. Hessel.

The credit can appear in **About**, **Credits** or **Third-party licenses**. Products without a graphical interface should make it available in their documentation or help.

# Package and authorship

- Package: [DLH.Controls.Wpf on NuGet.org](https://www.nuget.org/packages/DLH.Controls.Wpf)
- Latest published version: `0.4.0-preview.2`
- Author: **Leonardo D. de L. Hessel**
- Platform: Windows
- Framework: .NET 10 / WPF

Changes after the version listed above remain in development until a new release.
