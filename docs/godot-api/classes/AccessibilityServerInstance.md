<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AccessibilityServerInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

## 方法

### CreateElement

```csharp
public Rid CreateElement(int windowId, AccessibilityServer.AccessibilityRole role)
```

Creates a new, empty accessibility element resource.

**Note:** An accessibility element is created and freed automatically for each [`Node`](Node.md). In general, this function should not be called manually.

### CreateSubElement

```csharp
public Rid CreateSubElement(Rid parentRid, AccessibilityServer.AccessibilityRole role, int insertPos = -1)
```

Creates a new, empty accessibility sub-element resource. Sub-elements can be used to provide accessibility information for objects which are not [`Node`](Node.md)s, such as list items, table cells, or menu items. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `FreeElement()` method.

### CreateSubTextEditElements

```csharp
public Rid CreateSubTextEditElements(Rid parentRid, Rid shapedText, float minHeight, int insertPos = -1, bool isLastLine = false)
```

Creates a new, empty accessibility sub-element from the shaped text buffer. Sub-elements are freed automatically when the parent element is freed, or can be freed early using the `FreeElement()` method.

If `isLastLine` is `true`, no trailing newline is appended to the text content. Set to `true` for the last line in multi-line text fields and for single-line text fields.

### ElementGetMeta

```csharp
public Variant ElementGetMeta(Rid id)
```

Returns the metadata of the accessibility element `id`.

### ElementSetMeta

```csharp
public void ElementSetMeta(Rid id, Variant meta)
```

Sets the metadata of the accessibility element `id` to `meta`.

### FreeElement

```csharp
public void FreeElement(Rid id)
```

Frees the accessibility element `id` created by `CreateElement()`, `CreateSubElement()`, or `CreateSubTextEditElements()`.

### GetWindowRoot

```csharp
public Rid GetWindowRoot(int windowId)
```

Returns the main accessibility element of the OS native window.

### HasElement

```csharp
public bool HasElement(Rid id)
```

Returns `true` if `id` is a valid accessibility element.

### IsSupported

```csharp
public bool IsSupported()
```

Returns `true` if screen reader is support by this implementation.

### SetWindowFocused

```csharp
public void SetWindowFocused(int windowId, bool focused)
```

Sets the window focused state for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### SetWindowRect

```csharp
public void SetWindowRect(int windowId, Rect2 rectOut, Rect2 rectIn)
```

Sets window outer (with decorations) and inner (without decorations) bounds for assistive apps.

**Note:** This method is implemented on Linux, macOS, and Windows.

**Note:** Advanced users only! [`Window`](Window.md) objects call this method automatically.

### UpdateAddAction

```csharp
public void UpdateAddAction(Rid id, AccessibilityServer.AccessibilityAction action, Callable callable)
```

Adds a callback for the accessibility action (action which can be performed by using a special screen reader command or buttons on the Braille display), and marks this action as supported. The action callback receives one [`Variant`](Variant.md) argument, which value depends on action type.

### UpdateAddChild

```csharp
public void UpdateAddChild(Rid id, Rid childId)
```

Adds a child accessibility element.

**Note:** [`Node`](Node.md) children and sub-elements are added to the child list automatically.

### UpdateAddCustomAction

```csharp
public void UpdateAddCustomAction(Rid id, int actionId, string actionDescription)
```

Adds support for a custom accessibility action. `actionId` is passed as an argument to the callback of [`AccessibilityServer.AccessibilityAction.Custom`](AccessibilityServer.md) action.

### UpdateAddRelatedControls

```csharp
public void UpdateAddRelatedControls(Rid id, Rid relatedId)
```

Adds an element that is controlled by this element.

### UpdateAddRelatedDescribedBy

```csharp
public void UpdateAddRelatedDescribedBy(Rid id, Rid relatedId)
```

Adds an element that describes this element.

### UpdateAddRelatedDetails

```csharp
public void UpdateAddRelatedDetails(Rid id, Rid relatedId)
```

Adds an element that details this element.

### UpdateAddRelatedFlowTo

```csharp
public void UpdateAddRelatedFlowTo(Rid id, Rid relatedId)
```

Adds an element that this element flow into.

### UpdateAddRelatedLabeledBy

```csharp
public void UpdateAddRelatedLabeledBy(Rid id, Rid relatedId)
```

Adds an element that labels this element.

