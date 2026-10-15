<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsDirectBodyState3D

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

Provides direct access to a physics body in the [`PhysicsServer3D`](PhysicsServer3D.md), allowing safe changes to physics properties. This object is passed via the direct state callback of [`RigidBody3D`](RigidBody3D.md), and is intended for changing the direct state of that body. See [`RigidBody3D._IntegrateForces()`](RigidBody3D.md).

## 属性

### AngularVelocity

```csharp
public Vector3 AngularVelocity { get; set; }
```

引擎名：`angular_velocity`

The body's rotational velocity in *radians* per second.

### CenterOfMass

```csharp
public Vector3 CenterOfMass { get; }
```

引擎名：`center_of_mass`

The body's center of mass position relative to the body's center in the global coordinate system.

### CenterOfMassLocal

```csharp
public Vector3 CenterOfMassLocal { get; }
```

引擎名：`center_of_mass_local`

The body's center of mass position in the body's local coordinate system.

### CollisionLayer

```csharp
public uint CollisionLayer { get; set; }
```

引擎名：`collision_layer`

The body's collision layer.

### CollisionMask

```csharp
public uint CollisionMask { get; set; }
```

引擎名：`collision_mask`

The body's collision mask.

### InverseInertia

```csharp
public Vector3 InverseInertia { get; }
```

引擎名：`inverse_inertia`

The inverse of the inertia of the body.

### InverseInertiaTensor

```csharp
public Basis InverseInertiaTensor { get; }
```

引擎名：`inverse_inertia_tensor`

The inverse of the inertia tensor of the body.

### InverseMass

```csharp
public float InverseMass { get; }
```

引擎名：`inverse_mass`

The inverse of the mass of the body.

### LinearVelocity

```csharp
public Vector3 LinearVelocity { get; set; }
```

引擎名：`linear_velocity`

The body's linear velocity in units per second.

### PrincipalInertiaAxes

```csharp
public Basis PrincipalInertiaAxes { get; }
```

引擎名：`principal_inertia_axes`

### Sleeping

```csharp
public bool Sleeping { get; set; }
```

引擎名：`sleeping`

If `true`, this body is currently sleeping (not active).

### Step

```csharp
public float Step { get; }
```

引擎名：`step`

The timestep (delta) used for the simulation.

### TotalAngularDamp

```csharp
public float TotalAngularDamp { get; }
```

引擎名：`total_angular_damp`

The rate at which the body stops rotating, if there are not any other forces moving it.

### TotalGravity

```csharp
public Vector3 TotalGravity { get; }
```

引擎名：`total_gravity`

The total gravity vector being currently applied to this body.

### TotalLinearDamp

```csharp
public float TotalLinearDamp { get; }
```

引擎名：`total_linear_damp`

The rate at which the body stops moving, if there are not any other forces moving it.

### Transform

```csharp
public Transform3D Transform { get; set; }
```

引擎名：`transform`

The body's transformation matrix.

## 方法

### AddConstantCentralForce

```csharp
public void AddConstantCentralForce(Vector3? force = null)
```

引擎名：`add_constant_central_force`

Adds a constant directional force without affecting rotation that keeps being applied over time until cleared with `constant_force = Vector3(0, 0, 0)`.

This is equivalent to using `AddConstantForce()` at the body's center of mass.

参数：
- `force`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### AddConstantForce

```csharp
public void AddConstantForce(Vector3 force, Vector3? position = null)
```

引擎名：`add_constant_force`

Adds a constant positioned force to the body that keeps being applied over time until cleared with `constant_force = Vector3(0, 0, 0)`.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### AddConstantTorque

```csharp
public void AddConstantTorque(Vector3 torque)
```

引擎名：`add_constant_torque`

Adds a constant rotational force without affecting position that keeps being applied over time until cleared with `constant_torque = Vector3(0, 0, 0)`.

### ApplyCentralForce

```csharp
public void ApplyCentralForce(Vector3? force = null)
```

引擎名：`apply_central_force`

Applies a directional force without affecting rotation. A force is time dependent and meant to be applied every physics update.

This is equivalent to using `ApplyForce()` at the body's center of mass.

参数：
- `force`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### ApplyCentralImpulse

