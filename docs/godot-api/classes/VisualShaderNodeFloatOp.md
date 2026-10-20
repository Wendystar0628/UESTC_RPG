<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeFloatOp

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Applies `Operator` to two floating-point inputs: `a` and `b`.

## 构造函数

### VisualShaderNodeFloatOp

```csharp
public VisualShaderNodeFloatOp()
```

## 属性

### Operator

```csharp
public VisualShaderNodeFloatOp.OperatorEnum Operator { get; set; }
```

引擎名：`operator`

An operator to be applied to the inputs.

## 方法

### GetOperator

```csharp
public VisualShaderNodeFloatOp.OperatorEnum GetOperator()
```

引擎名：`get_operator`

### SetOperator

```csharp
public void SetOperator(VisualShaderNodeFloatOp.OperatorEnum op)
```

引擎名：`set_operator`

## OperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Sums two numbers using `a + b`.
- `Sub = 1`：Subtracts two numbers using `a - b`.
- `Mul = 2`：Multiplies two numbers using `a * b`.
- `Div = 3`：Divides two numbers using `a / b`.
- `Mod = 4`：Calculates the remainder of two numbers.
- `Pow = 5`：Raises the `a` to the power of `b`.
- `Max = 6`：Returns the greater of two numbers.
- `Min = 7`：Returns the lesser of two numbers.
- `Atan2 = 8`：Returns the arc-tangent of the parameters.
- `Step = 9`：Generates a step function by comparing `b`(x) to `a`(edge).
- `EnumSize = 10`：Represents the size of the `VisualShaderNodeFloatOp.OperatorEnum` enum.
