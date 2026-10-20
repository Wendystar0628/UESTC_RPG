<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer3D

命名空间 `Godot`；static class

PhysicsServer3D is the server responsible for all 3D physics. It can directly create and manipulate all physics objects:

- A *space* is a self-contained world for a physics simulation. It contains bodies, areas, and joints. Its state can be queried for collision and intersection information, and several parameters of the simulation can be modified.

- A *shape* is a geometric shape such as a sphere, a box, a cylinder, or a polygon. It can be used for collision detection by adding it to a body/area, possibly with an extra transformation relative to the body/area's origin. Bodies/areas can have multiple (transformed) shapes added to them, and a single shape can be added to bodies/areas multiple times with different local transformations.

- A *body* is a physical object which can be in static, kinematic, or rigid mode. Its state (such as position and velocity) can be queried and updated. A force integration callback can be set to customize the body's physics.

- An *area* is a region in space which can be used to detect bodies and areas entering and exiting it. A body monitoring callback can be set to report entering/exiting body shapes, and similarly an area monitoring callback can be set. Gravity and damping can be overridden within the area by setting area parameters.

- A *joint* is a constraint, either between two bodies or on one body relative to a point. Parameters such as the joint bias and the rest length of a spring joint can be adjusted.

Physics objects in `PhysicsServer3D` may be created and manipulated independently; they do not have to be tied to nodes in the scene tree.

**Note:** All the 3D physics nodes use the physics server internally. Adding a physics node to the scene tree will cause a corresponding physics object to be created in the physics server. A rigid body node registers a callback that updates the node's transform with the transform of the respective body object in the physics server (every physics update). An area node registers a callback to inform the area node about overlaps with the respective area object in the physics server. The raycast node queries the direct state of the relevant space in the physics server.

## 属性

### Singleton

```csharp
public static PhysicsServer3DInstance Singleton { get; }
```

## 方法

### AreaAddShape

```csharp
public static void AreaAddShape(Rid area, Rid shape, Transform3D? transform = null, bool disabled = false)
```

引擎名：`area_add_shape`

Adds a shape to the area, along with a transform matrix. Shapes are usually referenced by their index, so you should track which shape has a given index.

参数：
- `transform`：If the parameter is null, then the default value is `Transform3D.Identity`.

### AreaAttachObjectInstanceId

```csharp
public static void AreaAttachObjectInstanceId(Rid area, ulong id)
```

引擎名：`area_attach_object_instance_id`

Assigns the area to a descendant of [`GodotObject`](GodotObject.md), so it can exist in the node tree.

### AreaClearShapes

```csharp
public static void AreaClearShapes(Rid area)
```

引擎名：`area_clear_shapes`

Removes all shapes from an area. It does not delete the shapes, so they can be reassigned later.

### AreaCreate

```csharp
public static Rid AreaCreate()
```

引擎名：`area_create`

Creates a 3D area object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and `monitorable` set to `false`.

Use `AreaAddShape()` to add shapes to it, use `AreaSetTransform()` to set its transform, and use `AreaSetSpace()` to add the area to a space. If you want the area to be detectable use `AreaSetMonitorable()`.

### AreaGetCollisionLayer

```csharp
public static uint AreaGetCollisionLayer(Rid area)
```

引擎名：`area_get_collision_layer`

Returns the physics layer or layers an area belongs to.

### AreaGetCollisionMask

```csharp
public static uint AreaGetCollisionMask(Rid area)
```

引擎名：`area_get_collision_mask`

Returns the physics layer or layers an area can contact with.

### AreaGetObjectInstanceId

```csharp
public static ulong AreaGetObjectInstanceId(Rid area)
```

引擎名：`area_get_object_instance_id`

Gets the instance ID of the object the area is assigned to.

### AreaGetParam

```csharp
public static Variant AreaGetParam(Rid area, PhysicsServer3D.AreaParameter param)
```

引擎名：`area_get_param`

Returns an area parameter value. A list of available parameters is on the [`PhysicsServer3D.AreaParameter`](PhysicsServer3D.md) constants.

### AreaGetShape

```csharp
public static Rid AreaGetShape(Rid area, int shapeIdx)
```

引擎名：`area_get_shape`

Returns the [`Rid`](Rid.md) of the nth shape of an area.

### AreaGetShapeCount

```csharp
public static int AreaGetShapeCount(Rid area)
```

引擎名：`area_get_shape_count`

Returns the number of shapes assigned to an area.

### AreaGetShapeTransform

```csharp
public static Transform3D AreaGetShapeTransform(Rid area, int shapeIdx)
```

引擎名：`area_get_shape_transform`

Returns the transform matrix of a shape within an area.

### AreaGetSpace

```csharp
public static Rid AreaGetSpace(Rid area)
```

引擎名：`area_get_space`

Returns the space assigned to the area.

### AreaGetTransform

```csharp
public static Transform3D AreaGetTransform(Rid area)
```

引擎名：`area_get_transform`

Returns the transform matrix for an area.

### AreaRemoveShape

```csharp
public static void AreaRemoveShape(Rid area, int shapeIdx)
```

引擎名：`area_remove_shape`

Removes a shape from an area. It does not delete the shape, so it can be reassigned later.

### AreaSetAreaMonitorCallback

```csharp
public static void AreaSetAreaMonitorCallback(Rid area, Callable callback)
```

引擎名：`area_set_area_monitor_callback`

Sets the area's area monitor callback. This callback will be called when any other (shape of an) area enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer3D.AreaBodyStatus.Added`](PhysicsServer3D.md) or [`PhysicsServer3D.AreaBodyStatus.Removed`](PhysicsServer3D.md) depending on whether the other area's shape entered or exited the area,

2. an [`Rid`](Rid.md) `area_rid`: the [`Rid`](Rid.md) of the other area that entered or exited the area,

3. an integer `instance_id`: the `ObjectID` attached to the other area,

4. an integer `area_shape_idx`: the index of the shape of the other area that entered or exited the area,

5. an integer `self_shape_idx`: the index of the shape of the area where the other area entered or exited.

By counting (or keeping track of) the shapes that enter and exit, it can be determined if an area (with all its shapes) is entering for the first time or exiting for the last time.

### AreaSetCollisionLayer

```csharp
public static void AreaSetCollisionLayer(Rid area, uint layer)
```

引擎名：`area_set_collision_layer`

Assigns the area to one or many physics layers.

### AreaSetCollisionMask

```csharp
public static void AreaSetCollisionMask(Rid area, uint mask)
```

引擎名：`area_set_collision_mask`

Sets which physics layers the area will monitor.

### AreaSetMonitorCallback

```csharp
public static void AreaSetMonitorCallback(Rid area, Callable callback)
```

引擎名：`area_set_monitor_callback`

Sets the area's body monitor callback. This callback will be called when any other (shape of a) body enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer3D.AreaBodyStatus.Added`](PhysicsServer3D.md) or [`PhysicsServer3D.AreaBodyStatus.Removed`](PhysicsServer3D.md) depending on whether the other body shape entered or exited the area,

2. an [`Rid`](Rid.md) `body_rid`: the [`Rid`](Rid.md) of the body that entered or exited the area,

3. an integer `instance_id`: the `ObjectID` attached to the body,

4. an integer `body_shape_idx`: the index of the shape of the body that entered or exited the area,

5. an integer `self_shape_idx`: the index of the shape of the area where the body entered or exited.

By counting (or keeping track of) the shapes that enter and exit, it can be determined if a body (with all its shapes) is entering for the first time or exiting for the last time.

### AreaSetMonitorable

```csharp
public static void AreaSetMonitorable(Rid area, bool monitorable)
```

引擎名：`area_set_monitorable`

### AreaSetParam

```csharp
public static void AreaSetParam(Rid area, PhysicsServer3D.AreaParameter param, Variant value)
```

引擎名：`area_set_param`

