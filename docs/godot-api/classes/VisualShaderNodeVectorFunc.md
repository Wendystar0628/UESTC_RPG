<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeVectorFunc

命名空间 `Godot`；class；继承 [`VisualShaderNodeVectorBase`](VisualShaderNodeVectorBase.md) → [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A visual shader node able to perform different functions using vectors.

## 构造函数

### VisualShaderNodeVectorFunc

```csharp
public VisualShaderNodeVectorFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeVectorFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

The function to be performed.

## 方法

### GetFunction

```csharp
public VisualShaderNodeVectorFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeVectorFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Normalize = 0`：Normalizes the vector so that it has a length of `1` but points in the same direction.
- `Saturate = 1`：Clamps the value between `0.0` and `1.0`.
- `Negate = 2`：Returns the opposite value of the parameter.
- `Reciprocal = 3`：Returns `1/vector`.
- `Abs = 4`：Returns the absolute value of the parameter.
- `Acos = 5`：Returns the arc-cosine of the parameter.
- `Acosh = 6`：Returns the inverse hyperbolic cosine of the parameter.
- `Asin = 7`：Returns the arc-sine of the parameter.
- `Asinh = 8`：Returns the inverse hyperbolic sine of the parameter.
- `Atan = 9`：Returns the arc-tangent of the parameter.
- `Atanh = 10`：Returns the inverse hyperbolic tangent of the parameter.
- `Ceil = 11`：Finds the nearest integer that is greater than or equal to the parameter.
- `Cos = 12`：Returns the cosine of the parameter.
- `Cosh = 13`：Returns the hyperbolic cosine of the parameter.
- `Degrees = 14`：Converts a quantity in radians to degrees.
- `Exp = 15`：Base-e Exponential.
- `Exp2 = 16`：Base-2 Exponential.
- `Floor = 17`：Finds the nearest integer less than or equal to the parameter.
- `Fract = 18`：Computes the fractional part of the argument.
- `InverseSqrt = 19`：Returns the inverse of the square root of the parameter.
- `Log = 20`：Natural logarithm.
- `Log2 = 21`：Base-2 logarithm.
- `Radians = 22`：Converts a quantity in degrees to radians.
- `Round = 23`：Finds the nearest integer to the parameter.
- `Roundeven = 24`：Finds the nearest even integer to the parameter.
- `Sign = 25`：Extracts the sign of the parameter, i.e.
- `Sin = 26`：Returns the sine of the parameter.
- `Sinh = 27`：Returns the hyperbolic sine of the parameter.
- `Sqrt = 28`：Returns the square root of the parameter.
- `Tan = 29`：Returns the tangent of the parameter.
- `Tanh = 30`：Returns the hyperbolic tangent of the parameter.
- `Trunc = 31`：Returns a value equal to the nearest integer to the parameter whose absolute value is not larger than the absolute value of the parameter.
- `Oneminus = 32`：Returns `1.0 - vector`.
- `Max = 33`：Represents the size of the `VisualShaderNodeVectorFunc.FunctionEnum` enum.
