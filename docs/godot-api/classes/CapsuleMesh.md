<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# CapsuleMesh

命名空间 `Godot`；class；继承 [`PrimitiveMesh`](PrimitiveMesh.md) → [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Class representing a capsule-shaped [`PrimitiveMesh`](PrimitiveMesh.md).

## 构造函数

### CapsuleMesh

```csharp
public CapsuleMesh()
```

## 属性

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

Total height of the capsule mesh (including the hemispherical ends).

**Note:** The `Height` of a capsule must be at least twice its `Radius`. Otherwise, the capsule becomes a circle. If the `Height` is less than twice the `Radius`, the properties adjust to a valid value.

### RadialSegments

```csharp
public int RadialSegments { get; set; }
```

引擎名：`radial_segments`

Number of radial segments on the capsule mesh.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

Radius of the capsule mesh.

**Note:** The `Radius` of a capsule cannot be greater than half of its `Height`. Otherwise, the capsule becomes a circle. If the `Radius` is greater than half of the `Height`, the properties adjust to a valid value.

### Rings

```csharp
public int Rings { get; set; }
```

引擎名：`rings`

Number of rings along the height of the capsule.

## 方法

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

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

### SetRadialSegments

```csharp
public void SetRadialSegments(int segments)
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
