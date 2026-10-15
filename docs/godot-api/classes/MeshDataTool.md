<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MeshDataTool

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

MeshDataTool provides access to individual vertices in a [`Mesh`](Mesh.md). It allows users to read and edit vertex data of meshes. It also creates an array of faces and edges.

To use MeshDataTool, load a mesh with `CreateFromSurface()`. When you are finished editing the data commit the data to a mesh with `CommitToSurface()`.

Below is an example of how MeshDataTool may be used.

```csharp
var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new BoxMesh().GetMeshArrays());
var mdt = new MeshDataTool();
mdt.CreateFromSurface(mesh, 0);
for (var i = 0; i < mdt.GetVertexCount(); i++)
{
Vector3 vertex = mdt.GetVertex(i);
// In this example we extend the mesh by one unit, which results in separated faces as it is flat shaded.
vertex += mdt.GetVertexNormal(i);
// Save your change.
mdt.SetVertex(i, vertex);
}
mesh.ClearSurfaces();
mdt.CommitToSurface(mesh);
var mi = new MeshInstance();
mi.Mesh = mesh;
AddChild(mi);

```

See also [`ArrayMesh`](ArrayMesh.md), [`ImmediateMesh`](ImmediateMesh.md) and [`SurfaceTool`](SurfaceTool.md) for procedural geometry generation.

