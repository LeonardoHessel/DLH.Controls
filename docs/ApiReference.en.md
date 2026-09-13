# API Reference — TabControl

**🇺🇸 English** · [🇧🇷 Português](ApiReference.md)

Review for stabilization, based on the current implementation. Namespace: `DLH.Controls.Wpf`. Use on the WPF UI thread. Members inherited from TabControl keep their WPF contracts.

## Own properties

| Property | Default | Contract |
|---|---|---|
| CornerRadius | 12 | Uniform, finite, non-negative radius for every curve |
| HeaderIndent | NaN | Automatic indent; 0 sits flush with the edge; finite, non-negative values |
| TabSpacing | 0 | Gap along the strip's axis, finite and non-negative |
| IsShadowEnabled | true | Toggles the shadow without discarding its parameters |
| ShadowColor | #494949 | Shadow color |
| ShadowOpacity | 0.5 | Finite number between 0 and 1 |
| ShadowBlurRadius | 10 | Finite and non-negative |
| ShadowDepth | 0 | Finite and non-negative |
| ShadowDirection | 315 | Finite angle |
| CanReorderTabs | true | Controls drag, keyboard and MoveTab; doesn't block RestoreState |
| TabDragCursor | ScrollWE | Non-null cursor; doesn't automatically switch to ScrollNS on side tabs |
| MinimumDragDistance | 5 | Physical pixels along the axis; drag only starts past the threshold |
| IsDragPreviewEnabled | true | Floating preview; disabling it doesn't prevent reordering |
| IsAnimationEnabled | true | Drag animations |
| DragAnimationDuration | 180 ms | From zero to ten seconds |
| DragPreviewOpacity | 0.94 | Finite, between 0 and 1 |
| CanCloseTabs | true | Global permission, including RequestCloseTab and the routed command |
| ShowCloseButtons | false | Visual preference; not a removal permission |
| CloseTabCommand | null | Synchronous ICommand; the parameter is the item, not necessarily a TabItem |
| CanAddTabs | false | Shows the add action; still depends on a command or event able to fulfill the request |
| AddTabCommand | null | ICommand that creates the item; receives AddTabCommandParameter |
| AddTabCommandParameter | null | Optional context sent to the creation command or event |
| AddTabContent | + | Button content, independent of the collection |
| AddTabContentTemplate | null | Optional DataTemplate for the button's content |
| CanRenameTabs | false | Authorizes direct editing; doesn't make a data target writable on its own |
| TabHeaderPath | null | Editable text path on data items, such as Header or Document.Title |
| RenameTabCommand | null | ICommand that receives TabRenameRequest and updates the model |
| RenameActivation | F2AndDoubleClick | None, F2, DoubleClick or a combination of the two gestures |
| ItemKeyPath | Id | Path to a string key; accepts dot-separated members |

Attached properties: `CanCloseTab` (true) protects an individual container; `TabKey` (null) supplies the key for explicit items. Keys must be non-empty, unique and stable.

The inherited `TabStripPlacement` property accepts Top, Bottom, Left and Right. `BorderThickness` uses its largest side as a uniform stroke. `Padding`, colors and fonts are inherited properties. The defaults above are the library's, not the viewer's own adjustments.

## Operations and events

| Operation | Result and limits |
|---|---|
| MoveTab(oldIndex, newIndex) | bool; zero-based indices; requires a mutable source with no sorting/filter/group and CanReorderTabs; selection is preserved |
| RequestCloseTab(item) | bool; respects permissions, CanExecute and cancellation; true only if removed |
| RequestAddTab(parameter?) | bool; uses AddTabCommand or, absent that, AddTabRequested |
| BeginRenameTab(item) | bool; opens the editor when the feature and the data target allow it |
| CommitTabRename() | bool; confirms a different text after veto/CanExecute and only ends when accepted |
| CancelTabRename() | Ends the editor without changing the title |
| CaptureState(keySelector?) | TabControlState; captures order and selection by keys |
| RestoreState(state, keySelector?) | Validates structure/keys, ignores missing ones and appends new items; requires a mutable source |
| SaveState(stream, keySelector?) / LoadState(stream, keySelector?) | JSON; never closes the caller's stream |
| CaptureConfiguration() | TabControlConfiguration with textual values in invariant culture |
| ValidateConfiguration(configuration) | Static; validates without changing the control |
| RestoreConfiguration(configuration) | Validates every value before applying it; preserves bindings via SetCurrentValue |
| ResetConfiguration() | Applies the registered defaults; doesn't remove documents; doesn't mean restoring the theme or the application's initial settings |

