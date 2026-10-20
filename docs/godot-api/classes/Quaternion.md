<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Quaternion

命名空间 `Godot`；struct

A unit quaternion used for representing 3D rotations.
Quaternions need to be normalized to be used for rotation.

It is similar to [`Basis`](Basis.md), which implements matrix
representation of rotations, and can be parametrized using both
an axis-angle pair or Euler angles. Basis stores rotation, scale,
and shearing, while Quaternion only stores rotation.

Due to its compactness and the way it is stored in memory, certain
operations (obtaining axis-angle and performing SLERP, in particular)
are more efficient and robust against floating-point errors.

## 构造函数

### Quaternion

```csharp
public Quaternion(Basis basis)
```

Constructs a `Quaternion` from the given [`Basis`](Basis.md).

参数：
- `basis`：The [`Basis`](Basis.md) to construct from.

### Quaternion

```csharp
public Quaternion(Vector3 arcFrom, Vector3 arcTo)
```

### Quaternion

```csharp
public Quaternion(Vector3 axis, float angle)
```

Constructs a `Quaternion` that will rotate around the given axis
by the specified angle. The axis must be a normalized vector.

参数：
- `axis`：The axis to rotate around. Must be normalized.
- `angle`：The angle to rotate, in radians.

### Quaternion

```csharp
public Quaternion(float x, float y, float z, float w)
```

Constructs a `Quaternion` defined by the given values.

参数：
- `x`：X component of the quaternion (imaginary `i` axis part).
- `y`：Y component of the quaternion (imaginary `j` axis part).
- `z`：Z component of the quaternion (imaginary `k` axis part).
- `w`：W component of the quaternion (real part).

## 属性

### Identity

```csharp
public static Quaternion Identity { get; }
```

The identity quaternion, representing no rotation.
Equivalent to an identity [`Basis`](Basis.md) matrix. If a vector is transformed by
an identity quaternion, it will not change.

### Item

```csharp
public float this[int index] { get; set; }
```

Access quaternion components using their index.

## 方法

### AngleTo

```csharp
public float AngleTo(Quaternion to)
```

引擎名：`angle_to`

Returns the angle between this quaternion and `to`.
This is the magnitude of the angle you would need to rotate
by to get from one to the other.

Note: This method has an abnormally high amount
of floating-point error, so methods such as
[`Mathf.IsZeroApprox`](Mathf.md) will not work reliably.

参数：
- `to`：The other quaternion.

返回：The angle between the quaternions.

### Dot

```csharp
public float Dot(Quaternion b)
```

引擎名：`dot`

Returns the dot product of two quaternions.

参数：
- `b`：The other quaternion.

返回：The dot product.

### Equals

```csharp
public bool Equals(Quaternion other)
```

Returns `true` if this quaternion and `other` are equal.

参数：
- `other`：The other quaternion to compare.

返回：Whether or not the quaternions are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if this quaternion and `obj` are equal.

参数：
- `obj`：The other object to compare.

返回：Whether or not the quaternion and the other object are exactly equal.

### Exp

```csharp
public Quaternion Exp()
```

引擎名：`exp`

### FromEuler

```csharp
public static Quaternion FromEuler(Vector3 eulerYXZ)
```

引擎名：`from_euler`

Constructs a `Quaternion` that will perform a rotation specified by
Euler angles (in the YXZ convention: when decomposing, first Z, then X, and Y last),
given in the vector format as (X angle, Y angle, Z angle).

参数：
- `eulerYXZ`：Euler angles that the quaternion will be rotated by.

### GetAngle

```csharp
public float GetAngle()
```

引擎名：`get_angle`

### GetAxis

```csharp
public Vector3 GetAxis()
```

引擎名：`get_axis`

### GetEuler

```csharp
public Vector3 GetEuler(EulerOrder order = EulerOrder.Yxz)
```

引擎名：`get_euler`

