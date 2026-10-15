<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MeshConvexDecompositionSettings

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Parameters to be used with a [`Mesh`](Mesh.md) convex decomposition operation.

## 构造函数

### MeshConvexDecompositionSettings

```csharp
public MeshConvexDecompositionSettings()
```

## 属性

### ConvexHullApproximation

```csharp
public bool ConvexHullApproximation { get; set; }
```

引擎名：`convex_hull_approximation`

If `true`, uses approximation for computing convex hulls.

### ConvexHullDownsampling

```csharp
public uint ConvexHullDownsampling { get; set; }
```

引擎名：`convex_hull_downsampling`

Controls the precision of the convex-hull generation process during the clipping plane selection stage. Ranges from `1` to `16`.

### MaxConcavity

```csharp
public float MaxConcavity { get; set; }
```

引擎名：`max_concavity`

Maximum concavity. Ranges from `0.0` to `1.0`.

### MaxConvexHulls

```csharp
public uint MaxConvexHulls { get; set; }
```

引擎名：`max_convex_hulls`

The maximum number of convex hulls to produce from the merge operation.

### MaxNumVerticesPerConvexHull

```csharp
public uint MaxNumVerticesPerConvexHull { get; set; }
```

引擎名：`max_num_vertices_per_convex_hull`

Controls the maximum number of triangles per convex-hull. Ranges from `4` to `1024`.

### MinVolumePerConvexHull

```csharp
public float MinVolumePerConvexHull { get; set; }
```

引擎名：`min_volume_per_convex_hull`

Controls the adaptive sampling of the generated convex-hulls. Ranges from `0.0` to `0.01`.

### Mode

```csharp
public MeshConvexDecompositionSettings.ModeEnum Mode { get; set; }
```

引擎名：`mode`

Mode for the approximate convex decomposition.

### NormalizeMesh

```csharp
public bool NormalizeMesh { get; set; }
```

引擎名：`normalize_mesh`

If `true`, normalizes the mesh before applying the convex decomposition.

### PlaneDownsampling

```csharp
public uint PlaneDownsampling { get; set; }
```

引擎名：`plane_downsampling`

Controls the granularity of the search for the "best" clipping plane. Ranges from `1` to `16`.

### ProjectHullVertices

```csharp
public bool ProjectHullVertices { get; set; }
```

引擎名：`project_hull_vertices`

If `true`, projects output convex hull vertices onto the original source mesh to increase floating-point accuracy of the results.

### Resolution

```csharp
public uint Resolution { get; set; }
```

引擎名：`resolution`

Maximum number of voxels generated during the voxelization stage.

### RevolutionAxesClippingBias

```csharp
public float RevolutionAxesClippingBias { get; set; }
```

引擎名：`revolution_axes_clipping_bias`

Controls the bias toward clipping along revolution axes. Ranges from `0.0` to `1.0`.

### SymmetryPlanesClippingBias

```csharp
public float SymmetryPlanesClippingBias { get; set; }
```

引擎名：`symmetry_planes_clipping_bias`

Controls the bias toward clipping along symmetry planes. Ranges from `0.0` to `1.0`.

## 方法

### GetConvexHullApproximation

```csharp
public bool GetConvexHullApproximation()
```

引擎名：`get_convex_hull_approximation`

### GetConvexHullDownsampling

```csharp
public uint GetConvexHullDownsampling()
```

引擎名：`get_convex_hull_downsampling`

### GetMaxConcavity

```csharp
public float GetMaxConcavity()
```

引擎名：`get_max_concavity`

### GetMaxConvexHulls

```csharp
public uint GetMaxConvexHulls()
```

引擎名：`get_max_convex_hulls`

### GetMaxNumVerticesPerConvexHull

```csharp
public uint GetMaxNumVerticesPerConvexHull()
```

引擎名：`get_max_num_vertices_per_convex_hull`

### GetMinVolumePerConvexHull

```csharp
public float GetMinVolumePerConvexHull()
```

引擎名：`get_min_volume_per_convex_hull`

### GetMode

```csharp
public MeshConvexDecompositionSettings.ModeEnum GetMode()
```

引擎名：`get_mode`

### GetNormalizeMesh

```csharp
public bool GetNormalizeMesh()
```

引擎名：`get_normalize_mesh`

### GetPlaneDownsampling

```csharp
public uint GetPlaneDownsampling()
```

引擎名：`get_plane_downsampling`

### GetProjectHullVertices

```csharp
public bool GetProjectHullVertices()
```

引擎名：`get_project_hull_vertices`

### GetResolution

```csharp
public uint GetResolution()
```

引擎名：`get_resolution`

### GetRevolutionAxesClippingBias

```csharp
public float GetRevolutionAxesClippingBias()
```

引擎名：`get_revolution_axes_clipping_bias`

### GetSymmetryPlanesClippingBias

```csharp
public float GetSymmetryPlanesClippingBias()
```

引擎名：`get_symmetry_planes_clipping_bias`

### SetConvexHullApproximation

```csharp
public void SetConvexHullApproximation(bool convexHullApproximation)
```

引擎名：`set_convex_hull_approximation`

### SetConvexHullDownsampling

```csharp
public void SetConvexHullDownsampling(uint convexHullDownsampling)
```

引擎名：`set_convex_hull_downsampling`

### SetMaxConcavity

```csharp
public void SetMaxConcavity(float maxConcavity)
```

引擎名：`set_max_concavity`

### SetMaxConvexHulls

```csharp
public void SetMaxConvexHulls(uint maxConvexHulls)
```

引擎名：`set_max_convex_hulls`

### SetMaxNumVerticesPerConvexHull

```csharp
public void SetMaxNumVerticesPerConvexHull(uint maxNumVerticesPerConvexHull)
```

引擎名：`set_max_num_vertices_per_convex_hull`

### SetMinVolumePerConvexHull

```csharp
public void SetMinVolumePerConvexHull(float minVolumePerConvexHull)
```

引擎名：`set_min_volume_per_convex_hull`

### SetMode

```csharp
public void SetMode(MeshConvexDecompositionSettings.ModeEnum mode)
```

引擎名：`set_mode`

### SetNormalizeMesh

```csharp
public void SetNormalizeMesh(bool normalizeMesh)
```

引擎名：`set_normalize_mesh`

### SetPlaneDownsampling

```csharp
public void SetPlaneDownsampling(uint planeDownsampling)
```

引擎名：`set_plane_downsampling`

### SetProjectHullVertices

```csharp
public void SetProjectHullVertices(bool projectHullVertices)
```

引擎名：`set_project_hull_vertices`

### SetResolution

```csharp
public void SetResolution(uint minVolumePerConvexHull)
```

引擎名：`set_resolution`

### SetRevolutionAxesClippingBias

```csharp
public void SetRevolutionAxesClippingBias(float revolutionAxesClippingBias)
```

引擎名：`set_revolution_axes_clipping_bias`

### SetSymmetryPlanesClippingBias

```csharp
public void SetSymmetryPlanesClippingBias(float symmetryPlanesClippingBias)
```

引擎名：`set_symmetry_planes_clipping_bias`

## ModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Voxel = 0`：Constant for voxel-based approximate convex decomposition.
- `Tetrahedron = 1`：Constant for tetrahedron-based approximate convex decomposition.
