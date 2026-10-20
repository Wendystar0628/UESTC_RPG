<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Vector3I

命名空间 `Godot`；struct

3-element structure that can be used to represent 3D grid coordinates or sets of integers.

## 构造函数

### Vector3I

```csharp
public Vector3I(int x, int y, int z)
```

Constructs a new `Vector3I` with the given components.

参数：
- `x`：The vector's X component.
- `y`：The vector's Y component.
- `z`：The vector's Z component.

## 属性

### Back

```csharp
public static Vector3I Back { get; }
```

Back unit vector. Represents the local direction of back,
and the global direction of south.

### Down

```csharp
public static Vector3I Down { get; }
```

Down unit vector.

### Forward

```csharp
public static Vector3I Forward { get; }
```

Forward unit vector. Represents the local direction of forward,
and the global direction of north.

### Item

```csharp
public int this[int index] { get; set; }
```

Access vector components using their `index`.

### Left

```csharp
public static Vector3I Left { get; }
```

Left unit vector. Represents the local direction of left,
and the global direction of west.

### MaxValue

```csharp
public static Vector3I MaxValue { get; }
```

Max vector, a vector with all components equal to `System.Int32.MaxValue`. Can be used as an integer equivalent of [`Vector3.Inf`](Vector3.md).

### MinValue

```csharp
public static Vector3I MinValue { get; }
```

Min vector, a vector with all components equal to `System.Int32.MinValue`. Can be used as a negative integer equivalent of [`Vector3.Inf`](Vector3.md).

### One

```csharp
public static Vector3I One { get; }
```

One vector, a vector with all components set to `1`.

### Right

```csharp
public static Vector3I Right { get; }
```

Right unit vector. Represents the local direction of right,
and the global direction of east.

### Up

```csharp
public static Vector3I Up { get; }
```

Up unit vector.

### Zero

```csharp
public static Vector3I Zero { get; }
```

Zero vector, a vector with all components set to `0`.

## 方法

### Abs

```csharp
public Vector3I Abs()
```

Returns a new vector with all components in absolute values (i.e. positive).

返回：A vector with [`Mathf.Abs`](Mathf.md) called on each component.

### Clamp

```csharp
public Vector3I Clamp(Vector3I min, Vector3I max)
```

Returns a new vector with all components clamped between the
components of `min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The vector with minimum allowed values.
- `max`：The vector with maximum allowed values.

返回：The vector with all components clamped.

### Clamp

```csharp
public Vector3I Clamp(int min, int max)
```

Returns a new vector with all components clamped between the
`min` and `max` using
[`Mathf.Clamp`](Mathf.md).

参数：
- `min`：The minimum allowed value.
- `max`：The maximum allowed value.

返回：The vector with all components clamped.

### Deconstruct

```csharp
public void Deconstruct(out int x, out int y, out int z)
```

Helper method for deconstruction into a tuple.

### DistanceSquaredTo

```csharp
public int DistanceSquaredTo(Vector3I to)
```

Returns the squared distance between this vector and `to`.
This method runs faster than `DistanceTo()`, so prefer it if
you need to compare vectors or need the squared distance for some formula.

参数：
- `to`：The other vector to use.

返回：The squared distance between the two vectors.

### DistanceTo

```csharp
public float DistanceTo(Vector3I to)
```

Returns the distance between this vector and `to`.

参数：
- `to`：The other vector to use.

返回：The distance between the two vectors.

### Equals

```csharp
public bool Equals(Vector3I other)
```

Returns `true` if the vectors are equal.

参数：
- `other`：The other vector.

返回：Whether or not the vectors are equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the vector is equal
to the given object (`obj`).

参数：
- `obj`：The object to compare with.

返回：Whether or not the vector and the object are equal.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Vector3I`.

返回：A hash code for this vector.

### Length

```csharp
public float Length()
```

Returns the length (magnitude) of this vector.

返回：The length of this vector.

### LengthSquared

```csharp
public int LengthSquared()
```

Returns the squared length (squared magnitude) of this vector.
This method runs faster than `Length()`, so prefer it if
you need to compare vectors or need the squared length for some formula.

返回：The squared length of this vector.

### Max

