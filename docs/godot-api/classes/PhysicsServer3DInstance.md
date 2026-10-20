<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsServer3DInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

PhysicsServer3D is the server responsible for all 3D physics. It can directly create and manipulate all physics objects:

- A *space* is a self-contained world for a physics simulation. It contains bodies, areas, and joints. Its state can be queried for collision and intersection information, and several parameters of the simulation can be modified.

- A *shape* is a geometric shape such as a sphere, a box, a cylinder, or a polygon. It can be used for collision detection by adding it to a body/area, possibly with an extra transformation relative to the body/area's origin. Bodies/areas can have multiple (transformed) shapes added to them, and a single shape can be added to bodies/areas multiple times with different local transformations.

- A *body* is a physical object which can be in static, kinematic, or rigid mode. Its state (such as position and velocity) can be queried and updated. A force integration callback can be set to customize the body's physics.

- An *area* is a region in space which can be used to detect bodies and areas entering and exiting it. A body monitoring callback can be set to report entering/exiting body shapes, and similarly an area monitoring callback can be set. Gravity and damping can be overridden within the area by setting area parameters.

- A *joint* is a constraint, either between two bodies or on one body relative to a point. Parameters such as the joint bias and the rest length of a spring joint can be adjusted.

Physics objects in [`PhysicsServer3D`](PhysicsServer3D.md) may be created and manipulated independently; they do not have to be tied to nodes in the scene tree.

**Note:** All the 3D physics nodes use the physics server internally. Adding a physics node to the scene tree will cause a corresponding physics object to be created in the physics server. A rigid body node registers a callback that updates the node's transform with the transform of the respective body object in the physics server (every physics update). An area node registers a callback to inform the area node about overlaps with the respective area object in the physics server. The raycast node queries the direct state of the relevant space in the physics server.

## 方法

### AreaAddShape

```csharp
public void AreaAddShape(Rid area, Rid shape, Transform3D? transform = null, bool disabled = false)
```

Adds a shape to the area, along with a transform matrix. Shapes are usually referenced by their index, so you should track which shape has a given index.

参数：
- `transform`：If the parameter is null, then the default value is `Transform3D.Identity`.

### AreaAttachObjectInstanceId

```csharp
public void AreaAttachObjectInstanceId(Rid area, ulong id)
```

Assigns the area to a descendant of [`GodotObject`](GodotObject.md), so it can exist in the node tree.

### AreaClearShapes

```csharp
public void AreaClearShapes(Rid area)
```

Removes all shapes from an area. It does not delete the shapes, so they can be reassigned later.

### AreaCreate

```csharp
public Rid AreaCreate()
```

Creates a 3D area object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and `monitorable` set to `false`.

Use `AreaAddShape()` to add shapes to it, use `AreaSetTransform()` to set its transform, and use `AreaSetSpace()` to add the area to a space. If you want the area to be detectable use `AreaSetMonitorable()`.

### AreaGetCollisionLayer

```csharp
public uint AreaGetCollisionLayer(Rid area)
```

Returns the physics layer or layers an area belongs to.

### AreaGetCollisionMask

```csharp
public uint AreaGetCollisionMask(Rid area)
```

Returns the physics layer or layers an area can contact with.

### AreaGetObjectInstanceId

```csharp
public ulong AreaGetObjectInstanceId(Rid area)
```

Gets the instance ID of the object the area is assigned to.

### AreaGetParam

```csharp
public Variant AreaGetParam(Rid area, PhysicsServer3D.AreaParameter param)
```

Returns an area parameter value. A list of available parameters is on the [`PhysicsServer3D.AreaParameter`](PhysicsServer3D.md) constants.

### AreaGetShape

```csharp
public Rid AreaGetShape(Rid area, int shapeIdx)
```

Returns the [`Rid`](Rid.md) of the nth shape of an area.

### AreaGetShapeCount

```csharp
public int AreaGetShapeCount(Rid area)
```

Returns the number of shapes assigned to an area.

### AreaGetShapeTransform

```csharp
public Transform3D AreaGetShapeTransform(Rid area, int shapeIdx)
```

Returns the transform matrix of a shape within an area.

### AreaGetSpace

```csharp
public Rid AreaGetSpace(Rid area)
```

Returns the space assigned to the area.

### AreaGetTransform

```csharp
public Transform3D AreaGetTransform(Rid area)
```

Returns the transform matrix for an area.

### AreaRemoveShape

```csharp
public void AreaRemoveShape(Rid area, int shapeIdx)
```

Removes a shape from an area. It does not delete the shape, so it can be reassigned later.

### AreaSetAreaMonitorCallback

```csharp
public void AreaSetAreaMonitorCallback(Rid area, Callable callback)
```

Sets the area's area monitor callback. This callback will be called when any other (shape of an) area enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer3D.AreaBodyStatus.Added`](PhysicsServer3D.md) or [`PhysicsServer3D.AreaBodyStatus.Removed`](PhysicsServer3D.md) depending on whether the other area's shape entered or exited the area,

