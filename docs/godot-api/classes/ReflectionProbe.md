<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ReflectionProbe

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Captures its surroundings as a cubemap, and stores versions of it with increasing levels of blur to simulate different material roughnesses.

The `ReflectionProbe` is used to create high-quality reflections at a low performance cost (when `UpdateMode` is [`ReflectionProbe.UpdateModeEnum.Once`](ReflectionProbe.md)). `ReflectionProbe`s can be blended together and with the rest of the scene smoothly. `ReflectionProbe`s can also be combined with [`VoxelGI`](VoxelGI.md), SDFGI ([`Environment.SdfgiEnabled`](Environment.md)) and screen-space reflections ([`Environment.SsrEnabled`](Environment.md)) to get more accurate reflections in specific areas. `ReflectionProbe`s render all objects within their `CullMask`, so updating them can be quite expensive. It is best to update them once with the important static objects and then leave them as-is.

**Note:** Unlike [`VoxelGI`](VoxelGI.md) and SDFGI, `ReflectionProbe`s only source their environment from a [`WorldEnvironment`](WorldEnvironment.md) node. If you specify an [`Environment`](Environment.md) resource within a [`Camera3D`](Camera3D.md) node, it will be ignored by the `ReflectionProbe`. This can lead to incorrect lighting within the `ReflectionProbe`.

**Note:** When using the Mobile rendering method, only `8` reflection probes can be displayed on each mesh resource, while the Compatibility rendering method only supports up to `2` reflection probes on each mesh. Attempting to display more than `8` reflection probes on a single mesh resource using the Mobile renderer will result in reflection probes flickering in and out as the camera moves, while the Compatibility renderer will not render any additional probes if more than `2` reflection probes are being used.

**Note:** When using the Mobile rendering method, reflection probes will only correctly affect meshes whose visibility AABB intersects with the reflection probe's AABB. If using a shader to deform the mesh in a way that makes it go outside its AABB, [`GeometryInstance3D.ExtraCullMargin`](GeometryInstance3D.md) must be increased on the mesh. Otherwise, the reflection probe may not be visible on the mesh.

## 构造函数

### ReflectionProbe

```csharp
public ReflectionProbe()
```

## 属性

### AmbientColor

```csharp
public Color AmbientColor { get; set; }
```

引擎名：`ambient_color`

The custom ambient color to use within the `ReflectionProbe`'s box defined by its `Size`. Only effective if `AmbientMode` is [`ReflectionProbe.AmbientModeEnum.Color`](ReflectionProbe.md).

### AmbientColorEnergy

```csharp
public float AmbientColorEnergy { get; set; }
```

引擎名：`ambient_color_energy`

The custom ambient color energy to use within the `ReflectionProbe`'s box defined by its `Size`. Only effective if `AmbientMode` is [`ReflectionProbe.AmbientModeEnum.Color`](ReflectionProbe.md).

### AmbientMode

```csharp
public ReflectionProbe.AmbientModeEnum AmbientMode { get; set; }
```

引擎名：`ambient_mode`

The ambient color to use within the `ReflectionProbe`'s box defined by its `Size`. The ambient color will smoothly blend with other `ReflectionProbe`s and the rest of the scene (outside the `ReflectionProbe`'s box defined by its `Size`).

### BlendDistance

```csharp
public float BlendDistance { get; set; }
```

引擎名：`blend_distance`

Defines the distance in meters over which a probe blends into the scene.

### BoxProjection

```csharp
public bool BoxProjection { get; set; }
```

引擎名：`box_projection`

If `true`, enables box projection. This makes reflections look more correct in rectangle-shaped rooms by offsetting the reflection center depending on the camera's location.

**Note:** To better fit rectangle-shaped rooms that are not aligned to the grid, you can rotate the `ReflectionProbe` node.

### CullMask

```csharp
public uint CullMask { get; set; }
```

引擎名：`cull_mask`

Sets the cull mask which determines what objects are drawn by this probe. Every [`VisualInstance3D`](VisualInstance3D.md) with a layer included in this cull mask will be rendered by the probe. It is best to only include large objects which are likely to take up a lot of space in the reflection in order to save on rendering cost.

