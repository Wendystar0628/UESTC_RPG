<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AccessibilityServer

命名空间 `Godot`；static class

## 属性

### Singleton

```csharp
public static AccessibilityServerInstance Singleton { get; }
```

## 方法

### CreateElement

```csharp
public static Rid CreateElement(int windowId, AccessibilityServer.AccessibilityRole role)
```

引擎名：`create_element`

Creates a new, empty accessibility element resource.

**Note:** An accessibility element is created and freed automatically for each [`Node`](Node.md). In general, this function should not be called manually.

### CreateSubElement

```csharp
public static Rid CreateSubElement(Rid parentRid, AccessibilityServer.AccessibilityRole role, int insertPos = -1)
```

引擎名：`create_sub_element`

Creates a new, empty accessibility sub-element resource. Sub-elements can be used to provide accessibility information for objects which are not [`Node`](Node.md)s, such as list items, table cells, or menu items. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `FreeElement()` method.

### CreateSubTextEditElements

```csharp
public static Rid CreateSubTextEditElements(Rid parentRid, Rid shapedText, float minHeight, int insertPos = -1, bool isLastLine = false)
```

引擎名：`create_sub_text_edit_elements`

Creates a new, empty accessibility sub-element from the shaped text buffer. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `FreeElement()` method.

If `isLastLine` is `true`, no trailing newline is appended to the text content. Set to `true` for the last line in multi-line text fields and for single-line text fields.

### ElementGetMeta

```csharp
public static Variant ElementGetMeta(Rid id)
```

引擎名：`element_get_meta`

Returns the metadata of the accessibility element `id`.

### ElementSetMeta

```csharp
public static void ElementSetMeta(Rid id, Variant meta)
```

引擎名：`element_set_meta`

Sets the metadata of the accessibility element `id` to `meta`.

### FreeElement

```csharp
public static void FreeElement(Rid id)
```

引擎名：`free_element`

Frees the accessibility element `id` created by `CreateElement()`, `CreateSubElement()`, or `CreateSubTextEditElements()`.

### GetWindowRoot

```csharp
public static Rid GetWindowRoot(int windowId)
```

引擎名：`get_window_root`

Returns the main accessibility element of the OS native window.

### HasElement

```csharp
public static bool HasElement(Rid id)
```

引擎名：`has_element`

Returns `true` if `id` is a valid accessibility element.

### IsSupported

```csharp
public static bool IsSupported()
```

引擎名：`is_supported`

Returns `true` if screen reader is support by this implementation.

### SetWindowFocused

```csharp
public static void SetWindowFocused(int windowId, bool focused)
```

引擎名：`set_window_focused`

Sets the window focused state for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### SetWindowRect

```csharp
public static void SetWindowRect(int windowId, Rect2 rectOut, Rect2 rectIn)
```

引擎名：`set_window_rect`

Sets window outer (with decorations) and inner (without decorations) bounds for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### UpdateAddAction

```csharp
public static void UpdateAddAction(Rid id, AccessibilityServer.AccessibilityAction action, Callable callable)
```

引擎名：`update_add_action`

Adds a callback for the accessibility action (action which can be performed by using a special screen reader command or buttons on the Braille display), and marks this action as supported. The action callback receives one [`Variant`](Variant.md) argument, which value depends on action type.

### UpdateAddChild

```csharp
public static void UpdateAddChild(Rid id, Rid childId)
```

引擎名：`update_add_child`

Adds a child accessibility element.

**Note:** [`Node`](Node.md) children and sub-elements are added to the child list automatically.

### UpdateAddCustomAction

```csharp
public static void UpdateAddCustomAction(Rid id, int actionId, string actionDescription)
```

引擎名：`update_add_custom_action`

Adds support for a custom accessibility action. `actionId` is passed as an argument to the callback of [`AccessibilityServer.AccessibilityAction.Custom`](AccessibilityServer.md) action.

### UpdateAddRelatedControls

```csharp
public static void UpdateAddRelatedControls(Rid id, Rid relatedId)
```

引擎名：`update_add_related_controls`

Adds an element that is controlled by this element.

### UpdateAddRelatedDescribedBy

```csharp
public static void UpdateAddRelatedDescribedBy(Rid id, Rid relatedId)
```

引擎名：`update_add_related_described_by`

Adds an element that describes this element.

### UpdateAddRelatedDetails

```csharp
public static void UpdateAddRelatedDetails(Rid id, Rid relatedId)
```

引擎名：`update_add_related_details`

Adds an element that details this element.

### UpdateAddRelatedFlowTo

