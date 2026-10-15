<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShader

命名空间 `Godot`；class；继承 [`Shader`](Shader.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This class provides a graph-like visual editor for creating a [`Shader`](Shader.md). Although `VisualShader`s do not require coding, they share the same logic with script shaders. They use [`VisualShaderNode`](VisualShaderNode.md)s that can be connected to each other to control the flow of the shader. The visual shader graph is converted to a script shader behind the scenes.

## 构造函数

### VisualShader

```csharp
public VisualShader()
```

## 属性

### GraphOffset

```csharp
public Vector2 GraphOffset { get; set; }
```

引擎名：`graph_offset`

Deprecated.

## 方法

### AddNode

```csharp
public void AddNode(VisualShader.Type type, VisualShaderNode node, Vector2 position, int id)
```

引擎名：`add_node`

Adds the specified `node` to the shader.

### AddVarying

```csharp
public void AddVarying(string name, VisualShader.VaryingMode mode, VisualShader.VaryingType type)
```

引擎名：`add_varying`

Adds a new varying value node to the shader.

### AttachNodeToFrame

```csharp
public void AttachNodeToFrame(VisualShader.Type type, int id, int frame)
```

引擎名：`attach_node_to_frame`

Attaches the given node to the given frame.

### CanConnectNodes

```csharp
public bool CanConnectNodes(VisualShader.Type type, int fromNode, int fromPort, int toNode, int toPort)
```

引擎名：`can_connect_nodes`

Returns `true` if the specified nodes and ports can be connected together.

### ConnectNodes

```csharp
public Error ConnectNodes(VisualShader.Type type, int fromNode, int fromPort, int toNode, int toPort)
```

引擎名：`connect_nodes`

Connects the specified nodes and ports.

### ConnectNodesForced

```csharp
public void ConnectNodesForced(VisualShader.Type type, int fromNode, int fromPort, int toNode, int toPort)
```

引擎名：`connect_nodes_forced`

Connects the specified nodes and ports, even if they can't be connected. Such connection is invalid and will not function properly.

### DetachNodeFromFrame

```csharp
public void DetachNodeFromFrame(VisualShader.Type type, int id)
```

引擎名：`detach_node_from_frame`

Detaches the given node from the frame it is attached to.

### DisconnectNodes

```csharp
public void DisconnectNodes(VisualShader.Type type, int fromNode, int fromPort, int toNode, int toPort)
```

引擎名：`disconnect_nodes`

Connects the specified nodes and ports.

### GetGraphOffset

```csharp
public Vector2 GetGraphOffset()
```

引擎名：`get_graph_offset`

### GetNode

```csharp
public VisualShaderNode GetNode(VisualShader.Type type, int id)
```

引擎名：`get_node`

Returns the shader node instance with specified `type` and `id`.

### GetNodeConnections

```csharp
public Array<Dictionary> GetNodeConnections(VisualShader.Type type)
```

引擎名：`get_node_connections`

Returns the list of connected nodes with the specified type.

### GetNodeList

```csharp
public int[] GetNodeList(VisualShader.Type type)
```

引擎名：`get_node_list`

Returns the list of all nodes in the shader with the specified type.

### GetNodePosition

```csharp
public Vector2 GetNodePosition(VisualShader.Type type, int id)
```

引擎名：`get_node_position`

Returns the position of the specified node within the shader graph.

### GetValidNodeId

```csharp
public int GetValidNodeId(VisualShader.Type type)
```

引擎名：`get_valid_node_id`

Returns next valid node ID that can be added to the shader graph.

### HasVarying

```csharp
public bool HasVarying(string name)
```

引擎名：`has_varying`

Returns `true` if the shader has a varying with the given `name`.

### IsNodeConnection

```csharp
public bool IsNodeConnection(VisualShader.Type type, int fromNode, int fromPort, int toNode, int toPort)
```

引擎名：`is_node_connection`

Returns `true` if the specified node and port connection exist.

### RemoveNode

```csharp
public void RemoveNode(VisualShader.Type type, int id)
```

引擎名：`remove_node`

Removes the specified node from the shader.

### RemoveVarying

```csharp
public void RemoveVarying(string name)
```

引擎名：`remove_varying`

Removes a varying value node with the given `name`. Prints an error if a node with this name is not found.

### ReplaceNode

```csharp
public void ReplaceNode(VisualShader.Type type, int id, StringName newClass)
```

引擎名：`replace_node`

Replaces the specified node with a node of new class type.

### SetGraphOffset

```csharp
public void SetGraphOffset(Vector2 offset)
```

引擎名：`set_graph_offset`

### SetMode

```csharp
public void SetMode(Shader.Mode mode)
```

引擎名：`set_mode`

Sets the mode of this shader.

### SetNodePosition

```csharp
public void SetNodePosition(VisualShader.Type type, int id, Vector2 position)
```

引擎名：`set_node_position`

Sets the position of the specified node.

## 常量

### NodeIdInvalid

```csharp
public const long NodeIdInvalid = -1
```

Indicates an invalid `VisualShader` node.

### NodeIdOutput

```csharp
public const long NodeIdOutput = 0
```

Indicates an output node of `VisualShader`.

## Type

命名空间 `Godot`；enum

### 枚举值

- `Vertex = 0`：A vertex shader, operating on vertices.
- `Fragment = 1`：A fragment shader, operating on fragments (pixels).
- `Light = 2`：A shader for light calculations.
- `Start = 3`：A function for the "start" stage of particle shader.
- `Process = 4`：A function for the "process" stage of particle shader.
- `Collide = 5`：A function for the "collide" stage (particle collision handler) of particle shader.
- `StartCustom = 6`：A function for the "start" stage of particle shader, with customized output.
- `ProcessCustom = 7`：A function for the "process" stage of particle shader, with customized output.
- `Sky = 8`：A shader for 3D environment's sky.
- `Fog = 9`：A compute shader that runs for each froxel of the volumetric fog map.
- `TextureBlit = 10`：A shader used to process blit calls to a DrawableTexture.
- `Max = 11`：Represents the size of the `VisualShader.Type` enum.

## VaryingMode

命名空间 `Godot`；enum

### 枚举值

- `VertexToFragLight = 0`：Varying is passed from `Vertex` function to `Fragment` and `Light` functions.
- `FragToLight = 1`：Varying is passed from `Fragment` function to `Light` function.
- `Max = 2`：Represents the size of the `VisualShader.VaryingMode` enum.

## VaryingType

命名空间 `Godot`；enum

### 枚举值

- `Float = 0`：Varying is of type `System.Single`.
- `Int = 1`：Varying is of type `System.Int32`.
- `Uint = 2`：Varying is of type unsigned `System.Int32`.
- `Vector2D = 3`：Varying is of type [`Vector2`](Vector2.md).
- `Vector3D = 4`：Varying is of type [`Vector3`](Vector3.md).
- `Vector4D = 5`：Varying is of type [`Vector4`](Vector4.md).
- `Boolean = 6`：Varying is of type `System.Boolean`.
- `Transform = 7`：Varying is of type [`Transform3D`](Transform3D.md).
- `Max = 8`：Represents the size of the `VisualShader.VaryingType` enum.
