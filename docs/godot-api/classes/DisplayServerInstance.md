<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# DisplayServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

[`DisplayServer`](DisplayServer.md) handles everything related to window management. It is separated from [`OS`](OS.md) as a single operating system may support multiple display servers.

**Headless mode:** Starting the engine with the `--headless` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html) disables all rendering and window management functions. Most functions from [`DisplayServer`](DisplayServer.md) will return dummy values in this case.

## 方法

### AccessibilityCreateElement

```csharp
public Rid AccessibilityCreateElement(int windowId, DisplayServer.AccessibilityRole role)
```

Creates a new, empty accessibility element resource.

**Note:** An accessibility element is created and freed automatically for each [`Node`](Node.md). In general, this function should not be called manually.

### AccessibilityCreateSubElement

```csharp
public Rid AccessibilityCreateSubElement(Rid parentRid, DisplayServer.AccessibilityRole role, int insertPos = -1)
```

Creates a new, empty accessibility sub-element resource. Sub-elements can be used to provide accessibility information for objects which are not [`Node`](Node.md)s, such as list items, table cells, or menu items. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `AccessibilityFreeElement()` method.

### AccessibilityCreateSubTextEditElements

```csharp
public Rid AccessibilityCreateSubTextEditElements(Rid parentRid, Rid shapedText, float minHeight, int insertPos = -1, bool isLastLine = false)
```

Creates a new, empty accessibility sub-element from the shaped text buffer. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `AccessibilityFreeElement()` method.

If `isLastLine` is `true`, no trailing newline is appended to the text content. Set to `true` for the last line in multi-line text fields and for single-line text fields.

### AccessibilityCreateSubTextEditElements

```csharp
public Rid AccessibilityCreateSubTextEditElements(Rid parentRid, Rid shapedText, float minHeight, int insertPos)
```

Creates a new, empty accessibility sub-element from the shaped text buffer. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `AccessibilityFreeElement()` method.

If `isLastLine` is `true`, no trailing newline is appended to the text content. Set to `true` for the last line in multi-line text fields and for single-line text fields.

### AccessibilityElementGetMeta

```csharp
public Variant AccessibilityElementGetMeta(Rid id)
```

Returns the metadata of the accessibility element `id`.

### AccessibilityElementSetMeta

```csharp
public void AccessibilityElementSetMeta(Rid id, Variant meta)
```

Sets the metadata of the accessibility element `id` to `meta`.

### AccessibilityFreeElement

```csharp
public void AccessibilityFreeElement(Rid id)
```

Frees the accessibility element `id` created by `AccessibilityCreateElement()`, `AccessibilityCreateSubElement()`, or `AccessibilityCreateSubTextEditElements()`.

### AccessibilityGetWindowRoot

```csharp
public Rid AccessibilityGetWindowRoot(int windowId)
```

Returns the main accessibility element of the OS native window.

### AccessibilityHasElement

```csharp
public bool AccessibilityHasElement(Rid id)
```

Returns `true` if `id` is a valid accessibility element.

### AccessibilityScreenReaderActive

```csharp
public int AccessibilityScreenReaderActive()
```

Returns `1` if a screen reader, Braille display or other assistive app is active, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Accessibility debugging tools, such as Accessibility Insights for Windows, Accessibility Inspector (macOS), or AT-SPI Browser (Linux/BSD), do not count as assistive apps and will not affect this value. To test your project with these tools, set `ProjectSettings.accessibility/general/accessibility_support` to `1`.

### AccessibilitySetWindowFocused

```csharp
public void AccessibilitySetWindowFocused(int windowId, bool focused)
```

Sets the window focused state for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### AccessibilitySetWindowRect

```csharp
public void AccessibilitySetWindowRect(int windowId, Rect2 rectOut, Rect2 rectIn)
```

Sets window outer (with decorations) and inner (without decorations) bounds for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### AccessibilityShouldIncreaseContrast

```csharp
public int AccessibilityShouldIncreaseContrast()
```

Returns `1` if a high-contrast user interface theme should be used, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on Linux (X11/Wayland, GNOME), macOS, and Windows.

### AccessibilityShouldReduceAnimation

```csharp
public int AccessibilityShouldReduceAnimation()
```

Returns `1` if flashing, blinking, and other moving content that can cause seizures in users with photosensitive epilepsy should be disabled, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on macOS and Windows.

### AccessibilityShouldReduceTransparency

```csharp
public int AccessibilityShouldReduceTransparency()
```

Returns `1` if background images, transparency, and other features that can reduce the contrast between the foreground and background should be disabled, `0` otherwise. Returns `-1` if status is unknown.

**Note:** This method is implemented on macOS and Windows.

### AccessibilityUpdateAddAction

```csharp
public void AccessibilityUpdateAddAction(Rid id, DisplayServer.AccessibilityAction action, Callable callable)
```

Adds a callback for the accessibility action (action which can be performed by using a special screen reader command or buttons on the Braille display), and marks this action as supported. The action callback receives one [`Variant`](Variant.md) argument, which value depends on action type.

### AccessibilityUpdateAddChild

```csharp
public void AccessibilityUpdateAddChild(Rid id, Rid childId)
```

Adds a child accessibility element.

**Note:** [`Node`](Node.md) children and sub-elements are added to the child list automatically.

### AccessibilityUpdateAddCustomAction

```csharp
public void AccessibilityUpdateAddCustomAction(Rid id, int actionId, string actionDescription)
```

Adds support for a custom accessibility action. `actionId` is passed as an argument to the callback of [`DisplayServer.AccessibilityAction.Custom`](DisplayServer.md) action.

### AccessibilityUpdateAddRelatedControls

```csharp
public void AccessibilityUpdateAddRelatedControls(Rid id, Rid relatedId)
```

Adds an element that is controlled by this element.

### AccessibilityUpdateAddRelatedDescribedBy

```csharp
public void AccessibilityUpdateAddRelatedDescribedBy(Rid id, Rid relatedId)
```

Adds an element that describes this element.

### AccessibilityUpdateAddRelatedDetails

```csharp
public void AccessibilityUpdateAddRelatedDetails(Rid id, Rid relatedId)
```

Adds an element that details this element.

### AccessibilityUpdateAddRelatedFlowTo

```csharp
public void AccessibilityUpdateAddRelatedFlowTo(Rid id, Rid relatedId)
```

Adds an element that this element flow into.

### AccessibilityUpdateAddRelatedLabeledBy

```csharp
public void AccessibilityUpdateAddRelatedLabeledBy(Rid id, Rid relatedId)
```

Adds an element that labels this element.

### AccessibilityUpdateAddRelatedRadioGroup

```csharp
public void AccessibilityUpdateAddRelatedRadioGroup(Rid id, Rid relatedId)
```

Adds an element that is part of the same radio group.

**Note:** This method should be called on each element of the group, using all other elements as `relatedId`.

### AccessibilityUpdateSetActiveDescendant

```csharp
public void AccessibilityUpdateSetActiveDescendant(Rid id, Rid otherId)
```

Adds an element that is an active descendant of this element.

### AccessibilityUpdateSetBackgroundColor

```csharp
public void AccessibilityUpdateSetBackgroundColor(Rid id, Color color)
```

Sets element background color.

### AccessibilityUpdateSetBounds

```csharp
public void AccessibilityUpdateSetBounds(Rid id, Rect2 rect)
```

Sets element bounding box, relative to the node position.

### AccessibilityUpdateSetChecked

```csharp
public void AccessibilityUpdateSetChecked(Rid id, bool checekd)
```

Sets element checked state.

### AccessibilityUpdateSetClassname

```csharp
public void AccessibilityUpdateSetClassname(Rid id, string classname)
```

Sets element class name.

### AccessibilityUpdateSetColorValue

```csharp
public void AccessibilityUpdateSetColorValue(Rid id, Color color)
```

Sets element color value.

### AccessibilityUpdateSetDescription

```csharp
public void AccessibilityUpdateSetDescription(Rid id, string description)
```

Sets element accessibility description.

### AccessibilityUpdateSetErrorMessage

```csharp
public void AccessibilityUpdateSetErrorMessage(Rid id, Rid otherId)
```

Sets an element which contains an error message for this element.

### AccessibilityUpdateSetExtraInfo

```csharp
public void AccessibilityUpdateSetExtraInfo(Rid id, string name)
```

Sets element accessibility extra information added to the element name.

### AccessibilityUpdateSetFlag

```csharp
public void AccessibilityUpdateSetFlag(Rid id, DisplayServer.AccessibilityFlags flag, bool value)
```

Sets element flag.

### AccessibilityUpdateSetFocus

```csharp
public void AccessibilityUpdateSetFocus(Rid id)
```

Sets currently focused element.

### AccessibilityUpdateSetForegroundColor

```csharp
public void AccessibilityUpdateSetForegroundColor(Rid id, Color color)
```

Sets element foreground color.

### AccessibilityUpdateSetInPageLinkTarget

```csharp
public void AccessibilityUpdateSetInPageLinkTarget(Rid id, Rid otherId)
```

Sets target element for the link.

### AccessibilityUpdateSetLanguage

```csharp
public void AccessibilityUpdateSetLanguage(Rid id, string language)
```

Sets element text language.

### AccessibilityUpdateSetListItemCount

```csharp
public void AccessibilityUpdateSetListItemCount(Rid id, int size)
```

Sets number of items in the list.

### AccessibilityUpdateSetListItemExpanded

```csharp
public void AccessibilityUpdateSetListItemExpanded(Rid id, bool expanded)
```

