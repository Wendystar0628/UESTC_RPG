<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# LookAtModifier3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModifier3D`](SkeletonModifier3D.md) rotates a bone to look at a target. This is helpful for moving a character's head to look at the player, rotating a turret to look at a target, or any other case where you want to make a bone rotate towards something quickly and easily.

When applying multiple `LookAtModifier3D`s, the `LookAtModifier3D` assigned to the parent bone must be put above the `LookAtModifier3D` assigned to the child bone in the list in order for the child bone results to be correct.

## 构造函数

### LookAtModifier3D

```csharp
public LookAtModifier3D()
```

## 属性

### Bone

```csharp
public int Bone { get; set; }
```

引擎名：`bone`

Index of the `BoneName` in the parent [`Skeleton3D`](Skeleton3D.md).

### BoneName

```csharp
public string BoneName { get; set; }
```

引擎名：`bone_name`

The bone name of the [`Skeleton3D`](Skeleton3D.md) that the modification will operate on.

### Duration

```csharp
public float Duration { get; set; }
```

引擎名：`duration`

The duration of the time-based interpolation. Interpolation is triggered at the following cases:

- When the target node is changed

- When an axis is flipped due to angle limitation

**Note:** The flipping occurs when the target is outside the angle limitation and the internally computed secondary rotation axis of the forward vector is flipped. Visually, it occurs when the target is outside the angle limitation and crosses the plane of the `ForwardAxis` and `PrimaryRotationAxis`.

### EaseType

```csharp
public Tween.EaseType EaseType { get; set; }
```

引擎名：`ease_type`

The ease type of the time-based interpolation. See also [`Tween.EaseType`](Tween.md).

### ForwardAxis

```csharp
public SkeletonModifier3D.BoneAxis ForwardAxis { get; set; }
```

引擎名：`forward_axis`

The forward axis of the bone. This [`SkeletonModifier3D`](SkeletonModifier3D.md) modifies the bone so that this axis points toward the `TargetNode`.

### OriginBone

```csharp
public int OriginBone { get; set; }
```

引擎名：`origin_bone`

Index of the `OriginBoneName` in the parent [`Skeleton3D`](Skeleton3D.md).

### OriginBoneName

```csharp
public string OriginBoneName { get; set; }
```

引擎名：`origin_bone_name`

If `OriginFrom` is [`LookAtModifier3D.OriginFromEnum.SpecificBone`](LookAtModifier3D.md), the bone global pose position specified for this is used as origin.

### OriginExternalNode

```csharp
public NodePath OriginExternalNode { get; set; }
```

引擎名：`origin_external_node`

If `OriginFrom` is [`LookAtModifier3D.OriginFromEnum.ExternalNode`](LookAtModifier3D.md), the global position of the [`Node3D`](Node3D.md) specified for this is used as origin.

### OriginFrom

```csharp
public LookAtModifier3D.OriginFromEnum OriginFrom { get; set; }
```

引擎名：`origin_from`

This value determines from what origin is retrieved for use in the calculation of the forward vector.

### OriginOffset

```csharp
public Vector3 OriginOffset { get; set; }
```

引擎名：`origin_offset`

The offset of the bone pose origin. Matching the origins by offset is useful for cases where multiple bones must always face the same direction, such as the eyes.

**Note:** This value indicates the local position of the object set in `OriginFrom`.

### OriginSafeMargin

```csharp
public float OriginSafeMargin { get; set; }
```

引擎名：`origin_safe_margin`

If the target passes through too close to the origin than this value, time-based interpolation is used even if the target is within the angular limitations, to prevent the angular velocity from becoming too high.

### PrimaryDampThreshold

```csharp
public float PrimaryDampThreshold { get; set; }
```

引擎名：`primary_damp_threshold`

The threshold to start damping for `PrimaryLimitAngle`. It provides non-linear (b-spline) interpolation, let it feel more resistance the more it rotate to the edge limit. This is useful for simulating the limits of human motion.

