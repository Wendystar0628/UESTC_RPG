<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorScenePostImport

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Imported scenes can be automatically modified right after import by setting their **Custom Script** Import property to a `tool` script that inherits from this class.

The `_PostImport()` callback receives the imported scene's root node and returns the modified version of the scene:

```csharp
using Godot;

// This sample changes all node names.
// Called right after the scene is imported and gets the root node.
[Tool]
public partial class NodeRenamer : EditorScenePostImport
{
public override GodotObject _PostImport(Node scene)
{
// Change all node names to "modified_[oldnodename]"
Iterate(scene);
return scene; // Remember to return the imported scene
}

public void Iterate(Node node)
{
if (node != null)
{
node.Name = $"modified_{node.Name}";
foreach (Node child in node.GetChildren())
{
Iterate(child);
}
}
}
}

```

## 构造函数

### EditorScenePostImport

```csharp
public EditorScenePostImport()
```

## 方法

### GetSourceFile

```csharp
public string GetSourceFile()
```

引擎名：`get_source_file`

Returns the source file path which got imported (e.g. `res://scene.dae`).

### _PostImport

```csharp
public virtual GodotObject _PostImport(Node scene)
```

引擎名：`_post_import`

Called after the scene was imported. This method must return the modified version of the scene.
