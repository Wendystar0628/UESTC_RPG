<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PinJoint2D

命名空间 `Godot`；class；继承 [`Joint2D`](Joint2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that attaches two 2D physics bodies at a single point, allowing them to freely rotate. For example, a [`RigidBody2D`](RigidBody2D.md) can be attached to a [`StaticBody2D`](StaticBody2D.md) to create a pendulum or a seesaw.

## 构造函数

### PinJoint2D

```csharp
public PinJoint2D()
```

## 属性

### AngularLimitEnabled

```csharp
public bool AngularLimitEnabled { get; set; }
```

引擎名：`angular_limit_enabled`

If `true`, the pin maximum and minimum rotation, defined by `AngularLimitLower` and `AngularLimitUpper` are applied.

### AngularLimitLower

```csharp
public float AngularLimitLower { get; set; }
```

引擎名：`angular_limit_lower`

The minimum rotation. Only active if `AngularLimitEnabled` is `true`.

### AngularLimitUpper

```csharp
public float AngularLimitUpper { get; set; }
```

引擎名：`angular_limit_upper`

The maximum rotation. Only active if `AngularLimitEnabled` is `true`.

### MotorEnabled

```csharp
public bool MotorEnabled { get; set; }
```

引擎名：`motor_enabled`

When activated, a motor turns the pin.

### MotorTargetVelocity

```csharp
public float MotorTargetVelocity { get; set; }
```

引擎名：`motor_target_velocity`

Target speed for the motor. In radians per second.

### Softness

```csharp
public float Softness { get; set; }
```

引擎名：`softness`

The higher this value, the more the bond to the pinned partner can flex.

## 方法

### GetAngularLimitLower

```csharp
public float GetAngularLimitLower()
```

引擎名：`get_angular_limit_lower`

### GetAngularLimitUpper

```csharp
public float GetAngularLimitUpper()
```

引擎名：`get_angular_limit_upper`

### GetMotorTargetVelocity

```csharp
public float GetMotorTargetVelocity()
```

引擎名：`get_motor_target_velocity`

### GetSoftness

```csharp
public float GetSoftness()
```

引擎名：`get_softness`

### IsAngularLimitEnabled

```csharp
public bool IsAngularLimitEnabled()
```

引擎名：`is_angular_limit_enabled`

### IsMotorEnabled

```csharp
public bool IsMotorEnabled()
```

引擎名：`is_motor_enabled`

### SetAngularLimitEnabled

```csharp
public void SetAngularLimitEnabled(bool enabled)
```

引擎名：`set_angular_limit_enabled`

### SetAngularLimitLower

```csharp
public void SetAngularLimitLower(float angularLimitLower)
```

引擎名：`set_angular_limit_lower`

### SetAngularLimitUpper

```csharp
public void SetAngularLimitUpper(float angularLimitUpper)
```

引擎名：`set_angular_limit_upper`

### SetMotorEnabled

```csharp
public void SetMotorEnabled(bool enabled)
```

引擎名：`set_motor_enabled`

### SetMotorTargetVelocity

```csharp
public void SetMotorTargetVelocity(float motorTargetVelocity)
```

引擎名：`set_motor_target_velocity`

### SetSoftness

```csharp
public void SetSoftness(float softness)
```

引擎名：`set_softness`
