<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer2DInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

PhysicsServer2D is the server responsible for all 2D physics. It can directly create and manipulate all physics objects:

- A *space* is a self-contained world for a physics simulation. It contains bodies, areas, and joints. Its state can be queried for collision and intersection information, and several parameters of the simulation can be modified.

- A *shape* is a geometric shape such as a circle, a rectangle, a capsule, or a polygon. It can be used for collision detection by adding it to a body/area, possibly with an extra transformation relative to the body/area's origin. Bodies/areas can have multiple (transformed) shapes added to them, and a single shape can be added to bodies/areas multiple times with different local transformations.

- A *body* is a physical object which can be in static, kinematic, or rigid mode. Its state (such as position and velocity) can be queried and updated. A force integration callback can be set to customize the body's physics.

- An *area* is a region in space which can be used to detect bodies and areas entering and exiting it. A body monitoring callback can be set to report entering/exiting body shapes, and similarly an area monitoring callback can be set. Gravity and damping can be overridden within the area by setting area parameters.

- A *joint* is a constraint, either between two bodies or on one body relative to a point. Parameters such as the joint bias and the rest length of a spring joint can be adjusted.

Physics objects in [`PhysicsServer2D`](PhysicsServer2D.md) may be created and manipulated independently; they do not have to be tied to nodes in the scene tree.

**Note:** All the 2D physics nodes use the physics server internally. Adding a physics node to the scene tree will cause a corresponding physics object to be created in the physics server. A rigid body node registers a callback that updates the node's transform with the transform of the respective body object in the physics server (every physics update). An area node registers a callback to inform the area node about overlaps with the respective area object in the physics server. The raycast node queries the direct state of the relevant space in the physics server.

## 方法

### AreaAddShape

```csharp
public void AreaAddShape(Rid area, Rid shape, Transform2D? transform = null, bool disabled = false)
```

Adds a shape to the area, with the given local transform. The shape (together with its `transform` and `disabled` properties) is added to an array of shapes, and the shapes of an area are usually referenced by their index in this array.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### AreaAttachCanvasInstanceId

```csharp
public void AreaAttachCanvasInstanceId(Rid area, ulong id)
```

Attaches the `ObjectID` of a canvas to the area. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CanvasLayer`](CanvasLayer.md).

### AreaAttachObjectInstanceId

```csharp
public void AreaAttachObjectInstanceId(Rid area, ulong id)
```

Attaches the `ObjectID` of an [`GodotObject`](GodotObject.md) to the area. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CollisionObject2D`](CollisionObject2D.md).

### AreaClearShapes

```csharp
public void AreaClearShapes(Rid area)
```

Removes all shapes from the area. This does not delete the shapes themselves, so they can continue to be used elsewhere or added back later.

### AreaCreate

```csharp
public Rid AreaCreate()
```

Creates a 2D area object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and `monitorable` set to `false`.

Use `AreaAddShape()` to add shapes to it, use `AreaSetTransform()` to set its transform, and use `AreaSetSpace()` to add the area to a space. If you want the area to be detectable use `AreaSetMonitorable()`.

### AreaGetCanvasInstanceId

```csharp
public ulong AreaGetCanvasInstanceId(Rid area)
```

Returns the `ObjectID` of the canvas attached to the area. Use `GodotObject.InstanceFromId` to retrieve a [`CanvasLayer`](CanvasLayer.md) from a nonzero `ObjectID`.

### AreaGetCollisionLayer

```csharp
public uint AreaGetCollisionLayer(Rid area)
```

Returns the physics layer or layers the area belongs to, as a bitmask.

### AreaGetCollisionMask

```csharp
public uint AreaGetCollisionMask(Rid area)
```

Returns the physics layer or layers the area can contact with, as a bitmask.

### AreaGetObjectInstanceId

```csharp
public ulong AreaGetObjectInstanceId(Rid area)
```

Returns the `ObjectID` attached to the area. Use `GodotObject.InstanceFromId` to retrieve an [`GodotObject`](GodotObject.md) from a nonzero `ObjectID`.

### AreaGetParam

