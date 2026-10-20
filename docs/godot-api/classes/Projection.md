<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Projection

命名空间 `Godot`；struct

A 4x4 matrix used for 3D projective transformations. It can represent transformations such as
translation, rotation, scaling, shearing, and perspective division. It consists of four
[`Vector4`](Vector4.md) columns.
For purely linear transformations (translation, rotation, and scale), it is recommended to use
[`Transform3D`](Transform3D.md), as it is more performant and has a lower memory footprint.
Used internally as [`Camera3D`](Camera3D.md)'s projection matrix.

## 构造函数

### Projection

```csharp
public Projection(Transform3D transform)
```

Constructs a new `Projection` from a [`Transform3D`](Transform3D.md).

参数：
- `transform`：The [`Transform3D`](Transform3D.md).

### Projection

```csharp
public Projection(Vector4 x, Vector4 y, Vector4 z, Vector4 w)
```

Constructs a projection from 4 vectors (matrix columns).

参数：
- `x`：The X column, or column index 0.
- `y`：The Y column, or column index 1.
- `z`：The Z column, or column index 2.
- `w`：The W column, or column index 3.

### Projection

```csharp
public Projection(float xx, float xy, float xz, float xw, float yx, float yy, float yz, float yw, float zx, float zy, float zz, float zw, float wx, float wy, float wz, float ww)
```

Constructs a projection from 16 scalars.

参数：
- `xx`：The X column vector's X component, accessed via `p.X.X` or `[0][0]`.
- `xy`：The X column vector's Y component, accessed via `p.X.Y` or `[0][1]`.
- `xz`：The X column vector's Z component, accessed via `p.X.Z` or `[0][2]`.
- `xw`：The X column vector's W component, accessed via `p.X.W` or `[0][3]`.
- `yx`：The Y column vector's X component, accessed via `p.Y.X` or `[1][0]`.
- `yy`：The Y column vector's Y component, accessed via `p.Y.Y` or `[1][1]`.
- `yz`：The Y column vector's Z component, accessed via `p.Y.Z` or `[1][2]`.
- `yw`：The Y column vector's W component, accessed via `p.Y.W` or `[1][3]`.
- `zx`：The Z column vector's X component, accessed via `p.Z.X` or `[2][0]`.
- `zy`：The Z column vector's Y component, accessed via `p.Z.Y` or `[2][1]`.
- `zz`：The Z column vector's Z component, accessed via `p.Z.Z` or `[2][2]`.
- `zw`：The Z column vector's W component, accessed via `p.Z.W` or `[2][3]`.
- `wx`：The W column vector's X component, accessed via `p.W.X` or `[3][0]`.
- `wy`：The W column vector's Y component, accessed via `p.W.Y` or `[3][1]`.
- `wz`：The W column vector's Z component, accessed via `p.W.Z` or `[3][2]`.
- `ww`：The W column vector's W component, accessed via `p.W.W` or `[3][3]`.

## 属性

### Identity

```csharp
public static Projection Identity { get; }
```

The identity projection, with no distortion applied.
This is used as a replacement for `Projection()` in GDScript.
Do not use `new Projection()` with no arguments in C#, because it sets all values to zero.

### Item

```csharp
public Vector4 this[int column] { get; set; }
```

Access whole columns in the form of [`Vector4`](Vector4.md).

参数：
- `column`：Which column vector.

### Item

```csharp
public float this[int column, int row] { get; set; }
```

Access single values.

参数：
- `column`：Which column vector.
- `row`：Which row of the column.

### Zero

```csharp
public static Projection Zero { get; }
```

Zero projection, a projection with all components set to `0`.

## 方法

### CreateDepthCorrection

```csharp
public static Projection CreateDepthCorrection(bool flipY)
```

引擎名：`create_depth_correction`

Creates a new `Projection` that projects positions from a depth range of
`-1` to `1` to one that ranges from `0` to `1`, and flips the projected
positions vertically, according to `flipY`.

参数：
- `flipY`：If the projection should be flipped vertically.

返回：The created projection.

### CreateFitAabb

```csharp
public static Projection CreateFitAabb(Aabb aabb)
```

引擎名：`create_fit_aabb`

Creates a new `Projection` that scales a given projection to fit around
a given [`Aabb`](Aabb.md) in projection space.

参数：
- `aabb`：The Aabb to fit the projection around.

返回：The created projection.

### CreateForHmd

```csharp
public static Projection CreateForHmd(int eye, float aspect, float intraocularDist, float displayWidth, float displayToLens, float oversample, float zNear, float zFar)
```