Sets the value for an area parameter. A list of available parameters is on the [`PhysicsServer3D.AreaParameter`](PhysicsServer3D.md) constants.

### AreaSetRayPickable

```csharp
public static void AreaSetRayPickable(Rid area, bool enable)
```

引擎名：`area_set_ray_pickable`

Sets object pickable with rays.

### AreaSetShape

```csharp
public static void AreaSetShape(Rid area, int shapeIdx, Rid shape)
```

引擎名：`area_set_shape`

Substitutes a given area shape by another. The old shape is selected by its index, the new one by its [`Rid`](Rid.md).

### AreaSetShapeDisabled

```csharp
public static void AreaSetShapeDisabled(Rid area, int shapeIdx, bool disabled)
```

引擎名：`area_set_shape_disabled`

### AreaSetShapeTransform

```csharp
public static void AreaSetShapeTransform(Rid area, int shapeIdx, Transform3D transform)
```

引擎名：`area_set_shape_transform`

Sets the transform matrix for an area shape.

### AreaSetSpace

```csharp
public static void AreaSetSpace(Rid area, Rid space)
```

引擎名：`area_set_space`

Assigns a space to the area.

### AreaSetTransform

```csharp
public static void AreaSetTransform(Rid area, Transform3D transform)
```

引擎名：`area_set_transform`

Sets the transform matrix for an area.

### BodyAddCollisionException

```csharp
public static void BodyAddCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`body_add_collision_exception`

Adds a body to the list of bodies exempt from collisions.

### BodyAddConstantCentralForce

```csharp
public static void BodyAddConstantCentralForce(Rid body, Vector3 force)
```

引擎名：`body_add_constant_central_force`

Adds a constant directional force without affecting rotation that keeps being applied over time until cleared with `body_set_constant_force(body, Vector3(0, 0, 0))`.

This is equivalent to using `BodyAddConstantForce()` at the body's center of mass.

### BodyAddConstantForce

```csharp
public static void BodyAddConstantForce(Rid body, Vector3 force, Vector3? position = null)
```

引擎名：`body_add_constant_force`

Adds a constant positioned force to the body that keeps being applied over time until cleared with `body_set_constant_force(body, Vector3(0, 0, 0))`.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### BodyAddConstantTorque

```csharp
public static void BodyAddConstantTorque(Rid body, Vector3 torque)
```

引擎名：`body_add_constant_torque`

Adds a constant rotational force without affecting position that keeps being applied over time until cleared with `body_set_constant_torque(body, Vector3(0, 0, 0))`.

### BodyAddShape

```csharp
public static void BodyAddShape(Rid body, Rid shape, Transform3D? transform = null, bool disabled = false)
```

引擎名：`body_add_shape`

Adds a shape to the body, along with a transform matrix. Shapes are usually referenced by their index, so you should track which shape has a given index.

参数：
- `transform`：If the parameter is null, then the default value is `Transform3D.Identity`.

### BodyApplyCentralForce

```csharp
public static void BodyApplyCentralForce(Rid body, Vector3 force)
```

引擎名：`body_apply_central_force`

Applies a directional force without affecting rotation. A force is time dependent and meant to be applied every physics update.

This is equivalent to using `BodyApplyForce()` at the body's center of mass.

### BodyApplyCentralImpulse

```csharp
public static void BodyApplyCentralImpulse(Rid body, Vector3 impulse)
```

引擎名：`body_apply_central_impulse`

Applies a directional impulse without affecting rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

This is equivalent to using `BodyApplyImpulse()` at the body's center of mass.

### BodyApplyForce

```csharp
public static void BodyApplyForce(Rid body, Vector3 force, Vector3? position = null)
```

引擎名：`body_apply_force`

Applies a positioned force to the body. A force is time dependent and meant to be applied every physics update.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### BodyApplyImpulse

```csharp
public static void BodyApplyImpulse(Rid body, Vector3 impulse, Vector3? position = null)
```

引擎名：`body_apply_impulse`

Applies a positioned impulse to the body.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### BodyApplyTorque

```csharp
public static void BodyApplyTorque(Rid body, Vector3 torque)
```

引擎名：`body_apply_torque`

Applies a rotational force without affecting position. A force is time dependent and meant to be applied every physics update.

### BodyApplyTorqueImpulse

```csharp
public static void BodyApplyTorqueImpulse(Rid body, Vector3 impulse)
```

引擎名：`body_apply_torque_impulse`

Applies a rotational impulse to the body without affecting the position.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### BodyAttachObjectInstanceId

```csharp
public static void BodyAttachObjectInstanceId(Rid body, ulong id)
```

引擎名：`body_attach_object_instance_id`

Assigns the area to a descendant of [`GodotObject`](GodotObject.md), so it can exist in the node tree.

### BodyClearShapes

```csharp
public static void BodyClearShapes(Rid body)
```

引擎名：`body_clear_shapes`

Removes all shapes from a body.

### BodyCreate

```csharp
public static Rid BodyCreate()
```

引擎名：`body_create`

Creates a 3D body object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and body mode set to [`PhysicsServer3D.BodyMode.Rigid`](PhysicsServer3D.md).

Use `BodyAddShape()` to add shapes to it, use `BodySetState()` to set its transform, and use `BodySetSpace()` to add the body to a space.

### BodyGetCollisionLayer

```csharp
public static uint BodyGetCollisionLayer(Rid body)
```

引擎名：`body_get_collision_layer`

Returns the physics layer or layers a body belongs to.

### BodyGetCollisionMask

```csharp
public static uint BodyGetCollisionMask(Rid body)
```

引擎名：`body_get_collision_mask`

Returns the physics layer or layers a body can collide with.

### BodyGetCollisionPriority

```csharp
public static float BodyGetCollisionPriority(Rid body)
```

引擎名：`body_get_collision_priority`

Returns the body's collision priority.

### BodyGetConstantForce

```csharp
public static Vector3 BodyGetConstantForce(Rid body)
```

引擎名：`body_get_constant_force`

Returns the body's total constant positional forces applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodyGetConstantTorque

```csharp
public static Vector3 BodyGetConstantTorque(Rid body)
```

引擎名：`body_get_constant_torque`

Returns the body's total constant rotational forces applied during each physics update.

See `BodyAddConstantTorque()`.

### BodyGetDirectState

```csharp
public static PhysicsDirectBodyState3D BodyGetDirectState(Rid body)
```

引擎名：`body_get_direct_state`

Returns the [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md) of the body. Returns `null` if the body is destroyed or removed from the physics space.

### BodyGetMaxContactsReported

```csharp
public static int BodyGetMaxContactsReported(Rid body)
```

引擎名：`body_get_max_contacts_reported`

Returns the maximum contacts that can be reported. See `BodySetMaxContactsReported()`.

### BodyGetMode

```csharp
public static PhysicsServer3D.BodyMode BodyGetMode(Rid body)
```

引擎名：`body_get_mode`

Returns the body mode.

### BodyGetObjectInstanceId

```csharp
public static ulong BodyGetObjectInstanceId(Rid body)
```

引擎名：`body_get_object_instance_id`

Gets the instance ID of the object the area is assigned to.

### BodyGetParam

```csharp
public static Variant BodyGetParam(Rid body, PhysicsServer3D.BodyParameter param)
```

引擎名：`body_get_param`

Returns the value of a body parameter. A list of available parameters is on the [`PhysicsServer3D.BodyParameter`](PhysicsServer3D.md) constants.

### BodyGetShape

```csharp
public static Rid BodyGetShape(Rid body, int shapeIdx)
```

引擎名：`body_get_shape`

Returns the [`Rid`](Rid.md) of the nth shape of a body.

### BodyGetShapeCount

```csharp
public static int BodyGetShapeCount(Rid body)
```

引擎名：`body_get_shape_count`

Returns the number of shapes assigned to a body.

### BodyGetShapeTransform

```csharp
public static Transform3D BodyGetShapeTransform(Rid body, int shapeIdx)
```

引擎名：`body_get_shape_transform`

Returns the transform matrix of a body shape.

### BodyGetSpace

```csharp
public static Rid BodyGetSpace(Rid body)
```

引擎名：`body_get_space`

Returns the [`Rid`](Rid.md) of the space assigned to a body.

### BodyGetState

```csharp
public static Variant BodyGetState(Rid body, PhysicsServer3D.BodyState state)
```

引擎名：`body_get_state`

Returns a body state.

### BodyIsAxisLocked

```csharp
public static bool BodyIsAxisLocked(Rid body, PhysicsServer3D.BodyAxis axis)
```

引擎名：`body_is_axis_locked`

### BodyIsContinuousCollisionDetectionEnabled

```csharp
public static bool BodyIsContinuousCollisionDetectionEnabled(Rid body)
```

引擎名：`body_is_continuous_collision_detection_enabled`

If `true`, the continuous collision detection mode is enabled.

### BodyIsOmittingForceIntegration

```csharp
public static bool BodyIsOmittingForceIntegration(Rid body)
```

引擎名：`body_is_omitting_force_integration`

Returns `true` if the body is omitting the standard force integration. See `BodySetOmitForceIntegration()`.

### BodyRemoveCollisionException

```csharp
public static void BodyRemoveCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`body_remove_collision_exception`

