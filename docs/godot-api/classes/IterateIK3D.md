<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IterateIK3D

命名空间 `Godot`；class；继承 [`ChainIK3D`](ChainIK3D.md) → [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class of [`SkeletonModifier3D`](SkeletonModifier3D.md) to approach the goal by repeating small rotations.

Each bone chain (setting) has one effector, which is processed in order of the setting list. You can set some limitations for each joint.

**Note:** All the methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/target_node`).

## 属性

### AngularDeltaLimit

```csharp
public double AngularDeltaLimit { get; set; }
```

引擎名：`angular_delta_limit`

The maximum amount each bone can rotate in a single iteration.

**Note:** This limitation is applied during each iteration. For example, if `MaxIterations` is `4` and `AngularDeltaLimit` is `5` degrees, the maximum rotation possible in a single frame is `20` degrees.

### Deterministic

```csharp
public bool Deterministic { get; set; }
```

引擎名：`deterministic`

If `false`, the result is calculated from the previous frame's `IterateIK3D` result as the initial state.

If `true`, the previous frame's `IterateIK3D` result is discarded. At this point, the new result is calculated from the bone pose excluding the `IterateIK3D` as the initial state. This means the result will be always equal as long as the target position and the previous bone pose are the same. However, if `AngularDeltaLimit` and `MaxIterations` are set too small, the end bone of the chain will never reach the target.

### MaxIterations

```csharp
public int MaxIterations { get; set; }
```

引擎名：`max_iterations`

The number of iteration loops used by the IK solver to produce more accurate results.

### MinDistance

```csharp
public double MinDistance { get; set; }
```

引擎名：`min_distance`

The minimum distance between the end bone and the target. If the distance is below this value, the IK solver stops any further iterations.

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings.

## 方法

### GetAngularDeltaLimit

```csharp
public double GetAngularDeltaLimit()
```

引擎名：`get_angular_delta_limit`

### GetJointLimitation

```csharp
public JointLimitation3D GetJointLimitation(int index, int joint)
```

引擎名：`get_joint_limitation`

Returns the joint limitation at `joint` in the bone chain's joint list.

### GetJointLimitationRightAxis

```csharp
public SkeletonModifier3D.SecondaryDirection GetJointLimitationRightAxis(int index, int joint)
```

引擎名：`get_joint_limitation_right_axis`

Returns the joint limitation right axis at `joint` in the bone chain's joint list.

### GetJointLimitationRightAxisVector

```csharp
public Vector3 GetJointLimitationRightAxisVector(int index, int joint)
```

引擎名：`get_joint_limitation_right_axis_vector`

Returns the joint limitation right axis vector at `joint` in the bone chain's joint list.

If `GetJointLimitationRightAxis()` is [`SkeletonModifier3D.SecondaryDirection.None`](SkeletonModifier3D.md), this method returns `Vector3(0, 0, 0)`.

### GetJointLimitationRotationOffset

```csharp
public Quaternion GetJointLimitationRotationOffset(int index, int joint)
```

引擎名：`get_joint_limitation_rotation_offset`

Returns the joint limitation rotation offset at `joint` in the bone chain's joint list.

Rotation is done in the local space which is constructed by the bone direction (in general parent to child) as the +Y axis and `GetJointLimitationRightAxisVector()` as the +X axis.

If the +X and +Y axes are not orthogonal, the +X axis is implicitly modified to make it orthogonal.

Also, if the length of `GetJointLimitationRightAxisVector()` is zero, the space is created by rotating the reference pose using the shortest arc that rotates the +Y axis of the reference pose to match the bone direction.

In here, the reference pose is the bone pose immediately before processing IK.

### GetJointRotationAxis

```csharp
public SkeletonModifier3D.RotationAxis GetJointRotationAxis(int index, int joint)
```

引擎名：`get_joint_rotation_axis`

Returns the rotation axis at `joint` in the bone chain's joint list.

### GetJointRotationAxisVector

```csharp
public Vector3 GetJointRotationAxisVector(int index, int joint)
```

引擎名：`get_joint_rotation_axis_vector`

Returns the rotation axis vector for the specified joint in the bone chain. This vector represents the axis around which the joint can rotate. It is determined based on the rotation axis set for the joint.

If `GetJointRotationAxis()` is [`SkeletonModifier3D.RotationAxis.All`](SkeletonModifier3D.md), this method returns `Vector3(0, 0, 0)`.

### GetMaxIterations

```csharp
public int GetMaxIterations()
```

引擎名：`get_max_iterations`

### GetMinDistance

```csharp
public double GetMinDistance()
```

引擎名：`get_min_distance`

### GetTargetNode

```csharp
public NodePath GetTargetNode(int index)
```

引擎名：`get_target_node`

Returns the target node that the end bone is trying to reach.

### IsDeterministic

```csharp
public bool IsDeterministic()
```

引擎名：`is_deterministic`

### SetAngularDeltaLimit

```csharp
public void SetAngularDeltaLimit(double angularDeltaLimit)
```

引擎名：`set_angular_delta_limit`

### SetDeterministic

```csharp
public void SetDeterministic(bool deterministic)
```

引擎名：`set_deterministic`

### SetJointLimitation

```csharp
public void SetJointLimitation(int index, int joint, JointLimitation3D limitation)
```

引擎名：`set_joint_limitation`

Sets the joint limitation at `joint` in the bone chain's joint list.

### SetJointLimitationRightAxis

```csharp
public void SetJointLimitationRightAxis(int index, int joint, SkeletonModifier3D.SecondaryDirection direction)
```

引擎名：`set_joint_limitation_right_axis`

Sets the joint limitation right axis at `joint` in the bone chain's joint list.

### SetJointLimitationRightAxisVector

```csharp
public void SetJointLimitationRightAxisVector(int index, int joint, Vector3 vector)
```

引擎名：`set_joint_limitation_right_axis_vector`

Sets the optional joint limitation right axis vector at `joint` in the bone chain's joint list.

### SetJointLimitationRotationOffset

```csharp
public void SetJointLimitationRotationOffset(int index, int joint, Quaternion offset)
```

引擎名：`set_joint_limitation_rotation_offset`

Sets the joint limitation rotation offset at `joint` in the bone chain's joint list.

Rotation is done in the local space which is constructed by the bone direction (in general parent to child) as the +Y axis and `GetJointLimitationRightAxisVector()` as the +X axis.

If the +X and +Y axes are not orthogonal, the +X axis is implicitly modified to make it orthogonal.

Also, if the length of `GetJointLimitationRightAxisVector()` is zero, the space is created by rotating the reference pose using the shortest arc that rotates the +Y axis of the reference pose to match the bone direction.

In here, the reference pose is the bone pose immediately before processing IK.

### SetJointRotationAxis

```csharp
public void SetJointRotationAxis(int index, int joint, SkeletonModifier3D.RotationAxis axis)
```

引擎名：`set_joint_rotation_axis`

Sets the rotation axis at `joint` in the bone chain's joint list.

The axes are based on the reference pose's space, if `axis` is [`SkeletonModifier3D.RotationAxis.Custom`](SkeletonModifier3D.md), you can specify any axis.

In here, the reference pose is the bone pose immediately before processing IK.

**Note:** The rotation axis and the forward vector shouldn't be colinear to avoid unintended rotation since [`ChainIK3D`](ChainIK3D.md) does not factor in twisting forces.

### SetJointRotationAxisVector

```csharp
public void SetJointRotationAxisVector(int index, int joint, Vector3 axisVector)
```

引擎名：`set_joint_rotation_axis_vector`

Sets the rotation axis vector for the specified joint in the bone chain.

This vector is normalized by an internal process and represents the axis around which the bone chain can rotate.

If the vector length is `0`, it is considered synonymous with [`SkeletonModifier3D.RotationAxis.All`](SkeletonModifier3D.md).

### SetMaxIterations

```csharp
public void SetMaxIterations(int maxIterations)
```

引擎名：`set_max_iterations`

### SetMinDistance

```csharp
public void SetMinDistance(double minDistance)
```

引擎名：`set_min_distance`

### SetTargetNode

```csharp
public void SetTargetNode(int index, NodePath targetNode)
```

引擎名：`set_target_node`

Sets the target node that the end bone is trying to reach.
