<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer2D

命名空间 `Godot`；static class

PhysicsServer2D is the server responsible for all 2D physics. It can directly create and manipulate all physics objects:

- A *space* is a self-contained world for a physics simulation. It contains bodies, areas, and joints. Its state can be queried for collision and intersection information, and several parameters of the simulation can be modified.

- A *shape* is a geometric shape such as a circle, a rectangle, a capsule, or a polygon. It can be used for collision detection by adding it to a body/area, possibly with an extra transformation relative to the body/area's origin. Bodies/areas can have multiple (transformed) shapes added to them, and a single shape can be added to bodies/areas multiple times with different local transformations.

- A *body* is a physical object which can be in static, kinematic, or rigid mode. Its state (such as position and velocity) can be queried and updated. A force integration callback can be set to customize the body's physics.

- An *area* is a region in space which can be used to detect bodies and areas entering and exiting it. A body monitoring callback can be set to report entering/exiting body shapes, and similarly an area monitoring callback can be set. Gravity and damping can be overridden within the area by setting area parameters.

- A *joint* is a constraint, either between two bodies or on one body relative to a point. Parameters such as the joint bias and the rest length of a spring joint can be adjusted.

Physics objects in `PhysicsServer2D` may be created and manipulated independently; they do not have to be tied to nodes in the scene tree.

**Note:** All the 2D physics nodes use the physics server internally. Adding a physics node to the scene tree will cause a corresponding physics object to be created in the physics server. A rigid body node registers a callback that updates the node's transform with the transform of the respective body object in the physics server (every physics update). An area node registers a callback to inform the area node about overlaps with the respective area object in the physics server. The raycast node queries the direct state of the relevant space in the physics server.

## 属性

### Singleton

```csharp
public static PhysicsServer2DInstance Singleton { get; }
```

## 方法

### AreaAddShape

```csharp
public static void AreaAddShape(Rid area, Rid shape, Transform2D? transform = null, bool disabled = false)
```

引擎名：`area_add_shape`

Adds a shape to the area, with the given local transform. The shape (together with its `transform` and `disabled` properties) is added to an array of shapes, and the shapes of an area are usually referenced by their index in this array.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### AreaAttachCanvasInstanceId

```csharp
public static void AreaAttachCanvasInstanceId(Rid area, ulong id)
```

引擎名：`area_attach_canvas_instance_id`

Attaches the `ObjectID` of a canvas to the area. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CanvasLayer`](CanvasLayer.md).

### AreaAttachObjectInstanceId

```csharp
public static void AreaAttachObjectInstanceId(Rid area, ulong id)
```

引擎名：`area_attach_object_instance_id`

Attaches the `ObjectID` of an [`GodotObject`](GodotObject.md) to the area. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CollisionObject2D`](CollisionObject2D.md).

### AreaClearShapes

```csharp
public static void AreaClearShapes(Rid area)
```

引擎名：`area_clear_shapes`

Removes all shapes from the area. This does not delete the shapes themselves, so they can continue to be used elsewhere or added back later.

### AreaCreate

```csharp
public static Rid AreaCreate()
```

引擎名：`area_create`

Creates a 2D area object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and `monitorable` set to `false`.

Use `AreaAddShape()` to add shapes to it, use `AreaSetTransform()` to set its transform, and use `AreaSetSpace()` to add the area to a space. If you want the area to be detectable use `AreaSetMonitorable()`.

### AreaGetCanvasInstanceId

```csharp
public static ulong AreaGetCanvasInstanceId(Rid area)
```

引擎名：`area_get_canvas_instance_id`

Returns the `ObjectID` of the canvas attached to the area. Use `GodotObject.InstanceFromId` to retrieve a [`CanvasLayer`](CanvasLayer.md) from a nonzero `ObjectID`.

### AreaGetCollisionLayer

```csharp
public static uint AreaGetCollisionLayer(Rid area)
```

引擎名：`area_get_collision_layer`

Returns the physics layer or layers the area belongs to, as a bitmask.

### AreaGetCollisionMask

```csharp
public static uint AreaGetCollisionMask(Rid area)
```

引擎名：`area_get_collision_mask`

Returns the physics layer or layers the area can contact with, as a bitmask.

### AreaGetObjectInstanceId

```csharp
public static ulong AreaGetObjectInstanceId(Rid area)
```

引擎名：`area_get_object_instance_id`

Returns the `ObjectID` attached to the area. Use `GodotObject.InstanceFromId` to retrieve an [`GodotObject`](GodotObject.md) from a nonzero `ObjectID`.

### AreaGetParam

```csharp
public static Variant AreaGetParam(Rid area, PhysicsServer2D.AreaParameter param)
```

引擎名：`area_get_param`

Returns the value of the given area parameter.

### AreaGetShape

```csharp
public static Rid AreaGetShape(Rid area, int shapeIdx)
```

引擎名：`area_get_shape`

Returns the [`Rid`](Rid.md) of the shape with the given index in the area's array of shapes.

### AreaGetShapeCount

```csharp
public static int AreaGetShapeCount(Rid area)
```

引擎名：`area_get_shape_count`

Returns the number of shapes added to the area.

### AreaGetShapeTransform

```csharp
public static Transform2D AreaGetShapeTransform(Rid area, int shapeIdx)
```