2. an [`Rid`](Rid.md) `area_rid`: the [`Rid`](Rid.md) of the other area that entered or exited the area,

3. an integer `instance_id`: the `ObjectID` attached to the other area,

4. an integer `area_shape_idx`: the index of the shape of the other area that entered or exited the area,

5. an integer `self_shape_idx`: the index of the shape of the area where the other area entered or exited.

By counting (or keeping track of) the shapes that enter and exit, it can be determined if an area (with all its shapes) is entering for the first time or exiting for the last time.

### AreaSetCollisionLayer

```csharp
public void AreaSetCollisionLayer(Rid area, uint layer)
```

Assigns the area to one or many physics layers.

### AreaSetCollisionMask

```csharp
public void AreaSetCollisionMask(Rid area, uint mask)
```

Sets which physics layers the area will monitor.

### AreaSetMonitorCallback

```csharp
public void AreaSetMonitorCallback(Rid area, Callable callback)
```

Sets the area's body monitor callback. This callback will be called when any other (shape of a) body enters or exits (a shape of) the given area, and must take the following five parameters:

1. an integer `status`: either [`PhysicsServer3D.AreaBodyStatus.Added`](PhysicsServer3D.md) or [`PhysicsServer3D.AreaBodyStatus.Removed`](PhysicsServer3D.md) depending on whether the other body shape entered or exited the area,

2. an [`Rid`](Rid.md) `body_rid`: the [`Rid`](Rid.md) of the body that entered or exited the area,

3. an integer `instance_id`: the `ObjectID` attached to the body,

4. an integer `body_shape_idx`: the index of the shape of the body that entered or exited the area,

5. an integer `self_shape_idx`: the index of the shape of the area where the body entered or exited.

By counting (or keeping track of) the shapes that enter and exit, it can be determined if a body (with all its shapes) is entering for the first time or exiting for the last time.

### AreaSetMonitorable

```csharp
public void AreaSetMonitorable(Rid area, bool monitorable)
```

### AreaSetParam

```csharp
public void AreaSetParam(Rid area, PhysicsServer3D.AreaParameter param, Variant value)
```

Sets the value for an area parameter. A list of available parameters is on the [`PhysicsServer3D.AreaParameter`](PhysicsServer3D.md) constants.

### AreaSetRayPickable

```csharp
public void AreaSetRayPickable(Rid area, bool enable)
```

Sets object pickable with rays.

### AreaSetShape

```csharp
public void AreaSetShape(Rid area, int shapeIdx, Rid shape)
```

Substitutes a given area shape by another. The old shape is selected by its index, the new one by its [`Rid`](Rid.md).

### AreaSetShapeDisabled

```csharp
public void AreaSetShapeDisabled(Rid area, int shapeIdx, bool disabled)
```

### AreaSetShapeTransform

```csharp
public void AreaSetShapeTransform(Rid area, int shapeIdx, Transform3D transform)
```

Sets the transform matrix for an area shape.

### AreaSetSpace

```csharp
public void AreaSetSpace(Rid area, Rid space)
```

Assigns a space to the area.

### AreaSetTransform

```csharp
public void AreaSetTransform(Rid area, Transform3D transform)
```

Sets the transform matrix for an area.

### BodyAddCollisionException

```csharp
public void BodyAddCollisionException(Rid body, Rid exceptedBody)
```

Adds a body to the list of bodies exempt from collisions.

### BodyAddConstantCentralForce

```csharp
public void BodyAddConstantCentralForce(Rid body, Vector3 force)
```

Adds a constant directional force without affecting rotation that keeps being applied over time until cleared with `body_set_constant_force(body, Vector3(0, 0, 0))`.

This is equivalent to using `BodyAddConstantForce()` at the body's center of mass.

### BodyAddConstantForce

```csharp
public void BodyAddConstantForce(Rid body, Vector3 force, Vector3? position = null)
```

Adds a constant positioned force to the body that keeps being applied over time until cleared with `body_set_constant_force(body, Vector3(0, 0, 0))`.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### BodyAddConstantTorque

```csharp
public void BodyAddConstantTorque(Rid body, Vector3 torque)
```

Adds a constant rotational force without affecting position that keeps being applied over time until cleared with `body_set_constant_torque(body, Vector3(0, 0, 0))`.

### BodyAddShape

```csharp
public void BodyAddShape(Rid body, Rid shape, Transform3D? transform = null, bool disabled = false)
```

Adds a shape to the body, along with a transform matrix. Shapes are usually referenced by their index, so you should track which shape has a given index.

参数：
- `transform`：If the parameter is null, then the default value is `Transform3D.Identity`.

### BodyApplyCentralForce

```csharp
public void BodyApplyCentralForce(Rid body, Vector3 force)
```

Applies a directional force without affecting rotation. A force is time dependent and meant to be applied every physics update.

