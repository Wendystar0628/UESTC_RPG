<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DisplayServer

命名空间 `Godot`；static class

`DisplayServer` handles everything related to window management. It is separated from [`OS`](OS.md) as a single operating system may support multiple display servers.

**Headless mode:** Starting the engine with the `--headless` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html) disables all rendering and window management functions. Most functions from `DisplayServer` will return dummy values in this case.

## 属性

### Singleton

```csharp
public static DisplayServerInstance Singleton { get; }
```

## 方法

### AccessibilityCreateElement

```csharp
public static Rid AccessibilityCreateElement(int windowId, DisplayServer.AccessibilityRole role)
```

引擎名：`accessibility_create_element`

Creates a new, empty accessibility element resource.

**Note:** An accessibility element is created and freed automatically for each [`Node`](Node.md). In general, this function should not be called manually.

### AccessibilityCreateSubElement

```csharp
public static Rid AccessibilityCreateSubElement(Rid parentRid, DisplayServer.AccessibilityRole role, int insertPos = -1)
```

引擎名：`accessibility_create_sub_element`

Creates a new, empty accessibility sub-element resource. Sub-elements can be used to provide accessibility information for objects which are not [`Node`](Node.md)s, such as list items, table cells, or menu items. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `AccessibilityFreeElement()` method.

### AccessibilityCreateSubTextEditElements

```csharp
public static Rid AccessibilityCreateSubTextEditElements(Rid parentRid, Rid shapedText, float minHeight, int insertPos = -1, bool isLastLine = false)
```

引擎名：`accessibility_create_sub_text_edit_elements`

Creates a new, empty accessibility sub-element from the shaped text buffer. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `AccessibilityFreeElement()` method.

If `isLastLine` is `true`, no trailing newline is appended to the text content. Set to `true` for the last line in multi-line text fields and for single-line text fields.

### AccessibilityCreateSubTextEditElements

```csharp
public static Rid AccessibilityCreateSubTextEditElements(Rid parentRid, Rid shapedText, float minHeight, int insertPos)
```

引擎名：`accessibility_create_sub_text_edit_elements`

Creates a new, empty accessibility sub-element from the shaped text buffer. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `AccessibilityFreeElement()` method.

If `isLastLine` is `true`, no trailing newline is appended to the text content. Set to `true` for the last line in multi-line text fields and for single-line text fields.

### AccessibilityElementGetMeta

```csharp
public static Variant AccessibilityElementGetMeta(Rid id)
```

引擎名：`accessibility_element_get_meta`

Returns the metadata of the accessibility element `id`.

### AccessibilityElementSetMeta

```csharp
public static void AccessibilityElementSetMeta(Rid id, Variant meta)
```

引擎名：`accessibility_element_set_meta`

Sets the metadata of the accessibility element `id` to `meta`.

### AccessibilityFreeElement

```csharp
public static void AccessibilityFreeElement(Rid id)
```

引擎名：`accessibility_free_element`

Frees the accessibility element `id` created by `AccessibilityCreateElement()`, `AccessibilityCreateSubElement()`, or `AccessibilityCreateSubTextEditElements()`.

### AccessibilityGetWindowRoot

```csharp
public static Rid AccessibilityGetWindowRoot(int windowId)
```

引擎名：`accessibility_get_window_root`

Returns the main accessibility element of the OS native window.

### AccessibilityHasElement

```csharp
public static bool AccessibilityHasElement(Rid id)
```

引擎名：`accessibility_has_element`

Returns `true` if `id` is a valid accessibility element.

### AccessibilityScreenReaderActive

```csharp
public static int AccessibilityScreenReaderActive()
```

引擎名：`accessibility_screen_reader_active`

Returns `1` if a screen reader, Braille display or other assistive app is active, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Accessibility debugging tools, such as Accessibility Insights for Windows, Accessibility Inspector (macOS), or AT-SPI Browser (Linux/BSD), do not count as assistive apps and will not affect this value. To test your project with these tools, set `ProjectSettings.accessibility/general/accessibility_support` to `1`.

### AccessibilitySetWindowFocused

```csharp
public static void AccessibilitySetWindowFocused(int windowId, bool focused)
```

引擎名：`accessibility_set_window_focused`

Sets the window focused state for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### AccessibilitySetWindowRect

```csharp
public static void AccessibilitySetWindowRect(int windowId, Rect2 rectOut, Rect2 rectIn)
```

引擎名：`accessibility_set_window_rect`

Sets window outer (with decorations) and inner (without decorations) bounds for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### AccessibilityShouldIncreaseContrast

```csharp
public static int AccessibilityShouldIncreaseContrast()
```

引擎名：`accessibility_should_increase_contrast`

Returns `1` if a high-contrast user interface theme should be used, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on Linux (X11/Wayland, GNOME), macOS, and Windows.

### AccessibilityShouldReduceAnimation

```csharp
public static int AccessibilityShouldReduceAnimation()
```

引擎名：`accessibility_should_reduce_animation`

Returns `1` if flashing, blinking, and other moving content that can cause seizures in users with photosensitive epilepsy should be disabled, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on macOS and Windows.

### AccessibilityShouldReduceTransparency

```csharp
public static int AccessibilityShouldReduceTransparency()
```

引擎名：`accessibility_should_reduce_transparency`

Returns `1` if background images, transparency, and other features that can reduce the contrast between the foreground and background should be disabled, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on macOS and Windows.

### AccessibilityUpdateAddAction

```csharp
public static void AccessibilityUpdateAddAction(Rid id, DisplayServer.AccessibilityAction action, Callable callable)
```

引擎名：`accessibility_update_add_action`

Adds a callback for the accessibility action (action which can be performed by using a special screen reader command or buttons on the Braille display), and marks this action as supported. The action callback receives one [`Variant`](Variant.md) argument, which value depends on action type.

### AccessibilityUpdateAddChild

```csharp
public static void AccessibilityUpdateAddChild(Rid id, Rid childId)
```

引擎名：`accessibility_update_add_child`

Adds a child accessibility element.

**Note:** [`Node`](Node.md) children and sub-elements are added to the child list automatically.

### AccessibilityUpdateAddCustomAction

```csharp
public static void AccessibilityUpdateAddCustomAction(Rid id, int actionId, string actionDescription)
```

引擎名：`accessibility_update_add_custom_action`

Adds support for a custom accessibility action. `actionId` is passed as an argument to the callback of [`DisplayServer.AccessibilityAction.Custom`](DisplayServer.md) action.

### AccessibilityUpdateAddRelatedControls

```csharp
public static void AccessibilityUpdateAddRelatedControls(Rid id, Rid relatedId)
```

引擎名：`accessibility_update_add_related_controls`

Adds an element that is controlled by this element.

### AccessibilityUpdateAddRelatedDescribedBy

```csharp
public static void AccessibilityUpdateAddRelatedDescribedBy(Rid id, Rid relatedId)
```

引擎名：`accessibility_update_add_related_described_by`

Adds an element that describes this element.

### AccessibilityUpdateAddRelatedDetails

```csharp
public static void AccessibilityUpdateAddRelatedDetails(Rid id, Rid relatedId)
```

引擎名：`accessibility_update_add_related_details`

Adds an element that details this element.

### AccessibilityUpdateAddRelatedFlowTo

```csharp
public static void AccessibilityUpdateAddRelatedFlowTo(Rid id, Rid relatedId)
```

引擎名：`accessibility_update_add_related_flow_to`

Adds an element that this element flow into.

### AccessibilityUpdateAddRelatedLabeledBy

```csharp
public static void AccessibilityUpdateAddRelatedLabeledBy(Rid id, Rid relatedId)
```

引擎名：`accessibility_update_add_related_labeled_by`

Adds an element that labels this element.

### AccessibilityUpdateAddRelatedRadioGroup

```csharp
public static void AccessibilityUpdateAddRelatedRadioGroup(Rid id, Rid relatedId)
```

引擎名：`accessibility_update_add_related_radio_group`

Adds an element that is part of the same radio group.

**Note:** This method should be called on each element of the group, using all other elements as `relatedId`.

### AccessibilityUpdateSetActiveDescendant

```csharp
public static void AccessibilityUpdateSetActiveDescendant(Rid id, Rid otherId)
```

引擎名：`accessibility_update_set_active_descendant`

Adds an element that is an active descendant of this element.

### AccessibilityUpdateSetBackgroundColor

```csharp
public static void AccessibilityUpdateSetBackgroundColor(Rid id, Color color)
```

引擎名：`accessibility_update_set_background_color`

Sets element background color.

### AccessibilityUpdateSetBounds

```csharp
public static void AccessibilityUpdateSetBounds(Rid id, Rect2 rect)
```

引擎名：`accessibility_update_set_bounds`

Sets element bounding box, relative to the node position.

### AccessibilityUpdateSetChecked

```csharp
public static void AccessibilityUpdateSetChecked(Rid id, bool checekd)
```

引擎名：`accessibility_update_set_checked`

Sets element checked state.

### AccessibilityUpdateSetClassname

```csharp
public static void AccessibilityUpdateSetClassname(Rid id, string classname)
```

引擎名：`accessibility_update_set_classname`

Sets element class name.

### AccessibilityUpdateSetColorValue

```csharp
public static void AccessibilityUpdateSetColorValue(Rid id, Color color)
```

引擎名：`accessibility_update_set_color_value`

Sets element color value.

### AccessibilityUpdateSetDescription

```csharp
public static void AccessibilityUpdateSetDescription(Rid id, string description)
```

引擎名：`accessibility_update_set_description`

Sets element accessibility description.

### AccessibilityUpdateSetErrorMessage

```csharp
public static void AccessibilityUpdateSetErrorMessage(Rid id, Rid otherId)
```

引擎名：`accessibility_update_set_error_message`

Sets an element which contains an error message for this element.

### AccessibilityUpdateSetExtraInfo

```csharp
public static void AccessibilityUpdateSetExtraInfo(Rid id, string name)
```

引擎名：`accessibility_update_set_extra_info`

Sets element accessibility extra information added to the element name.

### AccessibilityUpdateSetFlag

```csharp
public static void AccessibilityUpdateSetFlag(Rid id, DisplayServer.AccessibilityFlags flag, bool value)
```

引擎名：`accessibility_update_set_flag`

Sets element flag.

### AccessibilityUpdateSetFocus

```csharp
public static void AccessibilityUpdateSetFocus(Rid id)
```

引擎名：`accessibility_update_set_focus`

Sets currently focused element.

### AccessibilityUpdateSetForegroundColor

```csharp
public static void AccessibilityUpdateSetForegroundColor(Rid id, Color color)
```

引擎名：`accessibility_update_set_foreground_color`

Sets element foreground color.

### AccessibilityUpdateSetInPageLinkTarget

```csharp
public static void AccessibilityUpdateSetInPageLinkTarget(Rid id, Rid otherId)
```

引擎名：`accessibility_update_set_in_page_link_target`

Sets target element for the link.

### AccessibilityUpdateSetLanguage

```csharp
public static void AccessibilityUpdateSetLanguage(Rid id, string language)
```

引擎名：`accessibility_update_set_language`

Sets element text language.

### AccessibilityUpdateSetListItemCount

```csharp
public static void AccessibilityUpdateSetListItemCount(Rid id, int size)
```

引擎名：`accessibility_update_set_list_item_count`

Sets number of items in the list.

### AccessibilityUpdateSetListItemExpanded

```csharp
public static void AccessibilityUpdateSetListItemExpanded(Rid id, bool expanded)
```

引擎名：`accessibility_update_set_list_item_expanded`

Sets list/tree item expanded status.

### AccessibilityUpdateSetListItemIndex

```csharp
public static void AccessibilityUpdateSetListItemIndex(Rid id, int index)
```

引擎名：`accessibility_update_set_list_item_index`

Sets the position of the element in the list.

### AccessibilityUpdateSetListItemLevel

```csharp
public static void AccessibilityUpdateSetListItemLevel(Rid id, int level)
```

引擎名：`accessibility_update_set_list_item_level`

Sets the hierarchical level of the element in the list.

### AccessibilityUpdateSetListItemSelected

```csharp
public static void AccessibilityUpdateSetListItemSelected(Rid id, bool selected)
```

引擎名：`accessibility_update_set_list_item_selected`

Sets list/tree item selected status.

### AccessibilityUpdateSetListOrientation

```csharp
public static void AccessibilityUpdateSetListOrientation(Rid id, bool vertical)
```

引擎名：`accessibility_update_set_list_orientation`

Sets the orientation of the list elements.

### AccessibilityUpdateSetLive

```csharp
public static void AccessibilityUpdateSetLive(Rid id, DisplayServer.AccessibilityLiveMode live)
```

引擎名：`accessibility_update_set_live`

Sets the priority of the live region updates.

### AccessibilityUpdateSetMemberOf

```csharp
public static void AccessibilityUpdateSetMemberOf(Rid id, Rid groupId)
```

引擎名：`accessibility_update_set_member_of`

Sets the element to be a member of the group.

### AccessibilityUpdateSetName

```csharp
public static void AccessibilityUpdateSetName(Rid id, string name)
```

引擎名：`accessibility_update_set_name`

Sets element accessibility name.

### AccessibilityUpdateSetNextOnLine

```csharp
public static void AccessibilityUpdateSetNextOnLine(Rid id, Rid otherId)
```

引擎名：`accessibility_update_set_next_on_line`

Sets next element on the line.

### AccessibilityUpdateSetNumJump

```csharp
public static void AccessibilityUpdateSetNumJump(Rid id, double jump)
```

引擎名：`accessibility_update_set_num_jump`

Sets numeric value jump.

### AccessibilityUpdateSetNumRange

```csharp
public static void AccessibilityUpdateSetNumRange(Rid id, double min, double max)
```

引擎名：`accessibility_update_set_num_range`

Sets numeric value range.

### AccessibilityUpdateSetNumStep

```csharp
public static void AccessibilityUpdateSetNumStep(Rid id, double step)
```

引擎名：`accessibility_update_set_num_step`

Sets numeric value step.

### AccessibilityUpdateSetNumValue

```csharp
public static void AccessibilityUpdateSetNumValue(Rid id, double position)
```

引擎名：`accessibility_update_set_num_value`

Sets numeric value.

### AccessibilityUpdateSetPlaceholder

```csharp
public static void AccessibilityUpdateSetPlaceholder(Rid id, string placeholder)
```

引擎名：`accessibility_update_set_placeholder`

Sets placeholder text.

### AccessibilityUpdateSetPopupType

