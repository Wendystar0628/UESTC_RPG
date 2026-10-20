<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicalBone3D

命名空间 `Godot`；class；继承 [`PhysicsBody3D`](PhysicsBody3D.md) → [`CollisionObject3D`](CollisionObject3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `PhysicalBone3D` node is a physics body that can be used to make bones in a [`Skeleton3D`](Skeleton3D.md) react to physics.

**Note:** In order to detect physical bones with raycasts, the [`SkeletonModifier3D.Active`](SkeletonModifier3D.md) property of the parent [`PhysicalBoneSimulator3D`](PhysicalBoneSimulator3D.md) must be `true` and the [`Skeleton3D`](Skeleton3D.md)'s bone must be assigned to `PhysicalBone3D` correctly; it means that `GetBoneId()` should return a valid id (`>= 0`).

## 构造函数

### PhysicalBone3D

```csharp
public PhysicalBone3D()
```

## 属性

### AngularDamp

```csharp
public float AngularDamp { get; set; }
```

引擎名：`angular_damp`

Damps the body's rotation. By default, the body will use the `ProjectSettings.physics/3d/default_angular_damp` project setting or any value override set by an [`Area3D`](Area3D.md) the body is in. Depending on `AngularDampMode`, you can set `AngularDamp` to be added to or to replace the body's damping value.

See `ProjectSettings.physics/3d/default_angular_damp` for more details about damping.

### AngularDampMode

```csharp
public PhysicalBone3D.DampMode AngularDampMode { get; set; }
```

引擎名：`angular_damp_mode`

Defines how `AngularDamp` is applied.

### AngularVelocity

```csharp
public Vector3 AngularVelocity { get; set; }
```

引擎名：`angular_velocity`

The PhysicalBone3D's rotational velocity in *radians* per second.

### BodyOffset

```csharp
public Transform3D BodyOffset { get; set; }
```

引擎名：`body_offset`

Sets the body's transform.

### Bounce

```csharp
public float Bounce { get; set; }
```

引擎名：`bounce`

The body's bounciness. Values range from `0` (no bounce) to `1` (full bounciness).

**Note:** Even with `Bounce` set to `1.0`, some energy will be lost over time due to linear and angular damping. To have a `PhysicalBone3D` that preserves all its energy over time, set `Bounce` to `1.0`, `LinearDampMode` to [`PhysicalBone3D.DampMode.Replace`](PhysicalBone3D.md), `LinearDamp` to `0.0`, `AngularDampMode` to [`PhysicalBone3D.DampMode.Replace`](PhysicalBone3D.md), and `AngularDamp` to `0.0`.

### CanSleep

```csharp
public bool CanSleep { get; set; }
```

引擎名：`can_sleep`

If `true`, the body is deactivated when there is no movement, so it will not take part in the simulation until it is awakened by an external force.

### CustomIntegrator

```csharp
public bool CustomIntegrator { get; set; }
```

引擎名：`custom_integrator`

If `true`, the standard force integration (like gravity or damping) will be disabled for this body. Other than collision response, the body will only move as determined by the `_IntegrateForces()` method, if that virtual method is overridden.

Setting this property will call the method [`PhysicsServer3D.BodySetOmitForceIntegration`](PhysicsServer3D.md) internally.

### Friction

```csharp
public float Friction { get; set; }
```

引擎名：`friction`

The body's friction, from `0` (frictionless) to `1` (max friction).

### GravityScale

```csharp
public float GravityScale { get; set; }
```

引擎名：`gravity_scale`

This is multiplied by `ProjectSettings.physics/3d/default_gravity` to produce this body's gravity. For example, a value of `1.0` will apply normal gravity, `2.0` will apply double the gravity, and `0.5` will apply half the gravity to this body.

### JointOffset

```csharp
public Transform3D JointOffset { get; set; }
```

引擎名：`joint_offset`

Sets the joint's transform.

### JointRotation

```csharp
public Vector3 JointRotation { get; set; }
```

引擎名：`joint_rotation`

Sets the joint's rotation in radians.

### JointType

```csharp
public PhysicalBone3D.JointTypeEnum JointType { get; set; }
```

引擎名：`joint_type`

Sets the joint type.

### LinearDamp

```csharp
public float LinearDamp { get; set; }
```

引擎名：`linear_damp`

Damps the body's movement. By default, the body will use `ProjectSettings.physics/3d/default_linear_damp` or any value override set by an [`Area3D`](Area3D.md) the body is in. Depending on `LinearDampMode`, `LinearDamp` may be added to or replace the body's damping value.

See `ProjectSettings.physics/3d/default_linear_damp` for more details about damping.

### LinearDampMode

```csharp
public PhysicalBone3D.DampMode LinearDampMode { get; set; }
```

引擎名：`linear_damp_mode`

Defines how `LinearDamp` is applied.

### LinearVelocity

```csharp
public Vector3 LinearVelocity { get; set; }
```

引擎名：`linear_velocity`

The body's linear velocity in units per second. Can be used sporadically, but **don't set this every frame**, because physics may run in another thread and runs at a different granularity. Use `_IntegrateForces()` as your process loop for precise control of the body state.

### Mass

```csharp
public float Mass { get; set; }
```

引擎名：`mass`

The body's mass.

## 方法

### ApplyCentralImpulse

```csharp
public void ApplyCentralImpulse(Vector3 impulse)
```

引擎名：`apply_central_impulse`

Applies a directional impulse without affecting rotation.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_integrate_forces" functions otherwise).

This is equivalent to using `ApplyImpulse()` at the body's center of mass.

### ApplyImpulse

```csharp
public void ApplyImpulse(Vector3 impulse, Vector3? position = null)
```

引擎名：`apply_impulse`

Applies a positioned impulse to the PhysicsBone3D.

An impulse is time-independent! Applying an impulse every frame would result in a framerate-dependent force. For this reason, it should only be used when simulating one-time impacts (use the "_integrate_forces" functions otherwise).

`position` is the offset from the PhysicsBone3D origin in global coordinates.

参数：
- `position`：If the parameter is null, then the default value is `new Vector3(0.0f, 0.0f, 0.0f)`.

### GetAngularDamp

```csharp
public float GetAngularDamp()
```

引擎名：`get_angular_damp`

### GetAngularDampMode

```csharp
public PhysicalBone3D.DampMode GetAngularDampMode()
```

引擎名：`get_angular_damp_mode`

### GetAngularVelocity

```csharp
public Vector3 GetAngularVelocity()
```

引擎名：`get_angular_velocity`

### GetBodyOffset

```csharp
public Transform3D GetBodyOffset()
```

引擎名：`get_body_offset`

### GetBoneId

```csharp
public int GetBoneId()
```

引擎名：`get_bone_id`

Returns the unique identifier of the PhysicsBone3D.

### GetBounce

```csharp
public float GetBounce()
```

引擎名：`get_bounce`

### GetFriction

```csharp
public float GetFriction()
```

引擎名：`get_friction`

### GetGravityScale

```csharp
public float GetGravityScale()
```

引擎名：`get_gravity_scale`

### GetJointOffset

```csharp
public Transform3D GetJointOffset()
```

引擎名：`get_joint_offset`

### GetJointRotation

```csharp
public Vector3 GetJointRotation()
```

引擎名：`get_joint_rotation`

### GetJointType

```csharp
public PhysicalBone3D.JointTypeEnum GetJointType()
```

引擎名：`get_joint_type`

### GetLinearDamp

```csharp
public float GetLinearDamp()
```

引擎名：`get_linear_damp`

### GetLinearDampMode

```csharp
public PhysicalBone3D.DampMode GetLinearDampMode()
```

引擎名：`get_linear_damp_mode`

### GetLinearVelocity

```csharp
public Vector3 GetLinearVelocity()
```

引擎名：`get_linear_velocity`

### GetMass

```csharp
public float GetMass()
```

引擎名：`get_mass`

### GetSimulatePhysics

```csharp
public bool GetSimulatePhysics()
```

引擎名：`get_simulate_physics`

Returns `true` if the PhysicsBone3D is allowed to simulate physics.

### IsAbleToSleep

```csharp
public bool IsAbleToSleep()
```

引擎名：`is_able_to_sleep`

### IsSimulatingPhysics

```csharp
public bool IsSimulatingPhysics()
```

引擎名：`is_simulating_physics`

Returns `true` if the PhysicsBone3D is currently simulating physics.

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
public void SetAngularDampMode(PhysicalBone3D.DampMode angularDampMode)
```

引擎名：`set_angular_damp_mode`

### SetAngularVelocity

```csharp
public void SetAngularVelocity(Vector3 angularVelocity)
```

引擎名：`set_angular_velocity`

### SetBodyOffset

```csharp
public void SetBodyOffset(Transform3D offset)
```

引擎名：`set_body_offset`

### SetBounce

```csharp
public void SetBounce(float bounce)
```

引擎名：`set_bounce`

### SetCanSleep

```csharp
public void SetCanSleep(bool ableToSleep)
```

引擎名：`set_can_sleep`

### SetFriction

```csharp
public void SetFriction(float friction)
```

引擎名：`set_friction`

### SetGravityScale

```csharp
public void SetGravityScale(float gravityScale)
```

引擎名：`set_gravity_scale`

### SetJointOffset

```csharp
public void SetJointOffset(Transform3D offset)
```

引擎名：`set_joint_offset`

### SetJointRotation

```csharp
public void SetJointRotation(Vector3 euler)
```

引擎名：`set_joint_rotation`

### SetJointType

```csharp
public void SetJointType(PhysicalBone3D.JointTypeEnum jointType)
```

引擎名：`set_joint_type`

### SetLinearDamp

```csharp
public void SetLinearDamp(float linearDamp)
```

引擎名：`set_linear_damp`

### SetLinearDampMode

```csharp
public void SetLinearDampMode(PhysicalBone3D.DampMode linearDampMode)
```

引擎名：`set_linear_damp_mode`

### SetLinearVelocity

```csharp
public void SetLinearVelocity(Vector3 linearVelocity)
```

引擎名：`set_linear_velocity`

### SetMass

```csharp
public void SetMass(float mass)
```

引擎名：`set_mass`

### SetUseCustomIntegrator

```csharp
public void SetUseCustomIntegrator(bool enable)
```

引擎名：`set_use_custom_integrator`

### _IntegrateForces

```csharp
public virtual void _IntegrateForces(PhysicsDirectBodyState3D state)
```

引擎名：`_integrate_forces`

Called during physics processing, allowing you to read and safely modify the simulation state for the object. By default, it is called before the standard force integration, but the `CustomIntegrator` property allows you to disable the standard force integration and do fully custom force integration for a body.

## DampMode

命名空间 `Godot`；enum

### 枚举值

- `Combine = 0`：In this mode, the body's damping value is added to any value set in areas or the default value.
- `Replace = 1`：In this mode, the body's damping value replaces any value set in areas or the default value.

## JointTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：No joint is applied to the PhysicsBone3D.
- `Pin = 1`：A pin joint is applied to the PhysicsBone3D.
- `Cone = 2`：A cone joint is applied to the PhysicsBone3D.
- `Hinge = 3`：A hinge joint is applied to the PhysicsBone3D.
- `Slider = 4`：A slider joint is applied to the PhysicsBone3D.
- `Type6Dof = 5`：A 6 degrees of freedom joint is applied to the PhysicsBone3D.