引擎名：`area_get_shape_transform`

Returns the local transform matrix of the shape with the given index in the area's array of shapes.

### AreaGetSpace

```csharp
public static Rid AreaGetSpace(Rid area)
```

引擎名：`area_get_space`

Returns the [`Rid`](Rid.md) of the space assigned to the area. Returns an empty [`Rid`](Rid.md) if no space is assigned.

### AreaGetTransform

```csharp
public static Transform2D AreaGetTransform(Rid area)
```

引擎名：`area_get_transform`

Returns the transform matrix of the area.

### AreaRemoveShape

```csharp
public static void AreaRemoveShape(Rid area, int shapeIdx)
```

引擎名：`area_remove_shape`

Removes the shape with the given index from the area's array of shapes. The shape itself is not deleted, so it can continue to be used elsewhere or added back later. As a result of this operation, the area's shapes which used to have indices higher than `shapeIdx` will have their index decreased by one.

### AreaSetAreaMonitorCallback

```csharp
public static void AreaSetAreaMonitorCallback(Rid area, Callable callback)
```

引擎名：`area_set_area_monitor_callback`

Sets the area's area monitor callback. This callback will be called when any other (shape of an) area enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer2D.AreaBodyStatus.Added`](PhysicsServer2D.md) or [`PhysicsServer2D.AreaBodyStatus.Removed`](PhysicsServer2D.md) depending on whether the other area's shape entered or exited the area,

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

Assigns the area to one or many physics layers, via a bitmask.

### AreaSetCollisionMask

```csharp
public static void AreaSetCollisionMask(Rid area, uint mask)
```

引擎名：`area_set_collision_mask`

Sets which physics layers the area will monitor, via a bitmask.

### AreaSetMonitorCallback

```csharp
public static void AreaSetMonitorCallback(Rid area, Callable callback)
```

引擎名：`area_set_monitor_callback`

Sets the area's body monitor callback. This callback will be called when any other (shape of a) body enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer2D.AreaBodyStatus.Added`](PhysicsServer2D.md) or [`PhysicsServer2D.AreaBodyStatus.Removed`](PhysicsServer2D.md) depending on whether the other body shape entered or exited the area,

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

Sets whether the area is monitorable or not. If `monitorable` is `true`, the area monitoring callback of other areas will be called when this area enters or exits them.

### AreaSetParam

```csharp
public static void AreaSetParam(Rid area, PhysicsServer2D.AreaParameter param, Variant value)
```

引擎名：`area_set_param`

Sets the value of the given area parameter.

### AreaSetShape

```csharp
public static void AreaSetShape(Rid area, int shapeIdx, Rid shape)
```

引擎名：`area_set_shape`

Replaces the area's shape at the given index by another shape, while not affecting the `transform` and `disabled` properties at the same index.

### AreaSetShapeDisabled

```csharp
public static void AreaSetShapeDisabled(Rid area, int shapeIdx, bool disabled)
```

引擎名：`area_set_shape_disabled`

Sets the disabled property of the area's shape with the given index. If `disabled` is `true`, then the shape will not detect any other shapes entering or exiting it.

### AreaSetShapeTransform

```csharp
public static void AreaSetShapeTransform(Rid area, int shapeIdx, Transform2D transform)
```

引擎名：`area_set_shape_transform`

Sets the local transform matrix of the area's shape with the given index.

### AreaSetSpace

```csharp
public static void AreaSetSpace(Rid area, Rid space)
```

引擎名：`area_set_space`

Adds the area to the given space, after removing the area from the previously assigned space (if any).

**Note:** To remove an area from a space without immediately adding it back elsewhere, use `PhysicsServer2D.area_set_space(area, RID())`.

### AreaSetTransform

```csharp
public static void AreaSetTransform(Rid area, Transform2D transform)
```

引擎名：`area_set_transform`

Sets the transform matrix of the area.

### BodyAddCollisionException

```csharp
public static void BodyAddCollisionException(Rid body, Rid exceptedBody)
```

引擎名：`body_add_collision_exception`

Adds `exceptedBody` to the body's list of collision exceptions, so that collisions with it are ignored.

### BodyAddConstantCentralForce

```csharp
public static void BodyAddConstantCentralForce(Rid body, Vector2 force)
```

引擎名：`body_add_constant_central_force`

Adds a constant directional force to the body. The force does not affect rotation. The force remains applied over time until cleared with `PhysicsServer2D.body_set_constant_force(body, Vector2(0, 0))`.

This is equivalent to using `BodyAddConstantForce()` at the body's center of mass.

### BodyAddConstantForce

```csharp
public static void BodyAddConstantForce(Rid body, Vector2 force, Vector2? position = null)
```

引擎名：`body_add_constant_force`

Adds a constant positioned force to the body. The force can affect rotation if `position` is different from the body's center of mass. The force remains applied over time until cleared with `PhysicsServer2D.body_set_constant_force(body, Vector2(0, 0))`.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BodyAddConstantTorque

```csharp
public static void BodyAddConstantTorque(Rid body, float torque)
```

引擎名：`body_add_constant_torque`

Adds a constant rotational force to the body. The force does not affect position. The force remains applied over time until cleared with `PhysicsServer2D.body_set_constant_torque(body, 0)`.

