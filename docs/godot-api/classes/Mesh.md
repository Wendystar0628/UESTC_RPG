<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Mesh

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Mesh is a type of [`Resource`](Resource.md) that contains vertex array-based geometry, divided in *surfaces*. Each surface contains a completely separate array and a material used to draw it. Design wise, a mesh with multiple surfaces is preferred to a single surface, because objects created in 3D editing software commonly contain multiple materials. The maximum number of surfaces per mesh is [`RenderingServer.MaxMeshSurfaces`](RenderingServer.md).

## 构造函数

### Mesh

```csharp
public Mesh()
```

## 属性

### LightmapSizeHint

```csharp
public Vector2I LightmapSizeHint { get; set; }
```

引擎名：`lightmap_size_hint`

Sets a hint to be used for lightmap resolution.

## 方法

### CreateConvexShape

```csharp
public ConvexPolygonShape3D CreateConvexShape(bool clean = true, bool simplify = false)
```

引擎名：`create_convex_shape`

Calculate a [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) from the mesh.

If `clean` is `true` (default), duplicate and interior vertices are removed automatically. You can set it to `false` to make the process faster if not needed.

If `simplify` is `true`, the geometry can be further simplified to reduce the number of vertices. Disabled by default.

### CreateOutline

```csharp
public Mesh CreateOutline(float margin)
```

引擎名：`create_outline`

Calculate an outline mesh at a defined offset (margin) from the original mesh.

**Note:** This method typically returns the vertices in reverse order (e.g. clockwise to counterclockwise).

### CreatePlaceholder

```csharp
public Resource CreatePlaceholder()
```

引擎名：`create_placeholder`

Creates a placeholder version of this resource ([`PlaceholderMesh`](PlaceholderMesh.md)).

### CreateTrimeshShape

```csharp
public ConcavePolygonShape3D CreateTrimeshShape()
```

引擎名：`create_trimesh_shape`

Calculate a [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) from the mesh.

### GenerateTriangleMesh

```csharp
public TriangleMesh GenerateTriangleMesh()
```

引擎名：`generate_triangle_mesh`

Generate a [`TriangleMesh`](TriangleMesh.md) from the mesh. Considers only surfaces using one of these primitive types: [`Mesh.PrimitiveType.Triangles`](Mesh.md), [`Mesh.PrimitiveType.TriangleStrip`](Mesh.md).

### GetAabb

```csharp
public Aabb GetAabb()
```

引擎名：`get_aabb`

Returns the smallest [`Aabb`](Aabb.md) enclosing this mesh in local space. Not affected by `custom_aabb`.

**Note:** This is only implemented for [`ArrayMesh`](ArrayMesh.md) and [`PrimitiveMesh`](PrimitiveMesh.md).

### GetFaces

```csharp
public Vector3[] GetFaces()
```

引擎名：`get_faces`

Returns all the vertices that make up the faces of the mesh. Each three vertices represent one triangle.

### GetLightmapSizeHint

```csharp
public Vector2I GetLightmapSizeHint()
```

引擎名：`get_lightmap_size_hint`

### GetSurfaceCount

```csharp
public int GetSurfaceCount()
```

引擎名：`get_surface_count`

Returns the number of surfaces that the `Mesh` holds. This is equivalent to [`MeshInstance3D.GetSurfaceOverrideMaterialCount`](MeshInstance3D.md).

### SetLightmapSizeHint

```csharp
public void SetLightmapSizeHint(Vector2I size)
```

引擎名：`set_lightmap_size_hint`

### SurfaceGetArrays

```csharp
public Array SurfaceGetArrays(int surfIdx)
```

引擎名：`surface_get_arrays`

Returns the arrays for the vertices, normals, UVs, etc. that make up the requested surface (see [`ArrayMesh.AddSurfaceFromArrays`](ArrayMesh.md)).

### SurfaceGetBlendShapeArrays

```csharp
public Array<Array> SurfaceGetBlendShapeArrays(int surfIdx)
```

引擎名：`surface_get_blend_shape_arrays`

Returns the blend shape arrays for the requested surface.

### SurfaceGetMaterial

```csharp
public Material SurfaceGetMaterial(int surfIdx)
```

引擎名：`surface_get_material`

Returns a [`Material`](Material.md) in a given surface. Surface is rendered using this material.

**Note:** This returns the material within the `Mesh` resource, not the [`Material`](Material.md) associated to the [`MeshInstance3D`](MeshInstance3D.md)'s Surface Material Override properties. To get the [`Material`](Material.md) associated to the [`MeshInstance3D`](MeshInstance3D.md)'s Surface Material Override properties, use [`MeshInstance3D.GetSurfaceOverrideMaterial`](MeshInstance3D.md) instead.

