<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ImporterMesh

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

ImporterMesh is a type of [`Resource`](Resource.md) analogous to [`ArrayMesh`](ArrayMesh.md). It contains vertex array-based geometry, divided in *surfaces*. Each surface contains a completely separate array and a material used to draw it. Design wise, a mesh with multiple surfaces is preferred to a single surface, because objects created in 3D editing software commonly contain multiple materials.

Unlike its runtime counterpart, `ImporterMesh` contains mesh data before various import steps, such as LOD and shadow mesh generation, have taken place. Modify surface data by calling `Clear()`, followed by `AddSurface()` for each surface.

## 构造函数

### ImporterMesh

```csharp
public ImporterMesh()
```

## 属性

### _Data

```csharp
public Dictionary _Data { get; set; }
```

## 方法

### AddBlendShape

```csharp
public void AddBlendShape(string name)
```

引擎名：`add_blend_shape`

Adds name for a blend shape that will be added with `AddSurface()`. Must be called before surface is added.

### AddSurface

```csharp
public void AddSurface(Mesh.PrimitiveType primitive, Array arrays, Array<Array> blendShapes = null, Dictionary lods = null, Material material = null, string name = "", ulong flags = 0)
```

引擎名：`add_surface`

Creates a new surface. [`Mesh.GetSurfaceCount`](Mesh.md) will become the `surf_idx` for this new surface.

Surfaces are created to be rendered using a `primitive`, which may be any of the values defined in [`Mesh.PrimitiveType`](Mesh.md).