### BodyAddShape

```csharp
public static void BodyAddShape(Rid body, Rid shape, Transform2D? transform = null, bool disabled = false)
```

引擎名：`body_add_shape`

Adds a shape to the area, with the given local transform. The shape (together with its `transform` and `disabled` properties) is added to an array of shapes, and the shapes of a body are usually referenced by their index in this array.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### BodyApplyCentralForce

```csharp
public static void BodyApplyCentralForce(Rid body, Vector2 force)
```

引擎名：`body_apply_central_force`

Applies a directional force to the body, at the body's center of mass. The force does not affect rotation. A force is time dependent and meant to be applied every physics update.

This is equivalent to using `BodyApplyForce()` at the body's center of mass.

### BodyApplyCentralImpulse

```csharp
public static void BodyApplyCentralImpulse(Rid body, Vector2 impulse)
```

引擎名：`body_apply_central_impulse`

Applies a directional impulse to the body, at the body's center of mass. The impulse does not affect rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

This is equivalent to using `BodyApplyImpulse()` at the body's center of mass.

### BodyApplyForce

```csharp
public static void BodyApplyForce(Rid body, Vector2 force, Vector2? position = null)
```

引擎名：`body_apply_force`

Applies a positioned force to the body. The force can affect rotation if `position` is different from the body's center of mass. A force is time dependent and meant to be applied every physics update.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BodyApplyImpulse

```csharp
public static void BodyApplyImpulse(Rid body, Vector2 impulse, Vector2? position = null)
```

引擎名：`body_apply_impulse`

Applies a positioned impulse to the body. The impulse can affect rotation if `position` is different from the body's center of mass.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BodyApplyTorque

```csharp
public static void BodyApplyTorque(Rid body, float torque)
```

引擎名：`body_apply_torque`

Applies a rotational force to the body. The force does not affect position. A force is time dependent and meant to be applied every physics update.

### BodyApplyTorqueImpulse

```csharp
public static void BodyApplyTorqueImpulse(Rid body, float impulse)
```

引擎名：`body_apply_torque_impulse`

Applies a rotational impulse to the body. The impulse does not affect position.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### BodyAttachCanvasInstanceId

```csharp
public static void BodyAttachCanvasInstanceId(Rid body, ulong id)
```

引擎名：`body_attach_canvas_instance_id`

Attaches the `ObjectID` of a canvas to the body. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CanvasLayer`](CanvasLayer.md).

### BodyAttachObjectInstanceId

```csharp
public static void BodyAttachObjectInstanceId(Rid body, ulong id)
```

引擎名：`body_attach_object_instance_id`

Attaches the `ObjectID` of an [`GodotObject`](GodotObject.md) to the body. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CollisionObject2D`](CollisionObject2D.md).

### BodyClearShapes

```csharp
public static void BodyClearShapes(Rid body)
```

引擎名：`body_clear_shapes`

Removes all shapes from the body. This does not delete the shapes themselves, so they can continue to be used elsewhere or added back later.

### BodyCreate

```csharp
public static Rid BodyCreate()
```

引擎名：`body_create`

Creates a 2D body object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and body mode set to [`PhysicsServer2D.BodyMode.Rigid`](PhysicsServer2D.md).

Use `BodyAddShape()` to add shapes to it, use `BodySetState()` to set its transform, and use `BodySetSpace()` to add the body to a space.

### BodyGetCanvasInstanceId

```csharp
public static ulong BodyGetCanvasInstanceId(Rid body)
```

引擎名：`body_get_canvas_instance_id`

Returns the `ObjectID` of the canvas attached to the body. Use `GodotObject.InstanceFromId` to retrieve a [`CanvasLayer`](CanvasLayer.md) from a nonzero `ObjectID`.

### BodyGetCollisionLayer

```csharp
public static uint BodyGetCollisionLayer(Rid body)
```

引擎名：`body_get_collision_layer`

Returns the physics layer or layers the body belongs to, as a bitmask.

### BodyGetCollisionMask

```csharp
public static uint BodyGetCollisionMask(Rid body)
```

引擎名：`body_get_collision_mask`

Returns the physics layer or layers the body can collide with, as a bitmask.

### BodyGetCollisionPriority

```csharp
public static float BodyGetCollisionPriority(Rid body)
```

引擎名：`body_get_collision_priority`

Returns the body's collision priority. This is used in the depenetration phase of `BodyTestMotion()`. The higher the priority is, the lower the penetration into the body will be.

### BodyGetConstantForce

```csharp
public static Vector2 BodyGetConstantForce(Rid body)
```

引擎名：`body_get_constant_force`

Returns the body's total constant positional force applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodyGetConstantTorque

```csharp
public static float BodyGetConstantTorque(Rid body)
```

引擎名：`body_get_constant_torque`

Returns the body's total constant rotational force applied during each physics update.

See `BodyAddConstantTorque()`.

### BodyGetContinuousCollisionDetectionMode

```csharp
public static PhysicsServer2D.CcdMode BodyGetContinuousCollisionDetectionMode(Rid body)
```

引擎名：`body_get_continuous_collision_detection_mode`

Returns the body's continuous collision detection mode.

### BodyGetDirectState

```csharp
public static PhysicsDirectBodyState2D BodyGetDirectState(Rid body)
```