Removes a body from the list of bodies exempt from collisions.

Continuous collision detection tries to predict where a moving body will collide, instead of moving it and correcting its movement if it collided.

### BodyRemoveShape

```csharp
public static void BodyRemoveShape(Rid body, int shapeIdx)
```

引擎名：`body_remove_shape`

Removes a shape from a body. The shape is not deleted, so it can be reused afterwards.

### BodyResetMassProperties

```csharp
public static void BodyResetMassProperties(Rid body)
```

引擎名：`body_reset_mass_properties`

Restores the default inertia and center of mass based on shapes to cancel any custom values previously set using `BodySetParam()`.

### BodySetAxisLock

```csharp
public static void BodySetAxisLock(Rid body, PhysicsServer3D.BodyAxis axis, bool lock)
```

引擎名：`body_set_axis_lock`

### BodySetAxisVelocity

```csharp
public static void BodySetAxisVelocity(Rid body, Vector3 axisVelocity)
```

引擎名：`body_set_axis_velocity`

Sets an axis velocity. The velocity in the given vector axis will be set as the given vector length. This is useful for jumping behavior.

### BodySetCollisionLayer

```csharp
public static void BodySetCollisionLayer(Rid body, uint layer)
```

引擎名：`body_set_collision_layer`

Sets the physics layer or layers a body belongs to.

### BodySetCollisionMask

```csharp
public static void BodySetCollisionMask(Rid body, uint mask)
```

引擎名：`body_set_collision_mask`

Sets the physics layer or layers a body can collide with.

### BodySetCollisionPriority

```csharp
public static void BodySetCollisionPriority(Rid body, float priority)
```

引擎名：`body_set_collision_priority`

Sets the body's collision priority.

### BodySetConstantForce

```csharp
public static void BodySetConstantForce(Rid body, Vector3 force)
```

引擎名：`body_set_constant_force`

Sets the body's total constant positional forces applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodySetConstantTorque

```csharp
public static void BodySetConstantTorque(Rid body, Vector3 torque)
```

引擎名：`body_set_constant_torque`

Sets the body's total constant rotational forces applied during each physics update.

See `BodyAddConstantTorque()`.

### BodySetEnableContinuousCollisionDetection

```csharp
public static void BodySetEnableContinuousCollisionDetection(Rid body, bool enable)
```

引擎名：`body_set_enable_continuous_collision_detection`

If `true`, the continuous collision detection mode is enabled.

Continuous collision detection tries to predict where a moving body will collide, instead of moving it and correcting its movement if it collided.

### BodySetForceIntegrationCallback

```csharp
public static void BodySetForceIntegrationCallback(Rid body, Callable callable, Variant userdata = default)
```

引擎名：`body_set_force_integration_callback`

Sets the body's custom force integration callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the custom callback.

The function `callable` will be called every physics tick, before the standard force integration (see `BodySetOmitForceIntegration()`). It can be used for example to update the body's linear and angular velocity based on contact with other bodies.

If `userdata` is not `null`, the function `callable` must take the following two parameters:

1. `state`: a [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md), used to retrieve and modify the body's state,

2. `userdata`: a [`Variant`](Variant.md); its value will be the `userdata` passed into this method.

If `userdata` is `null`, then `callable` must take only the `state` parameter.

### BodySetMaxContactsReported

```csharp
public static void BodySetMaxContactsReported(Rid body, int amount)
```

引擎名：`body_set_max_contacts_reported`

Sets the maximum contacts to report. Bodies can keep a log of the contacts with other bodies. This is enabled by setting the maximum number of contacts reported to a number greater than 0.

### BodySetMode

```csharp
public static void BodySetMode(Rid body, PhysicsServer3D.BodyMode mode)
```

引擎名：`body_set_mode`

Sets the body mode.

### BodySetOmitForceIntegration

```csharp
public static void BodySetOmitForceIntegration(Rid body, bool enable)
```

引擎名：`body_set_omit_force_integration`

Sets whether the body omits the standard force integration. If `enable` is `true`, the body will not automatically use applied forces, torques, and damping to update the body's linear and angular velocity. In this case, `BodySetForceIntegrationCallback()` can be used to manually update the linear and angular velocity instead.

This method is called when the property [`RigidBody3D.CustomIntegrator`](RigidBody3D.md) is set.

### BodySetParam

```csharp
public static void BodySetParam(Rid body, PhysicsServer3D.BodyParameter param, Variant value)
```

引擎名：`body_set_param`

Sets a body parameter. A list of available parameters is on the [`PhysicsServer3D.BodyParameter`](PhysicsServer3D.md) constants.

### BodySetRayPickable

```csharp
public static void BodySetRayPickable(Rid body, bool enable)
```

引擎名：`body_set_ray_pickable`

Sets the body pickable with rays if `enable` is set.

### BodySetShape

```csharp
public static void BodySetShape(Rid body, int shapeIdx, Rid shape)
```

引擎名：`body_set_shape`

Substitutes a given body shape by another. The old shape is selected by its index, the new one by its [`Rid`](Rid.md).

### BodySetShapeDisabled

```csharp
public static void BodySetShapeDisabled(Rid body, int shapeIdx, bool disabled)
```

引擎名：`body_set_shape_disabled`

### BodySetShapeTransform

```csharp
public static void BodySetShapeTransform(Rid body, int shapeIdx, Transform3D transform)
```

引擎名：`body_set_shape_transform`

Sets the transform matrix for a body shape.

### BodySetSpace

```csharp
public static void BodySetSpace(Rid body, Rid space)
```

引擎名：`body_set_space`

Assigns a space to the body (see `SpaceCreate()`).

### BodySetState

```csharp
public static void BodySetState(Rid body, PhysicsServer3D.BodyState state, Variant value)
```

引擎名：`body_set_state`

Sets a body state.

### BodySetStateSyncCallback

```csharp
public static void BodySetStateSyncCallback(Rid body, Callable callable)
```

引擎名：`body_set_state_sync_callback`

Sets the body's state synchronization callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the callback.

The function `callable` will be called every physics frame, assuming that the body was active during the previous physics tick, and can be used to fetch the latest state from the physics server.

The function `callable` must take the following parameters:

1. `state`: a [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md), used to retrieve the body's state.

### BodyTestMotion

```csharp
public static bool BodyTestMotion(Rid body, PhysicsTestMotionParameters3D parameters, PhysicsTestMotionResult3D result = null)
```

引擎名：`body_test_motion`

