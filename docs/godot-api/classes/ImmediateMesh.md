<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImmediateMesh

命名空间 `Godot`；class；继承 [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A mesh type optimized for creating geometry manually, similar to OpenGL 1.x immediate mode.

Here's a sample on how to generate a triangular face:

```csharp
var mesh = new ImmediateMesh();
mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
mesh.SurfaceAddVertex(Vector3.Left);
mesh.SurfaceAddVertex(Vector3.Forward);
mesh.SurfaceAddVertex(Vector3.Zero);
mesh.SurfaceEnd();

```

**Note:** Generating complex geometries with `ImmediateMesh` is highly inefficient. Instead, it is designed to generate simple geometry that changes often.

## 构造函数

### ImmediateMesh

```csharp
public ImmediateMesh()
```

## 方法

### ClearSurfaces

```csharp
public void ClearSurfaces()
```

引擎名：`clear_surfaces`

Clear all surfaces.

### SurfaceAddVertex

```csharp
public void SurfaceAddVertex(Vector3 vertex)
```

引擎名：`surface_add_vertex`

Add a 3D vertex using the current attributes previously set.

### SurfaceAddVertex2D

```csharp
public void SurfaceAddVertex2D(Vector2 vertex)
```

Add a 2D vertex using the current attributes previously set.

### SurfaceBegin

```csharp
public void SurfaceBegin(Mesh.PrimitiveType primitive, Material material = null)
```

引擎名：`surface_begin`

Begin a new surface.

### SurfaceEnd

```csharp
public void SurfaceEnd()
```

引擎名：`surface_end`

End and commit current surface. Note that surface being created will not be visible until this function is called.

### SurfaceSetColor

```csharp
public void SurfaceSetColor(Color color)
```

引擎名：`surface_set_color`

Set the color attribute that will be pushed with the next vertex.

### SurfaceSetNormal

```csharp
public void SurfaceSetNormal(Vector3 normal)
```

引擎名：`surface_set_normal`

Set the normal attribute that will be pushed with the next vertex.

### SurfaceSetTangent

```csharp
public void SurfaceSetTangent(Plane tangent)
```

引擎名：`surface_set_tangent`

Set the tangent attribute that will be pushed with the next vertex.

**Note:** Even though `tangent` is a [`Plane`](Plane.md), it does not directly represent the tangent plane. Its `Plane.X`, `Plane.Y`, and `Plane.Z` represent the tangent vector and `Plane.D` should be either `-1` or `1`. See also [`Mesh.ArrayType.Tangent`](Mesh.md).

### SurfaceSetUV

```csharp
public void SurfaceSetUV(Vector2 uV)
```

引擎名：`surface_set_uv`

Set the UV attribute that will be pushed with the next vertex.

### SurfaceSetUV2

```csharp
public void SurfaceSetUV2(Vector2 uV2)
```

引擎名：`surface_set_uv2`

Set the UV2 attribute that will be pushed with the next vertex.