引擎名：`create_for_hmd`

Creates a new `Projection` for projecting positions onto a head-mounted display with
the given X:Y aspect ratio, distance between eyes, display width, distance to lens, oversampling factor,
and depth clipping planes.
`eye` creates the projection for the left eye when set to 1,
or the right eye when set to 2.

参数：
- `eye`：The eye to create the projection for.
The left eye when set to 1, the right eye when set to 2.
- `aspect`：The aspect ratio.
- `intraocularDist`：The distance between the eyes.
- `displayWidth`：The display width.
- `displayToLens`：The distance to the lens.
- `oversample`：The oversampling factor.
- `zNear`：The near clipping distance.
- `zFar`：The far clipping distance.

返回：The created projection.

### CreateFrustum

```csharp
public static Projection CreateFrustum(float left, float right, float bottom, float top, float depthNear, float depthFar)
```

引擎名：`create_frustum`

Creates a new `Projection` that projects positions in a frustum with
the given clipping planes.

参数：
- `left`：The left clipping distance.
- `right`：The right clipping distance.
- `bottom`：The bottom clipping distance.
- `top`：The top clipping distance.
- `depthNear`：The near clipping distance.
- `depthFar`：The far clipping distance.

返回：The created projection.

### CreateFrustumAspect

```csharp
public static Projection CreateFrustumAspect(float size, float aspect, Vector2 offset, float depthNear, float depthFar, bool flipFov)
```

引擎名：`create_frustum_aspect`

Creates a new `Projection` that projects positions in a frustum with
the given size, X:Y aspect ratio, offset, and clipping planes.
`flipFov` determines whether the projection's field of view is flipped over its diagonal.

参数：
- `size`：The frustum size.
- `aspect`：The aspect ratio.
- `offset`：The offset to apply.
- `depthNear`：The near clipping distance.
- `depthFar`：The far clipping distance.
- `flipFov`：If the field of view is flipped over the projection's diagonal.

返回：The created projection.

### CreateLightAtlasRect

```csharp
public static Projection CreateLightAtlasRect(Rect2 rect)
```

引擎名：`create_light_atlas_rect`

Creates a new `Projection` that projects positions into the given [`Rect2`](Rect2.md).

参数：
- `rect`：The Rect2 to project positions into.

返回：The created projection.

### CreateOrthogonal

```csharp
public static Projection CreateOrthogonal(float left, float right, float bottom, float top, float zNear, float zFar)
```

引擎名：`create_orthogonal`

Creates a new `Projection` that projects positions using an orthogonal projection with
the given clipping planes.

参数：
- `left`：The left clipping distance.
- `right`：The right clipping distance.
- `bottom`：The bottom clipping distance.
- `top`：The top clipping distance.
- `zNear`：The near clipping distance.
- `zFar`：The far clipping distance.

返回：The created projection.

### CreateOrthogonalAspect

```csharp
public static Projection CreateOrthogonalAspect(float size, float aspect, float zNear, float zFar, bool flipFov)
```

引擎名：`create_orthogonal_aspect`

Creates a new `Projection` that projects positions using an orthogonal projection with
the given size, X:Y aspect ratio, and clipping planes.
`flipFov` determines whether the projection's field of view is flipped over its diagonal.

参数：
- `size`：The frustum size.
- `aspect`：The aspect ratio.
- `zNear`：The near clipping distance.
- `zFar`：The far clipping distance.
- `flipFov`：If the field of view is flipped over the projection's diagonal.

返回：The created projection.

### CreatePerspective

```csharp
public static Projection CreatePerspective(float fovyDegrees, float aspect, float zNear, float zFar, bool flipFov)
```

引擎名：`create_perspective`

Creates a new `Projection` that projects positions using a perspective projection with
the given Y-axis field of view (in degrees), X:Y aspect ratio, and clipping planes.
`flipFov` determines whether the projection's field of view is flipped over its diagonal.

参数：
- `fovyDegrees`：The vertical field of view (in degrees).
- `aspect`：The aspect ratio.
- `zNear`：The near clipping distance.
- `zFar`：The far clipping distance.
- `flipFov`：If the field of view is flipped over the projection's diagonal.

返回：The created projection.

### CreatePerspectiveHmd

```csharp
public static Projection CreatePerspectiveHmd(float fovyDegrees, float aspect, float zNear, float zFar, bool flipFov, int eye, float intraocularDist, float convergenceDist)
```

引擎名：`create_perspective_hmd`

