<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VoxelGI

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`VoxelGI`s are used to provide high-quality real-time indirect light and reflections to scenes. They precompute the effect of objects that emit light and the effect of static geometry to simulate the behavior of complex light in real-time. `VoxelGI`s need to be baked before having a visible effect. However, once baked, dynamic objects will receive light from them. Furthermore, lights can be fully dynamic or baked.

**Note:** `VoxelGI` is only supported in the Forward+ rendering method, not Mobile or Compatibility.

**Procedural generation:** `VoxelGI` can be baked in an exported project, which makes it suitable for procedurally generated or user-built levels as long as all the geometry is generated in advance. For games where geometry is generated at any time during gameplay, SDFGI is more suitable (see [`Environment.SdfgiEnabled`](Environment.md)).

**Performance:** `VoxelGI` is relatively demanding on the GPU and is not suited to low-end hardware such as integrated graphics (consider [`LightmapGI`](LightmapGI.md) instead). To improve performance, adjust `ProjectSettings.rendering/global_illumination/voxel_gi/quality` and enable `ProjectSettings.rendering/global_illumination/gi/use_half_resolution` in the Project Settings. To provide a fallback for low-end hardware, consider adding an option to disable `VoxelGI` in your project's options menus. A `VoxelGI` node can be disabled by hiding it.

**Note:** Meshes should have sufficiently thick walls to avoid light leaks (avoid one-sided walls). For interior levels, enclose your level geometry in a sufficiently large box and bridge the loops to close the mesh. To further prevent light leaks, you can also strategically place temporary [`MeshInstance3D`](MeshInstance3D.md) nodes with their [`GeometryInstance3D.GIMode`](GeometryInstance3D.md) set to [`GeometryInstance3D.GIModeEnum.Static`](GeometryInstance3D.md). These temporary nodes can then be hidden after baking the `VoxelGI` node.

## 构造函数

### VoxelGI

```csharp
public VoxelGI()
```

## 属性

### CameraAttributes

```csharp
public CameraAttributes CameraAttributes { get; set; }
```

引擎名：`camera_attributes`

The [`CameraAttributes`](CameraAttributes.md) resource that specifies exposure levels to bake at. Auto-exposure and non exposure properties will be ignored. Exposure settings should be used to reduce the dynamic range present when baking. If exposure is too high, the `VoxelGI` will have banding artifacts or may have over-exposure artifacts.

### Data

```csharp
public VoxelGIData Data { get; set; }
```

引擎名：`data`

The [`VoxelGIData`](VoxelGIData.md) resource that holds the data for this `VoxelGI`.

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

The size of the area covered by the `VoxelGI`. This must be `1.0` or greater on each axis.

**Note:** If you make the size larger without increasing the number of subdivisions with `Subdiv`, the size of each cell will increase and result in less detailed lighting.

### Subdiv

```csharp
public VoxelGI.SubdivEnum Subdiv { get; set; }
```

引擎名：`subdiv`

Number of times to subdivide the grid that the `VoxelGI` operates on. A higher number results in finer detail and thus higher visual quality, while lower numbers result in better performance.

## 方法

### Bake

```csharp
public void Bake(Node fromNode = null, bool createVisualDebug = false)
```

引擎名：`bake`

Bakes the effect from all [`GeometryInstance3D`](GeometryInstance3D.md)s marked with [`GeometryInstance3D.GIModeEnum.Static`](GeometryInstance3D.md) and [`Light3D`](Light3D.md)s marked with either [`Light3D.BakeMode.Static`](Light3D.md) or [`Light3D.BakeMode.Dynamic`](Light3D.md). If `createVisualDebug` is `true`, after baking the light, this will generate a [`MultiMesh`](MultiMesh.md) that has a cube representing each solid cell with each cube colored to the cell's albedo color. This can be used to visualize the `VoxelGI`'s data and debug any issues that may be occurring.

**Note:** `Bake()` works from the editor and in exported projects. This makes it suitable for procedurally generated or user-built levels. Baking a `VoxelGI` node generally takes from 5 to 20 seconds in most scenes. Reducing `Subdiv` can speed up baking.

**Note:** [`GeometryInstance3D`](GeometryInstance3D.md)s and [`Light3D`](Light3D.md)s must be fully ready before `Bake()` is called. If you are procedurally creating those and some meshes or lights are missing from your baked `VoxelGI`, use `call_deferred("bake")` instead of calling `Bake()` directly.

### DebugBake

```csharp
public void DebugBake()
```

引擎名：`debug_bake`

Calls `Bake()` with `create_visual_debug` enabled.

### GetCameraAttributes

```csharp
public CameraAttributes GetCameraAttributes()
```

引擎名：`get_camera_attributes`

### GetProbeData

```csharp
public VoxelGIData GetProbeData()
```

引擎名：`get_probe_data`

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### GetSubdiv

```csharp
public VoxelGI.SubdivEnum GetSubdiv()
```

引擎名：`get_subdiv`

### SetCameraAttributes

```csharp
public void SetCameraAttributes(CameraAttributes cameraAttributes)
```

引擎名：`set_camera_attributes`

### SetProbeData

```csharp
public void SetProbeData(VoxelGIData data)
```

引擎名：`set_probe_data`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`

### SetSubdiv

```csharp
public void SetSubdiv(VoxelGI.SubdivEnum subdiv)
```

引擎名：`set_subdiv`

## SubdivEnum

命名空间 `Godot`；enum

### 枚举值

- `Subdiv64 = 0`：Use 64 subdivisions.
- `Subdiv128 = 1`：Use 128 subdivisions.
- `Subdiv256 = 2`：Use 256 subdivisions.
- `Subdiv512 = 3`：Use 512 subdivisions.
- `Max = 4`：Represents the size of the `VoxelGI.SubdivEnum` enum.
