<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# EditorSelection

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)；仅编辑器（GodotSharpEditor）

This object manages the SceneTree selection in the editor.

**Note:** This class shouldn't be instantiated directly. Instead, access the singleton using [`EditorInterface.GetSelection`](EditorInterface.md).

## 构造函数

### EditorSelection

```csharp
public EditorSelection()
```

## 方法

### AddNode

```csharp
public void AddNode(Node node)
```

引擎名：`add_node`

Adds a node to the selection.

**Note:** The newly selected node will not be automatically edited in the inspector. If you want to edit a node, use [`EditorInterface.EditNode`](EditorInterface.md).

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clear the selection.

### GetSelectedNodes

```csharp
public Array<Node> GetSelectedNodes()
```

引擎名：`get_selected_nodes`

Returns the list of selected nodes.

### GetTopSelectedNodes

```csharp
public Array<Node> GetTopSelectedNodes()
```

引擎名：`get_top_selected_nodes`

Returns the list of top selected nodes only, excluding any children. This is useful for performing transform operations (moving them, rotating, etc.).

For example, if there is a node A with a child B and a sibling C, then selecting all three will cause this method to return only A and C. Changing the global transform of A will affect the global transform of B, so there is no need to change B separately.

### GetTransformableSelectedNodes

```csharp
public Array<Node> GetTransformableSelectedNodes()
```

引擎名：`get_transformable_selected_nodes`

Returns the list of top selected nodes only, excluding any children. This is useful for performing transform operations (moving them, rotating, etc.). See `GetTopSelectedNodes()`.

### RemoveNode

```csharp
public void RemoveNode(Node node)
```

引擎名：`remove_node`

Removes a node from the selection.

## 事件（信号）

### SelectionChanged

```csharp
public event Action SelectionChanged
```

引擎名：`selection_changed`

Emitted when the selection changes.
