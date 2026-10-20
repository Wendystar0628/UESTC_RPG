<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Mathf

命名空间 `Godot`；static class

Provides constants and static methods for common mathematical functions.

## 方法

### Abs

```csharp
public static double Abs(double s)
```

Returns the absolute value of `s` (i.e. positive value).

参数：
- `s`：The input number.

返回：The absolute value of `s`.

### Abs

```csharp
public static float Abs(float s)
```

Returns the absolute value of `s` (i.e. positive value).

参数：
- `s`：The input number.

返回：The absolute value of `s`.

### Abs

```csharp
public static int Abs(int s)
```

Returns the absolute value of `s` (i.e. positive value).

参数：
- `s`：The input number.

返回：The absolute value of `s`.

### Acos

```csharp
public static double Acos(double s)
```

Returns the arc cosine of `s` in radians.
Use to get the angle of cosine `s`.

参数：
- `s`：The input cosine value. Must be on the range of -1.0 to 1.0.

返回：An angle that would result in the given cosine value. On the range `0` to `Tau/2`.

### Acos

```csharp
public static float Acos(float s)
```

Returns the arc cosine of `s` in radians.
Use to get the angle of cosine `s`.

参数：
- `s`：The input cosine value. Must be on the range of -1.0 to 1.0.

返回：An angle that would result in the given cosine value. On the range `0` to `Tau/2`.

### Acosh

```csharp
public static double Acosh(double s)
```

Returns the hyperbolic arc (also called inverse) cosine of `s` in radians.
Use it to get the angle from an angle's cosine in hyperbolic space if
`s` is larger or equal to 1.

参数：
- `s`：The input hyperbolic cosine value.

返回：An angle that would result in the given hyperbolic cosine value.

### Acosh

```csharp
public static float Acosh(float s)
```

Returns the hyperbolic arc (also called inverse) cosine of `s` in radians.
Use it to get the angle from an angle's cosine in hyperbolic space if
`s` is larger or equal to 1.

参数：
- `s`：The input hyperbolic cosine value.

返回：An angle that would result in the given hyperbolic cosine value.

### AngleDifference

```csharp
public static double AngleDifference(double from, double to)
```

Returns the difference between the two angles,
in range of -`Pi`, `Pi`.
When `from` and `to` are opposite,
returns -`Pi` if `from` is smaller than `to`,
or `Pi` otherwise.

参数：
- `from`：The start angle.
- `to`：The destination angle.

返回：The difference between the two angles.

### AngleDifference

```csharp
public static float AngleDifference(float from, float to)
```

Returns the difference between the two angles,
in range of -`Pi`, `Pi`.
When `from` and `to` are opposite,
returns -`Pi` if `from` is smaller than `to`,
or `Pi` otherwise.

参数：
- `from`：The start angle.
- `to`：The destination angle.

返回：The difference between the two angles.

### Asin

```csharp
public static double Asin(double s)
```

Returns the arc sine of `s` in radians.
Use to get the angle of sine `s`.

参数：
- `s`：The input sine value. Must be on the range of -1.0 to 1.0.

返回：An angle that would result in the given sine value. On the range `-Tau/4` to `Tau/4`.

### Asin

```csharp
public static float Asin(float s)
```

Returns the arc sine of `s` in radians.
Use to get the angle of sine `s`.

参数：
- `s`：The input sine value. Must be on the range of -1.0 to 1.0.

返回：An angle that would result in the given sine value. On the range `-Tau/4` to `Tau/4`.

### Asinh

```csharp
public static double Asinh(double s)
```

Returns the hyperbolic arc (also called inverse) sine of `s` in radians.
Use it to get the angle from an angle's sine in hyperbolic space if
`s` is larger or equal to 1.

参数：
- `s`：The input hyperbolic sine value.

返回：An angle that would result in the given hyperbolic sine value.

### Asinh

```csharp
public static float Asinh(float s)
```

Returns the hyperbolic arc (also called inverse) sine of `s` in radians.
Use it to get the angle from an angle's sine in hyperbolic space if
`s` is larger or equal to 1.

参数：
- `s`：The input hyperbolic sine value.

返回：An angle that would result in the given hyperbolic sine value.

### Atan

```csharp
public static double Atan(double s)
```

Returns the arc tangent of `s` in radians.
Use to get the angle of tangent `s`.

The method cannot know in which quadrant the angle should fall.
See `Atan2()` if you have both `y` and `x`.

参数：
- `s`：The input tangent value.

