<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TabContainer

命名空间 `Godot`；class；继承 [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Arranges child controls into a tabbed view, creating a tab for each one. The active tab's corresponding control is made visible, while all other child controls are hidden. Ignores non-control children.

**Note:** The drawing of the clickable tabs is handled by this node; [`TabBar`](TabBar.md) is not needed.

## 构造函数

### TabContainer

```csharp
public TabContainer()
```

## 属性

### AllTabsInFront

```csharp
public bool AllTabsInFront { get; set; }
```

引擎名：`all_tabs_in_front`

This doesn't do anything.

### ClipTabs

```csharp
public bool ClipTabs { get; set; }
```

引擎名：`clip_tabs`

If `true`, tabs overflowing this node's width will be hidden, displaying two navigation buttons instead. Otherwise, this node's minimum size is updated so that all tabs are visible.

### CurrentTab

```csharp
public int CurrentTab { get; set; }
```

引擎名：`current_tab`

The current tab index. When set, this index's [`Control`](Control.md) node's `visible` property is set to `true` and all others are set to `false`.

A value of `-1` means that no tab is selected.

### DeselectEnabled

```csharp
public bool DeselectEnabled { get; set; }
```

引擎名：`deselect_enabled`

If `true`, all tabs can be deselected so that no tab is selected. Click on the `CurrentTab` to deselect it.

Only the tab header will be shown if no tabs are selected.

### DragToRearrangeEnabled

```csharp
public bool DragToRearrangeEnabled { get; set; }
```

引擎名：`drag_to_rearrange_enabled`

If `true`, tabs can be rearranged with mouse drag.

### SwitchOnDragHover

```csharp
public bool SwitchOnDragHover { get; set; }
```

引擎名：`switch_on_drag_hover`

If `true`, hovering over a tab while dragging something will switch to that tab. Does not have effect when hovering another tab to rearrange.

### TabAlignment

```csharp
public TabBar.AlignmentMode TabAlignment { get; set; }
```

引擎名：`tab_alignment`

The position at which tabs will be placed.

### TabFocusMode

```csharp
public Control.FocusModeEnum TabFocusMode { get; set; }
```

引擎名：`tab_focus_mode`

The focus access mode for the internal [`TabBar`](TabBar.md) node.

### TabsPosition

```csharp
public TabContainer.TabPosition TabsPosition { get; set; }
```

引擎名：`tabs_position`

The horizontal alignment of the tabs.

### TabsRearrangeGroup

```csharp
public int TabsRearrangeGroup { get; set; }
```

引擎名：`tabs_rearrange_group`

`TabContainer`s with the same rearrange group ID will allow dragging the tabs between them. Enable drag with `DragToRearrangeEnabled`.

Setting this to `-1` will disable rearranging between `TabContainer`s.

### TabsVisible

```csharp
public bool TabsVisible { get; set; }
```

引擎名：`tabs_visible`

If `true`, tabs are visible. If `false`, tabs' content and titles are hidden.

### UseHiddenTabsForMinSize

```csharp
public bool UseHiddenTabsForMinSize { get; set; }
```

引擎名：`use_hidden_tabs_for_min_size`

If `true`, child [`Control`](Control.md) nodes that are hidden have their minimum size take into account in the total, instead of only the currently visible one.

## 方法

### AreTabsVisible

```csharp
public bool AreTabsVisible()
```

引擎名：`are_tabs_visible`

### GetClipTabs

```csharp
public bool GetClipTabs()
```

引擎名：`get_clip_tabs`

### GetCurrentTab

```csharp
public int GetCurrentTab()
```

引擎名：`get_current_tab`

### GetCurrentTabControl

```csharp
public Control GetCurrentTabControl()
```

引擎名：`get_current_tab_control`

Returns the child [`Control`](Control.md) node located at the active tab index.

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

### GetPopup

```csharp
public Popup GetPopup()
```

引擎名：`get_popup`

Returns the [`Popup`](Popup.md) node instance if one has been set already with `SetPopup()`.

**Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their [`Window.Visible`](Window.md) property.

### GetPreviousTab

```csharp
public int GetPreviousTab()
```

引擎名：`get_previous_tab`

Returns the previously active tab index.

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

### GetTabBar

```csharp
public TabBar GetTabBar()
```

引擎名：`get_tab_bar`

Returns the [`TabBar`](TabBar.md) contained in this container.

**Warning:** This is a required internal node, removing and freeing it or editing its tabs may cause a crash. If you wish to edit the tabs, use the methods provided in `TabContainer`.

### GetTabButtonIcon

```csharp
public Texture2D GetTabButtonIcon(int tabIdx)
```

引擎名：`get_tab_button_icon`

Returns the button icon from the tab at index `tabIdx`.

### GetTabControl

```csharp
public Control GetTabControl(int tabIdx)
```

引擎名：`get_tab_control`

Returns the [`Control`](Control.md) node from the tab at index `tabIdx`.

### GetTabCount

```csharp
public int GetTabCount()
```

引擎名：`get_tab_count`

Returns the number of tabs.

### GetTabFocusMode

```csharp
public Control.FocusModeEnum GetTabFocusMode()
```

引擎名：`get_tab_focus_mode`

### GetTabIcon

```csharp
public Texture2D GetTabIcon(int tabIdx)
```

引擎名：`get_tab_icon`

Returns the [`Texture2D`](Texture2D.md) for the tab at index `tabIdx` or `null` if the tab has no [`Texture2D`](Texture2D.md).

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

### GetTabIdxFromControl

```csharp
public int GetTabIdxFromControl(Control control)
```

引擎名：`get_tab_idx_from_control`

Returns the index of the tab tied to the given `control`. The control must be a child of the `TabContainer`.

### GetTabMetadata

```csharp
public Variant GetTabMetadata(int tabIdx)
```

引擎名：`get_tab_metadata`

Returns the metadata value set to the tab at index `tabIdx` using `SetTabMetadata()`. If no metadata was previously set, returns `null` by default.

### GetTabTitle

```csharp
public string GetTabTitle(int tabIdx)
```

引擎名：`get_tab_title`

Returns the title of the tab at index `tabIdx`. Tab titles default to the name of the indexed child node, but this can be overridden with `SetTabTitle()`.

### GetTabTooltip

```csharp
public string GetTabTooltip(int tabIdx)
```

引擎名：`get_tab_tooltip`

Returns the tooltip text of the tab at index `tabIdx`.

### GetTabsPosition

```csharp
public TabContainer.TabPosition GetTabsPosition()
```

引擎名：`get_tabs_position`

### GetTabsRearrangeGroup

```csharp
public int GetTabsRearrangeGroup()
```

引擎名：`get_tabs_rearrange_group`

### GetUseHiddenTabsForMinSize

```csharp
public bool GetUseHiddenTabsForMinSize()
```

引擎名：`get_use_hidden_tabs_for_min_size`

### IsAllTabsInFront

```csharp
public bool IsAllTabsInFront()
```

引擎名：`is_all_tabs_in_front`

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

### SetAllTabsInFront

```csharp
public void SetAllTabsInFront(bool isFront)
```

引擎名：`set_all_tabs_in_front`

### SetClipTabs

```csharp
public void SetClipTabs(bool clipTabs)
```

引擎名：`set_clip_tabs`

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

### SetPopup

```csharp
public void SetPopup(Node popup)
```

引擎名：`set_popup`

If set on a [`Popup`](Popup.md) node instance, a popup menu icon appears in the top-right corner of the `TabContainer` (setting it to `null` will make it go away). Clicking it will expand the [`Popup`](Popup.md) node.

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

Sets the button icon from the tab at index `tabIdx`.

### SetTabDisabled

```csharp
public void SetTabDisabled(int tabIdx, bool disabled)
```

引擎名：`set_tab_disabled`

If `disabled` is `true`, disables the tab at index `tabIdx`, making it non-interactable.

### SetTabFocusMode

```csharp
public void SetTabFocusMode(Control.FocusModeEnum focusMode)
```

引擎名：`set_tab_focus_mode`

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

Sets an icon for the tab at index `tabIdx`.

### SetTabIconMaxWidth

```csharp
public void SetTabIconMaxWidth(int tabIdx, int width)
```

引擎名：`set_tab_icon_max_width`

Sets the maximum allowed width of the icon for the tab at index `tabIdx`. This limit is applied on top of the default size of the icon and on top of `icon_max_width`. The height is adjusted according to the icon's ratio.

### SetTabMetadata

```csharp
public void SetTabMetadata(int tabIdx, Variant metadata)
```

引擎名：`set_tab_metadata`

Sets the metadata value for the tab at index `tabIdx`, which can be retrieved later using `GetTabMetadata()`.

### SetTabTitle

```csharp
public void SetTabTitle(int tabIdx, string title)
```

引擎名：`set_tab_title`

Sets a custom title for the tab at index `tabIdx` (tab titles default to the name of the indexed child node). Set it back to the child's name to make the tab default to it again.

### SetTabTooltip

```csharp
public void SetTabTooltip(int tabIdx, string tooltip)
```

引擎名：`set_tab_tooltip`

Sets a custom tooltip text for tab at index `tabIdx`.

**Note:** By default, if the `tooltip` is empty and the tab text is truncated (not all characters fit into the tab), the title will be displayed as a tooltip. To hide the tooltip, assign `" "` as the `tooltip` text.

### SetTabsPosition

```csharp
public void SetTabsPosition(TabContainer.TabPosition tabsPosition)
```

引擎名：`set_tabs_position`

### SetTabsRearrangeGroup

```csharp
public void SetTabsRearrangeGroup(int groupId)
```

引擎名：`set_tabs_rearrange_group`

### SetTabsVisible

```csharp
public void SetTabsVisible(bool visible)
```

引擎名：`set_tabs_visible`

### SetUseHiddenTabsForMinSize

```csharp
public void SetUseHiddenTabsForMinSize(bool enabled)
```

引擎名：`set_use_hidden_tabs_for_min_size`

## 事件（信号）

### ActiveTabRearranged

```csharp
public event TabContainer.ActiveTabRearrangedEventHandler ActiveTabRearranged
```

引擎名：`active_tab_rearranged`

Emitted when the active tab is rearranged via mouse drag. See `DragToRearrangeEnabled`.

### PrePopupPressed

```csharp
public event Action PrePopupPressed
```

引擎名：`pre_popup_pressed`

Emitted when the `TabContainer`'s [`Popup`](Popup.md) button is clicked. See `SetPopup()` for details.

### TabButtonPressed

```csharp
public event TabContainer.TabButtonPressedEventHandler TabButtonPressed
```

引擎名：`tab_button_pressed`

Emitted when the user clicks on the button icon on this tab.

### TabChanged

```csharp
public event TabContainer.TabChangedEventHandler TabChanged
```

引擎名：`tab_changed`

Emitted when switching to another tab.

### TabClicked

```csharp
public event TabContainer.TabClickedEventHandler TabClicked
```

引擎名：`tab_clicked`

Emitted when a tab is clicked, even if it is the current tab.

### TabHovered

```csharp
public event TabContainer.TabHoveredEventHandler TabHovered
```

引擎名：`tab_hovered`

Emitted when a tab is hovered by the mouse.

### TabSelected

```csharp
public event TabContainer.TabSelectedEventHandler TabSelected
```

引擎名：`tab_selected`

Emitted when a tab is selected via click, directional input, or script, even if it is the current tab.

## ActiveTabRearrangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabContainer.ActiveTabRearranged`](TabContainer.md) event of a [`TabContainer`](TabContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void ActiveTabRearrangedEventHandler(long idxTo)
```

## TabButtonPressedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabContainer.TabButtonPressed`](TabContainer.md) event of a [`TabContainer`](TabContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void TabButtonPressedEventHandler(long tab)
```

## TabChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabContainer.TabChanged`](TabContainer.md) event of a [`TabContainer`](TabContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void TabChangedEventHandler(long tab)
```

## TabClickedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabContainer.TabClicked`](TabContainer.md) event of a [`TabContainer`](TabContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void TabClickedEventHandler(long tab)
```

## TabHoveredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabContainer.TabHovered`](TabContainer.md) event of a [`TabContainer`](TabContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void TabHoveredEventHandler(long tab)
```

## TabPosition

命名空间 `Godot`；enum

### 枚举值

- `Top = 0`：Places the tab bar at the top.
- `Bottom = 1`：Places the tab bar at the bottom.
- `Max = 2`：Represents the size of the `TabContainer.TabPosition` enum.

## TabSelectedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`TabContainer.TabSelected`](TabContainer.md) event of a [`TabContainer`](TabContainer.md) class.

### 签名

#### Invoke

```csharp
delegate void TabSelectedEventHandler(long tab)
```