This is equivalent to using `BodyApplyForce()` at the body's center of mass.

### BodyApplyCentralImpulse

```csharp
public void BodyApplyCentralImpulse(Rid body, Vector3 impulse)
```

Applies a directional impulse without affecting rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

This is equivalent to using `BodyApplyImpulse()` at the body's center of mass.

### BodyApplyForce

```csharp
public void BodyApplyForce(Rid body, Vector3 force, Vector3? position = null)
```

Applies a positioned force to the body. A force is time dependent and meant to be applied every physics update.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### BodyApplyImpulse

```csharp
public void BodyApplyImpulse(Rid body, Vector3 impulse, Vector3? position = null)
```

Applies a positioned impulse to the body.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### BodyApplyTorque

```csharp
public void BodyApplyTorque(Rid body, Vector3 torque)
```

Applies a rotational force without affecting position. A force is time dependent and meant to be applied every physics update.

### BodyApplyTorqueImpulse

```csharp
public void BodyApplyTorqueImpulse(Rid body, Vector3 impulse)
```

Applies a rotational impulse to the body without affecting the position.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### BodyAttachObjectInstanceId

```csharp
public void BodyAttachObjectInstanceId(Rid body, ulong id)
```

Assigns the area to a descendant of [`GodotObject`](GodotObject.md), so it can exist in the node tree.

### BodyClearShapes

```csharp
public void BodyClearShapes(Rid body)
```

Removes all shapes from a body.

### BodyCreate

```csharp
public Rid BodyCreate()
```

Creates a 3D body object in the physics server, and returns the [`Rid`](Rid.md) that identifies it. The default settings for the created area include a collision layer and mask set to `1`, and body mode set to [`PhysicsServer3D.BodyMode.Rigid`](PhysicsServer3D.md).

Use `BodyAddShape()` to add shapes to it, use `BodySetState()` to set its transform, and use `BodySetSpace()` to add the body to a space.

### BodyGetCollisionLayer

```csharp
public uint BodyGetCollisionLayer(Rid body)
```

Returns the physics layer or layers a body belongs to.

### BodyGetCollisionMask

```csharp
public uint BodyGetCollisionMask(Rid body)
```

Returns the physics layer or layers a body can collide with.

### BodyGetCollisionPriority

```csharp
public float BodyGetCollisionPriority(Rid body)
```

Returns the body's collision priority.

### BodyGetConstantForce

```csharp
public Vector3 BodyGetConstantForce(Rid body)
```

Returns the body's total constant positional forces applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodyGetConstantTorque

```csharp
public Vector3 BodyGetConstantTorque(Rid body)
```

Returns the body's total constant rotational forces applied during each physics update.

See `BodyAddConstantTorque()`.

### BodyGetDirectState

```csharp
public PhysicsDirectBodyState3D BodyGetDirectState(Rid body)
```

Returns the [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md) of the body. Returns `null` if the body is destroyed or removed from the physics space.

### BodyGetMaxContactsReported

```csharp
public int BodyGetMaxContactsReported(Rid body)
```

Returns the maximum contacts that can be reported. See `BodySetMaxContactsReported()`.

### BodyGetMode

```csharp
public PhysicsServer3D.BodyMode BodyGetMode(Rid body)
```

Returns the body mode.

### BodyGetObjectInstanceId

```csharp
public ulong BodyGetObjectInstanceId(Rid body)
```

Gets the instance ID of the object the area is assigned to.

### BodyGetParam

```csharp
public Variant BodyGetParam(Rid body, PhysicsServer3D.BodyParameter param)
```

Returns the value of a body parameter. A list of available parameters is on the [`PhysicsServer3D.BodyParameter`](PhysicsServer3D.md) constants.

### BodyGetShape

```csharp
public Rid BodyGetShape(Rid body, int shapeIdx)
```

Returns the [`Rid`](Rid.md) of the nth shape of a body.

### BodyGetShapeCount

```csharp
public int BodyGetShapeCount(Rid body)
```

Returns the number of shapes assigned to a body.

### BodyGetShapeTransform

```csharp
public Transform3D BodyGetShapeTransform(Rid body, int shapeIdx)
```

Returns the transform matrix of a body shape.

### BodyGetSpace

```csharp
public Rid BodyGetSpace(Rid body)
```

Returns the [`Rid`](Rid.md) of the space assigned to a body.

### BodyGetState

```csharp
public Variant BodyGetState(Rid body, PhysicsServer3D.BodyState state)
```

Returns a body state.

### BodyIsAxisLocked

```csharp
public bool BodyIsAxisLocked(Rid body, PhysicsServer3D.BodyAxis axis)
```

### BodyIsContinuousCollisionDetectionEnabled

```csharp
public bool BodyIsContinuousCollisionDetectionEnabled(Rid body)
```

If `true`, the continuous collision detection mode is enabled.

### BodyIsOmittingForceIntegration

