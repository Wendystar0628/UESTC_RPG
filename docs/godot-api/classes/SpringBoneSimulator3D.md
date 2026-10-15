<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SpringBoneSimulator3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModifier3D`](SkeletonModifier3D.md) can be used to wiggle hair, cloth, and tails. This modifier behaves differently from [`PhysicalBoneSimulator3D`](PhysicalBoneSimulator3D.md) as it attempts to return the original pose after modification.

If you setup `SetRootBone()` and `SetEndBone()`, it is treated as one bone chain. Note that it does not support a branched chain like Y-shaped chains.

When a bone chain is created, an array is generated from the bones that exist in between and listed in the joint list.

Several properties can be applied to each joint, such as `SetJointStiffness()`, `SetJointDrag()`, and `SetJointGravity()`.

For simplicity, you can set values to all joints at the same time by using a [`Curve`](Curve.md). If you want to specify detailed values individually, set `SetIndividualConfig()` to `true`.

For physical simulation, `SpringBoneSimulator3D` can have children as self-standing collisions that are not related to [`PhysicsServer3D`](PhysicsServer3D.md), see also [`SpringBoneCollision3D`](SpringBoneCollision3D.md).

**Warning:** A scaled `SpringBoneSimulator3D` will likely not behave as expected. Make sure that the parent [`Skeleton3D`](Skeleton3D.md) and its bones are not scaled.

**Note:** Most methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/root_bone_name`).

## 构造函数

### SpringBoneSimulator3D

```csharp
public SpringBoneSimulator3D()
```

## 属性

### ExternalForce

```csharp
public Vector3 ExternalForce { get; set; }
```

引擎名：`external_force`

The constant force that always affected bones. It is equal to the result when the parent [`Skeleton3D`](Skeleton3D.md) moves at this speed in the opposite direction.

This is useful for effects such as wind and anti-gravity.

### MutableBoneAxes

```csharp
public bool MutableBoneAxes { get; set; }
```

引擎名：`mutable_bone_axes`

If `true`, the solver retrieves the bone axis from the bone pose every frame.

If `false`, the solver retrieves the bone axis from the bone rest and caches it, which increases performance slightly, but position changes in the bone pose made before processing this `SpringBoneSimulator3D` are ignored.

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings.

## 方法

### AreAllChildCollisionsEnabled

```csharp
public bool AreAllChildCollisionsEnabled(int index)
```

引擎名：`are_all_child_collisions_enabled`

Returns `true` if all child [`SpringBoneCollision3D`](SpringBoneCollision3D.md)s are contained in the collision list at `index` in the settings.

### AreBoneAxesMutable

```csharp
public bool AreBoneAxesMutable()
```

引擎名：`are_bone_axes_mutable`

### ClearCollisions

```csharp
public void ClearCollisions(int index)
```

引擎名：`clear_collisions`

Clears all collisions from the collision list at `index` in the settings when `AreAllChildCollisionsEnabled()` is `false`.

### ClearExcludeCollisions

```csharp
public void ClearExcludeCollisions(int index)
```

引擎名：`clear_exclude_collisions`

Clears all exclude collisions from the collision list at `index` in the settings when `AreAllChildCollisionsEnabled()` is `true`.

### ClearSettings

```csharp
public void ClearSettings()
```

引擎名：`clear_settings`

Clears all settings.

### GetCenterBone

```csharp
public int GetCenterBone(int index)
```

引擎名：`get_center_bone`

Returns the center bone index of the bone chain.

### GetCenterBoneName

```csharp
public string GetCenterBoneName(int index)
```

引擎名：`get_center_bone_name`

Returns the center bone name of the bone chain.

### GetCenterFrom

```csharp
public SpringBoneSimulator3D.CenterFrom GetCenterFrom(int index)
```

引擎名：`get_center_from`

Returns what the center originates from in the bone chain.

### GetCenterNode

```csharp
public NodePath GetCenterNode(int index)
```

引擎名：`get_center_node`