**Note:** Godot uses clockwise [winding order](https://learnopengl.com/Advanced-OpenGL/Face-culling) for front faces of triangle primitive modes.

## 构造函数

### MeshDataTool

```csharp
public MeshDataTool()
```

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clears all data currently in MeshDataTool.

### CommitToSurface

```csharp
public Error CommitToSurface(ArrayMesh mesh)
```

引擎名：`commit_to_surface`

Adds a new surface to specified [`Mesh`](Mesh.md) with edited data.

### CommitToSurface

```csharp
public Error CommitToSurface(ArrayMesh mesh, ulong compressionFlags = 0)
```

引擎名：`commit_to_surface`

Adds a new surface to specified [`Mesh`](Mesh.md) with edited data.

### CreateFromSurface

```csharp
public Error CreateFromSurface(ArrayMesh mesh, int surface)
```

引擎名：`create_from_surface`

Uses specified surface of given [`Mesh`](Mesh.md) to populate data for MeshDataTool.

Requires [`Mesh`](Mesh.md) with primitive type [`Mesh.PrimitiveType.Triangles`](Mesh.md).

### GetEdgeCount

```csharp
public int GetEdgeCount()
```

引擎名：`get_edge_count`

Returns the number of edges in this [`Mesh`](Mesh.md).

### GetEdgeFaces

```csharp
public int[] GetEdgeFaces(int idx)
```

引擎名：`get_edge_faces`

Returns array of faces that touch given edge.

### GetEdgeMeta

```csharp
public Variant GetEdgeMeta(int idx)
```

引擎名：`get_edge_meta`

Returns meta information assigned to given edge.

### GetEdgeVertex

```csharp
public int GetEdgeVertex(int idx, int vertex)
```

引擎名：`get_edge_vertex`

Returns the index of the specified `vertex` connected to the edge at index `idx`.

`vertex` can only be `0` or `1`, as edges are composed of two vertices.

### GetFaceCount

```csharp
public int GetFaceCount()
```

引擎名：`get_face_count`

Returns the number of faces in this [`Mesh`](Mesh.md).

### GetFaceEdge

```csharp
public int GetFaceEdge(int idx, int edge)
```

引擎名：`get_face_edge`

Returns the edge associated with the face at index `idx`.

`edge` argument must be either `0`, `1`, or `2` because a face only has three edges.

### GetFaceMeta

```csharp
public Variant GetFaceMeta(int idx)
```

引擎名：`get_face_meta`

Returns the metadata associated with the given face.

### GetFaceNormal

```csharp
public Vector3 GetFaceNormal(int idx)
```

引擎名：`get_face_normal`

Calculates and returns the face normal of the given face.

### GetFaceVertex

```csharp
public int GetFaceVertex(int idx, int vertex)
```

引擎名：`get_face_vertex`

Returns the specified vertex index of the given face.

`vertex` must be either `0`, `1`, or `2` because faces contain three vertices.

```csharp
int index = meshDataTool.GetFaceVertex(0, 1); // Gets the index of the second vertex of the first face.
Vector3 position = meshDataTool.GetVertex(index);
Vector3 normal = meshDataTool.GetVertexNormal(index);

```

### GetFormat

```csharp
public ulong GetFormat()
```

引擎名：`get_format`

Returns the [`Mesh`](Mesh.md)'s format as a combination of the [`Mesh.ArrayFormat`](Mesh.md) flags. For example, a mesh containing both vertices and normals would return a format of `3` because [`Mesh.ArrayFormat.FormatVertex`](Mesh.md) is `1` and [`Mesh.ArrayFormat.FormatNormal`](Mesh.md) is `2`.

### GetMaterial

```csharp
public Material GetMaterial()
```

引擎名：`get_material`

Returns the material assigned to the [`Mesh`](Mesh.md).

### GetVertex

```csharp
public Vector3 GetVertex(int idx)
```

引擎名：`get_vertex`

Returns the position of the given vertex.

### GetVertexBones

```csharp
public int[] GetVertexBones(int idx)
```

引擎名：`get_vertex_bones`

Returns the bones of the given vertex.

### GetVertexColor

```csharp
public Color GetVertexColor(int idx)
```

引擎名：`get_vertex_color`

Returns the color of the given vertex.

### GetVertexCount

```csharp
public int GetVertexCount()
```

引擎名：`get_vertex_count`

Returns the total number of vertices in [`Mesh`](Mesh.md).

### GetVertexEdges

```csharp
public int[] GetVertexEdges(int idx)
```

引擎名：`get_vertex_edges`

Returns an array of edges that share the given vertex.

### GetVertexFaces

```csharp
public int[] GetVertexFaces(int idx)
```

引擎名：`get_vertex_faces`

Returns an array of faces that share the given vertex.

### GetVertexMeta

```csharp
public Variant GetVertexMeta(int idx)
```

引擎名：`get_vertex_meta`

Returns the metadata associated with the given vertex.

### GetVertexNormal

```csharp
public Vector3 GetVertexNormal(int idx)
```

引擎名：`get_vertex_normal`

Returns the normal of the given vertex.

### GetVertexTangent

```csharp
public Plane GetVertexTangent(int idx)
```

引擎名：`get_vertex_tangent`

Returns the tangent of the given vertex.

### GetVertexUV

```csharp
public Vector2 GetVertexUV(int idx)
```

引擎名：`get_vertex_uv`

Returns the UV of the given vertex.

### GetVertexUV2

```csharp
public Vector2 GetVertexUV2(int idx)
```

引擎名：`get_vertex_uv2`

Returns the UV2 of the given vertex.

### GetVertexWeights

```csharp
public float[] GetVertexWeights(int idx)
```

引擎名：`get_vertex_weights`

Returns bone weights of the given vertex.

### SetEdgeMeta

```csharp
public void SetEdgeMeta(int idx, Variant meta)
```

引擎名：`set_edge_meta`

Sets the metadata of the given edge.

### SetFaceMeta

```csharp
public void SetFaceMeta(int idx, Variant meta)
```

引擎名：`set_face_meta`

Sets the metadata of the given face.

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

Sets the material to be used by newly-constructed [`Mesh`](Mesh.md).

### SetVertex

```csharp
public void SetVertex(int idx, Vector3 vertex)
```

引擎名：`set_vertex`

Sets the position of the given vertex.

### SetVertexBones

```csharp
public void SetVertexBones(int idx, ReadOnlySpan<int> bones)
```

引擎名：`set_vertex_bones`

Sets the bones of the given vertex.

### SetVertexBones

```csharp
public void SetVertexBones(int idx, int[] bones)
```

引擎名：`set_vertex_bones`

Sets the bones of the given vertex.

### SetVertexColor

```csharp
public void SetVertexColor(int idx, Color color)
```

引擎名：`set_vertex_color`

Sets the color of the given vertex.

### SetVertexMeta

```csharp
public void SetVertexMeta(int idx, Variant meta)
```

引擎名：`set_vertex_meta`

Sets the metadata associated with the given vertex.

### SetVertexNormal

```csharp
public void SetVertexNormal(int idx, Vector3 normal)
```

引擎名：`set_vertex_normal`

Sets the normal of the given vertex.

### SetVertexTangent

```csharp
public void SetVertexTangent(int idx, Plane tangent)
```

引擎名：`set_vertex_tangent`

Sets the tangent of the given vertex.

**Note:** Even though `tangent` is a [`Plane`](Plane.md), it does not directly represent the tangent plane. Its `Plane.X`, `Plane.Y`, and `Plane.Z` represent the tangent vector and `Plane.D` should be either `-1` or `1`. See also [`Mesh.ArrayType.Tangent`](Mesh.md).

### SetVertexUV

```csharp
public void SetVertexUV(int idx, Vector2 uV)
```

引擎名：`set_vertex_uv`

Sets the UV of the given vertex.

### SetVertexUV2

```csharp
public void SetVertexUV2(int idx, Vector2 uV2)
```

引擎名：`set_vertex_uv2`

Sets the UV2 of the given vertex.

### SetVertexWeights

```csharp
public void SetVertexWeights(int idx, ReadOnlySpan<float> weights)
```

引擎名：`set_vertex_weights`

Sets the bone weights of the given vertex.

### SetVertexWeights

```csharp
public void SetVertexWeights(int idx, float[] weights)
```

引擎名：`set_vertex_weights`

Sets the bone weights of the given vertex.
