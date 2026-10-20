<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TileData

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

`TileData` object represents a single tile in a [`TileSet`](TileSet.md). It is usually edited using the tileset editor, but it can be modified at runtime using [`TileMapLayer._TileDataRuntimeUpdate()`](TileMapLayer.md).

## 构造函数

### TileData

```csharp
public TileData()
```

## 属性

### FlipH

```csharp
public bool FlipH { get; set; }
```

引擎名：`flip_h`

If `true`, the tile will have its texture flipped horizontally.

### FlipV

```csharp
public bool FlipV { get; set; }
```

引擎名：`flip_v`

If `true`, the tile will have its texture flipped vertically.

### Material

```csharp
public Material Material { get; set; }
```

引擎名：`material`

The [`Material`](Material.md) to use for this `TileData`. This can be a [`CanvasItemMaterial`](CanvasItemMaterial.md) to use the default shader, or a [`ShaderMaterial`](ShaderMaterial.md) to use a custom shader.

### Modulate

```csharp
public Color Modulate { get; set; }
```

引擎名：`modulate`

Color modulation of the tile.

### Probability

```csharp
public float Probability { get; set; }
```

引擎名：`probability`

Relative probability of this tile being selected when drawing a pattern of random tiles.

### Terrain

```csharp
public int Terrain { get; set; }
```

引擎名：`terrain`

ID of the terrain from the terrain set that the tile uses.

### TerrainSet

```csharp
public int TerrainSet { get; set; }
```

引擎名：`terrain_set`

ID of the terrain set that the tile uses.

### TextureOrigin

```csharp
public Vector2I TextureOrigin { get; set; }
```

引擎名：`texture_origin`

Offsets the position of where the tile is drawn.

### Transpose

```csharp
public bool Transpose { get; set; }
```

引擎名：`transpose`

If `true`, the tile will display transposed, i.e. with horizontal and vertical texture UVs swapped.

### YSortOrigin

```csharp
public int YSortOrigin { get; set; }
```

引擎名：`y_sort_origin`

Vertical point of the tile used for determining y-sorted order.

### ZIndex

```csharp
public int ZIndex { get; set; }
```

引擎名：`z_index`

Ordering index of this tile, relative to [`TileMapLayer`](TileMapLayer.md).

## 方法

### AddCollisionPolygon

```csharp
public void AddCollisionPolygon(int layerId)
```

引擎名：`add_collision_polygon`

Adds a collision polygon to the tile on the given TileSet physics layer.

### AddOccluderPolygon

```csharp
public void AddOccluderPolygon(int layerId)
```

引擎名：`add_occluder_polygon`

Adds an occlusion polygon to the tile on the TileSet occlusion layer with index `layerId`.

### GetCollisionPolygonOneWayMargin

```csharp
public float GetCollisionPolygonOneWayMargin(int layerId, int polygonIndex)
```

引擎名：`get_collision_polygon_one_way_margin`

Returns the one-way margin (for one-way platforms) of the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### GetCollisionPolygonPoints

```csharp
public Vector2[] GetCollisionPolygonPoints(int layerId, int polygonIndex)
```

引擎名：`get_collision_polygon_points`

Returns the points of the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### GetCollisionPolygonsCount

```csharp
public int GetCollisionPolygonsCount(int layerId)
```

引擎名：`get_collision_polygons_count`

Returns how many polygons the tile has for TileSet physics layer with index `layerId`.

### GetConstantAngularVelocity

```csharp
public float GetConstantAngularVelocity(int layerId)
```

引擎名：`get_constant_angular_velocity`

Returns the constant angular velocity applied to objects colliding with this tile.

### GetConstantLinearVelocity

```csharp
public Vector2 GetConstantLinearVelocity(int layerId)
```

引擎名：`get_constant_linear_velocity`

Returns the constant linear velocity applied to objects colliding with this tile.

### GetCustomData

```csharp
public Variant GetCustomData(string layerName)
```

引擎名：`get_custom_data`

Returns the custom data value for custom data layer named `layerName`. To check if a custom data layer exists, use `HasCustomData()`.

### GetCustomDataByLayerId

```csharp
public Variant GetCustomDataByLayerId(int layerId)
```

引擎名：`get_custom_data_by_layer_id`

Returns the custom data value for custom data layer with index `layerId`.

### GetFlipH

```csharp
public bool GetFlipH()
```

引擎名：`get_flip_h`

### GetFlipV

```csharp
public bool GetFlipV()
```

引擎名：`get_flip_v`

### GetMaterial

```csharp
public Material GetMaterial()
```

引擎名：`get_material`

### GetModulate

```csharp
public Color GetModulate()
```

引擎名：`get_modulate`