### SurfaceSetMaterial

```csharp
public void SurfaceSetMaterial(int surfIdx, Material material)
```

引擎名：`surface_set_material`

Sets a [`Material`](Material.md) for a given surface. Surface will be rendered using this material.

**Note:** This assigns the material within the `Mesh` resource, not the [`Material`](Material.md) associated to the [`MeshInstance3D`](MeshInstance3D.md)'s Surface Material Override properties. To set the [`Material`](Material.md) associated to the [`MeshInstance3D`](MeshInstance3D.md)'s Surface Material Override properties, use [`MeshInstance3D.SetSurfaceOverrideMaterial`](MeshInstance3D.md) instead.

### _GetAabb

```csharp
public virtual Aabb _GetAabb()
```

引擎名：`_get_aabb`

Virtual method to override the [`Aabb`](Aabb.md) for a custom class extending `Mesh`.

### _GetBlendShapeCount

```csharp
public virtual int _GetBlendShapeCount()
```

引擎名：`_get_blend_shape_count`

Virtual method to override the number of blend shapes for a custom class extending `Mesh`.

### _GetBlendShapeName

```csharp
public virtual StringName _GetBlendShapeName(int index)
```

引擎名：`_get_blend_shape_name`

Virtual method to override the retrieval of blend shape names for a custom class extending `Mesh`.

### _GetSurfaceCount

```csharp
public virtual int _GetSurfaceCount()
```

引擎名：`_get_surface_count`

Virtual method to override the surface count for a custom class extending `Mesh`.

### _SetBlendShapeName

```csharp
public virtual void _SetBlendShapeName(int index, StringName name)
```

引擎名：`_set_blend_shape_name`

Virtual method to override the names of blend shapes for a custom class extending `Mesh`.

### _SurfaceGetArrayIndexLen

```csharp
public virtual int _SurfaceGetArrayIndexLen(int index)
```

引擎名：`_surface_get_array_index_len`

Virtual method to override the surface array index length for a custom class extending `Mesh`.

### _SurfaceGetArrayLen

```csharp
public virtual int _SurfaceGetArrayLen(int index)
```

引擎名：`_surface_get_array_len`

Virtual method to override the surface array length for a custom class extending `Mesh`.

### _SurfaceGetArrays

```csharp
public virtual Array _SurfaceGetArrays(int index)
```

引擎名：`_surface_get_arrays`

Virtual method to override the surface arrays for a custom class extending `Mesh`.

### _SurfaceGetBlendShapeArrays

```csharp
public virtual Array<Array> _SurfaceGetBlendShapeArrays(int index)
```

引擎名：`_surface_get_blend_shape_arrays`

Virtual method to override the blend shape arrays for a custom class extending `Mesh`.

### _SurfaceGetFormat

```csharp
public virtual uint _SurfaceGetFormat(int index)
```

引擎名：`_surface_get_format`

Virtual method to override the surface format for a custom class extending `Mesh`.

### _SurfaceGetLods

```csharp
public virtual Dictionary _SurfaceGetLods(int index)
```

引擎名：`_surface_get_lods`

Virtual method to override the surface LODs for a custom class extending `Mesh`.

### _SurfaceGetMaterial

```csharp
public virtual Material _SurfaceGetMaterial(int index)
```

引擎名：`_surface_get_material`

Virtual method to override the surface material for a custom class extending `Mesh`.

### _SurfaceGetPrimitiveType

```csharp
public virtual uint _SurfaceGetPrimitiveType(int index)
```

引擎名：`_surface_get_primitive_type`

Virtual method to override the surface primitive type for a custom class extending `Mesh`.

### _SurfaceSetMaterial

```csharp
public virtual void _SurfaceSetMaterial(int index, Material material)
```

引擎名：`_surface_set_material`

Virtual method to override the setting of a `material` at the given `index` for a custom class extending `Mesh`.

## ArrayCustomFormat

命名空间 `Godot`；enum

### 枚举值

- `Rgba8Unorm = 0`：Indicates this custom channel contains unsigned normalized byte colors from 0 to 1, encoded as `System.Byte`[].
- `Rgba8Snorm = 1`：Indicates this custom channel contains signed normalized byte colors from -1 to 1, encoded as `System.Byte`[].
- `RgHalf = 2`：Indicates this custom channel contains half precision float colors, encoded as `System.Byte`[].
- `RgbaHalf = 3`：Indicates this custom channel contains half precision float colors, encoded as `System.Byte`[].
- `RFloat = 4`：Indicates this custom channel contains full float colors, in a `System.Single`[].
- `RgFloat = 5`：Indicates this custom channel contains full float colors, in a `System.Single`[].
- `RgbFloat = 6`：Indicates this custom channel contains full float colors, in a `System.Single`[].
- `RgbaFloat = 7`：Indicates this custom channel contains full float colors, in a `System.Single`[].
- `Max = 8`：Represents the size of the `Mesh.ArrayCustomFormat` enum.