Returns the center node path of the bone chain.

### GetCollisionCount

```csharp
public int GetCollisionCount(int index)
```

引擎名：`get_collision_count`

Returns the collision count of the bone chain's collision list when `AreAllChildCollisionsEnabled()` is `false`.

### GetCollisionPath

```csharp
public NodePath GetCollisionPath(int index, int collision)
```

引擎名：`get_collision_path`

Returns the node path of the [`SpringBoneCollision3D`](SpringBoneCollision3D.md) at `collision` in the bone chain's collision list when `AreAllChildCollisionsEnabled()` is `false`.

### GetDrag

```csharp
public float GetDrag(int index)
```

引擎名：`get_drag`

Returns the drag force damping curve of the bone chain.

### GetDragDampingCurve

```csharp
public Curve GetDragDampingCurve(int index)
```

引擎名：`get_drag_damping_curve`

Returns the drag force damping curve of the bone chain.

### GetEndBone

```csharp
public int GetEndBone(int index)
```

引擎名：`get_end_bone`

Returns the end bone index of the bone chain.

### GetEndBoneDirection

```csharp
public SkeletonModifier3D.BoneDirection GetEndBoneDirection(int index)
```

引擎名：`get_end_bone_direction`

Returns the tail direction of the end bone of the bone chain when `IsEndBoneExtended()` is `true`.

### GetEndBoneLength

```csharp
public float GetEndBoneLength(int index)
```

引擎名：`get_end_bone_length`

Returns the end bone tail length of the bone chain when `IsEndBoneExtended()` is `true`.

### GetEndBoneName

```csharp
public string GetEndBoneName(int index)
```

引擎名：`get_end_bone_name`

Returns the end bone name of the bone chain.

### GetExcludeCollisionCount

```csharp
public int GetExcludeCollisionCount(int index)
```

引擎名：`get_exclude_collision_count`

Returns the exclude collision count of the bone chain's exclude collision list when `AreAllChildCollisionsEnabled()` is `true`.

### GetExcludeCollisionPath

```csharp
public NodePath GetExcludeCollisionPath(int index, int collision)
```

引擎名：`get_exclude_collision_path`

Returns the node path of the [`SpringBoneCollision3D`](SpringBoneCollision3D.md) at `collision` in the bone chain's exclude collision list when `AreAllChildCollisionsEnabled()` is `true`.

### GetExternalForce

```csharp
public Vector3 GetExternalForce()
```

引擎名：`get_external_force`

### GetGravity

```csharp
public float GetGravity(int index)
```

引擎名：`get_gravity`

Returns the gravity amount of the bone chain.

### GetGravityDampingCurve

```csharp
public Curve GetGravityDampingCurve(int index)
```

引擎名：`get_gravity_damping_curve`

Returns the gravity amount damping curve of the bone chain.

### GetGravityDirection

```csharp
public Vector3 GetGravityDirection(int index)
```

引擎名：`get_gravity_direction`

Returns the gravity direction of the bone chain.

### GetJointBone

```csharp
public int GetJointBone(int index, int joint)
```

引擎名：`get_joint_bone`

Returns the bone index at `joint` in the bone chain's joint list.

### GetJointBoneName

```csharp
public string GetJointBoneName(int index, int joint)
```

引擎名：`get_joint_bone_name`

Returns the bone name at `joint` in the bone chain's joint list.

### GetJointCount

```csharp
public int GetJointCount(int index)
```

引擎名：`get_joint_count`

Returns the joint count of the bone chain's joint list.

### GetJointDrag

```csharp
public float GetJointDrag(int index, int joint)
```

引擎名：`get_joint_drag`

Returns the drag force at `joint` in the bone chain's joint list.

### GetJointGravity

```csharp
public float GetJointGravity(int index, int joint)
```

引擎名：`get_joint_gravity`

Returns the gravity amount at `joint` in the bone chain's joint list.

### GetJointGravityDirection

```csharp
public Vector3 GetJointGravityDirection(int index, int joint)
```