引擎名：`body_get_direct_state`

Returns the [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md) of the body. Returns `null` if the body is destroyed or not assigned to a space.

### BodyGetMaxContactsReported

```csharp
public static int BodyGetMaxContactsReported(Rid body)
```

引擎名：`body_get_max_contacts_reported`

Returns the maximum number of contacts that the body can report. See `BodySetMaxContactsReported()`.

### BodyGetMode

```csharp
public static PhysicsServer2D.BodyMode BodyGetMode(Rid body)
```

引擎名：`body_get_mode`

Returns the body's mode.

### BodyGetObjectInstanceId

```csharp
public static ulong BodyGetObjectInstanceId(Rid body)
```

引擎名：`body_get_object_instance_id`

Returns the `ObjectID` attached to the body. Use `GodotObject.InstanceFromId` to retrieve an [`GodotObject`](GodotObject.md) from a nonzero `ObjectID`.

### BodyGetParam

```csharp
public static Variant BodyGetParam(Rid body, PhysicsServer2D.BodyParameter param)
```

引擎名：`body_get_param`

Returns the value of the given body parameter.

### BodyGetShape

```csharp
public static Rid BodyGetShape(Rid body, int shapeIdx)
```

引擎名：`body_get_shape`

Returns the [`Rid`](Rid.md) of the shape with the given index in the body's array of shapes.

### BodyGetShapeCount

```csharp
public static int BodyGetShapeCount(Rid body)
```

引擎名：`body_get_shape_count`

Returns the number of shapes added to the body.

### BodyGetShapeTransform

```csharp
public static Transform2D BodyGetShapeTransform(Rid body, int shapeIdx)
```

引擎名：`body_get_shape_transform`

Returns the local transform matrix of the shape with the given index in the area's array of shapes.

### BodyGetSpace

```csharp
public static Rid BodyGetSpace(Rid body)
```

引擎名：`body_get_space`

Returns the [`Rid`](Rid.md) of the space assigned to the body. Returns an empty [`Rid`](Rid.md) if no space is assigned.

### BodyGetState

```csharp
public static Variant BodyGetState(Rid body, PhysicsServer2D.BodyState state)
```

引擎名：`body_get_state`

Returns the value of the given state of the body.

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

Removes `exceptedBody` from the body's list of collision exceptions, so that collisions with it are no longer ignored.

### BodyRemoveShape

```csharp
public static void BodyRemoveShape(Rid body, int shapeIdx)
```

引擎名：`body_remove_shape`

Removes the shape with the given index from the body's array of shapes. The shape itself is not deleted, so it can continue to be used elsewhere or added back later. As a result of this operation, the body's shapes which used to have indices higher than `shapeIdx` will have their index decreased by one.

### BodyResetMassProperties

```csharp
public static void BodyResetMassProperties(Rid body)
```

引擎名：`body_reset_mass_properties`

Restores the default inertia and center of mass of the body based on its shapes. This undoes any custom values previously set using `BodySetParam()`.

### BodySetAxisVelocity

```csharp
public static void BodySetAxisVelocity(Rid body, Vector2 axisVelocity)
```

引擎名：`body_set_axis_velocity`

Modifies the body's linear velocity so that its projection to the axis `axis_velocity.normalized()` is exactly `axis_velocity.length()`. This is useful for jumping behavior.

### BodySetCollisionLayer

```csharp
public static void BodySetCollisionLayer(Rid body, uint layer)
```

引擎名：`body_set_collision_layer`

Sets the physics layer or layers the body belongs to, via a bitmask.

### BodySetCollisionMask

```csharp
public static void BodySetCollisionMask(Rid body, uint mask)
```

引擎名：`body_set_collision_mask`

Sets the physics layer or layers the body can collide with, via a bitmask.

### BodySetCollisionPriority

```csharp
public static void BodySetCollisionPriority(Rid body, float priority)
```

引擎名：`body_set_collision_priority`

Sets the body's collision priority. This is used in the depenetration phase of `BodyTestMotion()`. The higher the priority is, the lower the penetration into the body will be.

### BodySetConstantForce

```csharp
public static void BodySetConstantForce(Rid body, Vector2 force)
```

引擎名：`body_set_constant_force`

Sets the body's total constant positional force applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodySetConstantTorque

```csharp
public static void BodySetConstantTorque(Rid body, float torque)
```

引擎名：`body_set_constant_torque`

Sets the body's total constant rotational force applied during each physics update.

See `BodyAddConstantTorque()`.

### BodySetContinuousCollisionDetectionMode

```csharp
public static void BodySetContinuousCollisionDetectionMode(Rid body, PhysicsServer2D.CcdMode mode)
```

引擎名：`body_set_continuous_collision_detection_mode`

Sets the continuous collision detection mode.

Continuous collision detection tries to predict where a moving body would collide in between physics updates, instead of moving it and correcting its movement if it collided.

### BodySetForceIntegrationCallback

```csharp
public static void BodySetForceIntegrationCallback(Rid body, Callable callable, Variant userdata = default)
```

引擎名：`body_set_force_integration_callback`

Sets the body's custom force integration callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the custom callback.

The function `callable` will be called every physics tick, before the standard force integration (see `BodySetOmitForceIntegration()`). It can be used for example to update the body's linear and angular velocity based on contact with other bodies.