### GetNavigationPolygon

```csharp
public NavigationPolygon GetNavigationPolygon(int _UnnamedArg0)
```

引擎名：`get_navigation_polygon`

Returns the navigation polygon of the tile for the TileSet navigation layer with index `layerId`.

`flipH`, `flipV`, and `transpose` allow transforming the returned polygon.

### GetNavigationPolygon

```csharp
public NavigationPolygon GetNavigationPolygon(int layerId, bool flipH = false, bool flipV = false, bool transpose = false)
```

引擎名：`get_navigation_polygon`

Returns the navigation polygon of the tile for the TileSet navigation layer with index `layerId`.

`flipH`, `flipV`, and `transpose` allow transforming the returned polygon.

### GetOccluder

```csharp
public OccluderPolygon2D GetOccluder(int _UnnamedArg0)
```

引擎名：`get_occluder`

Returns the occluder polygon of the tile for the TileSet occlusion layer with index `layerId`.

`flipH`, `flipV`, and `transpose` allow transforming the returned polygon.

### GetOccluder

```csharp
public OccluderPolygon2D GetOccluder(int layerId, bool flipH = false, bool flipV = false, bool transpose = false)
```

引擎名：`get_occluder`

Returns the occluder polygon of the tile for the TileSet occlusion layer with index `layerId`.

`flipH`, `flipV`, and `transpose` allow transforming the returned polygon.

### GetOccluderPolygon

```csharp
public OccluderPolygon2D GetOccluderPolygon(int layerId, int polygonIndex, bool flipH = false, bool flipV = false, bool transpose = false)
```

引擎名：`get_occluder_polygon`

Returns the occluder polygon at index `polygonIndex` from the TileSet occlusion layer with index `layerId`.

The `flipH`, `flipV`, and `transpose` parameters can be `true` to transform the returned polygon.

### GetOccluderPolygonsCount

```csharp
public int GetOccluderPolygonsCount(int layerId)
```

引擎名：`get_occluder_polygons_count`

Returns the number of occluder polygons of the tile in the TileSet occlusion layer with index `layerId`.

### GetProbability

```csharp
public float GetProbability()
```

引擎名：`get_probability`

### GetTerrain

```csharp
public int GetTerrain()
```

引擎名：`get_terrain`

### GetTerrainPeeringBit

```csharp
public int GetTerrainPeeringBit(TileSet.CellNeighbor peeringBit)
```

引擎名：`get_terrain_peering_bit`

Returns the tile's terrain bit for the given `peeringBit` direction. To check that a direction is valid, use `IsValidTerrainPeeringBit()`.

### GetTerrainSet

```csharp
public int GetTerrainSet()
```

引擎名：`get_terrain_set`

### GetTextureOrigin

```csharp
public Vector2I GetTextureOrigin()
```

引擎名：`get_texture_origin`

### GetTranspose

```csharp
public bool GetTranspose()
```

引擎名：`get_transpose`

### GetYSortOrigin

```csharp
public int GetYSortOrigin()
```

引擎名：`get_y_sort_origin`

### GetZIndex

```csharp
public int GetZIndex()
```

引擎名：`get_z_index`

### HasCustomData

```csharp
public bool HasCustomData(string layerName)
```

引擎名：`has_custom_data`

Returns whether there exists a custom data layer named `layerName`.

### IsCollisionPolygonOneWay

```csharp
public bool IsCollisionPolygonOneWay(int layerId, int polygonIndex)
```

引擎名：`is_collision_polygon_one_way`

Returns whether one-way collisions are enabled for the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### IsValidTerrainPeeringBit

```csharp
public bool IsValidTerrainPeeringBit(TileSet.CellNeighbor peeringBit)
```

引擎名：`is_valid_terrain_peering_bit`

Returns whether the given `peeringBit` direction is valid for this tile.

### RemoveCollisionPolygon

```csharp
public void RemoveCollisionPolygon(int layerId, int polygonIndex)
```

引擎名：`remove_collision_polygon`

Removes the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### RemoveOccluderPolygon

```csharp
public void RemoveOccluderPolygon(int layerId, int polygonIndex)
```

引擎名：`remove_occluder_polygon`

Removes the polygon at index `polygonIndex` for TileSet occlusion layer with index `layerId`.

### SetCollisionPolygonOneWay

```csharp
public void SetCollisionPolygonOneWay(int layerId, int polygonIndex, bool oneWay)
```

引擎名：`set_collision_polygon_one_way`

Enables/disables one-way collisions on the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### SetCollisionPolygonOneWayMargin

```csharp
public void SetCollisionPolygonOneWayMargin(int layerId, int polygonIndex, float oneWayMargin)
```

引擎名：`set_collision_polygon_one_way_margin`