```csharp
public void ApplyCentralImpulse(Vector3? impulse = null)
```

引擎名：`apply_central_impulse`

Applies a directional impulse without affecting rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

This is equivalent to using `ApplyImpulse()` at the body's center of mass.

参数：
- `impulse`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### ApplyForce

```csharp
public void ApplyForce(Vector3 force, Vector3? position = null)
```

引擎名：`apply_force`

Applies a positioned force to the body. A force is time dependent and meant to be applied every physics update.

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### ApplyImpulse

```csharp
public void ApplyImpulse(Vector3 impulse, Vector3? position = null)
```

引擎名：`apply_impulse`

Applies a positioned impulse to the body.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

`position` is the offset from the body origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### ApplyTorque

```csharp
public void ApplyTorque(Vector3 torque)
```

引擎名：`apply_torque`

Applies a rotational force without affecting position. A force is time dependent and meant to be applied every physics update.

**Note:** `InverseInertia` is required for this to work. To have `InverseInertia`, an active [`CollisionShape3D`](CollisionShape3D.md) must be a child of the node, or you can manually set `InverseInertia`.

### ApplyTorqueImpulse

```csharp
public void ApplyTorqueImpulse(Vector3 impulse)
```

引擎名：`apply_torque_impulse`

Applies a rotational impulse to the body without affecting the position.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_force" functions otherwise).

**Note:** `InverseInertia` is required for this to work. To have `InverseInertia`, an active [`CollisionShape3D`](CollisionShape3D.md) must be a child of the node, or you can manually set `InverseInertia`.

### GetAngularVelocity

```csharp
public Vector3 GetAngularVelocity()
```

引擎名：`get_angular_velocity`

### GetCenterOfMass

```csharp
public Vector3 GetCenterOfMass()
```

引擎名：`get_center_of_mass`

### GetCenterOfMassLocal

```csharp
public Vector3 GetCenterOfMassLocal()
```

引擎名：`get_center_of_mass_local`

### GetCollisionLayer

```csharp
public uint GetCollisionLayer()
```

引擎名：`get_collision_layer`

### GetCollisionMask

```csharp
public uint GetCollisionMask()
```

引擎名：`get_collision_mask`

### GetConstantForce

```csharp
public Vector3 GetConstantForce()
```

引擎名：`get_constant_force`

Returns the body's total constant positional forces applied during each physics update.

See `AddConstantForce()` and `AddConstantCentralForce()`.

### GetConstantTorque

```csharp
public Vector3 GetConstantTorque()
```

引擎名：`get_constant_torque`

Returns the body's total constant rotational forces applied during each physics update.

See `AddConstantTorque()`.

### GetContactCollider

```csharp
public Rid GetContactCollider(int contactIdx)
```

引擎名：`get_contact_collider`

Returns the collider's [`Rid`](Rid.md).

### GetContactColliderId

```csharp
public ulong GetContactColliderId(int contactIdx)
```

引擎名：`get_contact_collider_id`

Returns the collider's object id.

### GetContactColliderObject

```csharp
public GodotObject GetContactColliderObject(int contactIdx)
```

引擎名：`get_contact_collider_object`

Returns the collider object.

### GetContactColliderPosition

```csharp
public Vector3 GetContactColliderPosition(int contactIdx)
```

引擎名：`get_contact_collider_position`

Returns the position of the contact point on the collider in the global coordinate system.

### GetContactColliderShape

```csharp
public int GetContactColliderShape(int contactIdx)
```

引擎名：`get_contact_collider_shape`

Returns the collider's shape index.

### GetContactColliderVelocityAtPosition

```csharp
public Vector3 GetContactColliderVelocityAtPosition(int contactIdx)
```

引擎名：`get_contact_collider_velocity_at_position`

Returns the linear velocity vector at the collider's contact point.

### GetContactCount

```csharp
public int GetContactCount()
```

引擎名：`get_contact_count`

Returns the number of contacts this body has with other bodies.

**Note:** By default, this returns 0 unless bodies are configured to monitor contacts. See [`RigidBody3D.ContactMonitor`](RigidBody3D.md).

### GetContactImpulse

