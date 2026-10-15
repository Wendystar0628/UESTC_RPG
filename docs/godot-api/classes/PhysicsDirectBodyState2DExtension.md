<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsDirectBodyState2DExtension

命名空间 `Godot`；class；继承 [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md) → [`GodotObject`](GodotObject.md)

This class extends [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md) by providing additional virtual methods that can be overridden. When these methods are overridden, they will be called instead of the internal methods of the physics server.

Intended for use with GDExtension to create custom implementations of [`PhysicsDirectBodyState2D`](PhysicsDirectBodyState2D.md).

## 构造函数

### PhysicsDirectBodyState2DExtension

```csharp
public PhysicsDirectBodyState2DExtension()
```

## 方法

### _AddConstantCentralForce

```csharp
public virtual void _AddConstantCentralForce(Vector2 force)
```

引擎名：`_add_constant_central_force`

Overridable version of [`PhysicsDirectBodyState2D.AddConstantCentralForce`](PhysicsDirectBodyState2D.md).

### _AddConstantForce

```csharp
public virtual void _AddConstantForce(Vector2 force, Vector2 position)
```

引擎名：`_add_constant_force`

Overridable version of [`PhysicsDirectBodyState2D.AddConstantForce`](PhysicsDirectBodyState2D.md).

### _AddConstantTorque

```csharp
public virtual void _AddConstantTorque(float torque)
```

引擎名：`_add_constant_torque`

Overridable version of [`PhysicsDirectBodyState2D.AddConstantTorque`](PhysicsDirectBodyState2D.md).

### _ApplyCentralForce

```csharp
public virtual void _ApplyCentralForce(Vector2 force)
```

引擎名：`_apply_central_force`

Overridable version of [`PhysicsDirectBodyState2D.ApplyCentralForce`](PhysicsDirectBodyState2D.md).

### _ApplyCentralImpulse

```csharp
public virtual void _ApplyCentralImpulse(Vector2 impulse)
```

引擎名：`_apply_central_impulse`

Overridable version of [`PhysicsDirectBodyState2D.ApplyCentralImpulse`](PhysicsDirectBodyState2D.md).

### _ApplyForce

```csharp
public virtual void _ApplyForce(Vector2 force, Vector2 position)
```

引擎名：`_apply_force`

Overridable version of [`PhysicsDirectBodyState2D.ApplyForce`](PhysicsDirectBodyState2D.md).

### _ApplyImpulse

```csharp
public virtual void _ApplyImpulse(Vector2 impulse, Vector2 position)
```

引擎名：`_apply_impulse`

Overridable version of [`PhysicsDirectBodyState2D.ApplyImpulse`](PhysicsDirectBodyState2D.md).

### _ApplyTorque

```csharp
public virtual void _ApplyTorque(float torque)
```

引擎名：`_apply_torque`

Overridable version of [`PhysicsDirectBodyState2D.ApplyTorque`](PhysicsDirectBodyState2D.md).

### _ApplyTorqueImpulse

```csharp
public virtual void _ApplyTorqueImpulse(float impulse)
```

引擎名：`_apply_torque_impulse`

Overridable version of [`PhysicsDirectBodyState2D.ApplyTorqueImpulse`](PhysicsDirectBodyState2D.md).

### _GetAngularVelocity

```csharp
public virtual float _GetAngularVelocity()
```

引擎名：`_get_angular_velocity`

