<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationRegion3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A traversable 3D region based on a [`NavigationMesh`](NavigationMesh.md) that [`NavigationAgent3D`](NavigationAgent3D.md)s can use for pathfinding.

Two regions can be connected to each other if they share a similar edge. You can set the minimum distance between two vertices required to connect two edges by using [`NavigationServer3D.MapSetEdgeConnectionMargin`](NavigationServer3D.md).

**Note:** Overlapping two regions' navigation meshes is not enough for connecting two regions. They must share a similar edge.

The cost of entering this region from another region can be controlled with the `EnterCost` value.

**Note:** This value is not added to the path cost when the start position is already inside this region.

The cost of traveling distances inside this region can be controlled with the `TravelCost` multiplier.

**Note:** This node caches changes to its properties, so if you make changes to the underlying region [`Rid`](Rid.md) in [`NavigationServer3D`](NavigationServer3D.md), they will not be reflected in this node's properties.

## 构造函数

### NavigationRegion3D

```csharp
public NavigationRegion3D()
```

## 属性

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

Determines if the `NavigationRegion3D` is enabled or disabled.

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

A bitfield determining all navigation layers the region belongs to. These navigation layers can be checked upon when requesting a path with [`NavigationServer3D.MapGetPath`](NavigationServer3D.md).

### NavigationMesh

```csharp
public NavigationMesh NavigationMesh { get; set; }
```

引擎名：`navigation_mesh`

The [`NavigationMesh`](NavigationMesh.md) resource to use.

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

### BakeNavigationMesh

```csharp
public void BakeNavigationMesh(bool onThread = true)
```

引擎名：`bake_navigation_mesh`

Bakes the [`NavigationMesh`](NavigationMesh.md). If `onThread` is set to `true` (default), the baking is done on a separate thread. Baking on separate thread is useful because navigation baking is not a cheap operation. When it is completed, it automatically sets the new [`NavigationMesh`](NavigationMesh.md). Please note that baking on separate thread may be very slow if geometry is parsed from meshes as async access to each mesh involves heavy synchronization. Also, please note that baking on a separate thread is automatically disabled on operating systems that cannot use threads (such as Web with threads disabled).

### GetBounds

```csharp
public Aabb GetBounds()
```

引擎名：`get_bounds`

Returns the axis-aligned bounding box for the region's transformed navigation mesh.

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

### GetNavigationMesh

```csharp
public NavigationMesh GetNavigationMesh()
```

引擎名：`get_navigation_mesh`

### GetRegionRid

```csharp
public Rid GetRegionRid()
```

引擎名：`get_region_rid`

Returns the [`Rid`](Rid.md) of this region on the [`NavigationServer3D`](NavigationServer3D.md).

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the [`Rid`](Rid.md) of this region on the [`NavigationServer3D`](NavigationServer3D.md). Combined with [`NavigationServer3D.MapGetClosestPointOwner`](NavigationServer3D.md) can be used to identify the `NavigationRegion3D` closest to a point on the merged navigation map.

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

Returns `true` when the [`NavigationMesh`](NavigationMesh.md) is being baked on a background thread.

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

Sets the [`Rid`](Rid.md) of the navigation map this region should use. By default the region will automatically join the [`World3D`](World3D.md) default navigation map so this function is only required to override the default map.

### SetNavigationMesh

```csharp
public void SetNavigationMesh(NavigationMesh navigationMesh)
```

引擎名：`set_navigation_mesh`

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

Notifies when the navigation mesh bake operation is completed.

### NavigationMeshChanged

```csharp
public event Action NavigationMeshChanged
```

引擎名：`navigation_mesh_changed`

Notifies when the [`NavigationMesh`](NavigationMesh.md) has changed.