Creates a new `Projection` that projects positions using a perspective projection with
the given Y-axis field of view (in degrees), X:Y aspect ratio, and clipping distances.
The projection is adjusted for a head-mounted display with the given distance between eyes and distance
to a point that can be focused on.
`eye` creates the projection for the left eye when set to 1,
or the right eye when set to 2.
`flipFov` determines whether the projection's field of view is flipped over its diagonal.

参数：
- `fovyDegrees`：The vertical field of view (in degrees).
- `aspect`：The aspect ratio.
- `zNear`：The near clipping distance.
- `zFar`：The far clipping distance.
- `flipFov`：If the field of view is flipped over the projection's diagonal.
- `eye`：The eye to create the projection for.
The left eye when set to 1, the right eye when set to 2.
- `intraocularDist`：The distance between the eyes.
- `convergenceDist`：The distance to a point of convergence that can be focused on.

返回：The created projection.

### Determinant

```csharp
public float Determinant()
```

引擎名：`determinant`

Returns a scalar value that is the signed factor by which areas are scaled by this matrix.
If the sign is negative, the matrix flips the orientation of the area.
The determinant can be used to calculate the invertibility of a matrix or solve linear systems
of equations involving the matrix, among other applications.

返回：The determinant calculated from this projection.

### Equals

```csharp
public bool Equals(Projection other)
```

Returns `true` if the projections are exactly equal.

参数：
- `other`：The other projection.

返回：Whether or not the projections are exactly equal.

### Equals

```csharp
public virtual bool Equals(object obj)
```

Returns `true` if the projection is exactly equal
to the given object (`obj`).

参数：
- `obj`：The object to compare with.

返回：Whether or not the vector and the object are equal.

### FlippedY

```csharp
public Projection FlippedY()
```

引擎名：`flipped_y`

Returns a copy of this `Projection` with the signs of the values of the Y column flipped.

返回：The flipped projection.

### GetAspect

```csharp
public float GetAspect()
```

引擎名：`get_aspect`

Returns the X:Y aspect ratio of this `Projection`'s viewport.

返回：The aspect ratio from this projection's viewport.

### GetFarPlaneHalfExtents

```csharp
public Vector2 GetFarPlaneHalfExtents()
```

引擎名：`get_far_plane_half_extents`

Returns the dimensions of the far clipping plane of the projection, divided by two.

返回：The half extents for this projection's far plane.

### GetFov

```csharp
public float GetFov()
```

引擎名：`get_fov`

Returns the horizontal field of view of the projection (in degrees).

返回：The horizontal field of view of this projection.

### GetFovy

```csharp
public static float GetFovy(float fovx, float aspect)
```

引擎名：`get_fovy`

Returns the vertical field of view of the projection (in degrees) associated with
the given horizontal field of view (in degrees) and aspect ratio.

参数：
- `fovx`：The horizontal field of view (in degrees).
- `aspect`：The aspect ratio.

返回：The vertical field of view of this projection.

### GetHashCode

```csharp
public virtual int GetHashCode()
```

Serves as the hash function for `Projection`.

返回：A hash code for this projection.

### GetLodMultiplier

```csharp
public float GetLodMultiplier()
```

引擎名：`get_lod_multiplier`

Returns the factor by which the visible level of detail is scaled by this `Projection`.

返回：The level of detail factor for this projection.

### GetPixelsPerMeter

```csharp
public int GetPixelsPerMeter(int forPixelWidth)
```

引擎名：`get_pixels_per_meter`

Returns the number of pixels with the given pixel width displayed per meter, after
this `Projection` is applied.

参数：
- `forPixelWidth`：The width for each pixel (in meters).

返回：The number of pixels per meter.

### GetProjectionPlane

```csharp
public Plane GetProjectionPlane(Projection.Planes plane)
```

引擎名：`get_projection_plane`

Returns the clipping plane of this `Projection` whose index is given
by `plane`.
`plane` should be equal to one of [`Projection.Planes.Near`](Projection.md),
[`Projection.Planes.Far`](Projection.md), [`Projection.Planes.Left`](Projection.md), [`Projection.Planes.Top`](Projection.md),
[`Projection.Planes.Right`](Projection.md), or [`Projection.Planes.Bottom`](Projection.md).

参数：
- `plane`：The kind of clipping plane to get from the projection.

返回：The clipping plane of this projection.

### GetViewportHalfExtents

```csharp
public Vector2 GetViewportHalfExtents()
```

引擎名：`get_viewport_half_extents`

Returns the dimensions of the viewport plane that this `Projection`
projects positions onto, divided by two.

返回：The half extents for this projection's viewport plane.

### GetZFar

```csharp
public float GetZFar()
```

