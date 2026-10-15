<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorScriptPicker

命名空间 `Godot`；class；继承 [`EditorResourcePicker`](EditorResourcePicker.md) → [`HBoxContainer`](HBoxContainer.md) → [`BoxContainer`](BoxContainer.md) → [`Container`](Container.md) → [`Control`](Control.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

Similar to [`EditorResourcePicker`](EditorResourcePicker.md) this [`Control`](Control.md) node is used in the editor's Inspector dock, but only to edit the `script` property of a [`Node`](Node.md). Default options for creating new resources of all possible subtypes are replaced with dedicated buttons that open the "Attach Node Script" dialog. Can be used with [`EditorInspectorPlugin`](EditorInspectorPlugin.md) to recreate the same behavior.

**Note:** You must set the `ScriptOwner` for the custom context menu items to work.

## 构造函数

### EditorScriptPicker

```csharp
public EditorScriptPicker()
```

## 属性

### ScriptOwner

```csharp
public Node ScriptOwner { get; set; }
```

引擎名：`script_owner`

The owner [`Node`](Node.md) of the script property that holds the edited resource.

## 方法

### GetScriptOwner

```csharp
public Node GetScriptOwner()
```

引擎名：`get_script_owner`

### SetScriptOwner

```csharp
public void SetScriptOwner(Node ownerNode)
```

引擎名：`set_script_owner`
