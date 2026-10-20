<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeIntOp

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Applies `Operator` to two integer inputs: `a` and `b`.

## 构造函数

### VisualShaderNodeIntOp

```csharp
public VisualShaderNodeIntOp()
```

## 属性

### Operator

```csharp
public VisualShaderNodeIntOp.OperatorEnum Operator { get; set; }
```

引擎名：`operator`

An operator to be applied to the inputs.

## 方法

### GetOperator

```csharp
public VisualShaderNodeIntOp.OperatorEnum GetOperator()
```

引擎名：`get_operator`

### SetOperator

```csharp
public void SetOperator(VisualShaderNodeIntOp.OperatorEnum op)
```

引擎名：`set_operator`

## OperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Add = 0`：Sums two numbers using `a + b`.
- `Sub = 1`：Subtracts two numbers using `a - b`.
- `Mul = 2`：Multiplies two numbers using `a * b`.
- `Div = 3`：Divides two numbers using `a / b`.
- `Mod = 4`：Calculates the remainder of two numbers using `a % b`.
- `Max = 5`：Returns the greater of two numbers.
- `Min = 6`：Returns the lesser of two numbers.
- `BitwiseAnd = 7`：Returns the result of bitwise `AND` operation on the integer.
- `BitwiseOr = 8`：Returns the result of bitwise `OR` operation for two integers.
- `BitwiseXor = 9`：Returns the result of bitwise `XOR` operation for two integers.
- `BitwiseLeftShift = 10`：Returns the result of bitwise left shift operation on the integer.
- `BitwiseRightShift = 11`：Returns the result of bitwise right shift operation on the integer.
- `EnumSize = 12`：Represents the size of the `VisualShaderNodeIntOp.OperatorEnum` enum.
