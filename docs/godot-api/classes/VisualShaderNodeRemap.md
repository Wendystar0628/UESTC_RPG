<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeRemap

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Remap will transform the input range into output range, e.g. you can change a `0..1` value to `-2..2` etc. See `Mathf.Remap` for more details.

## 构造函数

### VisualShaderNodeRemap

```csharp
public VisualShaderNodeRemap()
```

## 属性

### OpType

```csharp
public VisualShaderNodeRemap.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

## 方法

### GetOpType

```csharp
public VisualShaderNodeRemap.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeRemap.OpTypeEnum opType)
```

引擎名：`set_op_type`

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：A floating-point scalar type.
- `Vector2D = 1`：A 2D vector type.
- `Vector2DScalar = 2`：The `value` port uses a 2D vector type, while the `input min`, `input max`, `output min`, and `output max` ports use a floating-point scalar type.
- `Vector3D = 3`：A 3D vector type.
- `Vector3DScalar = 4`：The `value` port uses a 3D vector type, while the `input min`, `input max`, `output min`, and `output max` ports use a floating-point scalar type.
- `Vector4D = 5`：A 4D vector type.
- `Vector4DScalar = 6`：The `value` port uses a 4D vector type, while the `input min`, `input max`, `output min`, and `output max` ports use a floating-point scalar type.
- `Max = 7`：Represents the size of the `VisualShaderNodeRemap.OpTypeEnum` enum.
