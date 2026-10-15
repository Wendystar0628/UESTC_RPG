<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MeshInstance3D

命名空间 `Godot`；class；继承 [`GeometryInstance3D`](GeometryInstance3D.md) → [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

MeshInstance3D is a node that takes a [`Mesh`](Mesh.md) resource and adds it to the current scenario by creating an instance of it. This is the class most often used to render 3D geometry and can be used to instance a single [`Mesh`](Mesh.md) in many places. This allows reusing geometry, which can save on resources. When a [`Mesh`](Mesh.md) has to be instantiated more than thousands of times at close proximity, consider using a [`MultiMesh`](MultiMesh.md) in a [`MultiMeshInstance3D`](MultiMeshInstance3D.md) instead.

## 构造函数

### MeshInstance3D

```csharp
public MeshInstance3D()
```

## 属性

### Mesh

```csharp
public Mesh Mesh { get; set; }
```

引擎名：`mesh`

The [`Mesh`](Mesh.md) resource for the instance.

### Skeleton

```csharp
public NodePath Skeleton { get; set; }
```

引擎名：`skeleton`

[`NodePath`](NodePath.md) to the [`Skeleton3D`](Skeleton3D.md) associated with the instance.

**Note:** The default value of this property has changed in Godot 4.6. Enable `ProjectSettings.animation/compatibility/default_parent_skeleton_in_mesh_instance_3d` if the old behavior is needed for compatibility.

### Skin

```csharp
public Skin Skin { get; set; }
```

引擎名：`skin`

The [`Skin`](Skin.md) to be used by this instance.

## 方法

### BakeMeshFromCurrentBlendShapeMix

```csharp
public ArrayMesh BakeMeshFromCurrentBlendShapeMix(ArrayMesh existing = null)
```

引擎名：`bake_mesh_from_current_blend_shape_mix`

Takes a snapshot from the current [`ArrayMesh`](ArrayMesh.md) with all blend shapes applied according to their current weights and bakes it to the provided `existing` mesh. If no `existing` mesh is provided a new [`ArrayMesh`](ArrayMesh.md) is created, baked and returned. Mesh surface materials are not copied.

**Performance:** [`Mesh`](Mesh.md) data needs to be received from the GPU, stalling the [`RenderingServer`](RenderingServer.md) in the process.

### BakeMeshFromCurrentSkeletonPose

```csharp
public ArrayMesh BakeMeshFromCurrentSkeletonPose(ArrayMesh existing = null)
```

引擎名：`bake_mesh_from_current_skeleton_pose`

Takes a snapshot of the current animated skeleton pose of the skinned mesh and bakes it to the provided `existing` mesh. If no `existing` mesh is provided a new [`ArrayMesh`](ArrayMesh.md) is created, baked, and returned. Requires a skeleton with a registered skin to work. Blendshapes are ignored. Mesh surface materials are not copied.

**Performance:** [`Mesh`](Mesh.md) data needs to be retrieved from the GPU, stalling the [`RenderingServer`](RenderingServer.md) in the process.

### CreateConvexCollision

```csharp
public void CreateConvexCollision(bool clean = true, bool simplify = false)
```

引擎名：`create_convex_collision`

This helper creates a [`StaticBody3D`](StaticBody3D.md) child node with a [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) collision shape calculated from the mesh geometry. It's mainly used for testing.

If `clean` is `true` (default), duplicate and interior vertices are removed automatically. You can set it to `false` to make the process faster if not needed.

If `simplify` is `true`, the geometry can be further simplified to reduce the number of vertices. Disabled by default.

### CreateDebugTangents

```csharp
public void CreateDebugTangents()
```

引擎名：`create_debug_tangents`

This helper creates a `MeshInstance3D` child node with gizmos at every vertex calculated from the mesh geometry. It's mainly used for testing.

### CreateMultipleConvexCollisions

```csharp
public void CreateMultipleConvexCollisions()
```

引擎名：`create_multiple_convex_collisions`

This helper creates a [`StaticBody3D`](StaticBody3D.md) child node with multiple [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) collision shapes calculated from the mesh geometry via convex decomposition. The convex decomposition operation can be controlled with parameters from the optional `settings`.

### CreateMultipleConvexCollisions

```csharp
public void CreateMultipleConvexCollisions(MeshConvexDecompositionSettings settings = null)
```

引擎名：`create_multiple_convex_collisions`

This helper creates a [`StaticBody3D`](StaticBody3D.md) child node with multiple [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) collision shapes calculated from the mesh geometry via convex decomposition. The convex decomposition operation can be controlled with parameters from the optional `settings`.

### CreateTrimeshCollision

```csharp
public void CreateTrimeshCollision()
```

引擎名：`create_trimesh_collision`

This helper creates a [`StaticBody3D`](StaticBody3D.md) child node with a [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) collision shape calculated from the mesh geometry. It's mainly used for testing.

### FindBlendShapeByName

```csharp
public int FindBlendShapeByName(StringName name)
```

引擎名：`find_blend_shape_by_name`

Returns the index of the blend shape with the given `name`. Returns `-1` if no blend shape with this name exists, including when `Mesh` is `null`.

### GetActiveMaterial

```csharp
public Material GetActiveMaterial(int surface)
```

引擎名：`get_active_material`

Returns the [`Material`](Material.md) that will be used by the [`Mesh`](Mesh.md) when drawing. This can return the [`GeometryInstance3D.MaterialOverride`](GeometryInstance3D.md), the surface override [`Material`](Material.md) defined in this `MeshInstance3D`, or the surface [`Material`](Material.md) defined in the `Mesh`. For example, if [`GeometryInstance3D.MaterialOverride`](GeometryInstance3D.md) is used, all surfaces will return the override material.

Returns `null` if no material is active, including when `Mesh` is `null`.

### GetBlendShapeCount

```csharp
public int GetBlendShapeCount()
```

引擎名：`get_blend_shape_count`

Returns the number of blend shapes available. Produces an error if `Mesh` is `null`.

### GetBlendShapeValue

```csharp
public float GetBlendShapeValue(int blendShapeIdx)
```

引擎名：`get_blend_shape_value`

Returns the value of the blend shape at the given `blendShapeIdx`. Returns `0.0` and produces an error if `Mesh` is `null` or doesn't have a blend shape at that index.

### GetMesh

```csharp
public Mesh GetMesh()
```

引擎名：`get_mesh`

### GetSkeletonPath

```csharp
public NodePath GetSkeletonPath()
```

引擎名：`get_skeleton_path`

### GetSkin

```csharp
public Skin GetSkin()
```

引擎名：`get_skin`

### GetSkinReference

```csharp
public SkinReference GetSkinReference()
```

引擎名：`get_skin_reference`

Returns the internal [`SkinReference`](SkinReference.md) containing the skeleton's [`Rid`](Rid.md) attached to this RID. See also [`Resource.GetRid`](Resource.md), [`SkinReference.GetSkeleton`](SkinReference.md), and [`RenderingServer.InstanceAttachSkeleton`](RenderingServer.md).

### GetSurfaceOverrideMaterial

```csharp
public Material GetSurfaceOverrideMaterial(int surface)
```

引擎名：`get_surface_override_material`

Returns the override [`Material`](Material.md) for the specified `surface` of the [`Mesh`](Mesh.md) resource. See also `GetSurfaceOverrideMaterialCount()`.

**Note:** This returns the [`Material`](Material.md) associated to the `MeshInstance3D`'s Surface Material Override properties, not the material within the [`Mesh`](Mesh.md) resource. To get the material within the [`Mesh`](Mesh.md) resource, use [`Mesh.SurfaceGetMaterial`](Mesh.md) instead.

### GetSurfaceOverrideMaterialCount

```csharp
public int GetSurfaceOverrideMaterialCount()
```

引擎名：`get_surface_override_material_count`

Returns the number of surface override materials. This is equivalent to [`Mesh.GetSurfaceCount`](Mesh.md). See also `GetSurfaceOverrideMaterial()`.

### SetBlendShapeValue

```csharp
public void SetBlendShapeValue(int blendShapeIdx, float value)
```

引擎名：`set_blend_shape_value`

Sets the value of the blend shape at `blendShapeIdx` to `value`. Produces an error if `Mesh` is `null` or doesn't have a blend shape at that index.

### SetMesh

```csharp
public void SetMesh(Mesh mesh)
```

引擎名：`set_mesh`

### SetSkeletonPath

```csharp
public void SetSkeletonPath(NodePath skeletonPath)
```

引擎名：`set_skeleton_path`

### SetSkin

```csharp
public void SetSkin(Skin skin)
```

引擎名：`set_skin`

### SetSurfaceOverrideMaterial

```csharp
public void SetSurfaceOverrideMaterial(int surface, Material material)
```

引擎名：`set_surface_override_material`

Sets the override `material` for the specified `surface` of the [`Mesh`](Mesh.md) resource. This material is associated with this `MeshInstance3D` rather than with `Mesh`.

**Note:** This assigns the [`Material`](Material.md) associated to the `MeshInstance3D`'s Surface Material Override properties, not the material within the [`Mesh`](Mesh.md) resource. To set the material within the [`Mesh`](Mesh.md) resource, use [`Mesh.SurfaceSetMaterial`](Mesh.md) instead.
