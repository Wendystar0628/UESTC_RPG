<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorFileDialog

命名空间 `Godot`；class；继承 [`FileDialog`](FileDialog.md) → [`ConfirmationDialog`](ConfirmationDialog.md) → [`AcceptDialog`](AcceptDialog.md) → [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

`EditorFileDialog` is a [`FileDialog`](FileDialog.md) tweaked to work in the editor. It automatically handles favorite and recent directory lists, and synchronizes some properties with their corresponding editor settings.

`EditorFileDialog` will automatically show a native dialog based on the `EditorSettings.interface/editor/appearance/use_native_file_dialogs` editor setting and ignores [`FileDialog.UseNativeDialog`](FileDialog.md).

**Note:** `EditorFileDialog` is invisible by default. To make it visible, call one of the `popup_*` methods from [`Window`](Window.md) on the node, such as [`Window.PopupCenteredClamped`](Window.md).

**Note:** On Linux and macOS, sandboxed apps always use native dialogs to access the host file system.

## 构造函数

### EditorFileDialog

```csharp
public EditorFileDialog()
```

## 属性

### DisableOverwriteWarning

```csharp
public bool DisableOverwriteWarning { get; set; }
```

引擎名：`disable_overwrite_warning`

If `true`, the `EditorFileDialog` will not warn the user before overwriting files.

## 方法

### AddSideMenu

```csharp
public void AddSideMenu(Control menu, string title = "")
```

引擎名：`add_side_menu`

This method is kept for compatibility and does nothing. As an alternative, you can display another dialog after showing the file dialog.

### IsOverwriteWarningDisabled

```csharp
public bool IsOverwriteWarningDisabled()
```

引擎名：`is_overwrite_warning_disabled`

### SetDisableOverwriteWarning

```csharp
public void SetDisableOverwriteWarning(bool disable)
```

引擎名：`set_disable_overwrite_warning`