返回：An angle that would result in the given tangent value. On the range `-Tau/4` to `Tau/4`.

### Atan

```csharp
public static float Atan(float s)
```

Returns the arc tangent of `s` in radians.
Use to get the angle of tangent `s`.

The method cannot know in which quadrant the angle should fall.
See `Atan2()` if you have both `y` and `x`.

参数：
- `s`：The input tangent value.

返回：An angle that would result in the given tangent value. On the range `-Tau/4` to `Tau/4`.

### Atan2

```csharp
public static double Atan2(double y, double x)
```

Returns the arc tangent of `y` and `x` in radians.
Use to get the angle of the tangent of `y/x`. To compute the value, the method takes into
account the sign of both arguments in order to determine the quadrant.

Important note: The Y coordinate comes first, by convention.

参数：
- `y`：The Y coordinate of the point to find the angle to.
- `x`：The X coordinate of the point to find the angle to.

返回：An angle that would result in the given tangent value. On the range `-Tau/2` to `Tau/2`.

### Atan2

```csharp
public static float Atan2(float y, float x)
```

Returns the arc tangent of `y` and `x` in radians.
Use to get the angle of the tangent of `y/x`. To compute the value, the method takes into
account the sign of both arguments in order to determine the quadrant.

Important note: The Y coordinate comes first, by convention.

参数：
- `y`：The Y coordinate of the point to find the angle to.
- `x`：The X coordinate of the point to find the angle to.

返回：An angle that would result in the given tangent value. On the range `-Tau/2` to `Tau/2`.

### Atanh

```csharp
public static double Atanh(double s)
```

Returns the hyperbolic arc (also called inverse) tangent of `s` in radians.
Use it to get the angle from an angle's tangent in hyperbolic space if
`s` is between -1 and 1 (non-inclusive).

参数：
- `s`：The input hyperbolic tangent value.

返回：An angle that would result in the given hyperbolic tangent value.

### Atanh

```csharp
public static float Atanh(float s)
```

Returns the hyperbolic arc (also called inverse) tangent of `s` in radians.
Use it to get the angle from an angle's tangent in hyperbolic space if
`s` is between -1 and 1 (non-inclusive).

参数：
- `s`：The input hyperbolic tangent value.

返回：An angle that would result in the given hyperbolic tangent value.

### BezierDerivative

```csharp
public static double BezierDerivative(double start, double control1, double control2, double end, double t)
```

Returns the derivative at the given `t` on a one dimensional Bezier curve defined by
the given `control1`, `control2`, and `end` points.

参数：
- `start`：The start value for the interpolation.
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination value for the interpolation.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### BezierDerivative

```csharp
public static float BezierDerivative(float start, float control1, float control2, float end, float t)
```

Returns the derivative at the given `t` on a one dimensional Bezier curve defined by
the given `control1`, `control2`, and `end` points.

参数：
- `start`：The start value for the interpolation.
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination value for the interpolation.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### BezierInterpolate

```csharp
public static double BezierInterpolate(double start, double control1, double control2, double end, double t)
```

Returns the point at the given `t` on a one-dimensional Bezier curve defined by
the given `control1`, `control2`, and `end` points.

参数：
- `start`：The start value for the interpolation.
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination value for the interpolation.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### BezierInterpolate

```csharp
public static float BezierInterpolate(float start, float control1, float control2, float end, float t)
```

Returns the point at the given `t` on a one-dimensional Bezier curve defined by
the given `control1`, `control2`, and `end` points.

参数：
- `start`：The start value for the interpolation.
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination value for the interpolation.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### Ceil

```csharp
public static double Ceil(double s)
```

Rounds `s` upward (towards positive infinity).

参数：
- `s`：The number to ceil.

返回：The smallest whole number that is not less than `s`.

### Ceil

```csharp
public static float Ceil(float s)
```

Rounds `s` upward (towards positive infinity).

参数：
- `s`：The number to ceil.

返回：The smallest whole number that is not less than `s`.

### CeilToInt

```csharp
public static int CeilToInt(double s)
```

Rounds `s` upward (towards positive infinity).

This is the same as `Ceil()`, but returns an `int`.

参数：
- `s`：The number to ceil.

返回：The smallest whole number that is not less than `s`.

### CeilToInt

```csharp
public static int CeilToInt(float s)
```

Rounds `s` upward (towards positive infinity).

This is the same as `Ceil()`, but returns an `int`.