`TabReordered` is raised after an effective change, with Item, OldIndex, NewIndex and Reason (Programmatic, Drag, Keyboard). It doesn't fire on cancellation, a no-op, or RestoreState. `TabClosing` allows a veto via Cancel; `TabClosed` fires after a confirmed removal. `AddTabRequested` handles creation without a command. `TabRenaming` and `TabRenameRequested` allow a veto; `TabRenamed` signals acceptance. `StateRestored` signals that order/selection restoration completed. These are CLR events, not routed events. `CloseTab` and `AddTab` are RoutedUICommand.

When `AddTabCommand` exists, it takes precedence over `AddTabRequested`; both are never executed for the same request. The `+` action never joins Items, selection, indices, state or reordering. When `RenameTabCommand` exists, it receives a `TabRenameRequest` with Item, OldHeader and NewHeader. Without a command, `TabHeaderPath` must end at a writable string property; explicit TabItem items accept a plain text Header. The control coordinates a single synchronous edit and doesn't persist an editor left in progress.

When CloseTabCommand is set, it's responsible for the synchronous removal. The library never awaits Tasks nor offers asynchronous confirmation. Changing the collection directly is the application's responsibility and bypasses the control's permissions/events.

## Persistence and errors

Both formats carry Version=1, independent of the NuGet version. State never saves page content. Configuration includes CanAddTabs, CanRenameTabs, TabHeaderPath and RenameActivation, but never saves commands, parameters, content/templates, items, selection or the application's theme. Missing properties keep their current value; unknown names and incompatible versions are rejected. Colors are values, not DynamicResource keys. Custom cursors/brushes must be convertible by WPF's converters; CaptureConfiguration may reject them.

Invalid keys or incompatible sources throw InvalidOperationException; invalid values/versions throw ArgumentException; invalid JSON throws JsonException, and null JSON content in LoadState throws InvalidDataException. Stream failures belong to the caller. Upfront validation provides no rollback for the effects of custom commands, events or collections that throw exceptions.

## Helper types and compatibility

`TabControlItem` is optional: the native `System.Windows.Controls.TabItem` is also accepted. `TabControlState`, `TabControlConfiguration` and the event args classes are public. `TabCursors.ClosedHand` and `TabSpacingConverter` are also public.

Names were standardized during the pre-release phase: `TabControl` replaces `CustomTabControl` and `TabControlItem` replaces `CustomTabItem`.

# API Reference — ScrollBar

`ScrollBar` derives from `System.Windows.Controls.Primitives.ScrollBar`. `Thickness` controls the cross axis according to `Orientation`; `CornerRadius` must be uniform and is visually clamped to half the thickness. `TrackBrush`, `ThumbBrush`, `ThumbHoverBrush`, `ThumbPressedBrush`, `TrackPadding` and `ThumbOpacity` control the visuals. `ShowButtons` displays the directional commands. The shadow is opt-in via `IsShadowEnabled` and uses `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` and `ShadowDepth`.

# API Reference — ContextMenu

`ContextMenu` derives from `System.Windows.Controls.ContextMenu` and preserves `Items`, `ItemContainerStyle`, commands, bindings, shortcuts, keyboard support, checkable items and native submenus. `CornerRadius`, `Padding`, `ItemPadding`, `IconSize`, `IconColumnWidth` and `ArrowColumnWidth` define the geometry. Each level organizes its items into five aligned columns: icon, title, value, shortcut and arrow. `MenuItemAssist.Value` and `MenuItemAssist.ValueTemplate` fill the value column of any item. `Background`, `Foreground`, `BorderBrush`, `HoverBrush`, `CheckedBrush` and `SeparatorBrush` define the colors. `DisabledOpacity` controls unavailable items. The shadow uses `IsShadowEnabled`, `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` and `ShadowDepth`.

