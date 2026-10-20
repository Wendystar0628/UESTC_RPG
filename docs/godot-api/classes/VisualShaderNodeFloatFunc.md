<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualShaderNodeFloatFunc

命名空间 `Godot`；class；继承 [`VisualShaderNode`](VisualShaderNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Accept a floating-point scalar (`x`) to the input port and transform it according to `Function`.

## 构造函数

### VisualShaderNodeFloatFunc

```csharp
public VisualShaderNodeFloatFunc()
```

## 属性

### Function

```csharp
public VisualShaderNodeFloatFunc.FunctionEnum Function { get; set; }
```

引擎名：`function`

A function to be applied to the scalar.

## 方法

### GetFunction

```csharp
public VisualShaderNodeFloatFunc.FunctionEnum GetFunction()
```

引擎名：`get_function`

### SetFunction

```csharp
public void SetFunction(VisualShaderNodeFloatFunc.FunctionEnum func)
```

引擎名：`set_function`

## FunctionEnum

命名空间 `Godot`；enum

### 枚举值

- `Sin = 0`：Returns the sine of the parameter.
- `Cos = 1`：Returns the cosine of the parameter.
- `Tan = 2`：Returns the tangent of the parameter.
- `Asin = 3`：Returns the arc-sine of the parameter.
- `Acos = 4`：Returns the arc-cosine of the parameter.
- `Atan = 5`：Returns the arc-tangent of the parameter.
- `Sinh = 6`：Returns the hyperbolic sine of the parameter.
- `Cosh = 7`：Returns the hyperbolic cosine of the parameter.
- `Tanh = 8`：Returns the hyperbolic tangent of the parameter.
- `Log = 9`：Returns the natural logarithm of the parameter.
- `Exp = 10`：Returns the natural exponentiation of the parameter.
- `Sqrt = 11`：Returns the square root of the parameter.
- `Abs = 12`：Returns the absolute value of the parameter.
- `Sign = 13`：Extracts the sign of the parameter.
- `Floor = 14`：Finds the nearest integer less than or equal to the parameter.
- `Round = 15`：Finds the nearest integer to the parameter.
- `Ceil = 16`：Finds the nearest integer that is greater than or equal to the parameter.
- `Fract = 17`：Computes the fractional part of the argument.
- `Saturate = 18`：Clamps the value between `0.0` and `1.0` using `min(max(x, 0.0), 1.0)`.
- `Negate = 19`：Negates the `x` using `-(x)`.
- `Acosh = 20`：Returns the arc-hyperbolic-cosine of the parameter.
- `Asinh = 21`：Returns the arc-hyperbolic-sine of the parameter.
- `Atanh = 22`：Returns the arc-hyperbolic-tangent of the parameter.
- `Degrees = 23`：Convert a quantity in radians to degrees.
- `Exp2 = 24`：Returns 2 raised by the power of the parameter.
- `InverseSqrt = 25`：Returns the inverse of the square root of the parameter.
- `Log2 = 26`：Returns the base 2 logarithm of the parameter.
- `Radians = 27`：Convert a quantity in degrees to radians.
- `Reciprocal = 28`：Finds reciprocal value of dividing 1 by `x` (i.e.
- `Roundeven = 29`：Finds the nearest even integer to the parameter.
- `Trunc = 30`：Returns a value equal to the nearest integer to `x` whose absolute value is not larger than the absolute value of `x`.
- `Oneminus = 31`：Subtracts scalar `x` from 1 (i.e.
- `Max = 32`：Represents the size of the `VisualShaderNodeFloatFunc.FunctionEnum` enum.