Sets list/tree item expanded status.

### AccessibilityUpdateSetListItemIndex

```csharp
public void AccessibilityUpdateSetListItemIndex(Rid id, int index)
```

Sets the position of the element in the list.

### AccessibilityUpdateSetListItemLevel

```csharp
public void AccessibilityUpdateSetListItemLevel(Rid id, int level)
```

Sets the hierarchical level of the element in the list.

### AccessibilityUpdateSetListItemSelected

```csharp
public void AccessibilityUpdateSetListItemSelected(Rid id, bool selected)
```

Sets list/tree item selected status.

### AccessibilityUpdateSetListOrientation

```csharp
public void AccessibilityUpdateSetListOrientation(Rid id, bool vertical)
```

Sets the orientation of the list elements.

### AccessibilityUpdateSetLive

```csharp
public void AccessibilityUpdateSetLive(Rid id, DisplayServer.AccessibilityLiveMode live)
```

Sets the priority of the live region updates.

### AccessibilityUpdateSetMemberOf

```csharp
public void AccessibilityUpdateSetMemberOf(Rid id, Rid groupId)
```

Sets the element to be a member of the group.

### AccessibilityUpdateSetName

```csharp
public void AccessibilityUpdateSetName(Rid id, string name)
```

Sets element accessibility name.

### AccessibilityUpdateSetNextOnLine

```csharp
public void AccessibilityUpdateSetNextOnLine(Rid id, Rid otherId)
```

Sets next element on the line.

### AccessibilityUpdateSetNumJump

```csharp
public void AccessibilityUpdateSetNumJump(Rid id, double jump)
```

Sets numeric value jump.

### AccessibilityUpdateSetNumRange

```csharp
public void AccessibilityUpdateSetNumRange(Rid id, double min, double max)
```

Sets numeric value range.

### AccessibilityUpdateSetNumStep

```csharp
public void AccessibilityUpdateSetNumStep(Rid id, double step)
```

Sets numeric value step.

### AccessibilityUpdateSetNumValue

```csharp
public void AccessibilityUpdateSetNumValue(Rid id, double position)
```

Sets numeric value.

### AccessibilityUpdateSetPlaceholder

```csharp
public void AccessibilityUpdateSetPlaceholder(Rid id, string placeholder)
```

Sets placeholder text.

### AccessibilityUpdateSetPopupType

```csharp
public void AccessibilityUpdateSetPopupType(Rid id, DisplayServer.AccessibilityPopupType popup)
```

Sets popup type for popup buttons.

### AccessibilityUpdateSetPreviousOnLine

```csharp
public void AccessibilityUpdateSetPreviousOnLine(Rid id, Rid otherId)
```

Sets previous element on the line.

### AccessibilityUpdateSetRole

```csharp
public void AccessibilityUpdateSetRole(Rid id, DisplayServer.AccessibilityRole role)
```

Sets element accessibility role.

### AccessibilityUpdateSetRoleDescription

```csharp
public void AccessibilityUpdateSetRoleDescription(Rid id, string description)
```

Sets element accessibility role description text.

### AccessibilityUpdateSetScrollX

```csharp
public void AccessibilityUpdateSetScrollX(Rid id, double position)
```

Sets scroll bar x position.

### AccessibilityUpdateSetScrollXRange

```csharp
public void AccessibilityUpdateSetScrollXRange(Rid id, double min, double max)
```

Sets scroll bar x range.

### AccessibilityUpdateSetScrollY

```csharp
public void AccessibilityUpdateSetScrollY(Rid id, double position)
```

Sets scroll bar y position.

### AccessibilityUpdateSetScrollYRange

```csharp
public void AccessibilityUpdateSetScrollYRange(Rid id, double min, double max)
```

Sets scroll bar y range.

### AccessibilityUpdateSetShortcut

```csharp
public void AccessibilityUpdateSetShortcut(Rid id, string shortcut)
```

Sets the list of keyboard shortcuts used by element.

### AccessibilityUpdateSetStateDescription

```csharp
public void AccessibilityUpdateSetStateDescription(Rid id, string description)
```

Sets human-readable description of the current checked state.

### AccessibilityUpdateSetTableCellPosition

```csharp
public void AccessibilityUpdateSetTableCellPosition(Rid id, int rowIndex, int columnIndex)
```

Sets cell position in the table.

### AccessibilityUpdateSetTableCellSpan

```csharp
public void AccessibilityUpdateSetTableCellSpan(Rid id, int rowSpan, int columnSpan)
```

Sets cell row/column span.

### AccessibilityUpdateSetTableColumnCount

```csharp
public void AccessibilityUpdateSetTableColumnCount(Rid id, int count)
```

Sets number of columns in the table.

### AccessibilityUpdateSetTableColumnIndex

```csharp
public void AccessibilityUpdateSetTableColumnIndex(Rid id, int index)
```

Sets position of the column.

### AccessibilityUpdateSetTableRowCount

```csharp
public void AccessibilityUpdateSetTableRowCount(Rid id, int count)
```

Sets number of rows in the table.

### AccessibilityUpdateSetTableRowIndex

```csharp
public void AccessibilityUpdateSetTableRowIndex(Rid id, int index)
```

Sets position of the row in the table.

### AccessibilityUpdateSetTextAlign

```csharp
public void AccessibilityUpdateSetTextAlign(Rid id, HorizontalAlignment align)
```

Sets element text alignment.

### AccessibilityUpdateSetTextDecorations

```csharp
public void AccessibilityUpdateSetTextDecorations(Rid id, bool underline, bool strikethrough, bool overline)
```

Sets text underline/overline/strikethrough.

### AccessibilityUpdateSetTextOrientation

```csharp
public void AccessibilityUpdateSetTextOrientation(Rid id, bool vertical)
```

Sets text orientation.

### AccessibilityUpdateSetTextSelection

```csharp
public void AccessibilityUpdateSetTextSelection(Rid id, Rid textStartId, int startChar, Rid textEndId, int endChar)
```

Sets text selection to the text field. `textStartId` and `textEndId` should be elements created by `AccessibilityCreateSubTextEditElements()`. Character offsets are relative to the corresponding element.

### AccessibilityUpdateSetTooltip

```csharp
public void AccessibilityUpdateSetTooltip(Rid id, string tooltip)
```

Sets tooltip text.

### AccessibilityUpdateSetTransform

```csharp
public void AccessibilityUpdateSetTransform(Rid id, Transform2D transform)
```

Sets element 2D transform.

### AccessibilityUpdateSetUrl

```csharp
public void AccessibilityUpdateSetUrl(Rid id, string url)
```

Sets link URL.

### AccessibilityUpdateSetValue

```csharp
public void AccessibilityUpdateSetValue(Rid id, string value)
```

Sets element text value.

### Beep

```csharp
public void Beep()
```

Plays the beep sound from the operative system, if possible. Because it comes from the OS, the beep sound will be audible even if the application is muted. It may also be disabled for the entire OS by the user.

**Note:** This method is implemented on macOS, Linux (X11/Wayland), and Windows.

### ClipboardGet

```csharp
public string ClipboardGet()
```

Returns the user's clipboard as a string if possible.

### ClipboardGetImage

```csharp
public Image ClipboardGetImage()
```

Returns the user's clipboard as an image if possible.

**Note:** This method uses the copied pixel data, e.g. from an image editing software or a web browser, not an image file copied from file explorer.

### ClipboardGetPrimary

```csharp
public string ClipboardGetPrimary()
```

Returns the user's [primary](https://unix.stackexchange.com/questions/139191/whats-the-difference-between-primary-selection-and-clipboard-buffer) clipboard as a string if possible. This is the clipboard that is set when the user selects text in any application, rather than when pressing Ctrl + C. The clipboard data can then be pasted by clicking the middle mouse button in any application that supports the primary clipboard mechanism.

**Note:** This method is only implemented on Linux (X11/Wayland).

### ClipboardHas

```csharp
public bool ClipboardHas()
```

Returns `true` if there is a text content on the user's clipboard.

### ClipboardHasImage

```csharp
public bool ClipboardHasImage()
```

Returns `true` if there is an image content on the user's clipboard.

### ClipboardSet

```csharp
public void ClipboardSet(string clipboard)
```

Sets the user's clipboard content to the given string.

### ClipboardSetPrimary

```csharp
public void ClipboardSetPrimary(string clipboardPrimary)
```

Sets the user's [primary](https://unix.stackexchange.com/questions/139191/whats-the-difference-between-primary-selection-and-clipboard-buffer) clipboard content to the given string. This is the clipboard that is set when the user selects text in any application, rather than when pressing Ctrl + C. The clipboard data can then be pasted by clicking the middle mouse button in any application that supports the primary clipboard mechanism.

**Note:** This method is only implemented on Linux (X11/Wayland).

### ColorPicker

```csharp
public bool ColorPicker(Callable callback)
```

Displays OS native color picker.