```csharp
public static void AccessibilityUpdateSetPopupType(Rid id, DisplayServer.AccessibilityPopupType popup)
```

引擎名：`accessibility_update_set_popup_type`

Sets popup type for popup buttons.

### AccessibilityUpdateSetPreviousOnLine

```csharp
public static void AccessibilityUpdateSetPreviousOnLine(Rid id, Rid otherId)
```

引擎名：`accessibility_update_set_previous_on_line`

Sets previous element on the line.

### AccessibilityUpdateSetRole

```csharp
public static void AccessibilityUpdateSetRole(Rid id, DisplayServer.AccessibilityRole role)
```

引擎名：`accessibility_update_set_role`

Sets element accessibility role.

### AccessibilityUpdateSetRoleDescription

```csharp
public static void AccessibilityUpdateSetRoleDescription(Rid id, string description)
```

引擎名：`accessibility_update_set_role_description`

Sets element accessibility role description text.

### AccessibilityUpdateSetScrollX

```csharp
public static void AccessibilityUpdateSetScrollX(Rid id, double position)
```

引擎名：`accessibility_update_set_scroll_x`

Sets scroll bar x position.

### AccessibilityUpdateSetScrollXRange

```csharp
public static void AccessibilityUpdateSetScrollXRange(Rid id, double min, double max)
```

引擎名：`accessibility_update_set_scroll_x_range`

Sets scroll bar x range.

### AccessibilityUpdateSetScrollY

```csharp
public static void AccessibilityUpdateSetScrollY(Rid id, double position)
```

引擎名：`accessibility_update_set_scroll_y`

Sets scroll bar y position.

### AccessibilityUpdateSetScrollYRange

```csharp
public static void AccessibilityUpdateSetScrollYRange(Rid id, double min, double max)
```

引擎名：`accessibility_update_set_scroll_y_range`

Sets scroll bar y range.

### AccessibilityUpdateSetShortcut

```csharp
public static void AccessibilityUpdateSetShortcut(Rid id, string shortcut)
```

引擎名：`accessibility_update_set_shortcut`

Sets the list of keyboard shortcuts used by element.

### AccessibilityUpdateSetStateDescription

```csharp
public static void AccessibilityUpdateSetStateDescription(Rid id, string description)
```

引擎名：`accessibility_update_set_state_description`

Sets human-readable description of the current checked state.

### AccessibilityUpdateSetTableCellPosition

```csharp
public static void AccessibilityUpdateSetTableCellPosition(Rid id, int rowIndex, int columnIndex)
```

引擎名：`accessibility_update_set_table_cell_position`

Sets cell position in the table.

### AccessibilityUpdateSetTableCellSpan

```csharp
public static void AccessibilityUpdateSetTableCellSpan(Rid id, int rowSpan, int columnSpan)
```

引擎名：`accessibility_update_set_table_cell_span`

Sets cell row/column span.

### AccessibilityUpdateSetTableColumnCount

```csharp
public static void AccessibilityUpdateSetTableColumnCount(Rid id, int count)
```

引擎名：`accessibility_update_set_table_column_count`

Sets number of columns in the table.

### AccessibilityUpdateSetTableColumnIndex

```csharp
public static void AccessibilityUpdateSetTableColumnIndex(Rid id, int index)
```

引擎名：`accessibility_update_set_table_column_index`

Sets position of the column.

### AccessibilityUpdateSetTableRowCount

```csharp
public static void AccessibilityUpdateSetTableRowCount(Rid id, int count)
```

引擎名：`accessibility_update_set_table_row_count`

Sets number of rows in the table.

### AccessibilityUpdateSetTableRowIndex

```csharp
public static void AccessibilityUpdateSetTableRowIndex(Rid id, int index)
```

引擎名：`accessibility_update_set_table_row_index`

Sets position of the row in the table.

### AccessibilityUpdateSetTextAlign

```csharp
public static void AccessibilityUpdateSetTextAlign(Rid id, HorizontalAlignment align)
```

引擎名：`accessibility_update_set_text_align`

Sets element text alignment.

### AccessibilityUpdateSetTextDecorations

```csharp
public static void AccessibilityUpdateSetTextDecorations(Rid id, bool underline, bool strikethrough, bool overline)
```

引擎名：`accessibility_update_set_text_decorations`

Sets text underline/overline/strikethrough.

### AccessibilityUpdateSetTextOrientation

```csharp
public static void AccessibilityUpdateSetTextOrientation(Rid id, bool vertical)
```

引擎名：`accessibility_update_set_text_orientation`

Sets text orientation.

### AccessibilityUpdateSetTextSelection

```csharp
public static void AccessibilityUpdateSetTextSelection(Rid id, Rid textStartId, int startChar, Rid textEndId, int endChar)
```

引擎名：`accessibility_update_set_text_selection`

Sets text selection to the text field. `textStartId` and `textEndId` should be elements created by `AccessibilityCreateSubTextEditElements()`. Character offsets are relative to the corresponding element.

### AccessibilityUpdateSetTooltip

```csharp
public static void AccessibilityUpdateSetTooltip(Rid id, string tooltip)
```

引擎名：`accessibility_update_set_tooltip`

Sets tooltip text.

### AccessibilityUpdateSetTransform

```csharp
public static void AccessibilityUpdateSetTransform(Rid id, Transform2D transform)
```

引擎名：`accessibility_update_set_transform`

Sets element 2D transform.

### AccessibilityUpdateSetUrl

```csharp
public static void AccessibilityUpdateSetUrl(Rid id, string url)
```

引擎名：`accessibility_update_set_url`

Sets link URL.

### AccessibilityUpdateSetValue

```csharp
public static void AccessibilityUpdateSetValue(Rid id, string value)
```

引擎名：`accessibility_update_set_value`

Sets element text value.

### Beep

```csharp
public static void Beep()
```

引擎名：`beep`

Plays the beep sound from the operative system, if possible. Because it comes from the OS, the beep sound will be audible even if the application is muted. It may also be disabled for the entire OS by the user.

**Note:** This method is implemented on macOS, Linux (X11/Wayland), and Windows.

### ClipboardGet

```csharp
public static string ClipboardGet()
```

引擎名：`clipboard_get`

Returns the user's clipboard as a string if possible.

### ClipboardGetImage

```csharp
public static Image ClipboardGetImage()
```

引擎名：`clipboard_get_image`

Returns the user's clipboard as an image if possible.

**Note:** This method uses the copied pixel data, e.g. from an image editing software or a web browser, not an image file copied from file explorer.

### ClipboardGetPrimary

```csharp
public static string ClipboardGetPrimary()
```

引擎名：`clipboard_get_primary`

Returns the user's [primary](https://unix.stackexchange.com/questions/139191/whats-the-difference-between-primary-selection-and-clipboard-buffer) clipboard as a string if possible. This is the clipboard that is set when the user selects text in any application, rather than when pressing Ctrl + C. The clipboard data can then be pasted by clicking the middle mouse button in any application that supports the primary clipboard mechanism.

**Note:** This method is only implemented on Linux (X11/Wayland).

### ClipboardHas

```csharp
public static bool ClipboardHas()
```

引擎名：`clipboard_has`

Returns `true` if there is a text content on the user's clipboard.

### ClipboardHasImage

```csharp
public static bool ClipboardHasImage()
```

引擎名：`clipboard_has_image`

Returns `true` if there is an image content on the user's clipboard.

### ClipboardSet

```csharp
public static void ClipboardSet(string clipboard)
```

引擎名：`clipboard_set`

Sets the user's clipboard content to the given string.

### ClipboardSetPrimary

```csharp
public static void ClipboardSetPrimary(string clipboardPrimary)
```

引擎名：`clipboard_set_primary`

Sets the user's [primary](https://unix.stackexchange.com/questions/139191/whats-the-difference-between-primary-selection-and-clipboard-buffer) clipboard content to the given string. This is the clipboard that is set when the user selects text in any application, rather than when pressing Ctrl + C. The clipboard data can then be pasted by clicking the middle mouse button in any application that supports the primary clipboard mechanism.

**Note:** This method is only implemented on Linux (X11/Wayland).

### ColorPicker

```csharp
public static bool ColorPicker(Callable callback)
```

引擎名：`color_picker`

Displays OS native color picker.

Callbacks have the following arguments: `status: bool, color: Color`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeColorPicker`](DisplayServer.md) feature.

**Note:** This method is only implemented on Linux (X11/Wayland).

### CreateStatusIndicator

```csharp
public static int CreateStatusIndicator(Texture2D icon, string tooltip, Callable callback)
```

引擎名：`create_status_indicator`

Creates a new application status indicator with the specified icon, tooltip, and activation callback.

`callback` should take two arguments: the pressed mouse button (one of the [`MouseButton`](MouseButton.md) constants) and the click position in screen coordinates (a [`Vector2I`](Vector2I.md)).

### CursorGetShape

```csharp
public static DisplayServer.CursorShape CursorGetShape()
```

引擎名：`cursor_get_shape`

Returns the default mouse cursor shape set by `CursorSetShape()`.

### CursorSetCustomImage

```csharp
public static void CursorSetCustomImage(Resource cursor, DisplayServer.CursorShape shape = DisplayServer.CursorShape.Arrow, Vector2? hotspot = null)
```

引擎名：`cursor_set_custom_image`

Sets a custom mouse cursor image for the given `shape`. This means the user's operating system and mouse cursor theme will no longer influence the mouse cursor's appearance.

`cursor` can be either a [`Texture2D`](Texture2D.md) or an [`Image`](Image.md), and it should not be larger than 256×256 to display correctly. Optionally, `hotspot` can be set to offset the image's position relative to the click point. By default, `hotspot` is set to the top-left corner of the image. See also `CursorSetShape()`.

**Note:** On Web, calling this method every frame can cause the cursor to flicker.

参数：
- `hotspot`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### CursorSetShape

```csharp
public static void CursorSetShape(DisplayServer.CursorShape shape)
```

引擎名：`cursor_set_shape`

Sets the default mouse cursor shape. The cursor's appearance will vary depending on the user's operating system and mouse cursor theme. See also `CursorGetShape()` and `CursorSetCustomImage()`.

### DeleteStatusIndicator

```csharp
public static void DeleteStatusIndicator(int id)
```

引擎名：`delete_status_indicator`

Removes the application status indicator.

### DialogInputText

```csharp
public static Error DialogInputText(string title, string description, string existingText, Callable callback)
```

引擎名：`dialog_input_text`

Shows a text input dialog which uses the operating system's native look-and-feel. `callback` should accept a single `System.String` parameter which contains the text field's contents.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogInput`](DisplayServer.md) feature. Supported platforms include macOS, Windows, and Android.

### DialogShow

```csharp
public static Error DialogShow(string title, string description, ReadOnlySpan<string> buttons, Callable callback)
```

引擎名：`dialog_show`

Shows a text dialog which uses the operating system's native look-and-feel. `callback` should accept a single `System.Int32` parameter which corresponds to the index of the pressed button.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialog`](DisplayServer.md) feature. Supported platforms include macOS, Windows, and Android.

### DialogShow

```csharp
public static Error DialogShow(string title, string description, string[] buttons, Callable callback)
```

引擎名：`dialog_show`

Shows a text dialog which uses the operating system's native look-and-feel. `callback` should accept a single `System.Int32` parameter which corresponds to the index of the pressed button.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialog`](DisplayServer.md) feature. Supported platforms include macOS, Windows, and Android.

### EnableForStealingFocus

```csharp
public static void EnableForStealingFocus(long processId)
```

引擎名：`enable_for_stealing_focus`

Allows the `processId` PID to steal focus from this window. In other words, this disables the operating system's focus stealing protection for the specified PID.

**Note:** This method is implemented only on Windows.

### FileDialogShow

```csharp
public static Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Callable callback)
```

引擎名：`file_dialog_show`

Displays OS native dialog for selecting files or directories in the file system.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int`. **On Android,** the third callback argument (`selected_filter_index`) is always `0`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFile`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, macOS, and Android.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Android and Linux, `showHidden` is ignored.

**Note:** On Android and macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

**Note:** On Android, this method uses the Android Storage Access Framework (SAF).

The file picker returns a URI instead of a filesystem path. This URI can be passed directly to [`FileAccess`](FileAccess.md) to perform read/write operations.

When using [`DisplayServer.FileDialogMode.OpenDir`](DisplayServer.md), it returns a tree URI that grants full access to the selected directory. File operations inside this directory can be performed by passing a path on the form `treeUri#relative/path/to/file` to [`FileAccess`](FileAccess.md).

To avoid opening the file picker again after each app restart, you can take persistable URI permission as follows:

The persistable URI permission remains valid across app restarts as long as the directory is not moved, renamed, or deleted.

### FileDialogShow

```csharp
public static Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Callable callback, int parentWindowId)
```

引擎名：`file_dialog_show`

Displays OS native dialog for selecting files or directories in the file system.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int`. **On Android,** the third callback argument (`selected_filter_index`) is always `0`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFile`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, macOS, and Android.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Android and Linux, `showHidden` is ignored.

**Note:** On Android and macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

**Note:** On Android, this method uses the Android Storage Access Framework (SAF).

The file picker returns a URI instead of a filesystem path. This URI can be passed directly to [`FileAccess`](FileAccess.md) to perform read/write operations.

When using [`DisplayServer.FileDialogMode.OpenDir`](DisplayServer.md), it returns a tree URI that grants full access to the selected directory. File operations inside this directory can be performed by passing a path on the form `treeUri#relative/path/to/file` to [`FileAccess`](FileAccess.md).

To avoid opening the file picker again after each app restart, you can take persistable URI permission as follows:

The persistable URI permission remains valid across app restarts as long as the directory is not moved, renamed, or deleted.

### FileDialogShow

```csharp
public static Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Callable callback)
```

引擎名：`file_dialog_show`

Displays OS native dialog for selecting files or directories in the file system.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int`. **On Android,** the third callback argument (`selected_filter_index`) is always `0`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFile`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, macOS, and Android.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Android and Linux, `showHidden` is ignored.

**Note:** On Android and macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

**Note:** On Android, this method uses the Android Storage Access Framework (SAF).

The file picker returns a URI instead of a filesystem path. This URI can be passed directly to [`FileAccess`](FileAccess.md) to perform read/write operations.

When using [`DisplayServer.FileDialogMode.OpenDir`](DisplayServer.md), it returns a tree URI that grants full access to the selected directory. File operations inside this directory can be performed by passing a path on the form `treeUri#relative/path/to/file` to [`FileAccess`](FileAccess.md).

