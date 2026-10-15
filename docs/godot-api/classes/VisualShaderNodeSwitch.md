<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeSwitch

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Returns an associated value of the `OpType` type if the provided boolean value is `true` or `false`.

## 构造函数

### VisualShaderNodeSwitch

```csharp
public VisualShaderNodeSwitch()
```

## 属性

### OpType

```csharp
public VisualShaderNodeSwitch.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

A type of operands and returned value.

## 方法

### GetOpType

```csharp
public VisualShaderNodeSwitch.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeSwitch.OpTypeEnum type)
```

引擎名：`set_op_type`

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Float = 0`：A floating-point scalar.
- `Int = 1`：An integer scalar.
- `Uint = 2`：An unsigned integer scalar.
- `Vector2D = 3`：A 2D vector type.
- `Vector3D = 4`：A 3D vector type.
- `Vector4D = 5`：A 4D vector type.
- `Boolean = 6`：A boolean type.
- `Transform = 7`：A transform type.
- `Max = 8`：Represents the size of the `VisualShaderNodeSwitch.OpTypeEnum` enum.
