<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeSmoothStep

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Translates to `smoothstep(edge0, edge1, x)` in the shader language.

Returns `0.0` if `x` is smaller than `edge0` and `1.0` if `x` is larger than `edge1`. Otherwise, the return value is interpolated between `0.0` and `1.0` using Hermite polynomials.

## 构造函数

### VisualShaderNodeSmoothStep

```csharp
public VisualShaderNodeSmoothStep()
```

## 属性

### OpType

```csharp
public VisualShaderNodeSmoothStep.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

A type of operands and returned value.

## 方法

### GetOpType

```csharp
public VisualShaderNodeSmoothStep.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeSmoothStep.OpTypeEnum opType)
```

引擎名：`set_op_type`

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：A floating-point scalar type.
- `Vector2D = 1`：A 2D vector type.
- `Vector2DScalar = 2`：The `x` port uses a 2D vector type.
- `Vector3D = 3`：A 3D vector type.
- `Vector3DScalar = 4`：The `x` port uses a 3D vector type.
- `Vector4D = 5`：A 4D vector type.
- `Vector4DScalar = 6`：The `a` and `b` ports use a 4D vector type.
- `Max = 7`：Represents the size of the `VisualShaderNodeSmoothStep.OpTypeEnum` enum.
