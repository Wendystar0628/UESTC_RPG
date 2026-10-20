<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GridMap

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

GridMap lets you place meshes on a grid interactively. It works both from the editor and from scripts, which can help you create in-game level editors.

GridMaps use a [`MeshLibrary`](MeshLibrary.md) which contains a list of tiles. Each tile is a mesh with materials plus optional collision and navigation shapes.

A GridMap contains a collection of cells. Each grid cell refers to a tile in the [`MeshLibrary`](MeshLibrary.md). All cells in the map have the same dimensions.

Internally, a GridMap is split into a sparse collection of octants for efficient rendering and physics processing. Every octant has the same dimensions and can contain several cells.

**Note:** GridMap doesn't extend [`VisualInstance3D`](VisualInstance3D.md) and therefore can't be hidden or cull masked based on [`VisualInstance3D.Layers`](VisualInstance3D.md). If you make a light not affect the first layer, the whole GridMap won't be lit by the light in question.

## 构造函数

### GridMap

```csharp
public GridMap()
```

## 属性

### BakeNavigation

```csharp
public bool BakeNavigation { get; set; }
```

引擎名：`bake_navigation`

If `true`, this GridMap creates a navigation region for each cell that uses a `MeshLibrary` item with a navigation mesh. The created navigation region will use the navigation layers bitmask assigned to the [`MeshLibrary`](MeshLibrary.md)'s item.

### CellCenterX

```csharp
public bool CellCenterX { get; set; }
```

引擎名：`cell_center_x`

If `true`, grid items are centered on the X axis.

### CellCenterY

```csharp
public bool CellCenterY { get; set; }
```

引擎名：`cell_center_y`

If `true`, grid items are centered on the Y axis.

### CellCenterZ

```csharp
public bool CellCenterZ { get; set; }
```

引擎名：`cell_center_z`

If `true`, grid items are centered on the Z axis.

### CellOctantSize

```csharp
public int CellOctantSize { get; set; }
```

引擎名：`cell_octant_size`

The size of each octant measured in number of cells. This applies to all three axis.

### CellScale

```csharp
public float CellScale { get; set; }
```

引擎名：`cell_scale`

The scale of the cell items.

This does not affect the size of the grid cells themselves, only the items in them. This can be used to make cell items overlap their neighbors.

### CellSize

```csharp
public Vector3 CellSize { get; set; }
```

引擎名：`cell_size`

The dimensions of the grid's cells.

This does not affect the size of the meshes. See `CellScale`.

### CollisionLayer

```csharp
public uint CollisionLayer { get; set; }
```

引擎名：`collision_layer`

The physics layers this GridMap is in.

GridMaps act as static bodies, meaning they aren't affected by gravity or other forces. They only affect other physics bodies that collide with them.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The physics layers this GridMap detects collisions in. See [Collision layers and masks]($DOCS_URL/tutorials/physics/physics_introduction.html#collision-layers-and-masks) in the documentation for more information.

### CollisionPriority

```csharp
public float CollisionPriority { get; set; }
```

引擎名：`collision_priority`

The priority used to solve colliding when occurring penetration. The higher the priority is, the lower the penetration into the object will be. This can for example be used to prevent the player from breaking through the boundaries of a level.

### CollisionVisibilityMode

```csharp
public GridMap.DebugVisibilityMode CollisionVisibilityMode { get; set; }
```

引擎名：`collision_visibility_mode`

Show or hide the `GridMap`'s collision shapes. If set to [`GridMap.DebugVisibilityMode.Default`](GridMap.md), this depends on the show collision debug settings.

### MeshLibrary

```csharp
public MeshLibrary MeshLibrary { get; set; }
```

引擎名：`mesh_library`

The assigned [`MeshLibrary`](MeshLibrary.md).

### PhysicsMaterial

```csharp
public PhysicsMaterial PhysicsMaterial { get; set; }
```

引擎名：`physics_material`

Overrides the default friction and bounce physics properties for the whole `GridMap`.

## 方法

### Clear

```csharp
public void Clear()
```

引擎名：`clear`

Clear all cells.

### ClearBakedMeshes

```csharp
public void ClearBakedMeshes()
```

引擎名：`clear_baked_meshes`

Clears all baked meshes. See `MakeBakedMeshes()`.

### GetBakeMeshInstance

```csharp
public Rid GetBakeMeshInstance(int idx)
```

引擎名：`get_bake_mesh_instance`