参数：
- `s`：The number to ceil.

返回：The smallest whole number that is not less than `s`.

### Clamp

```csharp
public static double Clamp(double value, double min, double max)
```

Clamps a `value` so that it is not less than `min`
and not more than `max`.

参数：
- `value`：The value to clamp.
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The clamped value.

### Clamp

```csharp
public static float Clamp(float value, float min, float max)
```

Clamps a `value` so that it is not less than `min`
and not more than `max`.

参数：
- `value`：The value to clamp.
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The clamped value.

### Clamp

```csharp
public static int Clamp(int value, int min, int max)
```

Clamps a `value` so that it is not less than `min`
and not more than `max`.

参数：
- `value`：The value to clamp.
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The clamped value.

### Cos

```csharp
public static double Cos(double s)
```

Returns the cosine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The cosine of that angle.

### Cos

```csharp
public static float Cos(float s)
```

Returns the cosine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The cosine of that angle.

### Cosh

```csharp
public static double Cosh(double s)
```

Returns the hyperbolic cosine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The hyperbolic cosine of that angle.

### Cosh

```csharp
public static float Cosh(float s)
```

Returns the hyperbolic cosine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The hyperbolic cosine of that angle.

### CubicInterpolate

```csharp
public static double CubicInterpolate(double from, double to, double pre, double post, double weight)
```

Cubic interpolates between two values by the factor defined in `weight`
with pre and post values.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolate

```csharp
public static float CubicInterpolate(float from, float to, float pre, float post, float weight)
```

Cubic interpolates between two values by the factor defined in `weight`
with pre and post values.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolateAngle

```csharp
public static double CubicInterpolateAngle(double from, double to, double pre, double post, double weight)
```

Cubic interpolates between two rotation values with shortest path
by the factor defined in `weight` with pre and post values.
See also `LerpAngle()`.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolateAngle

```csharp
public static float CubicInterpolateAngle(float from, float to, float pre, float post, float weight)
```

Cubic interpolates between two rotation values with shortest path
by the factor defined in `weight` with pre and post values.
See also `LerpAngle()`.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolateAngleInTime

```csharp
public static double CubicInterpolateAngleInTime(double from, double to, double pre, double post, double weight, double toT, double preT, double postT)
```

Cubic interpolates between two rotation values with shortest path
by the factor defined in `weight` with pre and post values.
See also `LerpAngle()`.
It can perform smoother interpolation than
`CubicInterpolateAngle()`
by the time values.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolateAngleInTime

```csharp
public static float CubicInterpolateAngleInTime(float from, float to, float pre, float post, float weight, float toT, float preT, float postT)
```

Cubic interpolates between two rotation values with shortest path
by the factor defined in `weight` with pre and post values.
See also `LerpAngle()`.
It can perform smoother interpolation than
`CubicInterpolateAngle()`
by the time values.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolateInTime

```csharp
public static double CubicInterpolateInTime(double from, double to, double pre, double post, double weight, double toT, double preT, double postT)
```

Cubic interpolates between two values by the factor defined in `weight`
with pre and post values.
It can perform smoother interpolation than
`CubicInterpolate()`
by the time values.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### CubicInterpolateInTime

```csharp
public static float CubicInterpolateInTime(float from, float to, float pre, float post, float weight, float toT, float preT, float postT)
```

Cubic interpolates between two values by the factor defined in `weight`
with pre and post values.
It can perform smoother interpolation than
`CubicInterpolate()`
by the time values.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `pre`：The value which before "from" value for interpolation.
- `post`：The value which after "to" value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### DbToLinear

```csharp
public static double DbToLinear(double db)
```

Converts from decibels to linear energy (audio).

参数：
- `db`：Decibels to convert.

返回：Audio volume as linear energy.

### DbToLinear

```csharp
public static float DbToLinear(float db)
```

Converts from decibels to linear energy (audio).

参数：
- `db`：Decibels to convert.

返回：Audio volume as linear energy.

### DecimalCount

```csharp
public static int DecimalCount(decimal s)
```

Returns the amount of digits after the decimal place.

参数：
- `s`：The input `decimal` value.

返回：The amount of digits.

### DecimalCount

```csharp
public static int DecimalCount(double s)
```

Returns the amount of digits after the decimal place.

参数：
- `s`：The input value.

返回：The amount of digits.

### DegToRad

```csharp
public static double DegToRad(double deg)
```

Converts an angle expressed in degrees to radians.

参数：
- `deg`：An angle expressed in degrees.