Callbacks have the following arguments: `status: bool, color: Color`.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeColorPicker`](DisplayServer.md) feature.

**Note:** This method is only implemented on Linux (X11/Wayland).

### CreateStatusIndicator

```csharp
public int CreateStatusIndicator(Texture2D icon, string tooltip, Callable callback)
```

Creates a new application status indicator with the specified icon, tooltip, and activation callback.

`callback` should take two arguments: the pressed mouse button (one of the [`MouseButton`](MouseButton.md) constants) and the click position in screen coordinates (a [`Vector2I`](Vector2I.md)).

### CursorGetShape

```csharp
public DisplayServer.CursorShape CursorGetShape()
```

Returns the default mouse cursor shape set by `CursorSetShape()`.

### CursorSetCustomImage

```csharp
public void CursorSetCustomImage(Resource cursor, DisplayServer.CursorShape shape = DisplayServer.CursorShape.Arrow, Vector2? hotspot = null)
```

Sets a custom mouse cursor image for the given `shape`. This means the user's operating system and mouse cursor theme will no longer influence the mouse cursor's appearance.

`cursor` can be either a [`Texture2D`](Texture2D.md) or an [`Image`](Image.md), and it should not be larger than 256×256 to display correctly. Optionally, `hotspot` can be set to offset the image's position relative to the click point. By default, `hotspot` is set to the top-left corner of the image. See also `CursorSetShape()`.

**Note:** On Web, calling this method every frame can cause the cursor to flicker.

参数：
- `hotspot`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### CursorSetShape

```csharp
public void CursorSetShape(DisplayServer.CursorShape shape)
```

Sets the default mouse cursor shape. The cursor's appearance will vary depending on the user's operating system and mouse cursor theme. See also `CursorGetShape()` and `CursorSetCustomImage()`.

### DeleteStatusIndicator

```csharp
public void DeleteStatusIndicator(int id)
```

Removes the application status indicator.

### DialogInputText

```csharp
public Error DialogInputText(string title, string description, string existingText, Callable callback)
```

Shows a text input dialog which uses the operating system's native look-and-feel. `callback` should accept a single `System.String` parameter which contains the text field's contents.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialogInput`](DisplayServer.md) feature. Supported platforms include macOS, Windows, and Android.

### DialogShow

```csharp
public Error DialogShow(string title, string description, ReadOnlySpan<string> buttons, Callable callback)
```

Shows a text dialog which uses the operating system's native look-and-feel. `callback` should accept a single `System.Int32` parameter which corresponds to the index of the pressed button.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialog`](DisplayServer.md) feature. Supported platforms include macOS, Windows, and Android.

### DialogShow

```csharp
public Error DialogShow(string title, string description, string[] buttons, Callable callback)
```

Shows a text dialog which uses the operating system's native look-and-feel. `callback` should accept a single `System.Int32` parameter which corresponds to the index of the pressed button.

**Note:** This method is implemented if the display server has the [`DisplayServer.Feature.NativeDialog`](DisplayServer.md) feature. Supported platforms include macOS, Windows, and Android.

### EnableForStealingFocus

```csharp
public void EnableForStealingFocus(long processId)
```

Allows the `processId` PID to steal focus from this window. In other words, this disables the operating system's focus stealing protection for the specified PID.

**Note:** This method is implemented only on Windows.

### FileDialogShow

```csharp
public Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Callable callback)
```

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
public Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Callable callback, int parentWindowId)
```

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
public Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Callable callback)
```

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
public Error FileDialogShow(string title, string currentDirectory, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Callable callback, int parentWindowId = 0)
```

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
public Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Array<Dictionary> options, Callable callback)
```

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
public Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, ReadOnlySpan<string> filters, Array<Dictionary> options, Callable callback, int parentWindowId)
```

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
public Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Array<Dictionary> options, Callable callback)
```

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
public Error FileDialogWithOptionsShow(string title, string currentDirectory, string root, string fileName, bool showHidden, DisplayServer.FileDialogMode mode, string[] filters, Array<Dictionary> options, Callable callback, int parentWindowId = 0)
```

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
public void ForceProcessAndDropEvents()
```

Forces window manager processing while ignoring all [`InputEvent`](InputEvent.md)s. See also `ProcessEvents()`.

**Note:** This method is implemented on Windows and macOS.

### GetAccentColor

```csharp
public Color GetAccentColor()
```

Returns OS theme accent color. Returns `Color(0, 0, 0, 0)`, if accent color is unknown.

**Note:** This method is implemented on macOS, Windows, Android, and Linux (X11/Wayland).

### GetBaseColor

```csharp
public Color GetBaseColor()
```

Returns the OS theme base color (default control background). Returns `Color(0, 0, 0, 0)` if the base color is unknown.

**Note:** This method is implemented on macOS, Windows, and Android.

### GetDisplayCutouts

```csharp
public Array<Rect2> GetDisplayCutouts()
```

Returns an [`Collections.Array`](Collections.Array.md) of [`Rect2`](Rect2.md), each of which is the bounding rectangle for a display cutout or notch. These are non-functional areas on edge-to-edge screens used by cameras and sensors. Returns an empty array if the device does not have cutouts. See also `GetDisplaySafeArea()`.

**Note:** Currently only implemented on Android. Other platforms will return an empty array even if they do have display cutouts or notches.

### GetDisplaySafeArea

```csharp
public Rect2I GetDisplaySafeArea()
```

Returns the unobscured area of the display where interactive controls should be rendered. See also `GetDisplayCutouts()`.

**Note:** Currently only implemented on Android and iOS. On other platforms, `screen_get_usable_rect(SCREEN_OF_MAIN_WINDOW)` will be returned as a fallback. See also `ScreenGetUsableRect()`.

### GetKeyboardFocusScreen

```csharp
public int GetKeyboardFocusScreen()
```

Returns the index of the screen containing the window with the keyboard focus, or the primary screen if there's no focused window.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns the primary screen.

### GetName

```csharp
public string GetName()
```

Returns the name of the [`DisplayServer`](DisplayServer.md) currently in use. Most operating systems only have a single [`DisplayServer`](DisplayServer.md), but Linux has access to more than one [`DisplayServer`](DisplayServer.md) (currently X11 and Wayland).

The names of built-in display servers are `Windows`, `macOS`, `X11` (Linux), `Wayland` (Linux), `Android`, `iOS`, `web` (HTML5), and `headless` (when started with the `--headless` [command line argument]($DOCS_URL/tutorials/editor/command_line_tutorial.html)).

### GetPrimaryScreen

```csharp
public int GetPrimaryScreen()
```

Returns the index of the primary screen.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns `0`.

### GetScreenCount

```csharp
public int GetScreenCount()
```

Returns the number of displays available.

**Note:** This method is implemented on Linux (X11 and Wayland), macOS, and Windows. On other platforms, this method always returns `1`.

### GetScreenFromRect

```csharp
public int GetScreenFromRect(Rect2 rect)
```

Returns the index of the screen that overlaps the most with the given rectangle. Returns [`DisplayServer.InvalidScreen`](DisplayServer.md) if the rectangle doesn't overlap with any screen or has no area.

### GetSwapCancelOk

```csharp
public bool GetSwapCancelOk()
```

Returns `true` if positions of **OK** and **Cancel** buttons are swapped in dialogs. This is enabled by default on Windows to follow interface conventions, and be toggled by changing `ProjectSettings.gui/common/swap_cancel_ok`.

**Note:** This doesn't affect native dialogs such as the ones spawned by [`DisplayServer.DialogShow`](DisplayServer.md).

### GetWindowAtScreenPosition