引擎名：`get_joint_gravity_direction`

Returns the gravity direction at `joint` in the bone chain's joint list.

### GetJointRadius

```csharp
public float GetJointRadius(int index, int joint)
```

引擎名：`get_joint_radius`

Returns the radius at `joint` in the bone chain's joint list.

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

### GetJointStiffness

```csharp
public float GetJointStiffness(int index, int joint)
```

引擎名：`get_joint_stiffness`

Returns the stiffness force at `joint` in the bone chain's joint list.

### GetRadius

```csharp
public float GetRadius(int index)
```

引擎名：`get_radius`

Returns the joint radius of the bone chain.

### GetRadiusDampingCurve

```csharp
public Curve GetRadiusDampingCurve(int index)
```

引擎名：`get_radius_damping_curve`

Returns the joint radius damping curve of the bone chain.

### GetRootBone

```csharp
public int GetRootBone(int index)
```

引擎名：`get_root_bone`

Returns the root bone index of the bone chain.

### GetRootBoneName

```csharp
public string GetRootBoneName(int index)
```

引擎名：`get_root_bone_name`

Returns the root bone name of the bone chain.

### GetRotationAxis

```csharp
public SkeletonModifier3D.RotationAxis GetRotationAxis(int index)
```

引擎名：`get_rotation_axis`

Returns the rotation axis of the bone chain.

### GetRotationAxisVector

```csharp
public Vector3 GetRotationAxisVector(int index)
```

引擎名：`get_rotation_axis_vector`

Returns the rotation axis vector of the bone chain. This vector represents the axis around which the bone chain can rotate. It is determined based on the rotation axis set for the bone chain.

If `GetRotationAxis()` is [`SkeletonModifier3D.RotationAxis.All`](SkeletonModifier3D.md), this method returns `Vector3(0, 0, 0)`.

### GetSettingCount

```csharp
public int GetSettingCount()
```

引擎名：`get_setting_count`

### GetStiffness

```csharp
public float GetStiffness(int index)
```

引擎名：`get_stiffness`

Returns the stiffness force of the bone chain.

### GetStiffnessDampingCurve

```csharp
public Curve GetStiffnessDampingCurve(int index)
```

引擎名：`get_stiffness_damping_curve`

Returns the stiffness force damping curve of the bone chain.

### IsConfigIndividual

```csharp
public bool IsConfigIndividual(int index)
```

引擎名：`is_config_individual`

Returns `true` if the config can be edited individually for each joint.

### IsEndBoneExtended

```csharp
public bool IsEndBoneExtended(int index)
```

引擎名：`is_end_bone_extended`

Returns `true` if the end bone is extended to have a tail.

### Reset

```csharp
public void Reset()
```

引擎名：`reset`

Resets a simulating state with respect to the current bone pose.

It is useful to prevent the simulation result getting violent. For example, calling this immediately after a call to [`AnimationPlayer.Play`](AnimationPlayer.md) without a fading, or within the previous [`SkeletonModifier3D.ModificationProcessed`](SkeletonModifier3D.md) signal if it's condition changes significantly.

### SetCenterBone

```csharp
public void SetCenterBone(int index, int bone)
```

引擎名：`set_center_bone`

Sets the center bone index of the bone chain.

### SetCenterBoneName

```csharp
public void SetCenterBoneName(int index, string boneName)
```

引擎名：`set_center_bone_name`

Sets the center bone name of the bone chain.

### SetCenterFrom

```csharp
public void SetCenterFrom(int index, SpringBoneSimulator3D.CenterFrom centerFrom)
```

引擎名：`set_center_from`

Sets what the center originates from in the bone chain.

Bone movement is calculated based on the difference in relative distance between center and bone in the previous and next frames.

For example, if the parent [`Skeleton3D`](Skeleton3D.md) is used as the center, the bones are considered to have not moved if the [`Skeleton3D`](Skeleton3D.md) moves in the world.

In this case, only a change in the bone pose is considered to be a bone movement.

### SetCenterNode

