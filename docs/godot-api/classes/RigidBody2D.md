<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# RigidBody2D

命名空间 `Godot`；class；继承 [`PhysicsBody2D`](PhysicsBody2D.md) → [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`RigidBody2D` implements full 2D physics. It cannot be controlled directly, instead, you must apply forces to it (gravity, impulses, etc.), and the physics simulation will calculate the resulting movement, rotation, react to collisions, and affect other physics bodies in its path.

The body's behavior can be adjusted via `LockRotation`, `Freeze`, and `FreezeMode`. By changing various properties of the object, such as `Mass`, you can control how the physics simulation acts on it.

A rigid body will always maintain its shape and size, even when forces are applied to it. It is useful for objects that can be interacted with in an environment, such as a tree that can be knocked over or a stack of crates that can be pushed around.

If you need to directly affect the body, prefer `_IntegrateForces()` as it allows you to directly access the physics state.

If you need to override the default physics behavior, you can write a custom force integration function. See `CustomIntegrator`.

**Note:** Changing the 2D transform or `LinearVelocity` of a `RigidBody2D` very often may lead to some unpredictable behaviors. This also happens when a `RigidBody2D` is the descendant of a constantly moving node, like another `RigidBody2D`, as that will cause its global transform to be set whenever its ancestor moves.

## 构造函数

### RigidBody2D

```csharp
public RigidBody2D()
```

## 属性

### AngularDamp

```csharp
public float AngularDamp { get; set; }
```

引擎名：`angular_damp`

Damps the body's rotation. By default, the body will use the `ProjectSettings.physics/2d/default_angular_damp` setting or any value override set by an [`Area2D`](Area2D.md) the body is in. Depending on `AngularDampMode`, you can set `AngularDamp` to be added to or to replace the body's damping value.

See `ProjectSettings.physics/2d/default_angular_damp` for more details about damping.

### AngularDampMode

```csharp
public RigidBody2D.DampMode AngularDampMode { get; set; }
```

引擎名：`angular_damp_mode`

Defines how `AngularDamp` is applied.

### AngularVelocity

```csharp
public float AngularVelocity { get; set; }
```

引擎名：`angular_velocity`

The body's rotational velocity in *radians* per second.

### CanSleep

```csharp
public bool CanSleep { get; set; }
```

引擎名：`can_sleep`

If `true`, the body can enter sleep mode when there is no movement. See `Sleeping`.

### CenterOfMass

```csharp
public Vector2 CenterOfMass { get; set; }
```

引擎名：`center_of_mass`

The body's custom center of mass, relative to the body's origin position, when `CenterOfMassMode` is set to [`RigidBody2D.CenterOfMassModeEnum.Custom`](RigidBody2D.md). This is the balanced point of the body, where applied forces only cause linear acceleration. Applying forces outside of the center of mass causes angular acceleration.

When `CenterOfMassMode` is set to [`RigidBody2D.CenterOfMassModeEnum.Auto`](RigidBody2D.md) (default value), the center of mass is automatically determined, but this does not update the value of `CenterOfMass`.

### CenterOfMassMode

```csharp
public RigidBody2D.CenterOfMassModeEnum CenterOfMassMode { get; set; }
```

引擎名：`center_of_mass_mode`

Defines the way the body's center of mass is set.

### ConstantForce

```csharp
public Vector2 ConstantForce { get; set; }
```

引擎名：`constant_force`

The body's total constant positional forces applied during each physics update.

See `AddConstantForce()` and `AddConstantCentralForce()`.

### ConstantTorque

```csharp
public float ConstantTorque { get; set; }
```

引擎名：`constant_torque`

The body's total constant rotational forces applied during each physics update.

See `AddConstantTorque()`.

### ContactMonitor

```csharp
public bool ContactMonitor { get; set; }
```

引擎名：`contact_monitor`

If `true`, the RigidBody2D will emit signals when it collides with another body.

**Note:** By default the maximum contacts reported is set to 0, meaning nothing will be recorded, see `MaxContactsReported`.

### ContinuousCd

```csharp
public RigidBody2D.CcdMode ContinuousCd { get; set; }
```

引擎名：`continuous_cd`

Continuous collision detection mode.

Continuous collision detection tries to predict where a moving body will collide instead of moving it and correcting its movement after collision. Continuous collision detection is slower, but more precise and misses fewer collisions with small, fast-moving objects. Raycasting and shapecasting methods are available.

### CustomIntegrator

```csharp
public bool CustomIntegrator { get; set; }
```

引擎名：`custom_integrator`

If `true`, the standard force integration (like gravity or damping) will be disabled for this body. Other than collision response, the body will only move as determined by the `_IntegrateForces()` method, if that virtual method is overridden.

Setting this property will call the method [`PhysicsServer2D.BodySetOmitForceIntegration`](PhysicsServer2D.md) internally.

### Freeze

```csharp
public bool Freeze { get; set; }
```

引擎名：`freeze`

If `true`, the body is frozen. Gravity and forces are not applied anymore.

See `FreezeMode` to set the body's behavior when frozen.

**Note:** For a body that is always frozen, use [`StaticBody2D`](StaticBody2D.md) or [`AnimatableBody2D`](AnimatableBody2D.md) instead.

### FreezeMode

```csharp
public RigidBody2D.FreezeModeEnum FreezeMode { get; set; }
```

引擎名：`freeze_mode`

The body's freeze mode. Determines the body's behavior when `Freeze` is `true`.

**Note:** For a body that is always frozen, use [`StaticBody2D`](StaticBody2D.md) or [`AnimatableBody2D`](AnimatableBody2D.md) instead.

### GravityScale

```csharp
public float GravityScale { get; set; }
```

引擎名：`gravity_scale`

Multiplies the gravity applied to the body. The body's gravity is calculated from the `ProjectSettings.physics/2d/default_gravity` project setting and/or any additional gravity vector applied by [`Area2D`](Area2D.md)s.

### Inertia

```csharp
public float Inertia { get; set; }
```

引擎名：`inertia`

The body's moment of inertia. This is like mass, but for rotation: it determines how much torque it takes to rotate the body. The moment of inertia is usually computed automatically from the mass and the shapes, but this property allows you to set a custom value.

If set to `0`, inertia is automatically computed (default value).

**Note:** This value does not change when inertia is automatically computed. Use [`PhysicsServer2D`](PhysicsServer2D.md) to get the computed inertia.

```csharp
private RigidBody2D _ball;

public override void _Ready()
{
_ball = GetNode<RigidBody2D>("Ball");
}

private float GetBallInertia()
{
return 1.0f / PhysicsServer2D.BodyGetDirectState(_ball.GetRid()).InverseInertia;
}

```

### LinearDamp

```csharp
public float LinearDamp { get; set; }
```

引擎名：`linear_damp`

Damps the body's movement. By default, the body will use the `ProjectSettings.physics/2d/default_linear_damp` setting or any value override set by an [`Area2D`](Area2D.md) the body is in. Depending on `LinearDampMode`, you can set `LinearDamp` to be added to or to replace the body's damping value.

See `ProjectSettings.physics/2d/default_linear_damp` for more details about damping.

### LinearDampMode

```csharp
public RigidBody2D.DampMode LinearDampMode { get; set; }
```

引擎名：`linear_damp_mode`

Defines how `LinearDamp` is applied.

### LinearVelocity

```csharp
public Vector2 LinearVelocity { get; set; }
```

引擎名：`linear_velocity`

The body's linear velocity in pixels per second. Can be used sporadically, but **don't set this every frame**, because physics may run in another thread and runs at a different granularity. Use `_IntegrateForces()` as your process loop for precise control of the body state.

### LockRotation

```csharp
public bool LockRotation { get; set; }
```

引擎名：`lock_rotation`

If `true`, the body cannot rotate. Gravity and forces only apply linear movement.

### Mass

```csharp
public float Mass { get; set; }
```

引擎名：`mass`

The body's mass.

### MaxContactsReported

```csharp
public int MaxContactsReported { get; set; }
```

引擎名：`max_contacts_reported`

The maximum number of contacts that will be recorded. Requires a value greater than 0 and `ContactMonitor` to be set to `true` to start to register contacts. Use `GetContactCount()` to retrieve the count or `GetCollidingBodies()` to retrieve bodies that have been collided with.

**Note:** The number of contacts is different from the number of collisions. Collisions between parallel edges will result in two contacts (one at each end), and collisions between parallel faces will result in four contacts (one at each corner).

### PhysicsMaterialOverride

```csharp
public PhysicsMaterial PhysicsMaterialOverride { get; set; }
```

引擎名：`physics_material_override`

The physics material override for the body.

If a material is assigned to this property, it will be used instead of any other physics material, such as an inherited one.

### Sleeping

```csharp
public bool Sleeping { get; set; }
```

引擎名：`sleeping`

If `true`, the body will not move and will not calculate forces until woken up by another body through, for example, a collision, or by using the `ApplyImpulse()` or `ApplyForce()` methods.

## 方法

### AddConstantCentralForce

```csharp
public void AddConstantCentralForce(Vector2 force)
```

引擎名：`add_constant_central_force`

Adds a constant directional force without affecting rotation that keeps being applied over time until cleared with `constant_force = Vector2(0, 0)`.

This is equivalent to using `AddConstantForce()` at the body's center of mass.

### AddConstantForce

```csharp
public void AddConstantForce(Vector2 force, Vector2? position = null)
```

引擎名：`add_constant_force`

Adds a constant positioned force to the body that keeps being applied over time until cleared with `constant_force = Vector2(0, 0)`.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### AddConstantTorque

```csharp
public void AddConstantTorque(float torque)
```

引擎名：`add_constant_torque`

Adds a constant rotational force without affecting position that keeps being applied over time until cleared with `constant_torque = 0`.

### ApplyCentralForce

```csharp
public void ApplyCentralForce(Vector2 force)
```

引擎名：`apply_central_force`

Applies a directional force without affecting rotation. A force is time dependent and meant to be applied every physics update.

This is equivalent to using `ApplyForce()` at the body's center of mass.

### ApplyCentralImpulse

```csharp
public void ApplyCentralImpulse(Vector2? impulse = null)
```

引擎名：`apply_central_impulse`

Applies a directional impulse without affecting rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

This is equivalent to using `ApplyImpulse()` at the body's center of mass.

参数：
- `impulse`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### ApplyForce

```csharp
public void ApplyForce(Vector2 force, Vector2? position = null)
```

引擎名：`apply_force`

Applies a positioned force to the body. A force is time dependent and meant to be applied every physics update.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### ApplyImpulse

```csharp
public void ApplyImpulse(Vector2 impulse, Vector2? position = null)
```

引擎名：`apply_impulse`

Applies a positioned impulse to the body.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### ApplyTorque

```csharp
public void ApplyTorque(float torque)
```

引擎名：`apply_torque`

Applies a rotational force without affecting position. A force is time dependent and meant to be applied every physics update.

**Note:** `Inertia` is required for this to work. To have `Inertia`, an active [`CollisionShape2D`](CollisionShape2D.md) must be a child of the node, or you can manually set `Inertia`.

### ApplyTorqueImpulse

```csharp
public void ApplyTorqueImpulse(float torque)
```

引擎名：`apply_torque_impulse`

Applies a rotational impulse to the body without affecting the position.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

**Note:** `Inertia` is required for this to work. To have `Inertia`, an active [`CollisionShape2D`](CollisionShape2D.md) must be a child of the node, or you can manually set `Inertia`.

### GetAngularDamp

```csharp
public float GetAngularDamp()
```

引擎名：`get_angular_damp`

### GetAngularDampMode

```csharp
public RigidBody2D.DampMode GetAngularDampMode()
```

引擎名：`get_angular_damp_mode`

### GetAngularVelocity

```csharp
public float GetAngularVelocity()
```

引擎名：`get_angular_velocity`

### GetCenterOfMass

```csharp
public Vector2 GetCenterOfMass()
```

引擎名：`get_center_of_mass`

### GetCenterOfMassMode

```csharp
public RigidBody2D.CenterOfMassModeEnum GetCenterOfMassMode()
```

引擎名：`get_center_of_mass_mode`

### GetCollidingBodies

```csharp
public Array<Node2D> GetCollidingBodies()
```

引擎名：`get_colliding_bodies`

Returns a list of the bodies colliding with this one. Requires `ContactMonitor` to be set to `true` and `MaxContactsReported` to be set high enough to detect all the collisions.

**Note:** The result of this test is not immediate after moving objects. For performance, list of collisions is updated once per frame and before the physics step. Consider using signals instead.

### GetConstantForce

```csharp
public Vector2 GetConstantForce()
```

引擎名：`get_constant_force`

### GetConstantTorque

```csharp
public float GetConstantTorque()
```

引擎名：`get_constant_torque`

### GetContactCount

```csharp
public int GetContactCount()
```

引擎名：`get_contact_count`

Returns the number of contacts this body has with other bodies. By default, this returns 0 unless bodies are configured to monitor contacts (see `ContactMonitor`).

**Note:** To retrieve the colliding bodies, use `GetCollidingBodies()`.

### GetContinuousCollisionDetectionMode

```csharp
public RigidBody2D.CcdMode GetContinuousCollisionDetectionMode()
```

引擎名：`get_continuous_collision_detection_mode`

### GetFreezeMode

```csharp
public RigidBody2D.FreezeModeEnum GetFreezeMode()
```

引擎名：`get_freeze_mode`

### GetGravityScale

```csharp
public float GetGravityScale()
```

引擎名：`get_gravity_scale`

### GetInertia

```csharp
public float GetInertia()
```

引擎名：`get_inertia`

### GetLinearDamp

```csharp
public float GetLinearDamp()
```

引擎名：`get_linear_damp`

### GetLinearDampMode

```csharp
public RigidBody2D.DampMode GetLinearDampMode()
```

引擎名：`get_linear_damp_mode`

### GetLinearVelocity

```csharp
public Vector2 GetLinearVelocity()
```

引擎名：`get_linear_velocity`

### GetMass

```csharp
public float GetMass()
```

引擎名：`get_mass`

### GetMaxContactsReported

```csharp
public int GetMaxContactsReported()
```

引擎名：`get_max_contacts_reported`

### GetPhysicsMaterialOverride

```csharp
public PhysicsMaterial GetPhysicsMaterialOverride()
```

引擎名：`get_physics_material_override`

### IsAbleToSleep

```csharp
public bool IsAbleToSleep()
```

引擎名：`is_able_to_sleep`

### IsContactMonitorEnabled

```csharp
public bool IsContactMonitorEnabled()
```

引擎名：`is_contact_monitor_enabled`

### IsFreezeEnabled

```csharp
public bool IsFreezeEnabled()
```

引擎名：`is_freeze_enabled`

### IsLockRotationEnabled

```csharp
public bool IsLockRotationEnabled()
```

引擎名：`is_lock_rotation_enabled`

### IsSleeping

```csharp
public bool IsSleeping()
```

引擎名：`is_sleeping`

### IsUsingCustomIntegrator

```csharp
public bool IsUsingCustomIntegrator()
```

引擎名：`is_using_custom_integrator`

### SetAngularDamp

```csharp
public void SetAngularDamp(float angularDamp)
```

引擎名：`set_angular_damp`

### SetAngularDampMode

```csharp
public void SetAngularDampMode(RigidBody2D.DampMode angularDampMode)
```

引擎名：`set_angular_damp_mode`

### SetAngularVelocity

```csharp
public void SetAngularVelocity(float angularVelocity)
```

引擎名：`set_angular_velocity`

### SetAxisVelocity

```csharp
public void SetAxisVelocity(Vector2 axisVelocity)
```

引擎名：`set_axis_velocity`

Sets the body's velocity on the given axis. The velocity in the given vector axis will be set as the given vector length. This is useful for jumping behavior.

### SetCanSleep

```csharp
public void SetCanSleep(bool ableToSleep)
```

引擎名：`set_can_sleep`

### SetCenterOfMass

```csharp
public void SetCenterOfMass(Vector2 centerOfMass)
```

引擎名：`set_center_of_mass`

### SetCenterOfMassMode

```csharp
public void SetCenterOfMassMode(RigidBody2D.CenterOfMassModeEnum mode)
```

引擎名：`set_center_of_mass_mode`

### SetConstantForce

```csharp
public void SetConstantForce(Vector2 force)
```

引擎名：`set_constant_force`

### SetConstantTorque

```csharp
public void SetConstantTorque(float torque)
```

引擎名：`set_constant_torque`

### SetContactMonitor

```csharp
public void SetContactMonitor(bool enabled)
```

引擎名：`set_contact_monitor`

### SetContinuousCollisionDetectionMode

```csharp
public void SetContinuousCollisionDetectionMode(RigidBody2D.CcdMode mode)
```

引擎名：`set_continuous_collision_detection_mode`

### SetFreezeEnabled

```csharp
public void SetFreezeEnabled(bool freezeMode)
```

引擎名：`set_freeze_enabled`

### SetFreezeMode

```csharp
public void SetFreezeMode(RigidBody2D.FreezeModeEnum freezeMode)
```

引擎名：`set_freeze_mode`

### SetGravityScale

```csharp
public void SetGravityScale(float gravityScale)
```

引擎名：`set_gravity_scale`

### SetInertia

```csharp
public void SetInertia(float inertia)
```

引擎名：`set_inertia`

### SetLinearDamp

```csharp
public void SetLinearDamp(float linearDamp)
```

引擎名：`set_linear_damp`

### SetLinearDampMode

```csharp
public void SetLinearDampMode(RigidBody2D.DampMode linearDampMode)
```

引擎名：`set_linear_damp_mode`

### SetLinearVelocity

```csharp
public void SetLinearVelocity(Vector2 linearVelocity)
```

引擎名：`set_linear_velocity`

### SetLockRotationEnabled

```csharp
public void SetLockRotationEnabled(bool lockRotation)
```

引擎名：`set_lock_rotation_enabled`

### SetMass

```csharp
public void SetMass(float mass)
```

引擎名：`set_mass`

### SetMaxContactsReported

```csharp
public void SetMaxContactsReported(int amount)
```

引擎名：`set_max_contacts_reported`

### SetPhysicsMaterialOverride

```csharp
public void SetPhysicsMaterialOverride(PhysicsMaterial physicsMaterialOverride)
```

引擎名：`set_physics_material_override`

### SetSleeping

```csharp
public void SetSleeping(bool sleeping)
```

引擎名：`set_sleeping`

### SetUseCustomIntegrator

```csharp
public void SetUseCustomIntegrator(bool enable)
```

引擎名：`set_use_custom_integrator`

### _IntegrateForces

```csharp
public virtual void _IntegrateForces(PhysicsDirectBodyState2D state)
```

引擎名：`_integrate_forces`

Called during physics processing, allowing you to read and safely modify the simulation state for the object. By default, it is called before the standard force integration, but the `CustomIntegrator` property allows you to disable the standard force integration and do fully custom force integration for a body.

## 事件（信号）

### BodyEntered

```csharp
public event RigidBody2D.BodyEnteredEventHandler BodyEntered
```

引擎名：`body_entered`

Emitted when a collision with another [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md) occurs. Requires `ContactMonitor` to be set to `true` and `MaxContactsReported` to be set high enough to detect all the collisions. [`TileMap`](TileMap.md)s are detected if the [`TileSet`](TileSet.md) has Collision [`Shape2D`](Shape2D.md)s.

`body` the [`Node`](Node.md), if it exists in the tree, of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md).