```csharp
public Vector3 GetContactImpulse(int contactIdx)
```

引擎名：`get_contact_impulse`

Impulse created by the contact.

### GetContactLocalNormal

```csharp
public Vector3 GetContactLocalNormal(int contactIdx)
```

引擎名：`get_contact_local_normal`

Returns the local normal at the contact point.

### GetContactLocalPosition

```csharp
public Vector3 GetContactLocalPosition(int contactIdx)
```

引擎名：`get_contact_local_position`

Returns the position of the contact point on the body in the global coordinate system.

### GetContactLocalShape

```csharp
public int GetContactLocalShape(int contactIdx)
```

引擎名：`get_contact_local_shape`

Returns the local shape index of the collision.

### GetContactLocalVelocityAtPosition

```csharp
public Vector3 GetContactLocalVelocityAtPosition(int contactIdx)
```

引擎名：`get_contact_local_velocity_at_position`

Returns the linear velocity vector at the body's contact point.

### GetInverseInertia

```csharp
public Vector3 GetInverseInertia()
```

引擎名：`get_inverse_inertia`

### GetInverseInertiaTensor

```csharp
public Basis GetInverseInertiaTensor()
```

引擎名：`get_inverse_inertia_tensor`

### GetInverseMass

```csharp
public float GetInverseMass()
```

引擎名：`get_inverse_mass`

### GetLinearVelocity

```csharp
public Vector3 GetLinearVelocity()
```

引擎名：`get_linear_velocity`

### GetPrincipalInertiaAxes

```csharp
public Basis GetPrincipalInertiaAxes()
```

引擎名：`get_principal_inertia_axes`

### GetSpaceState

```csharp
public PhysicsDirectSpaceState3D GetSpaceState()
```

引擎名：`get_space_state`

Returns the current state of the space, useful for queries.

### GetStep

```csharp
public float GetStep()
```

引擎名：`get_step`

### GetTotalAngularDamp

```csharp
public float GetTotalAngularDamp()
```

引擎名：`get_total_angular_damp`

### GetTotalGravity

```csharp
public Vector3 GetTotalGravity()
```

引擎名：`get_total_gravity`

### GetTotalLinearDamp

```csharp
public float GetTotalLinearDamp()
```

引擎名：`get_total_linear_damp`

### GetTransform

```csharp
public Transform3D GetTransform()
```

引擎名：`get_transform`

### GetVelocityAtLocalPosition

```csharp
public Vector3 GetVelocityAtLocalPosition(Vector3 localPosition)
```

引擎名：`get_velocity_at_local_position`

Returns the body's velocity at the given relative position.

`localPosition` is the offset from the body origin in global coordinates.

### IntegrateForces

```csharp
public void IntegrateForces()
```

引擎名：`integrate_forces`

Updates the body's linear and angular velocity by applying gravity and damping for the equivalent of one physics tick.

### IsSleeping

```csharp
public bool IsSleeping()
```

引擎名：`is_sleeping`

### SetAngularVelocity

```csharp
public void SetAngularVelocity(Vector3 velocity)
```

引擎名：`set_angular_velocity`

### SetCollisionLayer

```csharp
public void SetCollisionLayer(uint layer)
```

引擎名：`set_collision_layer`

### SetCollisionMask

```csharp
public void SetCollisionMask(uint mask)
```

引擎名：`set_collision_mask`

### SetConstantForce

```csharp
public void SetConstantForce(Vector3 force)
```

引擎名：`set_constant_force`

Sets the body's total constant positional forces applied during each physics update.

See `AddConstantForce()` and `AddConstantCentralForce()`.

### SetConstantTorque

```csharp
public void SetConstantTorque(Vector3 torque)
```

引擎名：`set_constant_torque`

Sets the body's total constant rotational forces applied during each physics update.

See `AddConstantTorque()`.

### SetLinearVelocity

```csharp
public void SetLinearVelocity(Vector3 velocity)
```

引擎名：`set_linear_velocity`

### SetSleepState

```csharp
public void SetSleepState(bool enabled)
```

引擎名：`set_sleep_state`

### SetTransform

```csharp
public void SetTransform(Transform3D transform)
```

引擎名：`set_transform`