If `1.0`, no damping is performed. If `0.0`, damping is always performed.

### PrimaryLimitAngle

```csharp
public float PrimaryLimitAngle { get; set; }
```

引擎名：`primary_limit_angle`

The limit angle of the primary rotation when `SymmetryLimitation` is `true`, in radians.

### PrimaryNegativeDampThreshold

```csharp
public float PrimaryNegativeDampThreshold { get; set; }
```

引擎名：`primary_negative_damp_threshold`

The threshold to start damping for `PrimaryNegativeLimitAngle`.

### PrimaryNegativeLimitAngle

```csharp
public float PrimaryNegativeLimitAngle { get; set; }
```

引擎名：`primary_negative_limit_angle`

The limit angle of negative side of the primary rotation when `SymmetryLimitation` is `false`, in radians.

### PrimaryPositiveDampThreshold

```csharp
public float PrimaryPositiveDampThreshold { get; set; }
```

引擎名：`primary_positive_damp_threshold`

The threshold to start damping for `PrimaryPositiveLimitAngle`.

### PrimaryPositiveLimitAngle

```csharp
public float PrimaryPositiveLimitAngle { get; set; }
```

引擎名：`primary_positive_limit_angle`

The limit angle of positive side of the primary rotation when `SymmetryLimitation` is `false`, in radians.

### PrimaryRotationAxis

```csharp
public Vector3.Axis PrimaryRotationAxis { get; set; }
```

引擎名：`primary_rotation_axis`

The axis of the first rotation. This [`SkeletonModifier3D`](SkeletonModifier3D.md) works by compositing the rotation by Euler angles to prevent to rotate the `ForwardAxis`.

### Relative

```csharp
public bool Relative { get; set; }
```

引擎名：`relative`

The relative option. If `true`, the rotation is applied relative to the pose. If `false`, the rotation is applied relative to the rest. It means to replace the current pose with the `LookAtModifier3D`'s result.

**Note:** This option affects the base angle for `UseAngleLimitation`. Since the `LookAtModifier3D` relies strongly on Euler rotation, the axis that determines the limitation and the actual rotation are strongly tied together.

### SecondaryDampThreshold

```csharp
public float SecondaryDampThreshold { get; set; }
```

引擎名：`secondary_damp_threshold`

The threshold to start damping for `SecondaryLimitAngle`.

### SecondaryLimitAngle

```csharp
public float SecondaryLimitAngle { get; set; }
```

引擎名：`secondary_limit_angle`

The limit angle of the secondary rotation when `SymmetryLimitation` is `true`, in radians.

### SecondaryNegativeDampThreshold

```csharp
public float SecondaryNegativeDampThreshold { get; set; }
```

引擎名：`secondary_negative_damp_threshold`

The threshold to start damping for `SecondaryNegativeLimitAngle`.

### SecondaryNegativeLimitAngle

```csharp
public float SecondaryNegativeLimitAngle { get; set; }
```

引擎名：`secondary_negative_limit_angle`

The limit angle of negative side of the secondary rotation when `SymmetryLimitation` is `false`, in radians.

### SecondaryPositiveDampThreshold

```csharp
public float SecondaryPositiveDampThreshold { get; set; }
```

引擎名：`secondary_positive_damp_threshold`

The threshold to start damping for `SecondaryPositiveLimitAngle`.

### SecondaryPositiveLimitAngle

```csharp
public float SecondaryPositiveLimitAngle { get; set; }
```

引擎名：`secondary_positive_limit_angle`

The limit angle of positive side of the secondary rotation when `SymmetryLimitation` is `false`, in radians.

### SymmetryLimitation

```csharp
public bool SymmetryLimitation { get; set; }
```

引擎名：`symmetry_limitation`

If `true`, the limitations are spread from the bone symmetrically.

If `false`, the limitation can be specified separately for each side of the bone rest.

### TargetNode

