<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# HingeJoint3D

命名空间 `Godot`；class；继承 [`Joint3D`](Joint3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that restricts the rotation of a 3D physics body around an axis relative to another physics body. For example, Body A can be a [`StaticBody3D`](StaticBody3D.md) representing a door hinge that a [`RigidBody3D`](RigidBody3D.md) rotates around.

## 构造函数

### HingeJoint3D

```csharp
public HingeJoint3D()
```

## 方法

### GetFlag

```csharp
public bool GetFlag(HingeJoint3D.Flag flag)
```

引擎名：`get_flag`

Returns the value of the specified flag.

### GetParam

```csharp
public float GetParam(HingeJoint3D.Param param)
```

引擎名：`get_param`

Returns the value of the specified parameter.

### SetFlag

```csharp
public void SetFlag(HingeJoint3D.Flag flag, bool enabled)
```

引擎名：`set_flag`

If `true`, enables the specified flag.

### SetParam

```csharp
public void SetParam(HingeJoint3D.Param param, float value)
```

引擎名：`set_param`

Sets the value of the specified parameter.

## Flag

命名空间 `Godot`；enum

### 枚举值

- `UseLimit = 0`：If `true`, the hinges maximum and minimum rotation, defined by `angular_limit/lower` and `angular_limit/upper` has effects.
- `EnableMotor = 1`：When activated, a motor turns the hinge.
- `Max = 2`：Represents the size of the `HingeJoint3D.Flag` enum.

## Param

命名空间 `Godot`；enum

### 枚举值

- `Bias = 0`：The speed with which the two bodies get pulled together when they move in different directions.
- `LimitUpper = 1`：The maximum rotation.
- `LimitLower = 2`：The minimum rotation.
- `LimitBias = 3`：The speed with which the rotation across the axis perpendicular to the hinge gets corrected.
- `LimitSoftness = 4`
- `LimitRelaxation = 5`：The lower this value, the more the rotation gets slowed down.
- `MotorTargetVelocity = 6`：Target speed for the motor.
- `MotorMaxImpulse = 7`：Maximum acceleration for the motor.
- `Max = 8`：Represents the size of the `HingeJoint3D.Param` enum.
