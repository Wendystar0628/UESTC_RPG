<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationRegion2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A traversable 2D region based on a [`NavigationPolygon`](NavigationPolygon.md) that [`NavigationAgent2D`](NavigationAgent2D.md)s can use for pathfinding.

Two regions can be connected to each other if they share a similar edge. You can set the minimum distance between two vertices required to connect two edges by using [`NavigationServer2D.MapSetEdgeConnectionMargin`](NavigationServer2D.md).

**Note:** Overlapping two regions' navigation polygons is not enough for connecting two regions. They must share a similar edge.

The pathfinding cost of entering a region from another region can be controlled with the `EnterCost` value.

**Note:** This value is not added to the path cost when the start position is already inside this region.

The pathfinding cost of traveling distances inside this region can be controlled with the `TravelCost` multiplier.

**Note:** This node caches changes to its properties, so if you make changes to the underlying region [`Rid`](Rid.md) in [`NavigationServer2D`](NavigationServer2D.md), they will not be reflected in this node's properties.

## 构造函数

### NavigationRegion2D

```csharp
public NavigationRegion2D()
```

## 属性

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

Determines if the `NavigationRegion2D` is enabled or disabled.

### EnterCost

```csharp
public float EnterCost { get; set; }
```

引擎名：`enter_cost`

When pathfinding enters this region's navigation mesh from another regions navigation mesh the `EnterCost` value is added to the path distance for determining the shortest path.

### NavigationLayers

```csharp
public uint NavigationLayers { get; set; }
```

引擎名：`navigation_layers`

A bitfield determining all navigation layers the region belongs to. These navigation layers can be checked upon when requesting a path with [`NavigationServer2D.MapGetPath`](NavigationServer2D.md).

### NavigationPolygon

```csharp
public NavigationPolygon NavigationPolygon { get; set; }
```

引擎名：`navigation_polygon`

The [`NavigationPolygon`](NavigationPolygon.md) resource to use.

### TravelCost

```csharp
public float TravelCost { get; set; }
```

引擎名：`travel_cost`

When pathfinding moves inside this region's navigation mesh the traveled distances are multiplied with `TravelCost` for determining the shortest path.

### UseEdgeConnections

```csharp
public bool UseEdgeConnections { get; set; }
```

引擎名：`use_edge_connections`

If enabled the navigation region will use edge connections to connect with other navigation regions within proximity of the navigation map edge connection margin.

## 方法

### BakeNavigationPolygon

```csharp
public void BakeNavigationPolygon(bool onThread = true)
```

引擎名：`bake_navigation_polygon`

Bakes the [`NavigationPolygon`](NavigationPolygon.md). If `onThread` is set to `true` (default), the baking is done on a separate thread.

### GetBounds

```csharp
public Rect2 GetBounds()
```

引擎名：`get_bounds`

Returns the axis-aligned rectangle for the region's transformed navigation mesh.

### GetEnterCost

```csharp
public float GetEnterCost()
```

引擎名：`get_enter_cost`

### GetNavigationLayerValue

```csharp
public bool GetNavigationLayerValue(int layerNumber)
```

引擎名：`get_navigation_layer_value`

Returns whether or not the specified layer of the `NavigationLayers` bitmask is enabled, given a `layerNumber` between 1 and 32.

### GetNavigationLayers

```csharp
public uint GetNavigationLayers()
```

引擎名：`get_navigation_layers`

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

Returns the current navigation map [`Rid`](Rid.md) used by this region.

### GetNavigationPolygon

```csharp
public NavigationPolygon GetNavigationPolygon()
```

引擎名：`get_navigation_polygon`

### GetRegionRid

```csharp
public Rid GetRegionRid()
```

引擎名：`get_region_rid`

Returns the [`Rid`](Rid.md) of this region on the [`NavigationServer2D`](NavigationServer2D.md).

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the [`Rid`](Rid.md) of this region on the [`NavigationServer2D`](NavigationServer2D.md). Combined with [`NavigationServer2D.MapGetClosestPointOwner`](NavigationServer2D.md) can be used to identify the `NavigationRegion2D` closest to a point on the merged navigation map.

### GetTravelCost

```csharp
public float GetTravelCost()
```

引擎名：`get_travel_cost`

### GetUseEdgeConnections

```csharp
public bool GetUseEdgeConnections()
```

引擎名：`get_use_edge_connections`

### IsBaking

```csharp
public bool IsBaking()
```

引擎名：`is_baking`

Returns `true` when the [`NavigationPolygon`](NavigationPolygon.md) is being baked on a background thread.

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetEnterCost

```csharp
public void SetEnterCost(float enterCost)
```

引擎名：`set_enter_cost`

### SetNavigationLayerValue

```csharp
public void SetNavigationLayerValue(int layerNumber, bool value)
```

引擎名：`set_navigation_layer_value`

Based on `value`, enables or disables the specified layer in the `NavigationLayers` bitmask, given a `layerNumber` between 1 and 32.

### SetNavigationLayers

```csharp
public void SetNavigationLayers(uint navigationLayers)
```

引擎名：`set_navigation_layers`

### SetNavigationMap

```csharp
public void SetNavigationMap(Rid navigationMap)
```

引擎名：`set_navigation_map`

Sets the [`Rid`](Rid.md) of the navigation map this region should use. By default the region will automatically join the [`World2D`](World2D.md) default navigation map so this function is only required to override the default map.

### SetNavigationPolygon

```csharp
public void SetNavigationPolygon(NavigationPolygon navigationPolygon)
```

引擎名：`set_navigation_polygon`

### SetTravelCost

```csharp
public void SetTravelCost(float travelCost)
```

引擎名：`set_travel_cost`

### SetUseEdgeConnections

```csharp
public void SetUseEdgeConnections(bool enabled)
```

引擎名：`set_use_edge_connections`

## 事件（信号）

### BakeFinished

```csharp
public event Action BakeFinished
```

引擎名：`bake_finished`

Emitted when a navigation polygon bake operation is completed.

### NavigationPolygonChanged

```csharp
public event Action NavigationPolygonChanged
```

引擎名：`navigation_polygon_changed`

Emitted when the used navigation polygon is replaced or changes to the internals of the current navigation polygon are committed.
