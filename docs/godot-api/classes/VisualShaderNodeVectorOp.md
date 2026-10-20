<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeVectorOp

命名空间 `Godot`；class；继承 [`VisualShaderNodeVectorBase`](VisualShaderNodeVectorBase.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A visual shader node for use of vector operators. Operates on vector `a` and vector `b`.

## 构造函数

### VisualShaderNodeVectorOp

```csharp
public VisualShaderNodeVectorOp()
```

## 属性

### Operator

```csharp
public VisualShaderNodeVectorOp.OperatorEnum Operator { get; set; }
```

引擎名：`operator`

The operator to be used.

## 方法

### GetOperator

```csharp
public VisualShaderNodeVectorOp.OperatorEnum GetOperator()
```

引擎名：`get_operator`

### SetOperator

```csharp
public void SetOperator(VisualShaderNodeVectorOp.OperatorEnum op)
```

引擎名：`set_operator`

## OperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Adds two vectors.
- `Sub = 1`：Subtracts a vector from a vector.
- `Mul = 2`：Multiplies two vectors.
- `Div = 3`：Divides vector by vector.
- `Mod = 4`：Returns the remainder of the two vectors.
- `Pow = 5`：Returns the value of the first parameter raised to the power of the second, for each component of the vectors.
- `Max = 6`：Returns the greater of two values, for each component of the vectors.
- `Min = 7`：Returns the lesser of two values, for each component of the vectors.
- `Cross = 8`：Calculates the cross product of two vectors.
- `Atan2 = 9`：Returns the arc-tangent of the parameters.
- `Reflect = 10`：Returns the vector that points in the direction of reflection.
- `Step = 11`：Vector step operator.
- `EnumSize = 12`：Represents the size of the `VisualShaderNodeVectorOp.OperatorEnum` enum.
