<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeMultiplyAdd

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Uses three operands to compute `(a * b + c)` expression.

## 构造函数

### VisualShaderNodeMultiplyAdd

```csharp
public VisualShaderNodeMultiplyAdd()
```

## 属性

### OpType

```csharp
public VisualShaderNodeMultiplyAdd.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

A type of operands and returned value.

## 方法

### GetOpType

```csharp
public VisualShaderNodeMultiplyAdd.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeMultiplyAdd.OpTypeEnum type)
```

引擎名：`set_op_type`

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：A floating-point scalar type.
- `Vector2D = 1`：A 2D vector type.
- `Vector3D = 2`：A 3D vector type.
- `Vector4D = 3`：A 4D vector type.
- `Max = 4`：Represents the size of the `VisualShaderNodeMultiplyAdd.OpTypeEnum` enum.