```csharp
public void SetCenterNode(int index, NodePath nodePath)
```

引擎名：`set_center_node`

Sets the center node path of the bone chain.

### SetCollisionCount

```csharp
public void SetCollisionCount(int index, int count)
```

引擎名：`set_collision_count`

Sets the number of collisions in the collision list at `index` in the settings when `AreAllChildCollisionsEnabled()` is `false`.

### SetCollisionPath

```csharp
public void SetCollisionPath(int index, int collision, NodePath nodePath)
```

引擎名：`set_collision_path`

Sets the node path of the [`SpringBoneCollision3D`](SpringBoneCollision3D.md) at `collision` in the bone chain's collision list when `AreAllChildCollisionsEnabled()` is `false`.

### SetDrag

```csharp
public void SetDrag(int index, float drag)
```

引擎名：`set_drag`

Sets the drag force of the bone chain. The greater the value, the more suppressed the wiggling.

The value is scaled by `SetDragDampingCurve()` and cached in each joint setting in the joint list.

### SetDragDampingCurve

```csharp
public void SetDragDampingCurve(int index, Curve curve)
```

引擎名：`set_drag_damping_curve`

Sets the drag force damping curve of the bone chain.

### SetEnableAllChildCollisions

```csharp
public void SetEnableAllChildCollisions(int index, bool enabled)
```

引擎名：`set_enable_all_child_collisions`

If `enabled` is `true`, all child [`SpringBoneCollision3D`](SpringBoneCollision3D.md)s are colliding and `SetExcludeCollisionPath()` is enabled as an exclusion list at `index` in the settings.

If `enabled` is `false`, you need to manually register all valid collisions with `SetCollisionPath()`.

### SetEndBone

```csharp
public void SetEndBone(int index, int bone)
```

引擎名：`set_end_bone`

Sets the end bone index of the bone chain.

### SetEndBoneDirection

```csharp
public void SetEndBoneDirection(int index, SkeletonModifier3D.BoneDirection boneDirection)
```

引擎名：`set_end_bone_direction`

Sets the end bone tail direction of the bone chain when `IsEndBoneExtended()` is `true`.

### SetEndBoneDirection

```csharp
public void SetEndBoneDirection(int index, int boneDirection)
```

引擎名：`set_end_bone_direction`

Sets the end bone tail direction of the bone chain when `IsEndBoneExtended()` is `true`.

### SetEndBoneLength

```csharp
public void SetEndBoneLength(int index, float length)
```

引擎名：`set_end_bone_length`

Sets the end bone tail length of the bone chain when `IsEndBoneExtended()` is `true`.

### SetEndBoneName

```csharp
public void SetEndBoneName(int index, string boneName)
```

引擎名：`set_end_bone_name`

Sets the end bone name of the bone chain.

**Note:** End bone must be the root bone or a child of the root bone. If they are the same, the tail must be extended by `SetExtendEndBone()` to jiggle the bone.

### SetExcludeCollisionCount

```csharp
public void SetExcludeCollisionCount(int index, int count)
```

引擎名：`set_exclude_collision_count`

Sets the number of exclude collisions in the exclude collision list at `index` in the settings when `AreAllChildCollisionsEnabled()` is `true`.

### SetExcludeCollisionPath

```csharp
public void SetExcludeCollisionPath(int index, int collision, NodePath nodePath)
```

引擎名：`set_exclude_collision_path`

Sets the node path of the [`SpringBoneCollision3D`](SpringBoneCollision3D.md) at `collision` in the bone chain's exclude collision list when `AreAllChildCollisionsEnabled()` is `true`.

### SetExtendEndBone

```csharp
public void SetExtendEndBone(int index, bool enabled)
```

引擎名：`set_extend_end_bone`

If `enabled` is `true`, the end bone is extended to have a tail.

The extended tail config is allocated to the last element in the joint list. In other words, if you set `enabled` to `false`, the config of the last element in the joint list has no effect in the simulated result.

### SetExternalForce