Returns `true` if a collision would result from moving along a motion vector from a given point in space. [`PhysicsTestMotionParameters3D`](PhysicsTestMotionParameters3D.md) is passed to set motion parameters. [`PhysicsTestMotionResult3D`](PhysicsTestMotionResult3D.md) can be passed to return additional information.

### BoxShapeCreate

```csharp
public static Rid BoxShapeCreate()
```

引擎名：`box_shape_create`

Creates a 3D box shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the box's half-extents.

### CapsuleShapeCreate

```csharp
public static Rid CapsuleShapeCreate()
```

引擎名：`capsule_shape_create`

Creates a 3D capsule shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the capsule's height and radius.

### ConcavePolygonShapeCreate

```csharp
public static Rid ConcavePolygonShapeCreate()
```

引擎名：`concave_polygon_shape_create`

Creates a 3D concave polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the concave polygon's triangles.

### ConeTwistJointGetParam

```csharp
public static float ConeTwistJointGetParam(Rid joint, PhysicsServer3D.ConeTwistJointParam param)
```

引擎名：`cone_twist_joint_get_param`

Gets a cone twist joint parameter.

### ConeTwistJointSetParam

```csharp
public static void ConeTwistJointSetParam(Rid joint, PhysicsServer3D.ConeTwistJointParam param, float value)
```

引擎名：`cone_twist_joint_set_param`

Sets a cone twist joint parameter.

### ConvexPolygonShapeCreate

```csharp
public static Rid ConvexPolygonShapeCreate()
```

引擎名：`convex_polygon_shape_create`

Creates a 3D convex polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the convex polygon's points.

### CustomShapeCreate

```csharp
public static Rid CustomShapeCreate()
```

引擎名：`custom_shape_create`

Creates a custom shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's data.

**Note:** Custom shapes are not supported by the built-in physics servers, so calling this method always produces an error when using Godot Physics or Jolt Physics. Custom physics servers implemented as GDExtensions may support a custom shape.

### CylinderShapeCreate

```csharp
public static Rid CylinderShapeCreate()
```

引擎名：`cylinder_shape_create`

Creates a 3D cylinder shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the cylinder's height and radius.

### FreeRid

```csharp
public static void FreeRid(Rid rid)
```

引擎名：`free_rid`

Destroys any of the objects created by PhysicsServer3D. If the [`Rid`](Rid.md) passed is not one of the objects that can be created by PhysicsServer3D, an error will be sent to the console.

### Generic6DofJointGetFlag

```csharp
public static bool Generic6DofJointGetFlag(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisFlag flag)
```

Returns the value of a generic 6DOF joint flag.

### Generic6DofJointGetParam

```csharp
public static float Generic6DofJointGetParam(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisParam param)
```

Returns the value of a generic 6DOF joint parameter.

### Generic6DofJointSetFlag

```csharp
public static void Generic6DofJointSetFlag(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisFlag flag, bool enable)
```

Sets the value of a given generic 6DOF joint flag.

### Generic6DofJointSetParam

```csharp
public static void Generic6DofJointSetParam(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisParam param, float value)
```

Sets the value of a given generic 6DOF joint parameter.

### GetProcessInfo

```csharp
public static int GetProcessInfo(PhysicsServer3D.ProcessInfo processInfo)
```

引擎名：`get_process_info`

Returns the value of a physics engine state specified by `processInfo`.

### HeightmapShapeCreate

```csharp
public static Rid HeightmapShapeCreate()
```

引擎名：`heightmap_shape_create`

Creates a 3D heightmap shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the heightmap's data.

### HingeJointGetFlag

```csharp
public static bool HingeJointGetFlag(Rid joint, PhysicsServer3D.HingeJointFlag flag)
```

引擎名：`hinge_joint_get_flag`

Gets a hinge joint flag.

### HingeJointGetParam

```csharp
public static float HingeJointGetParam(Rid joint, PhysicsServer3D.HingeJointParam param)
```

引擎名：`hinge_joint_get_param`

Gets a hinge joint parameter.

### HingeJointSetFlag

```csharp
public static void HingeJointSetFlag(Rid joint, PhysicsServer3D.HingeJointFlag flag, bool enabled)
```

引擎名：`hinge_joint_set_flag`

Sets a hinge joint flag.

### HingeJointSetParam

```csharp
public static void HingeJointSetParam(Rid joint, PhysicsServer3D.HingeJointParam param, float value)
```

引擎名：`hinge_joint_set_param`

Sets a hinge joint parameter.

### JointClear

```csharp
public static void JointClear(Rid joint)
```

引擎名：`joint_clear`

### JointCreate

```csharp
public static Rid JointCreate()
```

引擎名：`joint_create`

### JointDisableCollisionsBetweenBodies

```csharp
public static void JointDisableCollisionsBetweenBodies(Rid joint, bool disable)
```

引擎名：`joint_disable_collisions_between_bodies`

Sets whether the bodies attached to the [`Joint3D`](Joint3D.md) will collide with each other.

### JointGetSolverPriority

```csharp
public static int JointGetSolverPriority(Rid joint)
```

引擎名：`joint_get_solver_priority`

Gets the priority value of the Joint3D.

**Note:** Only supported when using GodotPhysics3D. This method always returns `1` when using Jolt Physics, as it does not support joint solver priority.

### JointGetType

```csharp
public static PhysicsServer3D.JointType JointGetType(Rid joint)
```

引擎名：`joint_get_type`

Returns the type of the Joint3D.

### JointIsDisabledCollisionsBetweenBodies

```csharp
public static bool JointIsDisabledCollisionsBetweenBodies(Rid joint)
```

引擎名：`joint_is_disabled_collisions_between_bodies`

Returns whether the bodies attached to the [`Joint3D`](Joint3D.md) will collide with each other.

### JointMakeConeTwist