`SubmenuPlacementDirection` controls exclusively which side the submenu opens on. In `Left`, the arrow occupies the first column, points left, and the submenu uses `PlacementMode.Left`; the other columns keep their normal order. Each submenu reuses the same template, colors, geometry and shadow as the menu that opened it.

`ContextMenu.ActivatePath(MenuItem)` keeps the item and its ancestors and closes the other branches. `CollapseAfter(MenuItem)` keeps only the ancestors, while `CollapseAll()` closes the whole tree. `MenuInteraction` offers the same operations for fixed compositions and identifies each root through the attached `IsScopeRoot` property. Coordination is limited to the given scope.

`ToggleMenuItem` specializes the checkable item with a two-way `IsChecked` by default, plus `CheckedIcon` and `UncheckedIcon`. `ChoiceMenuItem` represents a choice among several values and exposes the displayed text through `SelectedContent`. `SelectedIndex`, `SelectedItem` and `SelectedValue` are two-way by default; `DisplayMemberPath`, `SelectedValuePath` and `IconMemberPath` adapt external models. `CycleDirection`, `IsCycleWrappingEnabled` and `DropDownButtonWidth` control the cyclic click and the area that opens the submenu. `ChoiceMenuOption` is the simple declarative model with `Content`, `Value`, `Icon` and `IsEnabled`.

In C#, when `System.Windows.Controls` and `DLH.Controls.Wpf` are imported together, use an alias or the qualified name to distinguish the two types: `using ControlsContextMenu = DLH.Controls.Wpf.ContextMenu;`. In XAML, the `dlh:` prefix already removes the ambiguity.

# API Reference — DataGridView

`AllowMultipleSelection` toggles between single and extended selection. `GetBatchSelection()` returns a `DataGridViewBatchSelection`; `BatchSelectionChangedCommand` receives changes and `BatchActionCommand` is executed by `ExecuteBatchAction()`.

`IsCellEditingEnabled` unlocks native editing. `ShowValidationErrors` and `ValidationErrorBrush` configure the visual feedback for WPF's rules. `CellEditEndingCommand` receives a `DataGridViewCellEditContext`; set `Cancel=true` to prevent the commit.

`ExportCsv(TextWriter, options)` and `ExportCsv(Stream, options, encoding)` export the current view. `DataGridViewCsvOptions` offers `Delimiter`, `IncludeHeaders`, `Culture` and `ValueSelector`.

`IsGroupingEnabled` applies a `PropertyGroupDescription` for `GroupMemberPath`. `ShowRowDetailsOnSelection` controls the inline details on selection, and `SetRowDetailsVisibility(item, visible)` acts on a materialized row.

## Floating details

| Member | Default | Contract |
|---|---:|---|
| `ShowRowDetailsPopupOnClick` | `false` | A click opens the row's panel; another click on the same row closes it; a different row swaps the content |
| `RowDetailsPopupTemplate` | `null` | Item template; when absent, reuses `RowDetailsTemplate` |
| `RowDetailsPopupContentStyle` | `null` | Style of the `ContentControl` hosting the template |
| `RowDetailsPopupPlacement` | `Bottom` | WPF placement relative to the clicked row |
| `RowDetailsPopupHorizontalOffset` | `0` | Finite horizontal offset |
| `RowDetailsPopupVerticalOffset` | `4` | Finite vertical offset |
| `IsRowDetailsPopupOpen` | read-only | Reports whether the panel is open |
| `RowDetailsPopupItem` | read-only | Item currently presented |
| `CloseRowDetailsPopup()` | — | Closes the panel and clears the current item |

The panel uses `StaysOpen=false`, so a click outside it closes the display. Configuration changes also close an open panel, so the next opening uses the new template, style or placement.

