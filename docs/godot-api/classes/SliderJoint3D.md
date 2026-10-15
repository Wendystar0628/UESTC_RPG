<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SliderJoint3D

命名空间 `Godot`；class；继承 [`Joint3D`](Joint3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A physics joint that restricts the movement of a 3D physics body along an axis relative to another physics body. For example, Body A could be a [`StaticBody3D`](StaticBody3D.md) representing a piston base, while Body B could be a [`RigidBody3D`](RigidBody3D.md) representing the piston head, moving up and down.

## 构造函数

### SliderJoint3D

```csharp
public SliderJoint3D()
```

## 方法

### GetParam

```csharp
public float GetParam(SliderJoint3D.Param param)
```

引擎名：`get_param`

Returns the value of the given parameter.

### SetParam

```csharp
public void SetParam(SliderJoint3D.Param param, float value)
```

引擎名：`set_param`

Assigns `value` to the given parameter.

## Param

命名空间 `Godot`；enum

### 枚举值

- `LinearLimitUpper = 0`：Constant for accessing `linear_limit/upper_distance`.
- `LinearLimitLower = 1`：Constant for accessing `linear_limit/lower_distance`.
- `LinearLimitSoftness = 2`：Constant for accessing `linear_limit/softness`.
- `LinearLimitRestitution = 3`：Constant for accessing `linear_limit/restitution`.
- `LinearLimitDamping = 4`：Constant for accessing `linear_limit/damping`.
- `LinearMotionSoftness = 5`：Constant for accessing `linear_motion/softness`.
- `LinearMotionRestitution = 6`：Constant for accessing `linear_motion/restitution`.
- `LinearMotionDamping = 7`：Constant for accessing `linear_motion/damping`.
- `LinearOrthogonalSoftness = 8`：Constant for accessing `linear_ortho/softness`.
- `LinearOrthogonalRestitution = 9`：Constant for accessing `linear_motion/restitution`.
- `LinearOrthogonalDamping = 10`：Constant for accessing `linear_motion/damping`.
- `AngularLimitUpper = 11`：Constant for accessing `angular_limit/upper_angle`.
- `AngularLimitLower = 12`：Constant for accessing `angular_limit/lower_angle`.
- `AngularLimitSoftness = 13`：Constant for accessing `angular_limit/softness`.
- `AngularLimitRestitution = 14`：Constant for accessing `angular_limit/restitution`.
- `AngularLimitDamping = 15`：Constant for accessing `angular_limit/damping`.
- `AngularMotionSoftness = 16`：Constant for accessing `angular_motion/softness`.
- `AngularMotionRestitution = 17`：Constant for accessing `angular_motion/restitution`.
- `AngularMotionDamping = 18`：Constant for accessing `angular_motion/damping`.
- `AngularOrthogonalSoftness = 19`：Constant for accessing `angular_ortho/softness`.
- `AngularOrthogonalRestitution = 20`：Constant for accessing `angular_ortho/restitution`.
- `AngularOrthogonalDamping = 21`：Constant for accessing `angular_ortho/damping`.
- `Max = 22`：Represents the size of the `SliderJoint3D.Param` enum.
