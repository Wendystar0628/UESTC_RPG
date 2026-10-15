<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PrimitiveMesh

命名空间 `Godot`；class；继承 [`Mesh`](Mesh.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Base class for all primitive meshes. Handles applying a [`Material`](Material.md) to a primitive mesh. Examples include [`BoxMesh`](BoxMesh.md), [`CapsuleMesh`](CapsuleMesh.md), [`CylinderMesh`](CylinderMesh.md), [`PlaneMesh`](PlaneMesh.md), [`PrismMesh`](PrismMesh.md), and [`SphereMesh`](SphereMesh.md).

## 构造函数

### PrimitiveMesh

```csharp
public PrimitiveMesh()
```

## 属性

### AddUV2

```csharp
public bool AddUV2 { get; set; }
```

引擎名：`add_uv2`

If set, generates UV2 UV coordinates applying a padding using the `UV2Padding` setting. UV2 is needed for lightmapping.

### CustomAabb

```csharp
public Aabb CustomAabb { get; set; }
```

引擎名：`custom_aabb`

Overrides the [`Aabb`](Aabb.md) with one defined by user for use with frustum culling. Especially useful to avoid unexpected culling when using a shader to offset vertices.

### FlipFaces

```csharp
public bool FlipFaces { get; set; }
```

引擎名：`flip_faces`

If `true`, the order of the vertices in each triangle is reversed, resulting in the backside of the mesh being drawn.

This gives the same result as using [`BaseMaterial3D.CullModeEnum.Front`](BaseMaterial3D.md) in [`BaseMaterial3D.CullMode`](BaseMaterial3D.md).

### Material

```csharp
public Material Material { get; set; }
```

引擎名：`material`

The current [`Material`](Material.md) of the primitive mesh.

### UV2Padding

```csharp
public float UV2Padding { get; set; }
```

引擎名：`uv2_padding`

If `AddUV2` is set, specifies the padding in pixels applied along seams of the mesh. Lower padding values allow making better use of the lightmap texture (resulting in higher texel density), but may introduce visible lightmap bleeding along edges.

If the size of the lightmap texture can't be determined when generating the mesh, UV2 is calculated assuming a texture size of 1024x1024.

## 方法

### GetAddUV2

```csharp
public bool GetAddUV2()
```

引擎名：`get_add_uv2`

### GetCustomAabb

```csharp
public Aabb GetCustomAabb()
```

引擎名：`get_custom_aabb`

### GetFlipFaces

```csharp
public bool GetFlipFaces()
```

引擎名：`get_flip_faces`

### GetMaterial

```csharp
public Material GetMaterial()
```

引擎名：`get_material`

### GetMeshArrays

```csharp
public Array GetMeshArrays()
```

引擎名：`get_mesh_arrays`

Returns the mesh arrays used to make up the surface of this primitive mesh.

**Example:** Pass the result to [`ArrayMesh.AddSurfaceFromArrays`](ArrayMesh.md) to create a new surface:

```csharp
var c = new CylinderMesh();
var arrMesh = new ArrayMesh();
arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, c.GetMeshArrays());

```

### GetUV2Padding

```csharp
public float GetUV2Padding()
```

引擎名：`get_uv2_padding`

### RequestUpdate

```csharp
public void RequestUpdate()
```

引擎名：`request_update`

Request an update of this primitive mesh based on its properties.

### SetAddUV2

```csharp
public void SetAddUV2(bool addUV2)
```

引擎名：`set_add_uv2`

### SetCustomAabb

```csharp
public void SetCustomAabb(Aabb aabb)
```

引擎名：`set_custom_aabb`

### SetFlipFaces

```csharp
public void SetFlipFaces(bool flipFaces)
```

引擎名：`set_flip_faces`

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

### SetUV2Padding

```csharp
public void SetUV2Padding(float uV2Padding)
```

引擎名：`set_uv2_padding`

### _CreateMeshArray

```csharp
public virtual Array _CreateMeshArray()
```

引擎名：`_create_mesh_array`

Override this method to customize how this primitive mesh should be generated. Should return an [`Collections.Array`](Collections.Array.md) where each element is another Array of values required for the mesh (see the [`Mesh.ArrayType`](Mesh.md) constants).