```csharp
public Variant AreaGetParam(Rid area, PhysicsServer2D.AreaParameter param)
```

Returns the value of the given area parameter.

### AreaGetShape

```csharp
public Rid AreaGetShape(Rid area, int shapeIdx)
```

Returns the [`Rid`](Rid.md) of the shape with the given index in the area's array of shapes.

### AreaGetShapeCount

```csharp
public int AreaGetShapeCount(Rid area)
```

Returns the number of shapes added to the area.

### AreaGetShapeTransform

```csharp
public Transform2D AreaGetShapeTransform(Rid area, int shapeIdx)
```

Returns the local transform matrix of the shape with the given index in the area's array of shapes.

### AreaGetSpace

```csharp
public Rid AreaGetSpace(Rid area)
```

Returns the [`Rid`](Rid.md) of the space assigned to the area. Returns an empty [`Rid`](Rid.md) if no space is assigned.

### AreaGetTransform

```csharp
public Transform2D AreaGetTransform(Rid area)
```

Returns the transform matrix of the area.

### AreaRemoveShape

```csharp
public void AreaRemoveShape(Rid area, int shapeIdx)
```

Removes the shape with the given index from the area's array of shapes. The shape itself is not deleted, so it can continue to be used elsewhere or added back later. As a result of this operation, the area's shapes which used to have indices higher than `shapeIdx` will have their index decreased by one.

### AreaSetAreaMonitorCallback

```csharp
public void AreaSetAreaMonitorCallback(Rid area, Callable callback)
```

Sets the area's area monitor callback. This callback will be called when any other (shape of an) area enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer2D.AreaBodyStatus.Added`](PhysicsServer2D.md) or [`PhysicsServer2D.AreaBodyStatus.Removed`](PhysicsServer2D.md) depending on whether the other area's shape entered or exited the area,

2. an [`Rid`](Rid.md) `area_rid`: the [`Rid`](Rid.md) of the other area that entered or exited the area,

3. an integer `instance_id`: the `ObjectID` attached to the other area,

4. an integer `area_shape_idx`: the index of the shape of the other area that entered or exited the area,

5. an integer `self_shape_idx`: the index of the shape of the area where the other area entered or exited.

By counting (or keeping track of) the shapes that enter and exit, it can be determined if an area (with all its shapes) is entering for the first time or exiting for the last time.

### AreaSetCollisionLayer

```csharp
public void AreaSetCollisionLayer(Rid area, uint layer)
```

Assigns the area to one or many physics layers, via a bitmask.

### AreaSetCollisionMask

```csharp
public void AreaSetCollisionMask(Rid area, uint mask)
```

Sets which physics layers the area will monitor, via a bitmask.

### AreaSetMonitorCallback

```csharp
public void AreaSetMonitorCallback(Rid area, Callable callback)
```

Sets the area's body monitor callback. This callback will be called when any other (shape of a) body enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer2D.AreaBodyStatus.Added`](PhysicsServer2D.md) or [`PhysicsServer2D.AreaBodyStatus.Removed`](PhysicsServer2D.md) depending on whether the other body shape entered or exited the area,

2. an [`Rid`](Rid.md) `body_rid`: the [`Rid`](Rid.md) of the body that entered or exited the area,

3. an integer `instance_id`: the `ObjectID` attached to the body,

4. an integer `body_shape_idx`: the index of the shape of the body that entered or exited the area,

5. an integer `self_shape_idx`: the index of the shape of the area where the body entered or exited.

By counting (or keeping track of) the shapes that enter and exit, it can be determined if a body (with all its shapes) is entering for the first time or exiting for the last time.

### AreaSetMonitorable

```csharp
public void AreaSetMonitorable(Rid area, bool monitorable)
```

Sets whether the area is monitorable or not. If `monitorable` is `true`, the area monitoring callback of other areas will be called when this area enters or exits them.

### AreaSetParam

```csharp
public void AreaSetParam(Rid area, PhysicsServer2D.AreaParameter param, Variant value)
```

Sets the value of the given area parameter.

### AreaSetShape

