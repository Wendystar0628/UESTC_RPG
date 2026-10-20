<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Vector2

命名空间 `Godot`；struct

2-element structure that can be used to represent positions in 2D space or any other pair of numeric values.

## 构造函数

### Vector2

```csharp
public Vector2(float x, float y)
```

Constructs a new `Vector2` with the given components.

参数：
- `x`：The vector's X component.
- `y`：The vector's Y component.

## 属性

### Down

```csharp
public static Vector2 Down { get; }
```

Down unit vector. Y is down in 2D, so this vector points +Y.

### Inf

```csharp
public static Vector2 Inf { get; }
```

Infinity vector, a vector with all components set to [`Mathf.Inf`](Mathf.md).

### Item

```csharp
public float this[int index] { get; set; }
```

Access vector components using their index.

### Left

```csharp
public static Vector2 Left { get; }
```

Left unit vector. Represents the direction of left.

### One

```csharp
public static Vector2 One { get; }
```

One vector, a vector with all components set to `1`.

### Right

```csharp
public static Vector2 Right { get; }
```

Right unit vector. Represents the direction of right.

### Up

```csharp
public static Vector2 Up { get; }
```

Up unit vector. Y is down in 2D, so this vector points -Y.

### Zero

```csharp
public static Vector2 Zero { get; }
```

Zero vector, a vector with all components set to `0`.

## 方法

### Abs

```csharp
public Vector2 Abs()
```

引擎名：`abs`

Returns a new vector with all components in absolute values (i.e. positive).

返回：A vector with [`Mathf.Abs`](Mathf.md) called on each component.

### Angle

```csharp
public float Angle()
```

引擎名：`angle`

Returns this vector's angle with respect to the X axis, or (1, 0) vector, in radians.

Equivalent to the result of [`Mathf.Atan2`](Mathf.md) when
called with the vector's `Y` and `X` as parameters: `Mathf.Atan2(v.Y, v.X)`.

返回：The angle of this vector, in radians.

### AngleTo

```csharp
public float AngleTo(Vector2 to)
```

引擎名：`angle_to`

Returns the angle to the given vector, in radians.

参数：
- `to`：The other vector to compare this vector to.

返回：The angle between the two vectors, in radians.

### AngleToPoint

```csharp
public float AngleToPoint(Vector2 to)
```

引擎名：`angle_to_point`

Returns the angle between the line connecting the two points and the X axis, in radians.

参数：
- `to`：The other vector to compare this vector to.

返回：The angle between the two vectors, in radians.

### Aspect

```csharp
public float Aspect()
```

引擎名：`aspect`

Returns the aspect ratio of this vector, the ratio of `X` to `Y`.

返回：The `X` component divided by the `Y` component.

### BezierDerivative

```csharp
public Vector2 BezierDerivative(Vector2 control1, Vector2 control2, Vector2 end, float t)
```

引擎名：`bezier_derivative`

Returns the derivative at the given `t` on the Bezier curve defined by this vector
and the given `control1`, `control2`, and `end` points.

参数：
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination value for the interpolation.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting value of the interpolation.

### BezierInterpolate

```csharp
public Vector2 BezierInterpolate(Vector2 control1, Vector2 control2, Vector2 end, float t)
```

引擎名：`bezier_interpolate`

Returns the point at the given `t` on a one-dimensional Bezier curve defined by this vector
and the given `control1`, `control2`, and `end` points.

参数：
- `control1`：Control point that defines the bezier curve.
- `control2`：Control point that defines the bezier curve.
- `end`：The destination vector.
- `t`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated vector.

### Bounce

```csharp
public Vector2 Bounce(Vector2 normal)
```

引擎名：`bounce`

Returns the vector "bounced off" from a plane defined by the given normal.

参数：
- `normal`：The normal vector defining the plane to bounce off. Must be normalized.

返回：The bounced vector.

### Ceil

```csharp
public Vector2 Ceil()
```

引擎名：`ceil`

Returns a new vector with all components rounded up (towards positive infinity).