To avoid opening the file picker again after each app restart, you can take persistable URI permission as follows:

The persistable URI permission remains valid across app restarts as long as the directory is not moved, renamed, or deleted.

### FileDialogShow

```csharp
public static Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Callable callback, int parentWindowId = 0)
```

引擎名：`file_dialog_show`

Displays OS native dialog for selecting files or directories in the file system.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int`. **On Android,** the third callback argument (`selected_filter_index`) is always `0`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFile`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, macOS, and Android.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Android and Linux, `showHidden` is ignored.

**Note:** On Android and macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

**Note:** On Android, this method uses the Android Storage Access Framework (SAF).

The file picker returns a URI instead of a filesystem path. This URI can be passed directly to [`FileAccess`](FileAccess.md) to perform read/write operations.

When using [`DisplayServer.FileDialogMode.OpenDir`](DisplayServer.md), it returns a tree URI that grants full access to the selected directory. File operations inside this directory can be performed by passing a path on the form `treeUri#relative/path/to/file` to [`FileAccess`](FileAccess.md).

To avoid opening the file picker again after each app restart, you can take persistable URI permission as follows:

The persistable URI permission remains valid across app restarts as long as the directory is not moved, renamed, or deleted.

### FileDialogWithOptionsShow

```csharp
public static Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Array<Dictionary> options, Callable callback)
```

引擎名：`file_dialog_with_options_show`

Displays OS native dialog for selecting files or directories in the file system with additional user selectable options.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

`options` is array of [`Collections.Dictionary`](Collections.Dictionary.md)s with the following keys:

- `"name"` - option's name `System.String`.

- `"values"` - `System.String`[] of values. If empty, boolean option (check box) is used.

- `"default"` - default selected option index (`System.Int32`) or default boolean value (`System.Boolean`).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int, selected_option: Dictionary`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFileExtra`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, and macOS.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Linux (X11), `showHidden` is ignored.

**Note:** On macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

### FileDialogWithOptionsShow

```csharp
public static Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Array<Dictionary> options, Callable callback, int parentWindowId)
```

引擎名：`file_dialog_with_options_show`

Displays OS native dialog for selecting files or directories in the file system with additional user selectable options.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

`options` is array of [`Collections.Dictionary`](Collections.Dictionary.md)s with the following keys:

- `"name"` - option's name `System.String`.

- `"values"` - `System.String`[] of values. If empty, boolean option (check box) is used.

- `"default"` - default selected option index (`System.Int32`) or default boolean value (`System.Boolean`).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int, selected_option: Dictionary`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFileExtra`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, and macOS.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Linux (X11), `showHidden` is ignored.

**Note:** On macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

### FileDialogWithOptionsShow

```csharp
public static Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Array<Dictionary> options, Callable callback)
```

引擎名：`file_dialog_with_options_show`

Displays OS native dialog for selecting files or directories in the file system with additional user selectable options.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

`options` is array of [`Collections.Dictionary`](Collections.Dictionary.md)s with the following keys:

- `"name"` - option's name `System.String`.

- `"values"` - `System.String`[] of values. If empty, boolean option (check box) is used.

- `"default"` - default selected option index (`System.Int32`) or default boolean value (`System.Boolean`).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int, selected_option: Dictionary`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFileExtra`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, and macOS.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Linux (X11), `showHidden` is ignored.

**Note:** On macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

### FileDialogWithOptionsShow

```csharp
public static Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Array<Dictionary> options, Callable callback, int parentWindowId = 0)
```

引擎名：`file_dialog_with_options_show`

Displays OS native dialog for selecting files or directories in the file system with additional user selectable options.

Each filter string in the `filters` array should be formatted like this: `*.png,*.jpg,*.jpeg;Image Files;image/png,image/jpeg`. The description text of the filter is optional and can be omitted. It is recommended to set both file extension and MIME type. See also [`FileDialog.Filters`](FileDialog.md).

`options` is array of [`Collections.Dictionary`](Collections.Dictionary.md)s with the following keys:

- `"name"` - option's name `System.String`.

- `"values"` - `System.String`[] of values. If empty, boolean option (check box) is used.

- `"default"` - default selected option index (`System.Int32`) or default boolean value (`System.Boolean`).

Callbacks have the following arguments: `status: bool, selected_paths: PackedStringArray, selected_filter_index: int, selected_option: Dictionary`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogFileExtra`](DisplayServer.md) feature. Supported platforms include Linux (X11/Wayland), Windows, and macOS.

**Note:** `currentDirectory` might be ignored.

**Note:** Embedded file dialogs and Windows file dialogs support only file extensions, while Android, Linux, and macOS file dialogs also support MIME types.

**Note:** On Linux (X11), `showHidden` is ignored.

**Note:** On macOS, native file dialogs have no title.

**Note:** On macOS, sandboxed apps will save security-scoped bookmarks to retain access to the opened folders across multiple sessions. Use [`OS.GetGrantedPermissions`](OS.md) to get a list of saved bookmarks.

### ForceProcessAndDropEvents

```csharp
public static void ForceProcessAndDropEvents()
```

引擎名：`force_process_and_drop_events`

Forces window manager processing while ignoring all [`InputEvent`](InputEvent.md)s. See also `ProcessEvents()`.

**Note:** This method is implemented on Windows and macOS.

### GetAccentColor

```csharp
public static Color GetAccentColor()
```

引擎名：`get_accent_color`

Returns OS theme accent color. Returns `Color(0, 0, 0, 0)`, if accent color is unknown.

**Note:** This method is implemented on macOS, Windows, Android, and Linux (X11/Wayland).

### GetBaseColor

```csharp
public static Color GetBaseColor()
```

引擎名：`get_base_color`

Returns the OS theme base color (default control background). Returns `Color(0, 0, 0, 0)` if the base color is unknown.

**Note:** This method is implemented on macOS, Windows, and Android.

### GetDisplayCutouts

```csharp
public static Array<Rect2> GetDisplayCutouts()
```

引擎名：`get_display_cutouts`

Returns an [`Collections.Array`](Collections.Array.md) of [`Rect2`](Rect2.md), each of which is the bounding rectangle for a display cutout or notch. These are non-functional areas on edge-to-edge screens used by cameras and sensors. Returns an empty array if the device does not have cutouts. See also `GetDisplaySafeArea()`.

**Note:** Currently only implemented on Android. Other platforms will return an empty array even if they do have display cutouts or notches.

### GetDisplaySafeArea

```csharp
public static Rect2I GetDisplaySafeArea()
```

引擎名：`get_display_safe_area`

Returns the unobscured area of the display where interactive controls should be rendered. See also `GetDisplayCutouts()`.

**Note:** Currently only implemented on Android and iOS. On other platforms, `screen_get_usable_rect(SCREEN_OF_MAIN_WINDOW)` will be returned as a fallback. See also `ScreenGetUsableRect()`.

### GetKeyboardFocusScreen

```csharp
public static int GetKeyboardFocusScreen()
```

引擎名：`get_keyboard_focus_screen`

Returns the index of the screen containing the window with the keyboard focus, or the primary screen if there's no focused window.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns the primary screen.

### GetName

```csharp
public static string GetName()
```

引擎名：`get_name`

Returns the name of the `DisplayServer` currently in use. Most operating systems only have a single `DisplayServer`, but Linux has access to more than one `DisplayServer` (currently X11 and Wayland).

The names of built-in display servers are `Windows`, `macOS`, `X11` (Linux), `Wayland` (Linux), `Android`, `iOS`, `web` (HTML5), and `headless` (when started with the `--headless` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html)).

### GetPrimaryScreen

```csharp
public static int GetPrimaryScreen()
```

引擎名：`get_primary_screen`

Returns the index of the primary screen.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns `0`.

### GetScreenCount

```csharp
public static int GetScreenCount()
```

引擎名：`get_screen_count`

Returns the number of displays available.

**Note:** This method is implemented on Linux (X11 and Wayland), macOS, and Windows. On other platforms, this method always returns `1`.

### GetScreenFromRect

```csharp
public static int GetScreenFromRect(Rect2 rect)
```

引擎名：`get_screen_from_rect`

Returns the index of the screen that overlaps the most with the given rectangle. Returns `InvalidScreen` if the rectangle doesn't overlap with any screen or has no area.

### GetSwapCancelOk

```csharp
public static bool GetSwapCancelOk()
```

引擎名：`get_swap_cancel_ok`

Returns `true` if positions of **OK** and **Cancel** buttons are swapped in dialogs. This is enabled by default on Windows to follow interface conventions, and be toggled by changing `ProjectSettings.gui/common/swap_cancel_ok`.

**Note:** This doesn't affect native dialogs such as the ones spawned by `DialogShow()`.

### GetWindowAtScreenPosition

```csharp
public static int GetWindowAtScreenPosition(Vector2I position)
```

引擎名：`get_window_at_screen_position`

Returns the ID of the window at the specified screen `position` (in pixels). On multi-monitor setups, the screen position is relative to the virtual desktop area. On multi-monitor setups with different screen resolutions or orientations, the origin may be located outside any display like this:

```csharp
* (0, 0)        +-------+
|       |
+-------------+ |       |
|             | |       |
|             | |       |
+-------------+ +-------+

