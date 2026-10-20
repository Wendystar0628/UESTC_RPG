<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRPlaneTracker

命名空间 `Godot`；class；继承 [`OpenXRSpatialEntityTracker`](OpenXRSpatialEntityTracker.md) → [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Spatial entity tracker for our OpenXR spatial entity plane tracking extension. These trackers identify entities in our real space such as walls, floors, tables, etc. and map their location to our virtual space.

## 构造函数

### OpenXRPlaneTracker

```csharp
public OpenXRPlaneTracker()
```

## 属性

### BoundsSize

```csharp
public Vector2 BoundsSize { get; set; }
```

引擎名：`bounds_size`

The bounding size of the plane. This is a 2D size.

### PlaneAlignment

```csharp
public OpenXRSpatialComponentPlaneAlignmentList.PlaneAlignment PlaneAlignment { get; set; }
```

引擎名：`plane_alignment`

The main alignment in space of this plane.

### PlaneLabel

```csharp
public string PlaneLabel { get; set; }
```

引擎名：`plane_label`

The semantic label for this plane.

## 方法

### ClearMeshData

```csharp
public void ClearMeshData()
```

引擎名：`clear_mesh_data`

Clears the mesh data for this tracker. You should only call this if you are handling your own discovery logic.

### GetBoundsSize

```csharp
public Vector2 GetBoundsSize()
```

引擎名：`get_bounds_size`

### GetMesh

```csharp
public Mesh GetMesh()
```

引擎名：`get_mesh`

Gets a mesh created from either the mesh data or from our bounding size for this plane.

### GetMeshOffset

```csharp
public Transform3D GetMeshOffset()
```

引擎名：`get_mesh_offset`

Gets the transform by which to offset the mesh and collision shape from our pose to display these correctly.

### GetPlaneAlignment

```csharp
public OpenXRSpatialComponentPlaneAlignmentList.PlaneAlignment GetPlaneAlignment()
```

引擎名：`get_plane_alignment`

### GetPlaneLabel

```csharp
public string GetPlaneLabel()
```

引擎名：`get_plane_label`

### GetShape

```csharp
public Shape3D GetShape(float thickness = 0.01f)
```

引擎名：`get_shape`

Gets a collision shape built either from the mesh data or from our bounding size for this plane.

### SetBoundsSize

```csharp
public void SetBoundsSize(Vector2 boundsSize)
```

引擎名：`set_bounds_size`

### SetMeshData

```csharp
public void SetMeshData(Transform3D origin, ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> indices)
```

引擎名：`set_mesh_data`

Sets the mesh data for this plane. You should only call this if you are handling your own discovery logic.

### SetMeshData

```csharp
public void SetMeshData(Transform3D origin, Vector2[] vertices, int[] indices = null)
```

引擎名：`set_mesh_data`

Sets the mesh data for this plane. You should only call this if you are handling your own discovery logic.

参数：
- `indices`：If the parameter is null, then the default value is `Array.Empty<int>()`.

### SetPlaneAlignment

```csharp
public void SetPlaneAlignment(OpenXRSpatialComponentPlaneAlignmentList.PlaneAlignment planeAlignment)
```

引擎名：`set_plane_alignment`

### SetPlaneLabel

```csharp
public void SetPlaneLabel(string planeLabel)
```

引擎名：`set_plane_label`

## 事件（信号）

### MeshChanged

```csharp
public event Action MeshChanged
```

引擎名：`mesh_changed`

Emitted when our mesh data has changed the mesh instance and collision needs to be updated.