```csharp
public static void UpdateAddRelatedFlowTo(Rid id, Rid relatedId)
```

引擎名：`update_add_related_flow_to`

Adds an element that this element flow into.

### UpdateAddRelatedLabeledBy

```csharp
public static void UpdateAddRelatedLabeledBy(Rid id, Rid relatedId)
```

引擎名：`update_add_related_labeled_by`

Adds an element that labels this element.

### UpdateAddRelatedRadioGroup

```csharp
public static void UpdateAddRelatedRadioGroup(Rid id, Rid relatedId)
```

引擎名：`update_add_related_radio_group`

Adds an element that is part of the same radio group.

**Note:** This method should be called on each element of the group, using all other elements as `relatedId`.

### UpdateSetActiveDescendant

```csharp
public static void UpdateSetActiveDescendant(Rid id, Rid otherId)
```

引擎名：`update_set_active_descendant`

Adds an element that is an active descendant of this element.

### UpdateSetBackgroundColor

```csharp
public static void UpdateSetBackgroundColor(Rid id, Color color)
```

引擎名：`update_set_background_color`

Sets element background color.

### UpdateSetBounds

```csharp
public static void UpdateSetBounds(Rid id, Rect2 rect)
```

引擎名：`update_set_bounds`

Sets element bounding box, relative to the node position.

### UpdateSetBrailleLabel

```csharp
public static void UpdateSetBrailleLabel(Rid id, string name)
```

引擎名：`update_set_braille_label`

Sets element accessibility label for Braille display.

### UpdateSetBrailleRoleDescription

```csharp
public static void UpdateSetBrailleRoleDescription(Rid id, string description)
```

引擎名：`update_set_braille_role_description`

Sets element accessibility role description for Braille display.

### UpdateSetChecked

```csharp
public static void UpdateSetChecked(Rid id, bool checekd)
```

引擎名：`update_set_checked`

Sets element checked state.

### UpdateSetClassname

```csharp
public static void UpdateSetClassname(Rid id, string classname)
```

引擎名：`update_set_classname`

Sets element class name.

### UpdateSetColorValue

```csharp
public static void UpdateSetColorValue(Rid id, Color color)
```

引擎名：`update_set_color_value`

Sets element color value.

### UpdateSetDescription

```csharp
public static void UpdateSetDescription(Rid id, string description)
```

引擎名：`update_set_description`

Sets element accessibility description.

### UpdateSetErrorMessage

```csharp
public static void UpdateSetErrorMessage(Rid id, Rid otherId)
```

引擎名：`update_set_error_message`

Sets an element which contains an error message for this element.

### UpdateSetExtraInfo

```csharp
public static void UpdateSetExtraInfo(Rid id, string name)
```

引擎名：`update_set_extra_info`

Sets element accessibility extra information added to the element name.

### UpdateSetFlag

```csharp
public static void UpdateSetFlag(Rid id, AccessibilityServer.AccessibilityFlags flag, bool value)
```

引擎名：`update_set_flag`

Sets element flag.

### UpdateSetFocus

```csharp
public static void UpdateSetFocus(Rid id)
```

引擎名：`update_set_focus`

Sets currently focused element.

### UpdateSetForegroundColor

```csharp
public static void UpdateSetForegroundColor(Rid id, Color color)
```

引擎名：`update_set_foreground_color`

Sets element foreground color.

### UpdateSetInPageLinkTarget

```csharp
public static void UpdateSetInPageLinkTarget(Rid id, Rid otherId)
```

引擎名：`update_set_in_page_link_target`

Sets target element for the link.

### UpdateSetLanguage

```csharp
public static void UpdateSetLanguage(Rid id, string language)
```

引擎名：`update_set_language`

Sets element text language.

### UpdateSetListItemCount

```csharp
public static void UpdateSetListItemCount(Rid id, int size)
```

引擎名：`update_set_list_item_count`

Sets number of items in the list.

### UpdateSetListItemExpanded

```csharp
public static void UpdateSetListItemExpanded(Rid id, bool expanded)
```

引擎名：`update_set_list_item_expanded`

Sets list/tree item expanded status.

### UpdateSetListItemIndex

```csharp
public static void UpdateSetListItemIndex(Rid id, int index)
```

引擎名：`update_set_list_item_index`

Sets the position of the element in the list.

### UpdateSetListItemLevel

```csharp
public static void UpdateSetListItemLevel(Rid id, int level)
```

引擎名：`update_set_list_item_level`

Sets the hierarchical level of the element in the list.

### UpdateSetListItemSelected

```csharp
public static void UpdateSetListItemSelected(Rid id, bool selected)
```