返回：A vector with [`Mathf.Ceil`](Mathf.md) called on each component.

### Clamp

```csharp
public Vector2 Clamp(Vector2 min, Vector2 max)
```

引擎名：`clamp`

Returns a new vector with all components clamped between the
components of `min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The vector with minimum allowed values.
- `max`：The vector with maximum allowed values.

返回：The vector with all components clamped.

### Clamp

```csharp
public Vector2 Clamp(float min, float max)
```

引擎名：`clamp`

Returns a new vector with all components clamped between the
`min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The vector with all components clamped.

### Cross

```csharp
public float Cross(Vector2 with)
```

引擎名：`cross`

Returns the cross product of this vector and `with`.

参数：
- `with`：The other vector.

返回：The cross product value.

### CubicInterpolate

```csharp
public Vector2 CubicInterpolate(Vector2 b, Vector2 preA, Vector2 postB, float weight)
```

引擎名：`cubic_interpolate`

Performs a cubic interpolation between vectors `preA`, this vector,
`b`, and `postB`, by the given amount `weight`.

参数：
- `b`：The destination vector.
- `preA`：A vector before this vector.
- `postB`：A vector after `b`.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated vector.

### CubicInterpolateInTime

```csharp
public Vector2 CubicInterpolateInTime(Vector2 b, Vector2 preA, Vector2 postB, float weight, float t, float preAT, float postBT)
```

引擎名：`cubic_interpolate_in_time`

Performs a cubic interpolation between vectors `preA`, this vector,
`b`, and `postB`, by the given amount `weight`.
It can perform smoother interpolation than `CubicInterpolate()`
by the time values.

参数：
- `b`：The destination vector.
- `preA`：A vector before this vector.
- `postB`：A vector after `b`.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated vector.

### Deconstruct

```csharp
public void Deconstruct(out float x, out float y)
```

Helper method for deconstruction into a tuple.

### DirectionTo

```csharp
public Vector2 DirectionTo(Vector2 to)
```

引擎名：`direction_to`

Returns the normalized vector pointing from this vector to `to`.

参数：
- `to`：The other vector to point towards.

返回：The direction from this vector to `to`.

### DistanceSquaredTo

```csharp
public float DistanceSquaredTo(Vector2 to)
```

引擎名：`distance_squared_to`

Returns the squared distance between this vector and `to`.
This method runs faster than `DistanceTo()`, so prefer it if
you need to compare vectors or need the squared distance for some formula.

参数：
- `to`：The other vector to use.

返回：The squared distance between the two vectors.

### DistanceTo

```csharp
public float DistanceTo(Vector2 to)
```

引擎名：`distance_to`

Returns the distance between this vector and `to`.

参数：
- `to`：The other vector to use.

返回：The distance between the two vectors.

### Dot

```csharp
public float Dot(Vector2 with)
```

引擎名：`dot`

Returns the dot product of this vector and `with`.

参数：
- `with`：The other vector to use.

返回：The dot product of the two vectors.

### Equals

```csharp
public bool Equals(Vector2 other)
```

Returns `true` if the vectors are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `other`：The other vector.

返回：Whether or not the vectors are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the vector is exactly equal
to the given object (`obj`).
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `obj`：The object to compare with.

返回：Whether or not the vector and the object are equal.

### Floor

```csharp
public Vector2 Floor()
```

引擎名：`floor`

Returns a new vector with all components rounded down (towards negative infinity).

返回：A vector with [`Mathf.Floor`](Mathf.md) called on each component.

### FromAngle

```csharp
public static Vector2 FromAngle(float angle)
```

引擎名：`from_angle`

Creates a unit Vector2 rotated to the given angle. This is equivalent to doing
`Vector2(Mathf.Cos(angle), Mathf.Sin(angle))` or `Vector2.Right.Rotated(angle)`.

参数：
- `angle`：Angle of the vector, in radians.

返回：The resulting vector.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Vector2`.

返回：A hash code for this vector.

### Inverse

```csharp
public Vector2 Inverse()
```

Returns the inverse of this vector. This is the same as `new Vector2(1 / v.X, 1 / v.Y)`.

返回：The inverse of this vector.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Vector2 other)
```