Returns Euler angles (in the YXZ convention: when decomposing,
first Z, then X, and Y last) corresponding to the rotation
represented by the unit quaternion. Returned vector contains
the rotation angles in the format (X angle, Y angle, Z angle).

返回：The Euler angle representation of this quaternion.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Quaternion`.

返回：A hash code for this quaternion.

### Inverse

```csharp
public Quaternion Inverse()
```

引擎名：`inverse`

Returns the inverse of the quaternion.

返回：The inverse quaternion.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Quaternion other)
```

引擎名：`is_equal_approx`

Returns `true` if this quaternion and `other` are approximately equal,
by running [`Mathf.IsEqualApprox`](Mathf.md) on each component.

参数：
- `other`：The other quaternion to compare.

返回：Whether or not the quaternions are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this quaternion is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### IsNormalized

```csharp
public bool IsNormalized()
```

引擎名：`is_normalized`

Returns whether the quaternion is normalized or not.

返回：A `bool` for whether the quaternion is normalized or not.

### Length

```csharp
public float Length()
```

引擎名：`length`

Returns the length (magnitude) of the quaternion.

### LengthSquared

```csharp
public float LengthSquared()
```

引擎名：`length_squared`

Returns the squared length (squared magnitude) of the quaternion.
This method runs faster than `Length()`, so prefer it if
you need to compare quaternions or need the squared length for some formula.

### Log

```csharp
public Quaternion Log()
```

引擎名：`log`

### Normalized

```csharp
public Quaternion Normalized()
```

引擎名：`normalized`

Returns a copy of the quaternion, normalized to unit length.

返回：The normalized quaternion.

### Slerp

```csharp
public Quaternion Slerp(Quaternion to, float weight)
```

引擎名：`slerp`

Returns the result of the spherical linear interpolation between
this quaternion and `to` by amount `weight`.

Note: Both quaternions must be normalized.

参数：
- `to`：The destination quaternion for interpolation. Must be normalized.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting quaternion of the interpolation.

### Slerpni

```csharp
public Quaternion Slerpni(Quaternion to, float weight)
```

引擎名：`slerpni`

Returns the result of the spherical linear interpolation between
this quaternion and `to` by amount `weight`, but without
checking if the rotation path is not bigger than 90 degrees.

参数：
- `to`：The destination quaternion for interpolation. Must be normalized.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting quaternion of the interpolation.

### SphericalCubicInterpolate

```csharp
public Quaternion SphericalCubicInterpolate(Quaternion b, Quaternion preA, Quaternion postB, float weight)
```

引擎名：`spherical_cubic_interpolate`

Performs a spherical cubic interpolation between quaternions `preA`, this quaternion,
`b`, and `postB`, by the given amount `weight`.

参数：
- `b`：The destination quaternion.
- `preA`：A quaternion before this quaternion.
- `postB`：A quaternion after `b`.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated quaternion.

### SphericalCubicInterpolateInTime

```csharp
public Quaternion SphericalCubicInterpolateInTime(Quaternion b, Quaternion preA, Quaternion postB, float weight, float bT, float preAT, float postBT)
```

引擎名：`spherical_cubic_interpolate_in_time`

Performs a spherical cubic interpolation between quaternions `preA`, this quaternion,
`b`, and `postB`, by the given amount `weight`.
It can perform smoother interpolation than `SphericalCubicInterpolate()`
by the time values.

参数：
- `b`：The destination quaternion.
- `preA`：A quaternion before this quaternion.
- `postB`：A quaternion after `b`.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The interpolated quaternion.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Quaternion` to a string with the given `format`.

返回：A string representation of this quaternion.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Quaternion` to a string.

返回：A string representation of this quaternion.

## 运算符

### op_Addition

```csharp
public static Quaternion op_Addition(Quaternion left, Quaternion right)
```

Adds each component of the left `Quaternion`
to the right `Quaternion`. This operation is not
meaningful on its own, but it can be used as a part of a
larger expression, such as approximating an intermediate
rotation between two nearby rotations.

