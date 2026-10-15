<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeTransformVecMult

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A multiplication operation on a transform (4×4 matrix) and a vector, with support for different multiplication operators.

## 构造函数

### VisualShaderNodeTransformVecMult

```csharp
public VisualShaderNodeTransformVecMult()
```

## 属性

### Operator

```csharp
public VisualShaderNodeTransformVecMult.OperatorEnum Operator { get; set; }
```

引擎名：`operator`

The multiplication type to be performed.

## 方法

### GetOperator

```csharp
public VisualShaderNodeTransformVecMult.OperatorEnum GetOperator()
```

引擎名：`get_operator`

### SetOperator

```csharp
public void SetOperator(VisualShaderNodeTransformVecMult.OperatorEnum op)
```

引擎名：`set_operator`

## OperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Axb = 0`：Multiplies transform `a` by the vector `b`.
- `Bxa = 1`：Multiplies vector `b` by the transform `a`.
- `Op3X3Axb = 2`：Multiplies transform `a` by the vector `b`, skipping the last row and column of the transform.
- `Op3X3Bxa = 3`：Multiplies vector `b` by the transform `a`, skipping the last row and column of the transform.
- `Max = 4`：Represents the size of the `VisualShaderNodeTransformVecMult.OperatorEnum` enum.