返回：The same angle expressed in radians.

### DegToRad

```csharp
public static float DegToRad(float deg)
```

Converts an angle expressed in degrees to radians.

参数：
- `deg`：An angle expressed in degrees.

返回：The same angle expressed in radians.

### Ease

```csharp
public static double Ease(double s, double curve)
```

Easing function, based on exponent. The `curve` values are:
`0` is constant, `1` is linear, `0` to `1` is ease-in, `1` or more is ease-out.
Negative values are in-out/out-in.

参数：
- `s`：The value to ease.
- `curve`：`0` is constant, `1` is linear, `0` to `1` is ease-in, `1` or more is ease-out.

返回：The eased value.

### Ease

```csharp
public static float Ease(float s, float curve)
```

Easing function, based on exponent. The `curve` values are:
`0` is constant, `1` is linear, `0` to `1` is ease-in, `1` or more is ease-out.
Negative values are in-out/out-in.

参数：
- `s`：The value to ease.
- `curve`：`0` is constant, `1` is linear, `0` to `1` is ease-in, `1` or more is ease-out.

返回：The eased value.

### Exp

```csharp
public static double Exp(double s)
```

The natural exponential function. It raises the mathematical
constant `e` to the power of `s` and returns it.

参数：
- `s`：The exponent to raise `e` to.

返回：`e` raised to the power of `s`.

### Exp

```csharp
public static float Exp(float s)
```

The natural exponential function. It raises the mathematical
constant `e` to the power of `s` and returns it.

参数：
- `s`：The exponent to raise `e` to.

返回：`e` raised to the power of `s`.

### Floor

```csharp
public static double Floor(double s)
```

Rounds `s` downward (towards negative infinity).

参数：
- `s`：The number to floor.

返回：The largest whole number that is not more than `s`.

### Floor

```csharp
public static float Floor(float s)
```

Rounds `s` downward (towards negative infinity).

参数：
- `s`：The number to floor.

返回：The largest whole number that is not more than `s`.

### FloorToInt

```csharp
public static int FloorToInt(double s)
```

Rounds `s` downward (towards negative infinity).

This is the same as `Floor()`, but returns an `int`.

参数：
- `s`：The number to floor.

返回：The largest whole number that is not more than `s`.

### FloorToInt

```csharp
public static int FloorToInt(float s)
```

Rounds `s` downward (towards negative infinity).

This is the same as `Floor()`, but returns an `int`.

参数：
- `s`：The number to floor.

返回：The largest whole number that is not more than `s`.

### InverseLerp

```csharp
public static double InverseLerp(double from, double to, double weight)
```

Returns a normalized value considering the given range.
This is the opposite of `Lerp()`.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `weight`：The interpolated value.

返回：The resulting value of the inverse interpolation.
The returned value will be between 0.0 and 1.0 if `weight` is
between `from` and `to` (inclusive).

### InverseLerp

```csharp
public static float InverseLerp(float from, float to, float weight)
```

Returns a normalized value considering the given range.
This is the opposite of `Lerp()`.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `weight`：The interpolated value.

返回：The resulting value of the inverse interpolation.
The returned value will be between 0.0 and 1.0 if `weight` is
between `from` and `to` (inclusive).

### IsEqualApprox

```csharp
public static bool IsEqualApprox(double a, double b)
```

Returns `true` if `a` and `b` are approximately equal
to each other.
The comparison is done using a tolerance calculation with `Epsilon`.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：A `bool` for whether or not the two values are approximately equal.

### IsEqualApprox

```csharp
public static bool IsEqualApprox(double a, double b, double tolerance)
```

Returns `true` if `a` and `b` are approximately
equal to each other.
The comparison is done using the provided tolerance value.
If you want the tolerance to be calculated for you, use `IsEqualApprox()`.

参数：
- `a`：One of the values.
- `b`：The other value.
- `tolerance`：The pre-calculated tolerance value.

返回：A `bool` for whether or not the two values are equal.

### IsEqualApprox

```csharp
public static bool IsEqualApprox(float a, float b)
```

Returns `true` if `a` and `b` are approximately equal
to each other.
The comparison is done using a tolerance calculation with `Epsilon`.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：A `bool` for whether or not the two values are approximately equal.

### IsEqualApprox

```csharp
public static bool IsEqualApprox(float a, float b, float tolerance)
```

Returns `true` if `a` and `b` are approximately
equal to each other.
The comparison is done using the provided tolerance value.
If you want the tolerance to be calculated for you, use `IsEqualApprox()`.