```csharp
public bool BodyIsOmittingForceIntegration(Rid body)
```

Returns `true` if the body is omitting the standard force integration. See `BodySetOmitForceIntegration()`.

### BodyRemoveCollisionException

```csharp
public void BodyRemoveCollisionException(Rid body, Rid exceptedBody)
```

Removes a body from the list of bodies exempt from collisions.

Continuous collision detection tries to predict where a moving body will collide, instead of moving it and correcting its movement if it collided.

### BodyRemoveShape

```csharp
public void BodyRemoveShape(Rid body, int shapeIdx)
```

Removes a shape from a body. The shape is not deleted, so it can be reused afterwards.

### BodyResetMassProperties

```csharp
public void BodyResetMassProperties(Rid body)
```

Restores the default inertia and center of mass based on shapes to cancel any custom values previously set using `BodySetParam()`.

### BodySetAxisLock

```csharp
public void BodySetAxisLock(Rid body, PhysicsServer3D.BodyAxis axis, bool lock)
```

### BodySetAxisVelocity

```csharp
public void BodySetAxisVelocity(Rid body, Vector3 axisVelocity)
```

Sets an axis velocity. The velocity in the given vector axis will be set as the given vector length. This is useful for jumping behavior.

### BodySetCollisionLayer

```csharp
public void BodySetCollisionLayer(Rid body, uint layer)
```

Sets the physics layer or layers a body belongs to.

### BodySetCollisionMask

```csharp
public void BodySetCollisionMask(Rid body, uint mask)
```

Sets the physics layer or layers a body can collide with.

### BodySetCollisionPriority

```csharp
public void BodySetCollisionPriority(Rid body, float priority)
```

Sets the body's collision priority.

### BodySetConstantForce

```csharp
public void BodySetConstantForce(Rid body, Vector3 force)
```

Sets the body's total constant positional forces applied during each physics update.

See `BodyAddConstantForce()` and `BodyAddConstantCentralForce()`.

### BodySetConstantTorque

```csharp
public void BodySetConstantTorque(Rid body, Vector3 torque)
```

Sets the body's total constant rotational forces applied during each physics update.

See `BodyAddConstantTorque()`.

### BodySetEnableContinuousCollisionDetection

```csharp
public void BodySetEnableContinuousCollisionDetection(Rid body, bool enable)
```

If `true`, the continuous collision detection mode is enabled.

Continuous collision detection tries to predict where a moving body will collide, instead of moving it and correcting its movement if it collided.

### BodySetForceIntegrationCallback

```csharp
public void BodySetForceIntegrationCallback(Rid body, Callable callable, Variant userdata = default)
```

Sets the body's custom force integration callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the custom callback.

The function `callable` will be called every physics tick, before the standard force integration (see `BodySetOmitForceIntegration()`). It can be used for example to update the body's linear and angular velocity based on contact with other bodies.

If `userdata` is not `null`, the function `callable` must take the following two parameters:

1. `state`: a [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md), used to retrieve and modify the body's state,

2. `userdata`: a [`Variant`](Variant.md); its value will be the `userdata` passed into this method.

If `userdata` is `null`, then `callable` must take only the `state` parameter.

### BodySetMaxContactsReported

```csharp
public void BodySetMaxContactsReported(Rid body, int amount)
```

Sets the maximum contacts to report. Bodies can keep a log of the contacts with other bodies. This is enabled by setting the maximum number of contacts reported to a number greater than 0.

### BodySetMode

```csharp
public void BodySetMode(Rid body, PhysicsServer3D.BodyMode mode)
```

Sets the body mode.

### BodySetOmitForceIntegration

```csharp
public void BodySetOmitForceIntegration(Rid body, bool enable)
```

Sets whether the body omits the standard force integration. If `enable` is `true`, the body will not automatically use applied forces, torques, and damping to update the body's linear and angular velocity. In this case, `BodySetForceIntegrationCallback()` can be used to manually update the linear and angular velocity instead.

This method is called when the property [`RigidBody3D.CustomIntegrator`](RigidBody3D.md) is set.

### BodySetParam

```csharp
public void BodySetParam(Rid body, PhysicsServer3D.BodyParameter param, Variant value)
```

Sets a body parameter. A list of available parameters is on the [`PhysicsServer3D.BodyParameter`](PhysicsServer3D.md) constants.

### BodySetRayPickable

```csharp
public void BodySetRayPickable(Rid body, bool enable)
```

Sets the body pickable with rays if `enable` is set.

### BodySetShape

```csharp
public void BodySetShape(Rid body, int shapeIdx, Rid shape)
```

Substitutes a given body shape by another. The old shape is selected by its index, the new one by its [`Rid`](Rid.md).

### BodySetShapeDisabled

```csharp
public void BodySetShapeDisabled(Rid body, int shapeIdx, bool disabled)
```

### BodySetShapeTransform