```csharp
public Vector3I Max(Vector3I with)
```

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector3I(Mathf.Max(X, with.X), Mathf.Max(Y, with.Y), Mathf.Max(Z, with.Z))`.

参数：
- `with`：The other vector to use.

返回：The resulting maximum vector.

### Max

```csharp
public Vector3I Max(int with)
```

Returns the result of the component-wise maximum between
this vector and `with`.
Equivalent to `new Vector3I(Mathf.Max(X, with), Mathf.Max(Y, with), Mathf.Max(Z, with))`.

参数：
- `with`：The other value to use.

返回：The resulting maximum vector.

### MaxAxisIndex

```csharp
public Vector3I.Axis MaxAxisIndex()
```

Returns the axis of the vector's highest value. See [`Vector3I.Axis`](Vector3I.md).
If all components are equal, this method returns [`Vector3I.Axis.X`](Vector3I.md).

返回：The index of the highest axis.

### Min

```csharp
public Vector3I Min(Vector3I with)
```

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector3I(Mathf.Min(X, with.X), Mathf.Min(Y, with.Y), Mathf.Min(Z, with.Z))`.

参数：
- `with`：The other vector to use.

返回：The resulting minimum vector.

### Min

```csharp
public Vector3I Min(int with)
```

Returns the result of the component-wise minimum between
this vector and `with`.
Equivalent to `new Vector3I(Mathf.Min(X, with), Mathf.Min(Y, with), Mathf.Min(Z, with))`.

参数：
- `with`：The other value to use.

返回：The resulting minimum vector.

### MinAxisIndex

```csharp
public Vector3I.Axis MinAxisIndex()
```

Returns the axis of the vector's lowest value. See [`Vector3I.Axis`](Vector3I.md).
If all components are equal, this method returns [`Vector3I.Axis.Z`](Vector3I.md).

返回：The index of the lowest axis.

### Sign

```csharp
public Vector3I Sign()
```

Returns a vector with each component set to one or negative one, depending
on the signs of this vector's components, or zero if the component is zero,
by calling [`Mathf.Sign`](Mathf.md) on each component.

返回：A vector with all components as either `1`, `-1`, or `0`.

### Snapped

```csharp
public Vector3I Snapped(Vector3I step)
```

Returns a new vector with each component snapped to the closest multiple of the corresponding component in `step`.

参数：
- `step`：A vector value representing the step size to snap to.

返回：The snapped vector.

### Snapped

```csharp
public Vector3I Snapped(int step)
```

Returns a new vector with each component snapped to the closest multiple of `step`.

参数：
- `step`：The step size to snap to.

返回：The snapped vector.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Vector3I` to a string with the given `format`.

返回：A string representation of this vector.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Vector3I` to a string.

返回：A string representation of this vector.

## 运算符

### op_Addition

```csharp
public static Vector3I op_Addition(Vector3I left, Vector3I right)
```