参数：
- `a`：One of the values.
- `b`：The other value.
- `tolerance`：The pre-calculated tolerance value.

返回：A `bool` for whether or not the two values are equal.

### IsFinite

```csharp
public static bool IsFinite(double s)
```

Returns whether `s` is a finite value, i.e. it is not
`NaN`, positive infinite, or negative infinity.

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is a finite value.

### IsFinite

```csharp
public static bool IsFinite(float s)
```

Returns whether `s` is a finite value, i.e. it is not
`NaN`, positive infinite, or negative infinity.

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is a finite value.

### IsInf

```csharp
public static bool IsInf(double s)
```

Returns whether `s` is an infinity value (either positive infinity or negative infinity).

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is an infinity value.

### IsInf

```csharp
public static bool IsInf(float s)
```

Returns whether `s` is an infinity value (either positive infinity or negative infinity).

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is an infinity value.

### IsNaN

```csharp
public static bool IsNaN(double s)
```

Returns whether `s` is a `NaN` ("Not a Number" or invalid) value.

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is a `NaN` value.

### IsNaN

```csharp
public static bool IsNaN(float s)
```

Returns whether `s` is a `NaN` ("Not a Number" or invalid) value.

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is a `NaN` value.

### IsZeroApprox

```csharp
public static bool IsZeroApprox(double s)
```

Returns `true` if `s` is zero or almost zero.
The comparison is done using a tolerance calculation with `Epsilon`.

This method is faster than using `IsEqualApprox()` with
one value as zero.

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is nearly zero.

### IsZeroApprox

```csharp
public static bool IsZeroApprox(float s)
```

Returns `true` if `s` is zero or almost zero.
The comparison is done using a tolerance calculation with `Epsilon`.

This method is faster than using `IsEqualApprox()` with
one value as zero.

参数：
- `s`：The value to check.

返回：A `bool` for whether or not the value is nearly zero.

### Lerp

```csharp
public static double Lerp(double from, double to, double weight)
```

Linearly interpolates between two values by a normalized value.
This is the opposite `InverseLerp()`.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### Lerp

```csharp
public static float Lerp(float from, float to, float weight)
```

Linearly interpolates between two values by a normalized value.
This is the opposite `InverseLerp()`.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### LerpAngle

```csharp
public static double LerpAngle(double from, double to, double weight)
```

Linearly interpolates between two angles (in radians) by a normalized value.

Similar to `Lerp()`,
but interpolates correctly when the angles wrap around `Tau`.

参数：
- `from`：The start angle for interpolation.
- `to`：The destination angle for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting angle of the interpolation.

### LerpAngle

```csharp
public static float LerpAngle(float from, float to, float weight)
```

Linearly interpolates between two angles (in radians) by a normalized value.

Similar to `Lerp()`,
but interpolates correctly when the angles wrap around `Tau`.

参数：
- `from`：The start angle for interpolation.
- `to`：The destination angle for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting angle of the interpolation.

### LinearToDb

```csharp
public static double LinearToDb(double linear)
```