Implement to override the behavior of [`PhysicsDirectBodyState2D.AngularVelocity`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetCenterOfMass

```csharp
public virtual Vector2 _GetCenterOfMass()
```

引擎名：`_get_center_of_mass`

Implement to override the behavior of [`PhysicsDirectBodyState2D.CenterOfMass`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetCenterOfMassLocal

```csharp
public virtual Vector2 _GetCenterOfMassLocal()
```

引擎名：`_get_center_of_mass_local`

Implement to override the behavior of [`PhysicsDirectBodyState2D.CenterOfMassLocal`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetCollisionLayer

```csharp
public virtual uint _GetCollisionLayer()
```

引擎名：`_get_collision_layer`

### _GetCollisionMask

```csharp
public virtual uint _GetCollisionMask()
```

引擎名：`_get_collision_mask`

### _GetConstantForce

```csharp
public virtual Vector2 _GetConstantForce()
```

引擎名：`_get_constant_force`

Overridable version of [`PhysicsDirectBodyState2D.GetConstantForce`](PhysicsDirectBodyState2D.md).

### _GetConstantTorque

```csharp
public virtual float _GetConstantTorque()
```

引擎名：`_get_constant_torque`

Overridable version of [`PhysicsDirectBodyState2D.GetConstantTorque`](PhysicsDirectBodyState2D.md).

### _GetContactCollider

```csharp
public virtual Rid _GetContactCollider(int contactIdx)
```

引擎名：`_get_contact_collider`

Overridable version of [`PhysicsDirectBodyState2D.GetContactCollider`](PhysicsDirectBodyState2D.md).

### _GetContactColliderId

```csharp
public virtual ulong _GetContactColliderId(int contactIdx)
```

引擎名：`_get_contact_collider_id`

Overridable version of [`PhysicsDirectBodyState2D.GetContactColliderId`](PhysicsDirectBodyState2D.md).

### _GetContactColliderObject

```csharp
public virtual GodotObject _GetContactColliderObject(int contactIdx)
```

引擎名：`_get_contact_collider_object`

Overridable version of [`PhysicsDirectBodyState2D.GetContactColliderObject`](PhysicsDirectBodyState2D.md).

### _GetContactColliderPosition

```csharp
public virtual Vector2 _GetContactColliderPosition(int contactIdx)
```

引擎名：`_get_contact_collider_position`

Overridable version of [`PhysicsDirectBodyState2D.GetContactColliderPosition`](PhysicsDirectBodyState2D.md).

### _GetContactColliderShape

```csharp
public virtual int _GetContactColliderShape(int contactIdx)
```

引擎名：`_get_contact_collider_shape`

Overridable version of [`PhysicsDirectBodyState2D.GetContactColliderShape`](PhysicsDirectBodyState2D.md).

### _GetContactColliderVelocityAtPosition

```csharp
public virtual Vector2 _GetContactColliderVelocityAtPosition(int contactIdx)
```

引擎名：`_get_contact_collider_velocity_at_position`

Overridable version of [`PhysicsDirectBodyState2D.GetContactColliderVelocityAtPosition`](PhysicsDirectBodyState2D.md).

### _GetContactCount

```csharp
public virtual int _GetContactCount()
```

引擎名：`_get_contact_count`

Overridable version of [`PhysicsDirectBodyState2D.GetContactCount`](PhysicsDirectBodyState2D.md).

### _GetContactImpulse

```csharp
public virtual Vector2 _GetContactImpulse(int contactIdx)
```

引擎名：`_get_contact_impulse`

Overridable version of [`PhysicsDirectBodyState2D.GetContactImpulse`](PhysicsDirectBodyState2D.md).

### _GetContactLocalNormal

```csharp
public virtual Vector2 _GetContactLocalNormal(int contactIdx)
```

引擎名：`_get_contact_local_normal`

Overridable version of [`PhysicsDirectBodyState2D.GetContactLocalNormal`](PhysicsDirectBodyState2D.md).

### _GetContactLocalPosition

```csharp
public virtual Vector2 _GetContactLocalPosition(int contactIdx)
```

引擎名：`_get_contact_local_position`

Overridable version of [`PhysicsDirectBodyState2D.GetContactLocalPosition`](PhysicsDirectBodyState2D.md).

### _GetContactLocalShape

```csharp
public virtual int _GetContactLocalShape(int contactIdx)
```

引擎名：`_get_contact_local_shape`

Overridable version of [`PhysicsDirectBodyState2D.GetContactLocalShape`](PhysicsDirectBodyState2D.md).

### _GetContactLocalVelocityAtPosition

```csharp
public virtual Vector2 _GetContactLocalVelocityAtPosition(int contactIdx)
```

引擎名：`_get_contact_local_velocity_at_position`

Overridable version of [`PhysicsDirectBodyState2D.GetContactLocalVelocityAtPosition`](PhysicsDirectBodyState2D.md).

### _GetInverseInertia

```csharp
public virtual float _GetInverseInertia()
```

引擎名：`_get_inverse_inertia`

Implement to override the behavior of [`PhysicsDirectBodyState2D.InverseInertia`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetInverseMass

```csharp
public virtual float _GetInverseMass()
```

引擎名：`_get_inverse_mass`

Implement to override the behavior of [`PhysicsDirectBodyState2D.InverseMass`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetLinearVelocity

```csharp
public virtual Vector2 _GetLinearVelocity()
```

引擎名：`_get_linear_velocity`

Implement to override the behavior of [`PhysicsDirectBodyState2D.LinearVelocity`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetSpaceState

```csharp
public virtual PhysicsDirectSpaceState2D _GetSpaceState()
```

引擎名：`_get_space_state`

Overridable version of [`PhysicsDirectBodyState2D.GetSpaceState`](PhysicsDirectBodyState2D.md).

### _GetStep

```csharp
public virtual float _GetStep()
```

引擎名：`_get_step`

Implement to override the behavior of [`PhysicsDirectBodyState2D.Step`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetTotalAngularDamp

```csharp
public virtual float _GetTotalAngularDamp()
```

引擎名：`_get_total_angular_damp`

Implement to override the behavior of [`PhysicsDirectBodyState2D.TotalAngularDamp`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetTotalGravity

```csharp
public virtual Vector2 _GetTotalGravity()
```

引擎名：`_get_total_gravity`

Implement to override the behavior of [`PhysicsDirectBodyState2D.TotalGravity`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetTotalLinearDamp

```csharp
public virtual float _GetTotalLinearDamp()
```

引擎名：`_get_total_linear_damp`

Implement to override the behavior of [`PhysicsDirectBodyState2D.TotalLinearDamp`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetTransform

```csharp
public virtual Transform2D _GetTransform()
```

引擎名：`_get_transform`

Implement to override the behavior of [`PhysicsDirectBodyState2D.Transform`](PhysicsDirectBodyState2D.md) and its respective getter.

### _GetVelocityAtLocalPosition

```csharp
public virtual Vector2 _GetVelocityAtLocalPosition(Vector2 localPosition)
```

引擎名：`_get_velocity_at_local_position`

Overridable version of [`PhysicsDirectBodyState2D.GetVelocityAtLocalPosition`](PhysicsDirectBodyState2D.md).

### _IntegrateForces

```csharp
public virtual void _IntegrateForces()
```

引擎名：`_integrate_forces`

Overridable version of [`PhysicsDirectBodyState2D.IntegrateForces`](PhysicsDirectBodyState2D.md).

### _IsSleeping

```csharp
public virtual bool _IsSleeping()
```

引擎名：`_is_sleeping`

Implement to override the behavior of [`PhysicsDirectBodyState2D.Sleeping`](PhysicsDirectBodyState2D.md) and its respective getter.

### _SetAngularVelocity

```csharp
public virtual void _SetAngularVelocity(float velocity)
```

引擎名：`_set_angular_velocity`

Implement to override the behavior of [`PhysicsDirectBodyState2D.AngularVelocity`](PhysicsDirectBodyState2D.md) and its respective setter.

### _SetCollisionLayer

```csharp
public virtual void _SetCollisionLayer(uint layer)
```

引擎名：`_set_collision_layer`

### _SetCollisionMask

```csharp
public virtual void _SetCollisionMask(uint mask)
```

引擎名：`_set_collision_mask`

### _SetConstantForce

```csharp
public virtual void _SetConstantForce(Vector2 force)
```

引擎名：`_set_constant_force`

Overridable version of [`PhysicsDirectBodyState2D.SetConstantForce`](PhysicsDirectBodyState2D.md).

### _SetConstantTorque

```csharp
public virtual void _SetConstantTorque(float torque)
```

引擎名：`_set_constant_torque`

Overridable version of [`PhysicsDirectBodyState2D.SetConstantTorque`](PhysicsDirectBodyState2D.md).

### _SetLinearVelocity

```csharp
public virtual void _SetLinearVelocity(Vector2 velocity)
```

引擎名：`_set_linear_velocity`

Implement to override the behavior of [`PhysicsDirectBodyState2D.LinearVelocity`](PhysicsDirectBodyState2D.md) and its respective setter.

### _SetSleepState

```csharp
public virtual void _SetSleepState(bool enabled)
```

引擎名：`_set_sleep_state`

Implement to override the behavior of [`PhysicsDirectBodyState2D.Sleeping`](PhysicsDirectBodyState2D.md) and its respective setter.

### _SetTransform

```csharp
public virtual void _SetTransform(Transform2D transform)
```

引擎名：`_set_transform`

Implement to override the behavior of [`PhysicsDirectBodyState2D.Transform`](PhysicsDirectBodyState2D.md) and its respective setter.
