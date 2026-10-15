<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# VoxelGIData

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`VoxelGIData` contains baked voxel global illumination for use in a [`VoxelGI`](VoxelGI.md) node. `VoxelGIData` also offers several properties to adjust the final appearance of the global illumination. These properties can be adjusted at run-time without having to bake the [`VoxelGI`](VoxelGI.md) node again.

**Note:** To prevent text-based scene files (`.tscn`) from growing too much and becoming slow to load and save, always save `VoxelGIData` to an external binary resource file (`.res`) instead of embedding it within the scene. This can be done by clicking the dropdown arrow next to the `VoxelGIData` resource, choosing **Edit**, clicking the floppy disk icon at the top of the Inspector then choosing **Save As...**.

## 构造函数

### VoxelGIData

```csharp
public VoxelGIData()
```

## 属性

### Bias

```csharp
public float Bias { get; set; }
```

引擎名：`bias`

The normal bias to use for indirect lighting and reflections. Higher values reduce self-reflections visible in non-rough materials, at the cost of more visible light leaking and flatter-looking indirect lighting. To prioritize hiding self-reflections over lighting quality, set `Bias` to `0.0` and `NormalBias` to a value between `1.0` and `2.0`.

### DynamicRange

```csharp
public float DynamicRange { get; set; }
```

引擎名：`dynamic_range`

The dynamic range to use (`1.0` represents a low dynamic range scene brightness). Higher values can be used to provide brighter indirect lighting, at the cost of more visible color banding in dark areas (both in indirect lighting and reflections). To avoid color banding, it's recommended to use the lowest value that does not result in visible light clipping.

### Energy

```csharp
public float Energy { get; set; }
```

引擎名：`energy`

The energy of the indirect lighting and reflections produced by the [`VoxelGI`](VoxelGI.md) node. Higher values result in brighter indirect lighting. If indirect lighting looks too flat, try decreasing `Propagation` while increasing `Energy` at the same time. See also `UseTwoBounces` which influences the indirect lighting's effective brightness.

### Interior

```csharp
public bool Interior { get; set; }
```

引擎名：`interior`

If `true`, [`Environment`](Environment.md) lighting is ignored by the [`VoxelGI`](VoxelGI.md) node. If `false`, [`Environment`](Environment.md) lighting is taken into account by the [`VoxelGI`](VoxelGI.md) node. [`Environment`](Environment.md) lighting updates in real-time, which means it can be changed without having to bake the [`VoxelGI`](VoxelGI.md) node again.

### NormalBias

```csharp
public float NormalBias { get; set; }
```

引擎名：`normal_bias`

The normal bias to use for indirect lighting and reflections. Higher values reduce self-reflections visible in non-rough materials, at the cost of more visible light leaking and flatter-looking indirect lighting. See also `Bias`. To prioritize hiding self-reflections over lighting quality, set `Bias` to `0.0` and `NormalBias` to a value between `1.0` and `2.0`.

### Propagation

```csharp
public float Propagation { get; set; }
```

引擎名：`propagation`

The multiplier to use when light bounces off a surface. Higher values result in brighter indirect lighting. If indirect lighting looks too flat, try decreasing `Propagation` while increasing `Energy` at the same time. See also `UseTwoBounces` which influences the indirect lighting's effective brightness.

### UseTwoBounces

```csharp
public bool UseTwoBounces { get; set; }
```

引擎名：`use_two_bounces`

If `true`, performs two bounces of indirect lighting instead of one. This makes indirect lighting look more natural and brighter at a small performance cost. The second bounce is also visible in reflections. If the scene appears too bright after enabling `UseTwoBounces`, adjust `Propagation` and `Energy`.

### _Data

```csharp
public Dictionary _Data { get; set; }
```

## 方法

### Allocate

```csharp
public void Allocate(Transform3D toCellXform, Aabb aabb, Vector3 octreeSize, ReadOnlySpan<byte> octreeCells, ReadOnlySpan<byte> dataCells, ReadOnlySpan<byte> distanceField, ReadOnlySpan<int> levelCounts)
```