Returns [`Rid`](Rid.md) of a baked mesh with the given `idx`.

### GetBakeMeshes

```csharp
public Array GetBakeMeshes()
```

引擎名：`get_bake_meshes`

Returns an array of [`ArrayMesh`](ArrayMesh.md)es and [`Transform3D`](Transform3D.md) references of all bake meshes that exist within the current GridMap. Even indices contain [`ArrayMesh`](ArrayMesh.md)es, while odd indices contain [`Transform3D`](Transform3D.md)s that are always equal to `Transform3D.Identity`.

This method relies on the output of `MakeBakedMeshes()`, which will be called with `gen_lightmap_uv` set to `true` and `lightmap_uv_texel_size` set to `0.1` if it hasn't been called yet.

### GetBasisWithOrthogonalIndex

```csharp
public Basis GetBasisWithOrthogonalIndex(int index)
```

引擎名：`get_basis_with_orthogonal_index`

Returns one of 24 possible rotations that lie along the vectors (x,y,z) with each component being either -1, 0, or 1. For further details, refer to the Godot source code.

### GetCellItem

```csharp
public int GetCellItem(Vector3I position)
```

引擎名：`get_cell_item`

The [`MeshLibrary`](MeshLibrary.md) item index located at the given grid coordinates. If the cell is empty, `InvalidCellItem` will be returned.

### GetCellItemBasis

```csharp
public Basis GetCellItemBasis(Vector3I position)
```

引擎名：`get_cell_item_basis`

Returns the basis that gives the specified cell its orientation.

### GetCellItemOrientation

```csharp
public int GetCellItemOrientation(Vector3I position)
```

引擎名：`get_cell_item_orientation`

The orientation of the cell at the given grid coordinates. `-1` is returned if the cell is empty.

### GetCellScale

```csharp
public float GetCellScale()
```

引擎名：`get_cell_scale`

### GetCellSize

```csharp
public Vector3 GetCellSize()
```

引擎名：`get_cell_size`

### GetCenterX

```csharp
public bool GetCenterX()
```

引擎名：`get_center_x`

### GetCenterY

```csharp
public bool GetCenterY()
```

引擎名：`get_center_y`

### GetCenterZ

```csharp
public bool GetCenterZ()
```

引擎名：`get_center_z`

### GetCollisionLayer

```csharp
public uint GetCollisionLayer()
```

引擎名：`get_collision_layer`

### GetCollisionLayerValue

```csharp
public bool GetCollisionLayerValue(int layerNumber)
```

引擎名：`get_collision_layer_value`

Returns whether or not the specified layer of the `CollisionLayer` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetCollisionMaskValue

```csharp
public bool GetCollisionMaskValue(int layerNumber)
```

引擎名：`get_collision_mask_value`

Returns whether or not the specified layer of the `CollisionMask` is enabled, given a `layerNumber` between 1 and 32.

### GetCollisionPriority

```csharp
public float GetCollisionPriority()
```

引擎名：`get_collision_priority`

### GetCollisionVisibilityMode

```csharp
public GridMap.DebugVisibilityMode GetCollisionVisibilityMode()
```

引擎名：`get_collision_visibility_mode`

### GetMeshLibrary

```csharp
public MeshLibrary GetMeshLibrary()
```

引擎名：`get_mesh_library`

### GetMeshes

```csharp
public Array GetMeshes()
```

引擎名：`get_meshes`

Returns an array of [`Transform3D`](Transform3D.md) and [`Mesh`](Mesh.md) references corresponding to the non-empty cells in the grid. The transforms are specified in local space. Even indices contain [`Transform3D`](Transform3D.md)s, while odd indices contain [`Mesh`](Mesh.md)es related to the [`Transform3D`](Transform3D.md) in the index preceding it.

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

Returns the [`Rid`](Rid.md) of the navigation map this GridMap node uses for its cell baked navigation meshes.

This function returns always the map set on the GridMap node and not the map on the NavigationServer. If the map is changed directly with the NavigationServer API the GridMap node will not be aware of the map change.

### GetOctantCoordsFromCellCoords

```csharp
public Vector3I GetOctantCoordsFromCellCoords(Vector3I cellCoords)
```

引擎名：`get_octant_coords_from_cell_coords`

Returns the [`Vector3I`](Vector3I.md) octant coordinates of the octant that the cell at `cellCoords` belongs to.

### GetOctantSize