This can also be used to prevent an object from reflecting upon itself (for instance, a `ReflectionProbe` centered on a vehicle).

### EnableShadows

```csharp
public bool EnableShadows { get; set; }
```

引擎名：`enable_shadows`

If `true`, computes shadows in the reflection probe. This makes the reflection probe slower to render; you may want to disable this if using the [`ReflectionProbe.UpdateModeEnum.Always`](ReflectionProbe.md) `UpdateMode`.

### Intensity

```csharp
public float Intensity { get; set; }
```

引擎名：`intensity`

Defines the reflection intensity. Intensity modulates the strength of the reflection.

### Interior

```csharp
public bool Interior { get; set; }
```

引擎名：`interior`

If `true`, reflections will ignore sky contribution.

### MaxDistance

```csharp
public float MaxDistance { get; set; }
```

引擎名：`max_distance`

The maximum distance away from the `ReflectionProbe` an object can be before it is culled. Decrease this to improve performance, especially when using the [`ReflectionProbe.UpdateModeEnum.Always`](ReflectionProbe.md) `UpdateMode`.

**Note:** The maximum reflection distance is always at least equal to the probe's extents. This means that decreasing `MaxDistance` will not always cull objects from reflections, especially if the reflection probe's box defined by its `Size` is already large.

### MeshLodThreshold

```csharp
public float MeshLodThreshold { get; set; }
```

引擎名：`mesh_lod_threshold`

The automatic LOD bias to use for meshes rendered within the `ReflectionProbe` (this is analog to [`Viewport.MeshLodThreshold`](Viewport.md)). Higher values will use less detailed versions of meshes that have LOD variations generated. If set to `0.0`, automatic LOD is disabled. Increase `MeshLodThreshold` to improve performance at the cost of geometry detail, especially when using the [`ReflectionProbe.UpdateModeEnum.Always`](ReflectionProbe.md) `UpdateMode`.

**Note:** `MeshLodThreshold` does not affect [`GeometryInstance3D`](GeometryInstance3D.md) visibility ranges (also known as "manual" LOD or hierarchical LOD).

### OriginOffset

```csharp
public Vector3 OriginOffset { get; set; }
```

引擎名：`origin_offset`

Sets the origin offset to be used when this `ReflectionProbe` is in `BoxProjection` mode. This can be set to a non-zero value to ensure a reflection fits a rectangle-shaped room, while reducing the number of objects that "get in the way" of the reflection.

### ReflectionMask

```csharp
public uint ReflectionMask { get; set; }
```

引擎名：`reflection_mask`

Sets the reflection mask which determines what objects have reflections applied from this probe. Every [`VisualInstance3D`](VisualInstance3D.md) with a layer included in this reflection mask will have reflections applied from this probe. See also `CullMask`, which can be used to exclude objects from appearing in the reflection while still making them affected by the `ReflectionProbe`.

### Size

```csharp
public Vector3 Size { get; set; }
```

引擎名：`size`

The size of the reflection probe. The larger the size, the more space covered by the probe, which will lower the perceived resolution. It is best to keep the size only as large as you need it.

**Note:** To better fit areas that are not aligned to the grid, you can rotate the `ReflectionProbe` node.

### UpdateMode

```csharp
public ReflectionProbe.UpdateModeEnum UpdateMode { get; set; }
```

引擎名：`update_mode`

Sets how frequently the `ReflectionProbe` is updated. Can be [`ReflectionProbe.UpdateModeEnum.Once`](ReflectionProbe.md) or [`ReflectionProbe.UpdateModeEnum.Always`](ReflectionProbe.md).

## 方法

### AreShadowsEnabled

```csharp
public bool AreShadowsEnabled()
```

引擎名：`are_shadows_enabled`

### GetAmbientColor

```csharp
public Color GetAmbientColor()
```

引擎名：`get_ambient_color`

### GetAmbientColorEnergy

```csharp
public float GetAmbientColorEnergy()
```

引擎名：`get_ambient_color_energy`

### GetAmbientMode

```csharp
public ReflectionProbe.AmbientModeEnum GetAmbientMode()
```