引擎名：`update_set_list_item_selected`

Sets list/tree item selected status.

### UpdateSetListOrientation

```csharp
public static void UpdateSetListOrientation(Rid id, bool vertical)
```

引擎名：`update_set_list_orientation`

Sets the orientation of the list elements.

### UpdateSetLive

```csharp
public static void UpdateSetLive(Rid id, AccessibilityServer.AccessibilityLiveMode live)
```

引擎名：`update_set_live`

Sets the priority of the live region updates.

### UpdateSetMemberOf

```csharp
public static void UpdateSetMemberOf(Rid id, Rid groupId)
```

引擎名：`update_set_member_of`

Sets the element to be a member of the group.

### UpdateSetName

```csharp
public static void UpdateSetName(Rid id, string name)
```

引擎名：`update_set_name`

Sets element accessibility name.

### UpdateSetNextOnLine

```csharp
public static void UpdateSetNextOnLine(Rid id, Rid otherId)
```

引擎名：`update_set_next_on_line`

Sets next element on the line.

### UpdateSetNumJump

```csharp
public static void UpdateSetNumJump(Rid id, double jump)
```

引擎名：`update_set_num_jump`

Sets numeric value jump.

### UpdateSetNumRange

```csharp
public static void UpdateSetNumRange(Rid id, double min, double max)
```

引擎名：`update_set_num_range`

Sets numeric value range.

### UpdateSetNumStep

```csharp
public static void UpdateSetNumStep(Rid id, double step)
```

引擎名：`update_set_num_step`

Sets numeric value step.

### UpdateSetNumValue

```csharp
public static void UpdateSetNumValue(Rid id, double position)
```

引擎名：`update_set_num_value`

Sets numeric value.

### UpdateSetPlaceholder

```csharp
public static void UpdateSetPlaceholder(Rid id, string placeholder)
```

引擎名：`update_set_placeholder`

Sets placeholder text.

### UpdateSetPopupType

```csharp
public static void UpdateSetPopupType(Rid id, AccessibilityServer.AccessibilityPopupType popup)
```

引擎名：`update_set_popup_type`

Sets popup type for popup buttons.

### UpdateSetPreviousOnLine

```csharp
public static void UpdateSetPreviousOnLine(Rid id, Rid otherId)
```

引擎名：`update_set_previous_on_line`

Sets previous element on the line.

### UpdateSetRole

```csharp
public static void UpdateSetRole(Rid id, AccessibilityServer.AccessibilityRole role)
```

引擎名：`update_set_role`

Sets element accessibility role.

### UpdateSetRoleDescription

```csharp
public static void UpdateSetRoleDescription(Rid id, string description)
```

引擎名：`update_set_role_description`

Sets element accessibility role description text.

### UpdateSetScrollX

```csharp
public static void UpdateSetScrollX(Rid id, double position)
```

引擎名：`update_set_scroll_x`

Sets scroll bar x position.

### UpdateSetScrollXRange

```csharp
public static void UpdateSetScrollXRange(Rid id, double min, double max)
```

引擎名：`update_set_scroll_x_range`

Sets scroll bar x range.

### UpdateSetScrollY

```csharp
public static void UpdateSetScrollY(Rid id, double position)
```

引擎名：`update_set_scroll_y`

Sets scroll bar y position.

### UpdateSetScrollYRange

```csharp
public static void UpdateSetScrollYRange(Rid id, double min, double max)
```

引擎名：`update_set_scroll_y_range`

Sets scroll bar y range.

### UpdateSetShortcut

```csharp
public static void UpdateSetShortcut(Rid id, string shortcut)
```

引擎名：`update_set_shortcut`

Sets the list of keyboard shortcuts used by element.

### UpdateSetStateDescription

```csharp
public static void UpdateSetStateDescription(Rid id, string description)
```

引擎名：`update_set_state_description`

Sets human-readable description of the current checked state.

### UpdateSetTableCellPosition

```csharp
public static void UpdateSetTableCellPosition(Rid id, int rowIndex, int columnIndex)
```

引擎名：`update_set_table_cell_position`

Sets cell position in the table.

### UpdateSetTableCellSpan

```csharp
public static void UpdateSetTableCellSpan(Rid id, int rowSpan, int columnSpan)
```

引擎名：`update_set_table_cell_span`

Sets cell row/column span.

### UpdateSetTableColumnCount

```csharp
public static void UpdateSetTableColumnCount(Rid id, int count)
```

引擎名：`update_set_table_column_count`

Sets number of columns in the table.

### UpdateSetTableColumnIndex

