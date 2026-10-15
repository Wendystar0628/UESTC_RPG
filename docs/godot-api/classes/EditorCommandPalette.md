<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorCommandPalette

命名空间 `Godot`；class；继承 [`ConfirmationDialog`](ConfirmationDialog.md) → [`AcceptDialog`](AcceptDialog.md) → [`Window`](Window.md) → [`Viewport`](Viewport.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Object that holds all the available Commands and their shortcuts text. These Commands can be accessed through **Editor > Command Palette** menu.

Command key names use slash delimiters to distinguish sections, for example: `"example/command1"` then `example` will be the section name.

```csharp
EditorCommandPalette commandPalette = EditorInterface.Singleton.GetCommandPalette();
// ExternalCommand is a function that will be called with the command is executed.
Callable commandCallable = new Callable(this, MethodName.ExternalCommand);
commandPalette.AddCommand("command", "test/command", commandCallable)

```

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetCommandPalette`](EditorInterface.md).

## 构造函数

### EditorCommandPalette

```csharp
public EditorCommandPalette()
```

## 方法

### AddCommand

```csharp
public void AddCommand(string commandName, string keyName, Callable bindedCallable, string shortcutText = "None")
```

引擎名：`add_command`

Adds a custom command to EditorCommandPalette.

- `commandName`: `System.String` (Name of the **Command**. This is displayed to the user.)

- `keyName`: `System.String` (Name of the key for a particular **Command**. This is used to uniquely identify the **Command**.)

- `bindedCallable`: [`Callable`](Callable.md) (Callable of the **Command**. This will be executed when the **Command** is selected.)

- `shortcutText`: `System.String` (Shortcut text of the **Command** if available.)

### RemoveCommand

```csharp
public void RemoveCommand(string keyName)
```

引擎名：`remove_command`

Removes the custom command from EditorCommandPalette.

- `keyName`: `System.String` (Name of the key for a particular **Command**.)