### BodyExited

```csharp
public event RigidBody2D.BodyExitedEventHandler BodyExited
```

引擎名：`body_exited`

Emitted when the collision with another [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md) ends. Requires `ContactMonitor` to be set to `true` and `MaxContactsReported` to be set high enough to detect all the collisions. [`TileMap`](TileMap.md)s are detected if the [`TileSet`](TileSet.md) has Collision [`Shape2D`](Shape2D.md)s.

`body` the [`Node`](Node.md), if it exists in the tree, of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md).

### BodyShapeEntered

```csharp
public event RigidBody2D.BodyShapeEnteredEventHandler BodyShapeEntered
```

引擎名：`body_shape_entered`

Emitted when one of this RigidBody2D's [`Shape2D`](Shape2D.md)s collides with another [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md)'s [`Shape2D`](Shape2D.md)s. Requires `ContactMonitor` to be set to `true` and `MaxContactsReported` to be set high enough to detect all the collisions. [`TileMap`](TileMap.md)s are detected if the [`TileSet`](TileSet.md) has Collision [`Shape2D`](Shape2D.md)s.

`bodyRid` the [`Rid`](Rid.md) of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileSet`](TileSet.md)'s [`CollisionObject2D`](CollisionObject2D.md) used by the [`PhysicsServer2D`](PhysicsServer2D.md).

`body` the [`Node`](Node.md), if it exists in the tree, of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md).

`bodyShapeIndex` the index of the [`Shape2D`](Shape2D.md) of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md) used by the [`PhysicsServer2D`](PhysicsServer2D.md). Get the [`CollisionShape2D`](CollisionShape2D.md) node with `body.shape_owner_get_owner(body.shape_find_owner(body_shape_index))`.

`localShapeIndex` the index of the [`Shape2D`](Shape2D.md) of this RigidBody2D used by the [`PhysicsServer2D`](PhysicsServer2D.md). Get the [`CollisionShape2D`](CollisionShape2D.md) node with `self.shape_owner_get_owner(self.shape_find_owner(local_shape_index))`.

### BodyShapeExited

```csharp
public event RigidBody2D.BodyShapeExitedEventHandler BodyShapeExited
```

引擎名：`body_shape_exited`

Emitted when the collision between one of this RigidBody2D's [`Shape2D`](Shape2D.md)s and another [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md)'s [`Shape2D`](Shape2D.md)s ends. Requires `ContactMonitor` to be set to `true` and `MaxContactsReported` to be set high enough to detect all the collisions. [`TileMap`](TileMap.md)s are detected if the [`TileSet`](TileSet.md) has Collision [`Shape2D`](Shape2D.md)s.

`bodyRid` the [`Rid`](Rid.md) of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileSet`](TileSet.md)'s [`CollisionObject2D`](CollisionObject2D.md) used by the [`PhysicsServer2D`](PhysicsServer2D.md).

