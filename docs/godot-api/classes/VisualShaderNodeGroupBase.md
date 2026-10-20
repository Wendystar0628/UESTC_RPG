<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeGroupBase

命名空间 `Godot`；class；继承 [`VisualShaderNodeResizableBase`](VisualShaderNodeResizableBase.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Currently, has no direct usage, use the derived classes instead.

## 方法

### AddInputPort

```csharp
public void AddInputPort(int id, int type, string name)
```

引擎名：`add_input_port`

Adds an input port with the specified `type` (see [`VisualShaderNode.PortType`](VisualShaderNode.md)) and `name`.

### AddOutputPort

```csharp
public void AddOutputPort(int id, int type, string name)
```

引擎名：`add_output_port`

Adds an output port with the specified `type` (see [`VisualShaderNode.PortType`](VisualShaderNode.md)) and `name`.

### ClearInputPorts

```csharp
public void ClearInputPorts()
```

引擎名：`clear_input_ports`

Removes all previously specified input ports.

### ClearOutputPorts

```csharp
public void ClearOutputPorts()
```

引擎名：`clear_output_ports`

Removes all previously specified output ports.

### GetFreeInputPortId

```csharp
public int GetFreeInputPortId()
```

引擎名：`get_free_input_port_id`

Returns a free input port ID which can be used in `AddInputPort()`.

### GetFreeOutputPortId

```csharp
public int GetFreeOutputPortId()
```

引擎名：`get_free_output_port_id`

Returns a free output port ID which can be used in `AddOutputPort()`.

### GetInputPortCount

```csharp
public int GetInputPortCount()
```

引擎名：`get_input_port_count`

Returns the number of input ports in use. Alternative for `GetFreeInputPortId()`.

### GetInputs

```csharp
public string GetInputs()
```

引擎名：`get_inputs`

Returns a `System.String` description of the input ports as a colon-separated list using the format `id,type,name;` (see `AddInputPort()`).

### GetOutputPortCount

```csharp
public int GetOutputPortCount()
```

引擎名：`get_output_port_count`

Returns the number of output ports in use. Alternative for `GetFreeOutputPortId()`.

### GetOutputs

```csharp
public string GetOutputs()
```

引擎名：`get_outputs`

Returns a `System.String` description of the output ports as a colon-separated list using the format `id,type,name;` (see `AddOutputPort()`).

### HasInputPort

```csharp
public bool HasInputPort(int id)
```

引擎名：`has_input_port`

Returns `true` if the specified input port exists.

### HasOutputPort

```csharp
public bool HasOutputPort(int id)
```

引擎名：`has_output_port`

Returns `true` if the specified output port exists.

### IsValidPortName

```csharp
public bool IsValidPortName(string name)
```

引擎名：`is_valid_port_name`

Returns `true` if the specified port name does not override an existed port name and is valid within the shader.

### RemoveInputPort

```csharp
public void RemoveInputPort(int id)
```

引擎名：`remove_input_port`

Removes the specified input port.

### RemoveOutputPort

```csharp
public void RemoveOutputPort(int id)
```

引擎名：`remove_output_port`

Removes the specified output port.

### SetInputPortName

```csharp
public void SetInputPortName(int id, string name)
```

引擎名：`set_input_port_name`

Renames the specified input port.

### SetInputPortType

```csharp
public void SetInputPortType(int id, int type)
```

引擎名：`set_input_port_type`

Sets the specified input port's type (see [`VisualShaderNode.PortType`](VisualShaderNode.md)).

### SetInputs

```csharp
public void SetInputs(string inputs)
```

引擎名：`set_inputs`

Defines all input ports using a `System.String` formatted as a colon-separated list: `id,type,name;` (see `AddInputPort()`).

### SetOutputPortName

```csharp
public void SetOutputPortName(int id, string name)
```

引擎名：`set_output_port_name`

Renames the specified output port.

### SetOutputPortType

```csharp
public void SetOutputPortType(int id, int type)
```

引擎名：`set_output_port_type`

Sets the specified output port's type (see [`VisualShaderNode.PortType`](VisualShaderNode.md)).

### SetOutputs

```csharp
public void SetOutputs(string outputs)
```

引擎名：`set_outputs`

Defines all output ports using a `System.String` formatted as a colon-separated list: `id,type,name;` (see `AddOutputPort()`).
