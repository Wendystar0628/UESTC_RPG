<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorToaster

命名空间 `Godot`；class；继承 [`HBoxContainer`](HBoxContainer.md) → [`BoxContainer`](BoxContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This object manages the functionality and display of toast notifications within the editor, ensuring immediate and informative alerts are presented to the user.

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetEditorToaster`](EditorInterface.md).

## 方法

### PushToast

```csharp
public void PushToast(string message, EditorToaster.Severity severity = EditorToaster.Severity.Info, string tooltip = "")
```

引擎名：`push_toast`

Pushes a toast notification to the editor for display.

## Severity

命名空间 `Godot`；enum；仅编辑器（GodotSharpEditor）

### 枚举值

- `Info = 0`：Toast will display with an INFO severity.
- `Warning = 1`：Toast will display with a WARNING severity and have a corresponding color.
- `Error = 2`：Toast will display with an ERROR severity and have a corresponding color.
