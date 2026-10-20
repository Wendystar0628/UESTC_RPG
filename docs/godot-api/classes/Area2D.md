<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Area2D

命名空间 `Godot`；class；继承 [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Area2D` is a region of 2D space defined by one or multiple [`CollisionShape2D`](CollisionShape2D.md) or [`CollisionPolygon2D`](CollisionPolygon2D.md) child nodes. It detects when other [`CollisionObject2D`](CollisionObject2D.md)s enter or exit it, and it also keeps track of which collision objects haven't exited it yet (i.e. which one are overlapping it).

This node can also locally alter or override physics parameters (gravity, damping) and route audio to custom audio buses.

**Note:** Areas and bodies created with [`PhysicsServer2D`](PhysicsServer2D.md) might not interact as expected with `Area2D`s, and might not emit signals or track objects correctly.

## 构造函数

### Area2D

```csharp
public Area2D()
```

## 属性

### AngularDamp

```csharp
public float AngularDamp { get; set; }
```

引擎名：`angular_damp`

The rate at which objects stop spinning in this area. Represents the angular velocity lost per second.

See `ProjectSettings.physics/2d/default_angular_damp` for more details about damping.

### AngularDampSpaceOverride

```csharp
public Area2D.SpaceOverride AngularDampSpaceOverride { get; set; }
```

引擎名：`angular_damp_space_override`

Override mode for angular damping calculations within this area.

### AudioBusName

```csharp
public StringName AudioBusName { get; set; }
```

引擎名：`audio_bus_name`

The name of the area's audio bus.

### AudioBusOverride

```csharp
public bool AudioBusOverride { get; set; }
```

引擎名：`audio_bus_override`

If `true`, the area's audio bus overrides the default audio bus.

### Gravity

```csharp
public float Gravity { get; set; }
```

引擎名：`gravity`

The area's gravity intensity (in pixels per second squared). This value multiplies the gravity direction. This is useful to alter the force of gravity without altering its direction.

### GravityDirection

```csharp
public Vector2 GravityDirection { get; set; }
```

引擎名：`gravity_direction`

The area's gravity vector (not normalized).

### GravityPoint

```csharp
public bool GravityPoint { get; set; }
```

引擎名：`gravity_point`

If `true`, gravity is calculated from a point (set via `GravityPointCenter`). See also `GravitySpaceOverride`.

### GravityPointCenter

```csharp
public Vector2 GravityPointCenter { get; set; }
```

引擎名：`gravity_point_center`

If gravity is a point (see `GravityPoint`), this will be the point of attraction.

### GravityPointUnitDistance

```csharp
public float GravityPointUnitDistance { get; set; }
```

引擎名：`gravity_point_unit_distance`

The distance at which the gravity strength is equal to `Gravity`. For example, on a planet 100 pixels in radius with a surface gravity of 4.0 px/s², set the `Gravity` to 4.0 and the unit distance to 100.0. The gravity will have falloff according to the inverse square law, so in the example, at 200 pixels from the center the gravity will be 1.0 px/s² (twice the distance, 1/4th the gravity), at 50 pixels it will be 16.0 px/s² (half the distance, 4x the gravity), and so on.

The above is true only when the unit distance is a positive number. When this is set to 0.0, the gravity will be constant regardless of distance.

### GravitySpaceOverride

```csharp
public Area2D.SpaceOverride GravitySpaceOverride { get; set; }
```

引擎名：`gravity_space_override`

Override mode for gravity calculations within this area.

### LinearDamp

```csharp
public float LinearDamp { get; set; }
```

引擎名：`linear_damp`

The rate at which objects stop moving in this area. Represents the linear velocity lost per second.

See `ProjectSettings.physics/2d/default_linear_damp` for more details about damping.

### LinearDampSpaceOverride

```csharp
public Area2D.SpaceOverride LinearDampSpaceOverride { get; set; }
```

引擎名：`linear_damp_space_override`

Override mode for linear damping calculations within this area.

### Monitorable

```csharp
public bool Monitorable { get; set; }
```

引擎名：`monitorable`

If `true`, other monitoring areas can detect this area.

### Monitoring

```csharp
public bool Monitoring { get; set; }
```

引擎名：`monitoring`

If `true`, the area detects bodies or areas entering and exiting it.

### Priority

```csharp
public int Priority { get; set; }
```

引擎名：`priority`

The area's priority. Higher priority areas are processed first. The [`World2D`](World2D.md)'s physics is always processed last, after all areas.

## 方法

### GetAngularDamp

```csharp
public float GetAngularDamp()
```

引擎名：`get_angular_damp`

### GetAngularDampSpaceOverrideMode

```csharp
public Area2D.SpaceOverride GetAngularDampSpaceOverrideMode()
```

引擎名：`get_angular_damp_space_override_mode`

### GetAudioBusName

```csharp
public StringName GetAudioBusName()
```

引擎名：`get_audio_bus_name`

### GetGravity

```csharp
public float GetGravity()
```

引擎名：`get_gravity`

### GetGravityDirection

```csharp
public Vector2 GetGravityDirection()
```

引擎名：`get_gravity_direction`

### GetGravityPointCenter

```csharp
public Vector2 GetGravityPointCenter()
```

引擎名：`get_gravity_point_center`

### GetGravityPointUnitDistance

```csharp
public float GetGravityPointUnitDistance()
```

引擎名：`get_gravity_point_unit_distance`

### GetGravitySpaceOverrideMode

```csharp
public Area2D.SpaceOverride GetGravitySpaceOverrideMode()
```

引擎名：`get_gravity_space_override_mode`

### GetLinearDamp

```csharp
public float GetLinearDamp()
```

引擎名：`get_linear_damp`

### GetLinearDampSpaceOverrideMode

```csharp
public Area2D.SpaceOverride GetLinearDampSpaceOverrideMode()
```

引擎名：`get_linear_damp_space_override_mode`

### GetOverlappingAreas

```csharp
public Array<Area2D> GetOverlappingAreas()
```

引擎名：`get_overlapping_areas`

Returns a list of intersecting `Area2D`s. The overlapping area's [`CollisionObject2D.CollisionLayer`](CollisionObject2D.md) must be part of this area's [`CollisionObject2D.CollisionMask`](CollisionObject2D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) this list is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

### GetOverlappingBodies

```csharp
public Array<Node2D> GetOverlappingBodies()
```

引擎名：`get_overlapping_bodies`

Returns a list of intersecting [`PhysicsBody2D`](PhysicsBody2D.md)s and [`TileMap`](TileMap.md)s. The overlapping body's [`CollisionObject2D.CollisionLayer`](CollisionObject2D.md) must be part of this area's [`CollisionObject2D.CollisionMask`](CollisionObject2D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) this list is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

### GetPriority

```csharp
public int GetPriority()
```

引擎名：`get_priority`

### HasOverlappingAreas

```csharp
public bool HasOverlappingAreas()
```

引擎名：`has_overlapping_areas`

Returns `true` if intersecting any `Area2D`s, otherwise returns `false`. The overlapping area's [`CollisionObject2D.CollisionLayer`](CollisionObject2D.md) must be part of this area's [`CollisionObject2D.CollisionMask`](CollisionObject2D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) the list of overlapping areas is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

### HasOverlappingBodies

```csharp
public bool HasOverlappingBodies()
```

引擎名：`has_overlapping_bodies`

Returns `true` if intersecting any [`PhysicsBody2D`](PhysicsBody2D.md)s or [`TileMap`](TileMap.md)s, otherwise returns `false`. The overlapping body's [`CollisionObject2D.CollisionLayer`](CollisionObject2D.md) must be part of this area's [`CollisionObject2D.CollisionMask`](CollisionObject2D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) the list of overlapping bodies is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

### IsGravityAPoint

```csharp
public bool IsGravityAPoint()
```

引擎名：`is_gravity_a_point`

### IsMonitorable

```csharp
public bool IsMonitorable()
```

引擎名：`is_monitorable`

### IsMonitoring

```csharp
public bool IsMonitoring()
```

引擎名：`is_monitoring`

### IsOverridingAudioBus

```csharp
public bool IsOverridingAudioBus()
```

引擎名：`is_overriding_audio_bus`

### OverlapsArea

```csharp
public bool OverlapsArea(Node area)
```

引擎名：`overlaps_area`

Returns `true` if the given `Area2D` intersects or overlaps this `Area2D`, `false` otherwise.

**Note:** The result of this test is not immediate after moving objects. For performance, the list of overlaps is updated once per frame and before the physics step. Consider using signals instead.

### OverlapsBody

```csharp
public bool OverlapsBody(Node body)
```

引擎名：`overlaps_body`

Returns `true` if the given physics body intersects or overlaps this `Area2D`, `false` otherwise.

**Note:** The result of this test is not immediate after moving objects. For performance, list of overlaps is updated once per frame and before the physics step. Consider using signals instead.

The `body` argument can either be a [`PhysicsBody2D`](PhysicsBody2D.md) or a [`TileMap`](TileMap.md) instance. While TileMaps are not physics bodies themselves, they register their tiles with collision shapes as a virtual physics body.

### SetAngularDamp

```csharp
public void SetAngularDamp(float angularDamp)
```

引擎名：`set_angular_damp`

### SetAngularDampSpaceOverrideMode

```csharp
public void SetAngularDampSpaceOverrideMode(Area2D.SpaceOverride spaceOverrideMode)
```

引擎名：`set_angular_damp_space_override_mode`

### SetAudioBusName

```csharp
public void SetAudioBusName(StringName name)
```

引擎名：`set_audio_bus_name`

### SetAudioBusOverride

```csharp
public void SetAudioBusOverride(bool enable)
```

引擎名：`set_audio_bus_override`

### SetGravity

```csharp
public void SetGravity(float gravity)
```

引擎名：`set_gravity`

### SetGravityDirection

```csharp
public void SetGravityDirection(Vector2 direction)
```

引擎名：`set_gravity_direction`

### SetGravityIsPoint

```csharp
public void SetGravityIsPoint(bool enable)
```

引擎名：`set_gravity_is_point`

### SetGravityPointCenter

```csharp
public void SetGravityPointCenter(Vector2 center)
```

引擎名：`set_gravity_point_center`

### SetGravityPointUnitDistance

```csharp
public void SetGravityPointUnitDistance(float distanceScale)
```

引擎名：`set_gravity_point_unit_distance`

### SetGravitySpaceOverrideMode

```csharp
public void SetGravitySpaceOverrideMode(Area2D.SpaceOverride spaceOverrideMode)
```

引擎名：`set_gravity_space_override_mode`

### SetLinearDamp

```csharp
public void SetLinearDamp(float linearDamp)
```

引擎名：`set_linear_damp`

### SetLinearDampSpaceOverrideMode

```csharp
public void SetLinearDampSpaceOverrideMode(Area2D.SpaceOverride spaceOverrideMode)
```

引擎名：`set_linear_damp_space_override_mode`

### SetMonitorable

```csharp
public void SetMonitorable(bool enable)
```

引擎名：`set_monitorable`

### SetMonitoring

```csharp
public void SetMonitoring(bool enable)
```

引擎名：`set_monitoring`

### SetPriority

```csharp
public void SetPriority(int priority)
```

引擎名：`set_priority`

## 事件（信号）

### AreaEntered

```csharp
public event Area2D.AreaEnteredEventHandler AreaEntered
```

引擎名：`area_entered`

Emitted when the received `area` enters this area. Requires `Monitoring` to be set to `true`.

### AreaExited

```csharp
public event Area2D.AreaExitedEventHandler AreaExited
```

引擎名：`area_exited`

Emitted when the received `area` exits this area. Requires `Monitoring` to be set to `true`.

### AreaShapeEntered

```csharp
public event Area2D.AreaShapeEnteredEventHandler AreaShapeEntered
```

引擎名：`area_shape_entered`

Emitted when a [`Shape2D`](Shape2D.md) of the received `area` enters a shape of this area. Requires `Monitoring` to be set to `true`.

`localShapeIndex` and `areaShapeIndex` contain indices of the interacting shapes from this area and the other area, respectively. `areaRid` contains the [`Rid`](Rid.md) of the other area. These values can be used with the [`PhysicsServer2D`](PhysicsServer2D.md).

**Example:** Get the [`CollisionShape2D`](CollisionShape2D.md) node from the shape index:

### AreaShapeExited

```csharp
public event Area2D.AreaShapeExitedEventHandler AreaShapeExited
```

引擎名：`area_shape_exited`

Emitted when a [`Shape2D`](Shape2D.md) of the received `area` exits a shape of this area. Requires `Monitoring` to be set to `true`.

See also `AreaShapeEntered`.

### BodyEntered

```csharp
public event Area2D.BodyEnteredEventHandler BodyEntered
```

引擎名：`body_entered`

Emitted when the received `body` enters this area. `body` can be a [`PhysicsBody2D`](PhysicsBody2D.md) or a [`TileMap`](TileMap.md). [`TileMap`](TileMap.md)s are detected if their [`TileSet`](TileSet.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

### BodyExited

```csharp
public event Area2D.BodyExitedEventHandler BodyExited
```

引擎名：`body_exited`

Emitted when the received `body` exits this area. `body` can be a [`PhysicsBody2D`](PhysicsBody2D.md) or a [`TileMap`](TileMap.md). [`TileMap`](TileMap.md)s are detected if their [`TileSet`](TileSet.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

### BodyShapeEntered

```csharp
public event Area2D.BodyShapeEnteredEventHandler BodyShapeEntered
```

引擎名：`body_shape_entered`

Emitted when a [`Shape2D`](Shape2D.md) of the received `body` enters a shape of this area. `body` can be a [`PhysicsBody2D`](PhysicsBody2D.md) or a [`TileMap`](TileMap.md). [`TileMap`](TileMap.md)s are detected if their [`TileSet`](TileSet.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

`localShapeIndex` and `bodyShapeIndex` contain indices of the interacting shapes from this area and the interacting body, respectively. `bodyRid` contains the [`Rid`](Rid.md) of the body. These values can be used with the [`PhysicsServer2D`](PhysicsServer2D.md).

**Example:** Get the [`CollisionShape2D`](CollisionShape2D.md) node from the shape index:

### BodyShapeExited

```csharp
public event Area2D.BodyShapeExitedEventHandler BodyShapeExited
```

引擎名：`body_shape_exited`

Emitted when a [`Shape2D`](Shape2D.md) of the received `body` exits a shape of this area. `body` can be a [`PhysicsBody2D`](PhysicsBody2D.md) or a [`TileMap`](TileMap.md). [`TileMap`](TileMap.md)s are detected if their [`TileSet`](TileSet.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

See also `BodyShapeEntered`.

## AreaEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.AreaEntered`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaEnteredEventHandler(Area2D area)
```

## AreaExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.AreaExited`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaExitedEventHandler(Area2D area)
```

## AreaShapeEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.AreaShapeEntered`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaShapeEnteredEventHandler(Rid areaRid, Area2D area, long areaShapeIndex, long localShapeIndex)
```

## AreaShapeExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.AreaShapeExited`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaShapeExitedEventHandler(Rid areaRid, Area2D area, long areaShapeIndex, long localShapeIndex)
```

## BodyEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.BodyEntered`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyEnteredEventHandler(Node2D body)
```

## BodyExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.BodyExited`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyExitedEventHandler(Node2D body)
```

## BodyShapeEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.BodyShapeEntered`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyShapeEnteredEventHandler(Rid bodyRid, Node2D body, long bodyShapeIndex, long localShapeIndex)
```

## BodyShapeExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area2D.BodyShapeExited`](Area2D.md) event of a [`Area2D`](Area2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyShapeExitedEventHandler(Rid bodyRid, Node2D body, long bodyShapeIndex, long localShapeIndex)
```

## SpaceOverride

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This area does not affect gravity/damping.
- `Combine = 1`：This area adds its gravity/damping values to whatever has been calculated so far (in [`Area2D.Priority`](Area2D.md) order).
- `CombineReplace = 2`：This area adds its gravity/damping values to whatever has been calculated so far (in [`Area2D.Priority`](Area2D.md) order), ignoring any lower priority areas.
- `Replace = 3`：This area replaces any gravity/damping, even the defaults, ignoring any lower priority areas.
- `ReplaceCombine = 4`：This area replaces any gravity/damping calculated so far (in [`Area2D.Priority`](Area2D.md) order), but keeps calculating the rest of the areas.