```csharp
public void AreaSetShape(Rid area, int shapeIdx, Rid shape)
```

Replaces the area's shape at the given index by another shape, while not affecting the `transform` and `disabled` properties at the same index.

### AreaSetShapeDisabled

```csharp
public void AreaSetShapeDisabled(Rid area, int shapeIdx, bool disabled)
```

Sets the disabled property of the area's shape with the given index. If `disabled` is `true`, then the shape will not detect any other shapes entering or exiting it.

### AreaSetShapeTransform

```csharp
public void AreaSetShapeTransform(Rid area, int shapeIdx, Transform2D transform)
```

Sets the local transform matrix of the area's shape with the given index.

### AreaSetSpace

```csharp
public void AreaSetSpace(Rid area, Rid space)
```

Adds the area to the given space, after removing the area from the previously assigned space (if any).

**Note:** To remove an area from a space without immediately adding it back elsewhere, use `PhysicsServer2D.area_set_space(area, RID())`.

### AreaSetTransform

```csharp
public void AreaSetTransform(Rid area, Transform2D transform)
```

Sets the transform matrix of the area.

### BodyAddCollisionException

```csharp
public void BodyAddCollisionException(Rid body, Rid exceptedBody)
```

Adds `exceptedBody` to the body's list of collision exceptions, so that collisions with it are ignored.

### BodyAddConstantCentralForce

```csharp
public void BodyAddConstantCentralForce(Rid body, Vector2 force)
```

Adds a constant directional force to the body. The force does not affect rotation. The force remains applied over time until cleared with `PhysicsServer2D.body_set_constant_force(body, Vector2(0, 0))`.

This is equivalent to using `BodyAddConstantForce()` at the body's center of mass.

### BodyAddConstantForce

```csharp
public void BodyAddConstantForce(Rid body, Vector2 force, Vector2? position = null)
```

Adds a constant positioned force to the body. The force can affect rotation if `position` is different from the body's center of mass. The force remains applied over time until cleared with `PhysicsServer2D.body_set_constant_force(body, Vector2(0, 0))`.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BodyAddConstantTorque

```csharp
public void BodyAddConstantTorque(Rid body, float torque)
```

Adds a constant rotational force to the body. The force does not affect position. The force remains applied over time until cleared with `PhysicsServer2D.body_set_constant_torque(body, 0)`.

### BodyAddShape

```csharp
public void BodyAddShape(Rid body, Rid shape, Transform2D? transform = null, bool disabled = false)
```

Adds a shape to the area, with the given local transform. The shape (together with its `transform` and `disabled` properties) is added to an array of shapes, and the shapes of a body are usually referenced by their index in this array.

参数：
- `transform`：If the parameter is null, then the default value is `Transform2D.Identity`.

### BodyApplyCentralForce

```csharp
public void BodyApplyCentralForce(Rid body, Vector2 force)
```

Applies a directional force to the body, at the body's center of mass. The force does not affect rotation. A force is time dependent and meant to be applied every physics update.

This is equivalent to using `BodyApplyForce()` at the body's center of mass.

### BodyApplyCentralImpulse

```csharp
public void BodyApplyCentralImpulse(Rid body, Vector2 impulse)
```

Applies a directional impulse to the body, at the body's center of mass. The impulse does not affect rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

This is equivalent to using `BodyApplyImpulse()` at the body's center of mass.

### BodyApplyForce

```csharp
public void BodyApplyForce(Rid body, Vector2 force, Vector2? position = null)
```

Applies a positioned force to the body. The force can affect rotation if `position` is different from the body's center of mass. A force is time dependent and meant to be applied every physics update.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BodyApplyImpulse

```csharp
public void BodyApplyImpulse(Rid body, Vector2 impulse, Vector2? position = null)
```

Applies a positioned impulse to the body. The impulse can affect rotation if `position` is different from the body's center of mass.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### BodyApplyTorque

```csharp
public void BodyApplyTorque(Rid body, float torque)
```

Applies a rotational force to the body. The force does not affect position. A force is time dependent and meant to be applied every physics update.