```csharp
public static void JointMakeConeTwist(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

引擎名：`joint_make_cone_twist`

### JointMakeGeneric6Dof

```csharp
public static void JointMakeGeneric6Dof(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

Make the joint a generic six degrees of freedom (6DOF) joint. Use `Generic6DofJointSetFlag()` and `Generic6DofJointSetParam()` to set the joint's flags and parameters respectively.

### JointMakeHinge

```csharp
public static void JointMakeHinge(Rid joint, Rid bodyA, Transform3D hingeA, Rid bodyB, Transform3D hingeB)
```

引擎名：`joint_make_hinge`

### JointMakePin

```csharp
public static void JointMakePin(Rid joint, Rid bodyA, Vector3 localA, Rid bodyB, Vector3 localB)
```

引擎名：`joint_make_pin`

### JointMakeSlider

```csharp
public static void JointMakeSlider(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

引擎名：`joint_make_slider`

### JointSetSolverPriority

```csharp
public static void JointSetSolverPriority(Rid joint, int priority)
```

引擎名：`joint_set_solver_priority`

Sets the priority value of the Joint3D.

**Note:** Only supported when using GodotPhysics3D. This method has no effect when using Jolt Physics, as it does not support joint solver priority.

### PinJointGetLocalA

```csharp
public static Vector3 PinJointGetLocalA(Rid joint)
```

引擎名：`pin_joint_get_local_a`

Returns position of the joint in the local space of body a of the joint.

### PinJointGetLocalB

```csharp
public static Vector3 PinJointGetLocalB(Rid joint)
```

引擎名：`pin_joint_get_local_b`

Returns position of the joint in the local space of body b of the joint.

### PinJointGetParam

```csharp
public static float PinJointGetParam(Rid joint, PhysicsServer3D.PinJointParam param)
```

引擎名：`pin_joint_get_param`

Gets a pin joint parameter.

### PinJointSetLocalA

```csharp
public static void PinJointSetLocalA(Rid joint, Vector3 localA)
```

引擎名：`pin_joint_set_local_a`

Sets position of the joint in the local space of body a of the joint.

### PinJointSetLocalB

```csharp
public static void PinJointSetLocalB(Rid joint, Vector3 localB)
```

引擎名：`pin_joint_set_local_b`

Sets position of the joint in the local space of body b of the joint.

### PinJointSetParam

```csharp
public static void PinJointSetParam(Rid joint, PhysicsServer3D.PinJointParam param, float value)
```

引擎名：`pin_joint_set_param`

Sets a pin joint parameter.

### SeparationRayShapeCreate

```csharp
public static Rid SeparationRayShapeCreate()
```

引擎名：`separation_ray_shape_create`

Creates a 3D separation ray shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's `length` and `slide_on_slope` properties.

### SetActive

```csharp
public static void SetActive(bool active)
```

引擎名：`set_active`

Activates or deactivates the 3D physics engine.

### ShapeGetData

```csharp
public static Variant ShapeGetData(Rid shape)
```

引擎名：`shape_get_data`

Returns the shape data that configures the shape, such as the half-extents of a box or the triangles of a concave (trimesh) shape. See `ShapeSetData()` for the precise format of this data in each case.

### ShapeGetMargin

```csharp
public static float ShapeGetMargin(Rid shape)
```

引擎名：`shape_get_margin`

Returns the collision margin for the shape.

**Note:** This is not used in Godot Physics, so will always return `0`.

### ShapeGetType

```csharp
public static PhysicsServer3D.ShapeType ShapeGetType(Rid shape)
```

引擎名：`shape_get_type`

Returns the shape's type.

### ShapeSetData

```csharp
public static void ShapeSetData(Rid shape, Variant data)
```

引擎名：`shape_set_data`

Sets the shape data that configures the shape. The `data` to be passed depends on the shape's type (see `ShapeGetType()`):

- [`PhysicsServer3D.ShapeType.WorldBoundary`](PhysicsServer3D.md): a [`Plane`](Plane.md),

- [`PhysicsServer3D.ShapeType.SeparationRay`](PhysicsServer3D.md): a dictionary containing the key `"length"` with a `System.Single` value and the key `"slide_on_slope"` with a `System.Boolean` value,

- [`PhysicsServer3D.ShapeType.Sphere`](PhysicsServer3D.md): a `System.Single` that is the radius of the sphere,

- [`PhysicsServer3D.ShapeType.Box`](PhysicsServer3D.md): a [`Vector3`](Vector3.md) containing the half-extents of the box,

- [`PhysicsServer3D.ShapeType.Capsule`](PhysicsServer3D.md): a dictionary containing the keys `"height"` and `"radius"` with `System.Single` values,

- [`PhysicsServer3D.ShapeType.Cylinder`](PhysicsServer3D.md): a dictionary containing the keys `"height"` and `"radius"` with `System.Single` values,

- [`PhysicsServer3D.ShapeType.ConvexPolygon`](PhysicsServer3D.md): a [`Vector3`](Vector3.md)[] of points defining a convex polygon (the shape will be the convex hull of the points),

- [`PhysicsServer3D.ShapeType.ConcavePolygon`](PhysicsServer3D.md): a dictionary containing the key `"faces"` with a [`Vector3`](Vector3.md)[] value (with a length divisible by 3, so that each 3-tuple of points forms a face) and the key `"backface_collision"` with a `System.Boolean` value,

- [`PhysicsServer3D.ShapeType.Heightmap`](PhysicsServer3D.md): a dictionary containing the keys `"width"` and `"depth"` with `System.Int32` values, and the key `"heights"` with a value that is a packed array of `System.Single`s of length `width * depth` (that is a `System.Single`[], or a `System.Double`[] if Godot was compiled with the `precision=double` option), and optionally the keys `"min_height"` and `"max_height"` with `System.Single` values,

- [`PhysicsServer3D.ShapeType.SoftBody`](PhysicsServer3D.md): the input `data` is ignored and this method has no effect,

- [`PhysicsServer3D.ShapeType.Custom`](PhysicsServer3D.md): the input `data` is interpreted by a custom physics server, if it supports custom shapes.

### ShapeSetMargin

```csharp
public static void ShapeSetMargin(Rid shape, float margin)
```

引擎名：`shape_set_margin`

Sets the collision margin for the shape.

**Note:** This is not used in Godot Physics.

### SliderJointGetParam

```csharp
public static float SliderJointGetParam(Rid joint, PhysicsServer3D.SliderJointParam param)
```

引擎名：`slider_joint_get_param`

Gets a slider joint parameter.

### SliderJointSetParam

```csharp
public static void SliderJointSetParam(Rid joint, PhysicsServer3D.SliderJointParam param, float value)
```

引擎名：`slider_joint_set_param`

Gets a slider joint parameter.

### SoftBodyAddCollisionException

```csharp
public static void SoftBodyAddCollisionException(Rid body, Rid bodyB)
```

引擎名：`soft_body_add_collision_exception`

Adds the given body to the list of bodies exempt from collisions.

### SoftBodyApplyCentralForce

```csharp
public static void SoftBodyApplyCentralForce(Rid body, Vector3 force)
```

引擎名：`soft_body_apply_central_force`

Distributes and applies a force to all points. A force is time dependent and meant to be applied every physics update.

### SoftBodyApplyCentralImpulse

```csharp
public static void SoftBodyApplyCentralImpulse(Rid body, Vector3 impulse)
```

引擎名：`soft_body_apply_central_impulse`

Distributes and applies an impulse to all points.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### SoftBodyApplyPointForce

```csharp
public static void SoftBodyApplyPointForce(Rid body, int pointIndex, Vector3 force)
```

引擎名：`soft_body_apply_point_force`

Applies a force to a point. A force is time dependent and meant to be applied every physics update.

### SoftBodyApplyPointImpulse

```csharp
public static void SoftBodyApplyPointImpulse(Rid body, int pointIndex, Vector3 impulse)
```

引擎名：`soft_body_apply_point_impulse`

Applies an impulse to a point.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### SoftBodyCreate

```csharp
public static Rid SoftBodyCreate()
```

引擎名：`soft_body_create`

Creates a new soft body and returns its internal [`Rid`](Rid.md).

### SoftBodyGetBounds

```csharp
public static Aabb SoftBodyGetBounds(Rid body)
```

引擎名：`soft_body_get_bounds`

Returns the bounds of the given soft body in global coordinates.

### SoftBodyGetCollisionLayer

```csharp
public static uint SoftBodyGetCollisionLayer(Rid body)
```

引擎名：`soft_body_get_collision_layer`

Returns the physics layer or layers that the given soft body belongs to.

### SoftBodyGetCollisionMask

```csharp
public static uint SoftBodyGetCollisionMask(Rid body)
```

引擎名：`soft_body_get_collision_mask`

Returns the physics layer or layers that the given soft body can collide with.

### SoftBodyGetDampingCoefficient

```csharp
public static float SoftBodyGetDampingCoefficient(Rid body)
```

引擎名：`soft_body_get_damping_coefficient`

Returns the damping coefficient of the given soft body.

### SoftBodyGetDragCoefficient

```csharp
public static float SoftBodyGetDragCoefficient(Rid body)
```

引擎名：`soft_body_get_drag_coefficient`

Returns the drag coefficient of the given soft body.

### SoftBodyGetLinearStiffness

```csharp
public static float SoftBodyGetLinearStiffness(Rid body)
```

引擎名：`soft_body_get_linear_stiffness`

Returns the linear stiffness of the given soft body.

### SoftBodyGetPointGlobalPosition

```csharp
public static Vector3 SoftBodyGetPointGlobalPosition(Rid body, int pointIndex)
```

引擎名：`soft_body_get_point_global_position`

Returns the current position of the given soft body point in global coordinates.

### SoftBodyGetPressureCoefficient

```csharp
public static float SoftBodyGetPressureCoefficient(Rid body)
```

引擎名：`soft_body_get_pressure_coefficient`

Returns the pressure coefficient of the given soft body.

### SoftBodyGetShrinkingFactor

```csharp
public static float SoftBodyGetShrinkingFactor(Rid body)
```

引擎名：`soft_body_get_shrinking_factor`

Returns the shrinking factor of the given soft body.

### SoftBodyGetSimulationPrecision

```csharp
public static int SoftBodyGetSimulationPrecision(Rid body)
```

引擎名：`soft_body_get_simulation_precision`

Returns the simulation precision of the given soft body.

### SoftBodyGetSpace

```csharp
public static Rid SoftBodyGetSpace(Rid body)
```

引擎名：`soft_body_get_space`

Returns the [`Rid`](Rid.md) of the space assigned to the given soft body.

### SoftBodyGetState

```csharp
public static Variant SoftBodyGetState(Rid body, PhysicsServer3D.BodyState state)
```

引擎名：`soft_body_get_state`

Returns the given soft body state.

**Note:** Godot's default physics implementation does not support [`PhysicsServer3D.BodyState.LinearVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.AngularVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.Sleeping`](PhysicsServer3D.md), or [`PhysicsServer3D.BodyState.CanSleep`](PhysicsServer3D.md).

### SoftBodyGetTotalMass

```csharp
public static float SoftBodyGetTotalMass(Rid body)
```

引擎名：`soft_body_get_total_mass`

Returns the total mass assigned to the given soft body.

### SoftBodyIsPointPinned

```csharp
public static bool SoftBodyIsPointPinned(Rid body, int pointIndex)
```

引擎名：`soft_body_is_point_pinned`

Returns whether the given soft body point is pinned.

### SoftBodyMovePoint

```csharp
public static void SoftBodyMovePoint(Rid body, int pointIndex, Vector3 globalPosition)
```

引擎名：`soft_body_move_point`

Moves the given soft body point to a position in global coordinates.

### SoftBodyPinPoint

```csharp
public static void SoftBodyPinPoint(Rid body, int pointIndex, bool pin)
```

引擎名：`soft_body_pin_point`

Pins or unpins the given soft body point based on the value of `pin`.

**Note:** Pinning a point effectively makes it kinematic, preventing it from being affected by forces, but you can still move it using `SoftBodyMovePoint()`.

### SoftBodyRemoveAllPinnedPoints

```csharp
public static void SoftBodyRemoveAllPinnedPoints(Rid body)
```

引擎名：`soft_body_remove_all_pinned_points`

Unpins all points of the given soft body.

### SoftBodyRemoveCollisionException

```csharp
public static void SoftBodyRemoveCollisionException(Rid body, Rid bodyB)
```

引擎名：`soft_body_remove_collision_exception`

Removes the given body from the list of bodies exempt from collisions.

### SoftBodySetCollisionLayer

```csharp
public static void SoftBodySetCollisionLayer(Rid body, uint layer)
```

引擎名：`soft_body_set_collision_layer`

Sets the physics layer or layers the given soft body belongs to.

### SoftBodySetCollisionMask

```csharp
public static void SoftBodySetCollisionMask(Rid body, uint mask)
```

引擎名：`soft_body_set_collision_mask`

Sets the physics layer or layers the given soft body can collide with.

### SoftBodySetDampingCoefficient

```csharp
public static void SoftBodySetDampingCoefficient(Rid body, float dampingCoefficient)
```

引擎名：`soft_body_set_damping_coefficient`

Sets the damping coefficient of the given soft body. Higher values will slow down the body more noticeably when forces are applied.

### SoftBodySetDragCoefficient

```csharp
public static void SoftBodySetDragCoefficient(Rid body, float dragCoefficient)
```

引擎名：`soft_body_set_drag_coefficient`

Sets the drag coefficient of the given soft body. Higher values increase this body's air resistance.

**Note:** This value is currently unused by Godot's default physics implementation.

### SoftBodySetLinearStiffness

```csharp
public static void SoftBodySetLinearStiffness(Rid body, float stiffness)
```

引擎名：`soft_body_set_linear_stiffness`

Sets the linear stiffness of the given soft body. Higher values will result in a stiffer body, while lower values will increase the body's ability to bend. The value can be between `0.0` and `1.0` (inclusive).

### SoftBodySetMesh

```csharp
public static void SoftBodySetMesh(Rid body, Rid mesh)
```

引擎名：`soft_body_set_mesh`

Sets the mesh of the given soft body.

### SoftBodySetPressureCoefficient

```csharp
public static void SoftBodySetPressureCoefficient(Rid body, float pressureCoefficient)
```

引擎名：`soft_body_set_pressure_coefficient`

Sets the pressure coefficient of the given soft body. Simulates pressure build-up from inside this body. Higher values increase the strength of this effect.

### SoftBodySetRayPickable

```csharp
public static void SoftBodySetRayPickable(Rid body, bool enable)
```

引擎名：`soft_body_set_ray_pickable`

Sets whether the given soft body will be pickable when using object picking.

### SoftBodySetShrinkingFactor

```csharp
public static void SoftBodySetShrinkingFactor(Rid body, float shrinkingFactor)
```

引擎名：`soft_body_set_shrinking_factor`

Sets the shrinking factor of the given soft body.

### SoftBodySetSimulationPrecision

```csharp
public static void SoftBodySetSimulationPrecision(Rid body, int simulationPrecision)
```

引擎名：`soft_body_set_simulation_precision`

Sets the simulation precision of the given soft body. Increasing this value will improve the resulting simulation, but can affect performance. Use with care.

### SoftBodySetSpace

```csharp
public static void SoftBodySetSpace(Rid body, Rid space)
```

引擎名：`soft_body_set_space`

Assigns a space to the given soft body (see `SpaceCreate()`).

### SoftBodySetState

```csharp
public static void SoftBodySetState(Rid body, PhysicsServer3D.BodyState state, Variant variant)
```

引擎名：`soft_body_set_state`

Sets the given body state for the given body.

**Note:** Godot's default physics implementation does not support [`PhysicsServer3D.BodyState.LinearVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.AngularVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.Sleeping`](PhysicsServer3D.md), or [`PhysicsServer3D.BodyState.CanSleep`](PhysicsServer3D.md).

### SoftBodySetTotalMass

```csharp
public static void SoftBodySetTotalMass(Rid body, float totalMass)
```

引擎名：`soft_body_set_total_mass`

Sets the total mass for the given soft body.

### SoftBodySetTransform

```csharp
public static void SoftBodySetTransform(Rid body, Transform3D transform)
```

引擎名：`soft_body_set_transform`

Sets the global transform of the given soft body.

### SoftBodyUpdateRenderingServer

```csharp
public static void SoftBodyUpdateRenderingServer(Rid body, PhysicsServer3DRenderingServerHandler renderingServerHandler)
```

引擎名：`soft_body_update_rendering_server`

Requests that the physics server updates the rendering server with the latest positions of the given soft body's points through the `renderingServerHandler` interface.

### SpaceCreate

```csharp
public static Rid SpaceCreate()
```

引擎名：`space_create`

Creates a space. A space is a collection of parameters for the physics engine that can be assigned to an area or a body. It can be assigned to an area with `AreaSetSpace()`, or to a body with `BodySetSpace()`.

### SpaceGetDirectState

```csharp
public static PhysicsDirectSpaceState3D SpaceGetDirectState(Rid space)
```

引擎名：`space_get_direct_state`

Returns the state of a space, a [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md). This object can be used to make collision/intersection queries.

### SpaceGetParam

```csharp
public static float SpaceGetParam(Rid space, PhysicsServer3D.SpaceParameter param)
```

引擎名：`space_get_param`

Returns the value of a space parameter.

### SpaceIsActive

```csharp
public static bool SpaceIsActive(Rid space)
```

引擎名：`space_is_active`

Returns whether the space is active.

### SpaceSetActive

```csharp
public static void SpaceSetActive(Rid space, bool active)
```

引擎名：`space_set_active`

Marks a space as active. It will not have an effect, unless it is assigned to an area or body.

### SpaceSetParam

```csharp
public static void SpaceSetParam(Rid space, PhysicsServer3D.SpaceParameter param, float value)
```

引擎名：`space_set_param`

Sets the value for a space parameter. A list of available parameters is on the [`PhysicsServer3D.SpaceParameter`](PhysicsServer3D.md) constants.

### SphereShapeCreate

```csharp
public static Rid SphereShapeCreate()
```

引擎名：`sphere_shape_create`

Creates a 3D sphere shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the sphere's radius.

### WorldBoundaryShapeCreate

```csharp
public static Rid WorldBoundaryShapeCreate()
```

引擎名：`world_boundary_shape_create`

Creates a 3D world boundary shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's normal direction and distance properties.

## AreaBodyStatus

命名空间 `Godot`；enum

### 枚举值

- `Added = 0`：The value of the first parameter and area callback function receives, when an object enters one of its shapes.
- `Removed = 1`：The value of the first parameter and area callback function receives, when an object exits one of its shapes.

## AreaParameter

命名空间 `Godot`；enum

### 枚举值

- `GravityOverrideMode = 0`：Constant to set/get gravity override mode in an area.
- `Gravity = 1`：Constant to set/get gravity strength in an area.
- `GravityVector = 2`：Constant to set/get gravity vector/center in an area.
- `GravityIsPoint = 3`：Constant to set/get whether the gravity vector of an area is a direction, or a center point.
- `GravityPointUnitDistance = 4`：Constant to set/get the distance at which the gravity strength is equal to the gravity controlled by `Gravity`.
- `LinearDampOverrideMode = 5`：Constant to set/get linear damping override mode in an area.
- `LinearDamp = 6`：Constant to set/get the linear damping factor of an area.
- `AngularDampOverrideMode = 7`：Constant to set/get angular damping override mode in an area.
- `AngularDamp = 8`：Constant to set/get the angular damping factor of an area.
- `Priority = 9`：Constant to set/get the priority (order of processing) of an area.
- `WindForceMagnitude = 10`：Constant to set/get the magnitude of area-specific wind force.
- `WindSource = 11`：Constant to set/get the 3D vector that specifies the origin from which an area-specific wind blows.
- `WindDirection = 12`：Constant to set/get the 3D vector that specifies the direction in which an area-specific wind blows.
- `WindAttenuationFactor = 13`：Constant to set/get the exponential rate at which wind force decreases with distance from its origin.

## AreaSpaceOverrideMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This area does not affect gravity/damp.
- `Combine = 1`：This area adds its gravity/damp values to whatever has been calculated so far.
- `CombineReplace = 2`：This area adds its gravity/damp values to whatever has been calculated so far.
- `Replace = 3`：This area replaces any gravity/damp, even the default one, and stops taking into account the rest of the areas.
- `ReplaceCombine = 4`：This area replaces any gravity/damp calculated so far, but keeps calculating the rest of the areas, down to the default one.

## BodyAxis

命名空间 `Godot`；enum

### 枚举值

- `LinearX = 1`
- `LinearY = 2`
- `LinearZ = 4`
- `AngularX = 8`
- `AngularY = 16`
- `AngularZ = 32`

## BodyDampMode

命名空间 `Godot`；enum

### 枚举值

- `Combine = 0`：The body's damping value is added to any value set in areas or the default value.
- `Replace = 1`：The body's damping value replaces any value set in areas or the default value.

## BodyMode

命名空间 `Godot`；enum

### 枚举值

- `Static = 0`：Constant for static bodies.
- `Kinematic = 1`：Constant for kinematic bodies.
- `Rigid = 2`：Constant for rigid bodies.
- `RigidLinear = 3`：Constant for linear rigid bodies.

## BodyParameter

命名空间 `Godot`；enum

### 枚举值

- `Bounce = 0`：Constant to set/get a body's bounce factor.
- `Friction = 1`：Constant to set/get a body's friction.
- `Mass = 2`：Constant to set/get a body's mass.
- `Inertia = 3`：Constant to set/get a body's inertia.
- `CenterOfMass = 4`：Constant to set/get a body's center of mass position in the body's local coordinate system.
- `GravityScale = 5`：Constant to set/get a body's gravity multiplier.
- `LinearDampMode = 6`：Constant to set/get a body's linear damping mode.
- `AngularDampMode = 7`：Constant to set/get a body's angular damping mode.
- `LinearDamp = 8`：Constant to set/get a body's linear damping factor.
- `AngularDamp = 9`：Constant to set/get a body's angular damping factor.
- `Max = 10`：Represents the size of the `PhysicsServer3D.BodyParameter` enum.

## BodyState

命名空间 `Godot`；enum

### 枚举值

- `Transform = 0`：Constant to set/get the current transform matrix of the body.
- `LinearVelocity = 1`：Constant to set/get the current linear velocity of the body.
- `AngularVelocity = 2`：Constant to set/get the current angular velocity of the body.
- `Sleeping = 3`：Constant to sleep/wake up a body, or to get whether it is sleeping.
- `CanSleep = 4`：Constant to set/get whether the body can sleep.

## ConeTwistJointParam

命名空间 `Godot`；enum

### 枚举值

- `SwingSpan = 0`：Swing is rotation from side to side, around the axis perpendicular to the twist axis.
- `TwistSpan = 1`：Twist is the rotation around the twist axis, this value defined how far the joint can twist.
- `Bias = 2`：The speed with which the swing or twist will take place.
- `Softness = 3`：The ease with which the Joint3D twists, if it's too low, it takes more force to twist the joint.
- `Relaxation = 4`：Defines, how fast the swing- and twist-speed-difference on both sides gets synced.

## G6DofJointAxisFlag

命名空间 `Godot`；enum

### 枚举值

- `EnableLinearLimit = 0`：If set, linear motion is possible within the given limits.
- `EnableAngularLimit = 1`：If set, rotational motion is possible.
- `EnableAngularSpring = 2`
- `EnableLinearSpring = 3`
- `EnableMotor = 4`：If set, there is a rotational motor across these axes.
- `EnableLinearMotor = 5`：If set, there is a linear motor on this axis that targets a specific velocity.
- `Max = 6`：Represents the size of the `PhysicsServer3D.G6DofJointAxisFlag` enum.

## G6DofJointAxisParam

命名空间 `Godot`；enum

### 枚举值

- `LinearLowerLimit = 0`：The minimum difference between the pivot points' axes.
- `LinearUpperLimit = 1`：The maximum difference between the pivot points' axes.
- `LinearLimitSoftness = 2`：A factor that gets applied to the movement across the axes.
- `LinearRestitution = 3`：The amount of restitution on the axes movement.
- `LinearDamping = 4`：The amount of damping that happens at the linear motion across the axes.
- `LinearMotorTargetVelocity = 5`：The velocity that the joint's linear motor will attempt to reach.
- `LinearMotorForceLimit = 6`：The maximum force that the linear motor can apply while trying to reach the target velocity.
- `LinearSpringStiffness = 7`
- `LinearSpringDamping = 8`
- `LinearSpringEquilibriumPoint = 9`
- `AngularLowerLimit = 10`：The minimum rotation in negative direction to break loose and rotate around the axes.
- `AngularUpperLimit = 11`：The minimum rotation in positive direction to break loose and rotate around the axes.
- `AngularLimitSoftness = 12`：A factor that gets multiplied onto all rotations across the axes.
- `AngularDamping = 13`：The amount of rotational damping across the axes.
- `AngularRestitution = 14`：The amount of rotational restitution across the axes.
- `AngularForceLimit = 15`：The maximum amount of force that can occur, when rotating around the axes.
- `AngularErp = 16`：When correcting the crossing of limits in rotation across the axes, this error tolerance factor defines how much the correction gets slowed down.
- `AngularMotorTargetVelocity = 17`：Target speed for the motor at the axes.
- `AngularMotorForceLimit = 18`：Maximum acceleration for the motor at the axes.
- `AngularSpringStiffness = 19`
- `AngularSpringDamping = 20`
- `AngularSpringEquilibriumPoint = 21`
- `Max = 22`：Represents the size of the `PhysicsServer3D.G6DofJointAxisParam` enum.

## HingeJointFlag

命名空间 `Godot`；enum

### 枚举值

- `UseLimit = 0`：If `true`, the Hinge has a maximum and a minimum rotation.
- `EnableMotor = 1`：If `true`, a motor turns the Hinge.

## HingeJointParam

命名空间 `Godot`；enum

### 枚举值

- `Bias = 0`：The speed with which the two bodies get pulled together when they move in different directions.
- `LimitUpper = 1`：The maximum rotation across the Hinge.
- `LimitLower = 2`：The minimum rotation across the Hinge.
- `LimitBias = 3`：The speed with which the rotation across the axis perpendicular to the hinge gets corrected.
- `LimitSoftness = 4`：**Note:** Only supported when using GodotPhysics3D.
- `LimitRelaxation = 5`：The lower this value, the more the rotation gets slowed down.
- `MotorTargetVelocity = 6`：Target speed for the motor.
- `MotorMaxImpulse = 7`：Maximum acceleration for the motor.

## JointType

命名空间 `Godot`；enum

### 枚举值

- `Pin = 0`：The [`Joint3D`](Joint3D.md) is a [`PinJoint3D`](PinJoint3D.md).
- `Hinge = 1`：The [`Joint3D`](Joint3D.md) is a [`HingeJoint3D`](HingeJoint3D.md).
- `Slider = 2`：The [`Joint3D`](Joint3D.md) is a [`SliderJoint3D`](SliderJoint3D.md).
- `ConeTwist = 3`：The [`Joint3D`](Joint3D.md) is a [`ConeTwistJoint3D`](ConeTwistJoint3D.md).
- `Type6Dof = 4`：The [`Joint3D`](Joint3D.md) is a [`Generic6DofJoint3D`](Generic6DofJoint3D.md).
- `Max = 5`：Represents the size of the `PhysicsServer3D.JointType` enum.

## PinJointParam

命名空间 `Godot`；enum

### 枚举值

- `Bias = 0`：The strength with which the pinned objects try to stay in positional relation to each other.
- `Damping = 1`：The strength with which the pinned objects try to stay in velocity relation to each other.
- `ImpulseClamp = 2`：If above 0, this value is the maximum value for an impulse that this Joint3D puts on its ends.

## ProcessInfo

命名空间 `Godot`；enum

### 枚举值

- `ActiveObjects = 0`：Constant to get the number of objects that are not sleeping.
- `CollisionPairs = 1`：Constant to get the number of possible collisions.
- `IslandCount = 2`：Constant to get the number of space regions where a collision could occur.

## ShapeType

命名空间 `Godot`；enum

### 枚举值

- `WorldBoundary = 0`：Constant for creating a world boundary shape (used by the [`WorldBoundaryShape3D`](WorldBoundaryShape3D.md) resource).
- `SeparationRay = 1`：Constant for creating a separation ray shape (used by the [`SeparationRayShape3D`](SeparationRayShape3D.md) resource).
- `Sphere = 2`：Constant for creating a sphere shape (used by the [`SphereShape3D`](SphereShape3D.md) resource).
- `Box = 3`：Constant for creating a box shape (used by the [`BoxShape3D`](BoxShape3D.md) resource).
- `Capsule = 4`：Constant for creating a capsule shape (used by the [`CapsuleShape3D`](CapsuleShape3D.md) resource).
- `Cylinder = 5`：Constant for creating a cylinder shape (used by the [`CylinderShape3D`](CylinderShape3D.md) resource).
- `ConvexPolygon = 6`：Constant for creating a convex polygon shape (used by the [`ConvexPolygonShape3D`](ConvexPolygonShape3D.md) resource).
- `ConcavePolygon = 7`：Constant for creating a concave polygon (trimesh) shape (used by the [`ConcavePolygonShape3D`](ConcavePolygonShape3D.md) resource).
- `Heightmap = 8`：Constant for creating a heightmap shape (used by the [`HeightMapShape3D`](HeightMapShape3D.md) resource).
- `SoftBody = 9`：Constant used internally for a soft body shape.
- `Custom = 10`：Constant used internally for a custom shape.

## SliderJointParam

命名空间 `Godot`；enum

### 枚举值

- `LinearLimitUpper = 0`：The maximum difference between the pivot points on their X axis before damping happens.
- `LinearLimitLower = 1`：The minimum difference between the pivot points on their X axis before damping happens.
- `LinearLimitSoftness = 2`：A factor applied to the movement across the slider axis once the limits get surpassed.
- `LinearLimitRestitution = 3`：The amount of restitution once the limits are surpassed.
- `LinearLimitDamping = 4`：The amount of damping once the slider limits are surpassed.
- `LinearMotionSoftness = 5`：A factor applied to the movement across the slider axis as long as the slider is in the limits.
- `LinearMotionRestitution = 6`：The amount of restitution inside the slider limits.
- `LinearMotionDamping = 7`：The amount of damping inside the slider limits.
- `LinearOrthogonalSoftness = 8`：A factor applied to the movement across axes orthogonal to the slider.
- `LinearOrthogonalRestitution = 9`：The amount of restitution when movement is across axes orthogonal to the slider.
- `LinearOrthogonalDamping = 10`：The amount of damping when movement is across axes orthogonal to the slider.
- `AngularLimitUpper = 11`：The upper limit of rotation in the slider.
- `AngularLimitLower = 12`：The lower limit of rotation in the slider.
- `AngularLimitSoftness = 13`：A factor applied to the all rotation once the limit is surpassed.
- `AngularLimitRestitution = 14`：The amount of restitution of the rotation when the limit is surpassed.
- `AngularLimitDamping = 15`：The amount of damping of the rotation when the limit is surpassed.
- `AngularMotionSoftness = 16`：A factor that gets applied to the all rotation in the limits.
- `AngularMotionRestitution = 17`：The amount of restitution of the rotation in the limits.
- `AngularMotionDamping = 18`：The amount of damping of the rotation in the limits.
- `AngularOrthogonalSoftness = 19`：A factor that gets applied to the all rotation across axes orthogonal to the slider.
- `AngularOrthogonalRestitution = 20`：The amount of restitution of the rotation across axes orthogonal to the slider.
- `AngularOrthogonalDamping = 21`：The amount of damping of the rotation across axes orthogonal to the slider.
- `Max = 22`：Represents the size of the `PhysicsServer3D.SliderJointParam` enum.

## SpaceParameter

命名空间 `Godot`；enum

### 枚举值

- `ContactRecycleRadius = 0`：Constant to set/get the maximum distance a pair of bodies has to move before their collision status has to be recalculated.
- `ContactMaxSeparation = 1`：Constant to set/get the maximum distance a shape can be from another before they are considered separated and the contact is discarded.
- `ContactMaxAllowedPenetration = 2`：Constant to set/get the maximum distance a shape can penetrate another shape before it is considered a collision.
- `ContactDefaultBias = 3`：Constant to set/get the default solver bias for all physics contacts.
- `BodyLinearVelocitySleepThreshold = 4`：Constant to set/get the threshold linear velocity of activity.
- `BodyAngularVelocitySleepThreshold = 5`：Constant to set/get the threshold angular velocity of activity.
- `BodyTimeToSleep = 6`：Constant to set/get the maximum time of activity.
- `SolverIterations = 7`：Constant to set/get the number of solver iterations for contacts and constraints.