## ArrayFormat

命名空间 `Godot`；enum

### 枚举值

- `FormatVertex = 1`：Mesh array contains vertices.
- `FormatNormal = 2`：Mesh array contains normals.
- `FormatTangent = 4`：Mesh array contains tangents.
- `FormatColor = 8`：Mesh array contains colors.
- `FormatTexUV = 16`：Mesh array contains UVs.
- `FormatTexUV2 = 32`：Mesh array contains second UV.
- `FormatCustom0 = 64`：Mesh array contains custom channel index 0.
- `FormatCustom1 = 128`：Mesh array contains custom channel index 1.
- `FormatCustom2 = 256`：Mesh array contains custom channel index 2.
- `FormatCustom3 = 512`：Mesh array contains custom channel index 3.
- `FormatBones = 1024`：Mesh array contains bones.
- `FormatWeights = 2048`：Mesh array contains bone weights.
- `FormatIndex = 4096`：Mesh array uses indices.
- `FormatBlendShapeMask = 7`：Mask of mesh channels permitted in blend shapes.
- `FormatCustomBase = 13`：Shift of first custom channel.
- `FormatCustomBits = 3`：Number of format bits per custom channel.
- `FormatCustom0Shift = 13`：Amount to shift [`Mesh.ArrayCustomFormat`](Mesh.md) for custom channel index 0.
- `FormatCustom1Shift = 16`：Amount to shift [`Mesh.ArrayCustomFormat`](Mesh.md) for custom channel index 1.
- `FormatCustom2Shift = 19`：Amount to shift [`Mesh.ArrayCustomFormat`](Mesh.md) for custom channel index 2.
- `FormatCustom3Shift = 22`：Amount to shift [`Mesh.ArrayCustomFormat`](Mesh.md) for custom channel index 3.
- `FormatCustomMask = 7`：Mask of custom format bits per custom channel.
- `CompressFlagsBase = 25`：Shift of first compress flag.
- `FlagUse2DVertices = 33554432`：Flag used to mark that the array contains 2D vertices.
- `FlagUseDynamicUpdate = 67108864`：Flag used to mark that the mesh data will use `GL_DYNAMIC_DRAW` on GLES.
- `FlagUse8BoneWeights = 134217728`：Flag used to mark that the mesh contains up to 8 bone influences per vertex.
- `FlagUsesEmptyVertexArray = 268435456`：Flag used to mark that the mesh intentionally contains no vertex array.
- `FlagCompressAttributes = 536870912`：Flag used to mark that a mesh is using compressed attributes (vertices, normals, tangents, UVs).

## ArrayType

命名空间 `Godot`；enum

### 枚举值

- `Vertex = 0`：[`Vector3`](Vector3.md)[], [`Vector2`](Vector2.md)[], or [`Collections.Array`](Collections.Array.md) of vertex positions.
- `Normal = 1`：[`Vector3`](Vector3.md)[] of vertex normals.
- `Tangent = 2`：`System.Single`[] of vertex tangents.
- `Color = 3`：[`Color`](Color.md)[] of vertex colors.
- `TexUV = 4`：[`Vector2`](Vector2.md)[] for UV coordinates.
- `TexUV2 = 5`：[`Vector2`](Vector2.md)[] for second UV coordinates.
- `Custom0 = 6`：Contains custom color channel 0.
- `Custom1 = 7`：Contains custom color channel 1.
- `Custom2 = 8`：Contains custom color channel 2.
- `Custom3 = 9`：Contains custom color channel 3.
- `Bones = 10`：`System.Single`[] or `System.Int32`[] of bone indices.
- `Weights = 11`：`System.Single`[] or `System.Double`[] of bone weights in the range `0.0` to `1.0` (inclusive).
- `Index = 12`：`System.Int32`[] of integers used as indices referencing vertices, colors, normals, tangents, and textures.
- `Max = 13`：Represents the size of the `Mesh.ArrayType` enum.

## BlendShapeMode

命名空间 `Godot`；enum

### 枚举值

- `Normalized = 0`：Blend shapes are normalized.
- `Relative = 1`：Blend shapes are relative to base weight.

## PrimitiveType

命名空间 `Godot`；enum

### 枚举值

- `Points = 0`：Render array as points (one vertex equals one point).
- `Lines = 1`：Render array as lines (every two vertices a line is created).
- `LineStrip = 2`：Render array as line strip.
- `Triangles = 3`：Render array as triangles (every three vertices a triangle is created).
- `TriangleStrip = 4`：Render array as triangle strips.
