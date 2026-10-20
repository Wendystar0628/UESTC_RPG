<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SurfaceTool

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The `SurfaceTool` is used to construct a [`Mesh`](Mesh.md) by specifying vertex attributes individually. It can be used to construct a [`Mesh`](Mesh.md) from a script. All properties except indices need to be added before calling `AddVertex()`. For example, to add vertex colors and UVs:

```csharp
var st = new SurfaceTool();
st.Begin(Mesh.PrimitiveType.Triangles);
st.SetColor(new Color(1, 0, 0));
st.SetUV(new Vector2(0, 0));
st.AddVertex(new Vector3(0, 0, 0));

```

The above `SurfaceTool` now contains one vertex of a triangle which has a UV coordinate and a specified [`Color`](Color.md). If another vertex were added without calling `SetUV()` or `SetColor()`, then the last values would be used.

Vertex attributes must be passed **before** calling `AddVertex()`. Failure to do so will result in an error when committing the vertex information to a mesh.

Additionally, the attributes used before the first vertex is added determine the format of the mesh. For example, if you only add UVs to the first vertex, you cannot add color to any of the subsequent vertices.

See also [`ArrayMesh`](ArrayMesh.md), [`ImmediateMesh`](ImmediateMesh.md) and [`MeshDataTool`](MeshDataTool.md) for procedural geometry generation.

