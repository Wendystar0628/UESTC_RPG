<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeReroute

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Automatically adapts its port type to the type of the incoming connection and ensures valid connections.

## 构造函数

### VisualShaderNodeReroute

```csharp
public VisualShaderNodeReroute()
```

## 属性

### PortType

```csharp
public VisualShaderNode.PortType PortType { get; set; }
```

引擎名：`port_type`

## 方法

### GetPortType

```csharp
public VisualShaderNode.PortType GetPortType()
```

引擎名：`get_port_type`

Returns the port type of the reroute node.