Converts from linear energy to decibels (audio).
This can be used to implement volume sliders that behave as expected (since volume isn't linear).

参数：
- `linear`：The linear energy to convert.

返回：Audio as decibels.

### LinearToDb

```csharp
public static float LinearToDb(float linear)
```

Converts from linear energy to decibels (audio).
This can be used to implement volume sliders that behave as expected (since volume isn't linear).

参数：
- `linear`：The linear energy to convert.

返回：Audio as decibels.

### Log

```csharp
public static double Log(double s)
```

Natural logarithm. The amount of time needed to reach a certain level of continuous growth.

Note: This is not the same as the "log" function on most calculators, which uses a base 10 logarithm.

参数：
- `s`：The input value.

返回：The natural log of `s`.

### Log

```csharp
public static float Log(float s)
```

Natural logarithm. The amount of time needed to reach a certain level of continuous growth.

Note: This is not the same as the "log" function on most calculators, which uses a base 10 logarithm.

参数：
- `s`：The input value.

返回：The natural log of `s`.

### Max

```csharp
public static double Max(double a, double b)
```

Returns the maximum of two values.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：Whichever of the two values is higher.

### Max

```csharp
public static float Max(float a, float b)
```

Returns the maximum of two values.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：Whichever of the two values is higher.

### Max

```csharp
public static int Max(int a, int b)
```

Returns the maximum of two values.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：Whichever of the two values is higher.

### Min

```csharp
public static double Min(double a, double b)
```

Returns the minimum of two values.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：Whichever of the two values is lower.

### Min

```csharp
public static float Min(float a, float b)
```

Returns the minimum of two values.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：Whichever of the two values is lower.

### Min

```csharp
public static int Min(int a, int b)
```

Returns the minimum of two values.

参数：
- `a`：One of the values.
- `b`：The other value.

返回：Whichever of the two values is lower.

### MoveToward

```csharp
public static double MoveToward(double from, double to, double delta)
```

Moves `from` toward `to` by the `delta` value.

Use a negative `delta` value to move away.

参数：
- `from`：The start value.
- `to`：The value to move towards.
- `delta`：The amount to move by.

返回：The value after moving.

### MoveToward

```csharp
public static float MoveToward(float from, float to, float delta)
```

Moves `from` toward `to` by the `delta` value.

Use a negative `delta` value to move away.

参数：
- `from`：The start value.
- `to`：The value to move towards.
- `delta`：The amount to move by.

返回：The value after moving.

### NearestPo2

```csharp
public static int NearestPo2(int value)
```

Returns the nearest larger power of 2 for the integer `value`.

参数：
- `value`：The input value.

返回：The nearest larger power of 2.

### PingPong

```csharp
public static double PingPong(double value, double length)
```

Returns the `value` wrapped between `0` and the `length`.
If the limit is reached, the next value the function returned is decreased to the `0` side
or increased to the `length` side (like a triangle wave).
If `length` is less than zero, it becomes positive.

参数：
- `value`：The value to pingpong.
- `length`：The maximum value of the function.

返回：The ping-ponged value.

### PingPong

```csharp
public static float PingPong(float value, float length)
```

Returns the `value` wrapped between `0` and the `length`.
If the limit is reached, the next value the function returned is decreased to the `0` side
or increased to the `length` side (like a triangle wave).
If `length` is less than zero, it becomes positive.

参数：
- `value`：The value to pingpong.
- `length`：The maximum value of the function.

返回：The ping-ponged value.

### PosMod

```csharp
public static double PosMod(double a, double b)
```

Performs a canonical Modulus operation, where the output is on the range [0, `b`).

参数：
- `a`：The dividend, the primary input.
- `b`：The divisor. The output is on the range [0, `b`).

返回：The resulting output.

### PosMod

```csharp
public static float PosMod(float a, float b)
```

Performs a canonical Modulus operation, where the output is on the range [0, `b`).

参数：
- `a`：The dividend, the primary input.
- `b`：The divisor. The output is on the range [0, `b`).

返回：The resulting output.

### PosMod

```csharp
public static int PosMod(int a, int b)
```

Performs a canonical Modulus operation, where the output is on the range [0, `b`).

参数：
- `a`：The dividend, the primary input.
- `b`：The divisor. The output is on the range [0, `b`).

返回：The resulting output.

### Pow

```csharp
public static double Pow(double x, double y)
```

Returns the result of `x` raised to the power of `y`.

参数：
- `x`：The base.
- `y`：The exponent.

返回：`x` raised to the power of `y`.

### Pow

```csharp
public static float Pow(float x, float y)
```

Returns the result of `x` raised to the power of `y`.

参数：
- `x`：The base.
- `y`：The exponent.

返回：`x` raised to the power of `y`.

### RadToDeg

```csharp
public static double RadToDeg(double rad)
```

Converts an angle expressed in radians to degrees.

参数：
- `rad`：An angle expressed in radians.

返回：The same angle expressed in degrees.

### RadToDeg

```csharp
public static float RadToDeg(float rad)
```

Converts an angle expressed in radians to degrees.

参数：
- `rad`：An angle expressed in radians.

返回：The same angle expressed in degrees.

### Remap

```csharp
public static double Remap(double value, double inFrom, double inTo, double outFrom, double outTo)
```

Maps a `value` from [`inFrom`, `inTo`]
to [`outFrom`, `outTo`].

参数：
- `value`：The value to map.
- `inFrom`：The start value for the input interpolation.
- `inTo`：The destination value for the input interpolation.
- `outFrom`：The start value for the output interpolation.
- `outTo`：The destination value for the output interpolation.

返回：The resulting mapped value mapped.

### Remap

```csharp
public static float Remap(float value, float inFrom, float inTo, float outFrom, float outTo)
```

Maps a `value` from [`inFrom`, `inTo`]
to [`outFrom`, `outTo`].

参数：
- `value`：The value to map.
- `inFrom`：The start value for the input interpolation.
- `inTo`：The destination value for the input interpolation.
- `outFrom`：The start value for the output interpolation.
- `outTo`：The destination value for the output interpolation.

返回：The resulting mapped value mapped.

### RotateToward

```csharp
public static double RotateToward(double from, double to, double delta)
```

Rotates `from` toward `to` by the `delta` amount. Will not go past `to`.
Similar to `MoveToward()` but interpolates correctly when the angles wrap around `Tau`.
If `delta` is negative, this function will rotate away from `to`, toward the opposite angle, and will not go past the opposite angle.

参数：
- `from`：The start angle.
- `to`：The angle to move towards.
- `delta`：The amount to move by.

返回：The angle after moving.

### RotateToward

```csharp
public static float RotateToward(float from, float to, float delta)
```

Rotates `from` toward `to` by the `delta` amount. Will not go past `to`.
Similar to `MoveToward()` but interpolates correctly when the angles wrap around `Tau`.
If `delta` is negative, this function will rotate away from `to`, toward the opposite angle, and will not go past the opposite angle.

参数：
- `from`：The start angle.
- `to`：The angle to move towards.
- `delta`：The amount to move by.

返回：The angle after moving.

### Round

```csharp
public static double Round(double s)
```

Rounds `s` to the nearest whole number,
with halfway cases rounded towards the nearest multiple of two.

参数：
- `s`：The number to round.

返回：The rounded number.

### Round

```csharp
public static float Round(float s)
```

Rounds `s` to the nearest whole number,
with halfway cases rounded towards the nearest multiple of two.

参数：
- `s`：The number to round.

返回：The rounded number.

### RoundToInt

```csharp
public static int RoundToInt(double s)
```

Rounds `s` to the nearest whole number.

This is the same as `Round()`, but returns an `int`.

参数：
- `s`：The number to round.

返回：The rounded number.

### RoundToInt

```csharp
public static int RoundToInt(float s)
```

Rounds `s` to the nearest whole number.

This is the same as `Round()`, but returns an `int`.

参数：
- `s`：The number to round.

返回：The rounded number.

### Sign

```csharp
public static int Sign(double s)
```

Returns the sign of `s`: `-1` or `1`.
Returns `0` if `s` is `0`.

参数：
- `s`：The input number.

返回：One of three possible values: `1`, `-1`, or `0`.

### Sign

```csharp
public static int Sign(float s)
```

Returns the sign of `s`: `-1` or `1`.
Returns `0` if `s` is `0`.

参数：
- `s`：The input number.

返回：One of three possible values: `1`, `-1`, or `0`.

### Sign

```csharp
public static int Sign(int s)
```

Returns the sign of `s`: `-1` or `1`.
Returns `0` if `s` is `0`.

参数：
- `s`：The input number.

返回：One of three possible values: `1`, `-1`, or `0`.

### Sin

```csharp
public static double Sin(double s)
```

Returns the sine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The sine of that angle.

### Sin

```csharp
public static float Sin(float s)
```

Returns the sine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The sine of that angle.

### SinCos

```csharp
public static ValueTuple<double, double> SinCos(double s)
```

Returns the sine and cosine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The sine and cosine of that angle.

### SinCos

```csharp
public static ValueTuple<float, float> SinCos(float s)
```

Returns the sine and cosine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The sine and cosine of that angle.

### Sinh

```csharp
public static double Sinh(double s)
```

Returns the hyperbolic sine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The hyperbolic sine of that angle.

### Sinh

```csharp
public static float Sinh(float s)
```

Returns the hyperbolic sine of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The hyperbolic sine of that angle.

### SmoothStep

```csharp
public static double SmoothStep(double from, double to, double weight)
```

Returns a number smoothly interpolated between `from` and `to`,
based on the `weight`. Similar to `Lerp()`,
but interpolates faster at the beginning and slower at the end.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `weight`：A value representing the amount of interpolation.

返回：The resulting value of the interpolation.

### SmoothStep

```csharp
public static float SmoothStep(float from, float to, float weight)
```

Returns a number smoothly interpolated between `from` and `to`,
based on the `weight`. Similar to `Lerp()`,
but interpolates faster at the beginning and slower at the end.

参数：
- `from`：The start value for interpolation.
- `to`：The destination value for interpolation.
- `weight`：A value representing the amount of interpolation.

返回：The resulting value of the interpolation.

### Snapped

```csharp
public static double Snapped(double s, double step)
```

Snaps float value `s` to a given `step`.
This can also be used to round a floating point number to an arbitrary number of decimals.

参数：
- `s`：The value to snap.
- `step`：The step size to snap to.

返回：The snapped value.

### Snapped

```csharp
public static float Snapped(float s, float step)
```

Snaps float value `s` to a given `step`.
This can also be used to round a floating point number to an arbitrary number of decimals.

参数：
- `s`：The value to snap.
- `step`：The step size to snap to.

返回：The snapped value.

### Sqrt

```csharp
public static double Sqrt(double s)
```

Returns the square root of `s`, where `s` is a non-negative number.

If you need negative inputs, use `System.Numerics.Complex`.

参数：
- `s`：The input number. Must not be negative.

返回：The square root of `s`.

### Sqrt

```csharp
public static float Sqrt(float s)
```

Returns the square root of `s`, where `s` is a non-negative number.

If you need negative inputs, use `System.Numerics.Complex`.

参数：
- `s`：The input number. Must not be negative.

返回：The square root of `s`.

### StepDecimals

```csharp
public static int StepDecimals(double step)
```

Returns the position of the first non-zero digit, after the
decimal point. Note that the maximum return value is 10,
which is a design decision in the implementation.

参数：
- `step`：The input value.

返回：The position of the first non-zero digit.

### Tan

```csharp
public static double Tan(double s)
```

Returns the tangent of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The tangent of that angle.

### Tan

```csharp
public static float Tan(float s)
```

Returns the tangent of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The tangent of that angle.

### Tanh

```csharp
public static double Tanh(double s)
```

Returns the hyperbolic tangent of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The hyperbolic tangent of that angle.

### Tanh

```csharp
public static float Tanh(float s)
```

Returns the hyperbolic tangent of angle `s` in radians.

参数：
- `s`：The angle in radians.

返回：The hyperbolic tangent of that angle.

### Wrap

```csharp
public static double Wrap(double value, double min, double max)
```

Wraps `value` between `min` and `max`.
Usable for creating loop-alike behavior or infinite surfaces.
If `min` is `0`, this is equivalent
to `PosMod()`, so prefer using that instead.

参数：
- `value`：The value to wrap.
- `min`：The minimum allowed value and lower bound of the range.
- `max`：The maximum allowed value and upper bound of the range.

返回：The wrapped value.

### Wrap

```csharp
public static float Wrap(float value, float min, float max)
```

Wraps `value` between `min` and `max`.
Usable for creating loop-alike behavior or infinite surfaces.
If `min` is `0`, this is equivalent
to `PosMod()`, so prefer using that instead.

参数：
- `value`：The value to wrap.
- `min`：The minimum allowed value and lower bound of the range.
- `max`：The maximum allowed value and upper bound of the range.

返回：The wrapped value.

### Wrap

```csharp
public static int Wrap(int value, int min, int max)
```

Wraps `value` between `min` and `max`.
Usable for creating loop-alike behavior or infinite surfaces.
If `min` is `0`, this is equivalent
to `PosMod()`, so prefer using that instead.

参数：
- `value`：The value to wrap.
- `min`：The minimum allowed value and lower bound of the range.
- `max`：The maximum allowed value and upper bound of the range.

返回：The wrapped value.

## 常量

### E

```csharp
public const float E = 2.7182817f
```

The natural number `e`.

### Epsilon

```csharp
public const float Epsilon = 1E-06f
```

A very small number used for float comparison with error tolerance.
1e-06 with single-precision floats, but 1e-14 if `REAL_T_IS_DOUBLE`.

### Inf

```csharp
public const float Inf = Infinityf
```

Positive infinity. For negative infinity, use `-Mathf.Inf`.

### NaN

```csharp
public const float NaN = NaNf
```

"Not a Number", an invalid value. `NaN` has special properties, including
that it is not equal to itself. It is output by some invalid operations,
such as dividing zero by zero.

### Pi

```csharp
public const float Pi = 3.1415927f
```

Constant that represents how many times the diameter of a circle
fits around its perimeter. This is equivalent to `Mathf.Tau / 2`.

### Sqrt2

```csharp
public const float Sqrt2 = 1.4142135f
```

The square root of 2.

### Tau

```csharp
public const float Tau = 6.2831855f
```

The circle constant, the circumference of the unit circle in radians.