```csharp
public int GetOctantSize()
```

引擎名：`get_octant_size`

### GetOctantsInBounds

```csharp
public Array<Vector3I> GetOctantsInBounds(Aabb bounds)
```

引擎名：`get_octants_in_bounds`

Returns an array of [`Vector3I`](Vector3I.md) octant coordinates that are inside the given `bounds`, including octants that have no cells in use.

### GetOrthogonalIndexFromBasis

```csharp
public int GetOrthogonalIndexFromBasis(Basis basis)
```

引擎名：`get_orthogonal_index_from_basis`

This function considers a discretization of rotations into 24 points on unit sphere, lying along the vectors (x,y,z) with each component being either -1, 0, or 1, and returns the index (in the range from 0 to 23) of the point best representing the orientation of the object. For further details, refer to the Godot source code.

### GetPhysicsMaterial

```csharp
public PhysicsMaterial GetPhysicsMaterial()
```

引擎名：`get_physics_material`

### GetUsedCells

```csharp
public Array<Vector3I> GetUsedCells()
```

引擎名：`get_used_cells`

Returns an array of [`Vector3`](Vector3.md) with the non-empty cell coordinates in the grid map.

### GetUsedCellsByItem

```csharp
public Array<Vector3I> GetUsedCellsByItem(int item)
```

引擎名：`get_used_cells_by_item`

Returns an array of all cells with the given item index specified in `item`.

### GetUsedCellsInOctant

```csharp
public Array<Vector3I> GetUsedCellsInOctant(Vector3I octantCoords)
```

引擎名：`get_used_cells_in_octant`

Returns an array of [`Vector3I`](Vector3I.md)s with the cell coordinates of non-empty cells inside the octant at `octantCoords`.

### GetUsedCellsInOctantByItem

```csharp
public Array<Vector3I> GetUsedCellsInOctantByItem(Vector3I octantCoords, int item)
```

引擎名：`get_used_cells_in_octant_by_item`

Returns an array of [`Vector3I`](Vector3I.md)s with the cell coordinates of cells inside the octant at `octantCoords` that use the specified cell `item`.

### GetUsedOctants

```csharp
public Array<Vector3I> GetUsedOctants()
```

引擎名：`get_used_octants`

Returns an array of [`Vector3I`](Vector3I.md)s with the octant coordinates of the non-empty octants in the grid map.

### GetUsedOctantsByItem

```csharp
public Array<Vector3I> GetUsedOctantsByItem(int item)
```

引擎名：`get_used_octants_by_item`

Returns an array of [`Vector3I`](Vector3I.md)s with the octant coordinates of the octants that use the specified `item` in the grid map.

### GetUsedOctantsInBounds

```csharp
public Array<Vector3I> GetUsedOctantsInBounds(Aabb bounds)
```

引擎名：`get_used_octants_in_bounds`

Returns an array of [`Vector3I`](Vector3I.md)s with the octant coordinates of non-empty octants that are inside the local `bounds`.

### IsBakingNavigation

```csharp
public bool IsBakingNavigation()
```

引擎名：`is_baking_navigation`

### LocalToMap

```csharp
public Vector3I LocalToMap(Vector3 localPosition)
```

引擎名：`local_to_map`

Returns the map coordinates of the cell containing the given `localPosition`. If `localPosition` is in global coordinates, consider using [`Node3D.ToLocal`](Node3D.md) before passing it to this method. See also `MapToLocal()`.

### MakeBakedMeshes

```csharp
public void MakeBakedMeshes(bool genLightmapUV = false, float lightmapUVTexelSize = 0.1f)
```

引擎名：`make_baked_meshes`

Generates a baked mesh that represents all meshes in the assigned [`MeshLibrary`](MeshLibrary.md) for use with [`LightmapGI`](LightmapGI.md). If `genLightmapUV` is `true`, UV2 data will be generated for each mesh currently used in the `GridMap`. Otherwise, only meshes that already have UV2 data present will be able to use baked lightmaps. When generating UV2, `lightmapUVTexelSize` controls the texel density for lightmaps, with lower values resulting in more detailed lightmaps. `lightmapUVTexelSize` is ignored if `genLightmapUV` is `false`. See also `GetBakeMeshes()`, which relies on the output of this method.

**Note:** Calling this method will not actually bake lightmaps, as lightmap baking is performed using the [`LightmapGI`](LightmapGI.md) node.

### MapToLocal

