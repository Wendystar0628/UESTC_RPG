<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationMeshSourceGeometryData3D

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Container for parsed source geometry data used in navigation mesh baking.

## 构造函数

### NavigationMeshSourceGeometryData3D

```csharp
public NavigationMeshSourceGeometryData3D()
```

## 属性

### Indices

```csharp
public int[] Indices { get; set; }
```

引擎名：`indices`

### ProjectedObstructions

```csharp
public Array ProjectedObstructions { get; set; }
```

引擎名：`projected_obstructions`

### Vertices

```csharp
public float[] Vertices { get; set; }
```

引擎名：`vertices`

## 方法

### AddFaces

```csharp
public void AddFaces(ReadOnlySpan<Vector3> faces, Transform3D xform)
```

引擎名：`add_faces`

Adds an array of vertex positions to the geometry data for navigation mesh baking to form triangulated faces. For each face the array must have three vertex positions in clockwise winding order. Since [`NavigationMesh`](NavigationMesh.md) resources have no transform, all vertex positions need to be offset by the node's transform using `xform`.

### AddFaces

```csharp
public void AddFaces(Vector3[] faces, Transform3D xform)
```

引擎名：`add_faces`

Adds an array of vertex positions to the geometry data for navigation mesh baking to form triangulated faces. For each face the array must have three vertex positions in clockwise winding order. Since [`NavigationMesh`](NavigationMesh.md) resources have no transform, all vertex positions need to be offset by the node's transform using `xform`.

### AddMesh

```csharp
public void AddMesh(Mesh mesh, Transform3D xform)
```

引擎名：`add_mesh`

Adds the geometry data of a [`Mesh`](Mesh.md) resource to the navigation mesh baking data. The mesh must have valid triangulated mesh data to be considered. Since [`NavigationMesh`](NavigationMesh.md) resources have no transform, all vertex positions need to be offset by the node's transform using `xform`.

### AddMeshArray

```csharp
public void AddMeshArray(Array meshArray, Transform3D xform)
```

引擎名：`add_mesh_array`

Adds an [`Collections.Array`](Collections.Array.md) the size of [`Mesh.ArrayType.Max`](Mesh.md) and with vertices at index [`Mesh.ArrayType.Vertex`](Mesh.md) and indices at index [`Mesh.ArrayType.Index`](Mesh.md) to the navigation mesh baking data. The array must have valid triangulated mesh data to be considered. Since [`NavigationMesh`](NavigationMesh.md) resources have no transform, all vertex positions need to be offset by the node's transform using `xform`.

### AddProjectedObstruction

```csharp
public void AddProjectedObstruction(ReadOnlySpan<Vector3> vertices, float elevation, float height, bool carve)
```

引擎名：`add_projected_obstruction`

Adds a projected obstruction shape to the source geometry. The `vertices` are considered projected on an xz-axes plane, placed at the global y-axis `elevation` and extruded by `height`. If `carve` is `true` the carved shape will not be affected by additional offsets (e.g. agent radius) of the navigation mesh baking process.

### AddProjectedObstruction

```csharp
public void AddProjectedObstruction(Vector3[] vertices, float elevation, float height, bool carve)
```

引擎名：`add_projected_obstruction`

Adds a projected obstruction shape to the source geometry. The `vertices` are considered projected on an xz-axes plane, placed at the global y-axis `elevation` and extruded by `height`. If `carve` is `true` the carved shape will not be affected by additional offsets (e.g. agent radius) of the navigation mesh baking process.

### AppendArrays

```csharp
public void AppendArrays(ReadOnlySpan<float> vertices, ReadOnlySpan<int> indices)
```

引擎名：`append_arrays`

Appends arrays of `vertices` and `indices` at the end of the existing arrays. Adds the existing index as an offset to the appended indices.

### AppendArrays

```csharp
public void AppendArrays(float[] vertices, int[] indices)
```

引擎名：`append_arrays`

Appends arrays of `vertices` and `indices` at the end of the existing arrays. Adds the existing index as an offset to the appended indices.

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears the internal data.

### ClearProjectedObstructions

```csharp
public void ClearProjectedObstructions()
```

引擎名：`clear_projected_obstructions`

Clears all projected obstructions.

### GetBounds

```csharp
public Aabb GetBounds()
```

引擎名：`get_bounds`

Returns an axis-aligned bounding box that covers all the stored geometry data. The bounds are calculated when calling this function with the result cached until further geometry changes are made.

### GetIndices

```csharp
public int[] GetIndices()
```

引擎名：`get_indices`

Returns the parsed source geometry data indices array.

### GetProjectedObstructions

```csharp
public Array GetProjectedObstructions()
```

引擎名：`get_projected_obstructions`

Returns the projected obstructions as an [`Collections.Array`](Collections.Array.md) of dictionaries. Each [`Collections.Dictionary`](Collections.Dictionary.md) contains the following entries:

- `vertices` - A `System.Single`[] that defines the outline points of the projected shape.

- `elevation` - A `System.Single` that defines the projected shape placement on the y-axis.

- `height` - A `System.Single` that defines how much the projected shape is extruded along the y-axis.

- `carve` - A `System.Boolean` that defines how the obstacle affects the navigation mesh baking. If `true` the projected shape will not be affected by addition offsets, e.g. agent radius.

### GetVertices

```csharp
public float[] GetVertices()
```

引擎名：`get_vertices`

Returns the parsed source geometry data vertices array.

### HasData

```csharp
public bool HasData()
```

引擎名：`has_data`

Returns `true` when parsed source geometry data exists.

### Merge

```csharp
public void Merge(NavigationMeshSourceGeometryData3D otherGeometry)
```

引擎名：`merge`

Adds the geometry data of another `NavigationMeshSourceGeometryData3D` to the navigation mesh baking data.

### SetIndices

```csharp
public void SetIndices(ReadOnlySpan<int> indices)
```

引擎名：`set_indices`

Sets the parsed source geometry data indices. The indices need to be matched with appropriated vertices.

**Warning:** Inappropriate data can crash the baking process of the involved third-party libraries.

### SetIndices

```csharp
public void SetIndices(int[] indices)
```

引擎名：`set_indices`

Sets the parsed source geometry data indices. The indices need to be matched with appropriated vertices.

**Warning:** Inappropriate data can crash the baking process of the involved third-party libraries.

### SetProjectedObstructions

```csharp
public void SetProjectedObstructions(Array projectedObstructions)
```

引擎名：`set_projected_obstructions`

Sets the projected obstructions with an Array of Dictionaries with the following key value pairs:

### SetVertices

```csharp
public void SetVertices(ReadOnlySpan<float> vertices)
```

引擎名：`set_vertices`

Sets the parsed source geometry data vertices. The vertices need to be matched with appropriated indices.

**Warning:** Inappropriate data can crash the baking process of the involved third-party libraries.

### SetVertices

```csharp
public void SetVertices(float[] vertices)
```

引擎名：`set_vertices`

Sets the parsed source geometry data vertices. The vertices need to be matched with appropriated indices.

**Warning:** Inappropriate data can crash the baking process of the involved third-party libraries.
