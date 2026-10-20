<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Basis

命名空间 `Godot`；struct

3×3 matrix used for 3D rotation and scale.
Almost always used as an orthogonal basis for a Transform.

Contains 3 vector fields X, Y and Z as its columns, which are typically
interpreted as the local basis vectors of a 3D transformation. For such use,
it is composed of a scaling and a rotation matrix, in that order (M = R.S).

Can also be accessed as array of 3D vectors. These vectors are normally
orthogonal to each other, but are not necessarily normalized (due to scaling).

For more information, read this documentation article:
https://docs.godotengine.org/en/latest/tutorials/math/matrices_and_transforms.html

## 构造函数

### Basis

```csharp
public Basis(Quaternion quaternion)
```

Constructs a pure rotation basis matrix from the given quaternion.

参数：
- `quaternion`：The quaternion to create the basis from.

### Basis

```csharp
public Basis(Vector3 axis, float angle)
```

Constructs a pure rotation basis matrix, rotated around the given `axis`
by `angle` (in radians). The axis must be a normalized vector.

参数：
- `axis`：The axis to rotate around. Must be normalized.
- `angle`：The angle to rotate, in radians.

### Basis

```csharp
public Basis(Vector3 column0, Vector3 column1, Vector3 column2)
```

Constructs a basis matrix from 3 axis vectors (matrix columns).

参数：
- `column0`：The X vector, or Column0.
- `column1`：The Y vector, or Column1.
- `column2`：The Z vector, or Column2.

### Basis

```csharp
public Basis(float xx, float yx, float zx, float xy, float yy, float zy, float xz, float yz, float zz)
```

Constructs a transformation matrix from the given components.
Arguments are named such that xy is equal to calling `X.Y`.

参数：
- `xx`：The X component of the X column vector, accessed via `b.X.X` or `[0][0]`.
- `yx`：The X component of the Y column vector, accessed via `b.Y.X` or `[1][0]`.
- `zx`：The X component of the Z column vector, accessed via `b.Z.X` or `[2][0]`.
- `xy`：The Y component of the X column vector, accessed via `b.X.Y` or `[0][1]`.
- `yy`：The Y component of the Y column vector, accessed via `b.Y.Y` or `[1][1]`.
- `zy`：The Y component of the Z column vector, accessed via `b.Y.Y` or `[2][1]`.
- `xz`：The Z component of the X column vector, accessed via `b.X.Y` or `[0][2]`.
- `yz`：The Z component of the Y column vector, accessed via `b.Y.Y` or `[1][2]`.
- `zz`：The Z component of the Z column vector, accessed via `b.Y.Y` or `[2][2]`.

## 属性

### Column0

```csharp
public Vector3 Column0 { get; set; }
```

Column 0 of the basis matrix (the X vector).

### Column1

```csharp
public Vector3 Column1 { get; set; }
```

Column 1 of the basis matrix (the Y vector).

### Column2

```csharp
public Vector3 Column2 { get; set; }
```

Column 2 of the basis matrix (the Z vector).

### FlipX

```csharp
public static Basis FlipX { get; }
```

The basis that will flip something along the X axis when used in a transformation.

### FlipY

```csharp
public static Basis FlipY { get; }
```

The basis that will flip something along the Y axis when used in a transformation.

### FlipZ

```csharp
public static Basis FlipZ { get; }
```

The basis that will flip something along the Z axis when used in a transformation.

### Identity

```csharp
public static Basis Identity { get; }
```

The identity basis, with no rotation or scaling applied.
This is used as a replacement for `Basis()` in GDScript.
Do not use `new Basis()` with no arguments in C#, because it sets all values to zero.

### Item

```csharp
public Vector3 this[int column] { get; set; }
```

Access whole columns in the form of [`Vector3`](Vector3.md).

参数：
- `column`：Which column vector.

### Item

```csharp
public float this[int column, int row] { get; set; }
```

Access matrix elements in column-major order.

参数：
- `column`：Which column, the matrix horizontal position.
- `row`：Which row, the matrix vertical position.

### Scale

```csharp
public Vector3 Scale { get; }
```

Assuming that the matrix is the combination of a rotation and scaling,
return the absolute value of scaling factors along each axis.

### X

```csharp
public Vector3 X { get; set; }
```

引擎名：`x`

The basis matrix's X vector (column 0).

### Y

```csharp
public Vector3 Y { get; set; }
```

引擎名：`y`

The basis matrix's Y vector (column 1).

### Z

```csharp
public Vector3 Z { get; set; }
```

引擎名：`z`

The basis matrix's Z vector (column 2).

## 方法

### Determinant

```csharp
public float Determinant()
```

引擎名：`determinant`

Returns the determinant of the basis matrix. If the basis is
uniformly scaled, its determinant is the square of the scale.

