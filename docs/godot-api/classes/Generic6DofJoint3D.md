<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Generic6DofJoint3D

命名空间 `Godot`；class；继承 [`Joint3D`](Joint3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `Generic6DofJoint3D` (6 Degrees Of Freedom) joint allows for implementing custom types of joints by locking the rotation and translation of certain axes.

The first 3 DOF represent the linear motion of the physics bodies and the last 3 DOF represent the angular motion of the physics bodies. Each axis can be either locked, or limited.

## 构造函数

### Generic6DofJoint3D

```csharp
public Generic6DofJoint3D()
```

## 方法

### GetFlagX

```csharp
public bool GetFlagX(Generic6DofJoint3D.Flag flag)
```

### GetFlagY

```csharp
public bool GetFlagY(Generic6DofJoint3D.Flag flag)
```

### GetFlagZ

```csharp
public bool GetFlagZ(Generic6DofJoint3D.Flag flag)
```

### GetParamX

```csharp
public float GetParamX(Generic6DofJoint3D.Param param)
```

### GetParamY

```csharp
public float GetParamY(Generic6DofJoint3D.Param param)
```

### GetParamZ

```csharp
public float GetParamZ(Generic6DofJoint3D.Param param)
```

### SetFlagX

```csharp
public void SetFlagX(Generic6DofJoint3D.Flag flag, bool value)
```

### SetFlagY

```csharp
public void SetFlagY(Generic6DofJoint3D.Flag flag, bool value)
```

### SetFlagZ

```csharp
public void SetFlagZ(Generic6DofJoint3D.Flag flag, bool value)
```

### SetParamX

```csharp
public void SetParamX(Generic6DofJoint3D.Param param, float value)
```

### SetParamY

```csharp
public void SetParamY(Generic6DofJoint3D.Param param, float value)
```

### SetParamZ

```csharp
public void SetParamZ(Generic6DofJoint3D.Param param, float value)
```

## Flag

命名空间 `Godot`；enum

### 枚举值

- `EnableLinearLimit = 0`：If enabled, linear motion is possible within the given limits.
- `EnableAngularLimit = 1`：If enabled, rotational motion is possible within the given limits.
- `EnableLinearSpring = 3`
- `EnableAngularSpring = 2`
- `EnableMotor = 4`：If enabled, there is a rotational motor across these axes.
- `EnableLinearMotor = 5`：If enabled, there is a linear motor across these axes.
- `Max = 6`：Represents the size of the `Generic6DofJoint3D.Flag` enum.

## Param

命名空间 `Godot`；enum

### 枚举值

- `LinearLowerLimit = 0`：The minimum difference between the pivot points' axes.
- `LinearUpperLimit = 1`：The maximum difference between the pivot points' axes.
- `LinearLimitSoftness = 2`：A factor applied to the movement across the axes.
- `LinearRestitution = 3`：The amount of restitution on the axes' movement.
- `LinearDamping = 4`：The amount of damping that happens at the linear motion across the axes.
- `LinearMotorTargetVelocity = 5`：The velocity the linear motor will try to reach.
- `LinearMotorForceLimit = 6`：The maximum force the linear motor will apply while trying to reach the velocity target.
- `LinearSpringStiffness = 7`
- `LinearSpringDamping = 8`
- `LinearSpringEquilibriumPoint = 9`
- `AngularLowerLimit = 10`：The minimum rotation in negative direction to break loose and rotate around the axes.
- `AngularUpperLimit = 11`：The minimum rotation in positive direction to break loose and rotate around the axes.
- `AngularLimitSoftness = 12`：The speed of all rotations across the axes.
- `AngularDamping = 13`：The amount of rotational damping across the axes.
- `AngularRestitution = 14`：The amount of rotational restitution across the axes.
- `AngularForceLimit = 15`：The maximum amount of force that can occur, when rotating around the axes.
- `AngularErp = 16`：When rotating across the axes, this error tolerance factor defines how much the correction gets slowed down.
- `AngularMotorTargetVelocity = 17`：Target speed for the motor at the axes.
- `AngularMotorForceLimit = 18`：Maximum acceleration for the motor at the axes.
- `AngularSpringStiffness = 19`
- `AngularSpringDamping = 20`
- `AngularSpringEquilibriumPoint = 21`
- `Max = 22`：Represents the size of the `Generic6DofJoint3D.Param` enum.