### BodyApplyTorqueImpulse

```csharp
public void BodyApplyTorqueImpulse(Rid body, float impulse)
```

Applies a rotational impulse to the body. The impulse does not affect position.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### BodyAttachCanvasInstanceId

```csharp
public void BodyAttachCanvasInstanceId(Rid body, ulong id)
```

Attaches the `ObjectID` of a canvas to the body. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CanvasLayer`](CanvasLayer.md).

### BodyAttachObjectInstanceId

```csharp
public void BodyAttachObjectInstanceId(Rid body, ulong id)
```

Attaches the `ObjectID` of an [`GodotObject`](GodotObject.md) to the body. Use [`GodotObject.GetInstanceId`](GodotObject.md) to get the `ObjectID` of a [`CollisionObject2D`](CollisionObject2D.md).

### BodyClearShapes

```csharp
public void BodyClearShapes(Rid body)
```

Removes all shapes from the body. This does not delete the shapes themselves, so they can continue to be used elsewhere or added back later.

### BodyCreate

```csharp
public Rid BodyCreate()
```

Creates a 2D body object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and body mode set to [`PhysicsServer2D.BodyMode.Rigid`](PhysicsServer2D.md).

Use `BodyAddShape()` to add shapes to it, use `BodySetState()` to set its transform, and use `BodySetSpace()` to add the body to a space.

### BodyGetCanvasInstanceId

```csharp
public ulong BodyGetCanvasInstanceId(Rid body)
```

Returns the `ObjectID` of the canvas attached to the body. Use `GodotObject.InstanceFromId` to retrieve a [`CanvasLayer`](CanvasLayer.md) from a nonzero `ObjectID`.

### BodyGetCollisionLayer

```csharp
public uint BodyGetCollisionLayer(Rid body)
```

Returns the physics layer or layers the body belongs to, as a bitmask.

### BodyGetCollisionMask

```csharp
public uint BodyGetCollisionMask(Rid body)
```

Returns the physics layer or layers the body can collide with, as a bitmask.

### BodyGetCollisionPriority

```csharp
public float BodyGetCollisionPriority(Rid body)
```

Returns the body's collision priority. This is used in the depenetration phase of `BodyTestMotion()`. The higher the priority is, the lower the penetration into the body will be.

### BodyGetConstantForce

```csharp
public Vector2 BodyGetConstantForce(Rid body)
```

Returns the body's total constant positional force applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodyGetConstantTorque

```csharp
public float BodyGetConstantTorque(Rid body)
```

Returns the body's total constant rotational force applied during each physics update.

See `BodyAddConstantTorque()`.

### BodyGetContinuousCollisionDetectionMode

```csharp
public PhysicsServer2D.CcdMode BodyGetContinuousCollisionDetectionMode(Rid body)
```

Returns the body's continuous collision detection mode.

### BodyGetDirectState

```csharp
public PhysicsDirectBodyState2D BodyGetDirectState(Rid body)
```

Returns the [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md) of the body. Returns `null` if the body is destroyed or not assigned to a space.

### BodyGetMaxContactsReported

```csharp
public int BodyGetMaxContactsReported(Rid body)
```

Returns the maximum number of contacts that the body can report. See `BodySetMaxContactsReported()`.

### BodyGetMode

```csharp
public PhysicsServer2D.BodyMode BodyGetMode(Rid body)
```

Returns the body's mode.

### BodyGetObjectInstanceId

```csharp
public ulong BodyGetObjectInstanceId(Rid body)
```

Returns the `ObjectID` attached to the body. Use `GodotObject.InstanceFromId` to retrieve an [`GodotObject`](GodotObject.md) from a nonzero `ObjectID`.

### BodyGetParam

```csharp
public Variant BodyGetParam(Rid body, PhysicsServer2D.BodyParameter param)
```

Returns the value of the given body parameter.

### BodyGetShape

```csharp
public Rid BodyGetShape(Rid body, int shapeIdx)
```

Returns the [`Rid`](Rid.md) of the shape with the given index in the body's array of shapes.

### BodyGetShapeCount

```csharp
public int BodyGetShapeCount(Rid body)
```

Returns the number of shapes added to the body.

### BodyGetShapeTransform

```csharp
public Transform2D BodyGetShapeTransform(Rid body, int shapeIdx)
```

Returns the local transform matrix of the shape with the given index in the area's array of shapes.

### BodyGetSpace

```csharp
public Rid BodyGetSpace(Rid body)
```

Returns the [`Rid`](Rid.md) of the space assigned to the body. Returns an empty [`Rid`](Rid.md) if no space is assigned.

### BodyGetState

```csharp
public Variant BodyGetState(Rid body, PhysicsServer2D.BodyState state)
```

Returns the value of the given state of the body.

### BodyIsOmittingForceIntegration

```csharp
public bool BodyIsOmittingForceIntegration(Rid body)
```

Returns `true` if the body is omitting the standard force integration. See `BodySetOmitForceIntegration()`.

### BodyRemoveCollisionException

```csharp
public void BodyRemoveCollisionException(Rid body, Rid exceptedBody)
```

Removes `exceptedBody` from the body's list of collision exceptions, so that collisions with it are no longer ignored.

### BodyRemoveShape

```csharp
public void BodyRemoveShape(Rid body, int shapeIdx)
```

Removes the shape with the given index from the body's array of shapes. The shape itself is not deleted, so it can continue to be used elsewhere or added back later. As a result of this operation, the body's shapes which used to have indices higher than `shapeIdx` will have their index decreased by one.

### BodyResetMassProperties

```csharp
public void BodyResetMassProperties(Rid body)
```

Restores the default inertia and center of mass of the body based on its shapes. This undoes any custom values previously set using `BodySetParam()`.

### BodySetAxisVelocity

```csharp
public void BodySetAxisVelocity(Rid body, Vector2 axisVelocity)
```

Modifies the body's linear velocity so that its projection to the axis `axis_velocity.normalized()` is exactly `axis_velocity.length()`. This is useful for jumping behavior.

### BodySetCollisionLayer

```csharp
public void BodySetCollisionLayer(Rid body, uint layer)
```

Sets the physics layer or layers the body belongs to, via a bitmask.

### BodySetCollisionMask

```csharp
public void BodySetCollisionMask(Rid body, uint mask)
```

Sets the physics layer or layers the body can collide with, via a bitmask.

### BodySetCollisionPriority

```csharp
public void BodySetCollisionPriority(Rid body, float priority)
```

Sets the body's collision priority. This is used in the depenetration phase of `BodyTestMotion()`. The higher the priority is, the lower the penetration into the body will be.

### BodySetConstantForce

```csharp
public void BodySetConstantForce(Rid body, Vector2 force)
```

Sets the body's total constant positional force applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodySetConstantTorque

```csharp
public void BodySetConstantTorque(Rid body, float torque)
```

Sets the body's total constant rotational force applied during each physics update.

See `BodyAddConstantTorque()`.

### BodySetContinuousCollisionDetectionMode

```csharp
public void BodySetContinuousCollisionDetectionMode(Rid body, PhysicsServer2D.CcdMode mode)
```

Sets the continuous collision detection mode.

Continuous collision detection tries to predict where a moving body would collide in between physics updates, instead of moving it and correcting its movement if it collided.

### BodySetForceIntegrationCallback

```csharp
public void BodySetForceIntegrationCallback(Rid body, Callable callable, Variant userdata = default)
```

Sets the body's custom force integration callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the custom callback.

The function `callable` will be called every physics tick, before the standard force integration (see `BodySetOmitForceIntegration()`). It can be used for example to update the body's linear and angular velocity based on contact with other bodies.

If `userdata` is not `null`, the function `callable` must take the following two parameters:

1. `state`: a [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md) used to retrieve and modify the body's state,

2. `userdata`: a [`Variant`](Variant.md); its value will be the `userdata` passed into this method.

If `userdata` is `null`, then `callable` must take only the `state` parameter.

### BodySetMaxContactsReported

```csharp
public void BodySetMaxContactsReported(Rid body, int amount)
```

Sets the maximum number of contacts that the body can report. If `amount` is greater than zero, then the body will keep track of at most this many contacts with other bodies.

### BodySetMode

```csharp
public void BodySetMode(Rid body, PhysicsServer2D.BodyMode mode)
```

Sets the body's mode.

### BodySetOmitForceIntegration

```csharp
public void BodySetOmitForceIntegration(Rid body, bool enable)
```

Sets whether the body omits the standard force integration. If `enable` is `true`, the body will not automatically use applied forces, torques, and damping to update the body's linear and angular velocity. In this case, `BodySetForceIntegrationCallback()` can be used to manually update the linear and angular velocity instead.

This method is called when the property [`RigidBody2D.CustomIntegrator`](RigidBody2D.md) is set.

### BodySetParam

```csharp
public void BodySetParam(Rid body, PhysicsServer2D.BodyParameter param, Variant value)
```

Sets the value of the given body parameter.

### BodySetShape

```csharp
public void BodySetShape(Rid body, int shapeIdx, Rid shape)
```

Replaces the body's shape at the given index by another shape, while not affecting the `transform`, `disabled`, and one-way collision properties at the same index.

### BodySetShapeAsOneWayCollision

```csharp
public void BodySetShapeAsOneWayCollision(Rid body, int shapeIdx, bool enable, float margin)
```

Sets the one-way collision properties of the body's shape with the given index. If `enable` is `true`, the one-way collision direction given by `direction` in the shape's local space (that is `body_get_shape_transform(body, shape_idx).basis_xform(direction).normalized()` in the body's local space) will be used to ignore collisions with the shape in the opposite direction, and to ensure depenetration of kinematic bodies happens in this direction.

### BodySetShapeAsOneWayCollision

```csharp
public void BodySetShapeAsOneWayCollision(Rid body, int shapeIdx, bool enable, float margin, Vector2? direction = null)
```

Sets the one-way collision properties of the body's shape with the given index. If `enable` is `true`, the one-way collision direction given by `direction` in the shape's local space (that is `body_get_shape_transform(body, shape_idx).basis_xform(direction).normalized()` in the body's local space) will be used to ignore collisions with the shape in the opposite direction, and to ensure depenetration of kinematic bodies happens in this direction.

参数：
- `direction`：If the parameter is null, then the default value is `new Vector2(0.0f, 1.0f)`.

### BodySetShapeDisabled

```csharp
public void BodySetShapeDisabled(Rid body, int shapeIdx, bool disabled)
```

Sets the disabled property of the body's shape with the given index. If `disabled` is `true`, then the shape will be ignored in all collision detection.

### BodySetShapeTransform

```csharp
public void BodySetShapeTransform(Rid body, int shapeIdx, Transform2D transform)
```

Sets the local transform matrix of the body's shape with the given index.

### BodySetSpace

```csharp
public void BodySetSpace(Rid body, Rid space)
```

Adds the body to the given space, after removing the body from the previously assigned space (if any). If the body's mode is set to [`PhysicsServer2D.BodyMode.Rigid`](PhysicsServer2D.md), then adding the body to a space will have the following additional effects:

- If the parameter [`PhysicsServer2D.BodyParameter.CenterOfMass`](PhysicsServer2D.md) has never been set explicitly, then the value of that parameter will be recalculated based on the body's shapes.

- If the parameter [`PhysicsServer2D.BodyParameter.Inertia`](PhysicsServer2D.md) is set to a value `<= 0.0`, then the value of that parameter will be recalculated based on the body's shapes, mass, and center of mass.

**Note:** To remove a body from a space without immediately adding it back elsewhere, use `PhysicsServer2D.body_set_space(body, RID())`.

### BodySetState

```csharp
public void BodySetState(Rid body, PhysicsServer2D.BodyState state, Variant value)
```

Sets the value of a body's state.

**Note:** The state change doesn't take effect immediately. The state will change on the next physics frame.

### BodySetStateSyncCallback

```csharp
public void BodySetStateSyncCallback(Rid body, Callable callable)
```

Sets the body's state synchronization callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the callback.

The function `callable` will be called every physics frame, assuming that the body was active during the previous physics tick, and can be used to fetch the latest state from the physics server.

The function `callable` must take the following parameters:

1. `state`: a [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md), used to retrieve the body's state.

### BodyTestMotion

```csharp
public bool BodyTestMotion(Rid body, PhysicsTestMotionParameters2D parameters, PhysicsTestMotionResult2D result = null)
```

Returns `true` if a collision would result from moving the body along a motion vector from a given point in space. See [`PhysicsTestMotionParameters2D`](PhysicsTestMotionParameters2D.md) for the available motion parameters. Optionally a [`PhysicsTestMotionResult2D`](PhysicsTestMotionResult2D.md) object can be passed, which will be used to store the information about the resulting collision.

### CapsuleShapeCreate

```csharp
public Rid CapsuleShapeCreate()
```

Creates a 2D capsule shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the capsule's height and radius.

### CircleShapeCreate

```csharp
public Rid CircleShapeCreate()
```

Creates a 2D circle shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the circle's radius.

### ConcavePolygonShapeCreate

```csharp
public Rid ConcavePolygonShapeCreate()
```

Creates a 2D concave polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the concave polygon's segments.

### ConvexPolygonShapeCreate

```csharp
public Rid ConvexPolygonShapeCreate()
```

Creates a 2D convex polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the convex polygon's points.

### DampedSpringJointGetParam

```csharp
public float DampedSpringJointGetParam(Rid joint, PhysicsServer2D.DampedSpringParam param)
```

Returns the value of the given damped spring joint parameter.

### DampedSpringJointSetParam

```csharp
public void DampedSpringJointSetParam(Rid joint, PhysicsServer2D.DampedSpringParam param, float value)
```

Sets the value of the given damped spring joint parameter.

### FreeRid

```csharp
public void FreeRid(Rid rid)
```

Destroys any of the objects created by PhysicsServer2D. If the [`Rid`](Rid.md) passed is not one of the objects that can be created by PhysicsServer2D, an error will be printed to the console.

### GetProcessInfo

```csharp
public int GetProcessInfo(PhysicsServer2D.ProcessInfo processInfo)
```

Returns the value of a physics engine state specified by `processInfo`.

### JointClear

```csharp
public void JointClear(Rid joint)
```

Destroys the joint with the given [`Rid`](Rid.md), creates a new uninitialized joint, and makes the [`Rid`](Rid.md) refer to this new joint.

### JointCreate

```csharp
public Rid JointCreate()
```

Creates a 2D joint in the physics server, and returns the [`Rid`](Rid.md) that identifies it. To set the joint type, use `JointMakeDampedSpring()`, `JointMakeGroove()` or `JointMakePin()`. Use `JointSetParam()` to set generic joint parameters.

### JointDisableCollisionsBetweenBodies

```csharp
public void JointDisableCollisionsBetweenBodies(Rid joint, bool disable)
```

Sets whether the bodies attached to the [`Joint2D`](Joint2D.md) will collide with each other.

### JointGetParam

```csharp
public float JointGetParam(Rid joint, PhysicsServer2D.JointParam param)
```

Returns the value of the given joint parameter.

### JointGetType

```csharp
public PhysicsServer2D.JointType JointGetType(Rid joint)
```

Returns the joint's type.

### JointIsDisabledCollisionsBetweenBodies

```csharp
public bool JointIsDisabledCollisionsBetweenBodies(Rid joint)
```

Returns whether the bodies attached to the [`Joint2D`](Joint2D.md) will collide with each other.

### JointMakeDampedSpring

```csharp
public void JointMakeDampedSpring(Rid joint, Vector2 anchorA, Vector2 anchorB, Rid bodyA, Rid bodyB = default)
```

Makes the joint a damped spring joint, attached at the point `anchorA` (given in global coordinates) on the body `bodyA` and at the point `anchorB` (given in global coordinates) on the body `bodyB`. To set the parameters which are specific to the damped spring, see `DampedSpringJointSetParam()`.

### JointMakeGroove

```csharp
public void JointMakeGroove(Rid joint, Vector2 groove1A, Vector2 groove2A, Vector2 anchorB, Rid bodyA = default, Rid bodyB = default)
```

Makes the joint a groove joint.

### JointMakePin

```csharp
public void JointMakePin(Rid joint, Vector2 anchor, Rid bodyA, Rid bodyB = default)
```

Makes the joint a pin joint. If `bodyB` is an empty [`Rid`](Rid.md), then `bodyA` is pinned to the point `anchor` (given in global coordinates); otherwise, `bodyA` is pinned to `bodyB` at the point `anchor` (given in global coordinates). To set the parameters which are specific to the pin joint, see `PinJointSetParam()`.

### JointSetParam

```csharp
public void JointSetParam(Rid joint, PhysicsServer2D.JointParam param, float value)
```

Sets the value of the given joint parameter.

### PinJointGetFlag

```csharp
public bool PinJointGetFlag(Rid joint, PhysicsServer2D.PinJointFlag flag)
```

Gets a pin joint flag.

### PinJointGetParam

```csharp
public float PinJointGetParam(Rid joint, PhysicsServer2D.PinJointParam param)
```

Returns the value of a pin joint parameter.

### PinJointSetFlag

```csharp
public void PinJointSetFlag(Rid joint, PhysicsServer2D.PinJointFlag flag, bool enabled)
```

Sets a pin joint flag.

### PinJointSetParam

```csharp
public void PinJointSetParam(Rid joint, PhysicsServer2D.PinJointParam param, float value)
```

Sets a pin joint parameter.

### RectangleShapeCreate

```csharp
public Rid RectangleShapeCreate()
```

Creates a 2D rectangle shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the rectangle's half-extents.

### SegmentShapeCreate

```csharp
public Rid SegmentShapeCreate()
```

Creates a 2D segment shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the segment's start and end points.

### SeparationRayShapeCreate

```csharp
public Rid SeparationRayShapeCreate()
```

Creates a 2D separation ray shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's `length` and `slide_on_slope` properties.

### SetActive

```csharp
public void SetActive(bool active)
```

Activates or deactivates the 2D physics server. If `active` is `false`, then the physics server will not do anything in its physics step.

### ShapeGetData

```csharp
public Variant ShapeGetData(Rid shape)
```

Returns the shape data that defines the configuration of the shape, such as the half-extents of a rectangle or the segments of a concave shape. See `ShapeSetData()` for the precise format of this data in each case.

### ShapeGetType

```csharp
public PhysicsServer2D.ShapeType ShapeGetType(Rid shape)
```

Returns the shape's type.

### ShapeSetData

```csharp
public void ShapeSetData(Rid shape, Variant data)
```

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
public Rid SpaceCreate()
```

