<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTransformOp

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Applies `Operator` to two transform (4×4 matrices) inputs.

## 构造函数

### VisualShaderNodeTransformOp

```csharp
public VisualShaderNodeTransformOp()
```

## 属性

### Operator

```csharp
public VisualShaderNodeTransformOp.OperatorEnum Operator { get; set; }
```

引擎名：`operator`

The type of the operation to be performed on the transforms.

## 方法

### GetOperator

```csharp
public VisualShaderNodeTransformOp.OperatorEnum GetOperator()
```

引擎名：`get_operator`

### SetOperator

```csharp
public void SetOperator(VisualShaderNodeTransformOp.OperatorEnum op)
```

引擎名：`set_operator`

## OperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Axb = 0`：Multiplies transform `a` by the transform `b`.
- `Bxa = 1`：Multiplies transform `b` by the transform `a`.
- `AxbComp = 2`：Performs a component-wise multiplication of transform `a` by the transform `b`.
- `BxaComp = 3`：Performs a component-wise multiplication of transform `b` by the transform `a`.
- `Add = 4`：Adds two transforms.
- `AMinusB = 5`：Subtracts the transform `a` from the transform `b`.
- `BMinusA = 6`：Subtracts the transform `b` from the transform `a`.
- `ADivB = 7`：Divides the transform `a` by the transform `b`.
- `BDivA = 8`：Divides the transform `b` by the transform `a`.
- `Max = 9`：Represents the size of the `VisualShaderNodeTransformOp.OperatorEnum` enum.