```csharp
public static void UpdateSetTableColumnIndex(Rid id, int index)
```

引擎名：`update_set_table_column_index`

Sets position of the column.

### UpdateSetTableRowCount

```csharp
public static void UpdateSetTableRowCount(Rid id, int count)
```

引擎名：`update_set_table_row_count`

Sets number of rows in the table.

### UpdateSetTableRowIndex

```csharp
public static void UpdateSetTableRowIndex(Rid id, int index)
```

引擎名：`update_set_table_row_index`

Sets position of the row in the table.

### UpdateSetTextAlign

```csharp
public static void UpdateSetTextAlign(Rid id, HorizontalAlignment align)
```

引擎名：`update_set_text_align`

Sets element text alignment.

### UpdateSetTextDecorations

```csharp
public static void UpdateSetTextDecorations(Rid id, bool underline, bool strikethrough, bool overline, Color? color = null)
```

引擎名：`update_set_text_decorations`

Sets text underline/overline/strikethrough.

参数：
- `color`：If the parameter is null, then the default value is `new Color(0.0f, 0.0f, 0.0f, 1.0f)`.

### UpdateSetTextOrientation

```csharp
public static void UpdateSetTextOrientation(Rid id, bool vertical)
```

引擎名：`update_set_text_orientation`

Sets text orientation.

### UpdateSetTextSelection

```csharp
public static void UpdateSetTextSelection(Rid id, Rid textStartId, int startChar, Rid textEndId, int endChar)
```

引擎名：`update_set_text_selection`

Sets text selection to the text field. `textStartId` and `textEndId` should be elements created by `CreateSubTextEditElements()`. Character offsets are relative to the corresponding element.

### UpdateSetTooltip

```csharp
public static void UpdateSetTooltip(Rid id, string tooltip)
```

引擎名：`update_set_tooltip`

Sets tooltip text.

### UpdateSetTransform

```csharp
public static void UpdateSetTransform(Rid id, Transform2D transform)
```

引擎名：`update_set_transform`

Sets element 2D transform.

### UpdateSetUrl

```csharp
public static void UpdateSetUrl(Rid id, string url)
```

引擎名：`update_set_url`

Sets link URL.

### UpdateSetValue

```csharp
public static void UpdateSetValue(Rid id, string value)
```

引擎名：`update_set_value`

Sets element text value.

## AccessibilityAction

命名空间 `Godot`；enum

### 枚举值

- `Click = 0`：Single click action, callback argument is not set.
- `Focus = 1`：Focus action, callback argument is not set.
- `Blur = 2`：Blur action, callback argument is not set.
- `Collapse = 3`：Collapse action, callback argument is not set.
- `Expand = 4`：Expand action, callback argument is not set.
- `Decrement = 5`：Decrement action, callback argument is not set.
- `Increment = 6`：Increment action, callback argument is not set.
- `HideTooltip = 7`：Hide tooltip action, callback argument is not set.
- `ShowTooltip = 8`：Show tooltip action, callback argument is not set.
- `SetTextSelection = 9`：Set text selection action, callback argument is set to [`Collections.Dictionary`](Collections.Dictionary.md) with the following keys:
- `ReplaceSelectedText = 10`：Replace text action, callback argument is set to `System.String` with the replacement text.
- `ScrollBackward = 11`：Scroll backward action, callback argument is not set.
- `ScrollDown = 12`：Scroll down action, callback argument is set to [`AccessibilityServer.AccessibilityScrollUnit`](AccessibilityServer.md).
- `ScrollForward = 13`：Scroll forward action, callback argument is not set.
- `ScrollLeft = 14`：Scroll left action, callback argument is set to [`AccessibilityServer.AccessibilityScrollUnit`](AccessibilityServer.md).
- `ScrollRight = 15`：Scroll right action, callback argument is set to [`AccessibilityServer.AccessibilityScrollUnit`](AccessibilityServer.md).
- `ScrollUp = 16`：Scroll up action, callback argument is set to [`AccessibilityServer.AccessibilityScrollUnit`](AccessibilityServer.md).
- `ScrollIntoView = 17`：Scroll into view action, callback argument is set to [`AccessibilityServer.AccessibilityScrollHint`](AccessibilityServer.md).
- `ScrollToPoint = 18`：Scroll to point action, callback argument is set to [`Vector2`](Vector2.md) with the relative point coordinates.
- `SetScrollOffset = 19`：Set scroll offset action, callback argument is set to [`Vector2`](Vector2.md) with the scroll offset.
- `SetValue = 20`：Set value action, callback argument is set to `System.String` or number with the new value.
- `ShowContextMenu = 21`：Show context menu action, callback argument is not set.
- `Custom = 22`：Custom action, callback argument is set to the integer action ID.