```csharp
public void SetExternalForce(Vector3 force)
```

引擎名：`set_external_force`

### SetGravity

```csharp
public void SetGravity(int index, float gravity)
```

引擎名：`set_gravity`

Sets the gravity amount of the bone chain. This value is not an acceleration, but a constant velocity of movement in `SetGravityDirection()`.

If `gravity` is not `0`, the modified pose will not return to the original pose since it is always affected by gravity.

The value is scaled by `SetGravityDampingCurve()` and cached in each joint setting in the joint list.

### SetGravityDampingCurve

```csharp
public void SetGravityDampingCurve(int index, Curve curve)
```

引擎名：`set_gravity_damping_curve`

Sets the gravity amount damping curve of the bone chain.

### SetGravityDirection

```csharp
public void SetGravityDirection(int index, Vector3 gravityDirection)
```

引擎名：`set_gravity_direction`

Sets the gravity direction of the bone chain. This value is internally normalized and then multiplied by `SetGravity()`.

The value is cached in each joint setting in the joint list.

### SetIndividualConfig

```csharp
public void SetIndividualConfig(int index, bool enabled)
```

引擎名：`set_individual_config`

If `enabled` is `true`, the config can be edited individually for each joint.

### SetJointDrag

```csharp
public void SetJointDrag(int index, int joint, float drag)
```

引擎名：`set_joint_drag`

Sets the drag force at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

### SetJointGravity

```csharp
public void SetJointGravity(int index, int joint, float gravity)
```

引擎名：`set_joint_gravity`

Sets the gravity amount at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

### SetJointGravityDirection

```csharp
public void SetJointGravityDirection(int index, int joint, Vector3 gravityDirection)
```

引擎名：`set_joint_gravity_direction`

Sets the gravity direction at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

### SetJointRadius

```csharp
public void SetJointRadius(int index, int joint, float radius)
```

引擎名：`set_joint_radius`

Sets the joint radius at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

### SetJointRotationAxis

```csharp
public void SetJointRotationAxis(int index, int joint, SkeletonModifier3D.RotationAxis axis)
```

引擎名：`set_joint_rotation_axis`

Sets the rotation axis at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

The axes are based on the reference pose's space, if `axis` is [`SkeletonModifier3D.RotationAxis.Custom`](SkeletonModifier3D.md), you can specify any axis.

In here, the reference pose is the bone pose immediately before the simulation.

**Note:** The rotation axis and the forward vector shouldn't be colinear to avoid unintended rotation since `SpringBoneSimulator3D` does not factor in twisting forces.

### SetJointRotationAxis

```csharp
public void SetJointRotationAxis(int index, int joint, int axis)
```

引擎名：`set_joint_rotation_axis`

Sets the rotation axis at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

The axes are based on the reference pose's space, if `axis` is [`SkeletonModifier3D.RotationAxis.Custom`](SkeletonModifier3D.md), you can specify any axis.

In here, the reference pose is the bone pose immediately before the simulation.

**Note:** The rotation axis and the forward vector shouldn't be colinear to avoid unintended rotation since `SpringBoneSimulator3D` does not factor in twisting forces.

### SetJointRotationAxisVector

```csharp
public void SetJointRotationAxisVector(int index, int joint, Vector3 vector)
```

引擎名：`set_joint_rotation_axis_vector`

Sets the rotation axis vector for the specified joint in the bone chain.

This vector is normalized by an internal process and represents the axis around which the bone chain can rotate.

If the vector length is `0`, it is considered synonymous with [`SkeletonModifier3D.RotationAxis.All`](SkeletonModifier3D.md).

### SetJointStiffness

```csharp
public void SetJointStiffness(int index, int joint, float stiffness)
```

引擎名：`set_joint_stiffness`

Sets the stiffness force at `joint` in the bone chain's joint list when `IsConfigIndividual()` is `true`.

### SetMutableBoneAxes

```csharp
public void SetMutableBoneAxes(bool enabled)
```

引擎名：`set_mutable_bone_axes`

### SetRadius

