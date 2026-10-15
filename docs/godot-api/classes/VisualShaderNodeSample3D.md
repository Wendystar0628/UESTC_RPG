<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeSample3D

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A virtual class, use the descendants instead.

## 属性

### Source

```csharp
public VisualShaderNodeSample3D.SourceEnum Source { get; set; }
```

引擎名：`source`

An input source type.

## 方法

### GetSource

```csharp
public VisualShaderNodeSample3D.SourceEnum GetSource()
```

引擎名：`get_source`

### SetSource

```csharp
public void SetSource(VisualShaderNodeSample3D.SourceEnum value)
```

引擎名：`set_source`

## SourceEnum

命名空间 `Godot`；enum

### 枚举值

- `Texture = 0`：Creates internal uniform and provides a way to assign it within node.
- `Port = 1`：Use the uniform texture from sampler port.
- `Max = 2`：Represents the size of the `VisualShaderNodeSample3D.SourceEnum` enum.