## AccessibilityFlags

命名空间 `Godot`；enum

### 枚举值

- `Hidden = 0`：Element is hidden for accessibility tools.
- `Multiselectable = 1`：Element supports multiple item selection.
- `Required = 2`：Element require user input.
- `Visited = 3`：Element is a visited link.
- `Busy = 4`：Element content is not ready (e.g.
- `Modal = 5`：Element is modal window.
- `TouchPassthrough = 6`：Element allows touches to be passed through when a screen reader is in touch exploration mode.
- `Readonly = 7`：Element is text field with selectable but read-only text.
- `Disabled = 8`：Element is disabled.
- `ClipsChildren = 9`：Element clips children.

## AccessibilityLiveMode

命名空间 `Godot`；enum

### 枚举值

- `Off = 0`：Indicates that updates to the live region should not be presented.
- `Polite = 1`：Indicates that updates to the live region should be presented at the next opportunity (for example at the end of speaking the current sentence).
- `Assertive = 2`：Indicates that updates to the live region have the highest priority and should be presented immediately.

## AccessibilityPopupType

命名空间 `Godot`；enum

### 枚举值

- `Menu = 0`：Popup menu.
- `List = 1`：Popup list.
- `Tree = 2`：Popup tree view.
- `Dialog = 3`：Popup dialog.

## AccessibilityRole

命名空间 `Godot`；enum

### 枚举值

- `Unknown = 0`：Unknown or custom role.
- `DefaultButton = 1`：Default dialog button element.
- `Audio = 2`：Audio player element.
- `Video = 3`：Video player element.
- `StaticText = 4`：Non-editable text label.
- `Container = 5`：Container element.
- `Panel = 6`：Panel container element.
- `Button = 7`：Button element.
- `Link = 8`：Link element.
- `CheckBox = 9`：Check box element.
- `RadioButton = 10`：Radio button element.
- `CheckButton = 11`：Check button element.
- `ScrollBar = 12`：Scroll bar element.
- `ScrollView = 13`：Scroll container element.
- `Splitter = 14`：Container splitter handle element.
- `Slider = 15`：Slider element.
- `SpinButton = 16`：Spin box element.
- `ProgressIndicator = 17`：Progress indicator element.
- `TextField = 18`：Editable text field element.
- `MultilineTextField = 19`：Multiline editable text field element.
- `ColorPicker = 20`：Color picker element.
- `Table = 21`：Table element.
- `Cell = 22`：Table/tree cell element.
- `Row = 23`：Table/tree row element.
- `RowGroup = 24`：Table/tree row group element.
- `RowHeader = 25`：Table/tree row header element.
- `ColumnHeader = 26`：Table/tree column header element.
- `Tree = 27`：Tree view element.
- `TreeItem = 28`：Tree view item element.
- `List = 29`：List element.
- `ListItem = 30`：List item element.
- `ListBox = 31`：List view element.
- `ListBoxOption = 32`：List view item element.
- `TabBar = 33`：Tab bar element.
- `Tab = 34`：Tab bar item element.
- `TabPanel = 35`：Tab panel element.
- `MenuBar = 36`：Menu bar element.
- `Menu = 37`：Popup menu element.
- `MenuItem = 38`：Popup menu item element.
- `MenuItemCheckBox = 39`：Popup menu check button item element.
- `MenuItemRadio = 40`：Popup menu radio button item element.
- `Image = 41`：Image element.
- `Window = 42`：Window element.
- `TitleBar = 43`：Embedded window title bar element.
- `Dialog = 44`：Dialog window element.
- `Tooltip = 45`：Tooltip element.
- `Region = 46`：Region/landmark element.
- `TextRun = 47`：Unifor text run.

## AccessibilityScrollHint

命名空间 `Godot`；enum

### 枚举值

- `TopLeft = 0`：A preferred position for the node scrolled into view.
- `BottomRight = 1`：A preferred position for the node scrolled into view.
- `TopEdge = 2`：A preferred position for the node scrolled into view.
- `BottomEdge = 3`：A preferred position for the node scrolled into view.
- `LeftEdge = 4`：A preferred position for the node scrolled into view.
- `RightEdge = 5`：A preferred position for the node scrolled into view.

## AccessibilityScrollUnit

命名空间 `Godot`；enum

### 枚举值

- `Item = 0`：The amount by which to scroll.
- `Page = 1`：The amount by which to scroll.
