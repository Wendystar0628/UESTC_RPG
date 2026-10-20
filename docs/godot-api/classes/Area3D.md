<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Area3D

命名空间 `Godot`；class；继承 [`CollisionObject3D`](CollisionObject3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Area3D` is a region of 3D space defined by one or multiple [`CollisionShape3D`](CollisionShape3D.md) or [`CollisionPolygon3D`](CollisionPolygon3D.md) child nodes. It detects when other [`CollisionObject3D`](CollisionObject3D.md)s enter or exit it, and it also keeps track of which collision objects haven't exited it yet (i.e. which one are overlapping it).

This node can also locally alter or override physics parameters (gravity, damping) and route audio to custom audio buses.

**Note:** Areas and bodies created with [`PhysicsServer3D`](PhysicsServer3D.md) might not interact as expected with `Area3D`s, and might not emit signals or track objects correctly.

**Warning:** Using a [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) inside a [`CollisionShape3D`](CollisionShape3D.md) child of this node (created e.g. by using the **Create Trimesh Collision Sibling** option in the **Mesh** menu that appears when selecting a [`MeshInstance3D`](MeshInstance3D.md) node) may give unexpected results, since this collision shape is hollow. If this is not desired, it has to be split into multiple [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md)s or primitive shapes like [`BoxShape3D`](BoxShape3D.md), or in some cases it may be replaceable by a [`CollisionPolygon3D`](CollisionPolygon3D.md).

## 构造函数

### Area3D

```csharp
public Area3D()
```

## 属性

### AngularDamp

```csharp
public float AngularDamp { get; set; }
```

引擎名：`angular_damp`

The rate at which objects stop spinning in this area. Represents the angular velocity lost per second.

See `ProjectSettings.physics/3d/default_angular_damp` for more details about damping.

### AngularDampSpaceOverride

```csharp
public Area3D.SpaceOverride AngularDampSpaceOverride { get; set; }
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

The area's gravity intensity (in meters per second squared). This value multiplies the gravity direction. This is useful to alter the force of gravity without altering its direction.

### GravityDirection

```csharp
public Vector3 GravityDirection { get; set; }
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
public Vector3 GravityPointCenter { get; set; }
```

引擎名：`gravity_point_center`

If gravity is a point (see `GravityPoint`), this will be the point of attraction.

### GravityPointUnitDistance

```csharp
public float GravityPointUnitDistance { get; set; }
```

引擎名：`gravity_point_unit_distance`

The distance at which the gravity strength is equal to `Gravity`. For example, on a planet 100 meters in radius with a surface gravity of 4.0 m/s², set the `Gravity` to 4.0 and the unit distance to 100.0. The gravity will have falloff according to the inverse square law, so in the example, at 200 meters from the center the gravity will be 1.0 m/s² (twice the distance, 1/4th the gravity), at 50 meters it will be 16.0 m/s² (half the distance, 4x the gravity), and so on.

The above is true only when the unit distance is a positive number. When this is set to 0.0, the gravity will be constant regardless of distance.

### GravitySpaceOverride

```csharp
public Area3D.SpaceOverride GravitySpaceOverride { get; set; }
```

引擎名：`gravity_space_override`

Override mode for gravity calculations within this area.

### LinearDamp

```csharp
public float LinearDamp { get; set; }
```

引擎名：`linear_damp`

The rate at which objects stop moving in this area. Represents the linear velocity lost per second.

See `ProjectSettings.physics/3d/default_linear_damp` for more details about damping.

### LinearDampSpaceOverride

```csharp
public Area3D.SpaceOverride LinearDampSpaceOverride { get; set; }
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

The area's priority. Higher priority areas are processed first. The [`World3D`](World3D.md)'s physics is always processed last, after all areas.

### ReverbBusAmount

```csharp
public float ReverbBusAmount { get; set; }
```

引擎名：`reverb_bus_amount`

The degree to which this area applies reverb to its associated audio. Ranges from `0` to `1` with `0.1` precision.

### ReverbBusEnabled

```csharp
public bool ReverbBusEnabled { get; set; }
```

引擎名：`reverb_bus_enabled`

If `true`, the area applies reverb to its associated audio.

### ReverbBusName

```csharp
public StringName ReverbBusName { get; set; }
```

引擎名：`reverb_bus_name`

The name of the reverb bus to use for this area's associated audio.

### ReverbBusUniformity

```csharp
public float ReverbBusUniformity { get; set; }
```

引擎名：`reverb_bus_uniformity`

The degree to which this area's reverb is a uniform effect. Ranges from `0` to `1` with `0.1` precision.

### WindAttenuationFactor

```csharp
public float WindAttenuationFactor { get; set; }
```

引擎名：`wind_attenuation_factor`

The exponential rate at which wind force decreases with distance from its origin.

**Note:** This wind force only applies to [`SoftBody3D`](SoftBody3D.md) nodes. Other physics bodies are currently not affected by wind.

### WindForceMagnitude

```csharp
public float WindForceMagnitude { get; set; }
```

引擎名：`wind_force_magnitude`

The magnitude of area-specific wind force.

**Note:** This wind force only applies to [`SoftBody3D`](SoftBody3D.md) nodes. Other physics bodies are currently not affected by wind.

### WindSourcePath

```csharp
public NodePath WindSourcePath { get; set; }
```

引擎名：`wind_source_path`

The [`Node3D`](Node3D.md) which is used to specify the direction and origin of an area-specific wind force. The direction is opposite to the z-axis of the [`Node3D`](Node3D.md)'s local transform, and its origin is the origin of the [`Node3D`](Node3D.md)'s local transform.

**Note:** This wind force only applies to [`SoftBody3D`](SoftBody3D.md) nodes. Other physics bodies are currently not affected by wind.

## 方法

### GetAngularDamp

```csharp
public float GetAngularDamp()
```

引擎名：`get_angular_damp`

### GetAngularDampSpaceOverrideMode

```csharp
public Area3D.SpaceOverride GetAngularDampSpaceOverrideMode()
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
public Vector3 GetGravityDirection()
```

引擎名：`get_gravity_direction`

### GetGravityPointCenter

```csharp
public Vector3 GetGravityPointCenter()
```

引擎名：`get_gravity_point_center`

### GetGravityPointUnitDistance

```csharp
public float GetGravityPointUnitDistance()
```

引擎名：`get_gravity_point_unit_distance`

### GetGravitySpaceOverrideMode

```csharp
public Area3D.SpaceOverride GetGravitySpaceOverrideMode()
```

引擎名：`get_gravity_space_override_mode`

### GetLinearDamp

```csharp
public float GetLinearDamp()
```

引擎名：`get_linear_damp`

### GetLinearDampSpaceOverrideMode

```csharp
public Area3D.SpaceOverride GetLinearDampSpaceOverrideMode()
```

引擎名：`get_linear_damp_space_override_mode`

### GetOverlappingAreas

```csharp
public Array<Area3D> GetOverlappingAreas()
```

引擎名：`get_overlapping_areas`

Returns a list of intersecting `Area3D`s. The overlapping area's [`CollisionObject3D.CollisionLayer`](CollisionObject3D.md) must be part of this area's [`CollisionObject3D.CollisionMask`](CollisionObject3D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) this list is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

### GetOverlappingBodies

```csharp
public Array<Node3D> GetOverlappingBodies()
```

引擎名：`get_overlapping_bodies`

Returns a list of intersecting [`PhysicsBody3D`](PhysicsBody3D.md)s, [`SoftBody3D`](SoftBody3D.md)s, and [`GridMap`](GridMap.md)s. The overlapping body's [`CollisionObject3D.CollisionLayer`](CollisionObject3D.md) must be part of this area's [`CollisionObject3D.CollisionMask`](CollisionObject3D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) this list is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will not return any such bodies.

### GetPriority

```csharp
public int GetPriority()
```

引擎名：`get_priority`

### GetReverbAmount

```csharp
public float GetReverbAmount()
```

引擎名：`get_reverb_amount`

### GetReverbBusName

```csharp
public StringName GetReverbBusName()
```

引擎名：`get_reverb_bus_name`

### GetReverbUniformity

```csharp
public float GetReverbUniformity()
```

引擎名：`get_reverb_uniformity`

### GetWindAttenuationFactor

```csharp
public float GetWindAttenuationFactor()
```

引擎名：`get_wind_attenuation_factor`

### GetWindForceMagnitude

```csharp
public float GetWindForceMagnitude()
```

引擎名：`get_wind_force_magnitude`

### GetWindSourcePath

```csharp
public NodePath GetWindSourcePath()
```

引擎名：`get_wind_source_path`

### HasOverlappingAreas

```csharp
public bool HasOverlappingAreas()
```

引擎名：`has_overlapping_areas`

Returns `true` if intersecting any `Area3D`s, otherwise returns `false`. The overlapping area's [`CollisionObject3D.CollisionLayer`](CollisionObject3D.md) must be part of this area's [`CollisionObject3D.CollisionMask`](CollisionObject3D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) the list of overlapping areas is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

### HasOverlappingBodies

```csharp
public bool HasOverlappingBodies()
```

引擎名：`has_overlapping_bodies`

Returns `true` if intersecting any [`PhysicsBody3D`](PhysicsBody3D.md)s, [`SoftBody3D`](SoftBody3D.md)s, or [`GridMap`](GridMap.md)s, otherwise returns `false`. The overlapping body's [`CollisionObject3D.CollisionLayer`](CollisionObject3D.md) must be part of this area's [`CollisionObject3D.CollisionMask`](CollisionObject3D.md) in order to be detected.

For performance reasons (collisions are all processed at the same time) the list of overlapping bodies is modified once during the physics step, not immediately after objects are moved. Consider using signals instead.

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will not consider such bodies.

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

### IsUsingReverbBus

```csharp
public bool IsUsingReverbBus()
```

引擎名：`is_using_reverb_bus`

### OverlapsArea

```csharp
public bool OverlapsArea(Node area)
```

引擎名：`overlaps_area`

Returns `true` if the given `Area3D` intersects or overlaps this `Area3D`, `false` otherwise.

**Note:** The result of this test is not immediate after moving objects. For performance, list of overlaps is updated once per frame and before the physics step. Consider using signals instead.

### OverlapsBody

```csharp
public bool OverlapsBody(Node body)
```

引擎名：`overlaps_body`

Returns `true` if the given physics body intersects or overlaps this `Area3D`, `false` otherwise.

`body` argument can either be a [`PhysicsBody3D`](PhysicsBody3D.md), [`SoftBody3D`](SoftBody3D.md), or a [`GridMap`](GridMap.md) instance. While GridMaps are not physics body themselves, they register their tiles with collision shapes as a virtual physics body.

**Note:** The result of this test is not immediate after moving objects. For performance, list of overlaps is updated once per frame and before the physics step. Consider using signals instead.

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will return `false` in such cases.

### SetAngularDamp

```csharp
public void SetAngularDamp(float angularDamp)
```

引擎名：`set_angular_damp`

### SetAngularDampSpaceOverrideMode

```csharp
public void SetAngularDampSpaceOverrideMode(Area3D.SpaceOverride spaceOverrideMode)
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
public void SetGravityDirection(Vector3 direction)
```

引擎名：`set_gravity_direction`

### SetGravityIsPoint

```csharp
public void SetGravityIsPoint(bool enable)
```

引擎名：`set_gravity_is_point`

### SetGravityPointCenter

```csharp
public void SetGravityPointCenter(Vector3 center)
```

引擎名：`set_gravity_point_center`

### SetGravityPointUnitDistance

```csharp
public void SetGravityPointUnitDistance(float distanceScale)
```

引擎名：`set_gravity_point_unit_distance`

### SetGravitySpaceOverrideMode

```csharp
public void SetGravitySpaceOverrideMode(Area3D.SpaceOverride spaceOverrideMode)
```

引擎名：`set_gravity_space_override_mode`

### SetLinearDamp

```csharp
public void SetLinearDamp(float linearDamp)
```

引擎名：`set_linear_damp`

### SetLinearDampSpaceOverrideMode

```csharp
public void SetLinearDampSpaceOverrideMode(Area3D.SpaceOverride spaceOverrideMode)
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

### SetReverbAmount

```csharp
public void SetReverbAmount(float amount)
```

引擎名：`set_reverb_amount`

### SetReverbBusName

```csharp
public void SetReverbBusName(StringName name)
```

引擎名：`set_reverb_bus_name`

### SetReverbUniformity

```csharp
public void SetReverbUniformity(float amount)
```

引擎名：`set_reverb_uniformity`

### SetUseReverbBus

```csharp
public void SetUseReverbBus(bool enable)
```

引擎名：`set_use_reverb_bus`

### SetWindAttenuationFactor

```csharp
public void SetWindAttenuationFactor(float windAttenuationFactor)
```

引擎名：`set_wind_attenuation_factor`

### SetWindForceMagnitude

```csharp
public void SetWindForceMagnitude(float windForceMagnitude)
```

引擎名：`set_wind_force_magnitude`

### SetWindSourcePath

```csharp
public void SetWindSourcePath(NodePath windSourcePath)
```

引擎名：`set_wind_source_path`

## 事件（信号）

### AreaEntered

```csharp
public event Area3D.AreaEnteredEventHandler AreaEntered
```

引擎名：`area_entered`

Emitted when the received `area` enters this area. Requires `Monitoring` to be set to `true`.

### AreaExited

```csharp
public event Area3D.AreaExitedEventHandler AreaExited
```

引擎名：`area_exited`

Emitted when the received `area` exits this area. Requires `Monitoring` to be set to `true`.

### AreaShapeEntered

```csharp
public event Area3D.AreaShapeEnteredEventHandler AreaShapeEntered
```

引擎名：`area_shape_entered`

Emitted when a [`Shape3D`](Shape3D.md) of the received `area` enters a shape of this area. Requires `Monitoring` to be set to `true`.

`localShapeIndex` and `areaShapeIndex` contain indices of the interacting shapes from this area and the other area, respectively. `areaRid` contains the [`Rid`](Rid.md) of the other area. These values can be used with the [`PhysicsServer3D`](PhysicsServer3D.md).

**Example:** Get the [`CollisionShape3D`](CollisionShape3D.md) node from the shape index:

### AreaShapeExited

```csharp
public event Area3D.AreaShapeExitedEventHandler AreaShapeExited
```

引擎名：`area_shape_exited`

Emitted when a [`Shape3D`](Shape3D.md) of the received `area` exits a shape of this area. Requires `Monitoring` to be set to `true`.

See also `AreaShapeEntered`.

### BodyEntered

```csharp
public event Area3D.BodyEnteredEventHandler BodyEntered
```

引擎名：`body_entered`

Emitted when the received `body` enters this area. `body` can be a [`PhysicsBody3D`](PhysicsBody3D.md), [`SoftBody3D`](SoftBody3D.md) or [`GridMap`](GridMap.md). [`GridMap`](GridMap.md)s are detected if their [`MeshLibrary`](MeshLibrary.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will not emit this signal in such cases.

### BodyExited

```csharp
public event Area3D.BodyExitedEventHandler BodyExited
```

引擎名：`body_exited`

Emitted when the received `body` exits this area. `body` can be a [`PhysicsBody3D`](PhysicsBody3D.md), [`SoftBody3D`](SoftBody3D.md) or [`GridMap`](GridMap.md). [`GridMap`](GridMap.md)s are detected if their [`MeshLibrary`](MeshLibrary.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will not emit this signal in such cases.

### BodyShapeEntered

```csharp
public event Area3D.BodyShapeEnteredEventHandler BodyShapeEntered
```

引擎名：`body_shape_entered`

Emitted when a [`Shape3D`](Shape3D.md) of the received `body` enters a shape of this area. `body` can be a [`PhysicsBody3D`](PhysicsBody3D.md), [`SoftBody3D`](SoftBody3D.md) or [`GridMap`](GridMap.md). [`GridMap`](GridMap.md)s are detected if their [`MeshLibrary`](MeshLibrary.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

`localShapeIndex` and `bodyShapeIndex` contain indices of the interacting shapes from this area and the interacting body, respectively. `bodyRid` contains the [`Rid`](Rid.md) of the body. These values can be used with the [`PhysicsServer3D`](PhysicsServer3D.md).

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will not emit this signal in such cases.

**Example:** Get the [`CollisionShape3D`](CollisionShape3D.md) node from the shape index:

### BodyShapeExited

```csharp
public event Area3D.BodyShapeExitedEventHandler BodyShapeExited
```

引擎名：`body_shape_exited`

Emitted when a [`Shape3D`](Shape3D.md) of the received `body` exits a shape of this area. `body` can be a [`PhysicsBody3D`](PhysicsBody3D.md), [`SoftBody3D`](SoftBody3D.md) or [`GridMap`](GridMap.md). [`GridMap`](GridMap.md)s are detected if their [`MeshLibrary`](MeshLibrary.md) has collision shapes configured. Requires `Monitoring` to be set to `true`.

See also `BodyShapeEntered`.

**Note:** Godot Physics does not support reporting overlaps with [`SoftBody3D`](SoftBody3D.md), so will not emit this signal in such cases.

## AreaEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.AreaEntered`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaEnteredEventHandler(Area3D area)
```

## AreaExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.AreaExited`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaExitedEventHandler(Area3D area)
```

## AreaShapeEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.AreaShapeEntered`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaShapeEnteredEventHandler(Rid areaRid, Area3D area, long areaShapeIndex, long localShapeIndex)
```

## AreaShapeExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.AreaShapeExited`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void AreaShapeExitedEventHandler(Rid areaRid, Area3D area, long areaShapeIndex, long localShapeIndex)
```

## BodyEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.BodyEntered`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyEnteredEventHandler(Node3D body)
```

## BodyExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.BodyExited`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyExitedEventHandler(Node3D body)
```

## BodyShapeEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.BodyShapeEntered`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyShapeEnteredEventHandler(Rid bodyRid, Node3D body, long bodyShapeIndex, long localShapeIndex)
```

## BodyShapeExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Area3D.BodyShapeExited`](Area3D.md) event of a [`Area3D`](Area3D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyShapeExitedEventHandler(Rid bodyRid, Node3D body, long bodyShapeIndex, long localShapeIndex)
```

## SpaceOverride

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This area does not affect gravity/damping.
- `Combine = 1`：This area adds its gravity/damping values to whatever has been calculated so far (in [`Area3D.Priority`](Area3D.md) order).
- `CombineReplace = 2`：This area adds its gravity/damping values to whatever has been calculated so far (in [`Area3D.Priority`](Area3D.md) order), ignoring any lower priority areas.
- `Replace = 3`：This area replaces any gravity/damping, even the defaults, ignoring any lower priority areas.
- `ReplaceCombine = 4`：This area replaces any gravity/damping calculated so far (in [`Area3D.Priority`](Area3D.md) order), but keeps calculating the rest of the areas.
