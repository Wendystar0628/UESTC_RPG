<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeStep

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translates to `step(edge, x)` in the shader language.

Returns `0.0` if `x` is smaller than `edge` and `1.0` otherwise.

## 构造函数

### VisualShaderNodeStep

```csharp
public VisualShaderNodeStep()
```

## 属性

### OpType

```csharp
public VisualShaderNodeStep.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

A type of operands and returned value.

## 方法

### GetOpType

```csharp
public VisualShaderNodeStep.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeStep.OpTypeEnum opType)
```

引擎名：`set_op_type`

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：A floating-point scalar type.
- `Vector2D = 1`：A 2D vector type.
- `Vector2DScalar = 2`：The `x` port uses a 2D vector type, while the `edge` port uses a floating-point scalar type.
- `Vector3D = 3`：A 3D vector type.
- `Vector3DScalar = 4`：The `x` port uses a 3D vector type, while the `edge` port uses a floating-point scalar type.
- `Vector4D = 5`：A 4D vector type.
- `Vector4DScalar = 6`：The `a` and `b` ports use a 4D vector type.
- `Max = 7`：Represents the size of the `VisualShaderNodeStep.OpTypeEnum` enum.
