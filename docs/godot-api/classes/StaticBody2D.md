<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# StaticBody2D

命名空间 `Godot`；class；继承 [`PhysicsBody2D`](PhysicsBody2D.md) → [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A static 2D physics body. It can't be moved by external forces or contacts, but can be moved manually by other means such as code, [`AnimationMixer`](AnimationMixer.md)s (with [`AnimationMixer.CallbackModeProcess`](AnimationMixer.md) set to [`AnimationMixer.AnimationCallbackModeProcess.Physics`](AnimationMixer.md)), and [`RemoteTransform2D`](RemoteTransform2D.md).

When `StaticBody2D` is moved, it is teleported to its new position without affecting other physics bodies in its path. If this is not desired, use [`AnimatableBody2D`](AnimatableBody2D.md) instead.

`StaticBody2D` is useful for completely static objects like floors and walls, as well as moving surfaces like conveyor belts and circular revolving platforms (by using `ConstantLinearVelocity` and `ConstantAngularVelocity`).

## 构造函数

### StaticBody2D

```csharp
public StaticBody2D()
```

## 属性

### ConstantAngularVelocity

```csharp
public float ConstantAngularVelocity { get; set; }
```

引擎名：`constant_angular_velocity`

The body's constant angular velocity. This does not rotate the body, but affects touching bodies, as if it were rotating.

### ConstantLinearVelocity

```csharp
public Vector2 ConstantLinearVelocity { get; set; }
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
public float GetConstantAngularVelocity()
```

引擎名：`get_constant_angular_velocity`

### GetConstantLinearVelocity

```csharp
public Vector2 GetConstantLinearVelocity()
```

引擎名：`get_constant_linear_velocity`

### GetPhysicsMaterialOverride

```csharp
public PhysicsMaterial GetPhysicsMaterialOverride()
```

引擎名：`get_physics_material_override`

### SetConstantAngularVelocity

```csharp
public void SetConstantAngularVelocity(float vel)
```

引擎名：`set_constant_angular_velocity`

### SetConstantLinearVelocity

```csharp
public void SetConstantLinearVelocity(Vector2 vel)
```

引擎名：`set_constant_linear_velocity`

### SetPhysicsMaterialOverride

```csharp
public void SetPhysicsMaterialOverride(PhysicsMaterial physicsMaterialOverride)
```

引擎名：`set_physics_material_override`