引擎名：`get_ambient_mode`

### GetBlendDistance

```csharp
public float GetBlendDistance()
```

引擎名：`get_blend_distance`

### GetCullMask

```csharp
public uint GetCullMask()
```

引擎名：`get_cull_mask`

### GetIntensity

```csharp
public float GetIntensity()
```

引擎名：`get_intensity`

### GetMaxDistance

```csharp
public float GetMaxDistance()
```

引擎名：`get_max_distance`

### GetMeshLodThreshold

```csharp
public float GetMeshLodThreshold()
```

引擎名：`get_mesh_lod_threshold`

### GetOriginOffset

```csharp
public Vector3 GetOriginOffset()
```

引擎名：`get_origin_offset`

### GetReflectionMask

```csharp
public uint GetReflectionMask()
```

引擎名：`get_reflection_mask`

### GetSize

```csharp
public Vector3 GetSize()
```

引擎名：`get_size`

### GetUpdateMode

```csharp
public ReflectionProbe.UpdateModeEnum GetUpdateMode()
```

引擎名：`get_update_mode`

### IsBoxProjectionEnabled

```csharp
public bool IsBoxProjectionEnabled()
```

引擎名：`is_box_projection_enabled`

### IsSetAsInterior

```csharp
public bool IsSetAsInterior()
```

引擎名：`is_set_as_interior`

### SetAmbientColor

```csharp
public void SetAmbientColor(Color ambient)
```

引擎名：`set_ambient_color`

### SetAmbientColorEnergy

```csharp
public void SetAmbientColorEnergy(float ambientEnergy)
```

引擎名：`set_ambient_color_energy`

### SetAmbientMode

```csharp
public void SetAmbientMode(ReflectionProbe.AmbientModeEnum ambient)
```

引擎名：`set_ambient_mode`

### SetAsInterior

```csharp
public void SetAsInterior(bool enable)
```

引擎名：`set_as_interior`

### SetBlendDistance

```csharp
public void SetBlendDistance(float blendDistance)
```

引擎名：`set_blend_distance`

### SetCullMask

```csharp
public void SetCullMask(uint layers)
```

引擎名：`set_cull_mask`

### SetEnableBoxProjection

```csharp
public void SetEnableBoxProjection(bool enable)
```

引擎名：`set_enable_box_projection`

### SetEnableShadows

```csharp
public void SetEnableShadows(bool enable)
```

引擎名：`set_enable_shadows`

### SetIntensity

```csharp
public void SetIntensity(float intensity)
```

引擎名：`set_intensity`

### SetMaxDistance

```csharp
public void SetMaxDistance(float maxDistance)
```

引擎名：`set_max_distance`

### SetMeshLodThreshold

```csharp
public void SetMeshLodThreshold(float ratio)
```

引擎名：`set_mesh_lod_threshold`

### SetOriginOffset

```csharp
public void SetOriginOffset(Vector3 originOffset)
```

引擎名：`set_origin_offset`

### SetReflectionMask

```csharp
public void SetReflectionMask(uint layers)
```

引擎名：`set_reflection_mask`

### SetSize

```csharp
public void SetSize(Vector3 size)
```

引擎名：`set_size`

### SetUpdateMode

```csharp
public void SetUpdateMode(ReflectionProbe.UpdateModeEnum mode)
```

引擎名：`set_update_mode`

## AmbientModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Do not apply any ambient lighting inside the [`ReflectionProbe`](ReflectionProbe.md)'s box defined by its [`ReflectionProbe.Size`](ReflectionProbe.md).
- `Environment = 1`：Apply automatically-sourced environment lighting inside the [`ReflectionProbe`](ReflectionProbe.md)'s box defined by its [`ReflectionProbe.Size`](ReflectionProbe.md).
- `Color = 2`：Apply custom ambient lighting inside the [`ReflectionProbe`](ReflectionProbe.md)'s box defined by its [`ReflectionProbe.Size`](ReflectionProbe.md).

## UpdateModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Once = 0`：Update the probe once on the next frame (recommended for most objects).
- `Always = 1`：Update the probe every frame.