引擎名：`is_equal_approx`

Returns `true` if this vector and `other` are approximately equal,
by running [`Mathf.IsEqualApprox`](Mathf.md) on each component.

参数：
- `other`：The other vector to compare.

返回：Whether or not the vectors are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this vector is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### IsNormalized

```csharp
public bool IsNormalized()
```

引擎名：`is_normalized`

Returns `true` if the vector is normalized, and `false` otherwise.

返回：A `bool` indicating whether or not the vector is normalized.

### IsZeroApprox

```csharp
public bool IsZeroApprox()
```

引擎名：`is_zero_approx`

Returns `true` if this vector's values are approximately zero,
by running [`Mathf.IsZeroApprox`](Mathf.md) on each component.
This method is faster than using `IsEqualApprox()` with one value
as a zero vector.

返回：Whether or not the vector is approximately zero.

### Length

```csharp
public float Length()
```

引擎名：`length`

Returns the length (magnitude) of this vector.

返回：The length of this vector.

### LengthSquared

```csharp
public float LengthSquared()
```

引擎名：`length_squared`

Returns the squared length (squared magnitude) of this vector.
This method runs faster than `Length()`, so prefer it if
you need to compare vectors or need the squared length for some formula.

返回：The squared length of this vector.

### Lerp

```csharp
public Vector2 Lerp(Vector2 to, float weight)
```

引擎名：`lerp`

Returns the result of the linear interpolation between
this vector and `to` by amount `weight`.

参数：
- `to`：The destination vector for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting vector of the interpolation.

### LimitLength

```csharp
public Vector2 LimitLength(float length = 1f)
```

引擎名：`limit_length`

Returns the vector with a maximum length by limiting its length to `length`.

参数：
- `length`：The length to limit to.

返回：The vector with its length limited.

### Max

```csharp
public Vector2 Max(Vector2 with)
```

引擎名：`max`

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector2(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y))`.

参数：
- `with`：The other vector to use.

返回：The resulting maximum vector.

### Max

```csharp
public Vector2 Max(float with)
```

引擎名：`max`

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector2(Mathf.Max(X, with), Mathf.Max(Y, with))`.

参数：
- `with`：The other value to use.

返回：The resulting maximum vector.

### MaxAxisIndex

```csharp
public Vector2.Axis MaxAxisIndex()
```

引擎名：`max_axis_index`

Returns the axis of the vector's highest value. See [`Vector2.Axis`](Vector2.md).
If both components are equal, this method returns [`Vector2.Axis.X`](Vector2.md).

返回：The index of the highest axis.

### Min

```csharp
public Vector2 Min(Vector2 with)
```

引擎名：`min`

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector2(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y))`.

参数：
- `with`：The other vector to use.

返回：The resulting minimum vector.

### Min

```csharp
public Vector2 Min(float with)
```

引擎名：`min`

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector2(Mathf.Min(X, with), Mathf.Min(Y, with))`.

参数：
- `with`：The other value to use.

返回：The resulting minimum vector.

### MinAxisIndex

```csharp
public Vector2.Axis MinAxisIndex()
```

引擎名：`min_axis_index`

Returns the axis of the vector's lowest value. See [`Vector2.Axis`](Vector2.md).
If both components are equal, this method returns [`Vector2.Axis.Y`](Vector2.md).

返回：The index of the lowest axis.

### MoveToward

```csharp
public Vector2 MoveToward(Vector2 to, float delta)
```

引擎名：`move_toward`

Returns a new vector moved toward `to` by the fixed `delta` amount. Will not go past the final value.

参数：
- `to`：The vector to move towards.
- `delta`：The amount to move towards by.

返回：The resulting vector.

### Normalized

```csharp
public Vector2 Normalized()
```

引擎名：`normalized`

Returns the vector scaled to unit length. Equivalent to `v / v.Length()`.

返回：A normalized version of the vector.

### Orthogonal

```csharp
public Vector2 Orthogonal()
```