```csharp
public void BodySetShapeTransform(Rid body, int shapeIdx, Transform3D transform)
```

Sets the transform matrix for a body shape.

### BodySetSpace

```csharp
public void BodySetSpace(Rid body, Rid space)
```

Assigns a space to the body (see `SpaceCreate()`).

### BodySetState

```csharp
public void BodySetState(Rid body, PhysicsServer3D.BodyState state, Variant value)
```

Sets a body state.

### BodySetStateSyncCallback

```csharp
public void BodySetStateSyncCallback(Rid body, Callable callable)
```

Sets the body's state synchronization callback function to `callable`. Use an empty [`Callable`](Callable.md) (`Callable()`) to clear the callback.

The function `callable` will be called every physics frame, assuming that the body was active during the previous physics tick, and can be used to fetch the latest state from the physics server.

The function `callable` must take the following parameters:

1. `state`: a [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md), used to retrieve the body's state.

### BodyTestMotion

```csharp
public bool BodyTestMotion(Rid body, PhysicsTestMotionParameters3D parameters, PhysicsTestMotionResult3D result = null)
```

Returns `true` if a collision would result from moving along a motion vector from a given point in space. [`PhysicsTestMotionParameters3D`](PhysicsTestMotionParameters3D.md) is passed to set motion parameters. [`PhysicsTestMotionResult3D`](PhysicsTestMotionResult3D.md) can be passed to return additional information.

### BoxShapeCreate

```csharp
public Rid BoxShapeCreate()
```

Creates a 3D box shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the box's half-extents.

### CapsuleShapeCreate

```csharp
public Rid CapsuleShapeCreate()
```

Creates a 3D capsule shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the capsule's height and radius.

### ConcavePolygonShapeCreate

```csharp
public Rid ConcavePolygonShapeCreate()
```

Creates a 3D concave polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the concave polygon's triangles.

### ConeTwistJointGetParam

```csharp
public float ConeTwistJointGetParam(Rid joint, PhysicsServer3D.ConeTwistJointParam param)
```

Gets a cone twist joint parameter.

### ConeTwistJointSetParam

```csharp
public void ConeTwistJointSetParam(Rid joint, PhysicsServer3D.ConeTwistJointParam param, float value)
```

Sets a cone twist joint parameter.

### ConvexPolygonShapeCreate

```csharp
public Rid ConvexPolygonShapeCreate()
```

Creates a 3D convex polygon shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the convex polygon's points.

### CustomShapeCreate

```csharp
public Rid CustomShapeCreate()
```

Creates a custom shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's data.

**Note:** Custom shapes are not supported by the built-in physics servers, so calling this method always produces an error when using Godot Physics or Jolt Physics. Custom physics servers implemented as GDExtensions may support a custom shape.

### CylinderShapeCreate

```csharp
public Rid CylinderShapeCreate()
```

Creates a 3D cylinder shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the cylinder's height and radius.

### FreeRid

```csharp
public void FreeRid(Rid rid)
```

Destroys any of the objects created by PhysicsServer3D. If the [`Rid`](Rid.md) passed is not one of the objects that can be created by PhysicsServer3D, an error will be sent to the console.

### Generic6DofJointGetFlag

```csharp
public bool Generic6DofJointGetFlag(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisFlag flag)
```

Returns the value of a generic 6DOF joint flag.

### Generic6DofJointGetParam

```csharp
public float Generic6DofJointGetParam(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisParam param)
```

Returns the value of a generic 6DOF joint parameter.

### Generic6DofJointSetFlag

```csharp
public void Generic6DofJointSetFlag(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisFlag flag, bool enable)
```

Sets the value of a given generic 6DOF joint flag.

### Generic6DofJointSetParam

```csharp
public void Generic6DofJointSetParam(Rid joint, Vector3.Axis axis, PhysicsServer3D.G6DofJointAxisParam param, float value)
```

Sets the value of a given generic 6DOF joint parameter.

### GetProcessInfo

```csharp
public int GetProcessInfo(PhysicsServer3D.ProcessInfo processInfo)
```

Returns the value of a physics engine state specified by `processInfo`.

### HeightmapShapeCreate

```csharp
public Rid HeightmapShapeCreate()
```

Creates a 3D heightmap shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the heightmap's data.

### HingeJointGetFlag

```csharp
public bool HingeJointGetFlag(Rid joint, PhysicsServer3D.HingeJointFlag flag)
```

Gets a hinge joint flag.

### HingeJointGetParam

```csharp
public float HingeJointGetParam(Rid joint, PhysicsServer3D.HingeJointParam param)
```

Gets a hinge joint parameter.

### HingeJointSetFlag

```csharp
public void HingeJointSetFlag(Rid joint, PhysicsServer3D.HingeJointFlag flag, bool enabled)
```

Sets a hinge joint flag.

### HingeJointSetParam

```csharp
public void HingeJointSetParam(Rid joint, PhysicsServer3D.HingeJointParam param, float value)
```

