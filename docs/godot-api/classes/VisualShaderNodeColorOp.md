<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeColorOp

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Applies `Operator` to two color inputs.

## 构造函数

### VisualShaderNodeColorOp

```csharp
public VisualShaderNodeColorOp()
```

## 属性

### Operator

```csharp
public VisualShaderNodeColorOp.OperatorEnum Operator { get; set; }
```

引擎名：`operator`

An operator to be applied to the inputs.

## 方法

### GetOperator

```csharp
public VisualShaderNodeColorOp.OperatorEnum GetOperator()
```

引擎名：`get_operator`

### SetOperator

```csharp
public void SetOperator(VisualShaderNodeColorOp.OperatorEnum op)
```

引擎名：`set_operator`

## OperatorEnum

命名空间 `Godot`；enum

### 枚举值

- `Screen = 0`：Produce a screen effect with the following formula:
- `Difference = 1`：Produce a difference effect with the following formula:
- `Darken = 2`：Produce a darken effect with the following formula:
- `Lighten = 3`：Produce a lighten effect with the following formula:
- `Overlay = 4`：Produce an overlay effect with the following formula:
- `Dodge = 5`：Produce a dodge effect with the following formula:
- `Burn = 6`：Produce a burn effect with the following formula:
- `SoftLight = 7`：Produce a soft light effect with the following formula:
- `HardLight = 8`：Produce a hard light effect with the following formula:
- `Max = 9`：Represents the size of the `VisualShaderNodeColorOp.OperatorEnum` enum.