A negative determinant means the basis has a negative scale.
A zero determinant means the basis isn't invertible,
and is usually considered invalid.

返回：The determinant of the basis matrix.

### Equals

```csharp
public bool Equals(Basis other)
```

Returns `true` if the basis matrices are exactly
equal. Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `other`：The other basis.

返回：Whether or not the basis matrices are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the `Basis` is
exactly equal to the given object (`obj`).
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `obj`：The object to compare with.

返回：Whether or not the basis matrix and the object are exactly equal.

### FromEuler

```csharp
public static Basis FromEuler(Vector3 euler, EulerOrder order = EulerOrder.Yxz)
```

引擎名：`from_euler`

Constructs a Basis matrix from Euler angles in the specified rotation order. By default, use YXZ order (most common).

参数：
- `euler`：The Euler angles to use.
- `order`：The order to compose the Euler angles.

### FromScale

```csharp
public static Basis FromScale(Vector3 scale)
```

引擎名：`from_scale`

Constructs a pure scale basis matrix with no rotation or shearing.
The scale values are set as the main diagonal of the matrix,
and all of the other parts of the matrix are zero.

参数：
- `scale`：The scale Vector3.

返回：A pure scale Basis matrix.

### GetEuler

```csharp
public Vector3 GetEuler(EulerOrder order = EulerOrder.Yxz)
```

引擎名：`get_euler`

Returns the basis's rotation in the form of Euler angles.
The Euler order depends on the `order` parameter,
by default it uses the YXZ convention: when decomposing,
first Z, then X, and Y last. The returned vector contains
the rotation angles in the format (X angle, Y angle, Z angle).

Consider using the `GetRotationQuaternion()` method instead, which
returns a [`Quaternion`](Quaternion.md) quaternion instead of Euler angles.

参数：
- `order`：The Euler order to use. By default, use YXZ order (most common).

返回：A [`Vector3`](Vector3.md) representing the basis rotation in Euler angles.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Basis`.

返回：A hash code for this basis.

### GetRotationQuaternion

```csharp
public Quaternion GetRotationQuaternion()
```

引擎名：`get_rotation_quaternion`

Returns the `Basis`'s rotation in the form of a
[`Quaternion`](Quaternion.md). See `GetEuler()` if you
need Euler angles, but keep in mind quaternions should generally
be preferred to Euler angles.

返回：The basis rotation.

### Inverse

```csharp
public Basis Inverse()
```

引擎名：`inverse`

Returns the inverse of the matrix.

返回：The inverse matrix.

### IsEqualApprox

```csharp
public bool IsEqualApprox(Basis other)
```

引擎名：`is_equal_approx`

Returns `true` if this basis and `other` are approximately equal,
by running [`Vector3.IsEqualApprox`](Vector3.md) on each component.

参数：
- `other`：The other basis to compare.

返回：Whether or not the bases are approximately equal.

### IsFinite

```csharp
public bool IsFinite()
```

引擎名：`is_finite`

Returns `true` if this basis is finite, by calling
[`Mathf.IsFinite`](Mathf.md) on each component.

返回：Whether this vector is finite or not.

### LookingAt

```csharp
public static Basis LookingAt(Vector3 target, Vector3 up)
```

引擎名：`looking_at`

Creates a `Basis` with a rotation such that the forward
axis (-Z) points towards the `target` position.
The up axis (+Y) points as close to the `up` vector
as possible while staying perpendicular to the forward axis.
The resulting Basis is orthonormalized.
The `target` and `up` vectors
cannot be zero, and cannot be parallel to each other.

参数：
- `target`：The position to look at.
- `up`：The relative up direction.
- `useModelFront`：If true, then the model is oriented in reverse,
towards the model front axis (+Z, Vector3.ModelFront),
which is more useful for orienting 3D models.

返回：The resulting basis matrix.

### LookingAt

```csharp
public static Basis LookingAt(Vector3 target, Vector3? up = null, bool useModelFront = false)
```

引擎名：`looking_at`

Creates a `Basis` with a rotation such that the forward
axis (-Z) points towards the `target` position.
The up axis (+Y) points as close to the `up` vector
as possible while staying perpendicular to the forward axis.
The resulting Basis is orthonormalized.
The `target` and `up` vectors
cannot be zero, and cannot be parallel to each other.

参数：
- `target`：The position to look at.
- `up`：The relative up direction.
- `useModelFront`：If true, then the model is oriented in reverse,
towards the model front axis (+Z, Vector3.ModelFront),
which is more useful for orienting 3D models.

返回：The resulting basis matrix.

### Orthonormalized

```csharp
public Basis Orthonormalized()
```

引擎名：`orthonormalized`

Returns the orthonormalized version of the basis matrix (useful to
call occasionally to avoid rounding errors for orthogonal matrices).
This performs a Gram-Schmidt orthonormalization on the basis of the matrix.

返回：An orthonormalized basis matrix.

### Rotated

```csharp
public Basis Rotated(Vector3 axis, float angle)
```

引擎名：`rotated`

Introduce an additional rotation around the given `axis`
by `angle` (in radians). The axis must be a normalized vector.

参数：
- `axis`：The axis to rotate around. Must be normalized.
- `angle`：The angle to rotate, in radians.

返回：The rotated basis matrix.

### Scaled

```csharp
public Basis Scaled(Vector3 scale)
```

引擎名：`scaled`

Introduce an additional scaling specified by the given 3D scaling factor.

参数：
- `scale`：The scale to introduce.

返回：The scaled basis matrix.

### ScaledLocal

```csharp
public Basis ScaledLocal(Vector3 scale)
```

引擎名：`scaled_local`

Returns this basis with each axis scaled by the corresponding component in the given `scale`. The basis matrix's columns are multiplied by `scale`'s components. This operation is a local scale (relative to self).

参数：
- `scale`：The scale to introduce.

返回：The scaled basis matrix.

### Slerp

```csharp
public Basis Slerp(Basis target, float weight)
```

引擎名：`slerp`

Assuming that the matrix is a proper rotation matrix, slerp performs
a spherical-linear interpolation with another rotation matrix.

参数：
- `target`：The destination basis for interpolation.
- `weight`：A value on the range of 0.0 to 1.0, representing the amount of interpolation.

返回：The resulting basis matrix of the interpolation.

### Tdotx

```csharp
public float Tdotx(Vector3 with)
```

引擎名：`tdotx`

Transposed dot product with the X axis of the matrix.

参数：
- `with`：A vector to calculate the dot product with.

返回：The resulting dot product.

### Tdoty

```csharp
public float Tdoty(Vector3 with)
```

引擎名：`tdoty`

Transposed dot product with the Y axis of the matrix.

参数：
- `with`：A vector to calculate the dot product with.

返回：The resulting dot product.

### Tdotz

```csharp
public float Tdotz(Vector3 with)
```

引擎名：`tdotz`

Transposed dot product with the Z axis of the matrix.

参数：
- `with`：A vector to calculate the dot product with.

返回：The resulting dot product.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Basis` to a string with the given `format`.

