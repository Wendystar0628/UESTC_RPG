<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeVectorBase

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This is an abstract class. See the derived types for descriptions of the possible operations.

## 属性

### OpType

```csharp
public VisualShaderNodeVectorBase.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

A vector type that this operation is performed on.

## 方法

### GetOpType

```csharp
public VisualShaderNodeVectorBase.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeVectorBase.OpTypeEnum type)
```

引擎名：`set_op_type`

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Vector2D = 0`：A 2D vector type.
- `Vector3D = 1`：A 3D vector type.
- `Vector4D = 2`：A 4D vector type.
- `Max = 3`：Represents the size of the `VisualShaderNodeVectorBase.OpTypeEnum` enum.
