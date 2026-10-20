<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Window

命名空间 `Godot`；class；继承 [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node that creates a window. The window can either be a native system window or embedded inside another `Window` (see [`Viewport.GuiEmbedSubwindows`](Viewport.md)).

At runtime, `Window`s will not close automatically when requested. You need to handle it manually using the `CloseRequested` signal (this applies both to pressing the close button and clicking outside of a popup).

## 构造函数

### Window

```csharp
public Window()
```

## 属性

### AccessibilityDescription

```csharp
public string AccessibilityDescription { get; set; }
```

引擎名：`accessibility_description`

The human-readable node description that is reported to assistive apps.

### AccessibilityName

```csharp
public string AccessibilityName { get; set; }
```

引擎名：`accessibility_name`

The human-readable node name that is reported to assistive apps.

### AlwaysOnTop

```csharp
public bool AlwaysOnTop { get; set; }
```

引擎名：`always_on_top`

If `true`, the window will be on top of all other windows. Does not work if `Transient` is enabled.

### AutoTranslate

```csharp
public bool AutoTranslate { get; set; }
```

引擎名：`auto_translate`

Toggles if any text should automatically change to its translated version depending on the current locale.

### Borderless

```csharp
public bool Borderless { get; set; }
```

引擎名：`borderless`

If `true`, the window will have no borders.

### ContentScaleAspect

```csharp
public Window.ContentScaleAspectEnum ContentScaleAspect { get; set; }
```

引擎名：`content_scale_aspect`

Specifies how the content's aspect behaves when the `Window` is resized. The base aspect is determined by `ContentScaleSize`.

### ContentScaleFactor

```csharp
public float ContentScaleFactor { get; set; }
```

引擎名：`content_scale_factor`

Specifies the base scale of `Window`'s content when its `Size` is equal to `ContentScaleSize`. See also [`Viewport.GetStretchTransform`](Viewport.md).

### ContentScaleMode

```csharp
public Window.ContentScaleModeEnum ContentScaleMode { get; set; }
```

引擎名：`content_scale_mode`

Specifies how the content is scaled when the `Window` is resized.

### ContentScaleSize

```csharp
public Vector2I ContentScaleSize { get; set; }
```

引擎名：`content_scale_size`

The content's base size in "virtual" pixels. Not to be confused with `Size`, which sets the actual window's physical size in pixels. If set to a value greater than `0` and `ContentScaleMode` is set to a value other than [`Window.ContentScaleModeEnum.Disabled`](Window.md), the `Window`'s content will be scaled when the window is resized to a different size. Higher values will make the content appear *smaller*, as it will be able to fit more of the project in view. On the root `Window`, this is set to match `ProjectSettings.display/window/size/viewport_width` and `ProjectSettings.display/window/size/viewport_height` by default.

For example, when using [`Window.ContentScaleModeEnum.CanvasItems`](Window.md) and `ContentScaleSize` set to `Vector2i(1280, 720)`, using a window size of `2560×1440` will make 2D elements appear at double their original size, as the content is scaled by a factor of `2.0` (`2560.0 / 1280.0 = 2.0`, `1440.0 / 720.0 = 2.0`).

See [the Base size section of the Multiple resolutions documentation]($DOCS_URL/tutorials/rendering/multiple_resolutions.html#base-size) for details.

### ContentScaleStretch

```csharp
public Window.ContentScaleStretchEnum ContentScaleStretch { get; set; }
```

引擎名：`content_scale_stretch`

The policy to use to determine the final scale factor for 2D elements. This affects how `ContentScaleFactor` is applied, in addition to the automatic scale factor determined by `ContentScaleSize`.

### CurrentScreen

```csharp
public int CurrentScreen { get; set; }
```

引擎名：`current_screen`

The screen the window is currently on.

### ExcludeFromCapture

```csharp
public bool ExcludeFromCapture { get; set; }
```

引擎名：`exclude_from_capture`

If `true`, the `Window` is excluded from screenshots taken by [`DisplayServer.ScreenGetImage`](DisplayServer.md), [`DisplayServer.ScreenGetImageRect`](DisplayServer.md), and [`DisplayServer.ScreenGetPixel`](DisplayServer.md).

**Note:** This property is implemented on macOS and Windows.

**Note:** Enabling this setting will prevent standard screenshot methods from capturing a window image, but does **NOT** guarantee that other apps won't be able to capture an image. It should not be used as a DRM or security measure.

### Exclusive

```csharp
public bool Exclusive { get; set; }
```

引擎名：`exclusive`

If `true`, the `Window` will be in exclusive mode. Exclusive windows are always on top of their parent and will block all input going to the parent `Window`.

Needs `Transient` enabled to work.

### ExtendToTitle

```csharp
public bool ExtendToTitle { get; set; }
```

引擎名：`extend_to_title`

If `true`, the `Window` contents is expanded to the full size of the window, window title bar is transparent.

**Note:** This property is implemented only on macOS.

**Note:** This property only works with native windows.

### ForceNative

```csharp
public bool ForceNative { get; set; }
```

引擎名：`force_native`

If `true`, native window will be used regardless of parent viewport and project settings.

### HdrOutputRequested

```csharp
public bool HdrOutputRequested { get; set; }
```

引擎名：`hdr_output_requested`

If `true`, requests HDR output for the `Window`, falling back to SDR if not supported, and automatically switching between HDR and SDR as the window moves between screens, screen capabilities change, or system settings are modified. This will internally force [`Viewport.UseHdr2D`](Viewport.md) to be enabled on the main [`Viewport`](Viewport.md). All other [`SubViewport`](SubViewport.md) of this `Window` must have their [`Viewport.UseHdr2D`](Viewport.md) property enabled to produce HDR output.

### InitialPosition

```csharp
public Window.WindowInitialPosition InitialPosition { get; set; }
```

引擎名：`initial_position`

Specifies the initial type of position for the `Window`.

### KeepTitleVisible

```csharp
public bool KeepTitleVisible { get; set; }
```

引擎名：`keep_title_visible`

If `true`, the `Window` width is expanded to keep the title bar text fully visible.

### MaxSize

```csharp
public Vector2I MaxSize { get; set; }
```

引擎名：`max_size`

If non-zero, the `Window` can't be resized to be bigger than this size.

**Note:** This property will be ignored if the value is lower than `MinSize`.

### MaximizeDisabled

```csharp
public bool MaximizeDisabled { get; set; }
```

引擎名：`maximize_disabled`

If `true`, the `Window`'s maximize button is disabled.

**Note:** If both minimize and maximize buttons are disabled, buttons are fully hidden, and only close button is visible.

**Note:** This property is implemented only on macOS and Windows.

### MinSize

```csharp
public Vector2I MinSize { get; set; }
```

引擎名：`min_size`

If non-zero, the `Window` can't be resized to be smaller than this size.

**Note:** This property will be ignored in favor of `GetContentsMinimumSize()` if `WrapControls` is enabled and if its size is bigger.

### MinimizeDisabled

```csharp
public bool MinimizeDisabled { get; set; }
```

引擎名：`minimize_disabled`

If `true`, the `Window`'s minimize button is disabled.

**Note:** If both minimize and maximize buttons are disabled, buttons are fully hidden, and only close button is visible.

**Note:** This property is implemented only on macOS and Windows.

### Mode

```csharp
public Window.ModeEnum Mode { get; set; }
```

引擎名：`mode`

Set's the window's current mode.

**Note:** Fullscreen mode is not exclusive full screen on Windows and Linux.

**Note:** This method only works with native windows, i.e. the main window and `Window`-derived nodes when [`Viewport.GuiEmbedSubwindows`](Viewport.md) is disabled in the main viewport.

### MousePassthrough

```csharp
public bool MousePassthrough { get; set; }
```

引擎名：`mouse_passthrough`

If `true`, all mouse events will be passed to the underlying window of the same application. See also `MousePassthroughPolygon`.

**Note:** This property is implemented on Linux (X11), macOS and Windows.

**Note:** This property only works with native windows.

### MousePassthroughPolygon

```csharp
public Vector2[] MousePassthroughPolygon { get; set; }
```

引擎名：`mouse_passthrough_polygon`

Sets a polygonal region of the window which accepts mouse events. Mouse events outside the region will be passed through.

Passing an empty array will disable passthrough support (all mouse events will be intercepted by the window, which is the default behavior).

```csharp
// Set region, using Path2D node.
GetNode<Window>("Window").MousePassthroughPolygon = GetNode<Path2D>("Path2D").Curve.GetBakedPoints();

// Set region, using Polygon2D node.
GetNode<Window>("Window").MousePassthroughPolygon = GetNode<Polygon2D>("Polygon2D").Polygon;

// Reset region to default.
GetNode<Window>("Window").MousePassthroughPolygon = [];

```

**Note:** This property is ignored if `MousePassthrough` is set to `true`.

**Note:** On Windows, the portion of a window that lies outside the region is not drawn, while on Linux (X11) and macOS it is.

**Note:** This property is implemented on Linux (X11), macOS and Windows.

### NonclientArea

```csharp
public Rect2I NonclientArea { get; set; }
```

引擎名：`nonclient_area`

If set, defines the window's custom decoration area which will receive mouse input, even if normal input to the window is blocked (such as when it has an exclusive child opened). See also `NonclientWindowInput`.

### PopupWMHint

```csharp
public bool PopupWMHint { get; set; }
```

引擎名：`popup_wm_hint`

If `true`, the `Window` will signal to the window manager that it is supposed to be an implementation-defined "popup" (usually a floating, borderless, untileable and immovable child window).

### PopupWindow

```csharp
public bool PopupWindow { get; set; }
```

引擎名：`popup_window`

If `true`, the `Window` will be considered a popup. Popups are sub-windows that don't show as separate windows in system's window manager's window list and will send close request when anything is clicked outside of them (unless `Exclusive` is enabled).

### Position

```csharp
public Vector2I Position { get; set; }
```

引擎名：`position`

The window's position in pixels.

If `ProjectSettings.display/window/subwindows/embed_subwindows` is `false`, the position is in absolute screen coordinates. This typically applies to editor plugins. If the setting is `true`, the window's position is in the coordinates of its parent [`Viewport`](Viewport.md).

**Note:** This property only works if `InitialPosition` is set to [`Window.WindowInitialPosition.Absolute`](Window.md).

### SharpCorners

```csharp
public bool SharpCorners { get; set; }
```

引擎名：`sharp_corners`

If `true`, the `Window` will override the OS window style to display sharp corners.

**Note:** This property is implemented only on Windows (11).

**Note:** This property only works with native windows.

### Size

```csharp
public Vector2I Size { get; set; }
```

引擎名：`size`

The window's size in pixels. See also `ContentScaleSize`, which doesn't set the window's physical size but affects how scaling works relative to the current `ContentScaleMode`.

### Theme

```csharp
public Theme Theme { get; set; }
```

引擎名：`theme`

The [`Theme`](Theme.md) resource this node and all its [`Control`](Control.md) and `Window` children use. If a child node has its own [`Theme`](Theme.md) resource set, theme items are merged with child's definitions having higher priority.

**Note:** `Window` styles will have no effect unless the window is embedded.

### ThemeTypeVariation

```csharp
public StringName ThemeTypeVariation { get; set; }
```

引擎名：`theme_type_variation`

The name of a theme type variation used by this `Window` to look up its own theme items. See [`Control.ThemeTypeVariation`](Control.md) for more details.

### Title

```csharp
public string Title { get; set; }
```

引擎名：`title`

The window's title. If the `Window` is native, title styles set in [`Theme`](Theme.md) will have no effect.

### Transient

```csharp
public bool Transient { get; set; }
```

引擎名：`transient`

If `true`, the `Window` is transient, i.e. it's considered a child of another `Window`. The transient window will be destroyed with its transient parent and will return focus to their parent when closed. The transient window is displayed on top of a non-exclusive full-screen parent window. Transient windows can't enter full-screen mode.

Note that behavior might be different depending on the platform.

### TransientToFocused

```csharp
public bool TransientToFocused { get; set; }
```

引擎名：`transient_to_focused`

If `true`, and the `Window` is `Transient`, this window will (at the time of becoming visible) become transient to the currently focused window instead of the immediate parent window in the hierarchy. Note that the transient parent is assigned at the time this window becomes visible, so changing it afterwards has no effect until re-shown.

### Transparent

```csharp
public bool Transparent { get; set; }
```

引擎名：`transparent`

If `true`, the `Window`'s background can be transparent. This is best used with embedded windows.

**Note:** Transparency support is implemented on Linux, macOS and Windows, but availability might vary depending on GPU driver, display manager, and compositor capabilities.

**Note:** This property has no effect if `ProjectSettings.display/window/per_pixel_transparency/allowed` is set to `false`.

### Unfocusable

```csharp
public bool Unfocusable { get; set; }
```

引擎名：`unfocusable`

If `true`, the `Window` can't be focused nor interacted with. It can still be visible.

### Unresizable

```csharp
public bool Unresizable { get; set; }
```

引擎名：`unresizable`

If `true`, the window can't be resized.

### Visible

```csharp
public bool Visible { get; set; }
```

引擎名：`visible`

If `true`, the window is visible.

### WrapControls

```csharp
public bool WrapControls { get; set; }
```

引擎名：`wrap_controls`

If `true`, the window's size will automatically update when a child node is added or removed, ignoring `MinSize` if the new size is bigger.

If `false`, you need to call `ChildControlsChanged()` manually.

## 方法

### AddThemeColorOverride

```csharp
public void AddThemeColorOverride(StringName name, Color color)
```

引擎名：`add_theme_color_override`

Creates a local override for a theme [`Color`](Color.md) with the specified `name`. Local overrides always take precedence when fetching theme items for the control. An override can be removed with `RemoveThemeColorOverride()`.

See also `GetThemeColor()` and [`Control.AddThemeColorOverride`](Control.md) for more details.

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

See also `GetThemeStylebox()` and [`Control.AddThemeStyleboxOverride`](Control.md) for more details.

### BeginBulkThemeOverride

```csharp
public void BeginBulkThemeOverride()
```

引擎名：`begin_bulk_theme_override`

Prevents `*_theme_*_override` methods from emitting `NotificationThemeChanged` until `EndBulkThemeOverride()` is called.

### CanDraw

```csharp
public bool CanDraw()
```

引擎名：`can_draw`

Returns whether the window is being drawn to the screen.

### ChildControlsChanged

```csharp
public void ChildControlsChanged()
```

引擎名：`child_controls_changed`

Requests an update of the `Window` size to fit underlying [`Control`](Control.md) nodes.

### EndBulkThemeOverride

```csharp
public void EndBulkThemeOverride()
```

引擎名：`end_bulk_theme_override`

Ends a bulk theme override update. See `BeginBulkThemeOverride()`.

### GetAccessibilityDescription

```csharp
public string GetAccessibilityDescription()
```

引擎名：`get_accessibility_description`

### GetAccessibilityName

```csharp
public string GetAccessibilityName()
```

引擎名：`get_accessibility_name`

### GetContentScaleAspect

```csharp
public Window.ContentScaleAspectEnum GetContentScaleAspect()
```

引擎名：`get_content_scale_aspect`

### GetContentScaleFactor

```csharp
public float GetContentScaleFactor()
```

引擎名：`get_content_scale_factor`

### GetContentScaleMode

```csharp
public Window.ContentScaleModeEnum GetContentScaleMode()
```

引擎名：`get_content_scale_mode`

### GetContentScaleSize

```csharp
public Vector2I GetContentScaleSize()
```

引擎名：`get_content_scale_size`

### GetContentScaleStretch

```csharp
public Window.ContentScaleStretchEnum GetContentScaleStretch()
```

引擎名：`get_content_scale_stretch`

### GetContentsMinimumSize

```csharp
public Vector2 GetContentsMinimumSize()
```

引擎名：`get_contents_minimum_size`

Returns the combined minimum size from the child [`Control`](Control.md) nodes of the window. Use `ChildControlsChanged()` to update it when child nodes have changed.

The value returned by this method can be overridden with `_GetContentsMinimumSize()`.

### GetCurrentScreen

```csharp
public int GetCurrentScreen()
```

引擎名：`get_current_screen`

### GetFlag

```csharp
public bool GetFlag(Window.Flags flag)
```

引擎名：`get_flag`

Returns `true` if the `flag` is set.

### GetFocusedWindow

```csharp
public static Window GetFocusedWindow()
```

引擎名：`get_focused_window`

Returns the focused window.

### GetForceNative

```csharp
public bool GetForceNative()
```

引擎名：`get_force_native`

### GetInitialPosition

```csharp
public Window.WindowInitialPosition GetInitialPosition()
```

引擎名：`get_initial_position`

### GetKeepTitleVisible

```csharp
public bool GetKeepTitleVisible()
```

引擎名：`get_keep_title_visible`

### GetLayoutDirection

```csharp
public Window.LayoutDirection GetLayoutDirection()
```

引擎名：`get_layout_direction`

Returns layout direction and text writing direction.

### GetMaxSize

```csharp
public Vector2I GetMaxSize()
```

引擎名：`get_max_size`

### GetMinSize

```csharp
public Vector2I GetMinSize()
```

引擎名：`get_min_size`

### GetMode

```csharp
public Window.ModeEnum GetMode()
```

引擎名：`get_mode`

### GetMousePassthroughPolygon

```csharp
public Vector2[] GetMousePassthroughPolygon()
```

引擎名：`get_mouse_passthrough_polygon`

### GetNonclientArea

```csharp
public Rect2I GetNonclientArea()
```

引擎名：`get_nonclient_area`

### GetOutputMaxLinearValue

```csharp
public float GetOutputMaxLinearValue()
```

引擎名：`get_output_max_linear_value`

Returns the maximum value for linear color components that can be displayed in this window, regardless of SDR or HDR output. Returns `1.0` if HDR is not enabled or not supported. The `OutputMaxLinearValueChanged` signal will be emitted whenever this value changes.

This value is used by tonemapping and other [`Environment`](Environment.md) effects to ensure that bright colors are presented in the range that can be displayed by this window. When using this maximum linear value in your project, it should only be used to present colors directly to the screen without tonemapping and without influencing lighting, post-processing effects, or surrounding color. The following is an example that produces the brightest purple color that the screen can produce:

**Note:** You will need to convert sRGB colors to linear before multiplying by this value to get correct results.

### GetPosition

```csharp
public Vector2I GetPosition()
```

引擎名：`get_position`

### GetPositionWithDecorations

```csharp
public Vector2I GetPositionWithDecorations()
```

引擎名：`get_position_with_decorations`

Returns the window's position including its border.

**Note:** If `Visible` is `false`, this method returns the same value as `Position`.

### GetSize

```csharp
public Vector2I GetSize()
```

引擎名：`get_size`

### GetSizeWithDecorations

```csharp
public Vector2I GetSizeWithDecorations()
```

引擎名：`get_size_with_decorations`

Returns the window's size including its border.

**Note:** If `Visible` is `false`, this method returns the same value as `Size`.

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

Returns a [`Color`](Color.md) from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a color item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for more details.

### GetThemeConstant

```csharp
public int GetThemeConstant(StringName name, StringName themeType = null)
```

引擎名：`get_theme_constant`

Returns a constant from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a constant item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for more details.

### GetThemeDefaultBaseScale

```csharp
public float GetThemeDefaultBaseScale()
```

引擎名：`get_theme_default_base_scale`

Returns the default base scale value from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a valid [`Theme.DefaultBaseScale`](Theme.md) value.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeDefaultFont

```csharp
public Font GetThemeDefaultFont()
```

引擎名：`get_theme_default_font`

Returns the default font from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a valid [`Theme.DefaultFont`](Theme.md) value.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeDefaultFontSize

```csharp
public int GetThemeDefaultFontSize()
```

引擎名：`get_theme_default_font_size`

Returns the default font size value from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a valid [`Theme.DefaultFontSize`](Theme.md) value.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeFont

```csharp
public Font GetThemeFont(StringName name, StringName themeType = null)
```

引擎名：`get_theme_font`

Returns a [`Font`](Font.md) from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a font item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeFontSize

```csharp
public int GetThemeFontSize(StringName name, StringName themeType = null)
```

引擎名：`get_theme_font_size`

Returns a font size from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a font size item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeIcon

```csharp
public Texture2D GetThemeIcon(StringName name, StringName themeType = null)
```

引擎名：`get_theme_icon`

Returns an icon from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has an icon item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeStylebox

```csharp
public StyleBox GetThemeStylebox(StringName name, StringName themeType = null)
```

引擎名：`get_theme_stylebox`

Returns a [`StyleBox`](StyleBox.md) from the first matching [`Theme`](Theme.md) in the tree if that [`Theme`](Theme.md) has a stylebox item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### GetThemeTypeVariation

```csharp
public StringName GetThemeTypeVariation()
```

引擎名：`get_theme_type_variation`

### GetTitle

```csharp
public string GetTitle()
```

引擎名：`get_title`

### GetWindowId

```csharp
public int GetWindowId()
```

引擎名：`get_window_id`

Returns the ID of the window.

### GrabFocus

```csharp
public void GrabFocus()
```

引擎名：`grab_focus`

Causes the window to grab focus, allowing it to receive user input.

### HasFocus

```csharp
public bool HasFocus()
```

引擎名：`has_focus`

Returns `true` if the window is focused.

### HasThemeColor

```csharp
public bool HasThemeColor(StringName name, StringName themeType = null)
```

引擎名：`has_theme_color`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a color item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### HasThemeColorOverride

```csharp
public bool HasThemeColorOverride(StringName name)
```

引擎名：`has_theme_color_override`

Returns `true` if there is a local override for a theme [`Color`](Color.md) with the specified `name` in this [`Control`](Control.md) node.

See `AddThemeColorOverride()`.

### HasThemeConstant

```csharp
public bool HasThemeConstant(StringName name, StringName themeType = null)
```

引擎名：`has_theme_constant`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a constant item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### HasThemeConstantOverride

```csharp
public bool HasThemeConstantOverride(StringName name)
```

引擎名：`has_theme_constant_override`

Returns `true` if there is a local override for a theme constant with the specified `name` in this [`Control`](Control.md) node.

See `AddThemeConstantOverride()`.

### HasThemeFont

```csharp
public bool HasThemeFont(StringName name, StringName themeType = null)
```

引擎名：`has_theme_font`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a font item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### HasThemeFontOverride

```csharp
public bool HasThemeFontOverride(StringName name)
```

引擎名：`has_theme_font_override`

Returns `true` if there is a local override for a theme [`Font`](Font.md) with the specified `name` in this [`Control`](Control.md) node.

See `AddThemeFontOverride()`.

### HasThemeFontSize

```csharp
public bool HasThemeFontSize(StringName name, StringName themeType = null)
```

引擎名：`has_theme_font_size`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a font size item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### HasThemeFontSizeOverride

```csharp
public bool HasThemeFontSizeOverride(StringName name)
```

引擎名：`has_theme_font_size_override`

Returns `true` if there is a local override for a theme font size with the specified `name` in this [`Control`](Control.md) node.

See `AddThemeFontSizeOverride()`.

### HasThemeIcon

```csharp
public bool HasThemeIcon(StringName name, StringName themeType = null)
```

引擎名：`has_theme_icon`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has an icon item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### HasThemeIconOverride

```csharp
public bool HasThemeIconOverride(StringName name)
```

引擎名：`has_theme_icon_override`

Returns `true` if there is a local override for a theme icon with the specified `name` in this [`Control`](Control.md) node.

See `AddThemeIconOverride()`.

### HasThemeStylebox

```csharp
public bool HasThemeStylebox(StringName name, StringName themeType = null)
```

引擎名：`has_theme_stylebox`

Returns `true` if there is a matching [`Theme`](Theme.md) in the tree that has a stylebox item with the specified `name` and `themeType`.

See [`Control.GetThemeColor`](Control.md) for details.

### HasThemeStyleboxOverride

```csharp
public bool HasThemeStyleboxOverride(StringName name)
```

引擎名：`has_theme_stylebox_override`

Returns `true` if there is a local override for a theme [`StyleBox`](StyleBox.md) with the specified `name` in this [`Control`](Control.md) node.

See `AddThemeStyleboxOverride()`.

### Hide

```csharp
public void Hide()
```

引擎名：`hide`

Hides the window. This is not the same as minimized state. Hidden window can't be interacted with and needs to be made visible with `Show()`.

### IsAutoTranslating

```csharp
public bool IsAutoTranslating()
```

引擎名：`is_auto_translating`

### IsEmbedded

```csharp
public bool IsEmbedded()
```

引擎名：`is_embedded`

Returns `true` if the window is currently embedded in another window.

### IsExclusive

```csharp
public bool IsExclusive()
```

引擎名：`is_exclusive`

### IsHdrOutputRequested

```csharp
public bool IsHdrOutputRequested()
```

引擎名：`is_hdr_output_requested`

### IsLayoutRtl

```csharp
public bool IsLayoutRtl()
```

引擎名：`is_layout_rtl`

Returns `true` if the layout is right-to-left.

### IsMaximizeAllowed

```csharp
public bool IsMaximizeAllowed()
```

引擎名：`is_maximize_allowed`

Returns `true` if the window can be maximized (the maximize button is enabled).

### IsTransient

```csharp
public bool IsTransient()
```

引擎名：`is_transient`

### IsTransientToFocused

```csharp
public bool IsTransientToFocused()
```

引擎名：`is_transient_to_focused`

### IsUsingFontOversampling

```csharp
public bool IsUsingFontOversampling()
```

引擎名：`is_using_font_oversampling`

Returns `true` if font oversampling is enabled. See `SetUseFontOversampling()`.

### IsVisible

```csharp
public bool IsVisible()
```

引擎名：`is_visible`

### IsWrappingControls

```csharp
public bool IsWrappingControls()
```

引擎名：`is_wrapping_controls`

### MoveToCenter

```csharp
public void MoveToCenter()
```

引擎名：`move_to_center`

Centers the window in the current screen. If the window is embedded, it is centered in the embedder [`Viewport`](Viewport.md) instead.

### MoveToForeground

```csharp
public void MoveToForeground()
```

引擎名：`move_to_foreground`

Causes the window to grab focus, allowing it to receive user input.

### Popup

```csharp
public void Popup(Rect2I? rect = null)
```

引擎名：`popup`

Shows the `Window` and makes it transient (see `Transient`). If `rect` is provided, it will be set as the `Window`'s size. Fails if called on the main window.

If `ProjectSettings.display/window/subwindows/embed_subwindows` is `true` (single-window mode), `rect`'s coordinates are global and relative to the main window's top-left corner (excluding window decorations). If `rect`'s position coordinates are negative, the window will be located outside the main window and may not be visible as a result.

If `ProjectSettings.display/window/subwindows/embed_subwindows` is `false` (multi-window mode), `rect`'s coordinates are global and relative to the top-left corner of the leftmost screen. If `rect`'s position coordinates are negative, the window will be placed at the top-left corner of the screen.

**Note:** `rect` must be in global coordinates if specified.

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2I(new Vector2I(0, 0), new Vector2I(0, 0))`.

### PopupCentered

```csharp
public void PopupCentered(Vector2I? minsize = null)
```

引擎名：`popup_centered`

Popups the `Window` at the center of the current screen, with optionally given minimum size. If the `Window` is embedded, it will be centered in the parent [`Viewport`](Viewport.md) instead.

**Note:** Calling it with the default value of `minsize` is equivalent to calling it with `Size`.

参数：
- `minsize`：If the parameter is null, then the default value is `new Vector2I(0, 0)`.

### PopupCenteredClamped

```csharp
public void PopupCenteredClamped(Vector2I? minsize = null, float fallbackRatio = 0.75f)
```

引擎名：`popup_centered_clamped`

Popups the `Window` centered inside its parent `Window`. `fallbackRatio` determines the maximum size of the `Window`, in relation to its parent.

**Note:** Calling it with the default value of `minsize` is equivalent to calling it with `Size`.

参数：
- `minsize`：If the parameter is null, then the default value is `new Vector2I(0, 0)`.

### PopupCenteredRatio

```csharp
public void PopupCenteredRatio(float ratio = 0.8f)
```

引擎名：`popup_centered_ratio`

If `Window` is embedded, popups the `Window` centered inside its embedder and sets its size as a `ratio` of embedder's size.

If `Window` is a native window, popups the `Window` centered inside the screen of its parent `Window` and sets its size as a `ratio` of the screen size.

### PopupExclusive

```csharp
public void PopupExclusive(Node fromNode, Rect2I? rect = null)
```

引擎名：`popup_exclusive`

Attempts to parent this dialog to the last exclusive window relative to `fromNode`, and then calls `Popup()` on it. The dialog must have no current parent, otherwise the method fails.

See also `SetUnparentWhenInvisible()` and [`Node.GetLastExclusiveWindow`](Node.md).

参数：
- `rect`：If the parameter is null, then the default value is `new Rect2I(new Vector2I(0, 0), new Vector2I(0, 0))`.

### PopupExclusiveCentered

```csharp
public void PopupExclusiveCentered(Node fromNode, Vector2I? minsize = null)
```

引擎名：`popup_exclusive_centered`

Attempts to parent this dialog to the last exclusive window relative to `fromNode`, and then calls `PopupCentered()` on it. The dialog must have no current parent, otherwise the method fails.

See also `SetUnparentWhenInvisible()` and [`Node.GetLastExclusiveWindow`](Node.md).

参数：
- `minsize`：If the parameter is null, then the default value is `new Vector2I(0, 0)`.

### PopupExclusiveCenteredClamped

```csharp
public void PopupExclusiveCenteredClamped(Node fromNode, Vector2I? minsize = null, float fallbackRatio = 0.75f)
```

引擎名：`popup_exclusive_centered_clamped`

Attempts to parent this dialog to the last exclusive window relative to `fromNode`, and then calls `PopupCenteredClamped()` on it. The dialog must have no current parent, otherwise the method fails.

See also `SetUnparentWhenInvisible()` and [`Node.GetLastExclusiveWindow`](Node.md).

参数：
- `minsize`：If the parameter is null, then the default value is `new Vector2I(0, 0)`.

### PopupExclusiveCenteredRatio

```csharp
public void PopupExclusiveCenteredRatio(Node fromNode, float ratio = 0.8f)
```

引擎名：`popup_exclusive_centered_ratio`

Attempts to parent this dialog to the last exclusive window relative to `fromNode`, and then calls `PopupCenteredRatio()` on it. The dialog must have no current parent, otherwise the method fails.

See also `SetUnparentWhenInvisible()` and [`Node.GetLastExclusiveWindow`](Node.md).

### PopupExclusiveOnParent

```csharp
public void PopupExclusiveOnParent(Node fromNode, Rect2I parentRect)
```

引擎名：`popup_exclusive_on_parent`

Attempts to parent this dialog to the last exclusive window relative to `fromNode`, and then calls `PopupOnParent()` on it. The dialog must have no current parent, otherwise the method fails.

See also `SetUnparentWhenInvisible()` and [`Node.GetLastExclusiveWindow`](Node.md).

### PopupOnParent

```csharp
public void PopupOnParent(Rect2I parentRect)
```

引擎名：`popup_on_parent`

Popups the `Window` with a position shifted by parent `Window`'s position. If the `Window` is embedded, has the same effect as `Popup()`.

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

### RequestAttention

```csharp
public void RequestAttention()
```

引擎名：`request_attention`

Tells the OS that the `Window` needs an attention. This makes the window stand out in some way depending on the system, e.g. it might blink on the task bar.

### ResetSize

```csharp
public void ResetSize()
```

引擎名：`reset_size`

Resets the size to the minimum size, which is the max of `MinSize` and (if `WrapControls` is enabled) `GetContentsMinimumSize()`. This is equivalent to calling `set_size(Vector2i())` (or any size below the minimum).

### SetAccessibilityDescription

```csharp
public void SetAccessibilityDescription(string description)
```

引擎名：`set_accessibility_description`

### SetAccessibilityName

```csharp
public void SetAccessibilityName(string name)
```

引擎名：`set_accessibility_name`

### SetAutoTranslate

```csharp
public void SetAutoTranslate(bool enable)
```

引擎名：`set_auto_translate`

### SetContentScaleAspect

```csharp
public void SetContentScaleAspect(Window.ContentScaleAspectEnum aspect)
```

引擎名：`set_content_scale_aspect`

### SetContentScaleFactor

```csharp
public void SetContentScaleFactor(float factor)
```

引擎名：`set_content_scale_factor`

### SetContentScaleMode

```csharp
public void SetContentScaleMode(Window.ContentScaleModeEnum mode)
```

引擎名：`set_content_scale_mode`

### SetContentScaleSize

```csharp
public void SetContentScaleSize(Vector2I size)
```

引擎名：`set_content_scale_size`

### SetContentScaleStretch

```csharp
public void SetContentScaleStretch(Window.ContentScaleStretchEnum stretch)
```

引擎名：`set_content_scale_stretch`

### SetCurrentScreen

```csharp
public void SetCurrentScreen(int index)
```

引擎名：`set_current_screen`

### SetExclusive

```csharp
public void SetExclusive(bool exclusive)
```

引擎名：`set_exclusive`

### SetFlag

```csharp
public void SetFlag(Window.Flags flag, bool enabled)
```

引擎名：`set_flag`

Sets a specified window flag.

### SetForceNative

```csharp
public void SetForceNative(bool forceNative)
```

引擎名：`set_force_native`

### SetHdrOutputRequested

```csharp
public void SetHdrOutputRequested(bool requested)
```

引擎名：`set_hdr_output_requested`

### SetImeActive

```csharp
public void SetImeActive(bool active)
```

引擎名：`set_ime_active`

If `active` is `true`, enables system's native IME (Input Method Editor).

### SetImePosition

```csharp
public void SetImePosition(Vector2I position)
```

引擎名：`set_ime_position`

Moves IME to the given position.

### SetInitialPosition

```csharp
public void SetInitialPosition(Window.WindowInitialPosition initialPosition)
```

引擎名：`set_initial_position`

### SetKeepTitleVisible

```csharp
public void SetKeepTitleVisible(bool titleVisible)
```

引擎名：`set_keep_title_visible`

### SetLayoutDirection

```csharp
public void SetLayoutDirection(Window.LayoutDirection direction)
```

引擎名：`set_layout_direction`

Sets layout direction and text writing direction. Right-to-left layouts are necessary for certain languages (e.g. Arabic and Hebrew).

### SetMaxSize

```csharp
public void SetMaxSize(Vector2I maxSize)
```

引擎名：`set_max_size`

### SetMinSize

```csharp
public void SetMinSize(Vector2I minSize)
```

引擎名：`set_min_size`

### SetMode

```csharp
public void SetMode(Window.ModeEnum mode)
```

引擎名：`set_mode`

### SetMousePassthroughPolygon

```csharp
public void SetMousePassthroughPolygon(ReadOnlySpan<Vector2> polygon)
```

引擎名：`set_mouse_passthrough_polygon`

### SetMousePassthroughPolygon

```csharp
public void SetMousePassthroughPolygon(Vector2[] polygon)
```

引擎名：`set_mouse_passthrough_polygon`

### SetNonclientArea

```csharp
public void SetNonclientArea(Rect2I area)
```

引擎名：`set_nonclient_area`

### SetPosition

```csharp
public void SetPosition(Vector2I position)
```

引擎名：`set_position`

### SetSize

```csharp
public void SetSize(Vector2I size)
```

引擎名：`set_size`

### SetTaskbarProgressState

```csharp
public void SetTaskbarProgressState(DisplayServer.ProgressState state)
```

引擎名：`set_taskbar_progress_state`

Sets the type and state of the progress bar on the taskbar/dock icon of the `Window`. See [`DisplayServer.ProgressState`](DisplayServer.md) for possible values and how each mode behaves.

**Note:** This method is implemented only on Windows and macOS.

### SetTaskbarProgressValue

```csharp
public void SetTaskbarProgressValue(float value)
```

引擎名：`set_taskbar_progress_value`

Creates a progress bar on the taskbar/dock icon of the `Window` if it does not exist, sets the progress of the icon.

`value` acts as a relative percentage value, ranges from `0.0` (lowest) to `1.0` (highest).

**Note:** This method is implemented only on Windows and macOS.

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

### SetTitle

```csharp
public void SetTitle(string title)
```

引擎名：`set_title`

### SetTransient

```csharp
public void SetTransient(bool transient)
```

引擎名：`set_transient`

### SetTransientToFocused

```csharp
public void SetTransientToFocused(bool enable)
```

引擎名：`set_transient_to_focused`

### SetUnparentWhenInvisible

```csharp
public void SetUnparentWhenInvisible(bool unparent)
```

引擎名：`set_unparent_when_invisible`

If `unparent` is `true`, the window is automatically unparented when going invisible.

**Note:** Make sure to keep a reference to the node, otherwise it will be orphaned. You also need to manually call [`Node.QueueFree`](Node.md) to free the window if it's not parented.

### SetUseFontOversampling

```csharp
public void SetUseFontOversampling(bool enable)
```

引擎名：`set_use_font_oversampling`

Enables font oversampling. This makes fonts look better when they are scaled up.

### SetVisible

```csharp
public void SetVisible(bool visible)
```

引擎名：`set_visible`

### SetWrapControls

```csharp
public void SetWrapControls(bool enable)
```

引擎名：`set_wrap_controls`

### Show

```csharp
public void Show()
```

引擎名：`show`

Makes the `Window` appear. This enables interactions with the `Window` and doesn't change any of its property other than visibility (unlike e.g. `Popup()`).

### StartDrag

```csharp
public void StartDrag()
```

引擎名：`start_drag`

Starts an interactive drag operation on the window, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's title bar. Using this method allows the window to participate in space switching, tiling, and other system features.

### StartResize

```csharp
public void StartResize(DisplayServer.WindowResizeEdge edge)
```

引擎名：`start_resize`

Starts an interactive resize operation on the window, using the current mouse position. Call this method when handling a mouse button being pressed to simulate a pressed event on the window's edge.

### _GetContentsMinimumSize

```csharp
public virtual Vector2 _GetContentsMinimumSize()
```

引擎名：`_get_contents_minimum_size`

Virtual method to be implemented by the user. Overrides the value returned by `GetContentsMinimumSize()`.

## 事件（信号）

### AboutToPopup

```csharp
public event Action AboutToPopup
```

引擎名：`about_to_popup`

Emitted right after `Popup()` call, before the `Window` appears or does anything.

### CloseRequested

```csharp
public event Action CloseRequested
```

引擎名：`close_requested`

Emitted when the `Window`'s close button is pressed or when `PopupWindow` is enabled and user clicks outside the window.

This signal can be used to handle window closing, e.g. by connecting it to `Hide()`.

### DpiChanged

```csharp
public event Action DpiChanged
```

引擎名：`dpi_changed`

Emitted when the `Window`'s DPI changes as a result of OS-level changes (e.g. moving the window from a Retina display to a lower resolution one).

**Note:** Only implemented on macOS and Linux (Wayland).

### FilesDropped

```csharp
public event Window.FilesDroppedEventHandler FilesDropped
```

引擎名：`files_dropped`

Emitted when files are dragged from the OS file manager and dropped in the game window. The argument is a list of file paths.

```csharp
func _ready():
get_window().files_dropped.connect(on_files_dropped)

func on_files_dropped(files):
print(files)

```

**Note:** This signal only works with native windows, i.e. the main window and `Window`-derived nodes when [`Viewport.GuiEmbedSubwindows`](Viewport.md) is disabled in the main viewport.

### FocusEntered

```csharp
public event Action FocusEntered
```

引擎名：`focus_entered`

Emitted when the `Window` gains focus.

### FocusExited

```csharp
public event Action FocusExited
```

引擎名：`focus_exited`

Emitted when the `Window` loses its focus.

### GoBackRequested

```csharp
public event Action GoBackRequested
```

引擎名：`go_back_requested`

Emitted when a go back request is sent (e.g. pressing the "Back" button on Android), right after [`Node.NotificationWMGoBackRequest`](Node.md).

### MouseEntered

```csharp
public event Action MouseEntered
```

引擎名：`mouse_entered`

Emitted when the mouse cursor enters the `Window`'s visible area, that is not occluded behind other [`Control`](Control.md)s or windows, provided its [`Viewport.GuiDisableInput`](Viewport.md) is `false` and regardless if it's currently focused or not.

### MouseExited

```csharp
public event Action MouseExited
```

引擎名：`mouse_exited`

Emitted when the mouse cursor leaves the `Window`'s visible area, that is not occluded behind other [`Control`](Control.md)s or windows, provided its [`Viewport.GuiDisableInput`](Viewport.md) is `false` and regardless if it's currently focused or not.

### NonclientWindowInput

```csharp
public event Window.NonclientWindowInputEventHandler NonclientWindowInput
```

引擎名：`nonclient_window_input`

Emitted when the mouse event is received by the custom decoration area defined by `NonclientArea`, and normal input to the window is blocked (such as when it has an exclusive child opened). `event`'s position is in the embedder's coordinate system.

### OutputMaxLinearValueChanged

```csharp
public event Window.OutputMaxLinearValueChangedEventHandler OutputMaxLinearValueChanged
```

引擎名：`output_max_linear_value_changed`

Emitted when the output max linear value returned by `GetOutputMaxLinearValue()` has changed. This occurs when HDR output is enabled or disabled and when any HDR output luminance values of the window have changed, such as when the player adjusts their screen brightness setting or moves the window to a different screen. `outputMaxLinearValue` is the new value.

### ThemeChanged

```csharp
public event Action ThemeChanged
```

引擎名：`theme_changed`

Emitted when the `NotificationThemeChanged` notification is sent.

### TitleChanged

```csharp
public event Action TitleChanged
```

引擎名：`title_changed`

Emitted when window title bar text is changed.

### TitlebarChanged

```csharp
public event Action TitlebarChanged
```

引擎名：`titlebar_changed`

Emitted when window title bar decorations are changed, e.g. macOS window enter/exit full screen mode, or extend-to-title flag is changed.

### VisibilityChanged

```csharp
public event Action VisibilityChanged
```

引擎名：`visibility_changed`

Emitted when `Window` is made visible or disappears.

### WindowInput

```csharp
public event Window.WindowInputEventHandler WindowInput
```

引擎名：`window_input`

Emitted when the `Window` is currently focused and receives any input, passing the received event as an argument. The event's position, if present, is in the embedder's coordinate system.

## 常量

### NotificationThemeChanged

```csharp
public const long NotificationThemeChanged = 32
```

Sent when the node needs to refresh its theme items. This happens in one of the following cases:

- The `Theme` property is changed on this node or any of its ancestors.

- The `ThemeTypeVariation` property is changed on this node.

- The node enters the scene tree.

**Note:** As an optimization, this notification won't be sent from changes that occur while this node is outside of the scene tree. Instead, all of the theme item updates can be applied at once when the node enters the scene tree.

### NotificationVisibilityChanged

```csharp
public const long NotificationVisibilityChanged = 30
```

Emitted when `Window`'s visibility changes, right before `VisibilityChanged`.

## ContentScaleAspectEnum

命名空间 `Godot`；enum

### 枚举值

- `Ignore = 0`：The aspect will be ignored.
- `Keep = 1`：The content's aspect will be preserved.
- `KeepWidth = 2`：The content can be expanded vertically.
- `KeepHeight = 3`：The content can be expanded horizontally.
- `Expand = 4`：The content's aspect will be preserved.

## ContentScaleModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：The content will not be scaled to match the [`Window`](Window.md)'s size ([`Window.ContentScaleSize`](Window.md) is ignored).
- `CanvasItems = 1`：The content will be rendered at the target size.
- `Viewport = 2`：The content will be rendered at the base size and then scaled to the target size.

## ContentScaleStretchEnum

命名空间 `Godot`；enum

### 枚举值

- `Fractional = 0`：The content will be stretched according to a fractional factor.
- `Integer = 1`：The content will be stretched only according to an integer factor, preserving sharp pixels.

## FilesDroppedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Window.FilesDropped`](Window.md) event of a [`Window`](Window.md) class.

### 签名

#### Invoke

```csharp
delegate void FilesDroppedEventHandler(string[] files)
```

## Flags

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
- `ExcludeFromCapture = 9`：Windows is excluded from screenshots taken by [`DisplayServer.ScreenGetImage`](DisplayServer.md), [`DisplayServer.ScreenGetImageRect`](DisplayServer.md), and [`DisplayServer.ScreenGetPixel`](DisplayServer.md).
- `PopupWMHint = 10`：Signals the window manager that this window is supposed to be an implementation-defined "popup" (usually a floating, borderless, untileable and immovable child window).
- `MinimizeDisabled = 11`：Window minimize button is disabled.
- `MaximizeDisabled = 12`：Window maximize button is disabled.
- `Max = 13`：Max value of the `Window.Flags`.

## LayoutDirection

命名空间 `Godot`；enum

### 枚举值

- `Inherited = 0`：Automatic layout direction, determined from the parent window layout direction.
- `ApplicationLocale = 1`：Automatic layout direction, determined from the current locale.
- `Ltr = 2`：Left-to-right layout direction.
- `Rtl = 3`：Right-to-left layout direction.
- `SystemLocale = 4`：Automatic layout direction, determined from the system locale.
- `Max = 5`：Represents the size of the `Window.LayoutDirection` enum.
- `Locale = 1`

## ModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Windowed = 0`：Windowed mode, i.e.
- `Minimized = 1`：Minimized window mode, i.e.
- `Maximized = 2`：Maximized window mode, i.e.
- `Fullscreen = 3`：Full screen mode with full multi-window support.
- `ExclusiveFullscreen = 4`：A single window full screen mode.

## NonclientWindowInputEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Window.NonclientWindowInput`](Window.md) event of a [`Window`](Window.md) class.

### 签名

#### Invoke

```csharp
delegate void NonclientWindowInputEventHandler(InputEvent event)
```

## OutputMaxLinearValueChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Window.OutputMaxLinearValueChanged`](Window.md) event of a [`Window`](Window.md) class.

### 签名

#### Invoke

```csharp
delegate void OutputMaxLinearValueChangedEventHandler(double outputMaxLinearValue)
```

## WindowInitialPosition

命名空间 `Godot`；enum

### 枚举值

- `Absolute = 0`：Initial window position is determined by [`Window.Position`](Window.md).
- `CenterPrimaryScreen = 1`：Initial window position is the center of the primary screen.
- `CenterMainWindowScreen = 2`：Initial window position is the center of the main window screen.
- `CenterOtherScreen = 3`：Initial window position is the center of [`Window.CurrentScreen`](Window.md) screen.
- `CenterScreenWithMouseFocus = 4`：Initial window position is the center of the screen containing the mouse pointer.
- `CenterScreenWithKeyboardFocus = 5`：Initial window position is the center of the screen containing the window with the keyboard focus.

## WindowInputEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Window.WindowInput`](Window.md) event of a [`Window`](Window.md) class.

### 签名

#### Invoke

```csharp
delegate void WindowInputEventHandler(InputEvent event)
```
