<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeDerivativeFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This node is only available in `Fragment` and `Light` visual shaders.

## 构造函数

### VisualShaderNodeDerivativeFunc

```csharp
public VisualShaderNodeDerivativeFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeDerivativeFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

A derivative function type.

### OpType

```csharp
public VisualShaderNodeDerivativeFunc.OpTypeEnum OpType { get; set; }
```

引擎名：`op_type`

A type of operands and returned value.

### Precision

```csharp
public VisualShaderNodeDerivativeFunc.PrecisionEnum Precision { get; set; }
```

引擎名：`precision`

Sets the level of precision to use for the derivative function. When using the Compatibility renderer, this setting has no effect.

## 方法

### GetFunction

```csharp
public VisualShaderNodeDerivativeFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### GetOpType

```csharp
public VisualShaderNodeDerivativeFunc.OpTypeEnum GetOpType()
```

引擎名：`get_op_type`

### GetPrecision

```csharp
public VisualShaderNodeDerivativeFunc.PrecisionEnum GetPrecision()
```

引擎名：`get_precision`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeDerivativeFunc.FunctionEnum func)
```

引擎名：`set_function`

### SetOpType

```csharp
public void SetOpType(VisualShaderNodeDerivativeFunc.OpTypeEnum type)
```

引擎名：`set_op_type`

### SetPrecision

```csharp
public void SetPrecision(VisualShaderNodeDerivativeFunc.PrecisionEnum precision)
```

引擎名：`set_precision`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Sum = 0`：Sum of absolute derivative in `x` and `y`.
- `X = 1`：Derivative in `x` using local differencing.
- `Y = 2`：Derivative in `y` using local differencing.
- `Max = 3`：Represents the size of the `VisualShaderNodeDerivativeFunc.FunctionEnum` enum.

## OpTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Scalar = 0`：A floating-point scalar.
- `Vector2D = 1`：A 2D vector type.
- `Vector3D = 2`：A 3D vector type.
- `Vector4D = 3`：A 4D vector type.
- `Max = 4`：Represents the size of the `VisualShaderNodeDerivativeFunc.OpTypeEnum` enum.

## PrecisionEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No precision is specified, the GPU driver is allowed to use whatever level of precision it chooses.
- `Coarse = 1`：The derivative will be calculated using the current fragment's neighbors (which may not include the current fragment).
- `Fine = 2`：The derivative will be calculated using the current fragment and its immediate neighbors.
- `Max = 3`：Represents the size of the `VisualShaderNodeDerivativeFunc.PrecisionEnum` enum.