引擎名：`orthogonal`

Returns a perpendicular vector rotated 90 degrees counter-clockwise
compared to the original, with the same length.

返回：The perpendicular vector.

### PosMod

```csharp
public Vector2 PosMod(Vector2 modv)
```

Returns a vector composed of the [`Mathf.PosMod`](Mathf.md) of this vector's components
and `modv`'s components.

参数：
- `modv`：A vector representing the divisors of the operation.

返回：A vector with each component [`Mathf.PosMod`](Mathf.md) by `modv`'s components.

### PosMod

```csharp
public Vector2 PosMod(float mod)
```

Returns a vector composed of the [`Mathf.PosMod`](Mathf.md) of this vector's components
and `mod`.

参数：
- `mod`：A value representing the divisor of the operation.

返回：A vector with each component [`Mathf.PosMod`](Mathf.md) by `mod`.

### Project

```csharp
public Vector2 Project(Vector2 onNormal)
```

引擎名：`project`

Returns a new vector resulting from projecting this vector onto the given vector `onNormal`.
The resulting new vector is parallel to `onNormal`.
See also `Slide()`.
Note: If the vector `onNormal` is a zero vector, the components of the resulting new vector will be `System.Single.NaN`.

参数：
- `onNormal`：The vector to project onto.

返回：The projected vector.

### Reflect

```csharp
public Vector2 Reflect(Vector2 normal)
```

引擎名：`reflect`

Returns this vector reflected from a plane defined by the given `normal`.

参数：
- `normal`：The normal vector defining the plane to reflect from. Must be normalized.

返回：The reflected vector.

### Rotated

```csharp
public Vector2 Rotated(float angle)
```

引擎名：`rotated`

Rotates this vector by `angle` radians.

参数：
- `angle`：The angle to rotate by, in radians.

返回：The rotated vector.

### Round

```csharp
public Vector2 Round()
```

引擎名：`round`

Returns this vector with all components rounded to the nearest integer,
with halfway cases rounded towards the nearest multiple of two.

返回：The rounded vector.

### Sign

```csharp
public Vector2 Sign()
```

引擎名：`sign`

Returns a vector with each component set to one or negative one, depending
on the signs of this vector's components, or zero if the component is zero,
by calling [`Mathf.Sign`](Mathf.md) on each component.

返回：A vector with all components as either `1`, `-1`, or `0`.

### Slerp

```csharp
public Vector2 Slerp(Vector2 to, float weight)
```

引擎名：`slerp`

Returns the result of the spherical linear interpolation between
this vector and `to` by amount `weight`.

This method also handles interpolating the lengths if the input vectors
have different lengths. For the special case of one or both input vectors
having zero length, this method behaves like `Lerp()`.

参数：
- `to`：The destination vector for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting vector of the interpolation.

### Slide

```csharp
public Vector2 Slide(Vector2 normal)
```

引擎名：`slide`

Returns a new vector resulting from sliding this vector along a line with normal `normal`.
The resulting new vector is perpendicular to `normal`, and is equivalent to this vector minus its projection on `normal`.
See also `Project()`.
Note: The vector `normal` must be normalized. See also `Normalized()`.

参数：
- `normal`：The normal vector of the plane to slide on.

返回：The slid vector.

### Snapped

```csharp
public Vector2 Snapped(Vector2 step)
```

引擎名：`snapped`

Returns a new vector with each component snapped to the nearest multiple of the corresponding component in `step`.
This can also be used to round to an arbitrary number of decimals.

参数：
- `step`：A vector value representing the step size to snap to.

返回：The snapped vector.

### Snapped

```csharp
public Vector2 Snapped(float step)
```

引擎名：`snapped`

Returns a new vector with each component snapped to the nearest multiple of `step`.
This can also be used to round to an arbitrary number of decimals.

参数：
- `step`：The step size to snap to.

返回：The snapped vector.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Vector2` to a string with the given `format`.

返回：A string representation of this vector.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Vector2` to a string.

返回：A string representation of this vector.

## 运算符

### op_Addition

