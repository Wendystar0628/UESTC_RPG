<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationLink2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A link between two positions on [`NavigationRegion2D`](NavigationRegion2D.md)s that agents can be routed through. These positions can be on the same [`NavigationRegion2D`](NavigationRegion2D.md) or on two different ones. Links are useful to express navigation methods other than traveling along the surface of the navigation polygon, such as ziplines, teleporters, or gaps that can be jumped across.

## 构造函数

### NavigationLink2D

```csharp
public NavigationLink2D()
```

## 属性

### Bidirectional

```csharp
public bool Bidirectional { get; set; }
```

引擎名：`bidirectional`

Whether this link can be traveled in both directions or only from `StartPosition` to `EndPosition`.

### Enabled

```csharp
public bool Enabled { get; set; }
```

引擎名：`enabled`

Whether this link is currently active. If `false`, [`NavigationServer2D.MapGetPath`](NavigationServer2D.md) will ignore this link.

### EndPosition

```csharp
public Vector2 EndPosition { get; set; }
```

引擎名：`end_position`

Ending position of the link.

This position will search out the nearest polygon in the navigation mesh to attach to.

The distance the link will search is controlled by [`NavigationServer2D.MapSetLinkConnectionRadius`](NavigationServer2D.md).

### EnterCost

```csharp
public float EnterCost { get; set; }
```

引擎名：`enter_cost`

When pathfinding enters this link from another regions navigation mesh the `EnterCost` value is added to the path distance for determining the shortest path.

### NavigationLayers

```csharp
public uint NavigationLayers { get; set; }
```

引擎名：`navigation_layers`

A bitfield determining all navigation layers the link belongs to. These navigation layers will be checked when requesting a path with [`NavigationServer2D.MapGetPath`](NavigationServer2D.md).

### StartPosition

```csharp
public Vector2 StartPosition { get; set; }
```

引擎名：`start_position`

Starting position of the link.

This position will search out the nearest polygon in the navigation mesh to attach to.

The distance the link will search is controlled by [`NavigationServer2D.MapSetLinkConnectionRadius`](NavigationServer2D.md).

### TravelCost

```csharp
public float TravelCost { get; set; }
```

引擎名：`travel_cost`

When pathfinding moves along the link the traveled distance is multiplied with `TravelCost` for determining the shortest path.

## 方法

### GetEndPosition

```csharp
public Vector2 GetEndPosition()
```

引擎名：`get_end_position`

### GetEnterCost

```csharp
public float GetEnterCost()
```

引擎名：`get_enter_cost`

### GetGlobalEndPosition

```csharp
public Vector2 GetGlobalEndPosition()
```

引擎名：`get_global_end_position`

Returns the `EndPosition` that is relative to the link as a global position.

### GetGlobalStartPosition

```csharp
public Vector2 GetGlobalStartPosition()
```

引擎名：`get_global_start_position`

Returns the `StartPosition` that is relative to the link as a global position.

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

Returns the current navigation map [`Rid`](Rid.md) used by this link.

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the [`Rid`](Rid.md) of this link on the [`NavigationServer2D`](NavigationServer2D.md).

### GetStartPosition

```csharp
public Vector2 GetStartPosition()
```

引擎名：`get_start_position`

### GetTravelCost

```csharp
public float GetTravelCost()
```

引擎名：`get_travel_cost`

### IsBidirectional

```csharp
public bool IsBidirectional()
```

引擎名：`is_bidirectional`

### IsEnabled

```csharp
public bool IsEnabled()
```

引擎名：`is_enabled`

### SetBidirectional

```csharp
public void SetBidirectional(bool bidirectional)
```

引擎名：`set_bidirectional`

### SetEnabled

```csharp
public void SetEnabled(bool enabled)
```

引擎名：`set_enabled`

### SetEndPosition

```csharp
public void SetEndPosition(Vector2 position)
```

引擎名：`set_end_position`

### SetEnterCost

```csharp
public void SetEnterCost(float enterCost)
```

引擎名：`set_enter_cost`

### SetGlobalEndPosition

```csharp
public void SetGlobalEndPosition(Vector2 position)
```

引擎名：`set_global_end_position`

Sets the `EndPosition` that is relative to the link from a global `position`.

### SetGlobalStartPosition

```csharp
public void SetGlobalStartPosition(Vector2 position)
```

引擎名：`set_global_start_position`

Sets the `StartPosition` that is relative to the link from a global `position`.

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

Sets the [`Rid`](Rid.md) of the navigation map this link should use. By default the link will automatically join the [`World2D`](World2D.md) default navigation map so this function is only required to override the default map.

### SetStartPosition

```csharp
public void SetStartPosition(Vector2 position)
```

引擎名：`set_start_position`

### SetTravelCost

```csharp
public void SetTravelCost(float travelCost)
```

引擎名：`set_travel_cost`