`body` the [`Node`](Node.md), if it exists in the tree, of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md).

`bodyShapeIndex` the index of the [`Shape2D`](Shape2D.md) of the other [`PhysicsBody2D`](PhysicsBody2D.md) or [`TileMap`](TileMap.md) used by the [`PhysicsServer2D`](PhysicsServer2D.md). Get the [`CollisionShape2D`](CollisionShape2D.md) node with `body.shape_owner_get_owner(body.shape_find_owner(body_shape_index))`.

`localShapeIndex` the index of the [`Shape2D`](Shape2D.md) of this RigidBody2D used by the [`PhysicsServer2D`](PhysicsServer2D.md). Get the [`CollisionShape2D`](CollisionShape2D.md) node with `self.shape_owner_get_owner(self.shape_find_owner(local_shape_index))`.

### SleepingStateChanged

```csharp
public event Action SleepingStateChanged
```

引擎名：`sleeping_state_changed`

Emitted when the physics engine changes the body's sleeping state.

**Note:** Changing the value `Sleeping` will not trigger this signal. It is only emitted if the sleeping state is changed by the physics engine or `emit_signal("sleeping_state_changed")` is used.

## BodyEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RigidBody2D.BodyEntered`](RigidBody2D.md) event of a [`RigidBody2D`](RigidBody2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyEnteredEventHandler(Node body)
```

## BodyExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RigidBody2D.BodyExited`](RigidBody2D.md) event of a [`RigidBody2D`](RigidBody2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyExitedEventHandler(Node body)
```

## BodyShapeEnteredEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RigidBody2D.BodyShapeEntered`](RigidBody2D.md) event of a [`RigidBody2D`](RigidBody2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyShapeEnteredEventHandler(Rid bodyRid, Node body, long bodyShapeIndex, long localShapeIndex)
```

## BodyShapeExitedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`RigidBody2D.BodyShapeExited`](RigidBody2D.md) event of a [`RigidBody2D`](RigidBody2D.md) class.

### 签名

#### Invoke

```csharp
delegate void BodyShapeExitedEventHandler(Rid bodyRid, Node body, long bodyShapeIndex, long localShapeIndex)
```

## CcdMode

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Continuous collision detection disabled.
- `CastRay = 1`：Continuous collision detection enabled using raycasting.
- `CastShape = 2`：Continuous collision detection enabled using shapecasting.

## CenterOfMassModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Auto = 0`：In this mode, the body's center of mass is calculated automatically based on its shapes.
- `Custom = 1`：In this mode, the body's center of mass is set through [`RigidBody2D.CenterOfMass`](RigidBody2D.md).

## DampMode

命名空间 `Godot`；enum

### 枚举值

- `Combine = 0`：In this mode, the body's damping value is added to any value set in areas or the default value.
- `Replace = 1`：In this mode, the body's damping value replaces any value set in areas or the default value.

## FreezeModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Static = 0`：Static body freeze mode (default).
- `Kinematic = 1`：Kinematic body freeze mode.
