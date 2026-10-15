<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# NavigationObstacle3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

An obstacle needs a navigation map and outline `Vertices` defined to work correctly. The outlines can not cross or overlap and are restricted to a plane projection. This means the y-axis of the vertices is ignored, instead the obstacle's global y-axis position is used for placement. The projected shape is extruded by the obstacles height along the y-axis.

Obstacles can be included in the navigation mesh baking process when `AffectNavigationMesh` is enabled. They do not add walkable geometry, instead their role is to discard other source geometry inside the shape. This can be used to prevent navigation mesh from appearing in unwanted places, e.g. inside "solid" geometry or on top of it. If `CarveNavigationMesh` is enabled the baked shape will not be affected by offsets of the navigation mesh baking, e.g. the agent radius.

With `AvoidanceEnabled` the obstacle can constrain the avoidance velocities of avoidance using agents. If the obstacle's vertices are wound in clockwise order, avoidance agents will be pushed in by the obstacle, otherwise, avoidance agents will be pushed out. Obstacles using vertices and avoidance can warp to a new position but should not be moved every single frame as each change requires a rebuild of the avoidance map.

## 构造函数

### NavigationObstacle3D

```csharp
public NavigationObstacle3D()
```

## 属性

### AffectNavigationMesh

```csharp
public bool AffectNavigationMesh { get; set; }
```

引擎名：`affect_navigation_mesh`

If enabled and parsed in a navigation mesh baking process the obstacle will discard source geometry inside its `Vertices` and `Height` defined shape.

### AvoidanceEnabled

```csharp
public bool AvoidanceEnabled { get; set; }
```

引擎名：`avoidance_enabled`

If `true` the obstacle affects avoidance using agents.

### AvoidanceLayers

```csharp
public uint AvoidanceLayers { get; set; }
```

引擎名：`avoidance_layers`

A bitfield determining the avoidance layers for this obstacle. Agents with a matching bit on the their avoidance mask will avoid this obstacle.

### CarveNavigationMesh

```csharp
public bool CarveNavigationMesh { get; set; }
```

引擎名：`carve_navigation_mesh`

If enabled the obstacle vertices will carve into the baked navigation mesh with the shape unaffected by additional offsets (e.g. agent radius).

It will still be affected by further postprocessing of the baking process, like edge and polygon simplification.

Requires `AffectNavigationMesh` to be enabled.

### Height

```csharp
public float Height { get; set; }
```

引擎名：`height`

Sets the obstacle height used in 2D avoidance. 2D avoidance using agent's ignore obstacles that are below or above them.

### Radius

```csharp
public float Radius { get; set; }
```

引擎名：`radius`

Sets the avoidance radius for the obstacle.

### Use3DAvoidance

```csharp
public bool Use3DAvoidance { get; set; }
```

If `true` the obstacle affects 3D avoidance using agent's with obstacle `Radius`.

If `false` the obstacle affects 2D avoidance using agent's with both obstacle `Vertices` as well as obstacle `Radius`.

### Velocity

```csharp
public Vector3 Velocity { get; set; }
```

引擎名：`velocity`

Sets the wanted velocity for the obstacle so other agent's can better predict the obstacle if it is moved with a velocity regularly (every frame) instead of warped to a new position. Does only affect avoidance for the obstacles `Radius`. Does nothing for the obstacles static vertices.

### Vertices

```csharp
public Vector3[] Vertices { get; set; }
```

引擎名：`vertices`

The outline vertices of the obstacle. If the vertices are winded in clockwise order agents will be pushed in by the obstacle, else they will be pushed out. Outlines can not be crossed or overlap. Should the vertices using obstacle be warped to a new position agent's can not predict this movement and may get trapped inside the obstacle.

## 方法

### GetAffectNavigationMesh

```csharp
public bool GetAffectNavigationMesh()
```

引擎名：`get_affect_navigation_mesh`

### GetAvoidanceEnabled

```csharp
public bool GetAvoidanceEnabled()
```

引擎名：`get_avoidance_enabled`

### GetAvoidanceLayerValue

```csharp
public bool GetAvoidanceLayerValue(int layerNumber)
```

引擎名：`get_avoidance_layer_value`

Returns whether or not the specified layer of the `AvoidanceLayers` bitmask is enabled, given a `layerNumber` between 1 and 32.

### GetAvoidanceLayers

```csharp
public uint GetAvoidanceLayers()
```

引擎名：`get_avoidance_layers`

### GetCarveNavigationMesh

```csharp
public bool GetCarveNavigationMesh()
```

引擎名：`get_carve_navigation_mesh`

### GetHeight

```csharp
public float GetHeight()
```

引擎名：`get_height`

### GetNavigationMap

```csharp
public Rid GetNavigationMap()
```

引擎名：`get_navigation_map`

Returns the [`Rid`](Rid.md) of the navigation map for this NavigationObstacle node. This function returns always the map set on the NavigationObstacle node and not the map of the abstract obstacle on the NavigationServer. If the obstacle map is changed directly with the NavigationServer API the NavigationObstacle node will not be aware of the map change. Use `SetNavigationMap()` to change the navigation map for the NavigationObstacle and also update the obstacle on the NavigationServer.

### GetRadius

```csharp
public float GetRadius()
```

引擎名：`get_radius`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the [`Rid`](Rid.md) of this obstacle on the [`NavigationServer3D`](NavigationServer3D.md).

### GetUse3DAvoidance

```csharp
public bool GetUse3DAvoidance()
```

### GetVelocity

```csharp
public Vector3 GetVelocity()
```

引擎名：`get_velocity`

### GetVertices

```csharp
public Vector3[] GetVertices()
```

引擎名：`get_vertices`

### SetAffectNavigationMesh

```csharp
public void SetAffectNavigationMesh(bool enabled)
```

引擎名：`set_affect_navigation_mesh`

### SetAvoidanceEnabled

```csharp
public void SetAvoidanceEnabled(bool enabled)
```

引擎名：`set_avoidance_enabled`

### SetAvoidanceLayerValue

```csharp
public void SetAvoidanceLayerValue(int layerNumber, bool value)
```

引擎名：`set_avoidance_layer_value`

Based on `value`, enables or disables the specified layer in the `AvoidanceLayers` bitmask, given a `layerNumber` between 1 and 32.

### SetAvoidanceLayers

```csharp
public void SetAvoidanceLayers(uint layers)
```

引擎名：`set_avoidance_layers`

### SetCarveNavigationMesh

```csharp
public void SetCarveNavigationMesh(bool enabled)
```

引擎名：`set_carve_navigation_mesh`

### SetHeight

```csharp
public void SetHeight(float height)
```

引擎名：`set_height`

### SetNavigationMap

```csharp
public void SetNavigationMap(Rid navigationMap)
```

引擎名：`set_navigation_map`

Sets the [`Rid`](Rid.md) of the navigation map this NavigationObstacle node should use and also updates the `obstacle` on the NavigationServer.

### SetRadius

```csharp
public void SetRadius(float radius)
```

引擎名：`set_radius`

### SetUse3DAvoidance

```csharp
public void SetUse3DAvoidance(bool enabled)
```

### SetVelocity

```csharp
public void SetVelocity(Vector3 velocity)
```

引擎名：`set_velocity`

### SetVertices

```csharp
public void SetVertices(ReadOnlySpan<Vector3> vertices)
```

引擎名：`set_vertices`

### SetVertices

```csharp
public void SetVertices(Vector3[] vertices)
```

引擎名：`set_vertices`