```csharp
public NodePath TargetNode { get; set; }
```

引擎名：`target_node`

The [`NodePath`](NodePath.md) to the node that is the target for the look at modification. This node is what the modification will rotate the bone to.

### TransitionType

```csharp
public Tween.TransitionType TransitionType { get; set; }
```

引擎名：`transition_type`

The transition type of the time-based interpolation. See also [`Tween.TransitionType`](Tween.md).

### UseAngleLimitation

```csharp
public bool UseAngleLimitation { get; set; }
```

引擎名：`use_angle_limitation`

If `true`, limits the amount of rotation. For example, this helps to prevent a character's neck from rotating 360 degrees.

**Note:** As with [`AnimationTree`](AnimationTree.md) blending, interpolation is provided that favors [`Skeleton3D.GetBoneRest`](Skeleton3D.md) or [`Skeleton3D.GetBonePose`](Skeleton3D.md) depends on the `Relative` option. This means that interpolation does not select the shortest path in some cases.

**Note:** Some values for `TransitionType` (such as [`Tween.TransitionType.Back`](Tween.md), [`Tween.TransitionType.Elastic`](Tween.md), and [`Tween.TransitionType.Spring`](Tween.md)) may exceed the limitations. If interpolation occurs while overshooting the limitations, the result might not respect the bone rest.

### UseSecondaryRotation

```csharp
public bool UseSecondaryRotation { get; set; }
```

引擎名：`use_secondary_rotation`

If `true`, provides rotation by two axes.

## 方法

### GetBone

```csharp
public int GetBone()
```

引擎名：`get_bone`

### GetBoneName

```csharp
public string GetBoneName()
```

引擎名：`get_bone_name`

### GetDuration

```csharp
public float GetDuration()
```

引擎名：`get_duration`

### GetEaseType

```csharp
public Tween.EaseType GetEaseType()
```

引擎名：`get_ease_type`

### GetForwardAxis

```csharp
public SkeletonModifier3D.BoneAxis GetForwardAxis()
```

引擎名：`get_forward_axis`

### GetInterpolationRemaining

```csharp
public float GetInterpolationRemaining()
```

引擎名：`get_interpolation_remaining`

Returns the remaining seconds of the time-based interpolation.

### GetOriginBone

```csharp
public int GetOriginBone()
```

引擎名：`get_origin_bone`

### GetOriginBoneName

```csharp
public string GetOriginBoneName()
```

引擎名：`get_origin_bone_name`

### GetOriginExternalNode

```csharp
public NodePath GetOriginExternalNode()
```

引擎名：`get_origin_external_node`

### GetOriginFrom

```csharp
public LookAtModifier3D.OriginFromEnum GetOriginFrom()
```

引擎名：`get_origin_from`

### GetOriginOffset

```csharp
public Vector3 GetOriginOffset()
```

引擎名：`get_origin_offset`

### GetOriginSafeMargin

```csharp
public float GetOriginSafeMargin()
```

引擎名：`get_origin_safe_margin`

### GetPrimaryDampThreshold

```csharp
public float GetPrimaryDampThreshold()
```

引擎名：`get_primary_damp_threshold`

### GetPrimaryLimitAngle

```csharp
public float GetPrimaryLimitAngle()
```

引擎名：`get_primary_limit_angle`

### GetPrimaryNegativeDampThreshold

```csharp
public float GetPrimaryNegativeDampThreshold()
```

引擎名：`get_primary_negative_damp_threshold`

### GetPrimaryNegativeLimitAngle

```csharp
public float GetPrimaryNegativeLimitAngle()
```

引擎名：`get_primary_negative_limit_angle`

### GetPrimaryPositiveDampThreshold

```csharp
public float GetPrimaryPositiveDampThreshold()
```

引擎名：`get_primary_positive_damp_threshold`

### GetPrimaryPositiveLimitAngle

```csharp
public float GetPrimaryPositiveLimitAngle()
```