If `userdata` is not `null`, the function `callable` must take the following two parameters:

1. `state`: a [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md) used to retrieve and modify the body's state,

2. `userdata`: a [`Variant`](Variant.md); its value will be the `userdata` passed into this method.

If `userdata` is `null`, then `callable` must take only the `state` parameter.

### BodySetMaxContactsReported

```csharp
public static void BodySetMaxContactsReported(Rid body, int amount)
```

引擎名：`body_set_max_contacts_reported`

Sets the maximum number of contacts that the body can report. If `amount` is greater than zero, then the body will keep track of at most this many contacts with other bodies.

### BodySetMode

```csharp
public static void BodySetMode(Rid body, PhysicsServer2D.BodyMode mode)
```

引擎名：`body_set_mode`

Sets the body's mode.

### BodySetOmitForceIntegration

```csharp
public static void BodySetOmitForceIntegration(Rid body, bool enable)
```

引擎名：`body_set_omit_force_integration`

Sets whether the body omits the standard force integration. If `enable` is `true`, the body will not automatically use applied forces, torques, and damping to update the body's linear and angular velocity. In this case, `BodySetForceIntegrationCallback()` can be used to manually update the linear and angular velocity instead.

This method is called when the property [`RigidBody2D.CustomIntegrator`](RigidBody2D.md) is set.

### BodySetParam

```csharp
public static void BodySetParam(Rid body, PhysicsServer2D.BodyParameter param, Variant value)
```

引擎名：`body_set_param`

Sets the value of the given body parameter.

### BodySetShape

```csharp
public static void BodySetShape(Rid body, int shapeIdx, Rid shape)
```

引擎名：`body_set_shape`

Replaces the body's shape at the given index by another shape, while not affecting the `transform`, `disabled`, and one-way collision properties at the same index.

### BodySetShapeAsOneWayCollision

```csharp
public static void BodySetShapeAsOneWayCollision(Rid body, int shapeIdx, bool enable, float margin)
```

引擎名：`body_set_shape_as_one_way_collision`