Creates a 2D space in the physics server, and returns the [`Rid`](Rid.md) that identifies it. A space contains bodies and areas, and controls the stepping of the physics simulation of the objects in it.

### SpaceGetDirectState

```csharp
public PhysicsDirectSpaceState2D SpaceGetDirectState(Rid space)
```

Returns the state of a space, a [`PhysicsDirectSpaceState2D`](PhysicsDirectSpaceState2D.md). This object can be used for collision/intersection queries.

### SpaceGetParam

```csharp
public float SpaceGetParam(Rid space, PhysicsServer2D.SpaceParameter param)
```

Returns the value of the given space parameter.

### SpaceIsActive

```csharp
public bool SpaceIsActive(Rid space)
```

Returns `true` if the space is active.

### SpaceSetActive

```csharp
public void SpaceSetActive(Rid space, bool active)
```

Activates or deactivates the space. If `active` is `false`, then the physics server will not do anything with this space in its physics step.

### SpaceSetParam

```csharp
public void SpaceSetParam(Rid space, PhysicsServer2D.SpaceParameter param, float value)
```

Sets the value of the given space parameter.

### WorldBoundaryShapeCreate

```csharp
public Rid WorldBoundaryShapeCreate()
```

Creates a 2D world boundary shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's normal direction and distance properties.
