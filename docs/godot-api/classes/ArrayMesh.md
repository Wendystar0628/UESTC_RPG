<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ArrayMesh

命名空间 `Godot`；class；继承 [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `ArrayMesh` is used to construct a [`Mesh`](Mesh.md) by specifying the attributes as arrays.

The most basic example is the creation of a single triangle:

```csharp
Vector3[] vertices =
[
new Vector3(0, 1, 0),
new Vector3(1, 0, 0),
new Vector3(0, 0, 1),
];

// Initialize the ArrayMesh.
var arrMesh = new ArrayMesh();
Godot.Collections.Array arrays = [];
arrays.Resize((int)Mesh.ArrayType.Max);
arrays[(int)Mesh.ArrayType.Vertex] = vertices;

// Create the Mesh.
arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
var m = new MeshInstance3D();
m.Mesh = arrMesh;

```

The [`MeshInstance3D`](MeshInstance3D.md) is ready to be added to the [`SceneTree`](SceneTree.md) to be shown.

See also [`ImmediateMesh`](ImmediateMesh.md), [`MeshDataTool`](MeshDataTool.md) and [`SurfaceTool`](SurfaceTool.md) for procedural geometry generation.

**Note:** Godot uses clockwise [winding order](https://learnopengl.com/Advanced-OpenGL/Face-culling) for front faces of triangle primitive modes.

## 构造函数

### ArrayMesh

```csharp
public ArrayMesh()
```

## 属性

### BlendShapeMode

```csharp
public Mesh.BlendShapeMode BlendShapeMode { get; set; }
```

引擎名：`blend_shape_mode`

The blend shape mode.

### CustomAabb

```csharp
public Aabb CustomAabb { get; set; }
```

引擎名：`custom_aabb`

Overrides the [`Aabb`](Aabb.md) with one defined by user for use with frustum culling. Especially useful to avoid unexpected culling when using a shader to offset vertices.

### ShadowMesh

```csharp
public ArrayMesh ShadowMesh { get; set; }
```

引擎名：`shadow_mesh`

An optional mesh which can be used for rendering shadows and the depth prepass. Can be used to increase performance by supplying a mesh with fused vertices and only vertex position data (without normals, UVs, colors, etc.).

**Note:** This mesh must have exactly the same vertex positions as the source mesh (including the source mesh's LODs, if present). If vertex positions differ, then the mesh will not draw correctly.

### _BlendShapeNames

```csharp
public string[] _BlendShapeNames { get; set; }
```

### _Surfaces

```csharp
public Array _Surfaces { get; set; }
```

## 方法

### AddBlendShape

```csharp
public void AddBlendShape(StringName name)
```

引擎名：`add_blend_shape`

Adds name for a blend shape that will be added with `AddSurfaceFromArrays()`. Must be called before surface is added.

### AddSurfaceFromArrays

```csharp
public void AddSurfaceFromArrays(Mesh.PrimitiveType primitive, Array arrays, Array<Array> blendShapes = null, Dictionary lods = null, Mesh.ArrayFormat flags = Mesh.ArrayFormat.)
```

引擎名：`add_surface_from_arrays`

Creates a new surface. [`Mesh.GetSurfaceCount`](Mesh.md) will become the `surf_idx` for this new surface.

Surfaces are created to be rendered using a `primitive`, which may be any of the values defined in [`Mesh.PrimitiveType`](Mesh.md).

The `arrays` argument is an array of arrays. Each of the [`Mesh.ArrayType.Max`](Mesh.md) elements contains an array with some of the mesh data for this surface as described by the corresponding member of [`Mesh.ArrayType`](Mesh.md) or `null` if it is not used by the surface. For example, `arrays[0]` is the array of vertices. That first vertex sub-array is always required; the others are optional. Adding an index array puts this surface into "index mode" where the vertex and other arrays become the sources of data and the index array defines the vertex order. All sub-arrays must have the same length as the vertex array (or be an exact multiple of the vertex array's length, when multiple elements of a sub-array correspond to a single vertex) or be empty, except for [`Mesh.ArrayType.Index`](Mesh.md) if it is used.

The `blendShapes` argument is an array of vertex data for each blend shape. Each element is an array of the same structure as `arrays`, but [`Mesh.ArrayType.Vertex`](Mesh.md), [`Mesh.ArrayType.Normal`](Mesh.md), and [`Mesh.ArrayType.Tangent`](Mesh.md) are set if and only if they are set in `arrays` and all other entries are `null`.

The `lods` argument is a dictionary with `System.Single` keys and `System.Int32`[] values. Each entry in the dictionary represents an LOD level of the surface, where the value is the [`Mesh.ArrayType.Index`](Mesh.md) array to use for the LOD level and the key is roughly proportional to the distance at which the LOD stats being used. I.e., increasing the key of an LOD also increases the distance that the objects has to be from the camera before the LOD is used.

The `flags` argument is the bitwise OR of, as required: One value of [`Mesh.ArrayCustomFormat`](Mesh.md) left shifted by `ARRAY_FORMAT_CUSTOMn_SHIFT` for each custom channel in use, [`Mesh.ArrayFormat.FlagUseDynamicUpdate`](Mesh.md), [`Mesh.ArrayFormat.FlagUse8BoneWeights`](Mesh.md), or [`Mesh.ArrayFormat.FlagUsesEmptyVertexArray`](Mesh.md).

**Note:** When using indices, it is recommended to only use points, lines, or triangles.

### ClearBlendShapes

```csharp
public void ClearBlendShapes()
```

引擎名：`clear_blend_shapes`

Removes all blend shapes from this `ArrayMesh`.

### ClearSurfaces

```csharp
public void ClearSurfaces()
```

引擎名：`clear_surfaces`

Removes all surfaces from this `ArrayMesh`.

### GetBlendShapeCount

```csharp
public int GetBlendShapeCount()
```

引擎名：`get_blend_shape_count`

Returns the number of blend shapes that the `ArrayMesh` holds.

### GetBlendShapeMode

```csharp
public Mesh.BlendShapeMode GetBlendShapeMode()
```

引擎名：`get_blend_shape_mode`

### GetBlendShapeName

```csharp
public StringName GetBlendShapeName(int index)
```

引擎名：`get_blend_shape_name`

Returns the name of the blend shape at this index.

### GetCustomAabb

```csharp
public Aabb GetCustomAabb()
```

引擎名：`get_custom_aabb`

### GetShadowMesh

```csharp
public ArrayMesh GetShadowMesh()
```

引擎名：`get_shadow_mesh`

### LightmapUnwrap

```csharp
public Error LightmapUnwrap(Transform3D transform, float texelSize)
```

引擎名：`lightmap_unwrap`

Performs a UV unwrap on the `ArrayMesh` to prepare the mesh for lightmapping.

### RegenNormalMaps

```csharp
public void RegenNormalMaps()
```

引擎名：`regen_normal_maps`

Regenerates tangents for each of the `ArrayMesh`'s surfaces.

### SetBlendShapeMode

```csharp
public void SetBlendShapeMode(Mesh.BlendShapeMode mode)
```

引擎名：`set_blend_shape_mode`

### SetBlendShapeName

```csharp
public void SetBlendShapeName(int index, StringName name)
```

引擎名：`set_blend_shape_name`

Sets the name of the blend shape at this index.

### SetCustomAabb

```csharp
public void SetCustomAabb(Aabb aabb)
```

引擎名：`set_custom_aabb`

### SetShadowMesh

```csharp
public void SetShadowMesh(ArrayMesh mesh)
```

引擎名：`set_shadow_mesh`

### SurfaceFindByName

```csharp
public int SurfaceFindByName(string name)
```

引擎名：`surface_find_by_name`

Returns the index of the first surface with this name held within this `ArrayMesh`. If none are found, -1 is returned.

### SurfaceGetArrayIndexLen

```csharp
public int SurfaceGetArrayIndexLen(int surfIdx)
```

引擎名：`surface_get_array_index_len`

Returns the length in indices of the index array in the requested surface (see `AddSurfaceFromArrays()`).

### SurfaceGetArrayLen

```csharp
public int SurfaceGetArrayLen(int surfIdx)
```

引擎名：`surface_get_array_len`

Returns the length in vertices of the vertex array in the requested surface (see `AddSurfaceFromArrays()`).

### SurfaceGetFormat

```csharp
public Mesh.ArrayFormat SurfaceGetFormat(int surfIdx)
```

引擎名：`surface_get_format`

Returns the format mask of the requested surface (see `AddSurfaceFromArrays()`).

### SurfaceGetName

```csharp
public string SurfaceGetName(int surfIdx)
```

引擎名：`surface_get_name`

Gets the name assigned to this surface.

### SurfaceGetPrimitiveType

```csharp
public Mesh.PrimitiveType SurfaceGetPrimitiveType(int surfIdx)
```

引擎名：`surface_get_primitive_type`

Returns the primitive type of the requested surface (see `AddSurfaceFromArrays()`).

### SurfaceRemove

```csharp
public void SurfaceRemove(int surfIdx)
```

引擎名：`surface_remove`

Removes the surface at the given index from the Mesh, shifting surfaces with higher index down by one.

### SurfaceSetName

```csharp
public void SurfaceSetName(int surfIdx, string name)
```

引擎名：`surface_set_name`

Sets a name for a given surface.

### SurfaceUpdateAttributeRegion

```csharp
public void SurfaceUpdateAttributeRegion(int surfIdx, int offset, ReadOnlySpan<byte> data)
```

引擎名：`surface_update_attribute_region`

Updates the attribute buffer of this mesh's surface with the given `data`. The expected data per attribute is 12 or 8 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector3`](Vector3.md) or [`Vector2`](Vector2.md) vertices. This value can be determined with [`RenderingServer.MeshSurfaceGetFormatAttributeStride`](RenderingServer.md).

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each attribute.

A [`Vector3`](Vector3.md)[] of attribute locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### SurfaceUpdateAttributeRegion

```csharp
public void SurfaceUpdateAttributeRegion(int surfIdx, int offset, byte[] data)
```

引擎名：`surface_update_attribute_region`

Updates the attribute buffer of this mesh's surface with the given `data`. The expected data per attribute is 12 or 8 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector3`](Vector3.md) or [`Vector2`](Vector2.md) vertices. This value can be determined with [`RenderingServer.MeshSurfaceGetFormatAttributeStride`](RenderingServer.md).

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each attribute.

A [`Vector3`](Vector3.md)[] of attribute locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### SurfaceUpdateSkinRegion

```csharp
public void SurfaceUpdateSkinRegion(int surfIdx, int offset, ReadOnlySpan<byte> data)
```

引擎名：`surface_update_skin_region`

Updates the skin buffer of this mesh's surface with the given `data`. The expected data per skin is 12 or 8 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector3`](Vector3.md) or [`Vector2`](Vector2.md) vertices. This value can be determined with [`RenderingServer.MeshSurfaceGetFormatSkinStride`](RenderingServer.md).

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each skin.

A [`Vector3`](Vector3.md)[] of skin locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### SurfaceUpdateSkinRegion

```csharp
public void SurfaceUpdateSkinRegion(int surfIdx, int offset, byte[] data)
```

引擎名：`surface_update_skin_region`

Updates the skin buffer of this mesh's surface with the given `data`. The expected data per skin is 12 or 8 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector3`](Vector3.md) or [`Vector2`](Vector2.md) vertices. This value can be determined with [`RenderingServer.MeshSurfaceGetFormatSkinStride`](RenderingServer.md).

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each skin.

A [`Vector3`](Vector3.md)[] of skin locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### SurfaceUpdateVertexRegion

```csharp
public void SurfaceUpdateVertexRegion(int surfIdx, int offset, ReadOnlySpan<byte> data)
```

引擎名：`surface_update_vertex_region`

Updates the vertex buffer of this mesh's surface with the given `data`. The expected data per vertex is 12 or 8 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector3`](Vector3.md) or [`Vector2`](Vector2.md) vertices. This value can be determined with [`RenderingServer.MeshSurfaceGetFormatVertexStride`](RenderingServer.md).

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each vertex.

A [`Vector3`](Vector3.md)[] of vertex locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.

### SurfaceUpdateVertexRegion

```csharp
public void SurfaceUpdateVertexRegion(int surfIdx, int offset, byte[] data)
```

引擎名：`surface_update_vertex_region`

Updates the vertex buffer of this mesh's surface with the given `data`. The expected data per vertex is 12 or 8 bytes (4 bytes per float, 2 floats per [`Vector2`](Vector2.md), and 3 floats per [`Vector3`](Vector3.md)) depending on if the mesh is using [`Vector3`](Vector3.md) or [`Vector2`](Vector2.md) vertices. This value can be determined with [`RenderingServer.MeshSurfaceGetFormatVertexStride`](RenderingServer.md).

The starting point of the updates can be changed with `offset`. The value of `offset` should be a multiple of 12 bytes in most cases to align to each vertex.

A [`Vector3`](Vector3.md)[] of vertex locations can be converted into a `System.Byte`[] using `PackedVector3Array.to_byte_array` for use in `data`.