## Sticky row and column pinning

| Member | Default | Contract |
|---|---:|---|
| `CanPinRows` | `false` | Authorizes pinning records and enables its action in the row menu |
| `CanPinColumns` | `false` | Authorizes pinning columns and enables its action in the header menu |
| `MaxPinnedRows` | `5` | Positive limit of pinned rows; lowering it at runtime unpins the most recently pinned items in excess |
| `MaxPinnedColumns` | `4` | Positive limit of pinned columns; lowering it at runtime unpins the most recently pinned items in excess |
| `ShowPinnedBoundarySeparator` | `true` | Displays the boundary between sticky and scrollable content |
| `PinnedBoundarySeparatorBrush` | `#42A5E8` | Separator brush on any of the four possible edges |
| `PinnedBoundarySeparatorThickness` | `2` | Finite thickness, greater than zero |
| `PinnedRows` | read-only | Observable collection of pinned items |
| `PinnedColumns` | read-only | Observable collection of pinned columns |
| `RowKeyMemberPath` | `null` | Simple or nested path to the text key; nested segments are separated by a dot, such as `Identity.Key` |

`PinRow`, `UnpinRow`, `ToggleRowPin`, `PinColumn`, `UnpinColumn` and `ToggleColumnPin` return `true` when they change the state. `UnpinAllRows()` and `UnpinAllColumns()` remove every corresponding pin. `RowPinned`, `RowUnpinned`, `ColumnPinned` and `ColumnUnpinned` report the changed item or column. Hiding a pinned column (`Visibility` other than `Visible`) automatically unpins it and frees its slot in `MaxPinnedColumns`; it isn't re-pinned on its own once it becomes visible again.

A pin preserves the natural position until the item reaches the visible boundary. After that, rows stick to the top or bottom and columns to the left or right. Multiple items stack against the edge. The intersection layer keeps pinned rows and columns synchronized, and the boundary separator follows whichever sticky region is active.

## Layout persistence

`DataGridView.ColumnKey` defines a stable key per column. In its absence, `SortMemberPath` is used. Every column needs non-empty, unique keys to use persistence.

| Type or operation | Contract |
|---|---|
| `DataGridViewState` | Versioned format with columns, sorting and pinned items' keys |
| `DataGridViewColumnState` | Key, display index, width, unit and visibility |
| `DataGridViewSortState` | Column key and sort direction |
| `CaptureState()` | Captures the current configuration without keeping references to the columns |
| `RestoreState(state)` | Fully validates and restores; unknown columns are ignored and new ones are appended |
| `SaveState(stream)` / `LoadState(stream)` | Serializes or reads JSON without closing the caller's stream |
| `ResetState()` | Restores the initial state captured when the control was loaded, and reports whether one was available |
| `StateRestored` | Raised once after a completed restore |

The current format uses `Version=1`. States with an unknown version, duplicate keys, repeated indices, invalid widths, invalid enum values, or no visible column are rejected before being applied. Pinned columns use `ColumnKey` or `SortMemberPath`; pinned rows are only saved when `RowKeyMemberPath` is configured.

## Multi-column sorting

## Per-column filters

`Filters` exposes the active filters. Use `SetFilter(column, value, operator)`, `ClearFilter(column)` and `ClearFilters()`. The available operators are `Contains`, `Equals`, `StartsWith`, `EndsWith`, `GreaterThan` and `LessThan`. `DataGridView.FilterMemberPath` chooses the queried property, and `DataGridView.CanUserFilter` disables filtering on a column. Any filter already present on the `ICollectionView` is preserved and combined with the control's own filters.

`IsMultiColumnSortEnabled` is `false` by default. When enabled, a plain click replaces the sort, `Shift+click` appends a criterion, and `Ctrl+click` removes the targeted column's criterion. `ApplySort(column, direction, append)` and `RemoveSort(column)` offer the same control from code. The read-only attached `SortPriority` property reports the position, starting at 1 when there are multiple criteria.