Adds each component of the `Vector3I`
with the components of the given `Vector3I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The added vector.

### op_Division

```csharp
public static Vector3I op_Division(Vector3I vec, Vector3I divisorv)
```

Divides each component of the `Vector3I`
by the components of the given `Vector3I`.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The divided vector.

### op_Division

```csharp
public static Vector3I op_Division(Vector3I vec, int divisor)
```

Divides each component of the `Vector3I`
by the given `int`.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The divided vector.

### op_Equality

```csharp
public static bool op_Equality(Vector3I left, Vector3I right)
```

Returns `true` if the vectors are equal.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are equal.

### op_Explicit

```csharp
public static Vector3I op_Explicit(Vector3 value)
```

Converts a [`Vector3`](Vector3.md) to a `Vector3I` by truncating
components' fractional parts (rounding towards zero). For a different
behavior consider passing the result of [`Vector3.Ceil`](Vector3.md),
[`Vector3.Floor`](Vector3.md) or [`Vector3.Round`](Vector3.md) to this conversion operator instead.

参数：
- `value`：The vector to convert.

### op_GreaterThan

```csharp
public static bool op_GreaterThan(Vector3I left, Vector3I right)
```

Compares two `Vector3I` vectors by first checking if
the X value of the `left` vector is greater than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than the right.

### op_GreaterThanOrEqual

```csharp
public static bool op_GreaterThanOrEqual(Vector3I left, Vector3I right)
```

Compares two `Vector3I` vectors by first checking if
the X value of the `left` vector is greater than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is greater than or equal to the right.

### op_Implicit

```csharp
public static Vector3 op_Implicit(Vector3I value)
```

Converts this `Vector3I` to a [`Vector3`](Vector3.md).

参数：
- `value`：The vector to convert.

### op_Inequality

```csharp
public static bool op_Inequality(Vector3I left, Vector3I right)
```

Returns `true` if the vectors are not equal.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the vectors are not equal.

### op_LessThan

```csharp
public static bool op_LessThan(Vector3I left, Vector3I right)
```

Compares two `Vector3I` vectors by first checking if
the X value of the `left` vector is less than
the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than the right.

### op_LessThanOrEqual

```csharp
public static bool op_LessThanOrEqual(Vector3I left, Vector3I right)
```

Compares two `Vector3I` vectors by first checking if
the X value of the `left` vector is less than
or equal to the X value of the `right` vector.
If the X values are exactly equal, then it repeats this check
with the Y values of the two vectors, and then with the Z values.
This operator is useful for sorting vectors.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：Whether or not the left is less than or equal to the right.

### op_Modulus

```csharp
public static Vector3I op_Modulus(Vector3I vec, Vector3I divisorv)
```

Gets the remainder of each component of the `Vector3I`
with the components of the given `Vector3I`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using [`Mathf.PosMod`](Mathf.md) instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisorv`：The divisor vector.

返回：The remainder vector.

### op_Modulus

```csharp
public static Vector3I op_Modulus(Vector3I vec, int divisor)
```

Gets the remainder of each component of the `Vector3I`
with the components of the given `int`.
This operation uses truncated division, which is often not desired
as it does not work well with negative numbers.
Consider using [`Mathf.PosMod`](Mathf.md) instead
if you want to handle negative numbers.

参数：
- `vec`：The dividend vector.
- `divisor`：The divisor value.

返回：The remainder vector.

### op_Multiply

```csharp
public static Vector3I op_Multiply(Vector3I left, Vector3I right)
```

Multiplies each component of the `Vector3I`
by the components of the given `Vector3I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector3I op_Multiply(Vector3I vec, int scale)
```

Multiplies each component of the `Vector3I`
by the given `int`.

参数：
- `vec`：The vector to multiply.
- `scale`：The scale to multiply by.

返回：The multiplied vector.

### op_Multiply

```csharp
public static Vector3I op_Multiply(int scale, Vector3I vec)
```

Multiplies each component of the `Vector3I`
by the given `int`.

参数：
- `scale`：The scale to multiply by.
- `vec`：The vector to multiply.

返回：The multiplied vector.

### op_Subtraction

```csharp
public static Vector3I op_Subtraction(Vector3I left, Vector3I right)
```

Subtracts each component of the `Vector3I`
by the components of the given `Vector3I`.

参数：
- `left`：The left vector.
- `right`：The right vector.

返回：The subtracted vector.

### op_UnaryNegation

```csharp
public static Vector3I op_UnaryNegation(Vector3I vec)
```

Returns the negative value of the `Vector3I`.
This is the same as writing `new Vector3I(-v.X, -v.Y, -v.Z)`.
This operation flips the direction of the vector while
keeping the same magnitude.

参数：
- `vec`：The vector to negate/flip.

返回：The negated/flipped vector.

## 字段

### X

```csharp
public int X
```

The vector's X component. Also accessible by using the index position `[0]`.

### Y

```csharp
public int Y
```

The vector's Y component. Also accessible by using the index position `[1]`.

### Z

```csharp
public int Z
```

The vector's Z component. Also accessible by using the index position `[2]`.

## Axis

命名空间 `Godot`；enum

Enumerated index values for the axes.
Returned by [`Vector3I.MaxAxisIndex`](Vector3I.md) and [`Vector3I.MinAxisIndex`](Vector3I.md).

### 枚举值

- `X = 0`：The vector's X axis.
- `Y = 1`：The vector's Y axis.
- `Z = 2`：The vector's Z axis.