**Note:** Godot uses clockwise [winding order](https://learnopengl.com/Advanced-OpenGL/Face-culling) for front faces of triangle primitive modes.

## 构造函数

### SurfaceTool

```csharp
public SurfaceTool()
```

## 方法

### AddIndex

```csharp
public void AddIndex(int index)
```

引擎名：`add_index`

Adds a vertex to index array if you are using indexed vertices. Does not need to be called before adding vertices.

### AddTriangleFan

```csharp
public void AddTriangleFan(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<Vector2> uvs, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uv2S, ReadOnlySpan<Vector3> normals, Array<Plane> tangents)
```

引擎名：`add_triangle_fan`

Inserts a triangle fan made of array data into [`Mesh`](Mesh.md) being constructed.

Requires the primitive type be set to [`Mesh.PrimitiveType.Triangles`](Mesh.md).

### AddTriangleFan

```csharp
public void AddTriangleFan(Vector3[] vertices, Vector2[] uvs = null, Color[] colors = null, Vector2[] uv2S = null, Vector3[] normals = null, Array<Plane> tangents = null)
```

引擎名：`add_triangle_fan`

Inserts a triangle fan made of array data into [`Mesh`](Mesh.md) being constructed.

Requires the primitive type be set to [`Mesh.PrimitiveType.Triangles`](Mesh.md).

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.
- `colors`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `uv2S`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.
- `normals`：If the parameter is null, then the default value is `Array.Empty<Vector3>()`.

### AddTriangleFan

```csharp
public void AddTriangleFan(Vector3[] vertices, Vector2[] uvs, Color[] colors, Vector2[] uv2S, Vector3[] normals, Array tangents)
```

引擎名：`add_triangle_fan`

Inserts a triangle fan made of array data into [`Mesh`](Mesh.md) being constructed.

Requires the primitive type be set to [`Mesh.PrimitiveType.Triangles`](Mesh.md).

参数：
- `uvs`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.
- `colors`：If the parameter is null, then the default value is `Array.Empty<Color>()`.
- `uv2S`：If the parameter is null, then the default value is `Array.Empty<Vector2>()`.
- `normals`：If the parameter is null, then the default value is `Array.Empty<Vector3>()`.

### AddVertex

```csharp
public void AddVertex(Vector3 vertex)
```

引擎名：`add_vertex`

Specifies the position of current vertex. Should be called after specifying other vertex properties (e.g. Color, UV).

### AppendFrom

```csharp
public void AppendFrom(Mesh existing, int surface, Transform3D transform)
```

引擎名：`append_from`

Append vertices from a given [`Mesh`](Mesh.md) surface onto the current vertex array with specified [`Transform3D`](Transform3D.md).

### Begin

```csharp
public void Begin(Mesh.PrimitiveType primitive)
```

引擎名：`begin`

Called before adding any vertices. Takes the primitive type as an argument (e.g. [`Mesh.PrimitiveType.Triangles`](Mesh.md)).

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clear all information passed into the surface tool so far.

### Commit

```csharp
public ArrayMesh Commit(ArrayMesh existing = null, ulong flags = 0)
```

引擎名：`commit`

Returns a constructed [`ArrayMesh`](ArrayMesh.md) from current information passed in. If an existing [`ArrayMesh`](ArrayMesh.md) is passed in as an argument, will add an extra surface to the existing [`ArrayMesh`](ArrayMesh.md).

The `flags` argument can be the bitwise OR of [`Mesh.ArrayFormat.FlagUseDynamicUpdate`](Mesh.md), [`Mesh.ArrayFormat.FlagUse8BoneWeights`](Mesh.md), or [`Mesh.ArrayFormat.FlagUsesEmptyVertexArray`](Mesh.md).

### Commit

```csharp
public ArrayMesh Commit(ArrayMesh existing, uint flags)
```

引擎名：`commit`

Returns a constructed [`ArrayMesh`](ArrayMesh.md) from current information passed in. If an existing [`ArrayMesh`](ArrayMesh.md) is passed in as an argument, will add an extra surface to the existing [`ArrayMesh`](ArrayMesh.md).

The `flags` argument can be the bitwise OR of [`Mesh.ArrayFormat.FlagUseDynamicUpdate`](Mesh.md), [`Mesh.ArrayFormat.FlagUse8BoneWeights`](Mesh.md), or [`Mesh.ArrayFormat.FlagUsesEmptyVertexArray`](Mesh.md).

### CommitToArrays

```csharp
public Array CommitToArrays()
```

引擎名：`commit_to_arrays`

Commits the data to the same format used by [`ArrayMesh.AddSurfaceFromArrays`](ArrayMesh.md), [`ImporterMesh.AddSurface`](ImporterMesh.md), and `CreateFromArrays()`. This way you can further process the mesh data using the [`ArrayMesh`](ArrayMesh.md) or [`ImporterMesh`](ImporterMesh.md) APIs.

### CreateFrom

```csharp
public void CreateFrom(Mesh existing, int surface)
```

引擎名：`create_from`

Creates a vertex array from an existing [`Mesh`](Mesh.md).

### CreateFromArrays

```csharp
public void CreateFromArrays(Array arrays, Mesh.PrimitiveType primitiveType = Mesh.PrimitiveType.Triangles)
```

引擎名：`create_from_arrays`

Creates this SurfaceTool from existing vertex arrays such as returned by `CommitToArrays()`, [`Mesh.SurfaceGetArrays`](Mesh.md), [`Mesh.SurfaceGetBlendShapeArrays`](Mesh.md), [`ImporterMesh.GetSurfaceArrays`](ImporterMesh.md), and [`ImporterMesh.GetSurfaceBlendShapeArrays`](ImporterMesh.md). `primitiveType` controls the type of mesh data, defaulting to [`Mesh.PrimitiveType.Triangles`](Mesh.md).

### CreateFromBlendShape

```csharp
public void CreateFromBlendShape(Mesh existing, int surface, string blendShape)
```

引擎名：`create_from_blend_shape`

Creates a vertex array from the specified blend shape of an existing [`Mesh`](Mesh.md). This can be used to extract a specific pose from a blend shape.

### Deindex

```csharp
public void Deindex()
```

引擎名：`deindex`

Removes the index array by expanding the vertex array.

### GenerateLod

```csharp
public int[] GenerateLod(float ndThreshold, int targetIndexCount = 3)
```

引擎名：`generate_lod`

Generates an LOD for a given `ndThreshold` in linear units (square root of quadric error metric), using at most `targetIndexCount` indices.

### GenerateNormals

```csharp
public void GenerateNormals(bool flip = false)
```

引擎名：`generate_normals`

Generates normals from vertices so you do not have to do it manually. If `flip` is `true`, the resulting normals will be inverted. `GenerateNormals()` should be called *after* generating geometry and *before* committing the mesh using `Commit()` or `CommitToArrays()`. For correct display of normal-mapped surfaces, you will also have to generate tangents using `GenerateTangents()`.

**Note:** `GenerateNormals()` only works if the primitive type is set to [`Mesh.PrimitiveType.Triangles`](Mesh.md).

**Note:** `GenerateNormals()` takes smooth groups into account. To generate smooth normals, set the smooth group to a value greater than or equal to `0` using `SetSmoothGroup()` or leave the smooth group at the default of `0`. To generate flat normals, set the smooth group to `-1` using `SetSmoothGroup()` prior to adding vertices.

### GenerateTangents

```csharp
public void GenerateTangents()
```

引擎名：`generate_tangents`

Generates a tangent vector for each vertex. Requires that each vertex already has UVs and normals set (see `GenerateNormals()`).

### GetAabb

```csharp
public Aabb GetAabb()
```

引擎名：`get_aabb`

Returns the axis-aligned bounding box of the vertex positions.

### GetCustomFormat

```csharp
public SurfaceTool.CustomFormat GetCustomFormat(int channelIndex)
```

引擎名：`get_custom_format`

Returns the format for custom `channelIndex` (currently up to 4). Returns [`SurfaceTool.CustomFormat.Max`](SurfaceTool.md) if this custom channel is unused.

### GetPrimitiveType

```csharp
public Mesh.PrimitiveType GetPrimitiveType()
```

引擎名：`get_primitive_type`

Returns the type of mesh geometry, such as [`Mesh.PrimitiveType.Triangles`](Mesh.md).

### GetSkinWeightCount

```csharp
public SurfaceTool.SkinWeightCount GetSkinWeightCount()
```

引擎名：`get_skin_weight_count`

By default, returns [`SurfaceTool.SkinWeightCount.Skin4Weights`](SurfaceTool.md) to indicate only 4 bone influences per vertex are used.

Returns [`SurfaceTool.SkinWeightCount.Skin8Weights`](SurfaceTool.md) if up to 8 influences are used.

**Note:** This function returns an enum, not the exact number of weights.

### Index

```csharp
public void Index()
```

引擎名：`index`

Shrinks the vertex array by creating an index array. This can improve performance by avoiding vertex reuse.

### OptimizeIndicesForCache

```csharp
public void OptimizeIndicesForCache()
```

引擎名：`optimize_indices_for_cache`

Optimizes triangle sorting for performance. Requires that `GetPrimitiveType()` is [`Mesh.PrimitiveType.Triangles`](Mesh.md).

### SetBones

```csharp
public void SetBones(ReadOnlySpan<int> bones)
```

引擎名：`set_bones`

Specifies an array of bones to use for the *next* vertex. `bones` must contain 4 integers.

### SetBones

```csharp
public void SetBones(int[] bones)
```

引擎名：`set_bones`

Specifies an array of bones to use for the *next* vertex. `bones` must contain 4 integers.

### SetColor

```csharp
public void SetColor(Color color)
```

引擎名：`set_color`

Specifies a [`Color`](Color.md) to use for the *next* vertex. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

**Note:** The material must have [`BaseMaterial3D.VertexColorUseAsAlbedo`](BaseMaterial3D.md) enabled for the vertex color to be visible.

### SetCustom

```csharp
public void SetCustom(int channelIndex, Color customColor)
```

引擎名：`set_custom`

Sets the custom value on this vertex for `channelIndex`.

`SetCustomFormat()` must be called first for this `channelIndex`. Formats which are not RGBA will ignore other color channels.

### SetCustomFormat

```csharp
public void SetCustomFormat(int channelIndex, SurfaceTool.CustomFormat format)
```

引擎名：`set_custom_format`

Sets the color format for this custom `channelIndex`. Use [`SurfaceTool.CustomFormat.Max`](SurfaceTool.md) to disable.

Must be invoked after `Begin()` and should be set before `Commit()` or `CommitToArrays()`.

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

Sets [`Material`](Material.md) to be used by the [`Mesh`](Mesh.md) you are constructing.

### SetNormal

```csharp
public void SetNormal(Vector3 normal)
```

引擎名：`set_normal`

Specifies a normal to use for the *next* vertex. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

### SetSkinWeightCount

```csharp
public void SetSkinWeightCount(SurfaceTool.SkinWeightCount count)
```

引擎名：`set_skin_weight_count`

Set to [`SurfaceTool.SkinWeightCount.Skin8Weights`](SurfaceTool.md) to indicate that up to 8 bone influences per vertex may be used.

By default, only 4 bone influences are used ([`SurfaceTool.SkinWeightCount.Skin4Weights`](SurfaceTool.md)).

**Note:** This function takes an enum, not the exact number of weights.

### SetSmoothGroup

```csharp
public void SetSmoothGroup(uint index)
```

引擎名：`set_smooth_group`

Specifies the smooth group to use for the *next* vertex. If this is never called, all vertices will have the default smooth group of `0` and will be smoothed with adjacent vertices of the same group. To produce a mesh with flat normals, set the smooth group to `-1`.

**Note:** This function actually takes a `uint32_t`, so C# users should use `uint32.MaxValue` instead of `-1` to produce a mesh with flat normals.

### SetTangent

```csharp
public void SetTangent(Plane tangent)
```

引擎名：`set_tangent`

Specifies a tangent to use for the *next* vertex. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

**Note:** Even though `tangent` is a [`Plane`](Plane.md), it does not directly represent the tangent plane. Its `Plane.X`, `Plane.Y`, and `Plane.Z` represent the tangent vector and `Plane.D` should be either `-1` or `1`. See also [`Mesh.ArrayType.Tangent`](Mesh.md).

### SetUV

```csharp
public void SetUV(Vector2 uV)
```

引擎名：`set_uv`

Specifies a set of UV coordinates to use for the *next* vertex. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

### SetUV2

```csharp
public void SetUV2(Vector2 uV2)
```

引擎名：`set_uv2`

Specifies an optional second set of UV coordinates to use for the *next* vertex. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

### SetWeights

```csharp
public void SetWeights(ReadOnlySpan<float> weights)
```

引擎名：`set_weights`

Specifies weight values to use for the *next* vertex. `weights` must contain 4 values. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

### SetWeights

```csharp
public void SetWeights(float[] weights)
```

引擎名：`set_weights`

Specifies weight values to use for the *next* vertex. `weights` must contain 4 values. If every vertex needs to have this information set and you fail to submit it for the first vertex, this information may not be used at all.

## CustomFormat

命名空间 `Godot`；enum

### 枚举值

- `Rgba8Unorm = 0`：Limits range of data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) to unsigned normalized 0 to 1 stored in 8 bits per channel.
- `Rgba8Snorm = 1`：Limits range of data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) to signed normalized -1 to 1 stored in 8 bits per channel.
- `RgHalf = 2`：Stores data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) as half precision floats, and uses only red and green color channels.
- `RgbaHalf = 3`：Stores data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) as half precision floats and uses all color channels.
- `RFloat = 4`：Stores data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) as full precision floats, and uses only red color channel.
- `RgFloat = 5`：Stores data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) as full precision floats, and uses only red and green color channels.
- `RgbFloat = 6`：Stores data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) as full precision floats, and uses only red, green and blue color channels.
- `RgbaFloat = 7`：Stores data passed to [`SurfaceTool.SetCustom`](SurfaceTool.md) as full precision floats, and uses all color channels.
- `Max = 8`：Used to indicate a disabled custom channel.

## SkinWeightCount

命名空间 `Godot`；enum

### 枚举值

- `Skin4Weights = 0`：Each individual vertex can be influenced by only 4 bone weights.
- `Skin8Weights = 1`：Each individual vertex can be influenced by up to 8 bone weights.