### UpdateAddRelatedRadioGroup

```csharp
public void UpdateAddRelatedRadioGroup(Rid id, Rid relatedId)
```

Adds an element that is part of the same radio group.

**Note:** This method should be called on each element of the group, using all other elements as `relatedId`.

### UpdateSetActiveDescendant

```csharp
public void UpdateSetActiveDescendant(Rid id, Rid otherId)
```

Adds an element that is an active descendant of this element.

### UpdateSetBackgroundColor

```csharp
public void UpdateSetBackgroundColor(Rid id, Color color)
```

Sets element background color.

### UpdateSetBounds

```csharp
public void UpdateSetBounds(Rid id, Rect2 rect)
```

Sets element bounding box, relative to the node position.

### UpdateSetBrailleLabel

```csharp
public void UpdateSetBrailleLabel(Rid id, string name)
```

Sets element accessibility label for Braille display.

### UpdateSetBrailleRoleDescription

```csharp
public void UpdateSetBrailleRoleDescription(Rid id, string description)
```

Sets element accessibility role description for Braille display.

### UpdateSetChecked

```csharp
public void UpdateSetChecked(Rid id, bool checekd)
```

Sets element checked state.

### UpdateSetClassname

```csharp
public void UpdateSetClassname(Rid id, string classname)
```

Sets element class name.

### UpdateSetColorValue

```csharp
public void UpdateSetColorValue(Rid id, Color color)
```

Sets element color value.

### UpdateSetDescription

```csharp
public void UpdateSetDescription(Rid id, string description)
```

Sets element accessibility description.

### UpdateSetErrorMessage

```csharp
public void UpdateSetErrorMessage(Rid id, Rid otherId)
```

Sets an element which contains an error message for this element.

### UpdateSetExtraInfo

```csharp
public void UpdateSetExtraInfo(Rid id, string name)
```

Sets element accessibility extra information added to the element name.

### UpdateSetFlag

```csharp
public void UpdateSetFlag(Rid id, AccessibilityServer.AccessibilityFlags flag, bool value)
```

Sets element flag.

### UpdateSetFocus

```csharp
public void UpdateSetFocus(Rid id)
```

Sets currently focused element.

### UpdateSetForegroundColor

```csharp
public void UpdateSetForegroundColor(Rid id, Color color)
```

Sets element foreground color.

### UpdateSetInPageLinkTarget

```csharp
public void UpdateSetInPageLinkTarget(Rid id, Rid otherId)
```

Sets target element for the link.

### UpdateSetLanguage

```csharp
public void UpdateSetLanguage(Rid id, string language)
```

Sets element text language.

### UpdateSetListItemCount

```csharp
public void UpdateSetListItemCount(Rid id, int size)
```

Sets number of items in the list.

### UpdateSetListItemExpanded

```csharp
public void UpdateSetListItemExpanded(Rid id, bool expanded)
```

Sets list/tree item expanded status.

### UpdateSetListItemIndex

```csharp
public void UpdateSetListItemIndex(Rid id, int index)
```

Sets the position of the element in the list.

### UpdateSetListItemLevel

```csharp
public void UpdateSetListItemLevel(Rid id, int level)
```

Sets the hierarchical level of the element in the list.

### UpdateSetListItemSelected

```csharp
public void UpdateSetListItemSelected(Rid id, bool selected)
```

Sets list/tree item selected status.

### UpdateSetListOrientation

```csharp
public void UpdateSetListOrientation(Rid id, bool vertical)
```

Sets the orientation of the list elements.

### UpdateSetLive

```csharp
public void UpdateSetLive(Rid id, AccessibilityServer.AccessibilityLiveMode live)
```

Sets the priority of the live region updates.

### UpdateSetMemberOf

```csharp
public void UpdateSetMemberOf(Rid id, Rid groupId)
```

Sets the element to be a member of the group.

### UpdateSetName

```csharp
public void UpdateSetName(Rid id, string name)
```

Sets element accessibility name.

### UpdateSetNextOnLine

```csharp
public void UpdateSetNextOnLine(Rid id, Rid otherId)
```

Sets next element on the line.

### UpdateSetNumJump

```csharp
public void UpdateSetNumJump(Rid id, double jump)
```

Sets numeric value jump.

### UpdateSetNumRange

```csharp
public void UpdateSetNumRange(Rid id, double min, double max)
```

