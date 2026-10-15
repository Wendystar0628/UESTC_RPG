<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VisualInstance3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `VisualInstance3D` is used to connect a resource to a visual representation. All visual 3D nodes inherit from the `VisualInstance3D`. In general, you should not access the `VisualInstance3D` properties directly as they are accessed and managed by the nodes that inherit from `VisualInstance3D`. `VisualInstance3D` is the node representation of the [`RenderingServer`](RenderingServer.md) instance.

## 构造函数

### VisualInstance3D

```csharp
public VisualInstance3D()
```

## 属性

### Layers

```csharp
public uint Layers { get; set; }
```

引擎名：`layers`

The render layer(s) this `VisualInstance3D` is drawn on.

This object will only be visible for [`Camera3D`](Camera3D.md)s whose cull mask includes any of the render layers this `VisualInstance3D` is set to.

For [`Light3D`](Light3D.md)s, this can be used to control which `VisualInstance3D`s are affected by a specific light. For [`GpuParticles3D`](GpuParticles3D.md), this can be used to control which particles are effected by a specific attractor. For [`Decal`](Decal.md)s, this can be used to control which `VisualInstance3D`s are affected by a specific decal.

To adjust `Layers` more easily using a script, use `GetLayerMaskValue()` and `SetLayerMaskValue()`.

**Note:** [`VoxelGI`](VoxelGI.md), SDFGI and [`LightmapGI`](LightmapGI.md) will always take all layers into account to determine what contributes to global illumination. If this is an issue, set [`GeometryInstance3D.GIMode`](GeometryInstance3D.md) to [`GeometryInstance3D.GIModeEnum.Disabled`](GeometryInstance3D.md) for meshes and [`Light3D.LightBakeMode`](Light3D.md) to [`Light3D.BakeMode.Disabled`](Light3D.md) for lights to exclude them from global illumination.

### SortingOffset

```csharp
public float SortingOffset { get; set; }
```

引擎名：`sorting_offset`

The amount by which the depth of this `VisualInstance3D` will be adjusted when sorting by depth. Uses the same units as the engine (which are typically meters). Adjusting it to a higher value will make the `VisualInstance3D` reliably draw on top of other `VisualInstance3D`s that are otherwise positioned at the same spot. To ensure it always draws on top of other objects around it (not positioned at the same spot), set the value to be greater than the distance between this `VisualInstance3D` and the other nearby `VisualInstance3D`s.

### SortingUseAabbCenter

```csharp
public bool SortingUseAabbCenter { get; set; }
```

引擎名：`sorting_use_aabb_center`

If `true`, the object is sorted based on the [`Aabb`](Aabb.md) center. The object will be sorted based on the global position otherwise.

The [`Aabb`](Aabb.md) center based sorting is generally more accurate for 3D models. The position based sorting instead allows to better control the drawing order when working with [`GpuParticles3D`](GpuParticles3D.md) and [`CpuParticles3D`](CpuParticles3D.md).

## 方法

### GetAabb

```csharp
public Aabb GetAabb()
```

引擎名：`get_aabb`

Returns the [`Aabb`](Aabb.md) (also known as the bounding box) for this `VisualInstance3D`.

### GetBase

```csharp
public Rid GetBase()
```

引擎名：`get_base`

Returns the RID of the resource associated with this `VisualInstance3D`. For example, if the Node is a [`MeshInstance3D`](MeshInstance3D.md), this will return the RID of the associated [`Mesh`](Mesh.md).

### GetInstance

```csharp
public Rid GetInstance()
```

引擎名：`get_instance`

Returns the RID of this instance. This RID is the same as the RID returned by [`RenderingServer.InstanceCreate`](RenderingServer.md). This RID is needed if you want to call [`RenderingServer`](RenderingServer.md) functions directly on this `VisualInstance3D`.

### GetLayerMask

```csharp
public uint GetLayerMask()
```

引擎名：`get_layer_mask`

### GetLayerMaskValue

```csharp
public bool GetLayerMaskValue(int layerNumber)
```

引擎名：`get_layer_mask_value`

Returns whether or not the specified layer of the `Layers` is enabled, given a `layerNumber` between 1 and 20.

### GetSortingOffset

```csharp
public float GetSortingOffset()
```

引擎名：`get_sorting_offset`

### IsSortingUseAabbCenter

```csharp
public bool IsSortingUseAabbCenter()
```

引擎名：`is_sorting_use_aabb_center`

### SetBase

```csharp
public void SetBase(Rid base)
```

引擎名：`set_base`

Sets the resource that is instantiated by this `VisualInstance3D`, which changes how the engine handles the `VisualInstance3D` under the hood. Equivalent to [`RenderingServer.InstanceSetBase`](RenderingServer.md).

### SetLayerMask

```csharp
public void SetLayerMask(uint mask)
```

引擎名：`set_layer_mask`

### SetLayerMaskValue

```csharp
public void SetLayerMaskValue(int layerNumber, bool value)
```

引擎名：`set_layer_mask_value`

Based on `value`, enables or disables the specified layer in the `Layers`, given a `layerNumber` between 1 and 20.

### SetSortingOffset

```csharp
public void SetSortingOffset(float offset)
```

引擎名：`set_sorting_offset`

### SetSortingUseAabbCenter

```csharp
public void SetSortingUseAabbCenter(bool enabled)
```

引擎名：`set_sorting_use_aabb_center`

### _GetAabb

```csharp
public virtual Aabb _GetAabb()
```

引擎名：`_get_aabb`
