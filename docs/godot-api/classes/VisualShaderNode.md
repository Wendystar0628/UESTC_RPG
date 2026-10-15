<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNode

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Visual shader graphs consist of various nodes. Each node in the graph is a separate object and they are represented as a rectangular boxes with title and a set of properties. Each node also has connection ports that allow to connect it to another nodes and control the flow of the shader.

## 属性

### DefaultInputValues

```csharp
public Array DefaultInputValues { get; set; }
```

引擎名：`default_input_values`

### ExpandedOutputPorts

```csharp
public Array ExpandedOutputPorts { get; set; }
```

引擎名：`expanded_output_ports`

### LinkedParentGraphFrame

```csharp
public int LinkedParentGraphFrame { get; set; }
```

引擎名：`linked_parent_graph_frame`

Represents the index of the frame this node is linked to. If set to `-1` the node is not linked to any frame.

### OutputPortForPreview

```csharp
public int OutputPortForPreview { get; set; }
```

引擎名：`output_port_for_preview`

Sets the output port index which will be showed for preview. If set to `-1` no port will be open for preview.

## 方法

### ClearDefaultInputValues

```csharp
public void ClearDefaultInputValues()
```

引擎名：`clear_default_input_values`

Clears the default input ports value.

### GetDefaultInputPort

```csharp
public int GetDefaultInputPort(VisualShaderNode.PortType type)
```

引擎名：`get_default_input_port`

Returns the input port which should be connected by default when this node is created as a result of dragging a connection from an existing node to the empty space on the graph.

### GetDefaultInputValues

```csharp
public Array GetDefaultInputValues()
```

引擎名：`get_default_input_values`

Returns an [`Collections.Array`](Collections.Array.md) containing default values for all of the input ports of the node in the form `[index0, value0, index1, value1, ...]`.

### GetFrame

```csharp
public int GetFrame()
```

引擎名：`get_frame`

### GetInputPortDefaultValue

```csharp
public Variant GetInputPortDefaultValue(int port)
```

引擎名：`get_input_port_default_value`

Returns the default value of the input `port`.

### GetOutputPortForPreview

```csharp
public int GetOutputPortForPreview()
```

引擎名：`get_output_port_for_preview`

### RemoveInputPortDefaultValue

```csharp
public void RemoveInputPortDefaultValue(int port)
```

引擎名：`remove_input_port_default_value`

Removes the default value of the input `port`.

### SetDefaultInputValues

```csharp
public void SetDefaultInputValues(Array values)
```

引擎名：`set_default_input_values`

Sets the default input ports values using an [`Collections.Array`](Collections.Array.md) of the form `[index0, value0, index1, value1, ...]`. For example: `[0, Vector3(0, 0, 0), 1, Vector3(0, 0, 0)]`.

### SetFrame

```csharp
public void SetFrame(int frame)
```

引擎名：`set_frame`

### SetInputPortDefaultValue

```csharp
public void SetInputPortDefaultValue(int port, Variant value, Variant prevValue = default)
```

引擎名：`set_input_port_default_value`

Sets the default `value` for the selected input `port`.

### SetOutputPortForPreview

```csharp
public void SetOutputPortForPreview(int port)
```

引擎名：`set_output_port_for_preview`

## PortType

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：Floating-point scalar.
- `ScalarInt = 1`：Integer scalar.
- `ScalarUint = 2`：Unsigned integer scalar.
- `Vector2D = 3`：2D vector of floating-point values.
- `Vector3D = 4`：3D vector of floating-point values.
- `Vector4D = 5`：4D vector of floating-point values.
- `Boolean = 6`：Boolean type.
- `Transform = 7`：Transform type.
- `Sampler = 8`：Sampler type.
- `Max = 9`：Represents the size of the `VisualShaderNode.PortType` enum.
