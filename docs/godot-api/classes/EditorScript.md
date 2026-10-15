<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorScript

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Scripts extending this class and implementing its `_Run()` method can be executed from the Script Editor's **File > Run** menu option (or by pressing Ctrl + Shift + X) while the editor is running. This is useful for adding custom in-editor functionality to Godot. For more complex additions, consider using [`EditorPlugin`](EditorPlugin.md)s instead.

If a script extending this class also has a global class name, it will be included in the editor's command palette.

**Note:** Extending scripts need to have `tool` mode enabled.

**Example:** Running the following script prints "Hello from the Godot Editor!":

```csharp
using Godot;

[Tool]
public partial class HelloEditor : EditorScript
{
public override void _Run()
{
GD.Print("Hello from the Godot Editor!");
}
}

```

**Note:** EditorScript is [`RefCounted`](RefCounted.md), meaning it is destroyed when nothing references it. This can cause errors during asynchronous operations if there are no references to the script.

## 构造函数

### EditorScript

```csharp
public EditorScript()
```

## 方法

### AddRootNode

```csharp
public void AddRootNode(Node node)
```

引擎名：`add_root_node`

Makes `node` root of the currently opened scene. Only works if the scene is empty. If the `node` is a scene instance, an inheriting scene will be created.

### GetEditorInterface

```csharp
public EditorInterface GetEditorInterface()
```

引擎名：`get_editor_interface`

Returns the [`EditorInterface`](EditorInterface.md) singleton instance.

### GetScene

```csharp
public Node GetScene()
```

引擎名：`get_scene`

Returns the edited (current) scene's root [`Node`](Node.md). Equivalent of [`EditorInterface.GetEditedSceneRoot`](EditorInterface.md).

### _Run

```csharp
public virtual void _Run()
```

引擎名：`_run`

This method is executed by the Editor when **File > Run** is used.
