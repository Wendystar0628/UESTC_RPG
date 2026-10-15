<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeBlendTree

命名空间 `Godot`；class；继承 [`AnimationRootNode`](AnimationRootNode.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This animation node may contain a sub-tree of any other type animation nodes, such as [`AnimationNodeTransition`](AnimationNodeTransition.md), [`AnimationNodeBlend2`](AnimationNodeBlend2.md), [`AnimationNodeBlend3`](AnimationNodeBlend3.md), [`AnimationNodeOneShot`](AnimationNodeOneShot.md), etc. This is one of the most commonly used animation node roots.

An [`AnimationNodeOutput`](AnimationNodeOutput.md) node named `output` is created by default.

## 构造函数

### AnimationNodeBlendTree

```csharp
public AnimationNodeBlendTree()
```

## 属性

### GraphOffset

```csharp
public Vector2 GraphOffset { get; set; }
```

引擎名：`graph_offset`

The global offset of all sub animation nodes.

## 方法

### AddNode

```csharp
public void AddNode(StringName name, AnimationNode node, Vector2? position = null)
```

引擎名：`add_node`

Adds an [`AnimationNode`](AnimationNode.md) at the given `position`. The `name` is used to identify the created sub animation node later.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### ConnectNode

```csharp
public void ConnectNode(StringName inputNode, int inputIndex, StringName outputNode)
```

引擎名：`connect_node`

Connects the output of an [`AnimationNode`](AnimationNode.md) as input for another [`AnimationNode`](AnimationNode.md), at the input port specified by `inputIndex`.

### DisconnectNode

```csharp
public void DisconnectNode(StringName inputNode, int inputIndex)
```

引擎名：`disconnect_node`

Disconnects the animation node connected to the specified input.

### GetGraphOffset

```csharp
public Vector2 GetGraphOffset()
```

引擎名：`get_graph_offset`

### GetNode

```csharp
public AnimationNode GetNode(StringName name)
```

引擎名：`get_node`

Returns the sub animation node with the specified `name`.

### GetNodeList

```csharp
public Array<StringName> GetNodeList()
```

引擎名：`get_node_list`

Returns a list containing the names of all sub animation nodes in this blend tree.

### GetNodePosition

```csharp
public Vector2 GetNodePosition(StringName name)
```

引擎名：`get_node_position`

Returns the position of the sub animation node with the specified `name`.

### HasNode

```csharp
public bool HasNode(StringName name)
```

引擎名：`has_node`

Returns `true` if a sub animation node with specified `name` exists.

### RemoveNode

```csharp
public void RemoveNode(StringName name)
```

引擎名：`remove_node`

Removes a sub animation node.

### RenameNode

```csharp
public void RenameNode(StringName name, StringName newName)
```

引擎名：`rename_node`

Changes the name of a sub animation node.

### SetGraphOffset

```csharp
public void SetGraphOffset(Vector2 offset)
```

引擎名：`set_graph_offset`

### SetNodePosition

```csharp
public void SetNodePosition(StringName name, Vector2 position)
```

引擎名：`set_node_position`

Modifies the position of a sub animation node.

## 事件（信号）

### NodeChanged

```csharp
public event AnimationNodeBlendTree.NodeChangedEventHandler NodeChanged
```

引擎名：`node_changed`

Emitted when the input port information is changed.

## 常量

### ConnectionErrorConnectionExists

```csharp
public const long ConnectionErrorConnectionExists = 5
```

The specified connection already exists.

### ConnectionErrorNoInput

```csharp
public const long ConnectionErrorNoInput = 1
```

The input node is `null`.

### ConnectionErrorNoInputIndex

```csharp
public const long ConnectionErrorNoInputIndex = 2
```

The specified input port is out of range.

### ConnectionErrorNoOutput

```csharp
public const long ConnectionErrorNoOutput = 3
```

The output node is `null`.

### ConnectionErrorSameNode

```csharp
public const long ConnectionErrorSameNode = 4
```

Input and output nodes are the same.

### ConnectionOk

```csharp
public const long ConnectionOk = 0
```

The connection was successful.

## NodeChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationNodeBlendTree.NodeChanged`](AnimationNodeBlendTree.md) event of a [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md) class.

### 签名

#### Invoke

```csharp
delegate void NodeChangedEventHandler(StringName nodeName)
```