Sets numeric value range.

### UpdateSetNumStep

```csharp
public void UpdateSetNumStep(Rid id, double step)
```

Sets numeric value step.

### UpdateSetNumValue

```csharp
public void UpdateSetNumValue(Rid id, double position)
```

Sets numeric value.

### UpdateSetPlaceholder

```csharp
public void UpdateSetPlaceholder(Rid id, string placeholder)
```

Sets placeholder text.

### UpdateSetPopupType

```csharp
public void UpdateSetPopupType(Rid id, AccessibilityServer.AccessibilityPopupType popup)
```

Sets popup type for popup buttons.

### UpdateSetPreviousOnLine

```csharp
public void UpdateSetPreviousOnLine(Rid id, Rid otherId)
```

Sets previous element on the line.

### UpdateSetRole

```csharp
public void UpdateSetRole(Rid id, AccessibilityServer.AccessibilityRole role)
```

Sets element accessibility role.

### UpdateSetRoleDescription

```csharp
public void UpdateSetRoleDescription(Rid id, string description)
```

Sets element accessibility role description text.

### UpdateSetScrollX

```csharp
public void UpdateSetScrollX(Rid id, double position)
```

Sets scroll bar x position.

### UpdateSetScrollXRange

```csharp
public void UpdateSetScrollXRange(Rid id, double min, double max)
```

Sets scroll bar x range.

### UpdateSetScrollY

```csharp
public void UpdateSetScrollY(Rid id, double position)
```

Sets scroll bar y position.

### UpdateSetScrollYRange

```csharp
public void UpdateSetScrollYRange(Rid id, double min, double max)
```

Sets scroll bar y range.

### UpdateSetShortcut

```csharp
public void UpdateSetShortcut(Rid id, string shortcut)
```

Sets the list of keyboard shortcuts used by element.

### UpdateSetStateDescription

```csharp
public void UpdateSetStateDescription(Rid id, string description)
```

Sets human-readable description of the current checked state.

### UpdateSetTableCellPosition

```csharp
public void UpdateSetTableCellPosition(Rid id, int rowIndex, int columnIndex)
```

Sets cell position in the table.

### UpdateSetTableCellSpan

```csharp
public void UpdateSetTableCellSpan(Rid id, int rowSpan, int columnSpan)
```

Sets cell row/column span.

### UpdateSetTableColumnCount

```csharp
public void UpdateSetTableColumnCount(Rid id, int count)
```

Sets number of columns in the table.

### UpdateSetTableColumnIndex

```csharp
public void UpdateSetTableColumnIndex(Rid id, int index)
```

Sets position of the column.

### UpdateSetTableRowCount

```csharp
public void UpdateSetTableRowCount(Rid id, int count)
```

Sets number of rows in the table.

### UpdateSetTableRowIndex

```csharp
public void UpdateSetTableRowIndex(Rid id, int index)
```

Sets position of the row in the table.

### UpdateSetTextAlign

```csharp
public void UpdateSetTextAlign(Rid id, HorizontalAlignment align)
```

Sets element text alignment.

### UpdateSetTextDecorations

```csharp
public void UpdateSetTextDecorations(Rid id, bool underline, bool strikethrough, bool overline, Color? color = null)
```

Sets text underline/overline/strikethrough.

参数：
- `color`：If the parameter is null, then the default value is `new Color(0.0f, 0.0f, 0.0f, 1.0f)`.

### UpdateSetTextOrientation

```csharp
public void UpdateSetTextOrientation(Rid id, bool vertical)
```

Sets text orientation.

### UpdateSetTextSelection

```csharp
public void UpdateSetTextSelection(Rid id, Rid textStartId, int startChar, Rid textEndId, int endChar)
```

Sets text selection to the text field. `textStartId` and `textEndId` should be elements created by `CreateSubTextEditElements()`. Character offsets are relative to the corresponding element.

### UpdateSetTooltip

```csharp
public void UpdateSetTooltip(Rid id, string tooltip)
```

Sets tooltip text.

### UpdateSetTransform

```csharp
public void UpdateSetTransform(Rid id, Transform2D transform)
```

Sets element 2D transform.

### UpdateSetUrl

```csharp
public void UpdateSetUrl(Rid id, string url)
```

Sets link URL.

### UpdateSetValue

```csharp
public void UpdateSetValue(Rid id, string value)
```

Sets element text value.
