<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TabBar

命名空间 `Godot`；class；继承 [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A control that provides a horizontal bar with tabs. Similar to [`TabContainer`](TabContainer.md) but is only in charge of drawing tabs, not interacting with children.

## 构造函数

### TabBar

```csharp
public TabBar()
```

## 属性

### ClipTabs

```csharp
public bool ClipTabs { get; set; }
```

引擎名：`clip_tabs`

If `true`, tabs overflowing this node's width will be hidden, displaying two navigation buttons instead. Otherwise, this node's minimum size is updated so that all tabs are visible.

### CloseWithMiddleMouse

```csharp
public bool CloseWithMiddleMouse { get; set; }
```

引擎名：`close_with_middle_mouse`

If `true`, middle-clicking on a tab will emit the `TabClosePressed` signal.

### CurrentTab

```csharp
public int CurrentTab { get; set; }
```

引擎名：`current_tab`

The index of the current selected tab. A value of `-1` means that no tab is selected and can only be set when `DeselectEnabled` is `true` or if all tabs are hidden or disabled.

### DeselectEnabled

```csharp
public bool DeselectEnabled { get; set; }
```

引擎名：`deselect_enabled`

If `true`, all tabs can be deselected so that no tab is selected. Click on the current tab to deselect it.

### DragToRearrangeEnabled

```csharp
public bool DragToRearrangeEnabled { get; set; }
```

引擎名：`drag_to_rearrange_enabled`

If `true`, tabs can be rearranged with mouse drag.

### MaxTabWidth

```csharp
public int MaxTabWidth { get; set; }
```

引擎名：`max_tab_width`

Sets the maximum width which all tabs should be limited to. Unlimited if set to `0`.

### ScrollToSelected

```csharp
public bool ScrollToSelected { get; set; }
```

引擎名：`scroll_to_selected`

If `true`, the tab offset will be changed to keep the currently selected tab visible.

### ScrollingEnabled

```csharp
public bool ScrollingEnabled { get; set; }
```

引擎名：`scrolling_enabled`

if `true`, the mouse's scroll wheel can be used to navigate the scroll view.

### SelectWithRmb

```csharp
public bool SelectWithRmb { get; set; }
```

引擎名：`select_with_rmb`

If `true`, enables selecting a tab with the right mouse button.

### SwitchOnDragHover

```csharp
public bool SwitchOnDragHover { get; set; }
```

引擎名：`switch_on_drag_hover`

If `true`, hovering over a tab while dragging something will switch to that tab. Does not have effect when hovering another tab to rearrange. The delay for when this happens is dictated by `hover_switch_wait_msec`.

### TabAlignment

```csharp
public TabBar.AlignmentMode TabAlignment { get; set; }
```

引擎名：`tab_alignment`

The horizontal alignment of the tabs.

### TabCloseDisplayPolicy

```csharp
public TabBar.CloseButtonDisplayPolicy TabCloseDisplayPolicy { get; set; }
```

引擎名：`tab_close_display_policy`

When the close button will appear on the tabs.

### TabCount

```csharp
public int TabCount { get; set; }
```

引擎名：`tab_count`

The number of tabs currently in the bar.

### TabsRearrangeGroup

```csharp
public int TabsRearrangeGroup { get; set; }
```

引擎名：`tabs_rearrange_group`

`TabBar`s with the same rearrange group ID will allow dragging the tabs between them. Enable drag with `DragToRearrangeEnabled`.

Setting this to `-1` will disable rearranging between `TabBar`s.

## 方法

### AddTab

```csharp
public void AddTab(string title = "", Texture2D icon = null)
```

引擎名：`add_tab`

Adds a new tab.

### ClearTabs

```csharp
public void ClearTabs()
```

引擎名：`clear_tabs`

Clears all tabs.

### EnsureTabVisible

```csharp
public void EnsureTabVisible(int idx)
```

引擎名：`ensure_tab_visible`

Moves the scroll view to make the tab visible.

### GetClipTabs

```csharp
public bool GetClipTabs()
```

引擎名：`get_clip_tabs`

### GetCloseWithMiddleMouse

```csharp
public bool GetCloseWithMiddleMouse()
```

引擎名：`get_close_with_middle_mouse`

### GetCurrentTab

```csharp
public int GetCurrentTab()
```

引擎名：`get_current_tab`

### GetDeselectEnabled

```csharp
public bool GetDeselectEnabled()
```

引擎名：`get_deselect_enabled`

### GetDragToRearrangeEnabled

```csharp
public bool GetDragToRearrangeEnabled()
```

引擎名：`get_drag_to_rearrange_enabled`

### GetMaxTabWidth

```csharp
public int GetMaxTabWidth()
```

引擎名：`get_max_tab_width`

### GetOffsetButtonsVisible

```csharp
public bool GetOffsetButtonsVisible()
```

引擎名：`get_offset_buttons_visible`

Returns `true` if the offset buttons (the ones that appear when there's not enough space for all tabs) are visible.

### GetPreviousTab

```csharp
public int GetPreviousTab()
```

引擎名：`get_previous_tab`

Returns the previously active tab index.

### GetScrollToSelected

```csharp
public bool GetScrollToSelected()
```

引擎名：`get_scroll_to_selected`

### GetScrollingEnabled

```csharp
public bool GetScrollingEnabled()
```

引擎名：`get_scrolling_enabled`

### GetSelectWithRmb

```csharp
public bool GetSelectWithRmb()
```

引擎名：`get_select_with_rmb`

### GetSwitchOnDragHover

```csharp
public bool GetSwitchOnDragHover()
```

引擎名：`get_switch_on_drag_hover`

### GetTabAlignment

```csharp
public TabBar.AlignmentMode GetTabAlignment()
```

引擎名：`get_tab_alignment`

### GetTabButtonIcon

```csharp
public Texture2D GetTabButtonIcon(int tabIdx)
```

引擎名：`get_tab_button_icon`

Returns the icon for the right button of the tab at index `tabIdx` or `null` if the right button has no icon.

### GetTabCloseDisplayPolicy

```csharp
public TabBar.CloseButtonDisplayPolicy GetTabCloseDisplayPolicy()
```

引擎名：`get_tab_close_display_policy`

### GetTabCount

```csharp
public int GetTabCount()
```

引擎名：`get_tab_count`

### GetTabIcon

```csharp
public Texture2D GetTabIcon(int tabIdx)
```

引擎名：`get_tab_icon`

Returns the icon for the tab at index `tabIdx` or `null` if the tab has no icon.

### GetTabIconMaxWidth

```csharp
public int GetTabIconMaxWidth(int tabIdx)
```

引擎名：`get_tab_icon_max_width`

Returns the maximum allowed width of the icon for the tab at index `tabIdx`.

### GetTabIdxAtPoint

```csharp
public int GetTabIdxAtPoint(Vector2 point)
```

引擎名：`get_tab_idx_at_point`

Returns the index of the tab at local coordinates `point`. Returns `-1` if the point is outside the control boundaries or if there's no tab at the queried position.

### GetTabLanguage

```csharp
public string GetTabLanguage(int tabIdx)
```

引擎名：`get_tab_language`

Returns tab title language code.

### GetTabMetadata

```csharp
public Variant GetTabMetadata(int tabIdx)
```

引擎名：`get_tab_metadata`

Returns the metadata value set to the tab at index `tabIdx` using `SetTabMetadata()`. If no metadata was previously set, returns `null` by default.

### GetTabOffset

```csharp
public int GetTabOffset()
```

引擎名：`get_tab_offset`

Returns the number of hidden tabs offsetted to the left.

### GetTabRect

```csharp
public Rect2 GetTabRect(int tabIdx)
```

引擎名：`get_tab_rect`

Returns tab [`Rect2`](Rect2.md) with local position and size.

### GetTabTextDirection

```csharp
public Control.TextDirection GetTabTextDirection(int tabIdx)
```

引擎名：`get_tab_text_direction`

Returns tab title text base writing direction.

### GetTabTitle

```csharp
public string GetTabTitle(int tabIdx)
```

引擎名：`get_tab_title`

Returns the title of the tab at index `tabIdx`.

### GetTabTooltip

```csharp
public string GetTabTooltip(int tabIdx)
```

引擎名：`get_tab_tooltip`

Returns the tooltip text of the tab at index `tabIdx`.

### GetTabsRearrangeGroup

```csharp
public int GetTabsRearrangeGroup()
```

引擎名：`get_tabs_rearrange_group`

### IsTabDisabled

```csharp
public bool IsTabDisabled(int tabIdx)
```

引擎名：`is_tab_disabled`

Returns `true` if the tab at index `tabIdx` is disabled.

### IsTabHidden

```csharp
public bool IsTabHidden(int tabIdx)
```

引擎名：`is_tab_hidden`

Returns `true` if the tab at index `tabIdx` is hidden.

### MoveTab

```csharp
public void MoveTab(int from, int to)
```

引擎名：`move_tab`

Moves a tab from `from` to `to`.

### RemoveTab

```csharp
public void RemoveTab(int tabIdx)
```

引擎名：`remove_tab`

Removes the tab at index `tabIdx`.

### SelectNextAvailable

```csharp
public bool SelectNextAvailable()
```

引擎名：`select_next_available`

Selects the first available tab with greater index than the currently selected. Returns `true` if tab selection changed.

### SelectPreviousAvailable

```csharp
public bool SelectPreviousAvailable()
```

引擎名：`select_previous_available`

Selects the first available tab with lower index than the currently selected. Returns `true` if tab selection changed.

### SetClipTabs

```csharp
public void SetClipTabs(bool clipTabs)
```

引擎名：`set_clip_tabs`

### SetCloseWithMiddleMouse

```csharp
public void SetCloseWithMiddleMouse(bool enabled)
```

引擎名：`set_close_with_middle_mouse`

### SetCurrentTab

```csharp
public void SetCurrentTab(int tabIdx)
```

引擎名：`set_current_tab`

### SetDeselectEnabled

```csharp
public void SetDeselectEnabled(bool enabled)
```

引擎名：`set_deselect_enabled`

### SetDragToRearrangeEnabled

```csharp
public void SetDragToRearrangeEnabled(bool enabled)
```

引擎名：`set_drag_to_rearrange_enabled`

### SetMaxTabWidth

```csharp
public void SetMaxTabWidth(int width)
```

引擎名：`set_max_tab_width`

### SetScrollToSelected

```csharp
public void SetScrollToSelected(bool enabled)
```

引擎名：`set_scroll_to_selected`

### SetScrollingEnabled

```csharp
public void SetScrollingEnabled(bool enabled)
```

引擎名：`set_scrolling_enabled`

### SetSelectWithRmb

```csharp
public void SetSelectWithRmb(bool enabled)
```

引擎名：`set_select_with_rmb`

### SetSwitchOnDragHover

```csharp
public void SetSwitchOnDragHover(bool enabled)
```

引擎名：`set_switch_on_drag_hover`

### SetTabAlignment

```csharp
public void SetTabAlignment(TabBar.AlignmentMode alignment)
```

引擎名：`set_tab_alignment`

### SetTabButtonIcon

```csharp
public void SetTabButtonIcon(int tabIdx, Texture2D icon)
```

引擎名：`set_tab_button_icon`

Sets an `icon` for the button of the tab at index `tabIdx` (located to the right, before the close button), making it visible and clickable (See `TabButtonPressed`). Giving it a `null` value will hide the button.

### SetTabCloseDisplayPolicy

```csharp
public void SetTabCloseDisplayPolicy(TabBar.CloseButtonDisplayPolicy policy)
```

引擎名：`set_tab_close_display_policy`

### SetTabCount

```csharp
public void SetTabCount(int count)
```

引擎名：`set_tab_count`

### SetTabDisabled

```csharp
public void SetTabDisabled(int tabIdx, bool disabled)
```

引擎名：`set_tab_disabled`

If `disabled` is `true`, disables the tab at index `tabIdx`, making it non-interactable.

### SetTabHidden

```csharp
public void SetTabHidden(int tabIdx, bool hidden)
```

引擎名：`set_tab_hidden`

If `hidden` is `true`, hides the tab at index `tabIdx`, making it disappear from the tab area.

### SetTabIcon

```csharp
public void SetTabIcon(int tabIdx, Texture2D icon)
```

引擎名：`set_tab_icon`

Sets an `icon` for the tab at index `tabIdx`.

### SetTabIconMaxWidth

```csharp
public void SetTabIconMaxWidth(int tabIdx, int width)
```

引擎名：`set_tab_icon_max_width`

Sets the maximum allowed width of the icon for the tab at index `tabIdx`. This limit is applied on top of the default size of the icon and on top of `icon_max_width`. The height is adjusted according to the icon's ratio.

### SetTabLanguage

```csharp
public void SetTabLanguage(int tabIdx, string language)
```

引擎名：`set_tab_language`

Sets the language code of the title for the tab at index `tabIdx` to `language`. This is used for line-breaking and text shaping algorithms. If `language` is empty, the current locale is used.

### SetTabMetadata

```csharp
public void SetTabMetadata(int tabIdx, Variant metadata)
```

引擎名：`set_tab_metadata`

Sets the metadata value for the tab at index `tabIdx`, which can be retrieved later using `GetTabMetadata()`.

### SetTabTextDirection

```csharp
public void SetTabTextDirection(int tabIdx, Control.TextDirection direction)
```

引擎名：`set_tab_text_direction`

Sets tab title base writing direction.

### SetTabTitle

```csharp
public void SetTabTitle(int tabIdx, string title)
```

引擎名：`set_tab_title`

Sets a `title` for the tab at index `tabIdx`.

### SetTabTooltip

```csharp
public void SetTabTooltip(int tabIdx, string tooltip)
```

引擎名：`set_tab_tooltip`

Sets a `tooltip` for tab at index `tabIdx`.

**Note:** By default, if the `tooltip` is empty and the tab text is truncated (not all characters fit into the tab), the title will be displayed as a tooltip. To hide the tooltip, assign `" "` as the `tooltip` text.

### SetTabsRearrangeGroup

```csharp
public void SetTabsRearrangeGroup(int groupId)
```

引擎名：`set_tabs_rearrange_group`

## 事件（信号）

### ActiveTabRearranged

```csharp
public event TabBar.ActiveTabRearrangedEventHandler ActiveTabRearranged
```

引擎名：`active_tab_rearranged`

Emitted when the active tab is rearranged via mouse drag. See `DragToRearrangeEnabled`.

### TabButtonPressed

```csharp
public event TabBar.TabButtonPressedEventHandler TabButtonPressed
```

引擎名：`tab_button_pressed`

Emitted when a tab's right button is pressed. See `SetTabButtonIcon()`.

### TabChanged

```csharp
public event TabBar.TabChangedEventHandler TabChanged
```

引擎名：`tab_changed`

Emitted when switching to another tab.

### TabClicked

```csharp
public event TabBar.TabClickedEventHandler TabClicked
```

引擎名：`tab_clicked`

Emitted when a tab is clicked, even if it is the current tab.

### TabClosePressed

```csharp
public event TabBar.TabClosePressedEventHandler TabClosePressed
```

引擎名：`tab_close_pressed`

Emitted when a tab's close button is pressed or, if `CloseWithMiddleMouse` is `true`, when middle-clicking on a tab.

**Note:** Tabs are not removed automatically; this behavior needs to be coded manually. For example:

```csharp
GetNode<TabBar>("TabBar").TabClosePressed += GetNode<TabBar>("TabBar").RemoveTab;

```

### TabHovered

```csharp
public event TabBar.TabHoveredEventHandler TabHovered
```

引擎名：`tab_hovered`

Emitted when a tab is hovered by the mouse.

### TabRmbClicked

```csharp
public event TabBar.TabRmbClickedEventHandler TabRmbClicked
```

引擎名：`tab_rmb_clicked`

Emitted when a tab is right-clicked.

### TabSelected

```csharp
public event TabBar.TabSelectedEventHandler TabSelected
```

引擎名：`tab_selected`

Emitted when a tab is selected via click, directional input, or script, even if it is the current tab.

## ActiveTabRearrangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.ActiveTabRearranged`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void ActiveTabRearrangedEventHandler(long idxTo)
```

## AlignmentMode

命名空间 `Godot`；enum

### 枚举值

- `Left = 0`：Aligns tabs to the left.
- `Center = 1`：Aligns tabs in the middle.
- `Right = 2`：Aligns tabs to the right.
- `Max = 3`：Represents the size of the `TabBar.AlignmentMode` enum.

## CloseButtonDisplayPolicy

命名空间 `Godot`；enum

### 枚举值

- `ShowNever = 0`：Never show the close buttons.
- `ShowActiveOnly = 1`：Only show the close button on the currently active tab.
- `ShowAlways = 2`：Show the close button on all tabs.
- `Max = 3`：Represents the size of the `TabBar.CloseButtonDisplayPolicy` enum.

## TabButtonPressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabButtonPressed`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabButtonPressedEventHandler(long tab)
```

## TabChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabChanged`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabChangedEventHandler(long tab)
```

## TabClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabClicked`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabClickedEventHandler(long tab)
```

## TabClosePressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabClosePressed`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabClosePressedEventHandler(long tab)
```

## TabHoveredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabHovered`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabHoveredEventHandler(long tab)
```

## TabRmbClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabRmbClicked`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabRmbClickedEventHandler(long tab)
```

## TabSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabBar.TabSelected`](TabBar.md) event of a [`TabBar`](TabBar.md) class.

### 签名

#### Invoke

```csharp
delegate void TabSelectedEventHandler(long tab)
```
