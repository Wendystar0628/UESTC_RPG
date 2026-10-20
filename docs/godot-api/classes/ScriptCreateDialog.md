<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ScriptCreateDialog

命名空间 `Godot`；class；继承 [`ConfirmationDialog`](ConfirmationDialog.md) → [`AcceptDialog`](AcceptDialog.md) → [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

The `ScriptCreateDialog` creates script files according to a given template for a given scripting language. The standard use is to configure its fields prior to calling one of the [`Window.Popup`](Window.md) methods.

```csharp
public override void _Ready()
{
var dialog = new ScriptCreateDialog();
dialog.Config("Node", "res://NewNode.cs"); // For in-engine types.
dialog.Config("\"res://BaseNode.cs\"", "res://DerivedNode.cs"); // For script types.
dialog.PopupCentered();
}

```

## 构造函数

### ScriptCreateDialog

```csharp
public ScriptCreateDialog()
```

## 方法

### Config

```csharp
public void Config(string inherits, string path, bool builtInEnabled = true, bool loadEnabled = true)
```

引擎名：`config`

Prefills required fields to configure the ScriptCreateDialog for use.

## 事件（信号）

### ScriptCreated

```csharp
public event ScriptCreateDialog.ScriptCreatedEventHandler ScriptCreated
```

引擎名：`script_created`

Emitted when the user clicks the OK button.

## ScriptCreatedEventHandler

命名空间 `Godot`；delegate；仅编辑器（GodotSharpEditor）

Represents the method that handles the [`ScriptCreateDialog.ScriptCreated`](ScriptCreateDialog.md) event of a [`ScriptCreateDialog`](ScriptCreateDialog.md) class.

### 签名

#### Invoke

```csharp
delegate void ScriptCreatedEventHandler(Script script)
```