引擎名：`get_z_far`

Returns the distance for this `Projection` beyond which positions are clipped.

返回：The distance beyond which positions are clipped.

### GetZNear

```csharp
public float GetZNear()
```

引擎名：`get_z_near`

Returns the distance for this `Projection` before which positions are clipped.

返回：The distance before which positions are clipped.

### Inverse

```csharp
public Projection Inverse()
```

引擎名：`inverse`

Returns a `Projection` that performs the inverse of this `Projection`'s
projective transformation.

返回：The inverted projection.

### IsOrthogonal

```csharp
public bool IsOrthogonal()
```

引擎名：`is_orthogonal`

Returns `true` if this `Projection` performs an orthogonal projection.

返回：If the projection performs an orthogonal projection.

### JitterOffseted

```csharp
public Projection JitterOffseted(Vector2 offset)
```

引擎名：`jitter_offseted`

Returns a `Projection` with the X and Y values from the given [`Vector2`](Vector2.md)
added to the first and second values of the final column respectively.

参数：
- `offset`：The offset to apply to the projection.

返回：The offsetted projection.

### PerspectiveZNearAdjusted

```csharp
public Projection PerspectiveZNearAdjusted(float newZNear)
```

Returns a `Projection` with the near clipping distance adjusted to be
`newZNear`.
Note: The original `Projection` must be a perspective projection.

参数：
- `newZNear`：The near clipping distance to adjust the projection to.

返回：The adjusted projection.

### ToString

```csharp
public string ToString(string format)
```

Converts this `Projection` to a string with the given `format`.

返回：A string representation of this projection.

### ToString

```csharp
public virtual string ToString()
```

Converts this `Projection` to a string.

返回：A string representation of this projection.

## 运算符

### op_Equality

```csharp
public static bool op_Equality(Projection left, Projection right)
```

Returns `true` if the projections are exactly equal.

参数：
- `left`：The left projection.
- `right`：The right projection.

返回：Whether or not the projections are exactly equal.

### op_Explicit

```csharp
public static Transform3D op_Explicit(Projection proj)
```

Constructs a new [`Transform3D`](Transform3D.md) from the `Projection`.

参数：
- `proj`：The `Projection`.

### op_Inequality

```csharp
public static bool op_Inequality(Projection left, Projection right)
```

Returns `true` if the projections are not exactly equal.

参数：
- `left`：The left projection.
- `right`：The right projection.

返回：Whether or not the projections are not exactly equal.

### op_Multiply

```csharp
public static Projection op_Multiply(Projection left, Projection right)
```

Composes these two projections by multiplying them
together. This has the effect of applying the right
and then the left projection.

参数：
- `left`：The parent transform.
- `right`：The child transform.

返回：The composed projection.

### op_Multiply

```csharp
public static Vector3 op_Multiply(Projection proj, Vector3 vector)
```

Returns a Vector3 transformed (multiplied) by the projection.

参数：
- `proj`：The projection to apply.
- `vector`：A Vector3 to transform.

返回：The transformed Vector3.

### op_Multiply

```csharp
public static Vector4 op_Multiply(Projection proj, Vector4 vector)
```

Returns a Vector4 transformed (multiplied) by the projection.

参数：
- `proj`：The projection to apply.
- `vector`：A Vector4 to transform.

返回：The transformed Vector4.

### op_Multiply

```csharp
public static Vector4 op_Multiply(Vector4 vector, Projection proj)
```

Returns a Vector4 transformed (multiplied) by the transpose of the projection.
For transforming by inverse of a projection `projection.Inverse() * vector` can be used instead. See `Inverse()`.

参数：
- `proj`：The projection to apply.
- `vector`：A Vector4 to transform.

返回：The inversely transformed Vector4.

## 字段

### W

```csharp
public Vector4 W
```

The projection's W column. Also accessible by using the index position `[3]`.

### X

```csharp
public Vector4 X
```

The projection's X column. Also accessible by using the index position `[0]`.

### Y

```csharp
public Vector4 Y
```

The projection's Y column. Also accessible by using the index position `[1]`.

### Z

```csharp
public Vector4 Z
```

The projection's Z column. Also accessible by using the index position `[2]`.

## Planes

命名空间 `Godot`；enum

Enumerated index values for the planes.

### 枚举值

- `Near = 0`：The projection's near plane.
- `Far = 1`：The projection's far plane.
- `Left = 2`：The projection's left plane.
- `Top = 3`：The projection's top plane.
- `Right = 4`：The projection's right plane.
- `Bottom = 5`：The projection's bottom plane.