返回：A string representation of this basis.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Basis` to a string.

返回：A string representation of this basis.

### Transposed

```csharp
public Basis Transposed()
```

引擎名：`transposed`

Returns the transposed version of the basis matrix.

返回：The transposed basis matrix.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Basis left, Basis right)
```

Returns `true` if the basis matrices are exactly
equal. Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left basis.
- `right`：The right basis.

返回：Whether or not the basis matrices are exactly equal.

### op_Inequality

```csharp
public static bool op_Inequality(Basis left, Basis right)
```

Returns `true` if the basis matrices are not equal.
Note: Due to floating-point precision errors, consider using
`IsEqualApprox()` instead, which is more reliable.

参数：
- `left`：The left basis.
- `right`：The right basis.

返回：Whether or not the basis matrices are not equal.

### op_Multiply

```csharp
public static Basis op_Multiply(Basis left, Basis right)
```

Composes these two basis matrices by multiplying them
together. This has the effect of transforming the second basis
(the child) by the first basis (the parent).

参数：
- `left`：The parent basis.
- `right`：The child basis.

返回：The composed basis.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Basis basis, Vector3 vector)
```

Returns a Vector3 transformed (multiplied) by the basis matrix.

参数：
- `basis`：The basis matrix transformation to apply.
- `vector`：A Vector3 to transform.

返回：The transformed Vector3.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Vector3 vector, Basis basis)
```

Returns a Vector3 transformed (multiplied) by the inverse basis matrix,
under the assumption that the transformation basis is orthonormal (i.e. rotation/reflection
is fine, scaling/skew is not).
`vector * basis` is equivalent to `basis.Transposed() * vector`. See `Transposed()`.
For transforming by inverse of a non-orthonormal basis (e.g. with scaling) `basis.Inverse() * vector` can be used instead. See `Inverse()`.

参数：
- `vector`：A Vector3 to inversely transform.
- `basis`：The basis matrix transformation to apply.

返回：The inversely transformed vector.

## 字段

### Row0

```csharp
public Vector3 Row0
```

Row 0 of the basis matrix. Shows which vectors contribute
to the X direction. Rows are not very useful for user code,
but are more efficient for some internal calculations.

### Row1

```csharp
public Vector3 Row1
```

Row 1 of the basis matrix. Shows which vectors contribute
to the Y direction. Rows are not very useful for user code,
but are more efficient for some internal calculations.

### Row2

```csharp
public Vector3 Row2
```

Row 2 of the basis matrix. Shows which vectors contribute
to the Z direction. Rows are not very useful for user code,
but are more efficient for some internal calculations.