引擎名：`get_primary_positive_limit_angle`

### GetPrimaryRotationAxis

```csharp
public Vector3.Axis GetPrimaryRotationAxis()
```

引擎名：`get_primary_rotation_axis`

### GetSecondaryDampThreshold

```csharp
public float GetSecondaryDampThreshold()
```

引擎名：`get_secondary_damp_threshold`

### GetSecondaryLimitAngle

```csharp
public float GetSecondaryLimitAngle()
```

引擎名：`get_secondary_limit_angle`

### GetSecondaryNegativeDampThreshold

```csharp
public float GetSecondaryNegativeDampThreshold()
```

引擎名：`get_secondary_negative_damp_threshold`

### GetSecondaryNegativeLimitAngle

```csharp
public float GetSecondaryNegativeLimitAngle()
```

引擎名：`get_secondary_negative_limit_angle`

### GetSecondaryPositiveDampThreshold

```csharp
public float GetSecondaryPositiveDampThreshold()
```

引擎名：`get_secondary_positive_damp_threshold`

### GetSecondaryPositiveLimitAngle

```csharp
public float GetSecondaryPositiveLimitAngle()
```

引擎名：`get_secondary_positive_limit_angle`

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

引擎名：`get_target_node`

### GetTransitionType

```csharp
public Tween.TransitionType GetTransitionType()
```

引擎名：`get_transition_type`

### IsInterpolating

```csharp
public bool IsInterpolating()
```

引擎名：`is_interpolating`

Returns `true` if time-based interpolation is running. If `true`, it is equivalent to `GetInterpolationRemaining()` returning `0.0`.

This is useful to determine whether a `LookAtModifier3D` can be removed safely.

### IsLimitationSymmetry

```csharp
public bool IsLimitationSymmetry()
```

引擎名：`is_limitation_symmetry`

### IsRelative

```csharp
public bool IsRelative()
```

引擎名：`is_relative`

### IsTargetWithinLimitation

```csharp
public bool IsTargetWithinLimitation()
```

引擎名：`is_target_within_limitation`

Returns whether the target is within the angle limitations. It is useful for unsetting the `TargetNode` when the target is outside of the angle limitations.

**Note:** The value is updated after [`SkeletonModifier3D._ProcessModification()`](SkeletonModifier3D.md). To retrieve this value correctly, we recommend using the signal [`SkeletonModifier3D.ModificationProcessed`](SkeletonModifier3D.md).

### IsUsingAngleLimitation

```csharp
public bool IsUsingAngleLimitation()
```

引擎名：`is_using_angle_limitation`

### IsUsingSecondaryRotation

```csharp
public bool IsUsingSecondaryRotation()
```

引擎名：`is_using_secondary_rotation`

### SetBone

```csharp
public void SetBone(int bone)
```

引擎名：`set_bone`

### SetBoneName

```csharp
public void SetBoneName(string boneName)
```

引擎名：`set_bone_name`

### SetDuration

```csharp
public void SetDuration(float duration)
```

引擎名：`set_duration`

### SetEaseType

```csharp
public void SetEaseType(Tween.EaseType easeType)
```

引擎名：`set_ease_type`

### SetForwardAxis

```csharp
public void SetForwardAxis(SkeletonModifier3D.BoneAxis forwardAxis)
```

引擎名：`set_forward_axis`

### SetOriginBone

```csharp
public void SetOriginBone(int bone)
```

引擎名：`set_origin_bone`

### SetOriginBoneName

```csharp
public void SetOriginBoneName(string boneName)
```

引擎名：`set_origin_bone_name`

### SetOriginExternalNode

```csharp
public void SetOriginExternalNode(NodePath externalNode)
```

引擎名：`set_origin_external_node`

### SetOriginFrom

```csharp
public void SetOriginFrom(LookAtModifier3D.OriginFromEnum originFrom)
```

引擎名：`set_origin_from`

### SetOriginOffset