Sets a hinge joint parameter.

### JointClear

```csharp
public void JointClear(Rid joint)
```

### JointCreate

```csharp
public Rid JointCreate()
```

### JointDisableCollisionsBetweenBodies

```csharp
public void JointDisableCollisionsBetweenBodies(Rid joint, bool disable)
```

Sets whether the bodies attached to the [`Joint3D`](Joint3D.md) will collide with each other.

### JointGetSolverPriority

```csharp
public int JointGetSolverPriority(Rid joint)
```

Gets the priority value of the Joint3D.

**Note:** Only supported when using GodotPhysics3D. This method always returns `1` when using Jolt Physics, as it does not support joint solver priority.

### JointGetType

```csharp
public PhysicsServer3D.JointType JointGetType(Rid joint)
```

Returns the type of the Joint3D.

### JointIsDisabledCollisionsBetweenBodies

```csharp
public bool JointIsDisabledCollisionsBetweenBodies(Rid joint)
```

Returns whether the bodies attached to the [`Joint3D`](Joint3D.md) will collide with each other.

### JointMakeConeTwist

```csharp
public void JointMakeConeTwist(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

### JointMakeGeneric6Dof

```csharp
public void JointMakeGeneric6Dof(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

Make the joint a generic six degrees of freedom (6DOF) joint. Use `Generic6DofJointSetFlag()` and `Generic6DofJointSetParam()` to set the joint's flags and parameters respectively.

### JointMakeHinge

```csharp
public void JointMakeHinge(Rid joint, Rid bodyA, Transform3D hingeA, Rid bodyB, Transform3D hingeB)
```

### JointMakePin

```csharp
public void JointMakePin(Rid joint, Rid bodyA, Vector3 localA, Rid bodyB, Vector3 localB)
```

### JointMakeSlider

```csharp
public void JointMakeSlider(Rid joint, Rid bodyA, Transform3D localRefA, Rid bodyB, Transform3D localRefB)
```

### JointSetSolverPriority

```csharp
public void JointSetSolverPriority(Rid joint, int priority)
```

Sets the priority value of the Joint3D.

**Note:** Only supported when using GodotPhysics3D. This method has no effect when using Jolt Physics, as it does not support joint solver priority.

### PinJointGetLocalA

```csharp
public Vector3 PinJointGetLocalA(Rid joint)
```

Returns position of the joint in the local space of body a of the joint.

### PinJointGetLocalB

```csharp
public Vector3 PinJointGetLocalB(Rid joint)
```

Returns position of the joint in the local space of body b of the joint.

### PinJointGetParam

```csharp
public float PinJointGetParam(Rid joint, PhysicsServer3D.PinJointParam param)
```

Gets a pin joint parameter.

### PinJointSetLocalA

```csharp
public void PinJointSetLocalA(Rid joint, Vector3 localA)
```

Sets position of the joint in the local space of body a of the joint.

### PinJointSetLocalB

```csharp
public void PinJointSetLocalB(Rid joint, Vector3 localB)
```

Sets position of the joint in the local space of body b of the joint.

### PinJointSetParam

```csharp
public void PinJointSetParam(Rid joint, PhysicsServer3D.PinJointParam param, float value)
```

Sets a pin joint parameter.

### SeparationRayShapeCreate

```csharp
public Rid SeparationRayShapeCreate()
```

Creates a 3D separation ray shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's `length` and `slide_on_slope` properties.

### SetActive

```csharp
public void SetActive(bool active)
```

Activates or deactivates the 3D physics engine.

### ShapeGetData

```csharp
public Variant ShapeGetData(Rid shape)
```

Returns the shape data that configures the shape, such as the half-extents of a box or the triangles of a concave (trimesh) shape. See `ShapeSetData()` for the precise format of this data in each case.

### ShapeGetMargin

```csharp
public float ShapeGetMargin(Rid shape)
```

Returns the collision margin for the shape.

**Note:** This is not used in Godot Physics, so will always return `0`.

### ShapeGetType

```csharp
public PhysicsServer3D.ShapeType ShapeGetType(Rid shape)
```

Returns the shape's type.

### ShapeSetData

```csharp
public void ShapeSetData(Rid shape, Variant data)
```

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
public void ShapeSetMargin(Rid shape, float margin)
```

Sets the collision margin for the shape.

**Note:** This is not used in Godot Physics.

### SliderJointGetParam

```csharp
public float SliderJointGetParam(Rid joint, PhysicsServer3D.SliderJointParam param)
```

Gets a slider joint parameter.

### SliderJointSetParam

```csharp
public void SliderJointSetParam(Rid joint, PhysicsServer3D.SliderJointParam param, float value)
```

Gets a slider joint parameter.

### SoftBodyAddCollisionException

```csharp
public void SoftBodyAddCollisionException(Rid body, Rid bodyB)
```

Adds the given body to the list of bodies exempt from collisions.

### SoftBodyApplyCentralForce

```csharp
public void SoftBodyApplyCentralForce(Rid body, Vector3 force)
```

Distributes and applies a force to all points. A force is time dependent and meant to be applied every physics update.

### SoftBodyApplyCentralImpulse

```csharp
public void SoftBodyApplyCentralImpulse(Rid body, Vector3 impulse)
```

Distributes and applies an impulse to all points.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### SoftBodyApplyPointForce

```csharp
public void SoftBodyApplyPointForce(Rid body, int pointIndex, Vector3 force)
```

Applies a force to a point. A force is time dependent and meant to be applied every physics update.

### SoftBodyApplyPointImpulse

```csharp
public void SoftBodyApplyPointImpulse(Rid body, int pointIndex, Vector3 impulse)
```

Applies an impulse to a point.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

### SoftBodyCreate

```csharp
public Rid SoftBodyCreate()
```

Creates a new soft body and returns its internal [`Rid`](Rid.md).

### SoftBodyGetBounds

```csharp
public Aabb SoftBodyGetBounds(Rid body)
```

Returns the bounds of the given soft body in global coordinates.

### SoftBodyGetCollisionLayer

```csharp
public uint SoftBodyGetCollisionLayer(Rid body)
```

Returns the physics layer or layers that the given soft body belongs to.

### SoftBodyGetCollisionMask

```csharp
public uint SoftBodyGetCollisionMask(Rid body)
```

Returns the physics layer or layers that the given soft body can collide with.

### SoftBodyGetDampingCoefficient

```csharp
public float SoftBodyGetDampingCoefficient(Rid body)
```

Returns the damping coefficient of the given soft body.

### SoftBodyGetDragCoefficient

```csharp
public float SoftBodyGetDragCoefficient(Rid body)
```

Returns the drag coefficient of the given soft body.

### SoftBodyGetLinearStiffness

```csharp
public float SoftBodyGetLinearStiffness(Rid body)
```

Returns the linear stiffness of the given soft body.

### SoftBodyGetPointGlobalPosition

```csharp
public Vector3 SoftBodyGetPointGlobalPosition(Rid body, int pointIndex)
```

Returns the current position of the given soft body point in global coordinates.

### SoftBodyGetPressureCoefficient

```csharp
public float SoftBodyGetPressureCoefficient(Rid body)
```

Returns the pressure coefficient of the given soft body.

### SoftBodyGetShrinkingFactor

```csharp
public float SoftBodyGetShrinkingFactor(Rid body)
```

Returns the shrinking factor of the given soft body.

### SoftBodyGetSimulationPrecision

```csharp
public int SoftBodyGetSimulationPrecision(Rid body)
```

Returns the simulation precision of the given soft body.

### SoftBodyGetSpace

```csharp
public Rid SoftBodyGetSpace(Rid body)
```

Returns the [`Rid`](Rid.md) of the space assigned to the given soft body.

### SoftBodyGetState

```csharp
public Variant SoftBodyGetState(Rid body, PhysicsServer3D.BodyState state)
```

Returns the given soft body state.

**Note:** Godot's default physics implementation does not support [`PhysicsServer3D.BodyState.LinearVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.AngularVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.Sleeping`](PhysicsServer3D.md), or [`PhysicsServer3D.BodyState.CanSleep`](PhysicsServer3D.md).

### SoftBodyGetTotalMass

```csharp
public float SoftBodyGetTotalMass(Rid body)
```

Returns the total mass assigned to the given soft body.

### SoftBodyIsPointPinned

```csharp
public bool SoftBodyIsPointPinned(Rid body, int pointIndex)
```

Returns whether the given soft body point is pinned.

### SoftBodyMovePoint

```csharp
public void SoftBodyMovePoint(Rid body, int pointIndex, Vector3 globalPosition)
```

Moves the given soft body point to a position in global coordinates.

### SoftBodyPinPoint

```csharp
public void SoftBodyPinPoint(Rid body, int pointIndex, bool pin)
```

Pins or unpins the given soft body point based on the value of `pin`.

**Note:** Pinning a point effectively makes it kinematic, preventing it from being affected by forces, but you can still move it using `SoftBodyMovePoint()`.

### SoftBodyRemoveAllPinnedPoints

```csharp
public void SoftBodyRemoveAllPinnedPoints(Rid body)
```

Unpins all points of the given soft body.

### SoftBodyRemoveCollisionException

```csharp
public void SoftBodyRemoveCollisionException(Rid body, Rid bodyB)
```

Removes the given body from the list of bodies exempt from collisions.

### SoftBodySetCollisionLayer

```csharp
public void SoftBodySetCollisionLayer(Rid body, uint layer)
```

Sets the physics layer or layers the given soft body belongs to.

### SoftBodySetCollisionMask

```csharp
public void SoftBodySetCollisionMask(Rid body, uint mask)
```

Sets the physics layer or layers the given soft body can collide with.

### SoftBodySetDampingCoefficient

```csharp
public void SoftBodySetDampingCoefficient(Rid body, float dampingCoefficient)
```

Sets the damping coefficient of the given soft body. Higher values will slow down the body more noticeably when forces are applied.

### SoftBodySetDragCoefficient

```csharp
public void SoftBodySetDragCoefficient(Rid body, float dragCoefficient)
```

Sets the drag coefficient of the given soft body. Higher values increase this body's air resistance.

**Note:** This value is currently unused by Godot's default physics implementation.

### SoftBodySetLinearStiffness

```csharp
public void SoftBodySetLinearStiffness(Rid body, float stiffness)
```

Sets the linear stiffness of the given soft body. Higher values will result in a stiffer body, while lower values will increase the body's ability to bend. The value can be between `0.0` and `1.0` (inclusive).

### SoftBodySetMesh

```csharp
public void SoftBodySetMesh(Rid body, Rid mesh)
```

Sets the mesh of the given soft body.

### SoftBodySetPressureCoefficient

```csharp
public void SoftBodySetPressureCoefficient(Rid body, float pressureCoefficient)
```

Sets the pressure coefficient of the given soft body. Simulates pressure build-up from inside this body. Higher values increase the strength of this effect.

### SoftBodySetRayPickable

```csharp
public void SoftBodySetRayPickable(Rid body, bool enable)
```

Sets whether the given soft body will be pickable when using object picking.

### SoftBodySetShrinkingFactor

```csharp
public void SoftBodySetShrinkingFactor(Rid body, float shrinkingFactor)
```

Sets the shrinking factor of the given soft body.

### SoftBodySetSimulationPrecision

```csharp
public void SoftBodySetSimulationPrecision(Rid body, int simulationPrecision)
```

Sets the simulation precision of the given soft body. Increasing this value will improve the resulting simulation, but can affect performance. Use with care.

### SoftBodySetSpace

```csharp
public void SoftBodySetSpace(Rid body, Rid space)
```

Assigns a space to the given soft body (see `SpaceCreate()`).

### SoftBodySetState

```csharp
public void SoftBodySetState(Rid body, PhysicsServer3D.BodyState state, Variant variant)
```

Sets the given body state for the given body.

**Note:** Godot's default physics implementation does not support [`PhysicsServer3D.BodyState.LinearVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.AngularVelocity`](PhysicsServer3D.md), [`PhysicsServer3D.BodyState.Sleeping`](PhysicsServer3D.md), or [`PhysicsServer3D.BodyState.CanSleep`](PhysicsServer3D.md).

### SoftBodySetTotalMass

```csharp
public void SoftBodySetTotalMass(Rid body, float totalMass)
```

Sets the total mass for the given soft body.

### SoftBodySetTransform

```csharp
public void SoftBodySetTransform(Rid body, Transform3D transform)
```

Sets the global transform of the given soft body.

### SoftBodyUpdateRenderingServer

```csharp
public void SoftBodyUpdateRenderingServer(Rid body, PhysicsServer3DRenderingServerHandler renderingServerHandler)
```

Requests that the physics server updates the rendering server with the latest positions of the given soft body's points through the `renderingServerHandler` interface.

### SpaceCreate

```csharp
public Rid SpaceCreate()
```

Creates a space. A space is a collection of parameters for the physics engine that can be assigned to an area or a body. It can be assigned to an area with `AreaSetSpace()`, or to a body with `BodySetSpace()`.

### SpaceGetDirectState

```csharp
public PhysicsDirectSpaceState3D SpaceGetDirectState(Rid space)
```

Returns the state of a space, a [`PhysicsDirectSpaceState3D`](PhysicsDirectSpaceState3D.md). This object can be used to make collision/intersection queries.

### SpaceGetParam

```csharp
public float SpaceGetParam(Rid space, PhysicsServer3D.SpaceParameter param)
```

Returns the value of a space parameter.

### SpaceIsActive

```csharp
public bool SpaceIsActive(Rid space)
```

Returns whether the space is active.

### SpaceSetActive

```csharp
public void SpaceSetActive(Rid space, bool active)
```

Marks a space as active. It will not have an effect, unless it is assigned to an area or body.

### SpaceSetParam

```csharp
public void SpaceSetParam(Rid space, PhysicsServer3D.SpaceParameter param, float value)
```

Sets the value for a space parameter. A list of available parameters is on the [`PhysicsServer3D.SpaceParameter`](PhysicsServer3D.md) constants.

### SphereShapeCreate

```csharp
public Rid SphereShapeCreate()
```

Creates a 3D sphere shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the sphere's radius.

### WorldBoundaryShapeCreate

```csharp
public Rid WorldBoundaryShapeCreate()
```

Creates a 3D world boundary shape in the physics server, and returns the [`Rid`](Rid.md) that identifies it. Use `ShapeSetData()` to set the shape's normal direction and distance properties.