```

### GetWindowList

```csharp
public static int[] GetWindowList()
```

引擎名：`get_window_list`

Returns the list of Godot window IDs belonging to this process.

**Note:** Native dialogs are not included in this list.

### GlobalMenuAddCheckItem

```csharp
public static int GlobalMenuAddCheckItem(string menuRoot, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_check_item`

Adds a new checkable item with text `label` to the global menu with ID `menuRoot`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddIconCheckItem

```csharp
public static int GlobalMenuAddIconCheckItem(string menuRoot, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_icon_check_item`

Adds a new checkable item with text `label` and icon `icon` to the global menu with ID `menuRoot`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddIconItem

```csharp
public static int GlobalMenuAddIconItem(string menuRoot, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_icon_item`

Adds a new item with text `label` and icon `icon` to the global menu with ID `menuRoot`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddIconRadioCheckItem

```csharp
public static int GlobalMenuAddIconRadioCheckItem(string menuRoot, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_icon_radio_check_item`

Adds a new radio-checkable item with text `label` and icon `icon` to the global menu with ID `menuRoot`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** Radio-checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `GlobalMenuSetItemChecked()` for more info on how to control it.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddItem

```csharp
public static int GlobalMenuAddItem(string menuRoot, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_item`

Adds a new item with text `label` to the global menu with ID `menuRoot`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddMultistateItem

```csharp
public static int GlobalMenuAddMultistateItem(string menuRoot, string label, int maxStates, int defaultState, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_multistate_item`

Adds a new item with text `label` to the global menu with ID `menuRoot`.

Contrarily to normal binary items, multistate items can have more than two states, as defined by `maxStates`. Each press or activate of the item will increase the state by one. The default value is defined by `defaultState`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** By default, there's no indication of the current item state, it should be changed manually.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddRadioCheckItem

```csharp
public static int GlobalMenuAddRadioCheckItem(string menuRoot, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

引擎名：`global_menu_add_radio_check_item`

Adds a new radio-checkable item with text `label` to the global menu with ID `menuRoot`.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

An `accelerator` can optionally be defined, which is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The `accelerator` is generally a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** Radio-checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See `GlobalMenuSetItemChecked()` for more info on how to control it.

**Note:** The `callback` and `keyCallback` Callables need to accept exactly one Variant parameter, the parameter passed to the Callables will be the value passed to `tag`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddSeparator

```csharp
public static int GlobalMenuAddSeparator(string menuRoot, int index = -1)
```

引擎名：`global_menu_add_separator`

Adds a separator between items to the global menu with ID `menuRoot`. Separators also occupy an index.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuAddSubmenuItem

```csharp
public static int GlobalMenuAddSubmenuItem(string menuRoot, string label, string submenu, int index = -1)
```

引擎名：`global_menu_add_submenu_item`

Adds an item that will act as a submenu of the global menu `menuRoot`. The `submenu` argument is the ID of the global menu root that will be shown when the item is clicked.

Returns index of the inserted item, it's not guaranteed to be the same as `index` value.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuClear

```csharp
public static void GlobalMenuClear(string menuRoot)
```

引擎名：`global_menu_clear`

Removes all items from the global menu with ID `menuRoot`.

**Note:** This method is implemented only on macOS.

**Supported system menu IDs:**

```csharp
"_main" - Main menu (macOS).
"_dock" - Dock popup menu (macOS).
"_apple" - Apple menu (macOS, custom items added before "Services").
"_window" - Window menu (macOS, custom items added after "Bring All to Front").
"_help" - Help menu (macOS).

```

### GlobalMenuGetItemAccelerator

```csharp
public static Key GlobalMenuGetItemAccelerator(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_accelerator`

Returns the accelerator of the item at index `idx`. Accelerators are special combinations of keys that activate the item, no matter which control is focused.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemCallback

```csharp
public static Callable GlobalMenuGetItemCallback(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_callback`

Returns the callback of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemCount

```csharp
public static int GlobalMenuGetItemCount(string menuRoot)
```

引擎名：`global_menu_get_item_count`

Returns number of items in the global menu with ID `menuRoot`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIcon

```csharp
public static Texture2D GlobalMenuGetItemIcon(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_icon`

Returns the icon of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIndentationLevel

```csharp
public static int GlobalMenuGetItemIndentationLevel(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_indentation_level`

Returns the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIndexFromTag

```csharp
public static int GlobalMenuGetItemIndexFromTag(string menuRoot, Variant tag)
```

引擎名：`global_menu_get_item_index_from_tag`

Returns the index of the item with the specified `tag`. Indices are automatically assigned to each item by the engine, and cannot be set manually.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIndexFromText

```csharp
public static int GlobalMenuGetItemIndexFromText(string menuRoot, string text)
```

引擎名：`global_menu_get_item_index_from_text`

Returns the index of the item with the specified `text`. Indices are automatically assigned to each item by the engine, and cannot be set manually.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemKeyCallback

```csharp
public static Callable GlobalMenuGetItemKeyCallback(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_key_callback`

Returns the callback of the item accelerator at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemMaxStates

```csharp
public static int GlobalMenuGetItemMaxStates(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_max_states`

Returns number of states of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemState

```csharp
public static int GlobalMenuGetItemState(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_state`

Returns the state of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemSubmenu

```csharp
public static string GlobalMenuGetItemSubmenu(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_submenu`

Returns the submenu ID of the item at index `idx`. See `GlobalMenuAddSubmenuItem()` for more info on how to add a submenu.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemTag

```csharp
public static Variant GlobalMenuGetItemTag(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_tag`

Returns the metadata of the specified item, which might be of any type. You can set it with `GlobalMenuSetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemText

```csharp
public static string GlobalMenuGetItemText(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_text`

Returns the text of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemTooltip

```csharp
public static string GlobalMenuGetItemTooltip(string menuRoot, int idx)
```

引擎名：`global_menu_get_item_tooltip`

Returns the tooltip associated with the specified index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetSystemMenuRoots

```csharp
public static Dictionary GlobalMenuGetSystemMenuRoots()
```

引擎名：`global_menu_get_system_menu_roots`

Returns Dictionary of supported system menu IDs and names.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemCheckable

```csharp
public static bool GlobalMenuIsItemCheckable(string menuRoot, int idx)
```

引擎名：`global_menu_is_item_checkable`

Returns `true` if the item at index `idx` is checkable in some way, i.e. if it has a checkbox or radio button.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemChecked

```csharp
public static bool GlobalMenuIsItemChecked(string menuRoot, int idx)
```

引擎名：`global_menu_is_item_checked`

Returns `true` if the item at index `idx` is checked.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemDisabled

```csharp
public static bool GlobalMenuIsItemDisabled(string menuRoot, int idx)
```

引擎名：`global_menu_is_item_disabled`

Returns `true` if the item at index `idx` is disabled. When it is disabled it can't be selected, or its action invoked.

See `GlobalMenuSetItemDisabled()` for more info on how to disable an item.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemHidden

```csharp
public static bool GlobalMenuIsItemHidden(string menuRoot, int idx)
```

引擎名：`global_menu_is_item_hidden`

Returns `true` if the item at index `idx` is hidden.

See `GlobalMenuSetItemHidden()` for more info on how to hide an item.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemRadioCheckable

```csharp
public static bool GlobalMenuIsItemRadioCheckable(string menuRoot, int idx)
```

引擎名：`global_menu_is_item_radio_checkable`

Returns `true` if the item at index `idx` has radio button-style checkability.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented only on macOS.

### GlobalMenuRemoveItem

```csharp
public static void GlobalMenuRemoveItem(string menuRoot, int idx)
```

引擎名：`global_menu_remove_item`

Removes the item at index `idx` from the global menu `menuRoot`.

**Note:** The indices of items after the removed item will be shifted by one.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemAccelerator

```csharp
public static void GlobalMenuSetItemAccelerator(string menuRoot, int idx, Key keycode)
```

引擎名：`global_menu_set_item_accelerator`

Sets the accelerator of the item at index `idx`. `keycode` can be a single [`Key`](Key.md), or a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemCallback

```csharp
public static void GlobalMenuSetItemCallback(string menuRoot, int idx, Callable callback)
```

引擎名：`global_menu_set_item_callback`

Sets the callback of the item at index `idx`. Callback is emitted when an item is pressed.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemCheckable

```csharp
public static void GlobalMenuSetItemCheckable(string menuRoot, int idx, bool checkable)
```

引擎名：`global_menu_set_item_checkable`

Sets whether the item at index `idx` has a checkbox. If `false`, sets the type of the item to plain text.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemChecked

```csharp
public static void GlobalMenuSetItemChecked(string menuRoot, int idx, bool checked)
```

引擎名：`global_menu_set_item_checked`

Sets the checkstate status of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemDisabled

```csharp
public static void GlobalMenuSetItemDisabled(string menuRoot, int idx, bool disabled)
```

引擎名：`global_menu_set_item_disabled`

Enables/disables the item at index `idx`. When it is disabled, it can't be selected and its action can't be invoked.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemHidden

```csharp
public static void GlobalMenuSetItemHidden(string menuRoot, int idx, bool hidden)
```

引擎名：`global_menu_set_item_hidden`

Hides/shows the item at index `idx`. When it is hidden, an item does not appear in a menu and its action cannot be invoked.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemHoverCallbacks

```csharp
public static void GlobalMenuSetItemHoverCallbacks(string menuRoot, int idx, Callable callback)
```

引擎名：`global_menu_set_item_hover_callbacks`

Sets the callback of the item at index `idx`. The callback is emitted when an item is hovered.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemIcon

```csharp
public static void GlobalMenuSetItemIcon(string menuRoot, int idx, Texture2D icon)
```

引擎名：`global_menu_set_item_icon`

Replaces the [`Texture2D`](Texture2D.md) icon of the specified `idx`.

**Note:** This method is implemented only on macOS.

**Note:** This method is not supported by macOS "_dock" menu items.

### GlobalMenuSetItemIndentationLevel

```csharp
public static void GlobalMenuSetItemIndentationLevel(string menuRoot, int idx, int level)
```

引擎名：`global_menu_set_item_indentation_level`

Sets the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemKeyCallback

```csharp
public static void GlobalMenuSetItemKeyCallback(string menuRoot, int idx, Callable keyCallback)
```

引擎名：`global_menu_set_item_key_callback`

Sets the callback of the item at index `idx`. Callback is emitted when its accelerator is activated.

**Note:** The `keyCallback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemMaxStates

```csharp
public static void GlobalMenuSetItemMaxStates(string menuRoot, int idx, int maxStates)
```

引擎名：`global_menu_set_item_max_states`

Sets number of state of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemRadioCheckable

```csharp
public static void GlobalMenuSetItemRadioCheckable(string menuRoot, int idx, bool checkable)
```

引擎名：`global_menu_set_item_radio_checkable`

Sets the type of the item at the specified index `idx` to radio button. If `false`, sets the type of the item to plain text.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemState

```csharp
public static void GlobalMenuSetItemState(string menuRoot, int idx, int state)
```

引擎名：`global_menu_set_item_state`

Sets the state of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemSubmenu

```csharp
public static void GlobalMenuSetItemSubmenu(string menuRoot, int idx, string submenu)
```

引擎名：`global_menu_set_item_submenu`

Sets the submenu of the item at index `idx`. The submenu is the ID of a global menu root that would be shown when the item is clicked.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemTag

```csharp
public static void GlobalMenuSetItemTag(string menuRoot, int idx, Variant tag)
```

引擎名：`global_menu_set_item_tag`

Sets the metadata of an item, which may be of any type. You can later get it with `GlobalMenuGetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemText

```csharp
public static void GlobalMenuSetItemText(string menuRoot, int idx, string text)
```

引擎名：`global_menu_set_item_text`

Sets the text of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemTooltip

```csharp
public static void GlobalMenuSetItemTooltip(string menuRoot, int idx, string tooltip)
```

引擎名：`global_menu_set_item_tooltip`

Sets the `System.String` tooltip of the item at the specified index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetPopupCallbacks

```csharp
public static void GlobalMenuSetPopupCallbacks(string menuRoot, Callable openCallback, Callable closeCallback)
```

引擎名：`global_menu_set_popup_callbacks`

Registers callables to emit when the menu is respectively about to show or closed. Callback methods should have zero arguments.

### HasAdditionalOutputs

```csharp
public static bool HasAdditionalOutputs()
```

引擎名：`has_additional_outputs`

Returns `true` if any additional outputs have been registered via `RegisterAdditionalOutput()`.

### HasFeature

```csharp
public static bool HasFeature(DisplayServer.Feature feature)
```

引擎名：`has_feature`

Returns `true` if the specified `feature` is supported by the current `DisplayServer`, `false` otherwise.

### HasHardwareKeyboard

```csharp
public static bool HasHardwareKeyboard()
```

引擎名：`has_hardware_keyboard`

Returns `true` if a hardware keyboard is connected.

**Note:** This method is implemented on Android and iOS. On other platforms, this method always returns `true`.

### HelpSetSearchCallbacks

```csharp
public static void HelpSetSearchCallbacks(Callable searchCallback, Callable actionCallback)
```

引擎名：`help_set_search_callbacks`

Sets native help system search callbacks.

`searchCallback` has the following arguments: `String search_string, int result_limit` and return a [`Collections.Dictionary`](Collections.Dictionary.md) with "key, display name" pairs for the search results. Called when the user enters search terms in the `Help` menu.

`actionCallback` has the following arguments: `String key`. Called when the user selects a search result in the `Help` menu.

**Note:** This method is implemented only on macOS.

### ImeGetSelection

```csharp
public static Vector2I ImeGetSelection()
```

引擎名：`ime_get_selection`

Returns the text selection in the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) composition string, with the [`Vector2I`](Vector2I.md)'s `x` component being the caret position and `y` being the length of the selection.

**Note:** This method is implemented only on macOS.

### ImeGetText

```csharp
public static string ImeGetText()
```

引擎名：`ime_get_text`

Returns the composition string contained within the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) window.

**Note:** This method is implemented only on macOS.

### IsDarkMode

```csharp
public static bool IsDarkMode()
```

引擎名：`is_dark_mode`

Returns `true` if OS is using dark mode.

**Note:** This method is implemented on Android, iOS, macOS, Windows, and Linux (X11/Wayland).

### IsDarkModeSupported

```csharp
public static bool IsDarkModeSupported()
```

引擎名：`is_dark_mode_supported`

Returns `true` if OS supports dark mode.

**Note:** This method is implemented on Android, iOS, macOS, Windows, and Linux (X11/Wayland).

### IsInPipMode

```csharp
public static bool IsInPipMode(int windowId = 0)
```

引擎名：`is_in_pip_mode`

Returns `true` if the application is in picture-in-picture mode.

**Note:** This method is implemented on Android.

### IsTouchscreenAvailable

```csharp
public static bool IsTouchscreenAvailable()
```

引擎名：`is_touchscreen_available`

Returns `true` if touch events are available (Android or iOS), the capability is detected on the Web platform or if `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse` is `true`.

### IsWindowTransparencyAvailable

```csharp
public static bool IsWindowTransparencyAvailable()
```

引擎名：`is_window_transparency_available`

Returns `true` if the window background can be made transparent. This method returns `false` if `ProjectSettings.display/window/per_pixel_transparency/allowed` is set to `false`, or if transparency is not supported by the renderer or OS compositor.

### KeyboardGetCurrentLayout

```csharp
public static int KeyboardGetCurrentLayout()
```

引擎名：`keyboard_get_current_layout`

Returns active keyboard layout index.

**Note:** This method is implemented on Linux (X11/Wayland), macOS, and Windows.

### KeyboardGetKeycodeFromPhysical

```csharp
public static Key KeyboardGetKeycodeFromPhysical(Key keycode)
```

引擎名：`keyboard_get_keycode_from_physical`

Converts a physical (US QWERTY) `keycode` to one in the active keyboard layout.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLabelFromPhysical

```csharp
public static Key KeyboardGetLabelFromPhysical(Key keycode)
```

引擎名：`keyboard_get_label_from_physical`

Converts a physical (US QWERTY) `keycode` to localized label printed on the key in the active keyboard layout.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLayoutCount

```csharp
public static int KeyboardGetLayoutCount()
```

引擎名：`keyboard_get_layout_count`

Returns the number of keyboard layouts.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLayoutLanguage

```csharp
public static string KeyboardGetLayoutLanguage(int index)
```

引擎名：`keyboard_get_layout_language`

Returns the ISO-639/BCP-47 language code of the keyboard layout at position `index`.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLayoutName

```csharp
public static string KeyboardGetLayoutName(int index)
```

引擎名：`keyboard_get_layout_name`

Returns the localized name of the keyboard layout at position `index`.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardSetCurrentLayout

```csharp
public static void KeyboardSetCurrentLayout(int index)
```

引擎名：`keyboard_set_current_layout`

Sets the active keyboard layout.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### MouseGetButtonState

```csharp
public static MouseButtonMask MouseGetButtonState()
```

引擎名：`mouse_get_button_state`

Returns the current state of mouse buttons (whether each button is pressed) as a bitmask. If multiple mouse buttons are pressed at the same time, the bits are added together. Equivalent to [`Input.GetMouseButtonMask`](Input.md).

### MouseGetMode

```csharp
public static DisplayServer.MouseMode MouseGetMode()
```

引擎名：`mouse_get_mode`

Returns the current mouse mode. See also `MouseSetMode()`.

### MouseGetPosition

```csharp
public static Vector2I MouseGetPosition()
```

引擎名：`mouse_get_position`

Returns the mouse cursor's current position in screen coordinates.

### MouseSetMode

```csharp
public static void MouseSetMode(DisplayServer.MouseMode mouseMode)
```

引擎名：`mouse_set_mode`

Sets the current mouse mode. See also `MouseGetMode()`.

### PipModeEnter

```csharp
public static void PipModeEnter(int windowId = 0)
```

引擎名：`pip_mode_enter`

Enters picture-in-picture mode.

**Note:** This method is implemented on Android.

### PipModeSetAspectRatio

```csharp
public static void PipModeSetAspectRatio(int numerator, int denominator, int windowId = 0)
```

引擎名：`pip_mode_set_aspect_ratio`

Specifies the aspect ratio for picture-in-picture mode.

**Note:** This method is implemented on Android.

### PipModeSetAutoEnterOnBackground

```csharp
public static void PipModeSetAutoEnterOnBackground(bool autoEnterOnBackground, int windowId = 0)
```

引擎名：`pip_mode_set_auto_enter_on_background`

Specifies whether picture-in-picture mode should be entered automatically when the application goes in the background.

**Note:** This method is implemented on Android.

### ProcessEvents

```csharp
public static void ProcessEvents()
```

引擎名：`process_events`

Perform window manager processing, including input flushing. See also `ForceProcessAndDropEvents()`, [`Input.FlushBufferedEvents`](Input.md) and [`Input.UseAccumulatedInput`](Input.md).

### RegisterAdditionalOutput

```csharp
public static void RegisterAdditionalOutput(GodotObject object)
```

引擎名：`register_additional_output`

Registers an [`GodotObject`](GodotObject.md) which represents an additional output that will be rendered too, beyond normal windows. The [`GodotObject`](GodotObject.md) is only used as an identifier, which can be later passed to `UnregisterAdditionalOutput()`.

This can be used to prevent Godot from skipping rendering when no normal windows are visible.

### ScreenGetDpi

```csharp
public static int ScreenGetDpi(int screen = -1)
```

引擎名：`screen_get_dpi`

Returns the dots per inch density of the specified screen. Returns platform specific default value if `screen` is invalid.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** On macOS, returned value is inaccurate if fractional display scaling mode is used.

**Note:** On Android devices, the actual screen densities are grouped into six generalized densities:

```csharp
ldpi - 120 dpi
mdpi - 160 dpi
hdpi - 240 dpi
xhdpi - 320 dpi
xxhdpi - 480 dpi
xxxhdpi - 640 dpi

```

**Note:** This method is implemented on Android, iOS, Linux (X11/Wayland), macOS, Web, and Windows. On other platforms, this method always returns `72`.

### ScreenGetImage

```csharp
public static Image ScreenGetImage(int screen = -1)
```

引擎名：`screen_get_image`

Returns a screenshot of the `screen`. Returns `null` if `screen` is invalid or the `DisplayServer` fails to capture screenshot.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** This method is implemented on Linux (X11, excluding XWayland), macOS, and Windows. On other platforms, this method always returns `null`.

**Note:** On macOS, this method requires the "Screen Recording" permission. If permission is not granted, this method returns a screenshot that will not include other application windows or OS elements not related to the application.

### ScreenGetImageRect

```csharp
public static Image ScreenGetImageRect(Rect2I rect)
```

引擎名：`screen_get_image_rect`

Returns a screenshot of the screen region defined by `rect`. Returns `null` if `rect` is outside screen bounds or the `DisplayServer` fails to capture screenshot.

**Note:** This method is implemented on macOS and Windows. On other platforms, this method always returns `null`.

**Note:** On macOS, this method requires the "Screen Recording" permission. If permission is not granted, this method returns a screenshot that will not include other application windows or OS elements not related to the application.

### ScreenGetMaxScale

```csharp
public static float ScreenGetMaxScale()
```

引擎名：`screen_get_max_scale`

Returns the greatest scale factor of all screens. See also `ScreenGetScale()`.

**Note:** On macOS, the returned value is `2.0` if there is at least one hiDPI (Retina) screen in the system, and `1.0` in all other cases.

### ScreenGetOrientation

```csharp
public static DisplayServer.ScreenOrientation ScreenGetOrientation(int screen = -1)
```

引擎名：`screen_get_orientation`

Returns the `screen`'s current orientation. See also `ScreenSetOrientation()`. Returns [`DisplayServer.ScreenOrientation.Landscape`](DisplayServer.md) if `screen` is invalid.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** This method is implemented on Android and iOS. On other platforms, this method always returns [`DisplayServer.ScreenOrientation.Landscape`](DisplayServer.md).

### ScreenGetPixel

```csharp
public static Color ScreenGetPixel(Vector2I position)
```

引擎名：`screen_get_pixel`

Returns the color of the pixel at the given screen `position`. On multi-monitor setups, the screen position is relative to the virtual desktop area.

**Note:** This method is implemented on Linux (X11, excluding XWayland), macOS, and Windows. On other platforms, this method always returns `Color(0, 0, 0, 1)`.

**Note:** On macOS, this method requires the "Screen Recording" permission. If permission is not granted, this method returns a color from a screenshot that will not include other application windows or OS elements not related to the application.

### ScreenGetPosition

```csharp
public static Vector2I ScreenGetPosition(int screen = -1)
```

引擎名：`screen_get_position`

Returns the screen's top-left corner position in pixels. Returns `Vector2i.ZERO` if `screen` is invalid. On multi-monitor setups, the screen position is relative to the virtual desktop area. On multi-monitor setups with different screen resolutions or orientations, the origin might be located outside any display like this:

```csharp
* (0, 0)        +-------+
|       |
+-------------+ |       |
|             | |       |
|             | |       |
+-------------+ +-------+

```

See also `ScreenGetSize()`.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

### ScreenGetRefreshRate

```csharp
public static float ScreenGetRefreshRate(int screen = -1)
```

引擎名：`screen_get_refresh_rate`

Returns the current refresh rate of the specified screen. When V-Sync is enabled, this returns the maximum framerate the project can effectively reach. Returns `-1.0` if `screen` is invalid or the `DisplayServer` fails to find the refresh rate for the specified screen.

To fallback to a default refresh rate if the method fails, try:

```csharp
var refresh_rate = DisplayServer.screen_get_refresh_rate()
if refresh_rate < 0:
refresh_rate = 60.0

```

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** This method is implemented on Android, iOS, macOS, Linux (X11 and Wayland), and Windows. On other platforms, this method always returns `-1.0`.

### ScreenGetScale

```csharp
public static float ScreenGetScale(int screen = -1)
```

引擎名：`screen_get_scale`

Returns the scale factor of the specified screen by index. Returns `1.0` if `screen` is invalid. See also `ScreenGetMaxScale()`.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** On macOS, the returned value is `2.0` for hiDPI (Retina) screens, and `1.0` for all other cases.

**Note:** On Linux (Wayland), the returned value is accurate only when `screen` is `ScreenOfMainWindow`. Due to API limitations, passing a direct index will return a rounded-up integer, if the screen has a fractional scale (e.g. `1.25` would get rounded up to `2.0`).

**Note:** This method is implemented on Android, iOS, Web, macOS, and Linux (Wayland). On other platforms, this method always returns `1.0`.

### ScreenGetSize

```csharp
public static Vector2I ScreenGetSize(int screen = -1)
```

引擎名：`screen_get_size`

Returns the screen's size in pixels. See also `ScreenGetPosition()` and `ScreenGetUsableRect()`. Returns `Vector2i.ZERO` if `screen` is invalid.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

### ScreenGetUsableRect

```csharp
public static Rect2I ScreenGetUsableRect(int screen = -1)
```

引擎名：`screen_get_usable_rect`

Returns the portion of the screen that is not obstructed by a status bar in pixels. See also `ScreenGetSize()`.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns `Rect2i(screen_get_position(screen), screen_get_size(screen))`.

### ScreenIsKeptOn

```csharp
public static bool ScreenIsKeptOn()
```

引擎名：`screen_is_kept_on`

Returns `true` if the screen should never be turned off by the operating system's power-saving measures. See also `ScreenSetKeepOn()`.

### ScreenSetKeepOn

```csharp
public static void ScreenSetKeepOn(bool enable)
```

引擎名：`screen_set_keep_on`

Sets whether the screen should never be turned off by the operating system's power-saving measures. See also `ScreenIsKeptOn()`.

### ScreenSetOrientation

```csharp
public static void ScreenSetOrientation(DisplayServer.ScreenOrientation orientation, int screen = -1)
```

引擎名：`screen_set_orientation`

Sets the `screen`'s `orientation`. See also `ScreenGetOrientation()`.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** This method is implemented on Android and iOS.

**Note:** On iOS, this method has no effect if `ProjectSettings.display/window/handheld/orientation` is not set to [`DisplayServer.ScreenOrientation.Sensor`](DisplayServer.md).

### SetHardwareKeyboardConnectionChangeCallback

```csharp
public static void SetHardwareKeyboardConnectionChangeCallback(Callable callable)
```

引擎名：`set_hardware_keyboard_connection_change_callback`

Sets the callback that should be called when a hardware keyboard is connected or disconnected. `callable` should accept a single `System.Boolean` argument indicating whether the keyboard has been connected (`true`) or disconnected (`false`).

**Note:** This method is only implemented on Android.

### SetIcon

```csharp
public static void SetIcon(Image image)
```

引擎名：`set_icon`

Sets the application icon and icons of all windows with an [`Image`](Image.md). To use icons in the operating system's native format, use `SetNativeIcon()` instead.

**Note:** Requires support for [`DisplayServer.Feature.Icon`](DisplayServer.md).

### SetNativeIcon

```csharp
public static void SetNativeIcon(string fileName)
```

引擎名：`set_native_icon`

Sets the window icon (usually displayed in the top-left corner) in the operating system's *native* format. The file at `fileName` must be in `.ico` format on Windows or `.icns` on macOS. By using specially crafted `.ico` or `.icns` icons, `SetNativeIcon()` allows specifying different icons depending on the size the icon is displayed at. This size is determined by the operating system and user preferences (including the display scale factor). To use icons in other formats, use `SetIcon()` instead.

**Note:** Requires support for [`DisplayServer.Feature.NativeIcon`](DisplayServer.md).

### SetSystemThemeChangeCallback

```csharp
public static void SetSystemThemeChangeCallback(Callable callable)
```

引擎名：`set_system_theme_change_callback`

Sets the callback that should be called when the system's theme settings are changed. `callable` should accept zero arguments.

**Note:** This method is implemented on Android, iOS, macOS, Windows, and Linux (X11/Wayland).

### ShowEmojiAndSymbolPicker

```csharp
public static void ShowEmojiAndSymbolPicker()
```

引擎名：`show_emoji_and_symbol_picker`

Opens system emoji and symbol picker.

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorGetRect

```csharp
public static Rect2 StatusIndicatorGetRect(int id)
```

引擎名：`status_indicator_get_rect`

Returns the rectangle for the given status indicator `id` in screen coordinates. If the status indicator is not visible, returns an empty [`Rect2`](Rect2.md).

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorSetCallback

```csharp
public static void StatusIndicatorSetCallback(int id, Callable callback)
```

引擎名：`status_indicator_set_callback`

Sets the application status indicator activation callback. `callback` should take two arguments: `System.Int32` mouse button index (one of [`MouseButton`](MouseButton.md) values) and [`Vector2I`](Vector2I.md) click position in screen coordinates.

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorSetIcon

```csharp
public static void StatusIndicatorSetIcon(int id, Texture2D icon)
```

引擎名：`status_indicator_set_icon`

Sets the application status indicator icon.

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorSetMenu

```csharp
public static void StatusIndicatorSetMenu(int id, Rid menuRid)
```

引擎名：`status_indicator_set_menu`

Sets the application status indicator native popup menu.

**Note:** On macOS, the menu is activated by any mouse button. Its activation callback is *not* triggered.

**Note:** On Windows, the menu is activated by the right mouse button, selecting the status icon and pressing Shift + F10, or the applications key. The menu's activation callback for the other mouse buttons is still triggered.

**Note:** Native popup is only supported if [`NativeMenu`](NativeMenu.md) supports the [`NativeMenu.Feature.PopupMenu`](NativeMenu.md) feature.

### StatusIndicatorSetTooltip

```csharp
public static void StatusIndicatorSetTooltip(int id, string tooltip)
```

引擎名：`status_indicator_set_tooltip`

Sets the application status indicator tooltip.

**Note:** This method is implemented on macOS and Windows.

### TabletGetCurrentDriver

```csharp
public static string TabletGetCurrentDriver()
```

引擎名：`tablet_get_current_driver`

Returns current active tablet driver name.

**Note:** This method is implemented only on Windows.

### TabletGetDriverCount

```csharp
public static int TabletGetDriverCount()
```

引擎名：`tablet_get_driver_count`

Returns the total number of available tablet drivers.

**Note:** This method is implemented only on Windows.

### TabletGetDriverName

```csharp
public static string TabletGetDriverName(int idx)
```

引擎名：`tablet_get_driver_name`

Returns the tablet driver name for the given index.

**Note:** This method is implemented only on Windows.

### TabletSetCurrentDriver

```csharp
public static void TabletSetCurrentDriver(string name)
```

引擎名：`tablet_set_current_driver`

Set active tablet driver name.

Supported drivers:

- `winink`: Windows Ink API, default.

- `wintab`: Wacom Wintab API (compatible device driver required).

- `dummy`: Dummy driver, tablet input is disabled.

**Note:** This method is implemented only on Windows.

### TtsGetVoices

```csharp
public static Array<Dictionary> TtsGetVoices()
```

引擎名：`tts_get_voices`

Returns an [`Collections.Array`](Collections.Array.md) of voice information dictionaries.

Each [`Collections.Dictionary`](Collections.Dictionary.md) contains two `System.String` entries:

- `name` is voice name.

- `id` is voice identifier.

- `language` is language code in `lang_Variant` format. The `lang` part is a 2 or 3-letter code based on the ISO-639 standard, in lowercase. The `Variant` part is an engine-dependent string describing country, region or/and dialect.

Note that Godot depends on system libraries for text-to-speech functionality. These libraries are installed by default on Windows and macOS, but not on all Linux distributions. If they are not present, this method will return an empty list. This applies to both Godot users on Linux, as well as end-users on Linux running Godot games that use text-to-speech.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsGetVoicesForLanguage

```csharp
public static string[] TtsGetVoicesForLanguage(string language)
```

引擎名：`tts_get_voices_for_language`

Returns a `System.String`[] of voice identifiers for the `language`.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsIsPaused

```csharp
public static bool TtsIsPaused()
```

引擎名：`tts_is_paused`

Returns `true` if the synthesizer is in a paused state.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsIsSpeaking

```csharp
public static bool TtsIsSpeaking()
```

引擎名：`tts_is_speaking`

Returns `true` if the synthesizer is generating speech, or have utterance waiting in the queue.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsPause

```csharp
public static void TtsPause()
```

引擎名：`tts_pause`

Puts the synthesizer into a paused state.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsResume

```csharp
public static void TtsResume()
```

引擎名：`tts_resume`

Resumes the synthesizer if it was paused.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsSetUtteranceCallback

```csharp
public static void TtsSetUtteranceCallback(DisplayServer.TtsUtteranceEvent event, Callable callable)
```

引擎名：`tts_set_utterance_callback`

Adds a callback, which is called when the utterance has started, finished, canceled or reached a text boundary.

- [`DisplayServer.TtsUtteranceEvent.Started`](DisplayServer.md), [`DisplayServer.TtsUtteranceEvent.Ended`](DisplayServer.md), and [`DisplayServer.TtsUtteranceEvent.Canceled`](DisplayServer.md) callable's method should take one `System.Int32` parameter, the utterance ID.

- [`DisplayServer.TtsUtteranceEvent.Boundary`](DisplayServer.md) callable's method should take two `System.Int32` parameters, the index of the character and the utterance ID.

**Note:** The granularity of the boundary callbacks is engine dependent.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsSpeak

```csharp
public static void TtsSpeak(string text, string voice, int volume = 50, float pitch = 1f, float rate = 1f, long utteranceId = 0, bool interrupt = false)
```

引擎名：`tts_speak`

Adds an utterance to the queue. If `interrupt` is `true`, the queue is cleared first.

- `voice` identifier is one of the `"id"` values returned by `TtsGetVoices()` or one of the values returned by `TtsGetVoicesForLanguage()`.

- `volume` ranges from `0` (lowest) to `100` (highest).

- `pitch` ranges from `0.0` (lowest) to `2.0` (highest), `1.0` is default pitch for the current voice.

- `rate` ranges from `0.1` (lowest) to `10.0` (highest), `1.0` is a normal speaking rate. Other values act as a percentage relative.

- `utteranceId` is passed as a parameter to the callback functions.

**Note:** On Windows and Linux (X11/Wayland), utterance `text` can use SSML markup. SSML support is engine and voice dependent. If the engine does not support SSML, you should strip out all XML markup before calling `TtsSpeak()`.

**Note:** The granularity of pitch, rate, and volume is engine and voice dependent. Values may be truncated.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsSpeak

```csharp
public static void TtsSpeak(string text, string voice, int volume, float pitch, float rate, int utteranceId, bool interrupt)
```

引擎名：`tts_speak`

Adds an utterance to the queue. If `interrupt` is `true`, the queue is cleared first.

- `voice` identifier is one of the `"id"` values returned by `TtsGetVoices()` or one of the values returned by `TtsGetVoicesForLanguage()`.

- `volume` ranges from `0` (lowest) to `100` (highest).

- `pitch` ranges from `0.0` (lowest) to `2.0` (highest), `1.0` is default pitch for the current voice.

- `rate` ranges from `0.1` (lowest) to `10.0` (highest), `1.0` is a normal speaking rate. Other values act as a percentage relative.

- `utteranceId` is passed as a parameter to the callback functions.

**Note:** On Windows and Linux (X11/Wayland), utterance `text` can use SSML markup. SSML support is engine and voice dependent. If the engine does not support SSML, you should strip out all XML markup before calling `TtsSpeak()`.

**Note:** The granularity of pitch, rate, and volume is engine and voice dependent. Values may be truncated.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsStop

```csharp
public static void TtsStop()
```

引擎名：`tts_stop`

Stops synthesis in progress and removes all utterances from the queue.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### UnregisterAdditionalOutput

```csharp
public static void UnregisterAdditionalOutput(GodotObject object)
```

引擎名：`unregister_additional_output`

Unregisters an [`GodotObject`](GodotObject.md) representing an additional output, that was registered via `RegisterAdditionalOutput()`.

### VirtualKeyboardGetHeight

```csharp
public static int VirtualKeyboardGetHeight()
```

引擎名：`virtual_keyboard_get_height`

Returns the on-screen keyboard's height in pixels. Returns `0` if there is no keyboard or if it is currently hidden.

**Note:** On Android 7 and 8, the keyboard height may return `0` the first time the keyboard is opened in non-immersive mode. This behavior does not occur in immersive mode.

### VirtualKeyboardHide

```csharp
public static void VirtualKeyboardHide()
```

引擎名：`virtual_keyboard_hide`

Hides the virtual keyboard if it is shown, does nothing otherwise.

### VirtualKeyboardShow

```csharp
public static void VirtualKeyboardShow(string existingText, Rect2? position = null, DisplayServer.VirtualKeyboardType type = DisplayServer.VirtualKeyboardType.Default, int maxLength = -1, int cursorStart = -1, int cursorEnd = -1)
```

引擎名：`virtual_keyboard_show`

Shows the virtual keyboard if the platform has one.

`existingText` parameter is useful for implementing your own [`LineEdit`](LineEdit.md) or [`TextEdit`](TextEdit.md), as it tells the virtual keyboard what text has already been typed (the virtual keyboard uses it for auto-correct and predictions).

`position` parameter is the screen space [`Rect2`](Rect2.md) of the edited text.

`type` parameter allows configuring which type of virtual keyboard to show.

`maxLength` limits the number of characters that can be entered if different from `-1`.

`cursorStart` can optionally define the current text cursor position if `cursorEnd` is not set.

`cursorStart` and `cursorEnd` can optionally define the current text selection.

**Note:** This method is implemented on Android, iOS and Web.

参数：
- `position`：If the parameter is null, then the default value is `new Rect2(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f))`.

### WarpMouse

```csharp
public static void WarpMouse(Vector2I position)
```

引擎名：`warp_mouse`

Sets the mouse cursor position to the given `position` relative to an origin at the upper left corner of the currently focused game Window Manager window.

**Note:** `WarpMouse()` is only supported on Windows, macOS, and Linux (X11/Wayland). It has no effect on Android, iOS, and Web.

### WindowCanDraw

```csharp
public static bool WindowCanDraw(int windowId = 0)
```

引擎名：`window_can_draw`

Returns `true` if anything can be drawn in the window specified by `windowId`, `false` otherwise. Using the `--disable-render-loop` command line argument or a headless build will return `false`.

### WindowGetActivePopup

```csharp
public static int WindowGetActivePopup()
```

引擎名：`window_get_active_popup`

Returns ID of the active popup window, or `InvalidWindowId` if there is none.

### WindowGetAttachedInstanceId

```csharp
public static ulong WindowGetAttachedInstanceId(int windowId = 0)
```

引擎名：`window_get_attached_instance_id`

Returns the [`GodotObject.GetInstanceId`](GodotObject.md) of the [`Window`](Window.md) the `windowId` is attached to.

### WindowGetCurrentScreen

```csharp
public static int WindowGetCurrentScreen(int windowId = 0)
```

引擎名：`window_get_current_screen`

Returns the screen the window specified by `windowId` is currently positioned on. If the screen overlaps multiple displays, the screen where the window's center is located is returned. See also `WindowSetCurrentScreen()`. Returns `InvalidScreen` if `windowId` is invalid.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns `0`.

### WindowGetFlag

```csharp
public static bool WindowGetFlag(DisplayServer.WindowFlags flag, int windowId = 0)
```

引擎名：`window_get_flag`

Returns the current value of the given window's `flag`.

### WindowGetHdrOutputCurrentMaxLuminance

```csharp
public static float WindowGetHdrOutputCurrentMaxLuminance(int windowId = 0)
```

引擎名：`window_get_hdr_output_current_max_luminance`

When `WindowIsHdrOutputEnabled()` returns `true`, this returns the current maximum luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If the maximum luminance is being automatically adjusted based on the screen's capabilities, this method will return that value. Otherwise, it will return the value set by `WindowSetHdrOutputMaxLuminance()`. This maximum luminance value is used when calculating `WindowGetOutputMaxLinearValue()`.

**Note:** This maximum luminance may not match the physical behavior of the screen, but will always be proportionally correct relative to `WindowGetHdrOutputCurrentReferenceLuminance()`.

### WindowGetHdrOutputCurrentReferenceLuminance

```csharp
public static float WindowGetHdrOutputCurrentReferenceLuminance(int windowId = 0)
```

引擎名：`window_get_hdr_output_current_reference_luminance`

When `WindowIsHdrOutputEnabled()` returns `true`, this returns the current reference white luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If the reference luminance is being automatically adjusted to match the operating system brightness, this will return that value. Otherwise, it will return the value set by `WindowSetHdrOutputReferenceLuminance()`. This reference luminance value is used when calculating `WindowGetOutputMaxLinearValue()`.

**Note:** This reference white luminance may not match the physical behavior of the screen, but will always be proportionally correct relative to `WindowGetHdrOutputCurrentMaxLuminance()`.

### WindowGetHdrOutputMaxLuminance

```csharp
public static float WindowGetHdrOutputMaxLuminance(int windowId = 0)
```

引擎名：`window_get_hdr_output_max_luminance`

Returns the maximum luminance in nits (cd/m²) set for HDR output by the window specified by `windowId`. Negative values indicate that the value is being automatically adjusted based on the screen's capabilities. See also `WindowGetHdrOutputCurrentMaxLuminance()`.

### WindowGetHdrOutputReferenceLuminance

```csharp
public static float WindowGetHdrOutputReferenceLuminance(int windowId = 0)
```

引擎名：`window_get_hdr_output_reference_luminance`

Returns the reference white luminance in nits (cd/m²) set for HDR output by the window specified by `windowId`. Negative values indicate that the value is being automatically adjusted to match the operating system brightness. See also `WindowGetHdrOutputCurrentReferenceLuminance()`.

### WindowGetMaxSize

```csharp
public static Vector2I WindowGetMaxSize(int windowId = 0)
```

引擎名：`window_get_max_size`

Returns the window's maximum size (in pixels). See also `WindowSetMaxSize()`.

### WindowGetMinSize

```csharp
public static Vector2I WindowGetMinSize(int windowId = 0)
```

引擎名：`window_get_min_size`

Returns the window's minimum size (in pixels). See also `WindowSetMinSize()`.

### WindowGetMode

```csharp
public static DisplayServer.WindowMode WindowGetMode(int windowId = 0)
```

引擎名：`window_get_mode`

Returns the mode of the given window.

### WindowGetNativeHandle

```csharp
public static long WindowGetNativeHandle(DisplayServer.HandleType handleType, int windowId = 0)
```

引擎名：`window_get_native_handle`

Returns internal structure pointers for use in plugins.

**Note:** This method is implemented on Android, Linux (X11/Wayland), macOS, and Windows.

### WindowGetOutputMaxLinearValue

```csharp
public static float WindowGetOutputMaxLinearValue(int windowId = 0)
```

引擎名：`window_get_output_max_linear_value`

Returns the maximum value for linear color components that can be displayed for the window specified by `windowId`, regardless of SDR or HDR output. Returns `1.0` if HDR is not enabled or not supported. When HDR output is enabled, this is calculated based on `WindowGetHdrOutputCurrentReferenceLuminance()` and `WindowGetHdrOutputCurrentMaxLuminance()`. The [`Window.OutputMaxLinearValueChanged`](Window.md) signal will be emitted whenever this value changes. This value is used by tonemapping and other [`Environment`](Environment.md) effects to ensure that bright colors are presented in the range that can be displayed by this window. Corresponds to [`Window.GetOutputMaxLinearValue`](Window.md).

### WindowGetPopupSafeRect

```csharp
public static Rect2I WindowGetPopupSafeRect(int window)
```

引擎名：`window_get_popup_safe_rect`

Returns the bounding box of control, or menu item that was used to open the popup window, in the screen coordinate system.

### WindowGetPosition

```csharp
public static Vector2I WindowGetPosition(int windowId = 0)
```

引擎名：`window_get_position`

Returns the position of the client area of the given window on the screen.

### WindowGetPositionWithDecorations

```csharp
public static Vector2I WindowGetPositionWithDecorations(int windowId = 0)
```

引擎名：`window_get_position_with_decorations`

Returns the position of the given window on the screen including the borders drawn by the operating system. See also `WindowGetPosition()`.

### WindowGetSafeTitleMargins

```csharp
public static Vector3I WindowGetSafeTitleMargins(int windowId = 0)
```

引擎名：`window_get_safe_title_margins`

Returns left margins (`x`), right margins (`y`) and height (`z`) of the title that are safe to use (contains no buttons or other elements) when [`DisplayServer.WindowFlags.ExtendToTitle`](DisplayServer.md) flag is set.

### WindowGetSize

```csharp
public static Vector2I WindowGetSize(int windowId = 0)
```

引擎名：`window_get_size`

Returns the size of the window specified by `windowId` (in pixels), excluding the borders drawn by the operating system. This is also called the "client area". See also `WindowGetSizeWithDecorations()`, `WindowSetSize()` and `WindowGetPosition()`.

### WindowGetSizeWithDecorations

```csharp
public static Vector2I WindowGetSizeWithDecorations(int windowId = 0)
```

引擎名：`window_get_size_with_decorations`

Returns the size of the window specified by `windowId` (in pixels), including the borders drawn by the operating system. See also `WindowGetSize()`.

### WindowGetTitleSize

```csharp
public static Vector2I WindowGetTitleSize(string title, int windowId = 0)
```

引擎名：`window_get_title_size`

Returns the estimated window title bar size (including text and window buttons) for the window specified by `windowId` (in pixels). This method does not change the window title.

**Note:** This method is implemented on macOS and Windows.

### WindowGetVsyncMode

```csharp
public static DisplayServer.VSyncMode WindowGetVsyncMode(int windowId = 0)
```

引擎名：`window_get_vsync_mode`

Returns the V-Sync mode of the given window.

### WindowIsFocused

```csharp
public static bool WindowIsFocused(int windowId = 0)
```

引擎名：`window_is_focused`

Returns `true` if the window specified by `windowId` is focused.

### WindowIsHdrOutputEnabled

```csharp
public static bool WindowIsHdrOutputEnabled(int windowId = 0)
```

引擎名：`window_is_hdr_output_enabled`

Returns `true` if HDR output is currently enabled for the window specified by `windowId`. The returned value may change dynamically based on system settings, screen capabilities, and which screen the window is currently on.

### WindowIsHdrOutputRequested

```csharp
public static bool WindowIsHdrOutputRequested(int windowId = 0)
```

引擎名：`window_is_hdr_output_requested`

Returns `true` if HDR output is requested for the window specified by `windowId`. Corresponds to [`Window.HdrOutputRequested`](Window.md).

### WindowIsHdrOutputSupported

```csharp
public static bool WindowIsHdrOutputSupported(int windowId = 0)
```

引擎名：`window_is_hdr_output_supported`

Returns `true` if the window specified by `windowId` supports HDR output. This depends on the platform, screen capabilities, system settings, and the screen the window is currently on.

### WindowIsMaximizeAllowed

```csharp
public static bool WindowIsMaximizeAllowed(int windowId = 0)
```

引擎名：`window_is_maximize_allowed`

Returns `true` if the given window can be maximized (the maximize button is enabled).

### WindowMaximizeOnTitleDblClick

```csharp
public static bool WindowMaximizeOnTitleDblClick()
```

引擎名：`window_maximize_on_title_dbl_click`

Returns `true` if double-clicking on a window's title should maximize it.

**Note:** This method is implemented only on macOS.

### WindowMinimizeOnTitleDblClick

```csharp
public static bool WindowMinimizeOnTitleDblClick()
```

引擎名：`window_minimize_on_title_dbl_click`

Returns `true` if double-clicking on a window's title should minimize it.

**Note:** This method is implemented only on macOS.

### WindowMoveToForeground

```csharp
public static void WindowMoveToForeground(int windowId = 0)
```

引擎名：`window_move_to_foreground`

Moves the window specified by `windowId` to the foreground, so that it is visible over other windows.

### WindowRequestAttention

```csharp
public static void WindowRequestAttention(int windowId = 0)
```

引擎名：`window_request_attention`

Makes the window specified by `windowId` request attention, which is materialized by the window title and taskbar entry blinking until the window is focused. This usually has no visible effect if the window is currently focused. The exact behavior varies depending on the operating system.

### WindowRequestHdrOutput

```csharp
public static void WindowRequestHdrOutput(bool enable, int windowId = 0)
```

引擎名：`window_request_hdr_output`

If `enable` is `true`, HDR output is requested for the window specified by `windowId`. The window will automatically switch between HDR and SDR if it is moved between screens, screen capabilities change, or system settings are modified. This will internally force [`Viewport.UseHdr2D`](Viewport.md) to be enabled on the main [`Viewport`](Viewport.md). All other [`SubViewport`](SubViewport.md) of the [`Window`](Window.md) must have their [`Viewport.UseHdr2D`](Viewport.md) property enabled to produce HDR output. Corresponds to [`Window.HdrOutputRequested`](Window.md).

### WindowSetColor

```csharp
public static void WindowSetColor(Color color)
```

引擎名：`window_set_color`

Sets the background color of the root window.

**Note:** This method is implemented only on Android.

### WindowSetCurrentScreen

```csharp
public static void WindowSetCurrentScreen(int screen, int windowId = 0)
```

引擎名：`window_set_current_screen`

Moves the window specified by `windowId` to the specified `screen`. See also `WindowGetCurrentScreen()`.

**Note:** One of the following constants can be used as `screen`: `ScreenOfMainWindow`, `ScreenPrimary`, `ScreenWithMouseFocus`, or `ScreenWithKeyboardFocus`.

**Note:** This method is implemented on Linux/X11, macOS, and Windows.

### WindowSetDropFilesCallback

```csharp
public static void WindowSetDropFilesCallback(Callable callback, int windowId = 0)
```

引擎名：`window_set_drop_files_callback`

Sets the `callback` that should be called when files are dropped from the operating system's file manager to the window specified by `windowId`. `callback` should take one `System.String`[] argument, which is the list of dropped files.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

**Note:** This method is implemented on Windows, macOS, Linux (X11/Wayland), and Web.

### WindowSetExclusive

```csharp
public static void WindowSetExclusive(int windowId, bool exclusive)
```

引擎名：`window_set_exclusive`

If set to `true`, this window will always stay on top of its parent window, parent window will ignore input while this window is opened.

**Note:** On macOS, exclusive windows are confined to the same space (virtual desktop or screen) as the parent window.

**Note:** This method is implemented on macOS and Windows.

### WindowSetFlag

```csharp
public static void WindowSetFlag(DisplayServer.WindowFlags flag, bool enabled, int windowId = 0)
```

引擎名：`window_set_flag`

Enables or disables the given window's given `flag`.

### WindowSetHdrOutputMaxLuminance

```csharp
public static void WindowSetHdrOutputMaxLuminance(float maxLuminance, int windowId = 0)
```

引擎名：`window_set_hdr_output_max_luminance`

Sets the maximum luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If `maxLuminance` is negative, the window uses the screen's maximum luminance that is reported by the operating system. By default, this luminance is set to `-1.0` for every window. Typically this property should be left at this default value, but may optionally be exposed through in-game settings to allow the player to correct an inaccurate maximum luminance reported by the operating system. See also `WindowGetHdrOutputCurrentMaxLuminance()` and `WindowGetHdrOutputMaxLuminance()`.

**Note:** This method is only implemented on macOS and Windows. Other platforms will always use the screen's maximum luminance that is reported by the operating system.

### WindowSetHdrOutputReferenceLuminance

```csharp
public static void WindowSetHdrOutputReferenceLuminance(float referenceLuminance, int windowId = 0)
```

引擎名：`window_set_hdr_output_reference_luminance`

Sets the reference white luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If `referenceLuminance` is negative, the window automatically adjusts to the brightness set by the operating system. By default, this luminance is set to `-1.0` for every window. Typically this property should be left at this default value, but may optionally be exposed as an "HDR Brightness" in-game setting to allow the player to adjust the brightness of their game, independently of their device settings. See also `WindowGetHdrOutputCurrentReferenceLuminance()` and `WindowGetHdrOutputReferenceLuminance()`.

**Note:** This method is only implemented on Windows. Other platforms will always use the reference luminance that is reported by the operating system.

### WindowSetIcon

```csharp
public static void WindowSetIcon(Image icon, int windowId = 0)
```

引擎名：`window_set_icon`

Sets the window icon (usually displayed in the top-left corner) for the window specified by `windowId`.

**Note:** This method is implemented on Linux and Windows.

### WindowSetImeActive

```csharp
public static void WindowSetImeActive(bool active, int windowId = 0)
```

引擎名：`window_set_ime_active`

Sets whether [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) should be enabled for the window specified by `windowId`. See also `WindowSetImePosition()`.

### WindowSetImePosition

```csharp
public static void WindowSetImePosition(Vector2I position, int windowId = 0)
```

引擎名：`window_set_ime_position`

Sets the position of the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) popup for the specified `windowId`. Only effective if `WindowSetImeActive()` was set to `true` for the specified `windowId`.

### WindowSetInputEventCallback

```csharp
public static void WindowSetInputEventCallback(Callable callback, int windowId = 0)
```

引擎名：`window_set_input_event_callback`

Sets the `callback` that should be called when any [`InputEvent`](InputEvent.md) is sent to the window specified by `windowId`.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowSetInputTextCallback

```csharp
public static void WindowSetInputTextCallback(Callable callback, int windowId = 0)
```

引擎名：`window_set_input_text_callback`

Sets the `callback` that should be called when text is entered using the virtual keyboard to the window specified by `windowId`.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowSetMaxSize

```csharp
public static void WindowSetMaxSize(Vector2I maxSize, int windowId = 0)
```

引擎名：`window_set_max_size`

Sets the maximum size of the window specified by `windowId` in pixels. Normally, the user will not be able to drag the window to make it larger than the specified size. See also `WindowGetMaxSize()`.

**Note:** It's recommended to change this value using [`Window.MaxSize`](Window.md) instead.

**Note:** Using third-party tools, it is possible for users to disable window geometry restrictions and therefore bypass this limit.

### WindowSetMinSize

```csharp
public static void WindowSetMinSize(Vector2I minSize, int windowId = 0)
```

引擎名：`window_set_min_size`

Sets the minimum size for the given window to `minSize` in pixels. Normally, the user will not be able to drag the window to make it smaller than the specified size. See also `WindowGetMinSize()`.

**Note:** It's recommended to change this value using [`Window.MinSize`](Window.md) instead.

**Note:** By default, the main window has a minimum size of `Vector2i(64, 64)`. This prevents issues that can arise when the window is resized to a near-zero size.

**Note:** Using third-party tools, it is possible for users to disable window geometry restrictions and therefore bypass this limit.

### WindowSetMode

```csharp
public static void WindowSetMode(DisplayServer.WindowMode mode, int windowId = 0)
```

引擎名：`window_set_mode`

Sets window mode for the given window to `mode`.

**Note:** On Android, setting it to [`DisplayServer.WindowMode.Fullscreen`](DisplayServer.md) or [`DisplayServer.WindowMode.ExclusiveFullscreen`](DisplayServer.md) will enable immersive mode.

**Note:** Setting the window to full screen forcibly sets the borderless flag to `true`, so make sure to set it back to `false` when not wanted.

### WindowSetMousePassthrough

```csharp
public static void WindowSetMousePassthrough(ReadOnlySpan<Vector2> region, int windowId)
```

引擎名：`window_set_mouse_passthrough`

Sets a polygonal region of the window which accepts mouse events. Mouse events outside the region will be passed through.

Passing an empty array will disable passthrough support (all mouse events will be intercepted by the window, which is the default behavior).

```csharp
// Set region, using Path2D node.
DisplayServer.WindowSetMousePassthrough(GetNode<Path2D>("Path2D").Curve.GetBakedPoints());

// Set region, using Polygon2D node.
DisplayServer.WindowSetMousePassthrough(GetNode<Polygon2D>("Polygon2D").Polygon);

// Reset region to default.
DisplayServer.WindowSetMousePassthrough([]);

```

**Note:** On Windows, the portion of a window that lies outside the region is not drawn, while on Linux (X11) and macOS it is.

**Note:** This method is implemented on Linux (X11), macOS and Windows.

### WindowSetMousePassthrough

```csharp
public static void WindowSetMousePassthrough(Vector2[] region, int windowId = 0)
```

引擎名：`window_set_mouse_passthrough`

Sets a polygonal region of the window which accepts mouse events. Mouse events outside the region will be passed through.

Passing an empty array will disable passthrough support (all mouse events will be intercepted by the window, which is the default behavior).

```csharp
// Set region, using Path2D node.
DisplayServer.WindowSetMousePassthrough(GetNode<Path2D>("Path2D").Curve.GetBakedPoints());

// Set region, using Polygon2D node.
DisplayServer.WindowSetMousePassthrough(GetNode<Polygon2D>("Polygon2D").Polygon);

// Reset region to default.
DisplayServer.WindowSetMousePassthrough([]);

```

**Note:** On Windows, the portion of a window that lies outside the region is not drawn, while on Linux (X11) and macOS it is.

**Note:** This method is implemented on Linux (X11), macOS and Windows.

### WindowSetPopupSafeRect

```csharp
public static void WindowSetPopupSafeRect(int window, Rect2I rect)
```

引擎名：`window_set_popup_safe_rect`

Sets the bounding box of control, or menu item that was used to open the popup window, in the screen coordinate system. Clicking this area will not auto-close this popup.

### WindowSetPosition

```csharp
public static void WindowSetPosition(Vector2I position, int windowId = 0)
```

引擎名：`window_set_position`

Sets the position of the given window to `position`. On multi-monitor setups, the screen position is relative to the virtual desktop area. On multi-monitor setups with different screen resolutions or orientations, the origin may be located outside any display like this:

```csharp
* (0, 0)        +-------+
|       |
+-------------+ |       |
|             | |       |
|             | |       |
+-------------+ +-------+

```

See also `WindowGetPosition()` and `WindowSetSize()`.

**Note:** It's recommended to change this value using [`Window.Position`](Window.md) instead.

**Note:** On Linux (Wayland): this method is a no-op.

### WindowSetRectChangedCallback

```csharp
public static void WindowSetRectChangedCallback(Callable callback, int windowId = 0)
```

引擎名：`window_set_rect_changed_callback`

Sets the `callback` that will be called when the window specified by `windowId` is moved or resized.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowSetSize

```csharp
public static void WindowSetSize(Vector2I size, int windowId = 0)
```

引擎名：`window_set_size`

Sets the size of the given window to `size` (in pixels). See also `WindowGetSize()` and `WindowGetPosition()`.

**Note:** It's recommended to change this value using [`Window.Size`](Window.md) instead.

### WindowSetTaskbarProgressState

```csharp
public static void WindowSetTaskbarProgressState(DisplayServer.ProgressState state, int windowId = 0)
```

引擎名：`window_set_taskbar_progress_state`

Sets the type and state of the progress bar on the taskbar/dock icon of the window specified by `windowId`. See [`DisplayServer.ProgressState`](DisplayServer.md) for possible values and how each mode behaves.

**Note:** This method is implemented only on Windows and macOS.

**Note:** On macOS, the progress bar is displayed only for the main window.

### WindowSetTaskbarProgressValue

```csharp
public static void WindowSetTaskbarProgressValue(float value, int windowId = 0)
```

引擎名：`window_set_taskbar_progress_value`

Creates a progress bar on the taskbar/dock icon of the window specified by `windowId` if it does not exist, sets the progress of the icon.

`value` acts as a relative percentage value, ranges from `0.0` (lowest) to `1.0` (highest).

**Note:** This method is implemented only on Windows and macOS.

**Note:** On macOS, the progress bar is displayed only for the main window.

### WindowSetTitle

```csharp
public static void WindowSetTitle(string title, int windowId = 0)
```

引擎名：`window_set_title`

Sets the title of the given window to `title`.

**Note:** It's recommended to change this value using [`Window.Title`](Window.md) instead.

**Note:** Avoid changing the window title every frame, as this can cause performance issues on certain window managers. Try to change the window title only a few times per second at most.

### WindowSetTransient

```csharp
public static void WindowSetTransient(int windowId, int parentWindowId)
```

引擎名：`window_set_transient`

Sets window transient parent. Transient window will be destroyed with its transient parent and will return focus to their parent when closed. The transient window is displayed on top of a non-exclusive full-screen parent window. Transient windows can't enter full-screen mode.

**Note:** It's recommended to change this value using [`Window.Transient`](Window.md) instead.

**Note:** The behavior might be different depending on the platform.

### WindowSetVsyncMode

```csharp
public static void WindowSetVsyncMode(DisplayServer.VSyncMode vsyncMode, int windowId = 0)
```

引擎名：`window_set_vsync_mode`

Sets the V-Sync mode of the given window. See also `ProjectSettings.display/window/vsync/vsync_mode`.

Depending on the platform and used renderer, the engine will fall back to [`DisplayServer.VSyncMode.Enabled`](DisplayServer.md) if the desired mode is not supported.

**Note:** V-Sync modes other than [`DisplayServer.VSyncMode.Enabled`](DisplayServer.md) are only supported in the Forward+ and Mobile rendering methods, not Compatibility.

### WindowSetWindowButtonsOffset

```csharp
public static void WindowSetWindowButtonsOffset(Vector2I offset, int windowId = 0)
```

引擎名：`window_set_window_buttons_offset`

When [`DisplayServer.WindowFlags.ExtendToTitle`](DisplayServer.md) flag is set, set offset to the center of the first titlebar button.

**Note:** This flag is implemented only on macOS.

### WindowSetWindowEventCallback

```csharp
public static void WindowSetWindowEventCallback(Callable callback, int windowId = 0)
```

引擎名：`window_set_window_event_callback`

Sets the `callback` that will be called when an event occurs in the window specified by `windowId`.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowStartDrag

```csharp
public static void WindowStartDrag(int windowId = 0)
```

引擎名：`window_start_drag`

Starts an interactive drag operation on the window with the given `windowId`, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's title bar. Using this method allows the window to participate in space switching, tiling, and other system features.

**Note:** This method is implemented on Linux (X11/Wayland), macOS, and Windows.

### WindowStartResize

```csharp
public static void WindowStartResize(DisplayServer.WindowResizeEdge edge, int windowId = 0)
```

引擎名：`window_start_resize`

Starts an interactive resize operation on the window with the given `windowId`, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's edge.

**Note:** This method is implemented on Linux (X11/Wayland), macOS, and Windows.

## 事件（信号）

### OrientationChanged

```csharp
public event DisplayServer.OrientationChangedEventHandler OrientationChanged
```

引擎名：`orientation_changed`

Emitted when the device orientation changes. `orientation` is the new orientation.

Returns `1` for portrait, `2` for landscape, and `0` if the orientation is undefined.

**Note:** This method is implemented on Android and iOS.

## 常量

### InvalidIndicatorId

```csharp
public const long InvalidIndicatorId = -1
```

The ID that refers to a nonexistent application status indicator.

### InvalidScreen

```csharp
public const long InvalidScreen = -1
```

The ID that refers to a screen that does not exist. This is returned by some `DisplayServer` methods if no screen matches the requested result.

### InvalidWindowId

```csharp
public const long InvalidWindowId = -1
```

The ID that refers to a nonexistent window. This is returned by some `DisplayServer` methods if no window matches the requested result.

### MainWindowId

```csharp
public const long MainWindowId = 0
```

The ID of the main window spawned by the engine, which can be passed to methods expecting a `window_id`.

### ScreenOfMainWindow

```csharp
public const long ScreenOfMainWindow = -1
```

Represents the screen where the main window is located. This is usually the default value in functions that allow specifying one of several screens.

**Note:** On Android, iOS, Web, and Linux (Wayland), this constant always represents the screen at index `0`.

### ScreenPrimary

```csharp
public const long ScreenPrimary = -2
```

Represents the primary screen.

**Note:** On Android, iOS, Web, and Linux (Wayland), this constant always represents the screen at index `0`.

### ScreenWithKeyboardFocus

```csharp
public const long ScreenWithKeyboardFocus = -3
```

Represents the screen containing the window with the keyboard focus.

**Note:** On Android, iOS, Web, and Linux (Wayland), this constant always represents the screen at index `0`.

### ScreenWithMouseFocus

```csharp
public const long ScreenWithMouseFocus = -4
```

Represents the screen containing the mouse pointer.

**Note:** On Android, iOS, Web, and Linux (Wayland), this constant always represents the screen at index `0`.

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
- `ScrollDown = 12`：Scroll down action, callback argument is set to [`DisplayServer.AccessibilityScrollUnit`](DisplayServer.md).
- `ScrollForward = 13`：Scroll forward action, callback argument is not set.
- `ScrollLeft = 14`：Scroll left action, callback argument is set to [`DisplayServer.AccessibilityScrollUnit`](DisplayServer.md).
- `ScrollRight = 15`：Scroll right action, callback argument is set to [`DisplayServer.AccessibilityScrollUnit`](DisplayServer.md).
- `ScrollUp = 16`：Scroll up action, callback argument is set to [`DisplayServer.AccessibilityScrollUnit`](DisplayServer.md).
- `ScrollIntoView = 17`：Scroll into view action, callback argument is set to [`DisplayServer.AccessibilityScrollHint`](DisplayServer.md).
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

## CursorShape

命名空间 `Godot`；enum

### 枚举值

- `Arrow = 0`：Arrow cursor shape.
- `Ibeam = 1`：I-beam cursor shape.
- `PointingHand = 2`：Pointing hand cursor shape.
- `Cross = 3`：Crosshair cursor.
- `Wait = 4`：Wait cursor.
- `Busy = 5`：Wait cursor.
- `Drag = 6`：Dragging hand cursor.
- `CanDrop = 7`："Can drop" cursor.
- `Forbidden = 8`：Forbidden cursor.
- `Vsize = 9`：Vertical resize cursor.
- `Hsize = 10`：Horizontal resize cursor.
- `Bdiagsize = 11`：Secondary diagonal resize cursor (top-right/bottom-left).
- `Fdiagsize = 12`：Main diagonal resize cursor (top-left/bottom-right).
- `Move = 13`：Move cursor.
- `Vsplit = 14`：Vertical split cursor.
- `Hsplit = 15`：Horizontal split cursor.
- `Help = 16`：Help cursor.
- `Max = 17`：Represents the size of the `DisplayServer.CursorShape` enum.

## Feature

命名空间 `Godot`；enum

### 枚举值

- `GlobalMenu = 0`：Display server supports global menu.
- `Subwindows = 1`：Display server supports multiple windows that can be moved outside of the main window.
- `Touchscreen = 2`：Display server supports touchscreen input.
- `Mouse = 3`：Display server supports mouse input.
- `MouseWarp = 4`：Display server supports warping mouse coordinates to keep the mouse cursor constrained within an area, but looping when one of the edges is reached.
- `Clipboard = 5`：Display server supports setting and getting clipboard data.
- `VirtualKeyboard = 6`：Display server supports popping up a virtual keyboard when requested to input text without a physical keyboard.
- `CursorShape = 7`：Display server supports setting the mouse cursor shape to be different from the default.
- `CustomCursorShape = 8`：Display server supports setting the mouse cursor shape to a custom image.
- `NativeDialog = 9`：Display server supports spawning text dialogs using the operating system's native look-and-feel.
- `Ime = 10`：Display server supports [Input Method Editor](https://en.wikipedia.org/wiki/Input_method), which is commonly used for inputting Chinese/Japanese/Korean text.
- `WindowTransparency = 11`：Display server supports windows can use per-pixel transparency to make windows behind them partially or fully visible.
- `Hidpi = 12`：Display server supports querying the operating system's display scale factor.
- `Icon = 13`：Display server supports changing the window icon (usually displayed in the top-left corner).
- `NativeIcon = 14`：Display server supports changing the window icon (usually displayed in the top-left corner).
- `Orientation = 15`：Display server supports changing the screen orientation.
- `SwapBuffers = 16`：Display server supports V-Sync status can be changed from the default (which is forced to be enabled platforms not supporting this feature).
- `ClipboardPrimary = 18`：Display server supports Primary clipboard can be used.
- `TextToSpeech = 19`：Display server supports text-to-speech.
- `ExtendToTitle = 20`：Display server supports expanding window content to the title.
- `ScreenCapture = 21`：Display server supports reading screen pixels.
- `StatusIndicator = 22`：Display server supports application status indicators.
- `NativeHelp = 23`：Display server supports native help system search callbacks.
- `NativeDialogInput = 24`：Display server supports spawning text input dialogs using the operating system's native look-and-feel.
- `NativeDialogFile = 25`：Display server supports spawning dialogs for selecting files or directories using the operating system's native look-and-feel.
- `NativeDialogFileExtra = 26`：The display server supports all features of `NativeDialogFile`, with the added functionality of Options and native dialog file access to `res://` and `user://` paths.
- `WindowDrag = 27`：The display server supports initiating window drag and resize operations on demand.
- `ScreenExcludeFromCapture = 28`：Display server supports [`DisplayServer.WindowFlags.ExcludeFromCapture`](DisplayServer.md) window flag.
- `WindowEmbedding = 29`：Display server supports embedding a window from another process.
- `NativeDialogFileMime = 30`：Native file selection dialog supports MIME types as filters.
- `EmojiAndSymbolPicker = 31`：Display server supports system emoji and symbol picker.
- `NativeColorPicker = 32`：Display server supports native color picker.
- `SelfFittingWindows = 33`：Display server automatically fits popups according to the screen boundaries.
- `AccessibilityScreenReader = 34`：Display server supports interaction with screen reader or Braille display.
- `HdrOutput = 35`：Display server supports HDR output.
- `PipMode = 36`：Display server supports putting the application in picture-in-picture mode.

## FileDialogMode

命名空间 `Godot`；enum

### 枚举值

- `OpenFile = 0`：The native file dialog allows selecting one, and only one file.
- `OpenFiles = 1`：The native file dialog allows selecting multiple files.
- `OpenDir = 2`：The native file dialog only allows selecting a directory, disallowing the selection of any file.
- `OpenAny = 3`：The native file dialog allows selecting one file or directory.
- `SaveFile = 4`：The native file dialog will warn when a file exists.

## HandleType

命名空间 `Godot`；enum

### 枚举值

- `DisplayHandle = 0`：Display handle:
- `WindowHandle = 1`：Window handle:
- `WindowView = 2`：Window view:
- `OpenglContext = 3`：OpenGL context (only with the Compatibility renderer):
- `EglDisplay = 4`：- Windows: `EGLDisplay` for the window (ANGLE).
- `EglConfig = 5`：- Windows: `EGLConfig` for the window (ANGLE).
- `GlxVisualid = 6`：The GLX `VisualID` for the window.
- `GlxFbconfig = 7`：The `GLXFBConfig` for the window.

## MouseMode

命名空间 `Godot`；enum

### 枚举值

- `Visible = 0`：Makes the mouse cursor visible if it is hidden.
- `Hidden = 1`：Makes the mouse cursor hidden if it is visible.
- `Captured = 2`：Captures the mouse.
- `Confined = 3`：Confines the mouse cursor to the game window, and make it visible.
- `ConfinedHidden = 4`：Confines the mouse cursor to the game window, and make it hidden.
- `Max = 5`：Max value of the `DisplayServer.MouseMode`.

## OrientationChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`DisplayServer.OrientationChanged`](DisplayServer.md) event of a [`DisplayServer`](DisplayServer.md) class.

### 签名

#### Invoke

```csharp
delegate void OrientationChangedEventHandler(long orientation)
```

## ProgressState

命名空间 `Godot`；enum

### 枚举值

- `Noprogress = 0`：Stops displaying progress and returns the button to its normal state.
- `Indeterminate = 1`：The progress indicator shows an indeterminate progress.
- `Normal = 2`：The progress indicator shows progress normally.
- `Error = 3`：The progress indicator shows that an error has occurred.
- `Paused = 4`：The progress indicator shows it was paused.

## ScreenOrientation

命名空间 `Godot`；enum

### 枚举值

- `Landscape = 0`：Default landscape orientation.
- `Portrait = 1`：Default portrait orientation.
- `ReverseLandscape = 2`：Reverse landscape orientation (upside down).
- `ReversePortrait = 3`：Reverse portrait orientation (upside down).
- `SensorLandscape = 4`：Automatic landscape orientation (default or reverse depending on sensor).
- `SensorPortrait = 5`：Automatic portrait orientation (default or reverse depending on sensor).
- `Sensor = 6`：Automatic landscape or portrait orientation (default or reverse depending on sensor).

## TtsUtteranceEvent

命名空间 `Godot`；enum

### 枚举值

- `Started = 0`：Utterance has begun to be spoken.
- `Ended = 1`：Utterance was successfully finished.
- `Canceled = 2`：Utterance was canceled, or TTS service was unable to process it.
- `Boundary = 3`：Utterance reached a word or sentence boundary.

## VSyncMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：No vertical synchronization, which means the engine will display frames as fast as possible (tearing may be visible).
- `Enabled = 1`：Default vertical synchronization mode, the image is displayed only on vertical blanking intervals (no tearing is visible).
- `Adaptive = 2`：Behaves like `Disabled` when the framerate drops below the screen's refresh rate to reduce stuttering (tearing may be visible).
- `Mailbox = 3`：Displays the most recent image in the queue on vertical blanking intervals, while rendering to the other images (no tearing is visible).

## VirtualKeyboardType

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Default text virtual keyboard.
- `Multiline = 1`：Multiline virtual keyboard.
- `Number = 2`：Virtual number keypad, useful for PIN entry.
- `NumberDecimal = 3`：Virtual number keypad, useful for entering fractional numbers.
- `Phone = 4`：Virtual phone number keypad.
- `EmailAddress = 5`：Virtual keyboard with additional keys to assist with typing email addresses.
- `Password = 6`：Virtual keyboard for entering a password.
- `Url = 7`：Virtual keyboard with additional keys to assist with typing URLs.

## WindowEvent

命名空间 `Godot`；enum

### 枚举值

- `MouseEnter = 0`：Sent when the mouse pointer enters the window.
- `MouseExit = 1`：Sent when the mouse pointer exits the window.
- `FocusIn = 2`：Sent when the window grabs focus.
- `FocusOut = 3`：Sent when the window loses focus.
- `CloseRequest = 4`：Sent when the user has attempted to close the window (e.g.
- `GoBackRequest = 5`：Sent when the device "Back" button is pressed.
- `DpiChange = 6`：Sent when the window is moved to the display with different DPI, or display DPI is changed.
- `TitlebarChange = 7`：Sent when the window title bar decoration is changed (e.g.
- `ForceClose = 8`：Sent when the window has been forcibly closed by the display server.
- `OutputMaxLinearValueChanged = 9`：Sent when the output max linear value returned by [`Window.GetOutputMaxLinearValue`](Window.md) has changed.

## WindowFlags

命名空间 `Godot`；enum

### 枚举值

- `ResizeDisabled = 0`：The window can't be resized by dragging its resize grip.
- `Borderless = 1`：The window do not have native title bar and other decorations.
- `AlwaysOnTop = 2`：The window is floating on top of all other windows.
- `Transparent = 3`：The window background can be transparent.
- `NoFocus = 4`：The window can't be focused.
- `Popup = 5`：Window is part of menu or [`OptionButton`](OptionButton.md) dropdown.
- `ExtendToTitle = 6`：Window content is expanded to the full size of the window.
- `MousePassthrough = 7`：All mouse events are passed to the underlying window of the same application.
- `SharpCorners = 8`：Window style is overridden, forcing sharp corners.
- `ExcludeFromCapture = 9`：Window is excluded from screenshots taken by [`DisplayServer.ScreenGetImage`](DisplayServer.md), [`DisplayServer.ScreenGetImageRect`](DisplayServer.md), and [`DisplayServer.ScreenGetPixel`](DisplayServer.md).
- `PopupWMHint = 10`：Signals the window manager that this window is supposed to be an implementation-defined "popup" (usually a floating, borderless, untileable and immovable child window).
- `MinimizeDisabled = 11`：Window minimize button is disabled.
- `MaximizeDisabled = 12`：Window maximize button is disabled.
- `Max = 13`：Represents the size of the `DisplayServer.WindowFlags` enum.

## WindowMode

命名空间 `Godot`；enum

### 枚举值

- `Windowed = 0`：Windowed mode, i.e.
- `Minimized = 1`：Minimized window mode, i.e.
- `Maximized = 2`：Maximized window mode, i.e.
- `Fullscreen = 3`：Full screen mode with full multi-window support.
- `ExclusiveFullscreen = 4`：A single window full screen mode.

## WindowResizeEdge

命名空间 `Godot`；enum

### 枚举值

- `TopLeft = 0`：Top-left edge of a window.
- `Top = 1`：Top edge of a window.
- `TopRight = 2`：Top-right edge of a window.
- `Left = 3`：Left edge of a window.
- `Right = 4`：Right edge of a window.
- `BottomLeft = 5`：Bottom-left edge of a window.
- `Bottom = 6`：Bottom edge of a window.
- `BottomRight = 7`：Bottom-right edge of a window.
- `Max = 8`：Represents the size of the `DisplayServer.WindowResizeEdge` enum.