```csharp
public void SetOriginOffset(Vector3 offset)
```

引擎名：`set_origin_offset`

### SetOriginSafeMargin

```csharp
public void SetOriginSafeMargin(float margin)
```

引擎名：`set_origin_safe_margin`

### SetPrimaryDampThreshold

```csharp
public void SetPrimaryDampThreshold(float power)
```

引擎名：`set_primary_damp_threshold`

### SetPrimaryLimitAngle

```csharp
public void SetPrimaryLimitAngle(float angle)
```

引擎名：`set_primary_limit_angle`

### SetPrimaryNegativeDampThreshold

```csharp
public void SetPrimaryNegativeDampThreshold(float power)
```

引擎名：`set_primary_negative_damp_threshold`

### SetPrimaryNegativeLimitAngle

```csharp
public void SetPrimaryNegativeLimitAngle(float angle)
```

引擎名：`set_primary_negative_limit_angle`

### SetPrimaryPositiveDampThreshold

```csharp
public void SetPrimaryPositiveDampThreshold(float power)
```

引擎名：`set_primary_positive_damp_threshold`

### SetPrimaryPositiveLimitAngle

```csharp
public void SetPrimaryPositiveLimitAngle(float angle)
```

引擎名：`set_primary_positive_limit_angle`

### SetPrimaryRotationAxis

```csharp
public void SetPrimaryRotationAxis(Vector3.Axis axis)
```

引擎名：`set_primary_rotation_axis`

### SetRelative

```csharp
public void SetRelative(bool enabled)
```

引擎名：`set_relative`

### SetSecondaryDampThreshold

```csharp
public void SetSecondaryDampThreshold(float power)
```

引擎名：`set_secondary_damp_threshold`

### SetSecondaryLimitAngle

```csharp
public void SetSecondaryLimitAngle(float angle)
```

引擎名：`set_secondary_limit_angle`

### SetSecondaryNegativeDampThreshold

```csharp
public void SetSecondaryNegativeDampThreshold(float power)
```

引擎名：`set_secondary_negative_damp_threshold`

### SetSecondaryNegativeLimitAngle

```csharp
public void SetSecondaryNegativeLimitAngle(float angle)
```

引擎名：`set_secondary_negative_limit_angle`

### SetSecondaryPositiveDampThreshold

```csharp
public void SetSecondaryPositiveDampThreshold(float power)
```

引擎名：`set_secondary_positive_damp_threshold`

### SetSecondaryPositiveLimitAngle

```csharp
public void SetSecondaryPositiveLimitAngle(float angle)
```

引擎名：`set_secondary_positive_limit_angle`

### SetSymmetryLimitation

```csharp
public void SetSymmetryLimitation(bool enabled)
```

引擎名：`set_symmetry_limitation`

### SetTargetNode

```csharp
public void SetTargetNode(NodePath targetNode)
```

引擎名：`set_target_node`

### SetTransitionType

```csharp
public void SetTransitionType(Tween.TransitionType transitionType)
```

引擎名：`set_transition_type`

### SetUseAngleLimitation

```csharp
public void SetUseAngleLimitation(bool enabled)
```

引擎名：`set_use_angle_limitation`

### SetUseSecondaryRotation

```csharp
public void SetUseSecondaryRotation(bool enabled)
```

引擎名：`set_use_secondary_rotation`

## OriginFromEnum

命名空间 `Godot`；enum

### 枚举值

- `Self = 0`：The bone rest position of the bone specified in [`LookAtModifier3D.Bone`](LookAtModifier3D.md) is used as origin.
- `SpecificBone = 1`：The bone global pose position of the bone specified in [`LookAtModifier3D.OriginBone`](LookAtModifier3D.md) is used as origin.
- `ExternalNode = 2`：The global position of the [`Node3D`](Node3D.md) specified in [`LookAtModifier3D.OriginExternalNode`](LookAtModifier3D.md) is used as origin.