```csharp
public int GetWindowAtScreenPosition(Vector2I position)
```

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
public int[] GetWindowList()
```

Returns the list of Godot window IDs belonging to this process.

**Note:** Native dialogs are not included in this list.

### GlobalMenuAddCheckItem

```csharp
public int GlobalMenuAddCheckItem(string menuRoot, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddIconCheckItem(string menuRoot, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddIconItem(string menuRoot, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddIconRadioCheckItem(string menuRoot, Texture2D icon, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddItem(string menuRoot, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddMultistateItem(string menuRoot, string label, int maxStates, int defaultState, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddRadioCheckItem(string menuRoot, string label, Callable callback = default, Callable keyCallback = default, Variant tag = default, Key accelerator = Key.None, int index = -1)
```

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
public int GlobalMenuAddSeparator(string menuRoot, int index = -1)
```

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
public int GlobalMenuAddSubmenuItem(string menuRoot, string label, string submenu, int index = -1)
```

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
public void GlobalMenuClear(string menuRoot)
```

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
public Key GlobalMenuGetItemAccelerator(string menuRoot, int idx)
```

Returns the accelerator of the item at index `idx`. Accelerators are special combinations of keys that activate the item, no matter which control is focused.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemCallback

```csharp
public Callable GlobalMenuGetItemCallback(string menuRoot, int idx)
```

Returns the callback of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemCount

```csharp
public int GlobalMenuGetItemCount(string menuRoot)
```

Returns number of items in the global menu with ID `menuRoot`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIcon

```csharp
public Texture2D GlobalMenuGetItemIcon(string menuRoot, int idx)
```

Returns the icon of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIndentationLevel

```csharp
public int GlobalMenuGetItemIndentationLevel(string menuRoot, int idx)
```

Returns the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIndexFromTag

```csharp
public int GlobalMenuGetItemIndexFromTag(string menuRoot, Variant tag)
```

Returns the index of the item with the specified `tag`. Indices are automatically assigned to each item by the engine, and cannot be set manually.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemIndexFromText

```csharp
public int GlobalMenuGetItemIndexFromText(string menuRoot, string text)
```

Returns the index of the item with the specified `text`. Indices are automatically assigned to each item by the engine, and cannot be set manually.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemKeyCallback

```csharp
public Callable GlobalMenuGetItemKeyCallback(string menuRoot, int idx)
```

Returns the callback of the item accelerator at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemMaxStates

```csharp
public int GlobalMenuGetItemMaxStates(string menuRoot, int idx)
```

Returns number of states of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemState

```csharp
public int GlobalMenuGetItemState(string menuRoot, int idx)
```

Returns the state of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemSubmenu

```csharp
public string GlobalMenuGetItemSubmenu(string menuRoot, int idx)
```

Returns the submenu ID of the item at index `idx`. See `GlobalMenuAddSubmenuItem()` for more info on how to add a submenu.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemTag

```csharp
public Variant GlobalMenuGetItemTag(string menuRoot, int idx)
```

Returns the metadata of the specified item, which might be of any type. You can set it with `GlobalMenuSetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemText

```csharp
public string GlobalMenuGetItemText(string menuRoot, int idx)
```

Returns the text of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetItemTooltip

```csharp
public string GlobalMenuGetItemTooltip(string menuRoot, int idx)
```

Returns the tooltip associated with the specified index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuGetSystemMenuRoots

```csharp
public Dictionary GlobalMenuGetSystemMenuRoots()
```

Returns Dictionary of supported system menu IDs and names.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemCheckable

```csharp
public bool GlobalMenuIsItemCheckable(string menuRoot, int idx)
```

Returns `true` if the item at index `idx` is checkable in some way, i.e. if it has a checkbox or radio button.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemChecked

```csharp
public bool GlobalMenuIsItemChecked(string menuRoot, int idx)
```

Returns `true` if the item at index `idx` is checked.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemDisabled

```csharp
public bool GlobalMenuIsItemDisabled(string menuRoot, int idx)
```

Returns `true` if the item at index `idx` is disabled. When it is disabled it can't be selected, or its action invoked.

See `GlobalMenuSetItemDisabled()` for more info on how to disable an item.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemHidden

```csharp
public bool GlobalMenuIsItemHidden(string menuRoot, int idx)
```

Returns `true` if the item at index `idx` is hidden.

See `GlobalMenuSetItemHidden()` for more info on how to hide an item.

**Note:** This method is implemented only on macOS.

### GlobalMenuIsItemRadioCheckable

```csharp
public bool GlobalMenuIsItemRadioCheckable(string menuRoot, int idx)
```

Returns `true` if the item at index `idx` has radio button-style checkability.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented only on macOS.

### GlobalMenuRemoveItem

```csharp
public void GlobalMenuRemoveItem(string menuRoot, int idx)
```

Removes the item at index `idx` from the global menu `menuRoot`.

**Note:** The indices of items after the removed item will be shifted by one.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemAccelerator

```csharp
public void GlobalMenuSetItemAccelerator(string menuRoot, int idx, Key keycode)
```

Sets the accelerator of the item at index `idx`. `keycode` can be a single [`Key`](Key.md), or a combination of [`KeyModifierMask`](KeyModifierMask.md)s and [`Key`](Key.md)s using bitwise OR such as `KEY_MASK_CTRL | KEY_A` (Ctrl + A).

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemCallback

```csharp
public void GlobalMenuSetItemCallback(string menuRoot, int idx, Callable callback)
```

Sets the callback of the item at index `idx`. Callback is emitted when an item is pressed.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemCheckable

```csharp
public void GlobalMenuSetItemCheckable(string menuRoot, int idx, bool checkable)
```

Sets whether the item at index `idx` has a checkbox. If `false`, sets the type of the item to plain text.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemChecked

```csharp
public void GlobalMenuSetItemChecked(string menuRoot, int idx, bool checked)
```

Sets the checkstate status of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemDisabled

```csharp
public void GlobalMenuSetItemDisabled(string menuRoot, int idx, bool disabled)
```

Enables/disables the item at index `idx`. When it is disabled, it can't be selected and its action can't be invoked.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemHidden

```csharp
public void GlobalMenuSetItemHidden(string menuRoot, int idx, bool hidden)
```

Hides/shows the item at index `idx`. When it is hidden, an item does not appear in a menu and its action cannot be invoked.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemHoverCallbacks

```csharp
public void GlobalMenuSetItemHoverCallbacks(string menuRoot, int idx, Callable callback)
```

Sets the callback of the item at index `idx`. The callback is emitted when an item is hovered.

**Note:** The `callback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemIcon

```csharp
public void GlobalMenuSetItemIcon(string menuRoot, int idx, Texture2D icon)
```

Replaces the [`Texture2D`](Texture2D.md) icon of the specified `idx`.

**Note:** This method is implemented only on macOS.

**Note:** This method is not supported by macOS "_dock" menu items.

### GlobalMenuSetItemIndentationLevel

```csharp
public void GlobalMenuSetItemIndentationLevel(string menuRoot, int idx, int level)
```

Sets the horizontal offset of the item at the given `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemKeyCallback

```csharp
public void GlobalMenuSetItemKeyCallback(string menuRoot, int idx, Callable keyCallback)
```

Sets the callback of the item at index `idx`. Callback is emitted when its accelerator is activated.

**Note:** The `keyCallback` Callable needs to accept exactly one Variant parameter, the parameter passed to the Callable will be the value passed to the `tag` parameter when the menu item was created.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemMaxStates

```csharp
public void GlobalMenuSetItemMaxStates(string menuRoot, int idx, int maxStates)
```

Sets number of state of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemRadioCheckable

```csharp
public void GlobalMenuSetItemRadioCheckable(string menuRoot, int idx, bool checkable)
```

Sets the type of the item at the specified index `idx` to radio button. If `false`, sets the type of the item to plain text.

**Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemState

```csharp
public void GlobalMenuSetItemState(string menuRoot, int idx, int state)
```

Sets the state of a multistate item. See `GlobalMenuAddMultistateItem()` for details.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemSubmenu

```csharp
public void GlobalMenuSetItemSubmenu(string menuRoot, int idx, string submenu)
```

Sets the submenu of the item at index `idx`. The submenu is the ID of a global menu root that would be shown when the item is clicked.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemTag

```csharp
public void GlobalMenuSetItemTag(string menuRoot, int idx, Variant tag)
```

Sets the metadata of an item, which may be of any type. You can later get it with `GlobalMenuGetItemTag()`, which provides a simple way of assigning context data to items.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemText

```csharp
public void GlobalMenuSetItemText(string menuRoot, int idx, string text)
```

Sets the text of the item at index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetItemTooltip

```csharp
public void GlobalMenuSetItemTooltip(string menuRoot, int idx, string tooltip)
```

Sets the `System.String` tooltip of the item at the specified index `idx`.

**Note:** This method is implemented only on macOS.

### GlobalMenuSetPopupCallbacks

```csharp
public void GlobalMenuSetPopupCallbacks(string menuRoot, Callable openCallback, Callable closeCallback)
```

Registers callables to emit when the menu is respectively about to show or closed. Callback methods should have zero arguments.

### HasAdditionalOutputs

```csharp
public bool HasAdditionalOutputs()
```

Returns `true` if any additional outputs have been registered via `RegisterAdditionalOutput()`.

### HasFeature

```csharp
public bool HasFeature(DisplayServer.Feature feature)
```

Returns `true` if the specified `feature` is supported by the current [`DisplayServer`](DisplayServer.md), `false` otherwise.

### HasHardwareKeyboard

```csharp
public bool HasHardwareKeyboard()
```

Returns `true` if a hardware keyboard is connected.

**Note:** This method is implemented on Android and iOS. On other platforms, this method always returns `true`.

### HelpSetSearchCallbacks

```csharp
public void HelpSetSearchCallbacks(Callable searchCallback, Callable actionCallback)
```

Sets native help system search callbacks.

`searchCallback` has the following arguments: `String search_string, int result_limit` and return a [`Collections.Dictionary`](Collections.Dictionary.md) with "key, display name" pairs for the search results. Called when the user enters search terms in the `Help` menu.

`actionCallback` has the following arguments: `String key`. Called when the user selects a search result in the `Help` menu.

**Note:** This method is implemented only on macOS.

### ImeGetSelection

```csharp
public Vector2I ImeGetSelection()
```

Returns the text selection in the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) composition string, with the [`Vector2I`](Vector2I.md)'s `x` component being the caret position and `y` being the length of the selection.

**Note:** This method is implemented only on macOS.

### ImeGetText

```csharp
public string ImeGetText()
```

Returns the composition string contained within the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) window.

**Note:** This method is implemented only on macOS.

### IsDarkMode

```csharp
public bool IsDarkMode()
```

Returns `true` if OS is using dark mode.

**Note:** This method is implemented on Android, iOS, macOS, Windows, and Linux (X11/Wayland).

### IsDarkModeSupported

```csharp
public bool IsDarkModeSupported()
```

Returns `true` if OS supports dark mode.

**Note:** This method is implemented on Android, iOS, macOS, Windows, and Linux (X11/Wayland).

### IsInPipMode

```csharp
public bool IsInPipMode(int windowId = 0)
```

Returns `true` if the application is in picture-in-picture mode.

**Note:** This method is implemented on Android.

### IsTouchscreenAvailable

```csharp
public bool IsTouchscreenAvailable()
```

Returns `true` if touch events are available (Android or iOS), the capability is detected on the Web platform or if `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse` is `true`.

### IsWindowTransparencyAvailable

```csharp
public bool IsWindowTransparencyAvailable()
```

Returns `true` if the window background can be made transparent. This method returns `false` if `ProjectSettings.display/window/per_pixel_transparency/allowed` is set to `false`, or if transparency is not supported by the renderer or OS compositor.

### KeyboardGetCurrentLayout

```csharp
public int KeyboardGetCurrentLayout()
```

Returns active keyboard layout index.

**Note:** This method is implemented on Linux (X11/Wayland), macOS, and Windows.

### KeyboardGetKeycodeFromPhysical

```csharp
public Key KeyboardGetKeycodeFromPhysical(Key keycode)
```

Converts a physical (US QWERTY) `keycode` to one in the active keyboard layout.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLabelFromPhysical

```csharp
public Key KeyboardGetLabelFromPhysical(Key keycode)
```

Converts a physical (US QWERTY) `keycode` to localized label printed on the key in the active keyboard layout.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLayoutCount

```csharp
public int KeyboardGetLayoutCount()
```

Returns the number of keyboard layouts.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLayoutLanguage

```csharp
public string KeyboardGetLayoutLanguage(int index)
```

Returns the ISO-639/BCP-47 language code of the keyboard layout at position `index`.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardGetLayoutName

```csharp
public string KeyboardGetLayoutName(int index)
```

Returns the localized name of the keyboard layout at position `index`.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### KeyboardSetCurrentLayout

```csharp
public void KeyboardSetCurrentLayout(int index)
```

Sets the active keyboard layout.

**Note:** This method is implemented on Linux (X11/Wayland), macOS and Windows.

### MouseGetButtonState

```csharp
public MouseButtonMask MouseGetButtonState()
```

Returns the current state of mouse buttons (whether each button is pressed) as a bitmask. If multiple mouse buttons are pressed at the same time, the bits are added together. Equivalent to [`Input.GetMouseButtonMask`](Input.md).

### MouseGetMode

```csharp
public DisplayServer.MouseMode MouseGetMode()
```

Returns the current mouse mode. See also `MouseSetMode()`.

### MouseGetPosition

```csharp
public Vector2I MouseGetPosition()
```

Returns the mouse cursor's current position in screen coordinates.

### MouseSetMode

```csharp
public void MouseSetMode(DisplayServer.MouseMode mouseMode)
```

Sets the current mouse mode. See also `MouseGetMode()`.

### PipModeEnter

```csharp
public void PipModeEnter(int windowId = 0)
```

Enters picture-in-picture mode.

**Note:** This method is implemented on Android.

### PipModeSetAspectRatio

```csharp
public void PipModeSetAspectRatio(int numerator, int denominator, int windowId = 0)
```

Specifies the aspect ratio for picture-in-picture mode.

**Note:** This method is implemented on Android.

### PipModeSetAutoEnterOnBackground

```csharp
public void PipModeSetAutoEnterOnBackground(bool autoEnterOnBackground, int windowId = 0)
```

Specifies whether picture-in-picture mode should be entered automatically when the application goes in the background.

**Note:** This method is implemented on Android.

### ProcessEvents

```csharp
public void ProcessEvents()
```

Perform window manager processing, including input flushing. See also `ForceProcessAndDropEvents()`, [`Input.FlushBufferedEvents`](Input.md) and [`Input.UseAccumulatedInput`](Input.md).

### RegisterAdditionalOutput

```csharp
public void RegisterAdditionalOutput(GodotObject object)
```

Registers an [`GodotObject`](GodotObject.md) which represents an additional output that will be rendered too, beyond normal windows. The [`GodotObject`](GodotObject.md) is only used as an identifier, which can be later passed to `UnregisterAdditionalOutput()`.

This can be used to prevent Godot from skipping rendering when no normal windows are visible.

### ScreenGetDpi

```csharp
public int ScreenGetDpi(int screen = -1)
```

Returns the dots per inch density of the specified screen. Returns platform specific default value if `screen` is invalid.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

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
public Image ScreenGetImage(int screen = -1)
```

Returns a screenshot of the `screen`. Returns `null` if `screen` is invalid or the [`DisplayServer`](DisplayServer.md) fails to capture screenshot.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** This method is implemented on Linux (X11, excluding XWayland), macOS, and Windows. On other platforms, this method always returns `null`.

**Note:** On macOS, this method requires the "Screen Recording" permission. If permission is not granted, this method returns a screenshot that will not include other application windows or OS elements not related to the application.

### ScreenGetImageRect

```csharp
public Image ScreenGetImageRect(Rect2I rect)
```

Returns a screenshot of the screen region defined by `rect`. Returns `null` if `rect` is outside screen bounds or the [`DisplayServer`](DisplayServer.md) fails to capture screenshot.

**Note:** This method is implemented on macOS and Windows. On other platforms, this method always returns `null`.

**Note:** On macOS, this method requires the "Screen Recording" permission. If permission is not granted, this method returns a screenshot that will not include other application windows or OS elements not related to the application.

### ScreenGetMaxScale

```csharp
public float ScreenGetMaxScale()
```

Returns the greatest scale factor of all screens. See also `ScreenGetScale()`.

**Note:** On macOS, the returned value is `2.0` if there is at least one hiDPI (Retina) screen in the system, and `1.0` in all other cases.

### ScreenGetOrientation

```csharp
public DisplayServer.ScreenOrientation ScreenGetOrientation(int screen = -1)
```

Returns the `screen`'s current orientation. See also `ScreenSetOrientation()`. Returns [`DisplayServer.ScreenOrientation.Landscape`](DisplayServer.md) if `screen` is invalid.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** This method is implemented on Android and iOS. On other platforms, this method always returns [`DisplayServer.ScreenOrientation.Landscape`](DisplayServer.md).

### ScreenGetPixel

```csharp
public Color ScreenGetPixel(Vector2I position)
```

Returns the color of the pixel at the given screen `position`. On multi-monitor setups, the screen position is relative to the virtual desktop area.

**Note:** This method is implemented on Linux (X11, excluding XWayland), macOS, and Windows. On other platforms, this method always returns `Color(0, 0, 0, 1)`.

**Note:** On macOS, this method requires the "Screen Recording" permission. If permission is not granted, this method returns a color from a screenshot that will not include other application windows or OS elements not related to the application.

### ScreenGetPosition

```csharp
public Vector2I ScreenGetPosition(int screen = -1)
```

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

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

### ScreenGetRefreshRate

```csharp
public float ScreenGetRefreshRate(int screen = -1)
```

Returns the current refresh rate of the specified screen. When V-Sync is enabled, this returns the maximum framerate the project can effectively reach. Returns `-1.0` if `screen` is invalid or the [`DisplayServer`](DisplayServer.md) fails to find the refresh rate for the specified screen.

To fallback to a default refresh rate if the method fails, try:

```csharp
var refresh_rate = DisplayServer.screen_get_refresh_rate()
if refresh_rate < 0:
refresh_rate = 60.0

```

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** This method is implemented on Android, iOS, macOS, Linux (X11 and Wayland), and Windows. On other platforms, this method always returns `-1.0`.

### ScreenGetScale

```csharp
public float ScreenGetScale(int screen = -1)
```

Returns the scale factor of the specified screen by index. Returns `1.0` if `screen` is invalid. See also `ScreenGetMaxScale()`.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** On macOS, the returned value is `2.0` for hiDPI (Retina) screens, and `1.0` for all other cases.

**Note:** On Linux (Wayland), the returned value is accurate only when `screen` is [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md). Due to API limitations, passing a direct index will return a rounded-up integer, if the screen has a fractional scale (e.g. `1.25` would get rounded up to `2.0`).

**Note:** This method is implemented on Android, iOS, Web, macOS, and Linux (Wayland). On other platforms, this method always returns `1.0`.

### ScreenGetSize

```csharp
public Vector2I ScreenGetSize(int screen = -1)
```

Returns the screen's size in pixels. See also `ScreenGetPosition()` and `ScreenGetUsableRect()`. Returns `Vector2i.ZERO` if `screen` is invalid.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

### ScreenGetUsableRect

```csharp
public Rect2I ScreenGetUsableRect(int screen = -1)
```

Returns the portion of the screen that is not obstructed by a status bar in pixels. See also `ScreenGetSize()`.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns `Rect2i(screen_get_position(screen), screen_get_size(screen))`.

### ScreenIsKeptOn

```csharp
public bool ScreenIsKeptOn()
```

Returns `true` if the screen should never be turned off by the operating system's power-saving measures. See also `ScreenSetKeepOn()`.

### ScreenSetKeepOn

```csharp
public void ScreenSetKeepOn(bool enable)
```

Sets whether the screen should never be turned off by the operating system's power-saving measures. See also `ScreenIsKeptOn()`.

### ScreenSetOrientation

```csharp
public void ScreenSetOrientation(DisplayServer.ScreenOrientation orientation, int screen = -1)
```

Sets the `screen`'s `orientation`. See also `ScreenGetOrientation()`.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** This method is implemented on Android and iOS.

**Note:** On iOS, this method has no effect if `ProjectSettings.display/window/handheld/orientation` is not set to [`DisplayServer.ScreenOrientation.Sensor`](DisplayServer.md).

### SetHardwareKeyboardConnectionChangeCallback

```csharp
public void SetHardwareKeyboardConnectionChangeCallback(Callable callable)
```

Sets the callback that should be called when a hardware keyboard is connected or disconnected. `callable` should accept a single `System.Boolean` argument indicating whether the keyboard has been connected (`true`) or disconnected (`false`).

**Note:** This method is only implemented on Android.

### SetIcon

```csharp
public void SetIcon(Image image)
```

Sets the application icon and icons of all windows with an [`Image`](Image.md). To use icons in the operating system's native format, use `SetNativeIcon()` instead.

**Note:** Requires support for [`DisplayServer.Feature.Icon`](DisplayServer.md).

### SetNativeIcon

```csharp
public void SetNativeIcon(string fileName)
```

Sets the window icon (usually displayed in the top-left corner) in the operating system's *native* format. The file at `fileName` must be in `.ico` format on Windows or `.icns` on macOS. By using specially crafted `.ico` or `.icns` icons, `SetNativeIcon()` allows specifying different icons depending on the size the icon is displayed at. This size is determined by the operating system and user preferences (including the display scale factor). To use icons in other formats, use `SetIcon()` instead.

**Note:** Requires support for [`DisplayServer.Feature.NativeIcon`](DisplayServer.md).

### SetSystemThemeChangeCallback

```csharp
public void SetSystemThemeChangeCallback(Callable callable)
```

Sets the callback that should be called when the system's theme settings are changed. `callable` should accept zero arguments.

**Note:** This method is implemented on Android, iOS, macOS, Windows, and Linux (X11/Wayland).

### ShowEmojiAndSymbolPicker

```csharp
public void ShowEmojiAndSymbolPicker()
```

Opens system emoji and symbol picker.

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorGetRect

```csharp
public Rect2 StatusIndicatorGetRect(int id)
```

Returns the rectangle for the given status indicator `id` in screen coordinates. If the status indicator is not visible, returns an empty [`Rect2`](Rect2.md).

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorSetCallback

```csharp
public void StatusIndicatorSetCallback(int id, Callable callback)
```

Sets the application status indicator activation callback. `callback` should take two arguments: `System.Int32` mouse button index (one of [`MouseButton`](MouseButton.md) values) and [`Vector2I`](Vector2I.md) click position in screen coordinates.

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorSetIcon

```csharp
public void StatusIndicatorSetIcon(int id, Texture2D icon)
```

Sets the application status indicator icon.

**Note:** This method is implemented on macOS and Windows.

### StatusIndicatorSetMenu

```csharp
public void StatusIndicatorSetMenu(int id, Rid menuRid)
```

Sets the application status indicator native popup menu.

**Note:** On macOS, the menu is activated by any mouse button. Its activation callback is *not* triggered.

**Note:** On Windows, the menu is activated by the right mouse button, selecting the status icon and pressing Shift + F10, or the applications key. The menu's activation callback for the other mouse buttons is still triggered.

**Note:** Native popup is only supported if [`NativeMenu`](NativeMenu.md) supports the [`NativeMenu.Feature.PopupMenu`](NativeMenu.md) feature.

### StatusIndicatorSetTooltip

```csharp
public void StatusIndicatorSetTooltip(int id, string tooltip)
```

Sets the application status indicator tooltip.

**Note:** This method is implemented on macOS and Windows.

### TabletGetCurrentDriver

```csharp
public string TabletGetCurrentDriver()
```

Returns current active tablet driver name.

**Note:** This method is implemented only on Windows.

### TabletGetDriverCount

```csharp
public int TabletGetDriverCount()
```

Returns the total number of available tablet drivers.

**Note:** This method is implemented only on Windows.

### TabletGetDriverName

```csharp
public string TabletGetDriverName(int idx)
```

Returns the tablet driver name for the given index.

**Note:** This method is implemented only on Windows.

### TabletSetCurrentDriver

```csharp
public void TabletSetCurrentDriver(string name)
```

Set active tablet driver name.

Supported drivers:

- `winink`: Windows Ink API, default.

- `wintab`: Wacom Wintab API (compatible device driver required).

- `dummy`: Dummy driver, tablet input is disabled.

**Note:** This method is implemented only on Windows.

### TtsGetVoices

```csharp
public Array<Dictionary> TtsGetVoices()
```

Returns an [`Collections.Array`](Collections.Array.md) of voice information dictionaries.

Each [`Collections.Dictionary`](Collections.Dictionary.md) contains two `System.String` entries:

- `name` is voice name.

- `id` is voice identifier.

- `language` is language code in `lang_Variant` format. The `lang` part is a 2 or 3-letter code based on the ISO-639 standard, in lowercase. The `Variant` part is an engine-dependent string describing country, region or/and dialect.

Note that Godot depends on system libraries for text-to-speech functionality. These libraries are installed by default on Windows and macOS, but not on all Linux distributions. If they are not present, this method will return an empty list. This applies to both Godot users on Linux, as well as end-users on Linux running Godot games that use text-to-speech.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsGetVoicesForLanguage

```csharp
public string[] TtsGetVoicesForLanguage(string language)
```

Returns a `System.String`[] of voice identifiers for the `language`.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsIsPaused

```csharp
public bool TtsIsPaused()
```

Returns `true` if the synthesizer is in a paused state.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsIsSpeaking

```csharp
public bool TtsIsSpeaking()
```

Returns `true` if the synthesizer is generating speech, or have utterance waiting in the queue.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsPause

```csharp
public void TtsPause()
```

Puts the synthesizer into a paused state.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsResume

```csharp
public void TtsResume()
```

Resumes the synthesizer if it was paused.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsSetUtteranceCallback

```csharp
public void TtsSetUtteranceCallback(DisplayServer.TtsUtteranceEvent event, Callable callable)
```

Adds a callback, which is called when the utterance has started, finished, canceled or reached a text boundary.

- [`DisplayServer.TtsUtteranceEvent.Started`](DisplayServer.md), [`DisplayServer.TtsUtteranceEvent.Ended`](DisplayServer.md), and [`DisplayServer.TtsUtteranceEvent.Canceled`](DisplayServer.md) callable's method should take one `System.Int32` parameter, the utterance ID.

- [`DisplayServer.TtsUtteranceEvent.Boundary`](DisplayServer.md) callable's method should take two `System.Int32` parameters, the index of the character and the utterance ID.

**Note:** The granularity of the boundary callbacks is engine dependent.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### TtsSpeak

```csharp
public void TtsSpeak(string text, string voice, int volume = 50, float pitch = 1f, float rate = 1f, long utteranceId = 0, bool interrupt = false)
```

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
public void TtsStop()
```

Stops synthesis in progress and removes all utterances from the queue.

**Note:** This method is implemented on Android, iOS, Web, Linux (X11/Wayland), macOS, and Windows.

### UnregisterAdditionalOutput

```csharp
public void UnregisterAdditionalOutput(GodotObject object)
```

Unregisters an [`GodotObject`](GodotObject.md) representing an additional output, that was registered via `RegisterAdditionalOutput()`.

### VirtualKeyboardGetHeight

```csharp
public int VirtualKeyboardGetHeight()
```

Returns the on-screen keyboard's height in pixels. Returns `0` if there is no keyboard or if it is currently hidden.

**Note:** On Android 7 and 8, the keyboard height may return `0` the first time the keyboard is opened in non-immersive mode. This behavior does not occur in immersive mode.

### VirtualKeyboardHide

```csharp
public void VirtualKeyboardHide()
```

Hides the virtual keyboard if it is shown, does nothing otherwise.

### VirtualKeyboardShow

```csharp
public void VirtualKeyboardShow(string existingText, Rect2? position = null, DisplayServer.VirtualKeyboardType type = DisplayServer.VirtualKeyboardType.Default, int maxLength = -1, int cursorStart = -1, int cursorEnd = -1)
```

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
public void WarpMouse(Vector2I position)
```

Sets the mouse cursor position to the given `position` relative to an origin at the upper left corner of the currently focused game Window Manager window.

**Note:** `WarpMouse()` is only supported on Windows, macOS, and Linux (X11/Wayland). It has no effect on Android, iOS, and Web.

### WindowCanDraw

```csharp
public bool WindowCanDraw(int windowId = 0)
```

Returns `true` if anything can be drawn in the window specified by `windowId`, `false` otherwise. Using the `--disable-render-loop` command line argument or a headless build will return `false`.

### WindowGetActivePopup

```csharp
public int WindowGetActivePopup()
```

Returns ID of the active popup window, or [`DisplayServer.InvalidWindowId`](DisplayServer.md) if there is none.

### WindowGetAttachedInstanceId

```csharp
public ulong WindowGetAttachedInstanceId(int windowId = 0)
```

Returns the [`GodotObject.GetInstanceId`](GodotObject.md) of the [`Window`](Window.md) the `windowId` is attached to.

### WindowGetCurrentScreen

```csharp
public int WindowGetCurrentScreen(int windowId = 0)
```

Returns the screen the window specified by `windowId` is currently positioned on. If the screen overlaps multiple displays, the screen where the window's center is located is returned. See also `WindowSetCurrentScreen()`. Returns [`DisplayServer.InvalidScreen`](DisplayServer.md) if `windowId` is invalid.

**Note:** This method is implemented on Linux/X11, macOS, and Windows. On other platforms, this method always returns `0`.

### WindowGetFlag

```csharp
public bool WindowGetFlag(DisplayServer.WindowFlags flag, int windowId = 0)
```

Returns the current value of the given window's `flag`.

### WindowGetHdrOutputCurrentMaxLuminance

```csharp
public float WindowGetHdrOutputCurrentMaxLuminance(int windowId = 0)
```

When `WindowIsHdrOutputEnabled()` returns `true`, this returns the current maximum luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If the maximum luminance is being automatically adjusted based on the screen's capabilities, this method will return that value. Otherwise, it will return the value set by `WindowSetHdrOutputMaxLuminance()`. This maximum luminance value is used when calculating `WindowGetOutputMaxLinearValue()`.

**Note:** This maximum luminance may not match the physical behavior of the screen, but will always be proportionally correct relative to `WindowGetHdrOutputCurrentReferenceLuminance()`.

### WindowGetHdrOutputCurrentReferenceLuminance

```csharp
public float WindowGetHdrOutputCurrentReferenceLuminance(int windowId = 0)
```

When `WindowIsHdrOutputEnabled()` returns `true`, this returns the current reference white luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If the reference luminance is being automatically adjusted to match the operating system brightness, this will return that value. Otherwise, it will return the value set by `WindowSetHdrOutputReferenceLuminance()`. This reference luminance value is used when calculating `WindowGetOutputMaxLinearValue()`.

**Note:** This reference white luminance may not match the physical behavior of the screen, but will always be proportionally correct relative to `WindowGetHdrOutputCurrentMaxLuminance()`.

### WindowGetHdrOutputMaxLuminance

```csharp
public float WindowGetHdrOutputMaxLuminance(int windowId = 0)
```

Returns the maximum luminance in nits (cd/m²) set for HDR output by the window specified by `windowId`. Negative values indicate that the value is being automatically adjusted based on the screen's capabilities. See also `WindowGetHdrOutputCurrentMaxLuminance()`.

### WindowGetHdrOutputReferenceLuminance

```csharp
public float WindowGetHdrOutputReferenceLuminance(int windowId = 0)
```

Returns the reference white luminance in nits (cd/m²) set for HDR output by the window specified by `windowId`. Negative values indicate that the value is being automatically adjusted to match the operating system brightness. See also `WindowGetHdrOutputCurrentReferenceLuminance()`.

### WindowGetMaxSize

```csharp
public Vector2I WindowGetMaxSize(int windowId = 0)
```

Returns the window's maximum size (in pixels). See also `WindowSetMaxSize()`.

### WindowGetMinSize

```csharp
public Vector2I WindowGetMinSize(int windowId = 0)
```

Returns the window's minimum size (in pixels). See also `WindowSetMinSize()`.

### WindowGetMode

```csharp
public DisplayServer.WindowMode WindowGetMode(int windowId = 0)
```

Returns the mode of the given window.

### WindowGetNativeHandle

```csharp
public long WindowGetNativeHandle(DisplayServer.HandleType handleType, int windowId = 0)
```

Returns internal structure pointers for use in plugins.

**Note:** This method is implemented on Android, Linux (X11/Wayland), macOS, and Windows.

### WindowGetOutputMaxLinearValue

```csharp
public float WindowGetOutputMaxLinearValue(int windowId = 0)
```

Returns the maximum value for linear color components that can be displayed for the window specified by `windowId`, regardless of SDR or HDR output. Returns `1.0` if HDR is not enabled or not supported. When HDR output is enabled, this is calculated based on `WindowGetHdrOutputCurrentReferenceLuminance()` and `WindowGetHdrOutputCurrentMaxLuminance()`. The [`Window.OutputMaxLinearValueChanged`](Window.md) signal will be emitted whenever this value changes. This value is used by tonemapping and other [`Environment`](Environment.md) effects to ensure that bright colors are presented in the range that can be displayed by this window. Corresponds to [`Window.GetOutputMaxLinearValue`](Window.md).

### WindowGetPopupSafeRect

```csharp
public Rect2I WindowGetPopupSafeRect(int window)
```

Returns the bounding box of control, or menu item that was used to open the popup window, in the screen coordinate system.

### WindowGetPosition

```csharp
public Vector2I WindowGetPosition(int windowId = 0)
```

Returns the position of the client area of the given window on the screen.

### WindowGetPositionWithDecorations

```csharp
public Vector2I WindowGetPositionWithDecorations(int windowId = 0)
```

Returns the position of the given window on the screen including the borders drawn by the operating system. See also `WindowGetPosition()`.

### WindowGetSafeTitleMargins

```csharp
public Vector3I WindowGetSafeTitleMargins(int windowId = 0)
```

Returns left margins (`x`), right margins (`y`) and height (`z`) of the title that are safe to use (contains no buttons or other elements) when [`DisplayServer.WindowFlags.ExtendToTitle`](DisplayServer.md) flag is set.

### WindowGetSize

```csharp
public Vector2I WindowGetSize(int windowId = 0)
```

Returns the size of the window specified by `windowId` (in pixels), excluding the borders drawn by the operating system. This is also called the "client area". See also `WindowGetSizeWithDecorations()`, `WindowSetSize()` and `WindowGetPosition()`.

### WindowGetSizeWithDecorations

```csharp
public Vector2I WindowGetSizeWithDecorations(int windowId = 0)
```

Returns the size of the window specified by `windowId` (in pixels), including the borders drawn by the operating system. See also `WindowGetSize()`.

### WindowGetTitleSize

```csharp
public Vector2I WindowGetTitleSize(string title, int windowId = 0)
```

Returns the estimated window title bar size (including text and window buttons) for the window specified by `windowId` (in pixels). This method does not change the window title.

**Note:** This method is implemented on macOS and Windows.

### WindowGetVsyncMode

```csharp
public DisplayServer.VSyncMode WindowGetVsyncMode(int windowId = 0)
```

Returns the V-Sync mode of the given window.

### WindowIsFocused

```csharp
public bool WindowIsFocused(int windowId = 0)
```

Returns `true` if the window specified by `windowId` is focused.

### WindowIsHdrOutputEnabled

```csharp
public bool WindowIsHdrOutputEnabled(int windowId = 0)
```

Returns `true` if HDR output is currently enabled for the window specified by `windowId`. The returned value may change dynamically based on system settings, screen capabilities, and which screen the window is currently on.

### WindowIsHdrOutputRequested

```csharp
public bool WindowIsHdrOutputRequested(int windowId = 0)
```

Returns `true` if HDR output is requested for the window specified by `windowId`. Corresponds to [`Window.HdrOutputRequested`](Window.md).

### WindowIsHdrOutputSupported

```csharp
public bool WindowIsHdrOutputSupported(int windowId = 0)
```

Returns `true` if the window specified by `windowId` supports HDR output. This depends on the platform, screen capabilities, system settings, and the screen the window is currently on.

### WindowIsMaximizeAllowed

```csharp
public bool WindowIsMaximizeAllowed(int windowId = 0)
```

Returns `true` if the given window can be maximized (the maximize button is enabled).

### WindowMaximizeOnTitleDblClick

```csharp
public bool WindowMaximizeOnTitleDblClick()
```

Returns `true` if double-clicking on a window's title should maximize it.

**Note:** This method is implemented only on macOS.

### WindowMinimizeOnTitleDblClick

```csharp
public bool WindowMinimizeOnTitleDblClick()
```

Returns `true` if double-clicking on a window's title should minimize it.

**Note:** This method is implemented only on macOS.

### WindowMoveToForeground

```csharp
public void WindowMoveToForeground(int windowId = 0)
```

Moves the window specified by `windowId` to the foreground, so that it is visible over other windows.

### WindowRequestAttention

```csharp
public void WindowRequestAttention(int windowId = 0)
```

Makes the window specified by `windowId` request attention, which is materialized by the window title and taskbar entry blinking until the window is focused. This usually has no visible effect if the window is currently focused. The exact behavior varies depending on the operating system.

### WindowRequestHdrOutput

```csharp
public void WindowRequestHdrOutput(bool enable, int windowId = 0)
```

If `enable` is `true`, HDR output is requested for the window specified by `windowId`. The window will automatically switch between HDR and SDR if it is moved between screens, screen capabilities change, or system settings are modified. This will internally force [`Viewport.UseHdr2D`](Viewport.md) to be enabled on the main [`Viewport`](Viewport.md). All other [`SubViewport`](SubViewport.md) of the [`Window`](Window.md) must have their [`Viewport.UseHdr2D`](Viewport.md) property enabled to produce HDR output. Corresponds to [`Window.HdrOutputRequested`](Window.md).

### WindowSetColor

```csharp
public void WindowSetColor(Color color)
```

Sets the background color of the root window.

**Note:** This method is implemented only on Android.

### WindowSetCurrentScreen

```csharp
public void WindowSetCurrentScreen(int screen, int windowId = 0)
```

Moves the window specified by `windowId` to the specified `screen`. See also `WindowGetCurrentScreen()`.

**Note:** One of the following constants can be used as `screen`: [`DisplayServer.ScreenOfMainWindow`](DisplayServer.md), [`DisplayServer.ScreenPrimary`](DisplayServer.md), [`DisplayServer.ScreenWithMouseFocus`](DisplayServer.md), or [`DisplayServer.ScreenWithKeyboardFocus`](DisplayServer.md).

**Note:** This method is implemented on Linux/X11, macOS, and Windows.

### WindowSetDropFilesCallback

```csharp
public void WindowSetDropFilesCallback(Callable callback, int windowId = 0)
```

Sets the `callback` that should be called when files are dropped from the operating system's file manager to the window specified by `windowId`. `callback` should take one `System.String`[] argument, which is the list of dropped files.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

**Note:** This method is implemented on Windows, macOS, Linux (X11/Wayland), and Web.

### WindowSetExclusive

```csharp
public void WindowSetExclusive(int windowId, bool exclusive)
```

If set to `true`, this window will always stay on top of its parent window, parent window will ignore input while this window is opened.

**Note:** On macOS, exclusive windows are confined to the same space (virtual desktop or screen) as the parent window.

**Note:** This method is implemented on macOS and Windows.

### WindowSetFlag

```csharp
public void WindowSetFlag(DisplayServer.WindowFlags flag, bool enabled, int windowId = 0)
```

Enables or disables the given window's given `flag`.

### WindowSetHdrOutputMaxLuminance

```csharp
public void WindowSetHdrOutputMaxLuminance(float maxLuminance, int windowId = 0)
```

Sets the maximum luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If `maxLuminance` is negative, the window uses the screen's maximum luminance that is reported by the operating system. By default, this luminance is set to `-1.0` for every window. Typically this property should be left at this default value, but may optionally be exposed through in-game settings to allow the player to correct an inaccurate maximum luminance reported by the operating system. See also `WindowGetHdrOutputCurrentMaxLuminance()` and `WindowGetHdrOutputMaxLuminance()`.

**Note:** This method is only implemented on macOS and Windows. Other platforms will always use the screen's maximum luminance that is reported by the operating system.

### WindowSetHdrOutputReferenceLuminance

```csharp
public void WindowSetHdrOutputReferenceLuminance(float referenceLuminance, int windowId = 0)
```

Sets the reference white luminance in nits (cd/m²) for HDR output by the window specified by `windowId`. If `referenceLuminance` is negative, the window automatically adjusts to the brightness set by the operating system. By default, this luminance is set to `-1.0` for every window. Typically this property should be left at this default value, but may optionally be exposed as an "HDR Brightness" in-game setting to allow the player to adjust the brightness of their game, independently of their device settings. See also `WindowGetHdrOutputCurrentReferenceLuminance()` and `WindowGetHdrOutputReferenceLuminance()`.

**Note:** This method is only implemented on Windows. Other platforms will always use the reference luminance that is reported by the operating system.

### WindowSetIcon

```csharp
public void WindowSetIcon(Image icon, int windowId = 0)
```

Sets the window icon (usually displayed in the top-left corner) for the window specified by `windowId`.

**Note:** This method is implemented on Linux and Windows.

### WindowSetImeActive

```csharp
public void WindowSetImeActive(bool active, int windowId = 0)
```

Sets whether [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) should be enabled for the window specified by `windowId`. See also `WindowSetImePosition()`.

### WindowSetImePosition

```csharp
public void WindowSetImePosition(Vector2I position, int windowId = 0)
```

Sets the position of the [Input Method Editor](https://en.wikipedia.org/wiki/Input_method) popup for the specified `windowId`. Only effective if `WindowSetImeActive()` was set to `true` for the specified `windowId`.

### WindowSetInputEventCallback

```csharp
public void WindowSetInputEventCallback(Callable callback, int windowId = 0)
```

Sets the `callback` that should be called when any [`InputEvent`](InputEvent.md) is sent to the window specified by `windowId`.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowSetInputTextCallback

```csharp
public void WindowSetInputTextCallback(Callable callback, int windowId = 0)
```

Sets the `callback` that should be called when text is entered using the virtual keyboard to the window specified by `windowId`.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowSetMaxSize

```csharp
public void WindowSetMaxSize(Vector2I maxSize, int windowId = 0)
```

Sets the maximum size of the window specified by `windowId` in pixels. Normally, the user will not be able to drag the window to make it larger than the specified size. See also `WindowGetMaxSize()`.

**Note:** It's recommended to change this value using [`Window.MaxSize`](Window.md) instead.

**Note:** Using third-party tools, it is possible for users to disable window geometry restrictions and therefore bypass this limit.

### WindowSetMinSize

```csharp
public void WindowSetMinSize(Vector2I minSize, int windowId = 0)
```

Sets the minimum size for the given window to `minSize` in pixels. Normally, the user will not be able to drag the window to make it smaller than the specified size. See also `WindowGetMinSize()`.

**Note:** It's recommended to change this value using [`Window.MinSize`](Window.md) instead.

**Note:** By default, the main window has a minimum size of `Vector2i(64, 64)`. This prevents issues that can arise when the window is resized to a near-zero size.

**Note:** Using third-party tools, it is possible for users to disable window geometry restrictions and therefore bypass this limit.

### WindowSetMode

```csharp
public void WindowSetMode(DisplayServer.WindowMode mode, int windowId = 0)
```

Sets window mode for the given window to `mode`.

**Note:** On Android, setting it to [`DisplayServer.WindowMode.Fullscreen`](DisplayServer.md) or [`DisplayServer.WindowMode.ExclusiveFullscreen`](DisplayServer.md) will enable immersive mode.

**Note:** Setting the window to full screen forcibly sets the borderless flag to `true`, so make sure to set it back to `false` when not wanted.

### WindowSetMousePassthrough

```csharp
public void WindowSetMousePassthrough(ReadOnlySpan<Vector2> region, int windowId)
```

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
public void WindowSetMousePassthrough(Vector2[] region, int windowId = 0)
```

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
public void WindowSetPopupSafeRect(int window, Rect2I rect)
```

Sets the bounding box of control, or menu item that was used to open the popup window, in the screen coordinate system. Clicking this area will not auto-close this popup.

### WindowSetPosition

```csharp
public void WindowSetPosition(Vector2I position, int windowId = 0)
```

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
public void WindowSetRectChangedCallback(Callable callback, int windowId = 0)
```

Sets the `callback` that will be called when the window specified by `windowId` is moved or resized.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowSetSize

```csharp
public void WindowSetSize(Vector2I size, int windowId = 0)
```

Sets the size of the given window to `size` (in pixels). See also `WindowGetSize()` and `WindowGetPosition()`.

**Note:** It's recommended to change this value using [`Window.Size`](Window.md) instead.

### WindowSetTaskbarProgressState

```csharp
public void WindowSetTaskbarProgressState(DisplayServer.ProgressState state, int windowId = 0)
```

Sets the type and state of the progress bar on the taskbar/dock icon of the window specified by `windowId`. See [`DisplayServer.ProgressState`](DisplayServer.md) for possible values and how each mode behaves.

**Note:** This method is implemented only on Windows and macOS.

**Note:** On macOS, the progress bar is displayed only for the main window.

### WindowSetTaskbarProgressValue

```csharp
public void WindowSetTaskbarProgressValue(float value, int windowId = 0)
```

Creates a progress bar on the taskbar/dock icon of the window specified by `windowId` if it does not exist, sets the progress of the icon.

`value` acts as a relative percentage value, ranges from `0.0` (lowest) to `1.0` (highest).

**Note:** This method is implemented only on Windows and macOS.

**Note:** On macOS, the progress bar is displayed only for the main window.

### WindowSetTitle

```csharp
public void WindowSetTitle(string title, int windowId = 0)
```

Sets the title of the given window to `title`.

**Note:** It's recommended to change this value using [`Window.Title`](Window.md) instead.

**Note:** Avoid changing the window title every frame, as this can cause performance issues on certain window managers. Try to change the window title only a few times per second at most.

### WindowSetTransient

```csharp
public void WindowSetTransient(int windowId, int parentWindowId)
```

Sets window transient parent. Transient window will be destroyed with its transient parent and will return focus to their parent when closed. The transient window is displayed on top of a non-exclusive full-screen parent window. Transient windows can't enter full-screen mode.

**Note:** It's recommended to change this value using [`Window.Transient`](Window.md) instead.

**Note:** The behavior might be different depending on the platform.

### WindowSetVsyncMode

```csharp
public void WindowSetVsyncMode(DisplayServer.VSyncMode vsyncMode, int windowId = 0)
```

Sets the V-Sync mode of the given window. See also `ProjectSettings.display/window/vsync/vsync_mode`.

Depending on the platform and used renderer, the engine will fall back to [`DisplayServer.VSyncMode.Enabled`](DisplayServer.md) if the desired mode is not supported.

**Note:** V-Sync modes other than [`DisplayServer.VSyncMode.Enabled`](DisplayServer.md) are only supported in the Forward+ and Mobile rendering methods, not Compatibility.

### WindowSetWindowButtonsOffset

```csharp
public void WindowSetWindowButtonsOffset(Vector2I offset, int windowId = 0)
```

When [`DisplayServer.WindowFlags.ExtendToTitle`](DisplayServer.md) flag is set, set offset to the center of the first titlebar button.

**Note:** This flag is implemented only on macOS.

### WindowSetWindowEventCallback

```csharp
public void WindowSetWindowEventCallback(Callable callback, int windowId = 0)
```

Sets the `callback` that will be called when an event occurs in the window specified by `windowId`.

**Warning:** Advanced users only! Adding such a callback to a [`Window`](Window.md) node will override its default implementation, which can introduce bugs.

### WindowStartDrag

```csharp
public void WindowStartDrag(int windowId = 0)
```

Starts an interactive drag operation on the window with the given `windowId`, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's title bar. Using this method allows the window to participate in space switching, tiling, and other system features.

**Note:** This method is implemented on Linux (X11/Wayland), macOS, and Windows.

### WindowStartResize

```csharp
public void WindowStartResize(DisplayServer.WindowResizeEdge edge, int windowId = 0)
```

Starts an interactive resize operation on the window with the given `windowId`, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's edge.

**Note:** This method is implemented on Linux (X11/Wayland), macOS, and Windows.

## 事件（信号）

### OrientationChanged

```csharp
public event DisplayServerInstance.OrientationChangedEventHandler OrientationChanged
```

Emitted when the device orientation changes. `orientation` is the new orientation.

Returns `1` for portrait, `2` for landscape, and `0` if the orientation is undefined.

**Note:** This method is implemented on Android and iOS.

## OrientationChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`DisplayServerInstance.OrientationChanged`](DisplayServerInstance.md) event of a [`DisplayServerInstance`](DisplayServerInstance.md) class.

### 签名

#### Invoke

```csharp
delegate void OrientationChangedEventHandler(long orientation)
```