参数：
- `left`：The left quaternion to add.
- `right`：The right quaternion to add.

返回：The added quaternion.

### op_Division

```csharp
public static Quaternion op_Division(Quaternion left, float right)
```

Divides each component of the `Quaternion`
by the given `System.Single`. This operation is not
meaningful on its own, but it can be used as a part of a
larger expression.

参数：
- `left`：The quaternion to divide.
- `right`：The value to divide by.

返回：The divided quaternion.

### op_Equality

```csharp
public static bool op_Equality(Quaternion left, Quaternion right)
```

Returns `true` if the quaternions are exactly equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left quaternion.
- `right`：The right quaternion.

返回：Whether or not the quaternions are exactly equal.

### op_Inequality

```csharp
public static bool op_Inequality(Quaternion left, Quaternion right)
```

Returns `true` if the quaternions are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left quaternion.
- `right`：The right quaternion.

返回：Whether or not the quaternions are not equal.

### op_Multiply

```csharp
public static Quaternion op_Multiply(Quaternion left, Quaternion right)
```

Composes these two quaternions by multiplying them together.
This has the effect of rotating the second quaternion
(the child) by the first quaternion (the parent).

参数：
- `left`：The parent quaternion.
- `right`：The child quaternion.

返回：The composed quaternion.

### op_Multiply

```csharp
public static Quaternion op_Multiply(Quaternion left, float right)
```

Multiplies each component of the `Quaternion`
by the given `System.Single`. This operation is not
meaningful on its own, but it can be used as a part of a
larger expression.

参数：
- `left`：The quaternion to multiply.
- `right`：The value to multiply by.

返回：The multiplied quaternion.

### op_Multiply

```csharp
public static Quaternion op_Multiply(float left, Quaternion right)
```

Multiplies each component of the `Quaternion`
by the given `System.Single`. This operation is not
meaningful on its own, but it can be used as a part of a
larger expression.

参数：
- `left`：The value to multiply by.
- `right`：The quaternion to multiply.

返回：The multiplied quaternion.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Quaternion quaternion, Vector3 vector)
```

Returns a Vector3 rotated (multiplied) by the quaternion.

参数：
- `quaternion`：The quaternion to rotate by.
- `vector`：A Vector3 to transform.

返回：The rotated Vector3.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Vector3 vector, Quaternion quaternion)
```

Returns a Vector3 rotated (multiplied) by the inverse quaternion.
`vector * quaternion` is equivalent to `quaternion.Inverse() * vector`. See `Inverse()`.

参数：
- `vector`：A Vector3 to inversely rotate.
- `quaternion`：The quaternion to rotate by.

返回：The inversely rotated Vector3.

### op_Subtraction

```csharp
public static Quaternion op_Subtraction(Quaternion left, Quaternion right)
```

Subtracts each component of the left `Quaternion`
by the right `Quaternion`. This operation is not
meaningful on its own, but it can be used as a part of a
larger expression.

参数：
- `left`：The left quaternion to subtract.
- `right`：The right quaternion to subtract.

返回：The subtracted quaternion.

### op_UnaryNegation

```csharp
public static Quaternion op_UnaryNegation(Quaternion quat)
```

Returns the negative value of the `Quaternion`.
This is the same as writing
`new Quaternion(-q.X, -q.Y, -q.Z, -q.W)`. This operation
results in a quaternion that represents the same rotation.

参数：
- `quat`：The quaternion to negate.

返回：The negated quaternion.

## 字段

### W

```csharp
public float W
```

W component of the quaternion (real part).
Quaternion components should usually not be manipulated directly.

### X

```csharp
public float X
```

X component of the quaternion (imaginary `i` axis part).
Quaternion components should usually not be manipulated directly.

### Y

```csharp
public float Y
```

Y component of the quaternion (imaginary `j` axis part).
Quaternion components should usually not be manipulated directly.

### Z

```csharp
public float Z
```

Z component of the quaternion (imaginary `k` axis part).
Quaternion components should usually not be manipulated directly.
