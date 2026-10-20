<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SphereMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class representing a spherical [`PrimitiveMesh`](PrimitiveMesh.md).

## 构造函数

### SphereMesh

```csharp
public SphereMesh()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

Full height of the sphere.

### IsHemisphere

```csharp
public bool IsHemisphere { get; set; }
```

引擎名：`is_hemisphere`

If `true`, a hemisphere is created rather than a full sphere.

**Note:** To get a regular hemisphere, the height and radius of the sphere must be equal.

### RadialSegments

```csharp
public int RadialSegments { get; set; }
```

引擎名：`radial_segments`

Number of radial segments on the sphere.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

Radius of sphere.

### Rings

```csharp
public int Rings { get; set; }
```

引擎名：`rings`

Number of segments along the height of the sphere.

## 方法

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

### GetIsHemisphere

```csharp
public bool GetIsHemisphere()
```

引擎名：`get_is_hemisphere`

### GetRadialSegments

```csharp
public int GetRadialSegments()
```

引擎名：`get_radial_segments`

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetRings

```csharp
public int GetRings()
```

引擎名：`get_rings`

### SetHeight

```csharp
public void SetHeight(float height)
```

引擎名：`set_height`

### SetIsHemisphere

```csharp
public void SetIsHemisphere(bool isHemisphere)
```

引擎名：`set_is_hemisphere`

### SetRadialSegments

```csharp
public void SetRadialSegments(int radialSegments)
```

引擎名：`set_radial_segments`

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`

### SetRings

```csharp
public void SetRings(int rings)
```

引擎名：`set_rings`