```csharp
public static Vector2 op_Addition(Vector2 left, Vector2 right)
```

Adds each component of the `Vector2`
with the components of the given `Vector2`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The added vector.

### op_Division

```csharp
public static Vector2 op_Division(Vector2 vec, Vector2 divisorv)
```

Divides each component of the `Vector2`
by the components of the given `Vector2`.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The divided vector.

### op_Division

```csharp
public static Vector2 op_Division(Vector2 vec, float divisor)
```

Divides each component of the `Vector2`
by the given `System.Single`.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The divided vector.

### op_Equality

```csharp
public static bool op_Equality(Vector2 left, Vector2 right)
```

Returns `true` if the vectors are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are exactly equal.

### op_GreaterThan

```csharp
public static bool op_GreaterThan(Vector2 left, Vector2 right)
```

Compares two `Vector2` vectors by first checking if
the X value of the `left` vector is greater than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than the right.

### op_GreaterThanOrEqual

```csharp
public static bool op_GreaterThanOrEqual(Vector2 left, Vector2 right)
```

Compares two `Vector2` vectors by first checking if
the X value of the `left` vector is greater than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than or equal to the right.

### op_Inequality

```csharp
public static bool op_Inequality(Vector2 left, Vector2 right)
```

Returns `true` if the vectors are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are not equal.

### op_LessThan

```csharp
public static bool op_LessThan(Vector2 left, Vector2 right)
```

Compares two `Vector2` vectors by first checking if
the X value of the `left` vector is less than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than the right.

### op_LessThanOrEqual

```csharp
public static bool op_LessThanOrEqual(Vector2 left, Vector2 right)
```

Compares two `Vector2` vectors by first checking if
the X value of the `left` vector is less than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than or equal to the right.

### op_Modulus

```csharp
public static Vector2 op_Modulus(Vector2 vec, Vector2 divisorv)
```

Gets the remainder of each component of the `Vector2`
with the components of the given `Vector2`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using `PosMod()` instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The remainder vector.

### op_Modulus

```csharp
public static Vector2 op_Modulus(Vector2 vec, float divisor)
```

Gets the remainder of each component of the `Vector2`
with the components of the given `System.Single`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using `PosMod()` instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The remainder vector.

### op_Multiply

```csharp
public static Vector2 op_Multiply(Vector2 left, Vector2 right)
```

Multiplies each component of the `Vector2`
by the components of the given `Vector2`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector2 op_Multiply(Vector2 vec, float scale)
```

Multiplies each component of the `Vector2`
by the given `System.Single`.

参数：
- `vec`：The vector to multiply.
- `scale`：The scale to multiply by.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector2 op_Multiply(float scale, Vector2 vec)
```

Multiplies each component of the `Vector2`
by the given `System.Single`.

参数：
- `scale`：The scale to multiply by.
- `vec`：The vector to multiply.

返回：The multiplied vector.

### op_Subtraction

```csharp
public static Vector2 op_Subtraction(Vector2 left, Vector2 right)
```

Subtracts each component of the `Vector2`
by the components of the given `Vector2`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The subtracted vector.

### op_UnaryNegation

```csharp
public static Vector2 op_UnaryNegation(Vector2 vec)
```

Returns the negative value of the `Vector2`.
This is the same as writing `new Vector2(-v.X, -v.Y)`.
This operation flips the direction of the vector while
keeping the same magnitude.
With floats, the number zero can be either positive or negative.

参数：
- `vec`：The vector to negate/flip.

返回：The negated/flipped vector.

## 字段

### X

```csharp
public float X
```

The vector's X component. Also accessible by using the index position `[0]`.

### Y

```csharp
public float Y
```

The vector's Y component. Also accessible by using the index position `[1]`.

## Axis

命名空间 `Godot`；enum

Enumerated index values for the axes.
Returned by [`Vector2.MaxAxisIndex`](Vector2.md) and [`Vector2.MinAxisIndex`](Vector2.md).

### 枚举值

- `X = 0`：The vector's X axis.
- `Y = 1`：The vector's Y axis.