The `arrays` argument is an array of arrays. Each of the [`Mesh.ArrayType.Max`](Mesh.md) elements contains an array with some of the mesh data for this surface as described by the corresponding member of [`Mesh.ArrayType`](Mesh.md) or `null` if it is not used by the surface. For example, `arrays[0]` is the array of vertices. That first vertex sub-array is always required; the others are optional. Adding an index array puts this surface into "index mode" where the vertex and other arrays become the sources of data and the index array defines the vertex order. All sub-arrays must have the same length as the vertex array (or be an exact multiple of the vertex array's length, when multiple elements of a sub-array correspond to a single vertex) or be empty, except for [`Mesh.ArrayType.Index`](Mesh.md) if it is used.

The `blendShapes` argument is an array of vertex data for each blend shape. Each element is an array of the same structure as `arrays`, but [`Mesh.ArrayType.Vertex`](Mesh.md), [`Mesh.ArrayType.Normal`](Mesh.md), and [`Mesh.ArrayType.Tangent`](Mesh.md) are set if and only if they are set in `arrays` and all other entries are `null`.

The `lods` argument is a dictionary with `System.Single` keys and `System.Int32`[] values. Each entry in the dictionary represents an LOD level of the surface, where the value is the [`Mesh.ArrayType.Index`](Mesh.md) array to use for the LOD level and the key is roughly proportional to the distance at which the LOD stats being used. I.e., increasing the key of an LOD also increases the distance that the objects has to be from the camera before the LOD is used.

The `flags` argument is the bitwise OR of, as required: One value of [`Mesh.ArrayCustomFormat`](Mesh.md) left shifted by `ARRAY_FORMAT_CUSTOMn_SHIFT` for each custom channel in use, [`Mesh.ArrayFormat.FlagUseDynamicUpdate`](Mesh.md), [`Mesh.ArrayFormat.FlagUse8BoneWeights`](Mesh.md), or [`Mesh.ArrayFormat.FlagUsesEmptyVertexArray`](Mesh.md).

**Note:** When using indices, it is recommended to only use points, lines, or triangles.

### AddSurface

```csharp
public void AddSurface(Mesh.PrimitiveType primitive, Array arrays, Array<Array> blendShapes, Dictionary lods, Material material, string name, uint flags)
```

引擎名：`add_surface`

Creates a new surface. [`Mesh.GetSurfaceCount`](Mesh.md) will become the `surf_idx` for this new surface.

Surfaces are created to be rendered using a `primitive`, which may be any of the values defined in [`Mesh.PrimitiveType`](Mesh.md).

The `arrays` argument is an array of arrays. Each of the [`Mesh.ArrayType.Max`](Mesh.md) elements contains an array with some of the mesh data for this surface as described by the corresponding member of [`Mesh.ArrayType`](Mesh.md) or `null` if it is not used by the surface. For example, `arrays[0]` is the array of vertices. That first vertex sub-array is always required; the others are optional. Adding an index array puts this surface into "index mode" where the vertex and other arrays become the sources of data and the index array defines the vertex order. All sub-arrays must have the same length as the vertex array (or be an exact multiple of the vertex array's length, when multiple elements of a sub-array correspond to a single vertex) or be empty, except for [`Mesh.ArrayType.Index`](Mesh.md) if it is used.

The `blendShapes` argument is an array of vertex data for each blend shape. Each element is an array of the same structure as `arrays`, but [`Mesh.ArrayType.Vertex`](Mesh.md), [`Mesh.ArrayType.Normal`](Mesh.md), and [`Mesh.ArrayType.Tangent`](Mesh.md) are set if and only if they are set in `arrays` and all other entries are `null`.

The `lods` argument is a dictionary with `System.Single` keys and `System.Int32`[] values. Each entry in the dictionary represents an LOD level of the surface, where the value is the [`Mesh.ArrayType.Index`](Mesh.md) array to use for the LOD level and the key is roughly proportional to the distance at which the LOD stats being used. I.e., increasing the key of an LOD also increases the distance that the objects has to be from the camera before the LOD is used.

The `flags` argument is the bitwise OR of, as required: One value of [`Mesh.ArrayCustomFormat`](Mesh.md) left shifted by `ARRAY_FORMAT_CUSTOMn_SHIFT` for each custom channel in use, [`Mesh.ArrayFormat.FlagUseDynamicUpdate`](Mesh.md), [`Mesh.ArrayFormat.FlagUse8BoneWeights`](Mesh.md), or [`Mesh.ArrayFormat.FlagUsesEmptyVertexArray`](Mesh.md).

**Note:** When using indices, it is recommended to only use points, lines, or triangles.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Removes all surfaces and blend shapes from this `ImporterMesh`.

### FromMesh

```csharp
public static ImporterMesh FromMesh(Mesh mesh)
```

引擎名：`from_mesh`

Converts the given [`Mesh`](Mesh.md) into an `ImporterMesh` by copying all its surfaces, blend shapes, materials, and metadata into a new `ImporterMesh` object.

### GenerateLods

```csharp
public void GenerateLods(float normalMergeAngle, float normalSplitAngle, Array boneTransformArray)
```

引擎名：`generate_lods`

Generates all lods for this ImporterMesh.

`normalMergeAngle` is in degrees and used in the same way as the importer settings in `lods`.

`normalSplitAngle` is not used and only remains for compatibility with older versions of the API.

The number of generated lods can be accessed using `GetSurfaceLodCount()`, and each LOD is available in `GetSurfaceLodSize()` and `GetSurfaceLodIndices()`.

`boneTransformArray` is an [`Collections.Array`](Collections.Array.md) which can be either empty or contain [`Transform3D`](Transform3D.md)s which, for each of the mesh's bone IDs, will apply mesh skinning when generating the LOD mesh variations. This is usually used to account for discrepancies in scale between the mesh itself and its skinning data.

### GetBlendShapeCount

```csharp
public int GetBlendShapeCount()
```

引擎名：`get_blend_shape_count`

Returns the number of blend shapes that the mesh holds.

### GetBlendShapeMode

```csharp
public Mesh.BlendShapeMode GetBlendShapeMode()
```

引擎名：`get_blend_shape_mode`

Returns the blend shape mode for this Mesh.

### GetBlendShapeName

```csharp
public string GetBlendShapeName(int blendShapeIdx)
```

引擎名：`get_blend_shape_name`

Returns the name of the blend shape at this index.

### GetLightmapSizeHint

```csharp
public Vector2I GetLightmapSizeHint()
```

引擎名：`get_lightmap_size_hint`

Returns the size hint of this mesh for lightmap-unwrapping in UV-space.

### GetMesh

```csharp
public ArrayMesh GetMesh(ArrayMesh baseMesh = null)
```

引擎名：`get_mesh`

Returns the mesh data represented by this `ImporterMesh` as a usable [`ArrayMesh`](ArrayMesh.md).

This method caches the returned mesh, and subsequent calls will return the cached data until `Clear()` is called.

If not yet cached and `baseMesh` is provided, `baseMesh` will be used and mutated.

### GetSurfaceArrays

```csharp
public Array GetSurfaceArrays(int surfaceIdx)
```

引擎名：`get_surface_arrays`

Returns the arrays for the vertices, normals, UVs, etc. that make up the requested surface. See `AddSurface()`.

### GetSurfaceBlendShapeArrays

```csharp
public Array GetSurfaceBlendShapeArrays(int surfaceIdx, int blendShapeIdx)
```

引擎名：`get_surface_blend_shape_arrays`

Returns a single set of blend shape arrays for the requested blend shape index for a surface.

### GetSurfaceCount

```csharp
public int GetSurfaceCount()
```

引擎名：`get_surface_count`

Returns the number of surfaces that the mesh holds.

### GetSurfaceFormat

```csharp
public ulong GetSurfaceFormat(int surfaceIdx)
```

引擎名：`get_surface_format`

Returns the format of the surface that the mesh holds.

### GetSurfaceLodCount

```csharp
public int GetSurfaceLodCount(int surfaceIdx)
```

引擎名：`get_surface_lod_count`

Returns the number of lods that the mesh holds on a given surface.

### GetSurfaceLodIndices

```csharp
public int[] GetSurfaceLodIndices(int surfaceIdx, int lodIdx)
```

引擎名：`get_surface_lod_indices`

Returns the index buffer of a lod for a surface.

### GetSurfaceLodSize

```csharp
public float GetSurfaceLodSize(int surfaceIdx, int lodIdx)
```

引擎名：`get_surface_lod_size`

Returns the screen ratio which activates a lod for a surface.

### GetSurfaceMaterial

```csharp
public Material GetSurfaceMaterial(int surfaceIdx)
```

引擎名：`get_surface_material`

Returns a [`Material`](Material.md) in a given surface. Surface is rendered using this material.

### GetSurfaceName

```csharp
public string GetSurfaceName(int surfaceIdx)
```

引擎名：`get_surface_name`

Gets the name assigned to this surface.

### GetSurfacePrimitiveType

```csharp
public Mesh.PrimitiveType GetSurfacePrimitiveType(int surfaceIdx)
```

引擎名：`get_surface_primitive_type`

Returns the primitive type of the requested surface (see `AddSurface()`).

### MergeImporterMeshes

```csharp
public static ImporterMesh MergeImporterMeshes(Array<ImporterMesh> importerMeshes, Array<Transform3D> relativeTransforms, bool deduplicateSurfaces = true)
```

引擎名：`merge_importer_meshes`

Merges multiple `ImporterMesh`es into a single `ImporterMesh`. Each input mesh is transformed by the corresponding [`Transform3D`](Transform3D.md) in the `relativeTransforms` array, which must be the same size as `importerMeshes`. Negative scales are supported, and the winding order in the mesh data will be corrected to account for this.

If `deduplicateSurfaces` is `true` and multiple meshes have surfaces with the same names and formats, the surfaces will be merged together when the meshes are merged, and will use the material from the first matching surface. This is useful for reducing the number of surfaces in the resulting mesh, and avoids duplicating materials. Surfaces with bone weights will never be deduplicated. If `deduplicateSurfaces` is `false`, the surfaces will always be kept separate, and will be given unique names.

**Warning:** Blend shapes and LODs are not supported and will be discarded. Do not use this function to discard blend shapes and LODs, as support for these may be added in the future.

### SetBlendShapeMode

```csharp
public void SetBlendShapeMode(Mesh.BlendShapeMode mode)
```

引擎名：`set_blend_shape_mode`

Sets the blend shape mode.

### SetLightmapSizeHint

```csharp
public void SetLightmapSizeHint(Vector2I size)
```

引擎名：`set_lightmap_size_hint`

Sets the size hint of this mesh for lightmap-unwrapping in UV-space.

### SetSurfaceMaterial

```csharp
public void SetSurfaceMaterial(int surfaceIdx, Material material)
```

引擎名：`set_surface_material`

Sets a [`Material`](Material.md) for a given surface. Surface will be rendered using this material.

### SetSurfaceName

```csharp
public void SetSurfaceName(int surfaceIdx, string name)
```

引擎名：`set_surface_name`

Sets a name for a given surface.
