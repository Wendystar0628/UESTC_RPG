<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OccluderInstance3D

命名空间 `Godot`；class；继承 [`VisualInstance3D`](VisualInstance3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Occlusion culling can improve rendering performance in closed/semi-open areas by hiding geometry that is occluded by other objects.

The occlusion culling system is mostly static. `OccluderInstance3D`s can be moved or hidden at run-time, but doing so will trigger a background recomputation that can take several frames. It is recommended to only move `OccluderInstance3D`s sporadically (e.g. for procedural generation purposes), rather than doing so every frame.

The occlusion culling system works by rendering the occluders on the CPU in parallel using [Embree](https://www.embree.org/), drawing the result to a low-resolution buffer then using this to cull 3D nodes individually. In the 3D editor, you can preview the occlusion culling buffer by choosing **Perspective > Display Advanced... > Occlusion Culling Buffer** in the top-left corner of the 3D viewport. The occlusion culling buffer quality can be adjusted in the Project Settings.

**Baking:** Select an `OccluderInstance3D` node, then use the **Bake Occluders** button at the top of the 3D editor. Only opaque materials will be taken into account; transparent materials (alpha-blended or alpha-tested) will be ignored by the occluder generation.

**Note:** Occlusion culling is only effective if `ProjectSettings.rendering/occlusion_culling/use_occlusion_culling` is `true`. Enabling occlusion culling has a cost on the CPU. Only enable occlusion culling if you actually plan to use it. Large open scenes with few or no objects blocking the view will generally not benefit much from occlusion culling. Large open scenes generally benefit more from mesh LOD and visibility ranges ([`GeometryInstance3D.VisibilityRangeBegin`](GeometryInstance3D.md) and [`GeometryInstance3D.VisibilityRangeEnd`](GeometryInstance3D.md)) compared to occlusion culling.

**Note:** Due to memory constraints, occlusion culling is not supported by default in Web export templates. It can be enabled by compiling custom Web export templates with `module_raycast_enabled=yes`.

## 构造函数

### OccluderInstance3D

```csharp
public OccluderInstance3D()
```

## 属性

### BakeMask

```csharp
public uint BakeMask { get; set; }
```

引擎名：`bake_mask`

The visual layers to account for when baking for occluders. Only [`MeshInstance3D`](MeshInstance3D.md)s whose [`VisualInstance3D.Layers`](VisualInstance3D.md) match with this `BakeMask` will be included in the generated occluder mesh. By default, all objects with *opaque* materials are taken into account for the occluder baking.

To improve performance and avoid artifacts, it is recommended to exclude dynamic objects, small objects and fixtures from the baking process by moving them to a separate visual layer and excluding this layer in `BakeMask`.

### BakeSimplificationDistance

```csharp
public float BakeSimplificationDistance { get; set; }
```

引擎名：`bake_simplification_distance`

The simplification distance to use for simplifying the generated occluder polygon (in 3D units). Higher values result in a less detailed occluder mesh, which improves performance but reduces culling accuracy.

The occluder geometry is rendered on the CPU, so it is important to keep its geometry as simple as possible. Since the buffer is rendered at a low resolution, less detailed occluder meshes generally still work well. The default value is fairly aggressive, so you may have to decrease it if you run into false negatives (objects being occluded even though they are visible by the camera). A value of `0.01` will act conservatively, and will keep geometry *perceptually* unaffected in the occlusion culling buffer. Depending on the scene, a value of `0.01` may still simplify the mesh noticeably compared to disabling simplification entirely.

Setting this to `0.0` disables simplification entirely, but vertices in the exact same position will still be merged. The mesh will also be re-indexed to reduce both the number of vertices and indices.

**Note:** This uses the [meshoptimizer](https://meshoptimizer.org/) library under the hood, similar to LOD generation.

### Occluder

```csharp
public Occluder3D Occluder { get; set; }
```

引擎名：`occluder`

The occluder resource for this `OccluderInstance3D`. You can generate an occluder resource by selecting an `OccluderInstance3D` node then using the **Bake Occluders** button at the top of the editor.

You can also draw your own 2D occluder polygon by adding a new [`PolygonOccluder3D`](PolygonOccluder3D.md) resource to the `Occluder` property in the Inspector.

Alternatively, you can select a primitive occluder to use: [`QuadOccluder3D`](QuadOccluder3D.md), [`BoxOccluder3D`](BoxOccluder3D.md) or [`SphereOccluder3D`](SphereOccluder3D.md).

## 方法

### GetBakeMask

```csharp
public uint GetBakeMask()
```

引擎名：`get_bake_mask`

### GetBakeMaskValue

```csharp
public bool GetBakeMaskValue(int layerNumber)
```

引擎名：`get_bake_mask_value`

Returns whether or not the specified layer of the `BakeMask` is enabled, given a `layerNumber` between 1 and 32.

### GetBakeSimplificationDistance

```csharp
public float GetBakeSimplificationDistance()
```

引擎名：`get_bake_simplification_distance`

### GetOccluder

```csharp
public Occluder3D GetOccluder()
```

引擎名：`get_occluder`

### SetBakeMask

```csharp
public void SetBakeMask(uint mask)
```

引擎名：`set_bake_mask`

### SetBakeMaskValue

```csharp
public void SetBakeMaskValue(int layerNumber, bool value)
```

引擎名：`set_bake_mask_value`

Based on `value`, enables or disables the specified layer in the `BakeMask`, given a `layerNumber` between 1 and 32.

### SetBakeSimplificationDistance

```csharp
public void SetBakeSimplificationDistance(float simplificationDistance)
```

引擎名：`set_bake_simplification_distance`

### SetOccluder

```csharp
public void SetOccluder(Occluder3D occluder)
```

引擎名：`set_occluder`