```csharp
public Vector3 MapToLocal(Vector3I mapPosition)
```

引擎名：`map_to_local`

Returns the position of a grid cell in the GridMap's local coordinate space. To convert the returned value into global coordinates, use [`Node3D.ToGlobal`](Node3D.md). See also `LocalToMap()`.

### ResourceChanged

```csharp
public void ResourceChanged(Resource resource)
```

引擎名：`resource_changed`

This method does nothing.

### SetBakeNavigation

```csharp
public void SetBakeNavigation(bool bakeNavigation)
```

引擎名：`set_bake_navigation`

### SetCellItem

```csharp
public void SetCellItem(Vector3I position, int item, int orientation = 0)
```

引擎名：`set_cell_item`

Sets the mesh index for the cell referenced by its grid coordinates.

A negative item index such as `InvalidCellItem` will clear the cell.

Optionally, the item's orientation can be passed. For valid orientation values, see `GetOrthogonalIndexFromBasis()`.

### SetCellScale

```csharp
public void SetCellScale(float scale)
```

引擎名：`set_cell_scale`

### SetCellSize

```csharp
public void SetCellSize(Vector3 size)
```

引擎名：`set_cell_size`

### SetCenterX

```csharp
public void SetCenterX(bool enable)
```

引擎名：`set_center_x`

### SetCenterY

```csharp
public void SetCenterY(bool enable)
```

引擎名：`set_center_y`

### SetCenterZ

```csharp
public void SetCenterZ(bool enable)
```

引擎名：`set_center_z`

### SetCollisionLayer

```csharp
public void SetCollisionLayer(uint layer)
```

引擎名：`set_collision_layer`

### SetCollisionLayerValue

```csharp
public void SetCollisionLayerValue(int layerNumber, bool value)
```

引擎名：`set_collision_layer_value`

Based on `value`, enables or disables the specified layer in the `CollisionLayer`, given a `layerNumber` between 1 and 32.

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

引擎名：`set_collision_mask`

### SetCollisionMaskValue

```csharp
public void SetCollisionMaskValue(int layerNumber, bool value)
```

引擎名：`set_collision_mask_value`

Based on `value`, enables or disables the specified layer in the `CollisionMask`, given a `layerNumber` between 1 and 32.

### SetCollisionPriority

```csharp
public void SetCollisionPriority(float priority)
```

引擎名：`set_collision_priority`

### SetCollisionVisibilityMode

```csharp
public void SetCollisionVisibilityMode(GridMap.DebugVisibilityMode visibilityMode)
```

引擎名：`set_collision_visibility_mode`

### SetMeshLibrary

```csharp
public void SetMeshLibrary(MeshLibrary meshLibrary)
```

引擎名：`set_mesh_library`

### SetNavigationMap

```csharp
public void SetNavigationMap(Rid navigationMap)
```

引擎名：`set_navigation_map`

Sets the [`Rid`](Rid.md) of the navigation map this GridMap node should use for its cell baked navigation meshes.

### SetOctantSize

```csharp
public void SetOctantSize(int size)
```

引擎名：`set_octant_size`

### SetPhysicsMaterial

```csharp
public void SetPhysicsMaterial(PhysicsMaterial material)
```

引擎名：`set_physics_material`

## 事件（信号）

### CellSizeChanged

```csharp
public event GridMap.CellSizeChangedEventHandler CellSizeChanged
```

引擎名：`cell_size_changed`

Emitted when `CellSize` changes.

### Changed

```csharp
public event Action Changed
```

引擎名：`changed`

Emitted when the [`MeshLibrary`](MeshLibrary.md) of this GridMap changes.

## 常量

### InvalidCellItem

```csharp
public const long InvalidCellItem = -1
```

Invalid cell item that can be used in `SetCellItem()` to clear cells (or represent an empty cell in `GetCellItem()`).

## CellSizeChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`GridMap.CellSizeChanged`](GridMap.md) event of a [`GridMap`](GridMap.md) class.

### 签名

#### Invoke

```csharp
delegate void CellSizeChangedEventHandler(Vector3 cellSize)
```

## DebugVisibilityMode

命名空间 `Godot`；enum

### 枚举值

- `Default = 0`：Hide the collisions debug shapes in the editor, and use the debug settings to determine their visibility in game (i.e.
- `ForceShow = 1`：Always show the collisions debug shapes.
- `ForceHide = 2`：Always hide the collisions debug shapes.