引擎名：`allocate`

Initializes this `VoxelGIData` with the specified data. `octreeCells` must be a multiple of 32. `octreeCells` must be double the size of `dataCells`. The allocated data can be retrieved later using the various getter methods.

### Allocate

```csharp
public void Allocate(Transform3D toCellXform, Aabb aabb, Vector3 octreeSize, byte[] octreeCells, byte[] dataCells, byte[] distanceField, int[] levelCounts)
```

引擎名：`allocate`

Initializes this `VoxelGIData` with the specified data. `octreeCells` must be a multiple of 32. `octreeCells` must be double the size of `dataCells`. The allocated data can be retrieved later using the various getter methods.

### GetBias

```csharp
public float GetBias()
```

引擎名：`get_bias`

### GetBounds

```csharp
public Aabb GetBounds()
```

引擎名：`get_bounds`

Returns the bounds of the baked voxel data as an [`Aabb`](Aabb.md), which should match [`VoxelGI.Size`](VoxelGI.md) after being baked (which only contains the size as a [`Vector3`](Vector3.md)).

**Note:** If the size was modified without baking the VoxelGI data, then the value of `GetBounds()` and [`VoxelGI.Size`](VoxelGI.md) will not match.

### GetDataCells

```csharp
public byte[] GetDataCells()
```

引擎名：`get_data_cells`

Returns the baked cell data for this `VoxelGIData`.

### GetDynamicRange

```csharp
public float GetDynamicRange()
```

引擎名：`get_dynamic_range`

### GetEnergy

```csharp
public float GetEnergy()
```

引擎名：`get_energy`

### GetLevelCounts

```csharp
public int[] GetLevelCounts()
```

引擎名：`get_level_counts`

Returns the baked level counts for this `VoxelGIData`.

### GetNormalBias

```csharp
public float GetNormalBias()
```

引擎名：`get_normal_bias`

### GetOctreeCells

```csharp
public byte[] GetOctreeCells()
```

引擎名：`get_octree_cells`

Returns the baked octree cell data for this `VoxelGIData`.

### GetOctreeSize

```csharp
public Vector3 GetOctreeSize()
```

引擎名：`get_octree_size`

Returns the baked octree size for this `VoxelGIData`, which corresponds to the number of subdivisions per axis. This can be viewed in the editor by hovering the **Bake VoxelGI** button at the top of the 3D editor viewport when a [`VoxelGI`](VoxelGI.md) node is selected and looking at the **Subdivisions** field in the tooltip.

### GetPropagation

```csharp
public float GetPropagation()
```

引擎名：`get_propagation`

### GetToCellXform

```csharp
public Transform3D GetToCellXform()
```

引擎名：`get_to_cell_xform`

Returns the baked cell transform for this `VoxelGIData`.

### IsInterior

```csharp
public bool IsInterior()
```

引擎名：`is_interior`

### IsUsingTwoBounces

```csharp
public bool IsUsingTwoBounces()
```

引擎名：`is_using_two_bounces`

### SetBias

```csharp
public void SetBias(float bias)
```

引擎名：`set_bias`

### SetDynamicRange

```csharp
public void SetDynamicRange(float dynamicRange)
```

引擎名：`set_dynamic_range`

### SetEnergy

```csharp
public void SetEnergy(float energy)
```

引擎名：`set_energy`

### SetInterior

```csharp
public void SetInterior(bool interior)
```

引擎名：`set_interior`

### SetNormalBias

```csharp
public void SetNormalBias(float bias)
```

引擎名：`set_normal_bias`

### SetPropagation

```csharp
public void SetPropagation(float propagation)
```

引擎名：`set_propagation`

### SetUseTwoBounces

```csharp
public void SetUseTwoBounces(bool enable)
```

引擎名：`set_use_two_bounces`