```csharp
public void SetRadius(int index, float radius)
```

引擎名：`set_radius`

Sets the joint radius of the bone chain. It is used to move and slide with the [`SpringBoneCollision3D`](SpringBoneCollision3D.md) in the collision list.

The value is scaled by `SetRadiusDampingCurve()` and cached in each joint setting in the joint list.

### SetRadiusDampingCurve

```csharp
public void SetRadiusDampingCurve(int index, Curve curve)
```

引擎名：`set_radius_damping_curve`

Sets the joint radius damping curve of the bone chain.

### SetRootBone

```csharp
public void SetRootBone(int index, int bone)
```

引擎名：`set_root_bone`

Sets the root bone index of the bone chain.

### SetRootBoneName

```csharp
public void SetRootBoneName(int index, string boneName)
```

引擎名：`set_root_bone_name`

Sets the root bone name of the bone chain.

### SetRotationAxis

```csharp
public void SetRotationAxis(int index, SkeletonModifier3D.RotationAxis axis)
```

引擎名：`set_rotation_axis`

Sets the rotation axis of the bone chain. If set to a specific axis, it acts like a hinge joint. The value is cached in each joint setting in the joint list.

The axes are based on the reference pose's space, if `axis` is [`SkeletonModifier3D.RotationAxis.Custom`](SkeletonModifier3D.md), you can specify any axis.

In here, the reference pose is the bone pose immediately before the simulation.

**Note:** The rotation axis vector and the forward vector shouldn't be colinear to avoid unintended rotation since `SpringBoneSimulator3D` does not factor in twisting forces.

### SetRotationAxis

```csharp
public void SetRotationAxis(int index, int axis)
```

引擎名：`set_rotation_axis`

Sets the rotation axis of the bone chain. If set to a specific axis, it acts like a hinge joint. The value is cached in each joint setting in the joint list.

The axes are based on the reference pose's space, if `axis` is [`SkeletonModifier3D.RotationAxis.Custom`](SkeletonModifier3D.md), you can specify any axis.

In here, the reference pose is the bone pose immediately before the simulation.

**Note:** The rotation axis vector and the forward vector shouldn't be colinear to avoid unintended rotation since `SpringBoneSimulator3D` does not factor in twisting forces.

### SetRotationAxisVector

```csharp
public void SetRotationAxisVector(int index, Vector3 vector)
```

引擎名：`set_rotation_axis_vector`

Sets the rotation axis vector of the bone chain. The value is cached in each joint setting in the joint list.

This vector is normalized by an internal process and represents the axis around which the bone chain can rotate.

If the vector length is `0`, it is considered synonymous with [`SkeletonModifier3D.RotationAxis.All`](SkeletonModifier3D.md).

### SetSettingCount

```csharp
public void SetSettingCount(int count)
```

引擎名：`set_setting_count`

### SetStiffness

```csharp
public void SetStiffness(int index, float stiffness)
```

引擎名：`set_stiffness`

Sets the stiffness force of the bone chain. The greater the value, the faster it recovers to its initial pose.

If `stiffness` is `0`, the modified pose will not return to the original pose.

The value is scaled by `SetStiffnessDampingCurve()` and cached in each joint setting in the joint list.

### SetStiffnessDampingCurve

```csharp
public void SetStiffnessDampingCurve(int index, Curve curve)
```

引擎名：`set_stiffness_damping_curve`

Sets the stiffness force damping curve of the bone chain.

## CenterFrom

命名空间 `Godot`；enum

### 枚举值

- `WorldOrigin = 0`：The world origin is defined as center.
- `Node = 1`：The [`Node3D`](Node3D.md) specified by [`SpringBoneSimulator3D.SetCenterNode`](SpringBoneSimulator3D.md) is defined as center.
- `Bone = 2`：The bone pose origin of the parent [`Skeleton3D`](Skeleton3D.md) specified by [`SpringBoneSimulator3D.SetCenterBone`](SpringBoneSimulator3D.md) is defined as center.
