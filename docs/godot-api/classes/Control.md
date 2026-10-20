<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Control

命名空间 `Godot`；class；继承 [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class for all UI-related nodes. `Control` features a bounding rectangle that defines its extents, an anchor position relative to its parent control or the current viewport, and offsets relative to the anchor. The offsets update automatically when the node, any of its parents, or the screen size change.

For more information on Godot's UI system, anchors, offsets, and containers, see the related tutorials in the manual. To build flexible UIs, you'll need a mix of UI elements that inherit from `Control` and [`Container`](Container.md) nodes.

**Note:** Since both [`Node2D`](Node2D.md) and `Control` inherit from [`CanvasItem`](CanvasItem.md), they share several concepts from the class such as the [`CanvasItem.ZIndex`](CanvasItem.md) and [`CanvasItem.Visible`](CanvasItem.md) properties.

**User Interface nodes and input**

Godot propagates input events via viewports. Each [`Viewport`](Viewport.md) is responsible for propagating [`InputEvent`](InputEvent.md)s to their child nodes. As the [`SceneTree.Root`](SceneTree.md) is a [`Window`](Window.md), this already happens automatically for all UI elements in your game.

Input events are propagated through the [`SceneTree`](SceneTree.md) from the root node to all child nodes by calling [`Node._Input()`](Node.md). For UI elements specifically, it makes more sense to override the virtual method `_GuiInput()`, which filters out unrelated input events, such as by checking z-order, `MouseFilter`, focus, or if the event was inside of the control's bounding box.

Call `AcceptEvent()` so no other node receives the event. Once you accept an input, it becomes handled so [`Node._UnhandledInput()`](Node.md) will not process it.

Only one `Control` node can be in focus. Only the node in focus will receive events. To get the focus, call `GrabFocus()`. `Control` nodes lose focus when another node grabs it, or if you hide the node in focus. Focus will not be represented visually if gained via mouse/touch input, only appearing with keyboard/gamepad input (for accessibility), or via `GrabFocus()`.

Set `MouseFilter` to [`Control.MouseFilterEnum.Ignore`](Control.md) to tell a `Control` node to ignore mouse or touch events. You'll need it if you place an icon on top of a button.

[`Theme`](Theme.md) resources change the control's appearance. The `Theme` of a `Control` node affects all of its direct and indirect children (as long as a chain of controls is uninterrupted). To override some of the theme items, call one of the `add_theme_*_override` methods, like `AddThemeFontOverride()`. You can also override theme items in the Inspector.

**Note:** Theme items are *not* [`GodotObject`](GodotObject.md) properties. This means you can't access their values using [`GodotObject.Get`](GodotObject.md) and [`GodotObject.Set`](GodotObject.md). Instead, use the `get_theme_*` and `add_theme_*_override` methods provided by this class.

## 构造函数

### Control

```csharp
public Control()
```

## 属性

### AccessibilityControlsNodes

```csharp
public Array<NodePath> AccessibilityControlsNodes { get; set; }
```

引擎名：`accessibility_controls_nodes`

The paths to the nodes which are controlled by this node.

### AccessibilityDescribedByNodes

```csharp
public Array<NodePath> AccessibilityDescribedByNodes { get; set; }
```

引擎名：`accessibility_described_by_nodes`

The paths to the nodes which are describing this node.

### AccessibilityDescription

```csharp
public string AccessibilityDescription { get; set; }
```

引擎名：`accessibility_description`

The human-readable node description that is reported to assistive apps.

### AccessibilityFlowToNodes

```csharp
public Array<NodePath> AccessibilityFlowToNodes { get; set; }
```

引擎名：`accessibility_flow_to_nodes`

The paths to the nodes which this node flows into.

### AccessibilityLabeledByNodes

```csharp
public Array<NodePath> AccessibilityLabeledByNodes { get; set; }
```

引擎名：`accessibility_labeled_by_nodes`

The paths to the nodes which label this node.

### AccessibilityLive

```csharp
public AccessibilityServer.AccessibilityLiveMode AccessibilityLive { get; set; }
```

引擎名：`accessibility_live`

The mode with which a live region updates. A live region is a [`Node`](Node.md) that is updated as a result of an external event when the user's focus may be elsewhere.

### AccessibilityName

```csharp
public string AccessibilityName { get; set; }
```

引擎名：`accessibility_name`

The human-readable node name that is reported to assistive apps.

### AnchorBottom

```csharp
public float AnchorBottom { get; set; }
```

引擎名：`anchor_bottom`

Anchors the bottom edge of the node to the origin, the center, or the end of its parent control. It changes how the bottom offset updates when the node moves or changes size. You can use one of the [`Control.Anchor`](Control.md) constants for convenience.

### AnchorLeft

```csharp
public float AnchorLeft { get; set; }
```

引擎名：`anchor_left`

Anchors the left edge of the node to the origin, the center or the end of its parent control. It changes how the left offset updates when the node moves or changes size. You can use one of the [`Control.Anchor`](Control.md) constants for convenience.

### AnchorRight

```csharp
public float AnchorRight { get; set; }
```

引擎名：`anchor_right`

Anchors the right edge of the node to the origin, the center or the end of its parent control. It changes how the right offset updates when the node moves or changes size. You can use one of the [`Control.Anchor`](Control.md) constants for convenience.

### AnchorTop

```csharp
public float AnchorTop { get; set; }
```

引擎名：`anchor_top`

Anchors the top edge of the node to the origin, the center or the end of its parent control. It changes how the top offset updates when the node moves or changes size. You can use one of the [`Control.Anchor`](Control.md) constants for convenience.

### AnchorsPreset

```csharp
public int AnchorsPreset { get; set; }
```

引擎名：`anchors_preset`

### AutoTranslate

```csharp
public bool AutoTranslate { get; set; }
```

引擎名：`auto_translate`

Toggles if any text should automatically change to its translated version depending on the current locale.

### ClipContents

```csharp
public bool ClipContents { get; set; }
```

引擎名：`clip_contents`

Enables whether rendering of [`CanvasItem`](CanvasItem.md) based children should be clipped to this control's rectangle. If `true`, parts of a child which would be visibly outside of this control's rectangle will not be rendered and won't receive input.

### CustomMaximumSize

```csharp
public Vector2 CustomMaximumSize { get; set; }
```

引擎名：`custom_maximum_size`

The maximum size of this Control's bounding rectangle. If set to a value greater than or equal to `(0, 0)`, the node's bounding rectangle will never exceed this size. A value below `(0, 0)` means there is no maximum size.

**Note:** The final effective maximum size may be subject to parent Container sizing and propagated maximum sizes. See also: `GetCombinedMaximumSize()`.

**Note:** Not all `Control` subtypes handle a custom maximum size gracefully, which may lead to unexpected behavior if the control's contents exceed this size.

**Note:** This value has priority over `CustomMinimumSize`. For example, if you set `CustomMaximumSize` to `(100, 100)` and `CustomMinimumSize` to `(200, 200)`, the resulting size will be `(100, 100)`.

**Note:** It is recommended to use `GetBoundMinimumSize()` instead of `GetCombinedMinimumSize()` when using this property, as the former respects maximum size limits when calculating the minimum size, while the latter does not.

### CustomMinimumSize

```csharp
public Vector2 CustomMinimumSize { get; set; }
```

引擎名：`custom_minimum_size`

The minimum size of the node's bounding rectangle. If you set it to a value greater than `(0, 0)`, the node's bounding rectangle will always have at least this size. Note that `Control` nodes have their internal minimum size returned by `GetMinimumSize()`. It depends on the control's contents, like text, textures, or style boxes. The actual minimum size is the maximum value of this property and the internal minimum size (see `GetCombinedMinimumSize()`).

**Note:** `CustomMaximumSize` has priority over this property. For example, if you set `CustomMinimumSize` to `(200, 200)` and `CustomMaximumSize` to `(100, 100)`, the resulting size will be `(100, 100)`.

### FocusBehaviorRecursive

```csharp
public Control.FocusBehaviorRecursiveEnum FocusBehaviorRecursive { get; set; }
```

引擎名：`focus_behavior_recursive`

Determines which controls can be focused together with `FocusMode`. See `GetFocusModeWithOverride()`. Since the default behavior is [`Control.FocusBehaviorRecursiveEnum.Inherited`](Control.md), this can be used to prevent all children controls from getting focused.

### FocusMode

```csharp
public Control.FocusModeEnum FocusMode { get; set; }
```

引擎名：`focus_mode`

Determines which controls can be focused. Only one control can be focused at a time, and the focused control will receive keyboard, gamepad, and mouse events in `_GuiInput()`. Use `GetFocusModeWithOverride()` to determine if a control can grab focus, since `FocusBehaviorRecursive` also affects it. See also `GrabFocus()`.

### FocusNeighborBottom

```csharp
public NodePath FocusNeighborBottom { get; set; }
```

引擎名：`focus_neighbor_bottom`

Tells Godot which node it should give focus to if the user presses the down arrow on the keyboard or down on a gamepad by default. You can change the key by editing the `ProjectSettings.input/ui_down` input action. The node must be a `Control`. If this property is not set, Godot will give focus to the closest `Control` to the bottom of this one.

### FocusNeighborLeft

```csharp
public NodePath FocusNeighborLeft { get; set; }
```

引擎名：`focus_neighbor_left`

Tells Godot which node it should give focus to if the user presses the left arrow on the keyboard or left on a gamepad by default. You can change the key by editing the `ProjectSettings.input/ui_left` input action. The node must be a `Control`. If this property is not set, Godot will give focus to the closest `Control` to the left of this one.

### FocusNeighborRight

```csharp
public NodePath FocusNeighborRight { get; set; }
```

引擎名：`focus_neighbor_right`

Tells Godot which node it should give focus to if the user presses the right arrow on the keyboard or right on a gamepad by default. You can change the key by editing the `ProjectSettings.input/ui_right` input action. The node must be a `Control`. If this property is not set, Godot will give focus to the closest `Control` to the right of this one.

### FocusNeighborTop

```csharp
public NodePath FocusNeighborTop { get; set; }
```

引擎名：`focus_neighbor_top`

Tells Godot which node it should give focus to if the user presses the top arrow on the keyboard or top on a gamepad by default. You can change the key by editing the `ProjectSettings.input/ui_up` input action. The node must be a `Control`. If this property is not set, Godot will give focus to the closest `Control` to the top of this one.

### FocusNext

```csharp
public NodePath FocusNext { get; set; }
```

引擎名：`focus_next`

Tells Godot which node it should give focus to if the user presses Tab on a keyboard by default. You can change the key by editing the `ProjectSettings.input/ui_focus_next` input action.

If this property is not set, Godot will select a "best guess" based on surrounding nodes in the scene tree.

### FocusPrevious

```csharp
public NodePath FocusPrevious { get; set; }
```

引擎名：`focus_previous`

Tells Godot which node it should give focus to if the user presses Shift + Tab on a keyboard by default. You can change the key by editing the `ProjectSettings.input/ui_focus_prev` input action.

If this property is not set, Godot will select a "best guess" based on surrounding nodes in the scene tree.

### GlobalPosition

```csharp
public Vector2 GlobalPosition { get; set; }
```

引擎名：`global_position`

The node's global position, relative to the world (usually to the [`CanvasLayer`](CanvasLayer.md)).

### GrowHorizontal

```csharp
public Control.GrowDirection GrowHorizontal { get; set; }
```

引擎名：`grow_horizontal`

Controls the direction on the horizontal axis in which the control should grow if its horizontal minimum size is changed to be greater than its current size, as the control always has to be at least the minimum size.

### GrowVertical

```csharp
public Control.GrowDirection GrowVertical { get; set; }
```

引擎名：`grow_vertical`

Controls the direction on the vertical axis in which the control should grow if its vertical minimum size is changed to be greater than its current size, as the control always has to be at least the minimum size.

### LayoutDirection

```csharp
public Control.LayoutDirectionEnum LayoutDirection { get; set; }
```

引擎名：`layout_direction`

Controls layout direction and text writing direction. Right-to-left layouts are necessary for certain languages (e.g. Arabic and Hebrew). See also `IsLayoutRtl()`.

### LayoutMode

```csharp
public int LayoutMode { get; set; }
```

引擎名：`layout_mode`

### LocalizeNumeralSystem

```csharp
public bool LocalizeNumeralSystem { get; set; }
```

引擎名：`localize_numeral_system`

If `true`, automatically converts code line numbers, list indices, [`SpinBox`](SpinBox.md) and [`ProgressBar`](ProgressBar.md) values from the Western Arabic (0..9) to the numeral systems used in current locale.

**Note:** Numbers within the text are not automatically converted, it can be done manually, using [`TextServer.FormatNumber`](TextServer.md).

### MouseBehaviorRecursive

```csharp
public Control.MouseBehaviorRecursiveEnum MouseBehaviorRecursive { get; set; }
```

引擎名：`mouse_behavior_recursive`

Determines which controls can receive mouse input together with `MouseFilter`. See `GetMouseFilterWithOverride()`. Since the default behavior is [`Control.MouseBehaviorRecursiveEnum.Inherited`](Control.md), this can be used to prevent all children controls from receiving mouse input.

### MouseDefaultCursorShape

```csharp
public Control.CursorShape MouseDefaultCursorShape { get; set; }
```

引擎名：`mouse_default_cursor_shape`

The default cursor shape for this control. Useful for Godot plugins and applications or games that use the system's mouse cursors.

**Note:** On Linux, shapes may vary depending on the cursor theme of the system.

### MouseFilter

```csharp
public Control.MouseFilterEnum MouseFilter { get; set; }
```

引擎名：`mouse_filter`

Determines which controls will be able to receive mouse button input events through `_GuiInput()` and the `MouseEntered`, and `MouseExited` signals. Also determines how these events should be propagated. See the constants to learn what each does. Use `GetMouseFilterWithOverride()` to determine if a control can receive mouse input, since `MouseBehaviorRecursive` also affects it.

### MouseForcePassScrollEvents

```csharp
public bool MouseForcePassScrollEvents { get; set; }
```

引擎名：`mouse_force_pass_scroll_events`

When enabled, scroll wheel events processed by `_GuiInput()` will be passed to the parent control even if `MouseFilter` is set to [`Control.MouseFilterEnum.Stop`](Control.md).

You should disable it on the root of your UI if you do not want scroll events to go to the [`Node._UnhandledInput()`](Node.md) processing.

**Note:** Because this property defaults to `true`, this allows nested scrollable containers to work out of the box.

### OffsetBottom

```csharp
public float OffsetBottom { get; set; }
```

引擎名：`offset_bottom`

Distance between the node's bottom edge and its parent control, based on `AnchorBottom`.

Offsets are often controlled by one or multiple parent [`Container`](Container.md) nodes, so you should not modify them manually if your node is a direct child of a [`Container`](Container.md). Offsets update automatically when you move or resize the node.

### OffsetLeft

```csharp
public float OffsetLeft { get; set; }
```

引擎名：`offset_left`

Distance between the node's left edge and its parent control, based on `AnchorLeft`.

Offsets are often controlled by one or multiple parent [`Container`](Container.md) nodes, so you should not modify them manually if your node is a direct child of a [`Container`](Container.md). Offsets update automatically when you move or resize the node.

### OffsetRight

```csharp
public float OffsetRight { get; set; }
```

引擎名：`offset_right`

Distance between the node's right edge and its parent control, based on `AnchorRight`.

Offsets are often controlled by one or multiple parent [`Container`](Container.md) nodes, so you should not modify them manually if your node is a direct child of a [`Container`](Container.md). Offsets update automatically when you move or resize the node.

### OffsetTop

```csharp
public float OffsetTop { get; set; }
```

引擎名：`offset_top`

Distance between the node's top edge and its parent control, based on `AnchorTop`.

Offsets are often controlled by one or multiple parent [`Container`](Container.md) nodes, so you should not modify them manually if your node is a direct child of a [`Container`](Container.md). Offsets update automatically when you move or resize the node.

### OffsetTransformEnabled

```csharp
public bool OffsetTransformEnabled { get; set; }
```

引擎名：`offset_transform_enabled`

If `true`, applies all offset transform properties. Otherwise, no offset transform is applied and the properties have no effect.

### OffsetTransformPivot

```csharp
public Vector2 OffsetTransformPivot { get; set; }
```

引擎名：`offset_transform_pivot`

Pivot used by `OffsetTransformRotation` and `OffsetTransformScale` in absolute units.

The final pivot position is the combined value of this property and `OffsetTransformPivotRatio`.

Has no effect unless `OffsetTransformEnabled` is `true`.

### OffsetTransformPivotRatio

```csharp
public Vector2 OffsetTransformPivotRatio { get; set; }
```

引擎名：`offset_transform_pivot_ratio`

Same as `OffsetTransformPivot` but expressed in units relative to the `Control` `Size` where `Vector2(0, 0)` is the top-left corner of this control, and `Vector2(1, 1)` is its bottom-right corner.

The final pivot position is the combined value of this property and `OffsetTransformPivot`.

Has no effect unless `OffsetTransformEnabled` is `true`.

### OffsetTransformPosition

```csharp
public Vector2 OffsetTransformPosition { get; set; }
```

引擎名：`offset_transform_position`

Position offset in absolute units. The final offset is the combined value of this property and `OffsetTransformPositionRatio`.

Has no effect unless `OffsetTransformEnabled` is `true`.

### OffsetTransformPositionRatio

```csharp
public Vector2 OffsetTransformPositionRatio { get; set; }
```

引擎名：`offset_transform_position_ratio`

Same as `OffsetTransformPosition` but expressed in units relative to the `Control` `Size` where `Vector2(0, 0)` is the top-left corner of this control, and `Vector2(1, 1)` is its bottom-right corner.

The final offset is the combined value of this property and `OffsetTransformPosition`.

Has no effect unless `OffsetTransformEnabled` is `true`.

### OffsetTransformRotation

```csharp
public float OffsetTransformRotation { get; set; }
```

引擎名：`offset_transform_rotation`

Rotation offset. The rotation pivot is defined by `OffsetTransformPivot` and `OffsetTransformPivotRatio`.

Has no effect unless `OffsetTransformEnabled` is `true`.

### OffsetTransformScale

```csharp
public Vector2 OffsetTransformScale { get; set; }
```

引擎名：`offset_transform_scale`

Scale offset. The scale pivot is defined by `OffsetTransformPivot` and `OffsetTransformPivotRatio`.

Has no effect unless `OffsetTransformEnabled` is `true`.

### OffsetTransformVisualOnly

```csharp
public bool OffsetTransformVisualOnly { get; set; }
```

引擎名：`offset_transform_visual_only`

If `true`, the offset transforms is only applied visually and does not affect input. In other words, this Control will still receive input events at its original location before the offset transform is applied.

If `false`, the entire transform of this Control is affected and input events will register where the Control is visually.

Has no effect unless `OffsetTransformEnabled` is `true`.

### PivotOffset

```csharp
public Vector2 PivotOffset { get; set; }
```

引擎名：`pivot_offset`

By default, the node's pivot is its top-left corner. When you change its `Rotation` or `Scale`, it will rotate or scale around this pivot.

The actual offset is the combined value of this property and `PivotOffsetRatio`.

### PivotOffsetRatio

```csharp
public Vector2 PivotOffsetRatio { get; set; }
```

引擎名：`pivot_offset_ratio`

Same as `PivotOffset`, but expressed as uniform vector, where `Vector2(0, 0)` is the top-left corner of this control, and `Vector2(1, 1)` is its bottom-right corner. Set this property to `Vector2(0.5, 0.5)` to pivot around this control's center.

The actual offset is the combined value of this property and `PivotOffset`.

### Position

```csharp
public Vector2 Position { get; set; }
```

引擎名：`position`

The node's position, relative to its containing node. It corresponds to the rectangle's top-left corner. The property is not affected by `PivotOffset`.

### PropagateMaximumSize

```csharp
public bool PropagateMaximumSize { get; set; }
```

引擎名：`propagate_maximum_size`

If `true`, this Control's children will use the value returned by `GetCombinedMaximumSize()` in their own size calculations.

### Rotation

```csharp
public float Rotation { get; set; }
```

引擎名：`rotation`

The node's rotation around its pivot, in radians. See `PivotOffset` to change the pivot's position.

**Note:** This property is edited in the inspector in degrees. If you want to use degrees in a script, use `RotationDegrees`.

### RotationDegrees

```csharp
public float RotationDegrees { get; set; }
```

引擎名：`rotation_degrees`

Helper property to access `Rotation` in degrees instead of radians.

### Scale

```csharp
public Vector2 Scale { get; set; }
```

引擎名：`scale`

The node's scale, relative to its `Size`. Change this property to scale the node around its `PivotOffset`. The Control's tooltip will also scale according to this value.

**Note:** This property is mainly intended to be used for animation purposes. To support multiple resolutions in your project, use an appropriate viewport stretch mode as described in the [documentation]($DOCS_URL/tutorials/rendering/multiple_resolutions.html) instead of scaling Controls individually.

**Note:** [`FontFile.Oversampling`](FontFile.md) does *not* take `Control` `Scale` into account. This means that scaling up/down will cause bitmap fonts and rasterized (non-MSDF) dynamic fonts to appear blurry or pixelated. To ensure text remains crisp regardless of scale, you can enable MSDF font rendering by enabling `ProjectSettings.gui/theme/default_font_multichannel_signed_distance_field` (applies to the default project font only), or enabling **Multichannel Signed Distance Field** in the import options of a DynamicFont for custom fonts. On system fonts, [`SystemFont.MultichannelSignedDistanceField`](SystemFont.md) can be enabled in the inspector.

**Note:** If the Control node is a child of a [`Container`](Container.md) node, the scale will be reset to `Vector2(1, 1)` when the scene is instantiated. To set the Control's scale when it's instantiated, wait for one frame using `await get_tree().process_frame` then set its `Scale` property.

### ShortcutContext

```csharp
public Node ShortcutContext { get; set; }
```

引擎名：`shortcut_context`

The [`Node`](Node.md) which must be a parent of the focused `Control` for the shortcut to be activated. If `null`, the shortcut can be activated when any control is focused (a global shortcut). This allows shortcuts to be accepted only when the user has a certain area of the GUI focused.

### Size

```csharp
public Vector2 Size { get; set; }
```

引擎名：`size`

The size of the node's bounding rectangle, in the node's coordinate system. [`Container`](Container.md) nodes update this property automatically.

### SizeFlagsHorizontal

```csharp
public Control.SizeFlags SizeFlagsHorizontal { get; set; }
```

引擎名：`size_flags_horizontal`

Tells the parent [`Container`](Container.md) nodes how they should resize and place the node on the X axis. Use a combination of the [`Control.SizeFlags`](Control.md) constants to change the flags. See the constants to learn what each does.

### SizeFlagsStretchRatio

```csharp
public float SizeFlagsStretchRatio { get; set; }
```

引擎名：`size_flags_stretch_ratio`

If the node and at least one of its neighbors uses the [`Control.SizeFlags.Expand`](Control.md) size flag, the parent [`Container`](Container.md) will let it take more or less space depending on this property. If this node has a stretch ratio of 2 and its neighbor a ratio of 1, this node will take two thirds of the available space.

### SizeFlagsVertical

```csharp
public Control.SizeFlags SizeFlagsVertical { get; set; }
```

引擎名：`size_flags_vertical`

Tells the parent [`Container`](Container.md) nodes how they should resize and place the node on the Y axis. Use a combination of the [`Control.SizeFlags`](Control.md) constants to change the flags. See the constants to learn what each does.

### Theme

```csharp
public Theme Theme { get; set; }
```

引擎名：`theme`

The [`Theme`](Theme.md) resource this node and all its `Control` and [`Window`](Window.md) children use. If a child node has its own [`Theme`](Theme.md) resource set, theme items are merged with child's definitions having higher priority.

**Note:** [`Window`](Window.md) styles will have no effect unless the window is embedded.

### ThemeTypeVariation

```csharp
public StringName ThemeTypeVariation { get; set; }
```

引擎名：`theme_type_variation`

The name of a theme type variation used by this `Control` to look up its own theme items. When empty, the class name of the node is used (e.g. `Button` for the [`Button`](Button.md) control), as well as the class names of all parent classes (in order of inheritance).

When set, this property gives the highest priority to the type of the specified name. This type can in turn extend another type, forming a dependency chain. See [`Theme.SetTypeVariation`](Theme.md). If the theme item cannot be found using this type or its base types, lookup falls back on the class names.

**Note:** To look up `Control`'s own items use various `get_theme_*` methods without specifying `theme_type`.

**Note:** Theme items are looked for in the tree order, from branch to root, where each `Control` node is checked for its `Theme` property. The earliest match against any type/class name is returned. The project-level Theme and the default Theme are checked last.

### TooltipAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum TooltipAutoTranslateMode { get; set; }
```

引擎名：`tooltip_auto_translate_mode`

Defines if tooltip text should automatically change to its translated version depending on the current locale. Uses the same auto translate mode as this control when set to [`Node.AutoTranslateModeEnum.Inherit`](Node.md).

**Note:** Tooltips customized using `_MakeCustomTooltip()` do not use this auto translate mode automatically.

### TooltipText

```csharp
public string TooltipText { get; set; }
```

引擎名：`tooltip_text`

The default tooltip text. The tooltip appears when the user's mouse cursor stays idle over this control for a few moments, provided that the `MouseFilter` property is not [`Control.MouseFilterEnum.Ignore`](Control.md). The time required for the tooltip to appear can be changed with the `ProjectSettings.gui/timers/tooltip_delay_sec` setting.

This string is the default return value of `GetTooltip()`. Override `_GetTooltip()` to generate tooltip text dynamically. Override `_MakeCustomTooltip()` to customize the tooltip interface and behavior.

The tooltip popup will use either a default implementation, or a custom one that you can provide by overriding `_MakeCustomTooltip()`. The default tooltip includes a [`PopupPanel`](PopupPanel.md) and [`Label`](Label.md) whose theme properties can be customized using [`Theme`](Theme.md) methods with the `"TooltipPanel"` and `"TooltipLabel"` respectively. For example:

```csharp
var styleBox = new StyleBoxFlat();
styleBox.SetBgColor(new Color(1, 1, 0));
styleBox.SetBorderWidthAll(2);
// We assume here that the `Theme` property has been assigned a custom Theme beforehand.
Theme.SetStyleBox("panel", "TooltipPanel", styleBox);
Theme.SetColor("font_color", "TooltipLabel", new Color(0, 1, 1));

```

### TranslationContext

```csharp
public StringName TranslationContext { get; set; }
```

引擎名：`translation_context`

The translation context used when translating this control's displayed text, if it has any. Also used when generating translation templates.

## 方法

### AcceptEvent

```csharp
public void AcceptEvent()
```

引擎名：`accept_event`

Marks an input event as handled. Once you accept an input event, it stops propagating, even to nodes listening to [`Node._UnhandledInput()`](Node.md) or [`Node._UnhandledKeyInput()`](Node.md).

**Note:** This does not affect the methods in [`Input`](Input.md), only the way events are propagated.

### AccessibilityDrag

```csharp
public void AccessibilityDrag()
```

引擎名：`accessibility_drag`

Starts drag-and-drop operation without using a mouse.

### AccessibilityDrop

```csharp
public void AccessibilityDrop()
```

引擎名：`accessibility_drop`

Ends drag-and-drop operation without using a mouse.

### AddThemeColorOverride

```csharp
public void AddThemeColorOverride(StringName name, Color color)
```

引擎名：`add_theme_color_override`

Creates a local override for a theme [`Color`](Color.md) with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeColorOverride()`.

See also `GetThemeColor()`.

**Example:** Override a [`Label`](Label.md)'s color and reset it later:

```csharp
// Given the child Label node "MyLabel", override its font color with a custom value.
GetNode<Label>("MyLabel").AddThemeColorOverride("font_color", new Color(1, 0.5f, 0));
// Reset the font color of the child label.
GetNode<Label>("MyLabel").RemoveThemeColorOverride("font_color");
// Alternatively it can be overridden with the default value from the Label type.
GetNode<Label>("MyLabel").AddThemeColorOverride("font_color", GetThemeColor("font_color", "Label"));

```

### AddThemeConstantOverride

```csharp
public void AddThemeConstantOverride(StringName name, int constant)
```

引擎名：`add_theme_constant_override`

Creates a local override for a theme constant with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeConstantOverride()`.

See also `GetThemeConstant()`.

### AddThemeFontOverride

```csharp
public void AddThemeFontOverride(StringName name, Font font)
```

引擎名：`add_theme_font_override`

Creates a local override for a theme [`Font`](Font.md) with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeFontOverride()`.

See also `GetThemeFont()`.

### AddThemeFontSizeOverride

```csharp
public void AddThemeFontSizeOverride(StringName name, int fontSize)
```

引擎名：`add_theme_font_size_override`

Creates a local override for a theme font size with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeFontSizeOverride()`.

See also `GetThemeFontSize()`.

### AddThemeIconOverride

```csharp
public void AddThemeIconOverride(StringName name, Texture2D texture)
```

引擎名：`add_theme_icon_override`

Creates a local override for a theme icon with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeIconOverride()`.

See also `GetThemeIcon()`.

### AddThemeStyleboxOverride

```csharp
public void AddThemeStyleboxOverride(StringName name, StyleBox stylebox)
```

引擎名：`add_theme_stylebox_override`

Creates a local override for a theme [`StyleBox`](StyleBox.md) with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeStyleboxOverride()`.

See also `GetThemeStylebox()`.

**Example:** Modify a property in a [`StyleBox`](StyleBox.md) by duplicating it:

```csharp
// The snippet below assumes the child node "MyButton" has a StyleBoxFlat assigned.
// Resources are shared across instances, so we need to duplicate it
// to avoid modifying the appearance of all other buttons.
StyleBoxFlat newStyleboxNormal = GetNode<Button>("MyButton").GetThemeStylebox("normal").Duplicate() as StyleBoxFlat;
newStyleboxNormal.BorderWidthTop = 3;
newStyleboxNormal.BorderColor = new Color(0, 1, 0.5f);
GetNode<Button>("MyButton").AddThemeStyleboxOverride("normal", newStyleboxNormal);
// Remove the stylebox override.
GetNode<Button>("MyButton").RemoveThemeStyleboxOverride("normal");

```

### BeginBulkThemeOverride

```csharp
public void BeginBulkThemeOverride()
```

引擎名：`begin_bulk_theme_override`

Prevents `*_theme_*_override` methods from emitting `NotificationThemeChanged` until `EndBulkThemeOverride()` is called.

### EndBulkThemeOverride

```csharp
public void EndBulkThemeOverride()
```

引擎名：`end_bulk_theme_override`

Ends a bulk theme override update. See `BeginBulkThemeOverride()`.

### FindNextValidFocus

```csharp
public Control FindNextValidFocus()
```

引擎名：`find_next_valid_focus`

Finds the next (below in the tree) `Control` that can receive the focus.

### FindPrevValidFocus

```csharp
public Control FindPrevValidFocus()
```

引擎名：`find_prev_valid_focus`

Finds the previous (above in the tree) `Control` that can receive the focus.

### FindValidFocusNeighbor

```csharp
public Control FindValidFocusNeighbor(Side side)
```

引擎名：`find_valid_focus_neighbor`

Finds the next `Control` that can receive the focus on the specified [`Side`](Side.md).

**Note:** This is different from `GetFocusNeighbor()`, which returns the path of a specified focus neighbor.

### ForceDrag

```csharp
public void ForceDrag(Variant data, Control preview)
```

引擎名：`force_drag`

Forces drag and bypasses `_GetDragData()` and `SetDragPreview()` by passing `data` and `preview`. Drag will start even if the mouse is neither over nor pressed on this control.

The methods `_CanDropData()` and `_DropData()` must be implemented on controls that want to receive drop data.

### GetAccessibilityControlsNodes

```csharp
public Array<NodePath> GetAccessibilityControlsNodes()
```

引擎名：`get_accessibility_controls_nodes`

### GetAccessibilityDescribedByNodes

```csharp
public Array<NodePath> GetAccessibilityDescribedByNodes()
```

引擎名：`get_accessibility_described_by_nodes`

### GetAccessibilityDescription

```csharp
public string GetAccessibilityDescription()
```

引擎名：`get_accessibility_description`

### GetAccessibilityFlowToNodes

```csharp
public Array<NodePath> GetAccessibilityFlowToNodes()
```

引擎名：`get_accessibility_flow_to_nodes`

### GetAccessibilityLabeledByNodes

```csharp
public Array<NodePath> GetAccessibilityLabeledByNodes()
```

引擎名：`get_accessibility_labeled_by_nodes`

### GetAccessibilityLive

```csharp
public AccessibilityServer.AccessibilityLiveMode GetAccessibilityLive()
```

引擎名：`get_accessibility_live`

### GetAccessibilityName

```csharp
public string GetAccessibilityName()
```

引擎名：`get_accessibility_name`

### GetAnchor

```csharp
public float GetAnchor(Side side)
```

引擎名：`get_anchor`

Returns the anchor for the specified [`Side`](Side.md). A getter method for `AnchorBottom`, `AnchorLeft`, `AnchorRight` and `AnchorTop`.

### GetBegin

```csharp
public Vector2 GetBegin()
```

引擎名：`get_begin`

Returns `OffsetLeft` and `OffsetTop`. See also `Position`.

### GetBoundMinimumSize

```csharp
public Vector2 GetBoundMinimumSize()
```

引擎名：`get_bound_minimum_size`

Returns the bound value of `GetCombinedMinimumSize()` by `GetCombinedMaximumSize()`.

This value is the true minimum size of the container, as the maximum size has priority over the minimum size.

For example, if the combined minimum size is (100, 100) and the combined maximum size is (50, 150), the bound minimum size will be (50, 100).

### GetCombinedMaximumSize

```csharp
public Vector2 GetCombinedMaximumSize()
```

引擎名：`get_combined_maximum_size`

Returns the combined maximum size from `CustomMaximumSize` and `GetMaximumSize()`, as well as the `CustomMaximumSize` of this node's parent if it is a Control node with `PropagateMaximumSize` set to `true`.

### GetCombinedMinimumSize

```csharp
public Vector2 GetCombinedMinimumSize()
```

引擎名：`get_combined_minimum_size`

Returns the combined minimum size from `CustomMinimumSize` and `GetMinimumSize()`.

### GetCombinedPivotOffset

```csharp
public Vector2 GetCombinedPivotOffset()
```

引擎名：`get_combined_pivot_offset`

Returns the combined value of `PivotOffset` and `PivotOffsetRatio`, in pixels. The ratio is multiplied by the control's size.

### GetCursorShape

```csharp
public Control.CursorShape GetCursorShape(Vector2? atPosition = null)
```

引擎名：`get_cursor_shape`

Returns the mouse cursor shape for this control when hovered over `atPosition` in local coordinates. For most controls, this is the same as `MouseDefaultCursorShape`, but some built-in controls implement more complex logic.

You can override `_GetCursorShape()` to implement custom behavior for this method.

参数：
- `atPosition`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### GetCustomMaximumSize

```csharp
public Vector2 GetCustomMaximumSize()
```

引擎名：`get_custom_maximum_size`

### GetCustomMinimumSize

```csharp
public Vector2 GetCustomMinimumSize()
```

引擎名：`get_custom_minimum_size`

### GetDefaultCursorShape

```csharp
public Control.CursorShape GetDefaultCursorShape()
```

引擎名：`get_default_cursor_shape`

### GetEnd

```csharp
public Vector2 GetEnd()
```

引擎名：`get_end`

Returns `OffsetRight` and `OffsetBottom`.

### GetFocusBehaviorRecursive

```csharp
public Control.FocusBehaviorRecursiveEnum GetFocusBehaviorRecursive()
```

引擎名：`get_focus_behavior_recursive`

### GetFocusMode

```csharp
public Control.FocusModeEnum GetFocusMode()
```

引擎名：`get_focus_mode`

### GetFocusModeWithOverride

```csharp
public Control.FocusModeEnum GetFocusModeWithOverride()
```

引擎名：`get_focus_mode_with_override`

Returns the `FocusMode`, but takes the `FocusBehaviorRecursive` into account. If `FocusBehaviorRecursive` is set to [`Control.FocusBehaviorRecursiveEnum.Disabled`](Control.md), or it is set to [`Control.FocusBehaviorRecursiveEnum.Inherited`](Control.md) and its ancestor is set to [`Control.FocusBehaviorRecursiveEnum.Disabled`](Control.md), then this returns [`Control.FocusModeEnum.None`](Control.md).

### GetFocusNeighbor

```csharp
public NodePath GetFocusNeighbor(Side side)
```

引擎名：`get_focus_neighbor`

Returns the focus neighbor for the specified [`Side`](Side.md). A getter method for `FocusNeighborBottom`, `FocusNeighborLeft`, `FocusNeighborRight` and `FocusNeighborTop`.

**Note:** To find the next `Control` on the specific [`Side`](Side.md), even if a neighbor is not assigned, use `FindValidFocusNeighbor()`.

### GetFocusNext

```csharp
public NodePath GetFocusNext()
```

引擎名：`get_focus_next`

### GetFocusPrevious

```csharp
public NodePath GetFocusPrevious()
```

引擎名：`get_focus_previous`

### GetGlobalPosition

```csharp
public Vector2 GetGlobalPosition()
```

引擎名：`get_global_position`

### GetGlobalRect

```csharp
public Rect2 GetGlobalRect()
```

引擎名：`get_global_rect`

Returns the position and size of the control relative to the containing canvas. See `GlobalPosition` and `Size`.

**Note:** If the node itself or any parent [`CanvasItem`](CanvasItem.md) between the node and the canvas have a non default rotation or skew, the resulting size is likely not meaningful.

**Note:** Setting [`Viewport.GuiSnapControlsToPixels`](Viewport.md) to `true` can lead to rounding inaccuracies between the displayed control and the returned [`Rect2`](Rect2.md).

### GetHGrowDirection

```csharp
public Control.GrowDirection GetHGrowDirection()
```

引擎名：`get_h_grow_direction`

### GetHSizeFlags

```csharp
public Control.SizeFlags GetHSizeFlags()
```

引擎名：`get_h_size_flags`

### GetLayoutDirection

```csharp
public Control.LayoutDirectionEnum GetLayoutDirection()
```

引擎名：`get_layout_direction`

### GetMaximumSize

```csharp
public Vector2 GetMaximumSize()
```

引擎名：`get_maximum_size`

Returns the maximum size for this control. See `CustomMaximumSize`.

### GetMinimumSize

```csharp
public Vector2 GetMinimumSize()
```

引擎名：`get_minimum_size`

Returns the minimum size for this control. See `CustomMinimumSize`.

### GetMouseBehaviorRecursive

```csharp
public Control.MouseBehaviorRecursiveEnum GetMouseBehaviorRecursive()
```

引擎名：`get_mouse_behavior_recursive`

### GetMouseFilter

```csharp
public Control.MouseFilterEnum GetMouseFilter()
```

引擎名：`get_mouse_filter`

### GetMouseFilterWithOverride

```csharp
public Control.MouseFilterEnum GetMouseFilterWithOverride()
```

引擎名：`get_mouse_filter_with_override`

Returns the `MouseFilter`, but takes the `MouseBehaviorRecursive` into account. If `MouseBehaviorRecursive` is set to [`Control.MouseBehaviorRecursiveEnum.Disabled`](Control.md), or it is set to [`Control.MouseBehaviorRecursiveEnum.Inherited`](Control.md) and its ancestor is set to [`Control.MouseBehaviorRecursiveEnum.Disabled`](Control.md), then this returns [`Control.MouseFilterEnum.Ignore`](Control.md).

### GetOffset

```csharp
public float GetOffset(Side offset)
```

引擎名：`get_offset`

Returns the offset for the specified [`Side`](Side.md). A getter method for `OffsetBottom`, `OffsetLeft`, `OffsetRight` and `OffsetTop`.

### GetOffsetTransformPivot

```csharp
public Vector2 GetOffsetTransformPivot()
```

引擎名：`get_offset_transform_pivot`

### GetOffsetTransformPivotRatio

```csharp
public Vector2 GetOffsetTransformPivotRatio()
```

引擎名：`get_offset_transform_pivot_ratio`

### GetOffsetTransformPosition

```csharp
public Vector2 GetOffsetTransformPosition()
```

引擎名：`get_offset_transform_position`

### GetOffsetTransformPositionRatio

```csharp
public Vector2 GetOffsetTransformPositionRatio()
```

引擎名：`get_offset_transform_position_ratio`

### GetOffsetTransformRotation

```csharp
public float GetOffsetTransformRotation()
```

引擎名：`get_offset_transform_rotation`

### GetOffsetTransformScale

```csharp
public Vector2 GetOffsetTransformScale()
```

引擎名：`get_offset_transform_scale`

### GetParentAreaSize

```csharp
public Vector2 GetParentAreaSize()
```

引擎名：`get_parent_area_size`

Returns the width/height occupied in the parent control.

### GetParentControl

```csharp
public Control GetParentControl()
```

引擎名：`get_parent_control`

Returns the parent control node.

### GetPivotOffset

```csharp
public Vector2 GetPivotOffset()
```

引擎名：`get_pivot_offset`

### GetPivotOffsetRatio

```csharp
public Vector2 GetPivotOffsetRatio()
```

引擎名：`get_pivot_offset_ratio`

### GetPosition

```csharp
public Vector2 GetPosition()
```

引擎名：`get_position`

### GetRect

```csharp
public Rect2 GetRect()
```

引擎名：`get_rect`

Returns the position and size of the control in the coordinate system of the containing node. See `Position`, `Scale` and `Size`.

**Note:** If `Rotation` is not the default rotation, the resulting size is not meaningful.

**Note:** Setting [`Viewport.GuiSnapControlsToPixels`](Viewport.md) to `true` can lead to rounding inaccuracies between the displayed control and the returned [`Rect2`](Rect2.md).

### GetRotation

```csharp
public float GetRotation()
```

引擎名：`get_rotation`

### GetRotationDegrees

```csharp
public float GetRotationDegrees()
```

引擎名：`get_rotation_degrees`

### GetScale

```csharp
public Vector2 GetScale()
```

引擎名：`get_scale`

### GetScreenPosition

```csharp
public Vector2 GetScreenPosition()
```

引擎名：`get_screen_position`

Returns the position of this `Control` in global screen coordinates (i.e. taking window position into account). Mostly useful for editor plugins.

Equivalent to `get_screen_transform().origin` (see [`CanvasItem.GetScreenTransform`](CanvasItem.md)).

**Example:** Show a popup at the mouse position:

```csharp
popup_menu.position = get_screen_position() + get_screen_transform().basis_xform(get_local_mouse_position())

# The above code is equivalent to:
popup_menu.position = get_screen_transform() * get_local_mouse_position()

popup_menu.reset_size()
popup_menu.popup()

```

### GetShortcutContext

```csharp
public Node GetShortcutContext()
```

引擎名：`get_shortcut_context`

### GetSize

```csharp
public Vector2 GetSize()
```

引擎名：`get_size`

### GetStretchRatio

```csharp
public float GetStretchRatio()
```

引擎名：`get_stretch_ratio`

### GetTheme

```csharp
public Theme GetTheme()
```

引擎名：`get_theme`

### GetThemeColor

```csharp
public Color GetThemeColor(StringName name, StringName themeType = null)
```

引擎名：`get_theme_color`

Returns a [`Color`](Color.md) from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a color item with the specified `name` and `themeType`. If `themeType` is omitted the class name of the current control is used as the type, or `ThemeTypeVariation` if it is defined. If the type is a class name its parent classes are also checked, in order of inheritance. If the type is a variation its base types are checked, in order of dependency, then the control's class name and its parent classes are checked.

For the current control its local overrides are considered first (see `AddThemeColorOverride()`), then its assigned `Theme`. After the current control, each parent control and its assigned `Theme` are considered; controls without a `Theme` assigned are skipped. If no matching [`Theme`](Theme.md) is found in the tree, the custom project [`Theme`](Theme.md) (see `ProjectSettings.gui/theme/custom`) and the default [`Theme`](Theme.md) are used (see [`ThemeDB`](ThemeDB.md)).

```csharp
public override void _Ready()
{
// Get the font color defined for the current Control's class, if it exists.
Modulate = GetThemeColor("font_color");
// Get the font color defined for the Button class.
Modulate = GetThemeColor("font_color", "Button");
}

```

### GetThemeConstant

```csharp
public int GetThemeConstant(StringName name, StringName themeType = null)
```

引擎名：`get_theme_constant`

Returns a constant from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a constant item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### GetThemeDefaultBaseScale

```csharp
public float GetThemeDefaultBaseScale()
```

引擎名：`get_theme_default_base_scale`

Returns the default base scale value from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a valid [`Theme.DefaultBaseScale`](Theme.md) value.

See `GetThemeColor()` for details.

### GetThemeDefaultFont

```csharp
public Font GetThemeDefaultFont()
```

引擎名：`get_theme_default_font`

Returns the default font from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a valid [`Theme.DefaultFont`](Theme.md) value.

See `GetThemeColor()` for details.

### GetThemeDefaultFontSize

```csharp
public int GetThemeDefaultFontSize()
```

引擎名：`get_theme_default_font_size`

Returns the default font size value from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a valid [`Theme.DefaultFontSize`](Theme.md) value.

See `GetThemeColor()` for details.

### GetThemeFont

```csharp
public Font GetThemeFont(StringName name, StringName themeType = null)
```

引擎名：`get_theme_font`

Returns a [`Font`](Font.md) from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a font item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### GetThemeFontSize

```csharp
public int GetThemeFontSize(StringName name, StringName themeType = null)
```

引擎名：`get_theme_font_size`

Returns a font size from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a font size item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### GetThemeIcon

```csharp
public Texture2D GetThemeIcon(StringName name, StringName themeType = null)
```

引擎名：`get_theme_icon`

Returns an icon from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has an icon item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### GetThemeStylebox

```csharp
public StyleBox GetThemeStylebox(StringName name, StringName themeType = null)
```

引擎名：`get_theme_stylebox`

Returns a [`StyleBox`](StyleBox.md) from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a stylebox item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### GetThemeTypeVariation

```csharp
public StringName GetThemeTypeVariation()
```

引擎名：`get_theme_type_variation`

### GetTooltip

```csharp
public string GetTooltip(Vector2? atPosition = null)
```

引擎名：`get_tooltip`

Returns the tooltip text for the position `atPosition` in the control's local coordinates, which will typically appear when the cursor is resting over this control. By default, it returns `TooltipText`.

You can override `_GetTooltip()` to implement custom behavior for this method.

**Note:** If this method returns an empty `System.String` and `_MakeCustomTooltip()` is not overridden, no tooltip is displayed.

参数：
- `atPosition`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### GetTooltipAutoTranslateMode

```csharp
public Node.AutoTranslateModeEnum GetTooltipAutoTranslateMode()
```

引擎名：`get_tooltip_auto_translate_mode`

### GetTooltipText

```csharp
public string GetTooltipText()
```

引擎名：`get_tooltip_text`

### GetTranslationContext

```csharp
public StringName GetTranslationContext()
```

引擎名：`get_translation_context`

### GetVGrowDirection

```csharp
public Control.GrowDirection GetVGrowDirection()
```

引擎名：`get_v_grow_direction`

### GetVSizeFlags

```csharp
public Control.SizeFlags GetVSizeFlags()
```

引擎名：`get_v_size_flags`

### GrabClickFocus

```csharp
public void GrabClickFocus()
```

引擎名：`grab_click_focus`

Creates an [`InputEventMouseButton`](InputEventMouseButton.md) that attempts to click the control. If the event is received, the control gains focus.

```csharp
public override void _Process(double delta)
{
GrabClickFocus(); // When clicking another Control node, this node will be clicked instead.
}

```

### GrabFocus

```csharp
public void GrabFocus()
```

引擎名：`grab_focus`

Steal the focus from another control and become the focused control (see `FocusMode`).

If `hideFocus` is `true`, the control will not visually show its focused state. Has no effect for [`LineEdit`](LineEdit.md) and [`TextEdit`](TextEdit.md) when `ProjectSettings.gui/common/show_focus_state_on_pointer_event` is set to `Text Input Controls`, or for any control when it is set to `Always`.

**Note:** Using this method together with `Callable.CallDeferred` makes it more reliable, especially when called inside [`Node.Ready`](Node.md).

### GrabFocus

```csharp
public void GrabFocus(bool hideFocus = false)
```

引擎名：`grab_focus`

Steal the focus from another control and become the focused control (see `FocusMode`).

If `hideFocus` is `true`, the control will not visually show its focused state. Has no effect for [`LineEdit`](LineEdit.md) and [`TextEdit`](TextEdit.md) when `ProjectSettings.gui/common/show_focus_state_on_pointer_event` is set to `Text Input Controls`, or for any control when it is set to `Always`.

**Note:** Using this method together with `Callable.CallDeferred` makes it more reliable, especially when called inside [`Node.Ready`](Node.md).

### HasFocus

```csharp
public bool HasFocus()
```

引擎名：`has_focus`

Returns `true` if this is the current focused control. See `FocusMode`.

If `ignoreHiddenFocus` is `true`, controls that have their focus hidden will always return `false`. Hidden focus happens automatically when controls gain focus via mouse input, or manually using `GrabFocus()` with `hide_focus` set to `true`.

### HasFocus

```csharp
public bool HasFocus(bool ignoreHiddenFocus = false)
```

引擎名：`has_focus`

Returns `true` if this is the current focused control. See `FocusMode`.

If `ignoreHiddenFocus` is `true`, controls that have their focus hidden will always return `false`. Hidden focus happens automatically when controls gain focus via mouse input, or manually using `GrabFocus()` with `hide_focus` set to `true`.

### HasThemeColor

```csharp
public bool HasThemeColor(StringName name, StringName themeType = null)
```

引擎名：`has_theme_color`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a color item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### HasThemeColorOverride

```csharp
public bool HasThemeColorOverride(StringName name)
```

引擎名：`has_theme_color_override`

Returns `true` if there is a local override for a theme [`Color`](Color.md) with the specified `name` in this `Control` node.

See `AddThemeColorOverride()`.

### HasThemeConstant

```csharp
public bool HasThemeConstant(StringName name, StringName themeType = null)
```

引擎名：`has_theme_constant`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a constant item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### HasThemeConstantOverride

```csharp
public bool HasThemeConstantOverride(StringName name)
```

引擎名：`has_theme_constant_override`

Returns `true` if there is a local override for a theme constant with the specified `name` in this `Control` node.

See `AddThemeConstantOverride()`.

### HasThemeFont

```csharp
public bool HasThemeFont(StringName name, StringName themeType = null)
```

引擎名：`has_theme_font`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a font item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### HasThemeFontOverride

```csharp
public bool HasThemeFontOverride(StringName name)
```

引擎名：`has_theme_font_override`

Returns `true` if there is a local override for a theme [`Font`](Font.md) with the specified `name` in this `Control` node.

See `AddThemeFontOverride()`.

### HasThemeFontSize

```csharp
public bool HasThemeFontSize(StringName name, StringName themeType = null)
```

引擎名：`has_theme_font_size`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a font size item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### HasThemeFontSizeOverride

```csharp
public bool HasThemeFontSizeOverride(StringName name)
```

引擎名：`has_theme_font_size_override`

Returns `true` if there is a local override for a theme font size with the specified `name` in this `Control` node.

See `AddThemeFontSizeOverride()`.

### HasThemeIcon

```csharp
public bool HasThemeIcon(StringName name, StringName themeType = null)
```

引擎名：`has_theme_icon`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has an icon item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### HasThemeIconOverride

```csharp
public bool HasThemeIconOverride(StringName name)
```

引擎名：`has_theme_icon_override`

Returns `true` if there is a local override for a theme icon with the specified `name` in this `Control` node.

See `AddThemeIconOverride()`.

### HasThemeStylebox

```csharp
public bool HasThemeStylebox(StringName name, StringName themeType = null)
```

引擎名：`has_theme_stylebox`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a stylebox item with the specified `name` and `themeType`.

See `GetThemeColor()` for details.

### HasThemeStyleboxOverride

```csharp
public bool HasThemeStyleboxOverride(StringName name)
```

引擎名：`has_theme_stylebox_override`

Returns `true` if there is a local override for a theme [`StyleBox`](StyleBox.md) with the specified `name` in this `Control` node.

See `AddThemeStyleboxOverride()`.

### IsAutoTranslating

```csharp
public bool IsAutoTranslating()
```

引擎名：`is_auto_translating`

### IsClippingContents

```csharp
public bool IsClippingContents()
```

引擎名：`is_clipping_contents`

### IsDragSuccessful

```csharp
public bool IsDragSuccessful()
```

引擎名：`is_drag_successful`

Returns `true` if a drag operation is successful. Alternative to [`Viewport.GuiIsDragSuccessful`](Viewport.md).

Best used with [`Node.NotificationDragEnd`](Node.md).

### IsForcePassScrollEvents

```csharp
public bool IsForcePassScrollEvents()
```

引擎名：`is_force_pass_scroll_events`

### IsLayoutRtl

```csharp
public bool IsLayoutRtl()
```

引擎名：`is_layout_rtl`

Returns `true` if the layout is right-to-left. See also `LayoutDirection`.

### IsLocalizingNumeralSystem

```csharp
public bool IsLocalizingNumeralSystem()
```

引擎名：`is_localizing_numeral_system`

### IsOffsetTransformEnabled

```csharp
public bool IsOffsetTransformEnabled()
```

引擎名：`is_offset_transform_enabled`

### IsOffsetTransformVisualOnly

```csharp
public bool IsOffsetTransformVisualOnly()
```

引擎名：`is_offset_transform_visual_only`

### IsPropagatingMaximumSize

```csharp
public bool IsPropagatingMaximumSize()
```

引擎名：`is_propagating_maximum_size`

### ReleaseFocus

```csharp
public void ReleaseFocus()
```

引擎名：`release_focus`

Give up the focus. No other control will be able to receive input.

### RemoveThemeColorOverride

```csharp
public void RemoveThemeColorOverride(StringName name)
```

引擎名：`remove_theme_color_override`

Removes a local override for a theme [`Color`](Color.md) with the specified `name` previously added by `AddThemeColorOverride()` or via the Inspector dock.

### RemoveThemeConstantOverride

```csharp
public void RemoveThemeConstantOverride(StringName name)
```

引擎名：`remove_theme_constant_override`

Removes a local override for a theme constant with the specified `name` previously added by `AddThemeConstantOverride()` or via the Inspector dock.

### RemoveThemeFontOverride

```csharp
public void RemoveThemeFontOverride(StringName name)
```

引擎名：`remove_theme_font_override`

Removes a local override for a theme [`Font`](Font.md) with the specified `name` previously added by `AddThemeFontOverride()` or via the Inspector dock.

### RemoveThemeFontSizeOverride

```csharp
public void RemoveThemeFontSizeOverride(StringName name)
```

引擎名：`remove_theme_font_size_override`

Removes a local override for a theme font size with the specified `name` previously added by `AddThemeFontSizeOverride()` or via the Inspector dock.

### RemoveThemeIconOverride

```csharp
public void RemoveThemeIconOverride(StringName name)
```

引擎名：`remove_theme_icon_override`

Removes a local override for a theme icon with the specified `name` previously added by `AddThemeIconOverride()` or via the Inspector dock.

### RemoveThemeStyleboxOverride

```csharp
public void RemoveThemeStyleboxOverride(StringName name)
```

引擎名：`remove_theme_stylebox_override`

Removes a local override for a theme [`StyleBox`](StyleBox.md) with the specified `name` previously added by `AddThemeStyleboxOverride()` or via the Inspector dock.

### ResetSize

```csharp
public void ResetSize()
```

引擎名：`reset_size`

Resets the size to `GetCombinedMinimumSize()`. This is equivalent to calling `set_size(Vector2())` (or any size below the minimum).

### SetAccessibilityControlsNodes

```csharp
public void SetAccessibilityControlsNodes(Array<NodePath> nodePath)
```

引擎名：`set_accessibility_controls_nodes`

### SetAccessibilityDescribedByNodes

```csharp
public void SetAccessibilityDescribedByNodes(Array<NodePath> nodePath)
```

引擎名：`set_accessibility_described_by_nodes`

### SetAccessibilityDescription

```csharp
public void SetAccessibilityDescription(string description)
```

引擎名：`set_accessibility_description`

### SetAccessibilityFlowToNodes

```csharp
public void SetAccessibilityFlowToNodes(Array<NodePath> nodePath)
```

引擎名：`set_accessibility_flow_to_nodes`

### SetAccessibilityLabeledByNodes

```csharp
public void SetAccessibilityLabeledByNodes(Array<NodePath> nodePath)
```

引擎名：`set_accessibility_labeled_by_nodes`

### SetAccessibilityLive

```csharp
public void SetAccessibilityLive(AccessibilityServer.AccessibilityLiveMode mode)
```

引擎名：`set_accessibility_live`

### SetAccessibilityLive

```csharp
public void SetAccessibilityLive(DisplayServer.AccessibilityLiveMode mode)
```

引擎名：`set_accessibility_live`

### SetAccessibilityName

```csharp
public void SetAccessibilityName(string name)
```

引擎名：`set_accessibility_name`

### SetAnchor

```csharp
public void SetAnchor(Side side, float anchor, bool keepOffset = false, bool pushOppositeAnchor = true)
```

引擎名：`set_anchor`

Sets the anchor for the specified [`Side`](Side.md) to `anchor`. A setter method for `AnchorBottom`, `AnchorLeft`, `AnchorRight` and `AnchorTop`.

If `keepOffset` is `true`, offsets aren't updated after this operation.

If `pushOppositeAnchor` is `true` and the opposite anchor overlaps this anchor, the opposite one will have its value overridden. For example, when setting left anchor to 1 and the right anchor has value of 0.5, the right anchor will also get value of 1. If `pushOppositeAnchor` was `false`, the left anchor would get value 0.5.

### SetAnchorAndOffset

```csharp
public void SetAnchorAndOffset(Side side, float anchor, float offset, bool pushOppositeAnchor = false)
```

引擎名：`set_anchor_and_offset`

Works the same as `SetAnchor()`, but instead of `keep_offset` argument and automatic update of offset, it allows to set the offset yourself (see `SetOffset()`).

### SetAnchorsAndOffsetsPreset

```csharp
public void SetAnchorsAndOffsetsPreset(Control.LayoutPreset preset, Control.LayoutPresetMode resizeMode = Control.LayoutPresetMode.Minsize, int margin = 0)
```

引擎名：`set_anchors_and_offsets_preset`

Sets both anchor preset and offset preset. See `SetAnchorsPreset()` and `SetOffsetsPreset()`.

### SetAnchorsPreset

```csharp
public void SetAnchorsPreset(Control.LayoutPreset preset, bool keepOffsets = false)
```

引擎名：`set_anchors_preset`

Sets the anchors to a `preset` from [`Control.LayoutPreset`](Control.md) enum. This is the code equivalent to using the Layout menu in the 2D editor.

If `keepOffsets` is `true`, control's position will also be updated.

### SetAutoTranslate

```csharp
public void SetAutoTranslate(bool enable)
```

引擎名：`set_auto_translate`

### SetBegin

```csharp
public void SetBegin(Vector2 position)
```

引擎名：`set_begin`

Sets `OffsetLeft` and `OffsetTop` at the same time. Equivalent of changing `Position`.

### SetClipContents

```csharp
public void SetClipContents(bool enable)
```

引擎名：`set_clip_contents`

### SetCustomMaximumSize

```csharp
public void SetCustomMaximumSize(Vector2 size)
```

引擎名：`set_custom_maximum_size`

### SetCustomMinimumSize

```csharp
public void SetCustomMinimumSize(Vector2 size)
```

引擎名：`set_custom_minimum_size`

### SetDefaultCursorShape

```csharp
public void SetDefaultCursorShape(Control.CursorShape shape)
```

引擎名：`set_default_cursor_shape`

### SetDragForwarding

```csharp
public void SetDragForwarding(Callable dragFunc, Callable canDropFunc, Callable dropFunc)
```

引擎名：`set_drag_forwarding`

Sets the given callables to be used instead of the control's own drag-and-drop virtual methods. If a callable is empty, its respective virtual method is used as normal.

The arguments for each callable should be exactly the same as their respective virtual methods, which would be:

- `dragFunc` corresponds to `_GetDragData()` and requires a [`Vector2`](Vector2.md);

- `canDropFunc` corresponds to `_CanDropData()` and requires both a [`Vector2`](Vector2.md) and a [`Variant`](Variant.md);

- `dropFunc` corresponds to `_DropData()` and requires both a [`Vector2`](Vector2.md) and a [`Variant`](Variant.md).

### SetDragPreview

```csharp
public void SetDragPreview(Control control)
```

引擎名：`set_drag_preview`

Shows the given control at the mouse pointer. A good time to call this method is in `_GetDragData()`. The control must not be in the scene tree. You should not free the control, and you should not keep a reference to the control beyond the duration of the drag. It will be deleted automatically after the drag has ended.

```csharp
[Export]
private Color _color = new Color(1, 0, 0, 1);

public override Variant _GetDragData(Vector2 atPosition)
{
// Use a control that is not in the tree
var cpb = new ColorPickerButton();
cpb.Color = _color;
cpb.Size = new Vector2(50, 50);
SetDragPreview(cpb);
return _color;
}

```

### SetEnd

```csharp
public void SetEnd(Vector2 position)
```

引擎名：`set_end`

Sets `OffsetRight` and `OffsetBottom` at the same time.

### SetFocusBehaviorRecursive

```csharp
public void SetFocusBehaviorRecursive(Control.FocusBehaviorRecursiveEnum focusBehaviorRecursive)
```

引擎名：`set_focus_behavior_recursive`

### SetFocusMode

```csharp
public void SetFocusMode(Control.FocusModeEnum mode)
```

引擎名：`set_focus_mode`

### SetFocusNeighbor

```csharp
public void SetFocusNeighbor(Side side, NodePath neighbor)
```

引擎名：`set_focus_neighbor`

Sets the focus neighbor for the specified [`Side`](Side.md) to the `Control` at `neighbor` node path. A setter method for `FocusNeighborBottom`, `FocusNeighborLeft`, `FocusNeighborRight` and `FocusNeighborTop`.

### SetFocusNext

```csharp
public void SetFocusNext(NodePath next)
```

引擎名：`set_focus_next`

### SetFocusPrevious

```csharp
public void SetFocusPrevious(NodePath previous)
```

引擎名：`set_focus_previous`

### SetForcePassScrollEvents

```csharp
public void SetForcePassScrollEvents(bool forcePassScrollEvents)
```

引擎名：`set_force_pass_scroll_events`

### SetGlobalPosition

```csharp
public void SetGlobalPosition(Vector2 position, bool keepOffsets = false)
```

引擎名：`set_global_position`

Sets the `GlobalPosition` to given `position`.

If `keepOffsets` is `true`, control's anchors will be updated instead of offsets.

### SetHGrowDirection

```csharp
public void SetHGrowDirection(Control.GrowDirection direction)
```

引擎名：`set_h_grow_direction`

### SetHSizeFlags

```csharp
public void SetHSizeFlags(Control.SizeFlags flags)
```

引擎名：`set_h_size_flags`

### SetLayoutDirection

```csharp
public void SetLayoutDirection(Control.LayoutDirectionEnum direction)
```

引擎名：`set_layout_direction`

### SetLocalizeNumeralSystem

```csharp
public void SetLocalizeNumeralSystem(bool enable)
```

引擎名：`set_localize_numeral_system`

### SetMouseBehaviorRecursive

```csharp
public void SetMouseBehaviorRecursive(Control.MouseBehaviorRecursiveEnum mouseBehaviorRecursive)
```

引擎名：`set_mouse_behavior_recursive`

### SetMouseFilter

```csharp
public void SetMouseFilter(Control.MouseFilterEnum filter)
```

引擎名：`set_mouse_filter`

### SetOffset

```csharp
public void SetOffset(Side side, float offset)
```

引擎名：`set_offset`

Sets the offset for the specified [`Side`](Side.md) to `offset`. A setter method for `OffsetBottom`, `OffsetLeft`, `OffsetRight` and `OffsetTop`.

### SetOffsetTransformEnabled

```csharp
public void SetOffsetTransformEnabled(bool enabled)
```

引擎名：`set_offset_transform_enabled`

### SetOffsetTransformPivot

```csharp
public void SetOffsetTransformPivot(Vector2 pivot)
```

引擎名：`set_offset_transform_pivot`

### SetOffsetTransformPivotRatio

```csharp
public void SetOffsetTransformPivotRatio(Vector2 pivot)
```

引擎名：`set_offset_transform_pivot_ratio`

### SetOffsetTransformPosition

```csharp
public void SetOffsetTransformPosition(Vector2 offset)
```

引擎名：`set_offset_transform_position`

### SetOffsetTransformPositionRatio

```csharp
public void SetOffsetTransformPositionRatio(Vector2 offset)
```

引擎名：`set_offset_transform_position_ratio`

### SetOffsetTransformRotation

```csharp
public void SetOffsetTransformRotation(float rotation)
```

引擎名：`set_offset_transform_rotation`

### SetOffsetTransformScale

```csharp
public void SetOffsetTransformScale(Vector2 scale)
```

引擎名：`set_offset_transform_scale`

### SetOffsetTransformVisualOnly

```csharp
public void SetOffsetTransformVisualOnly(bool enabled)
```

引擎名：`set_offset_transform_visual_only`

### SetOffsetsPreset

```csharp
public void SetOffsetsPreset(Control.LayoutPreset preset, Control.LayoutPresetMode resizeMode = Control.LayoutPresetMode.Minsize, int margin = 0)
```

引擎名：`set_offsets_preset`

Sets the offsets to a `preset` from [`Control.LayoutPreset`](Control.md) enum. This is the code equivalent to using the Layout menu in the 2D editor.

Use parameter `resizeMode` with constants from [`Control.LayoutPresetMode`](Control.md) to better determine the resulting size of the `Control`. Constant size will be ignored if used with presets that change size, e.g. [`Control.LayoutPreset.LeftWide`](Control.md).

Use parameter `margin` to determine the gap between the `Control` and the edges.

### SetPivotOffset

```csharp
public void SetPivotOffset(Vector2 pivotOffset)
```

引擎名：`set_pivot_offset`

### SetPivotOffsetRatio

```csharp
public void SetPivotOffsetRatio(Vector2 ratio)
```

引擎名：`set_pivot_offset_ratio`

### SetPosition

```csharp
public void SetPosition(Vector2 position, bool keepOffsets = false)
```

引擎名：`set_position`

Sets the `Position` to given `position`.

If `keepOffsets` is `true`, control's anchors will be updated instead of offsets.

### SetPropagateMaximumSize

```csharp
public void SetPropagateMaximumSize(bool enable)
```

引擎名：`set_propagate_maximum_size`

### SetRotation

```csharp
public void SetRotation(float radians)
```

引擎名：`set_rotation`

### SetRotationDegrees

```csharp
public void SetRotationDegrees(float degrees)
```

引擎名：`set_rotation_degrees`

### SetScale

```csharp
public void SetScale(Vector2 scale)
```

引擎名：`set_scale`

### SetShortcutContext

```csharp
public void SetShortcutContext(Node node)
```

引擎名：`set_shortcut_context`

### SetSize

```csharp
public void SetSize(Vector2 size, bool keepOffsets = false)
```

引擎名：`set_size`

Sets the size (see `Size`).

If `keepOffsets` is `true`, control's anchors will be updated instead of offsets.

### SetStretchRatio

```csharp
public void SetStretchRatio(float ratio)
```

引擎名：`set_stretch_ratio`

### SetTheme

```csharp
public void SetTheme(Theme theme)
```

引擎名：`set_theme`

### SetThemeTypeVariation

```csharp
public void SetThemeTypeVariation(StringName themeType)
```

引擎名：`set_theme_type_variation`

### SetTooltipAutoTranslateMode

```csharp
public void SetTooltipAutoTranslateMode(Node.AutoTranslateModeEnum mode)
```

引擎名：`set_tooltip_auto_translate_mode`

### SetTooltipText

```csharp
public void SetTooltipText(string hint)
```

引擎名：`set_tooltip_text`

### SetTranslationContext

```csharp
public void SetTranslationContext(StringName context)
```

引擎名：`set_translation_context`

### SetVGrowDirection

```csharp
public void SetVGrowDirection(Control.GrowDirection direction)
```

引擎名：`set_v_grow_direction`

### SetVSizeFlags

```csharp
public void SetVSizeFlags(Control.SizeFlags flags)
```

引擎名：`set_v_size_flags`

### UpdateMaximumSize

```csharp
public void UpdateMaximumSize()
```

引擎名：`update_maximum_size`

Invalidates the maximum size cache in this node and in parent nodes up to top level. Intended to be used with `GetMaximumSize()` when the return value is changed. Setting `CustomMaximumSize` directly calls this method automatically.

**Note:** Calling this method also calls `UpdateMinimumSize()` since the combined minimum size may be affected by the maximum size change.

### UpdateMinimumSize

```csharp
public void UpdateMinimumSize()
```

引擎名：`update_minimum_size`

Invalidates the minimum size cache in this node and in parent nodes up to top level. Intended to be used with `GetMinimumSize()` when the return value is changed. Setting `CustomMinimumSize` directly calls this method automatically.

### WarpMouse

```csharp
public void WarpMouse(Vector2 position)
```

引擎名：`warp_mouse`

Moves the mouse cursor to `position`, relative to `Position` of this `Control`.

**Note:** `WarpMouse()` is only supported on Windows, macOS and Linux. It has no effect on Android, iOS and Web.

### _AccessibilityGetContextualInfo

```csharp
public virtual string _AccessibilityGetContextualInfo()
```

引擎名：`_accessibility_get_contextual_info`

Return the description of the keyboard shortcuts and other contextual help for this control.

### _CanDropData

```csharp
public virtual bool _CanDropData(Vector2 atPosition, Variant data)
```

引擎名：`_can_drop_data`

Godot calls this method to test if `data` from a control's `_GetDragData()` can be dropped at `atPosition`. `atPosition` is local to this control.

This method should only be used to test the data. Process the data in `_DropData()`.

**Note:** If the drag was initiated by a keyboard shortcut or `AccessibilityDrag()`, `atPosition` is set to `Vector2.Inf`, and the currently selected item/text position should be used as the drop position.

```csharp
public override bool _CanDropData(Vector2 atPosition, Variant data)
{
// Check position if it is relevant to you
// Otherwise, just check data
return data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().ContainsKey("expected");
}

```

### _DropData

```csharp
public virtual void _DropData(Vector2 atPosition, Variant data)
```

引擎名：`_drop_data`

Godot calls this method to pass you the `data` from a control's `_GetDragData()` result. Godot first calls `_CanDropData()` to test if `data` is allowed to drop at `atPosition` where `atPosition` is local to this control.

**Note:** If the drag was initiated by a keyboard shortcut or `AccessibilityDrag()`, `atPosition` is set to `Vector2.Inf`, and the currently selected item/text position should be used as the drop position.

```csharp
public override bool _CanDropData(Vector2 atPosition, Variant data)
{
return data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().ContainsKey("color");
}

public override void _DropData(Vector2 atPosition, Variant data)
{
Color color = data.AsGodotDictionary()["color"].AsColor();
}

```

### _GetAccessibilityContainerName

```csharp
public virtual string _GetAccessibilityContainerName(Node node)
```

引擎名：`_get_accessibility_container_name`

Override this method to return a human-readable description of the position of the child `node` in the custom container, added to the `AccessibilityName`.

### _GetCursorShape

```csharp
public virtual int _GetCursorShape(Vector2 atPosition)
```

引擎名：`_get_cursor_shape`

Virtual method to be implemented by the user. Returns the cursor shape for the position `atPosition` in the control's local coordinates, which will typically be used while hovering over this control. See `GetCursorShape()`.

If not overridden, defaults to `MouseDefaultCursorShape`.

### _GetDragData

```csharp
public virtual Variant _GetDragData(Vector2 atPosition)
```

引擎名：`_get_drag_data`

Godot calls this method to get data that can be dragged and dropped onto controls that expect drop data. Returns `null` if there is no data to drag. Controls that want to receive drop data should implement `_CanDropData()` and `_DropData()`. `atPosition` is local to this control. Drag may be forced with `ForceDrag()`.

A preview that will follow the mouse that should represent the data can be set with `SetDragPreview()`. A good time to set the preview is in this method.

**Note:** If the drag was initiated by a keyboard shortcut or `AccessibilityDrag()`, `atPosition` is set to `Vector2.Inf`, and the currently selected item/text position should be used as the drag position.

```csharp
public override Variant _GetDragData(Vector2 atPosition)
{
var myData = MakeData(); // This is your custom method generating the drag data.
SetDragPreview(MakePreview(myData)); // This is your custom method generating the preview of the drag data.
return myData;
}

```

### _GetMaximumSize

```csharp
public virtual Vector2 _GetMaximumSize()
```

引擎名：`_get_maximum_size`

Virtual method to be implemented by the user. Returns the maximum size for this control. Alternative to `CustomMaximumSize` for controlling maximum size via code. The actual maximum size will be the max value of these two (in each axis separately).

If not overridden, defaults to `Vector2.Zero`.

**Note:** This method will not be called when the script is attached to a `Control` node that already overrides its maximum size (e.g. [`ScrollContainer`](ScrollContainer.md)).

**Note:** It is recommended to use `GetBoundMinimumSize()` instead of `GetCombinedMinimumSize()` when implementing this method, as the former respects maximum size limits when calculating the minimum size, while the latter does not.

### _GetMinimumSize

```csharp
public virtual Vector2 _GetMinimumSize()
```

引擎名：`_get_minimum_size`

Virtual method to be implemented by the user. Returns the minimum size for this control. Alternative to `CustomMinimumSize` for controlling minimum size via code. The actual minimum size will be the max value of these two (in each axis separately).

If not overridden, defaults to `Vector2.Zero`.

**Note:** This method will not be called when the script is attached to a `Control` node that already overrides its minimum size (e.g. [`Label`](Label.md), [`Button`](Button.md), [`PanelContainer`](PanelContainer.md) etc.). It can only be used with most basic GUI nodes, like `Control`, [`Container`](Container.md), [`Panel`](Panel.md) etc.

### _GetTooltip

```csharp
public virtual string _GetTooltip(Vector2 atPosition)
```

引擎名：`_get_tooltip`

Virtual method to be implemented by the user. Returns the tooltip text for the position `atPosition` in the control's local coordinates, which will typically appear when the cursor is resting over this control. See `GetTooltip()`.

**Note:** If this method returns an empty `System.String` and `_MakeCustomTooltip()` is not overridden, no tooltip is displayed.

### _GetTooltipAutoTranslateModeAt

```csharp
public virtual Node.AutoTranslateModeEnum _GetTooltipAutoTranslateModeAt(Vector2 atPosition)
```

引擎名：`_get_tooltip_auto_translate_mode_at`

Return the auto-translation mode at the given `atPosition`. If not implemented, the `TooltipAutoTranslateMode` property will be used instead.

### _GuiInput

```csharp
public virtual void _GuiInput(InputEvent event)
```

引擎名：`_gui_input`

Virtual method to be implemented by the user. Override this method to handle and accept inputs on UI elements. See also `AcceptEvent()`.

**Example:** Click on the control to print a message:

```csharp
public override void _GuiInput(InputEvent @event)
{
if (@event is InputEventMouseButton mb)
{
if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
{
GD.Print("I've been clicked D:");
}
}
}

```

If the `event` inherits [`InputEventMouse`](InputEventMouse.md), this method will **not** be called when:

- the control's `MouseFilter` is set to [`Control.MouseFilterEnum.Ignore`](Control.md);

- the control is obstructed by another control on top, that doesn't have `MouseFilter` set to [`Control.MouseFilterEnum.Ignore`](Control.md);

- the control's parent has `MouseFilter` set to [`Control.MouseFilterEnum.Stop`](Control.md) or has accepted the event;

- the control's parent has `ClipContents` enabled and the `event`'s position is outside the parent's rectangle;

- the `event`'s position is outside the control (see `_HasPoint()`).

**Note:** The `event`'s position is relative to this control's origin.

### _HasPoint

```csharp
public virtual bool _HasPoint(Vector2 point)
```

引擎名：`_has_point`

Virtual method to be implemented by the user. Returns whether the given `point` is inside this control.

If not overridden, default behavior is checking if the point is within the control's Rect.

**Note:** If you want to check if a point is inside the control, you can use `Rect2(Vector2.ZERO, size).has_point(point)`.

### _MakeCustomTooltip

```csharp
public virtual GodotObject _MakeCustomTooltip(string forText)
```

引擎名：`_make_custom_tooltip`

Virtual method to be implemented by the user. Returns a `Control` node that should be used as a tooltip instead of the default one. `forText` is the return value of `GetTooltip()`.

The returned node must be of type `Control` or Control-derived. It can have child nodes of any type. It is freed when the tooltip disappears, so make sure you always provide a new instance (if you want to use a pre-existing node from your scene tree, you can duplicate it and pass the duplicated instance). When `null` or a non-Control node is returned, the default tooltip will be used instead.

The returned node will be added as child to a [`PopupPanel`](PopupPanel.md), so you should only provide the contents of that panel. That [`PopupPanel`](PopupPanel.md) can be themed using [`Theme.SetStylebox`](Theme.md) for the type `"TooltipPanel"` (see `TooltipText` for an example).

**Note:** The tooltip is shrunk to minimal size. If you want to ensure it's fully visible, you might want to set its `CustomMinimumSize` to some non-zero value.

**Note:** The node (and any relevant children) should have their [`CanvasItem.Visible`](CanvasItem.md) set to `true` when returned, otherwise, the viewport that instantiates it will not be able to calculate its minimum size reliably.

**Note:** If overridden, this method is called even if `GetTooltip()` returns an empty string. When this happens with the default tooltip, it is not displayed. To copy this behavior, return `null` in this method when `forText` is empty.

**Example:** Use a constructed node as a tooltip:

```csharp
public override Control _MakeCustomTooltip(string forText)
{
var label = new Label();
label.Text = forText;
return label;
}

```

**Example:** Use a scene instance as a tooltip:

```csharp
public override Control _MakeCustomTooltip(string forText)
{
Node tooltip = ResourceLoader.Load<PackedScene>("res://some_tooltip_scene.tscn").Instantiate();
tooltip.GetNode<Label>("Label").Text = forText;
return tooltip;
}

```

### _StructuredTextParser

```csharp
public virtual Array<Vector3I> _StructuredTextParser(Array args, string text)
```

引擎名：`_structured_text_parser`

User defined BiDi algorithm override function.

Returns an [`Collections.Array`](Collections.Array.md) of [`Vector3I`](Vector3I.md) text ranges and text base directions, in the left-to-right order. Ranges should cover full source `text` without overlaps. BiDi algorithm will be used on each range separately.

## 事件（信号）

### FocusEntered

```csharp
public event Action FocusEntered
```

引擎名：`focus_entered`

Emitted when the node gains focus.

### FocusExited

```csharp
public event Action FocusExited
```

引擎名：`focus_exited`

Emitted when the node loses focus.

### GuiInput

```csharp
public event Control.GuiInputEventHandler GuiInput
```

引擎名：`gui_input`

Emitted when the node receives an [`InputEvent`](InputEvent.md).

### MaximumSizeChanged

```csharp
public event Action MaximumSizeChanged
```

引擎名：`maximum_size_changed`

Emitted when the node's maximum size changes.

### MinimumSizeChanged

```csharp
public event Action MinimumSizeChanged
```

引擎名：`minimum_size_changed`

Emitted when the node's minimum size changes.

### MouseEntered

```csharp
public event Action MouseEntered
```

引擎名：`mouse_entered`

Emitted when the mouse cursor enters the control's (or any child control's) visible area, that is not occluded behind other Controls or Windows, provided its `MouseFilter` lets the event reach it and regardless if it's currently focused or not.

**Note:** [`CanvasItem.ZIndex`](CanvasItem.md) doesn't affect, which Control receives the signal.

### MouseExited

```csharp
public event Action MouseExited
```

引擎名：`mouse_exited`

Emitted when the mouse cursor leaves the control's (and all child control's) visible area, that is not occluded behind other Controls or Windows, provided its `MouseFilter` lets the event reach it and regardless if it's currently focused or not.

**Note:** [`CanvasItem.ZIndex`](CanvasItem.md) doesn't affect, which Control receives the signal.

**Note:** If you want to check whether the mouse truly left the area, ignoring any top nodes, you can use code like this:

```csharp
func _on_mouse_exited():
if not Rect2(Vector2(), size).has_point(get_local_mouse_position()):
# Not hovering over area.

```

### Resized

```csharp
public event Action Resized
```

引擎名：`resized`

Emitted when the control changes size.

### SizeFlagsChanged

```csharp
public event Action SizeFlagsChanged
```

引擎名：`size_flags_changed`

Emitted when one of the size flags changes. See `SizeFlagsHorizontal` and `SizeFlagsVertical`.

### ThemeChanged

```csharp
public event Action ThemeChanged
```

引擎名：`theme_changed`

Emitted when the `NotificationThemeChanged` notification is sent.

## 常量

### NotificationFocusEnter

```csharp
public const long NotificationFocusEnter = 43
```

Sent when the node grabs focus.

### NotificationFocusExit

```csharp
public const long NotificationFocusExit = 44
```

Sent when the node loses focus.

This notification is sent in reversed order.

### NotificationLayoutDirectionChanged

```csharp
public const long NotificationLayoutDirectionChanged = 49
```

Sent when the control layout direction is changed from LTR or RTL or vice versa. This notification is propagated to child Control nodes as result of a change to `LayoutDirection`.

### NotificationMouseEnter

```csharp
public const long NotificationMouseEnter = 41
```

Sent when the mouse cursor enters the control's (or any child control's) visible area, that is not occluded behind other Controls or Windows, provided its `MouseFilter` lets the event reach it and regardless if it's currently focused or not.

**Note:** [`CanvasItem.ZIndex`](CanvasItem.md) doesn't affect which Control receives the notification.

See also `NotificationMouseEnterSelf`.

### NotificationMouseEnterSelf

```csharp
public const long NotificationMouseEnterSelf = 60
```

Sent when the mouse cursor enters the control's visible area, that is not occluded behind other Controls or Windows, provided its `MouseFilter` lets the event reach it and regardless if it's currently focused or not.

**Note:** [`CanvasItem.ZIndex`](CanvasItem.md) doesn't affect which Control receives the notification.

See also `NotificationMouseEnter`.

### NotificationMouseExit

```csharp
public const long NotificationMouseExit = 42
```

Sent when the mouse cursor leaves the control's (and all child control's) visible area, that is not occluded behind other Controls or Windows, provided its `MouseFilter` lets the event reach it and regardless if it's currently focused or not.

**Note:** [`CanvasItem.ZIndex`](CanvasItem.md) doesn't affect which Control receives the notification.

See also `NotificationMouseExitSelf`.

### NotificationMouseExitSelf

```csharp
public const long NotificationMouseExitSelf = 61
```

Sent when the mouse cursor leaves the control's visible area, that is not occluded behind other Controls or Windows, provided its `MouseFilter` lets the event reach it and regardless if it's currently focused or not.

**Note:** [`CanvasItem.ZIndex`](CanvasItem.md) doesn't affect which Control receives the notification.

See also `NotificationMouseExit`.

### NotificationResized

```csharp
public const long NotificationResized = 40
```

Sent when the node changes size. Use `Size` to get the new size.

### NotificationScrollBegin

```csharp
public const long NotificationScrollBegin = 47
```

Sent when this node is inside a [`ScrollContainer`](ScrollContainer.md) which has begun being scrolled when dragging the scrollable area *with a touch event*. This notification is *not* sent when scrolling by dragging the scrollbar, scrolling with the mouse wheel or scrolling with keyboard/gamepad events.

**Note:** This signal is only emitted on Android or iOS, or on desktop/web platforms when `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse` is enabled.

### NotificationScrollEnd

```csharp
public const long NotificationScrollEnd = 48
```

Sent when this node is inside a [`ScrollContainer`](ScrollContainer.md) which has stopped being scrolled when dragging the scrollable area *with a touch event*. This notification is *not* sent when scrolling by dragging the scrollbar, scrolling with the mouse wheel or scrolling with keyboard/gamepad events.

**Note:** This signal is only emitted on Android or iOS, or on desktop/web platforms when `ProjectSettings.input_devices/pointing/emulate_touch_from_mouse` is enabled.

### NotificationThemeChanged

```csharp
public const long NotificationThemeChanged = 45
```

Sent when the node needs to refresh its theme items. This happens in one of the following cases:

- The `Theme` property is changed on this node or any of its ancestors.

- The `ThemeTypeVariation` property is changed on this node.

- One of the node's theme property overrides is changed.

- The node enters the scene tree.

**Note:** As an optimization, this notification won't be sent from changes that occur while this node is outside of the scene tree. Instead, all of the theme item updates can be applied at once when the node enters the scene tree.

**Note:** This notification is received alongside [`Node.NotificationEnterTree`](Node.md), so if you are instantiating a scene, the child nodes will not be initialized yet. You can use it to setup theming for this node, child nodes created from script, or if you want to access child nodes added in the editor, make sure the node is ready using [`Node.IsNodeReady`](Node.md).

```csharp
func _notification(what):
if what == NOTIFICATION_THEME_CHANGED:
if not is_node_ready():
await ready # Wait until ready signal.
$Label.add_theme_color_override("font_color", Color.YELLOW)

```

## Anchor

命名空间 `Godot`；enum

### 枚举值

- `Begin = 0`：Snaps one of the 4 anchor's sides to the origin of the node's `Rect`, in the top left.
- `End = 1`：Snaps one of the 4 anchor's sides to the end of the node's `Rect`, in the bottom right.

## CursorShape

命名空间 `Godot`；enum

### 枚举值

- `Arrow = 0`：Show the system's arrow mouse cursor when the user hovers the node.
- `Ibeam = 1`：Show the system's I-beam mouse cursor when the user hovers the node.
- `PointingHand = 2`：Show the system's pointing hand mouse cursor when the user hovers the node.
- `Cross = 3`：Show the system's cross mouse cursor when the user hovers the node.
- `Wait = 4`：Show the system's wait mouse cursor when the user hovers the node.
- `Busy = 5`：Show the system's busy mouse cursor when the user hovers the node.
- `Drag = 6`：Show the system's drag mouse cursor, often a closed fist or a cross symbol, when the user hovers the node.
- `CanDrop = 7`：Show the system's drop mouse cursor when the user hovers the node.
- `Forbidden = 8`：Show the system's forbidden mouse cursor when the user hovers the node.
- `Vsize = 9`：Show the system's vertical resize mouse cursor when the user hovers the node.
- `Hsize = 10`：Show the system's horizontal resize mouse cursor when the user hovers the node.
- `Bdiagsize = 11`：Show the system's window resize mouse cursor when the user hovers the node.
- `Fdiagsize = 12`：Show the system's window resize mouse cursor when the user hovers the node.
- `Move = 13`：Show the system's move mouse cursor when the user hovers the node.
- `Vsplit = 14`：Show the system's vertical split mouse cursor when the user hovers the node.
- `Hsplit = 15`：Show the system's horizontal split mouse cursor when the user hovers the node.
- `Help = 16`：Show the system's help mouse cursor when the user hovers the node, a question mark.

## FocusBehaviorRecursiveEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherited = 0`：Inherits the [`Control.FocusBehaviorRecursive`](Control.md) from the parent control.
- `Disabled = 1`：Prevents the control from getting focused.
- `Enabled = 2`：Allows the control to be focused, depending on the [`Control.FocusMode`](Control.md).

## FocusModeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The node cannot grab focus.
- `Click = 1`：The node can only grab focus on mouse clicks.
- `All = 2`：The node can grab focus on mouse click, using the arrows and the Tab keys on the keyboard, or using the D-pad buttons on a gamepad.
- `Accessibility = 3`：The node can grab focus only when screen reader is active.

## GrowDirection

命名空间 `Godot`；enum

### 枚举值

- `Begin = 0`：The control will grow to the left or top to make up if its minimum size is changed to be greater than its current size on the respective axis.
- `End = 1`：The control will grow to the right or bottom to make up if its minimum size is changed to be greater than its current size on the respective axis.
- `Both = 2`：The control will grow in both directions equally to make up if its minimum size is changed to be greater than its current size.

## GuiInputEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Control.GuiInput`](Control.md) event of a [`Control`](Control.md) class.

### 签名

#### Invoke

```csharp
delegate void GuiInputEventHandler(InputEvent event)
```

## LayoutDirectionEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherited = 0`：Automatic layout direction, determined from the parent control layout direction.
- `ApplicationLocale = 1`：Automatic layout direction, determined from the current locale.
- `Ltr = 2`：Left-to-right layout direction.
- `Rtl = 3`：Right-to-left layout direction.
- `SystemLocale = 4`：Automatic layout direction, determined from the system locale.
- `Max = 5`：Represents the size of the `Control.LayoutDirectionEnum` enum.
- `Locale = 1`

## LayoutPreset

命名空间 `Godot`；enum

### 枚举值

- `TopLeft = 0`：Snap all 4 anchors to the top-left of the parent control's bounds.
- `TopRight = 1`：Snap all 4 anchors to the top-right of the parent control's bounds.
- `BottomLeft = 2`：Snap all 4 anchors to the bottom-left of the parent control's bounds.
- `BottomRight = 3`：Snap all 4 anchors to the bottom-right of the parent control's bounds.
- `CenterLeft = 4`：Snap all 4 anchors to the center of the left edge of the parent control's bounds.
- `CenterTop = 5`：Snap all 4 anchors to the center of the top edge of the parent control's bounds.
- `CenterRight = 6`：Snap all 4 anchors to the center of the right edge of the parent control's bounds.
- `CenterBottom = 7`：Snap all 4 anchors to the center of the bottom edge of the parent control's bounds.
- `Center = 8`：Snap all 4 anchors to the center of the parent control's bounds.
- `LeftWide = 9`：Snap all 4 anchors to the left edge of the parent control.
- `TopWide = 10`：Snap all 4 anchors to the top edge of the parent control.
- `RightWide = 11`：Snap all 4 anchors to the right edge of the parent control.
- `BottomWide = 12`：Snap all 4 anchors to the bottom edge of the parent control.
- `VcenterWide = 13`：Snap all 4 anchors to a vertical line that cuts the parent control in half.
- `HcenterWide = 14`：Snap all 4 anchors to a horizontal line that cuts the parent control in half.
- `FullRect = 15`：Snap all 4 anchors to the respective corners of the parent control.

## LayoutPresetMode

命名空间 `Godot`；enum

### 枚举值

- `Minsize = 0`：The control will be resized to its minimum size.
- `KeepWidth = 1`：The control's width will not change.
- `KeepHeight = 2`：The control's height will not change.
- `KeepSize = 3`：The control's size will not change.

## MouseBehaviorRecursiveEnum

命名空间 `Godot`；enum

### 枚举值

- `Inherited = 0`：Inherits the [`Control.MouseBehaviorRecursive`](Control.md) from the parent control.
- `Disabled = 1`：Prevents the control from receiving mouse input.
- `Enabled = 2`：Allows the control to receive mouse input, depending on the [`Control.MouseFilter`](Control.md).

## MouseFilterEnum

命名空间 `Godot`；enum

### 枚举值

- `Stop = 0`：The control will receive mouse movement input events and mouse button input events if clicked on through [`Control.GuiInput`](Control.md).
- `Pass = 1`：The control will receive mouse movement input events and mouse button input events if clicked on through [`Control.GuiInput`](Control.md).
- `Ignore = 2`：The control will not receive any mouse movement input events nor mouse button input events through [`Control.GuiInput`](Control.md).

## SizeFlags

命名空间 `Godot`；enum

### 枚举值

- `ShrinkBegin = 0`：Tells the parent [`Container`](Container.md) to align the node with its start, either the top or the left edge.
- `Fill = 1`：Tells the parent [`Container`](Container.md) to expand the bounds of this node to fill all the available space without pushing any other node.
- `Expand = 2`：Tells the parent [`Container`](Container.md) to let this node take all the available space on the axis you flag.
- `ExpandFill = 3`：Sets the node's size flags to both fill and expand.
- `ShrinkCenter = 4`：Tells the parent [`Container`](Container.md) to center the node in the available space.
- `ShrinkEnd = 8`：Tells the parent [`Container`](Container.md) to align the node with its end, either the bottom or the right edge.

## TextDirection

命名空间 `Godot`；enum

### 枚举值

- `Inherited = 3`：Text writing direction is the same as layout direction.
- `Auto = 0`：Automatic text writing direction, determined from the current locale and text content.
- `Ltr = 1`：Left-to-right text writing direction.
- `Rtl = 2`：Right-to-left text writing direction.