Sets the one-way collision properties of the body's shape with the given index. If `enable` is `true`, the one-way collision direction given by `direction` in the shape's local space (that is `body_get_shape_transform(body, shape_idx).basis_xform(direction).normalized()` in the body's local space) will be used to ignore collisions with the shape in the opposite direction, and to ensure depenetration of kinematic bodies happens in this direction.

### BodySetShapeAsOneWayCollision

```csharp
public static void BodySetShapeAsOneWayCollision(Rid body, int shapeIdx, bool enable, float margin, Vector2? direction = null)
```

引擎名：`body_set_shape_as_one_way_collision`

Sets the one-way collision properties of the body's shape with the given index. If `enable` is `true`, the one-way collision direction given by `direction` in the shape's local space (that is `body_get_shape_transform(body, shape_idx).basis_xform(direction).normalized()` in the body's local space) will be used to ignore collisions with the shape in the opposite direction, and to ensure depenetration of kinematic bodies happens in this direction.

参数：
- `direction`：If the parameter is null, then the default value is `new Vector2(0.0f, 1.0f)`.

### BodySetShapeDisabled

```csharp
public static void BodySetShapeDisabled(Rid body, int shapeIdx, bool disabled)
```

引擎名：`body_set_shape_disabled`

Sets the disabled property of the body's shape with the given index. If `disabled` is `true`, then the shape will be ignored in all collision detection.

### BodySetShapeTransform

```csharp
public static void BodySetShapeTransform(Rid body, int shapeIdx, Transform2D transform)
```

引擎名：`body_set_shape_transform`

Sets the local transform matrix of the body's shape with the given index.

### BodySetSpace

```csharp
public static void BodySetSpace(Rid body, Rid space)
```

引擎名：`body_set_space`

Adds the body to the given space, after removing the body from the previously assigned space (if any). If the body's mode is set to [`PhysicsServer2D.BodyMode.Rigid`](PhysicsServer2D.md), then adding the body to a space will have the following additional effects:

- If the parameter [`PhysicsServer2D.BodyParameter.CenterOfMass`](PhysicsServer2D.md) has never been set explicitly, then the value of that parameter will be recalculated based on the body's shapes.

- If the parameter [`PhysicsServer2D.BodyParameter.Inertia`](PhysicsServer2D.md) is set to a value `<= 0.0`, then the value of that parameter will be recalculated based on the body's shapes, mass, and center of mass.

**Note:** To remove a body from a space without immediately adding it back elsewhere, use `PhysicsServer2D.body_set_space(body, RID())`.

### BodySetState

```csharp
public static void BodySetState(Rid body, PhysicsServer2D.BodyState state, Variant value)
```

引擎名：`body_set_state`

Sets the value of a body's state.

**Note:** The state change doesn't take effect immediately. The state will change on the next physics frame.

### BodySetStateSyncCallback

```csharp
public static void BodySetStateSyncCallback(Rid body, Callable callable)
```

引擎名：`body_set_state_sync_callback`

Sets the body's state synchronization callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the callback.

The function `callable` will be called every physics frame, assuming that the body was active during the previous physics tick, and can be used to fetch the latest state from the physics server.

The function `callable` must take the following parameters:

1. `state`: a [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md), used to retrieve the body's state.

### BodyTestMotion

```csharp
public static bool BodyTestMotion(Rid body, PhysicsTestMotionParameters2D parameters, PhysicsTestMotionResult2D result = null)
```

引擎名：`body_test_motion`

Returns `true` if a collision would result from moving the body along a motion vector from a given point in space. See [`PhysicsTestMotionParameters2D`](PhysicsTestMotionParameters2D.md) for the available motion parameters. Optionally a [`PhysicsTestMotionResult2D`](PhysicsTestMotionResult2D.md) object can be passed, which will be used to store the information about the resulting collision.

### CapsuleShapeCreate

```csharp
public static Rid CapsuleShapeCreate()
```

引擎名：`capsule_shape_create`

Creates a 2D capsule shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the capsule's height and radius.

### CircleShapeCreate

```csharp
public static Rid CircleShapeCreate()
```

引擎名：`circle_shape_create`

Creates a 2D circle shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the circle's radius.

### ConcavePolygonShapeCreate

```csharp
public static Rid ConcavePolygonShapeCreate()
```

引擎名：`concave_polygon_shape_create`

Creates a 2D concave polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the concave polygon's segments.

### ConvexPolygonShapeCreate

```csharp
public static Rid ConvexPolygonShapeCreate()
```

引擎名：`convex_polygon_shape_create`

Creates a 2D convex polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the convex polygon's points.

### DampedSpringJointGetParam

```csharp
public static float DampedSpringJointGetParam(Rid joint, PhysicsServer2D.DampedSpringParam param)
```

引擎名：`damped_spring_joint_get_param`

Returns the value of the given damped spring joint parameter.

### DampedSpringJointSetParam

```csharp
public static void DampedSpringJointSetParam(Rid joint, PhysicsServer2D.DampedSpringParam param, float value)
```

引擎名：`damped_spring_joint_set_param`

Sets the value of the given damped spring joint parameter.

### FreeRid

```csharp
public static void FreeRid(Rid rid)
```

引擎名：`free_rid`

Destroys any of the objects created by PhysicsServer2D. If the [`Rid`](Rid.md) passed is not one of the objects that can be created by PhysicsServer2D, an error will be printed to the console.

### GetProcessInfo

```csharp
public static int GetProcessInfo(PhysicsServer2D.ProcessInfo processInfo)
```

引擎名：`get_process_info`

Returns the value of a physics engine state specified by `processInfo`.

### JointClear

```csharp
public static void JointClear(Rid joint)
```

引擎名：`joint_clear`

Destroys the joint with the given [`Rid`](Rid.md), creates a new uninitialized joint, and makes the [`Rid`](Rid.md) refer to this new joint.

### JointCreate

```csharp
public static Rid JointCreate()
```

引擎名：`joint_create`

Creates a 2D joint in the physics server, and returns the [`Rid`](Rid.md) that identifies it. To set the joint type, use `JointMakeDampedSpring()`, `JointMakeGroove()` or `JointMakePin()`. Use `JointSetParam()` to set generic joint parameters.

### JointDisableCollisionsBetweenBodies

```csharp
public static void JointDisableCollisionsBetweenBodies(Rid joint, bool disable)
```

引擎名：`joint_disable_collisions_between_bodies`

Sets whether the bodies attached to the [`Joint2D`](Joint2D.md) will collide with each other.

### JointGetParam

```csharp
public static float JointGetParam(Rid joint, PhysicsServer2D.JointParam param)
```

引擎名：`joint_get_param`

Returns the value of the given joint parameter.

### JointGetType

```csharp
public static PhysicsServer2D.JointType JointGetType(Rid joint)
```

引擎名：`joint_get_type`

Returns the joint's type.

### JointIsDisabledCollisionsBetweenBodies

```csharp
public static bool JointIsDisabledCollisionsBetweenBodies(Rid joint)
```

引擎名：`joint_is_disabled_collisions_between_bodies`

Returns whether the bodies attached to the [`Joint2D`](Joint2D.md) will collide with each other.

### JointMakeDampedSpring

```csharp
public static void JointMakeDampedSpring(Rid joint, Vector2 anchorA, Vector2 anchorB, Rid bodyA, Rid bodyB = default)
```

引擎名：`joint_make_damped_spring`

Makes the joint a damped spring joint, attached at the point `anchorA` (given in global coordinates) on the body `bodyA` and at the point `anchorB` (given in global coordinates) on the body `bodyB`. To set the parameters which are specific to the damped spring, see `DampedSpringJointSetParam()`.

### JointMakeGroove

```csharp
public static void JointMakeGroove(Rid joint, Vector2 groove1A, Vector2 groove2A, Vector2 anchorB, Rid bodyA = default, Rid bodyB = default)
```

引擎名：`joint_make_groove`

Makes the joint a groove joint.

### JointMakePin

```csharp
public static void JointMakePin(Rid joint, Vector2 anchor, Rid bodyA, Rid bodyB = default)
```

引擎名：`joint_make_pin`

Makes the joint a pin joint. If `bodyB` is an empty [`Rid`](Rid.md), then `bodyA` is pinned to the point `anchor` (given in global coordinates); otherwise, `bodyA` is pinned to `bodyB` at the point `anchor` (given in global coordinates). To set the parameters which are specific to the pin joint, see `PinJointSetParam()`.

### JointSetParam

```csharp
public static void JointSetParam(Rid joint, PhysicsServer2D.JointParam param, float value)
```

引擎名：`joint_set_param`

Sets the value of the given joint parameter.

### PinJointGetFlag

```csharp
public static bool PinJointGetFlag(Rid joint, PhysicsServer2D.PinJointFlag flag)
```

引擎名：`pin_joint_get_flag`

Gets a pin joint flag.

### PinJointGetParam

```csharp
public static float PinJointGetParam(Rid joint, PhysicsServer2D.PinJointParam param)
```

引擎名：`pin_joint_get_param`

Returns the value of a pin joint parameter.

### PinJointSetFlag

```csharp
public static void PinJointSetFlag(Rid joint, PhysicsServer2D.PinJointFlag flag, bool enabled)
```

引擎名：`pin_joint_set_flag`

Sets a pin joint flag.

### PinJointSetParam

```csharp
public static void PinJointSetParam(Rid joint, PhysicsServer2D.PinJointParam param, float value)
```

引擎名：`pin_joint_set_param`

Sets a pin joint parameter.

### RectangleShapeCreate

```csharp
public static Rid RectangleShapeCreate()
```

引擎名：`rectangle_shape_create`

Creates a 2D rectangle shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the rectangle's half-extents.

### SegmentShapeCreate

```csharp
public static Rid SegmentShapeCreate()
```

引擎名：`segment_shape_create`

Creates a 2D segment shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the segment's start and end points.

### SeparationRayShapeCreate

```csharp
public static Rid SeparationRayShapeCreate()
```

引擎名：`separation_ray_shape_create`

Creates a 2D separation ray shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's `length` and `slide_on_slope` properties.

### SetActive

```csharp
public static void SetActive(bool active)
```

引擎名：`set_active`

Activates or deactivates the 2D physics server. If `active` is `false`, then the physics server will not do anything in its physics step.

### ShapeGetData

```csharp
public static Variant ShapeGetData(Rid shape)
```

引擎名：`shape_get_data`

Returns the shape data that defines the configuration of the shape, such as the half-extents of a rectangle or the segments of a concave shape. See `ShapeSetData()` for the precise format of this data in each case.

### ShapeGetType

```csharp
public static PhysicsServer2D.ShapeType ShapeGetType(Rid shape)
```

引擎名：`shape_get_type`

Returns the shape's type.

### ShapeSetData

```csharp
public static void ShapeSetData(Rid shape, Variant data)
```

引擎名：`shape_set_data`

Sets the shape data that defines the configuration of the shape. The `data` to be passed depends on the shape's type (see `ShapeGetType()`):

- [`PhysicsServer2D.ShapeType.WorldBoundary`](PhysicsServer2D.md): an array of length two containing a [`Vector2`](Vector2.md) `normal` direction and a `System.Single` distance `d`,

- [`PhysicsServer2D.ShapeType.SeparationRay`](PhysicsServer2D.md): a dictionary containing the key `length` with a `System.Single` value and the key `slide_on_slope` with a `System.Boolean` value,

- [`PhysicsServer2D.ShapeType.Segment`](PhysicsServer2D.md): a [`Rect2`](Rect2.md) `rect` containing the first point of the segment in `rect.position` and the second point of the segment in `rect.size`,

- [`PhysicsServer2D.ShapeType.Circle`](PhysicsServer2D.md): a `System.Single` `radius`,

- [`PhysicsServer2D.ShapeType.Rectangle`](PhysicsServer2D.md): a [`Vector2`](Vector2.md) `half_extents`,

- [`PhysicsServer2D.ShapeType.Capsule`](PhysicsServer2D.md): an array of length two (or a [`Vector2`](Vector2.md)) containing a `System.Single` `height` and a `System.Single` `radius`,

- [`PhysicsServer2D.ShapeType.ConvexPolygon`](PhysicsServer2D.md): either a [`Vector2`](Vector2.md)[] of points defining a convex polygon in counterclockwise order (the clockwise outward normal of each segment formed by consecutive points is calculated internally), or a `System.Single`[] of length divisible by four so that every 4-tuple of `System.Single`s contains the coordinates of a point followed by the coordinates of the clockwise outward normal vector to the segment between the current point and the next point,

- [`PhysicsServer2D.ShapeType.ConcavePolygon`](PhysicsServer2D.md): a [`Vector2`](Vector2.md)[] of length divisible by two (each pair of points forms one segment).

**Warning:** In the case of [`PhysicsServer2D.ShapeType.ConvexPolygon`](PhysicsServer2D.md), this method does not check if the points supplied actually form a convex polygon (unlike the [`CollisionPolygon2D.Polygon`](CollisionPolygon2D.md) property).

### SpaceCreate

```csharp
public static Rid SpaceCreate()
```

引擎名：`space_create`

Creates a 2D space in the physics server, and returns the [`Rid`](Rid.md) that identifies it. A space contains bodies and areas, and controls the stepping of the physics simulation of the objects in it.

### SpaceGetDirectState

```csharp
public static PhysicsDirectSpaceState2D SpaceGetDirectState(Rid space)
```

引擎名：`space_get_direct_state`

Returns the state of a space, a [`PhysicsDirectSpaceState2D`](PhysicsDirectSpaceState2D.md). This object can be used for collision/intersection queries.

### SpaceGetParam

```csharp
public static float SpaceGetParam(Rid space, PhysicsServer2D.SpaceParameter param)
```

引擎名：`space_get_param`

Returns the value of the given space parameter.

### SpaceIsActive

```csharp
public static bool SpaceIsActive(Rid space)
```

引擎名：`space_is_active`

Returns `true` if the space is active.

### SpaceSetActive

```csharp
public static void SpaceSetActive(Rid space, bool active)
```

引擎名：`space_set_active`

Activates or deactivates the space. If `active` is `false`, then the physics server will not do anything with this space in its physics step.

### SpaceSetParam

```csharp
public static void SpaceSetParam(Rid space, PhysicsServer2D.SpaceParameter param, float value)
```

引擎名：`space_set_param`

Sets the value of the given space parameter.

### WorldBoundaryShapeCreate

```csharp
public static Rid WorldBoundaryShapeCreate()
```

引擎名：`world_boundary_shape_create`

Creates a 2D world boundary shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's normal direction and distance properties.

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

## AreaSpaceOverrideMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：This area does not affect gravity/damp.
- `Combine = 1`：This area adds its gravity/damp values to whatever has been calculated so far.
- `CombineReplace = 2`：This area adds its gravity/damp values to whatever has been calculated so far.
- `Replace = 3`：This area replaces any gravity/damp, even the default one, and stops taking into account the rest of the areas.
- `ReplaceCombine = 4`：This area replaces any gravity/damp calculated so far, but keeps calculating the rest of the areas, down to the default one.

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
- `Max = 10`：Represents the size of the `PhysicsServer2D.BodyParameter` enum.

## BodyState

命名空间 `Godot`；enum

### 枚举值

- `Transform = 0`：Constant to set/get the current transform matrix of the body.
- `LinearVelocity = 1`：Constant to set/get the current linear velocity of the body.
- `AngularVelocity = 2`：Constant to set/get the current angular velocity of the body.
- `Sleeping = 3`：Constant to sleep/wake up a body, or to get whether it is sleeping.
- `CanSleep = 4`：Constant to set/get whether the body can sleep.

## CcdMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Disables continuous collision detection.
- `CastRay = 1`：Enables continuous collision detection by raycasting.
- `CastShape = 2`：Enables continuous collision detection by shapecasting.

## DampedSpringParam

命名空间 `Godot`；enum

### 枚举值

- `RestLength = 0`：Sets the resting length of the spring joint.
- `Stiffness = 1`：Sets the stiffness of the spring joint.
- `Damping = 2`：Sets the damping ratio of the spring joint.

## JointParam

命名空间 `Godot`；enum

### 枚举值

- `Bias = 0`：Constant to set/get how fast the joint pulls the bodies back to satisfy the joint constraint.
- `MaxBias = 1`：Constant to set/get the maximum speed with which the joint can apply corrections.
- `MaxForce = 2`：Constant to set/get the maximum force that the joint can use to act on the two bodies.

## JointType

命名空间 `Godot`；enum

### 枚举值

- `Pin = 0`：Constant to create pin joints.
- `Groove = 1`：Constant to create groove joints.
- `DampedSpring = 2`：Constant to create damped spring joints.
- `Max = 3`：Represents the size of the `PhysicsServer2D.JointType` enum.

## PinJointFlag

命名空间 `Godot`；enum

### 枚举值

- `AngularLimitEnabled = 0`：If `true`, the pin has a maximum and a minimum rotation.
- `MotorEnabled = 1`：If `true`, a motor turns the pin.

## PinJointParam

命名空间 `Godot`；enum

### 枚举值

- `Softness = 0`：Constant to set/get a how much the bond of the pin joint can flex.
- `LimitUpper = 1`：The maximum rotation around the pin.
- `LimitLower = 2`：The minimum rotation around the pin.
- `MotorTargetVelocity = 3`：Target speed for the motor.

## ProcessInfo

命名空间 `Godot`；enum

### 枚举值

- `ActiveObjects = 0`：Constant to get the number of objects that are not sleeping.
- `CollisionPairs = 1`：Constant to get the number of possible collisions.
- `IslandCount = 2`：Constant to get the number of space regions where a collision could occur.

## ShapeType

命名空间 `Godot`；enum

### 枚举值

- `WorldBoundary = 0`：This is the constant for creating world boundary shapes.
- `SeparationRay = 1`：This is the constant for creating separation ray shapes.
- `Segment = 2`：This is the constant for creating segment shapes.
- `Circle = 3`：This is the constant for creating circle shapes.
- `Rectangle = 4`：This is the constant for creating rectangle shapes.
- `Capsule = 5`：This is the constant for creating capsule shapes.
- `ConvexPolygon = 6`：This is the constant for creating convex polygon shapes.
- `ConcavePolygon = 7`：This is the constant for creating concave polygon shapes.
- `Custom = 8`：This constant is used internally by the engine.

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
- `ConstraintDefaultBias = 7`：Constant to set/get the default solver bias for all physics constraints.
- `SolverIterations = 8`：Constant to set/get the number of solver iterations for all contacts and constraints.
