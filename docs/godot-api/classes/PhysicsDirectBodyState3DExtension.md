<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicsDirectBodyState3DExtension

命名空间 `Godot`；class；继承 [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md) → [`GodotObject`](GodotObject.md)

This class extends [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md) by providing additional virtual methods that can be overridden. When these methods are overridden, they will be called instead of the internal methods of the physics server.

Intended for use with GDExtension to create custom implementations of [`PhysicsDirectBodyState3D`](PhysicsDirectBodyState3D.md).

## 构造函数

### PhysicsDirectBodyState3DExtension

```csharp
public PhysicsDirectBodyState3DExtension()
```

## 方法

### _AddConstantCentralForce

```csharp
public virtual void _AddConstantCentralForce(Vector3 force)
```

引擎名：`_add_constant_central_force`

### _AddConstantForce

```csharp
public virtual void _AddConstantForce(Vector3 force, Vector3 position)
```

引擎名：`_add_constant_force`

### _AddConstantTorque

```csharp
public virtual void _AddConstantTorque(Vector3 torque)
```

引擎名：`_add_constant_torque`

### _ApplyCentralForce

```csharp
public virtual void _ApplyCentralForce(Vector3 force)
```

引擎名：`_apply_central_force`

### _ApplyCentralImpulse

```csharp
public virtual void _ApplyCentralImpulse(Vector3 impulse)
```

引擎名：`_apply_central_impulse`

### _ApplyForce

```csharp
public virtual void _ApplyForce(Vector3 force, Vector3 position)
```

引擎名：`_apply_force`

### _ApplyImpulse

```csharp
public virtual void _ApplyImpulse(Vector3 impulse, Vector3 position)
```

引擎名：`_apply_impulse`

### _ApplyTorque

```csharp
public virtual void _ApplyTorque(Vector3 torque)
```

引擎名：`_apply_torque`

### _ApplyTorqueImpulse

```csharp
public virtual void _ApplyTorqueImpulse(Vector3 impulse)
```

引擎名：`_apply_torque_impulse`

### _GetAngularVelocity

```csharp
public virtual Vector3 _GetAngularVelocity()
```

引擎名：`_get_angular_velocity`

### _GetCenterOfMass

```csharp
public virtual Vector3 _GetCenterOfMass()
```

引擎名：`_get_center_of_mass`

### _GetCenterOfMassLocal

```csharp
public virtual Vector3 _GetCenterOfMassLocal()
```

引擎名：`_get_center_of_mass_local`

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
public virtual Vector3 _GetConstantForce()
```

引擎名：`_get_constant_force`

### _GetConstantTorque

```csharp
public virtual Vector3 _GetConstantTorque()
```

引擎名：`_get_constant_torque`

### _GetContactCollider

```csharp
public virtual Rid _GetContactCollider(int contactIdx)
```

引擎名：`_get_contact_collider`

### _GetContactColliderId

```csharp
public virtual ulong _GetContactColliderId(int contactIdx)
```

引擎名：`_get_contact_collider_id`

### _GetContactColliderObject

```csharp
public virtual GodotObject _GetContactColliderObject(int contactIdx)
```

引擎名：`_get_contact_collider_object`

### _GetContactColliderPosition

```csharp
public virtual Vector3 _GetContactColliderPosition(int contactIdx)
```

引擎名：`_get_contact_collider_position`

### _GetContactColliderShape

```csharp
public virtual int _GetContactColliderShape(int contactIdx)
```

引擎名：`_get_contact_collider_shape`

### _GetContactColliderVelocityAtPosition

```csharp
public virtual Vector3 _GetContactColliderVelocityAtPosition(int contactIdx)
```

引擎名：`_get_contact_collider_velocity_at_position`

### _GetContactCount

```csharp
public virtual int _GetContactCount()
```

引擎名：`_get_contact_count`

### _GetContactImpulse

```csharp
public virtual Vector3 _GetContactImpulse(int contactIdx)
```

引擎名：`_get_contact_impulse`

### _GetContactLocalNormal

```csharp
public virtual Vector3 _GetContactLocalNormal(int contactIdx)
```

引擎名：`_get_contact_local_normal`

### _GetContactLocalPosition

```csharp
public virtual Vector3 _GetContactLocalPosition(int contactIdx)
```

引擎名：`_get_contact_local_position`

### _GetContactLocalShape

```csharp
public virtual int _GetContactLocalShape(int contactIdx)
```

引擎名：`_get_contact_local_shape`

### _GetContactLocalVelocityAtPosition

```csharp
public virtual Vector3 _GetContactLocalVelocityAtPosition(int contactIdx)
```

引擎名：`_get_contact_local_velocity_at_position`

### _GetInverseInertia

```csharp
public virtual Vector3 _GetInverseInertia()
```

引擎名：`_get_inverse_inertia`

### _GetInverseInertiaTensor

```csharp
public virtual Basis _GetInverseInertiaTensor()
```

引擎名：`_get_inverse_inertia_tensor`

### _GetInverseMass

```csharp
public virtual float _GetInverseMass()
```

引擎名：`_get_inverse_mass`

### _GetLinearVelocity

```csharp
public virtual Vector3 _GetLinearVelocity()
```

引擎名：`_get_linear_velocity`

### _GetPrincipalInertiaAxes

```csharp
public virtual Basis _GetPrincipalInertiaAxes()
```

引擎名：`_get_principal_inertia_axes`

### _GetSpaceState

```csharp
public virtual PhysicsDirectSpaceState3D _GetSpaceState()
```

引擎名：`_get_space_state`

### _GetStep

```csharp
public virtual float _GetStep()
```

引擎名：`_get_step`

### _GetTotalAngularDamp

```csharp
public virtual float _GetTotalAngularDamp()
```

引擎名：`_get_total_angular_damp`

### _GetTotalGravity

```csharp
public virtual Vector3 _GetTotalGravity()
```

引擎名：`_get_total_gravity`

### _GetTotalLinearDamp

```csharp
public virtual float _GetTotalLinearDamp()
```

引擎名：`_get_total_linear_damp`

### _GetTransform

```csharp
public virtual Transform3D _GetTransform()
```

引擎名：`_get_transform`

### _GetVelocityAtLocalPosition

```csharp
public virtual Vector3 _GetVelocityAtLocalPosition(Vector3 localPosition)
```

引擎名：`_get_velocity_at_local_position`

### _IntegrateForces

```csharp
public virtual void _IntegrateForces()
```

引擎名：`_integrate_forces`

### _IsSleeping

```csharp
public virtual bool _IsSleeping()
```

引擎名：`_is_sleeping`

### _SetAngularVelocity

```csharp
public virtual void _SetAngularVelocity(Vector3 velocity)
```

引擎名：`_set_angular_velocity`

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
public virtual void _SetConstantForce(Vector3 force)
```

引擎名：`_set_constant_force`

### _SetConstantTorque

```csharp
public virtual void _SetConstantTorque(Vector3 torque)
```

引擎名：`_set_constant_torque`

### _SetLinearVelocity

```csharp
public virtual void _SetLinearVelocity(Vector3 velocity)
```

引擎名：`_set_linear_velocity`

### _SetSleepState

```csharp
public virtual void _SetSleepState(bool enabled)
```

引擎名：`_set_sleep_state`

### _SetTransform

```csharp
public virtual void _SetTransform(Transform3D transform)
```

引擎名：`_set_transform`