Sets the one-way margin (for one-way platforms) of the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### SetCollisionPolygonPoints

```csharp
public void SetCollisionPolygonPoints(int layerId, int polygonIndex, ReadOnlySpan<Vector2> polygon)
```

引擎名：`set_collision_polygon_points`

Sets the points of the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### SetCollisionPolygonPoints

```csharp
public void SetCollisionPolygonPoints(int layerId, int polygonIndex, Vector2[] polygon)
```

引擎名：`set_collision_polygon_points`

Sets the points of the polygon at index `polygonIndex` for TileSet physics layer with index `layerId`.

### SetCollisionPolygonsCount

```csharp
public void SetCollisionPolygonsCount(int layerId, int polygonsCount)
```

引擎名：`set_collision_polygons_count`

Sets the polygons count for TileSet physics layer with index `layerId`.

### SetConstantAngularVelocity

```csharp
public void SetConstantAngularVelocity(int layerId, float velocity)
```

引擎名：`set_constant_angular_velocity`

Sets the constant angular velocity. This does not rotate the tile. This angular velocity is applied to objects colliding with this tile.

### SetConstantLinearVelocity

```csharp
public void SetConstantLinearVelocity(int layerId, Vector2 velocity)
```

引擎名：`set_constant_linear_velocity`

Sets the constant linear velocity. This does not move the tile. This linear velocity is applied to objects colliding with this tile. This is useful to create conveyor belts.

### SetCustomData

```csharp
public void SetCustomData(string layerName, Variant value)
```

引擎名：`set_custom_data`

Sets the tile's custom data value for the TileSet custom data layer with name `layerName`.

### SetCustomDataByLayerId

```csharp
public void SetCustomDataByLayerId(int layerId, Variant value)
```

引擎名：`set_custom_data_by_layer_id`

Sets the tile's custom data value for the TileSet custom data layer with index `layerId`.

### SetFlipH

```csharp
public void SetFlipH(bool flipH)
```

引擎名：`set_flip_h`

### SetFlipV

```csharp
public void SetFlipV(bool flipV)
```

引擎名：`set_flip_v`

### SetMaterial

```csharp
public void SetMaterial(Material material)
```

引擎名：`set_material`

### SetModulate

```csharp
public void SetModulate(Color modulate)
```

引擎名：`set_modulate`

### SetNavigationPolygon

```csharp
public void SetNavigationPolygon(int layerId, NavigationPolygon navigationPolygon)
```

引擎名：`set_navigation_polygon`

Sets the navigation polygon for the TileSet navigation layer with index `layerId`.

### SetOccluder

```csharp
public void SetOccluder(int layerId, OccluderPolygon2D occluderPolygon)
```

引擎名：`set_occluder`

Sets the occluder for the TileSet occlusion layer with index `layerId`.

### SetOccluderPolygon

```csharp
public void SetOccluderPolygon(int layerId, int polygonIndex, OccluderPolygon2D polygon)
```

引擎名：`set_occluder_polygon`

Sets the occluder for polygon with index `polygonIndex` in the TileSet occlusion layer with index `layerId`.

### SetOccluderPolygonsCount

```csharp
public void SetOccluderPolygonsCount(int layerId, int polygonsCount)
```

引擎名：`set_occluder_polygons_count`

Sets the occluder polygon count in the TileSet occlusion layer with index `layerId`.

### SetProbability

```csharp
public void SetProbability(float probability)
```

引擎名：`set_probability`

### SetTerrain

```csharp
public void SetTerrain(int terrain)
```

引擎名：`set_terrain`

### SetTerrainPeeringBit

```csharp
public void SetTerrainPeeringBit(TileSet.CellNeighbor peeringBit, int terrain)
```

引擎名：`set_terrain_peering_bit`

Sets the tile's terrain bit for the given `peeringBit` direction. To check that a direction is valid, use `IsValidTerrainPeeringBit()`.

### SetTerrainSet

```csharp
public void SetTerrainSet(int terrainSet)
```

引擎名：`set_terrain_set`

### SetTextureOrigin

```csharp
public void SetTextureOrigin(Vector2I textureOrigin)
```

引擎名：`set_texture_origin`

### SetTranspose

```csharp
public void SetTranspose(bool transpose)
```

引擎名：`set_transpose`

### SetYSortOrigin

```csharp
public void SetYSortOrigin(int ySortOrigin)
```

引擎名：`set_y_sort_origin`

### SetZIndex

```csharp
public void SetZIndex(int zIndex)
```

引擎名：`set_z_index`

## 事件（信号）

### Changed

```csharp
public event Action Changed
```

引擎名：`changed`

Emitted when any of the properties are changed.
