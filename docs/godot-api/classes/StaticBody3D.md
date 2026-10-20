<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StaticBody3D

命名空间 `Godot`；class；继承 [`PhysicsBody3D`](PhysicsBody3D.md) → [`CollisionObject3D`](CollisionObject3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A static 3D physics body. It can't be moved by external forces or contacts, but can be moved manually by other means such as code, [`AnimationMixer`](AnimationMixer.md)s (with [`AnimationMixer.CallbackModeProcess`](AnimationMixer.md) set to [`AnimationMixer.AnimationCallbackModeProcess.Physics`](AnimationMixer.md)), and [`RemoteTransform3D`](RemoteTransform3D.md).

When `StaticBody3D` is moved, it is teleported to its new position without affecting other physics bodies in its path. If this is not desired, use [`AnimatableBody3D`](AnimatableBody3D.md) instead.

`StaticBody3D` is useful for completely static objects like floors and walls, as well as moving surfaces like conveyor belts and circular revolving platforms (by using `ConstantLinearVelocity` and `ConstantAngularVelocity`).

## 构造函数

### StaticBody3D

```csharp
public StaticBody3D()
```

## 属性

### ConstantAngularVelocity

```csharp
public Vector3 ConstantAngularVelocity { get; set; }
```

引擎名：`constant_angular_velocity`

The body's constant angular velocity. This does not rotate the body, but affects touching bodies, as if it were rotating.

### ConstantLinearVelocity

```csharp
public Vector3 ConstantLinearVelocity { get; set; }
```

引擎名：`constant_linear_velocity`

The body's constant linear velocity. This does not move the body, but affects touching bodies, as if it were moving.

### PhysicsMaterialOverride

```csharp
public PhysicsMaterial PhysicsMaterialOverride { get; set; }
```

引擎名：`physics_material_override`

The physics material override for the body.

If a material is assigned to this property, it will be used instead of any other physics material, such as an inherited one.

## 方法

### GetConstantAngularVelocity

```csharp
public Vector3 GetConstantAngularVelocity()
```

引擎名：`get_constant_angular_velocity`

### GetConstantLinearVelocity

```csharp
public Vector3 GetConstantLinearVelocity()
```

引擎名：`get_constant_linear_velocity`

### GetPhysicsMaterialOverride

```csharp
public PhysicsMaterial GetPhysicsMaterialOverride()
```

引擎名：`get_physics_material_override`

### SetConstantAngularVelocity

```csharp
public void SetConstantAngularVelocity(Vector3 vel)
```

引擎名：`set_constant_angular_velocity`

### SetConstantLinearVelocity

```csharp
public void SetConstantLinearVelocity(Vector3 vel)
```

引擎名：`set_constant_linear_velocity`

### SetPhysicsMaterialOverride

```csharp
public void SetPhysicsMaterialOverride(PhysicsMaterial physicsMaterialOverride)
```

引擎名：`set_physics_material_override`
