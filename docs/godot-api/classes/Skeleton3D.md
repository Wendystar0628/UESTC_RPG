<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Skeleton3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Skeleton3D` provides an interface for managing a hierarchy of bones, including pose, rest and animation (see [`Animation`](Animation.md)). It can also use ragdoll physics.

The overall transform of a bone with respect to the skeleton is determined by bone pose. Bone rest defines the initial transform of the bone pose.

Note that "global pose" below refers to the overall transform of the bone with respect to skeleton, so it is not the actual global/world transform of the bone.

## 构造函数

### Skeleton3D

```csharp
public Skeleton3D()
```

## 属性

### AnimatePhysicalBones

```csharp
public bool AnimatePhysicalBones { get; set; }
```

引擎名：`animate_physical_bones`

If you follow the recommended workflow and explicitly have [`PhysicalBoneSimulator3D`](PhysicalBoneSimulator3D.md) as a child of `Skeleton3D`, you can control whether it is affected by raycasting without running `PhysicalBonesStartSimulation()`, by its [`SkeletonModifier3D.Active`](SkeletonModifier3D.md).

However, for old (deprecated) configurations, `Skeleton3D` has an internal virtual [`PhysicalBoneSimulator3D`](PhysicalBoneSimulator3D.md) for compatibility. This property controls the internal virtual [`PhysicalBoneSimulator3D`](PhysicalBoneSimulator3D.md)'s [`SkeletonModifier3D.Active`](SkeletonModifier3D.md).

### ModifierCallbackModeProcess

```csharp
public Skeleton3D.ModifierCallbackModeProcessEnum ModifierCallbackModeProcess { get; set; }
```

引擎名：`modifier_callback_mode_process`

Sets the processing timing for the Modifier.

### MotionScale

```csharp
public float MotionScale { get; set; }
```

引擎名：`motion_scale`

Multiplies the 3D position track animation.

**Note:** Unless this value is `1.0`, the key value in animation will not match the actual position value.

### ShowRestOnly

```csharp
public bool ShowRestOnly { get; set; }
```

引擎名：`show_rest_only`

If `true`, forces the bones in their default rest pose, regardless of their values. In the editor, this also prevents the bones from being edited.

## 方法

### AddBone

```csharp
public int AddBone(string name)
```

引擎名：`add_bone`

Adds a new bone with the given name. Returns the new bone's index, or `-1` if this method fails.

**Note:** Bone names should be unique, non empty, and cannot include the `:` and `/` characters.

### Advance

```csharp
public void Advance(double delta)
```

引擎名：`advance`

Manually advance the child [`SkeletonModifier3D`](SkeletonModifier3D.md)s by the specified time (in seconds).

**Note:** The `delta` is temporarily accumulated in the `Skeleton3D`, and the deferred process uses the accumulated value to process the modification.

### ClearBones

```csharp
public void ClearBones()
```

引擎名：`clear_bones`

Clear all the bones in this skeleton.

### ClearBonesGlobalPoseOverride

```csharp
public void ClearBonesGlobalPoseOverride()
```

引擎名：`clear_bones_global_pose_override`

Removes the global pose override on all bones in the skeleton.

### CreateSkinFromRestTransforms

```csharp
public Skin CreateSkinFromRestTransforms()
```

引擎名：`create_skin_from_rest_transforms`

### FindBone

```csharp
public int FindBone(string name)
```

引擎名：`find_bone`

Returns the bone index that matches `name` as its name. Returns `-1` if no bone with this name exists.

### ForceUpdateAllBoneTransforms

```csharp
public void ForceUpdateAllBoneTransforms()
```

引擎名：`force_update_all_bone_transforms`

Force updates the bone transforms/poses for all bones in the skeleton.

### ForceUpdateBoneChildTransform

```csharp
public void ForceUpdateBoneChildTransform(int boneIdx)
```

引擎名：`force_update_bone_child_transform`

Force updates the bone transform for the bone at `boneIdx` and all of its children.

### GetAnimatePhysicalBones

```csharp
public bool GetAnimatePhysicalBones()
```

引擎名：`get_animate_physical_bones`

### GetBoneChildren

```csharp
public int[] GetBoneChildren(int boneIdx)
```

引擎名：`get_bone_children`

Returns an array containing the bone indexes of all the child node of the passed in bone, `boneIdx`.

### GetBoneCount

```csharp
public int GetBoneCount()
```

引擎名：`get_bone_count`

Returns the number of bones in the skeleton.

### GetBoneGlobalPose

```csharp
public Transform3D GetBoneGlobalPose(int boneIdx)
```

引擎名：`get_bone_global_pose`

Returns the overall transform of the specified bone, with respect to the skeleton. Being relative to the skeleton frame, this is not the actual "global" transform of the bone.

**Note:** This is the global pose you set to the skeleton in the process, the final global pose can get overridden by modifiers in the deferred process, if you want to access the final global pose, use [`SkeletonModifier3D.ModificationProcessed`](SkeletonModifier3D.md).

### GetBoneGlobalPoseNoOverride

```csharp
public Transform3D GetBoneGlobalPoseNoOverride(int boneIdx)
```

引擎名：`get_bone_global_pose_no_override`

Returns the overall transform of the specified bone, with respect to the skeleton, but without any global pose overrides. Being relative to the skeleton frame, this is not the actual "global" transform of the bone.

### GetBoneGlobalPoseOverride

```csharp
public Transform3D GetBoneGlobalPoseOverride(int boneIdx)
```

引擎名：`get_bone_global_pose_override`

Returns the global pose override transform for `boneIdx`.

### GetBoneGlobalRest

```csharp
public Transform3D GetBoneGlobalRest(int boneIdx)
```

引擎名：`get_bone_global_rest`

Returns the global rest transform for `boneIdx`.

### GetBoneMeta

```csharp
public Variant GetBoneMeta(int boneIdx, StringName key)
```

引擎名：`get_bone_meta`

Returns the metadata with the given `key` for the bone at index `boneIdx`.

### GetBoneMetaList

```csharp
public Array<StringName> GetBoneMetaList(int boneIdx)
```

引擎名：`get_bone_meta_list`

Returns the list of all metadata keys for the bone at index `boneIdx`.

### GetBoneName

```csharp
public string GetBoneName(int boneIdx)
```

引擎名：`get_bone_name`

Returns the name of the bone at index `boneIdx`.

### GetBoneParent

```csharp
public int GetBoneParent(int boneIdx)
```

引擎名：`get_bone_parent`

Returns the bone index which is the parent of the bone at `boneIdx`. If -1, then bone has no parent.

**Note:** The parent bone returned will always be less than `boneIdx`.

### GetBonePose

```csharp
public Transform3D GetBonePose(int boneIdx)
```

引擎名：`get_bone_pose`

Returns the pose transform of the specified bone.

**Note:** This is the pose you set to the skeleton in the process, the final pose can get overridden by modifiers in the deferred process, if you want to access the final pose, use [`SkeletonModifier3D.ModificationProcessed`](SkeletonModifier3D.md).

### GetBonePosePosition

```csharp
public Vector3 GetBonePosePosition(int boneIdx)
```

引擎名：`get_bone_pose_position`

Returns the pose position of the bone at `boneIdx`. The returned [`Vector3`](Vector3.md) is in the local coordinate space of the `Skeleton3D` node.

### GetBonePoseRotation

```csharp
public Quaternion GetBonePoseRotation(int boneIdx)
```

引擎名：`get_bone_pose_rotation`

Returns the pose rotation of the bone at `boneIdx`. The returned [`Quaternion`](Quaternion.md) is local to the bone with respect to the rotation of any parent bones.

### GetBonePoseScale

```csharp
public Vector3 GetBonePoseScale(int boneIdx)
```

引擎名：`get_bone_pose_scale`

Returns the pose scale of the bone at `boneIdx`.

### GetBoneRest

```csharp
public Transform3D GetBoneRest(int boneIdx)
```

引擎名：`get_bone_rest`

Returns the rest transform for a bone `boneIdx`.

### GetConcatenatedBoneNames

```csharp
public StringName GetConcatenatedBoneNames()
```

引擎名：`get_concatenated_bone_names`

Returns all bone names concatenated with commas (`,`) as a single [`StringName`](StringName.md).

It is useful to set it as a hint for the enum property.

### GetModifierCallbackModeProcess

```csharp
public Skeleton3D.ModifierCallbackModeProcessEnum GetModifierCallbackModeProcess()
```

引擎名：`get_modifier_callback_mode_process`

### GetMotionScale

```csharp
public float GetMotionScale()
```

引擎名：`get_motion_scale`

### GetParentlessBones

```csharp
public int[] GetParentlessBones()
```

引擎名：`get_parentless_bones`

Returns an array with all of the bones that are parentless. Another way to look at this is that it returns the indexes of all the bones that are not dependent or modified by other bones in the Skeleton.

### GetVersion

```csharp
public ulong GetVersion()
```

引擎名：`get_version`

Returns the number of times the bone hierarchy has changed within this skeleton, including renames.

The Skeleton version is not serialized: only use within a single instance of Skeleton3D.

Use for invalidating caches in IK solvers and other nodes which process bones.

### HasBoneMeta

```csharp
public bool HasBoneMeta(int boneIdx, StringName key)
```

引擎名：`has_bone_meta`

Returns `true` if the bone at index `boneIdx` has metadata with the given `key`.

### IsBoneEnabled

```csharp
public bool IsBoneEnabled(int boneIdx)
```

引擎名：`is_bone_enabled`

Returns whether the bone pose for the bone at `boneIdx` is enabled.

### IsShowRestOnly

```csharp
public bool IsShowRestOnly()
```

引擎名：`is_show_rest_only`

### LocalizeRests

```csharp
public void LocalizeRests()
```

引擎名：`localize_rests`

Returns all bones in the skeleton to their rest poses.

### PhysicalBonesAddCollisionException

```csharp
public void PhysicalBonesAddCollisionException(Rid exception)
```

引擎名：`physical_bones_add_collision_exception`

Adds a collision exception to the physical bone.

Works just like the [`RigidBody3D`](RigidBody3D.md) node.

### PhysicalBonesRemoveCollisionException

```csharp
public void PhysicalBonesRemoveCollisionException(Rid exception)
```

引擎名：`physical_bones_remove_collision_exception`

Removes a collision exception to the physical bone.

Works just like the [`RigidBody3D`](RigidBody3D.md) node.

### PhysicalBonesStartSimulation

```csharp
public void PhysicalBonesStartSimulation(Array<StringName> bones = null)
```

引擎名：`physical_bones_start_simulation`

Tells the [`PhysicalBone3D`](PhysicalBone3D.md) nodes in the Skeleton to start simulating and reacting to the physics world.

Optionally, a list of bone names can be passed-in, allowing only the passed-in bones to be simulated.

### PhysicalBonesStopSimulation

```csharp
public void PhysicalBonesStopSimulation()
```

引擎名：`physical_bones_stop_simulation`

Tells the [`PhysicalBone3D`](PhysicalBone3D.md) nodes in the Skeleton to stop simulating.

### RegisterSkin

```csharp
public SkinReference RegisterSkin(Skin skin)
```

引擎名：`register_skin`

Binds the given Skin to the Skeleton.

### ResetBonePose

```csharp
public void ResetBonePose(int boneIdx)
```

引擎名：`reset_bone_pose`

Sets the bone pose to rest for `boneIdx`.

### ResetBonePoses

```csharp
public void ResetBonePoses()
```

引擎名：`reset_bone_poses`

Sets all bone poses to rests.

### SetAnimatePhysicalBones

```csharp
public void SetAnimatePhysicalBones(bool enabled)
```

引擎名：`set_animate_physical_bones`

### SetBoneEnabled

```csharp
public void SetBoneEnabled(int boneIdx, bool enabled = true)
```

引擎名：`set_bone_enabled`

Disables the pose for the bone at `boneIdx` if `false`, enables the bone pose if `true`.

### SetBoneGlobalPose

```csharp
public void SetBoneGlobalPose(int boneIdx, Transform3D pose)
```

引擎名：`set_bone_global_pose`

Sets the global pose transform, `pose`, for the bone at `boneIdx`.

**Note:** If other bone poses have been changed, this method executes a dirty poses recalculation and will cause performance to deteriorate. If you know that multiple global poses will be applied, consider using `SetBonePose()` with precalculation.

### SetBoneGlobalPoseOverride

```csharp
public void SetBoneGlobalPoseOverride(int boneIdx, Transform3D pose, float amount, bool persistent = false)
```

引擎名：`set_bone_global_pose_override`

Sets the global pose transform, `pose`, for the bone at `boneIdx`.

`amount` is the interpolation strength that will be used when applying the pose, and `persistent` determines if the applied pose will remain.

**Note:** The pose transform needs to be a global pose! To convert a world transform from a [`Node3D`](Node3D.md) to a global bone pose, multiply the `Transform3D.AffineInverse` of the node's [`Node3D.GlobalTransform`](Node3D.md) by the desired world transform.

### SetBoneMeta

```csharp
public void SetBoneMeta(int boneIdx, StringName key, Variant value)
```

引擎名：`set_bone_meta`

Sets the metadata with the given `key` to `value` for the bone at index `boneIdx`.

### SetBoneName

```csharp
public void SetBoneName(int boneIdx, string name)
```

引擎名：`set_bone_name`

Sets the bone name, `name`, for the bone at `boneIdx`.

### SetBoneParent

```csharp
public void SetBoneParent(int boneIdx, int parentIdx)
```

引擎名：`set_bone_parent`

Sets the bone index `parentIdx` as the parent of the bone at `boneIdx`. If -1, then bone has no parent.

**Note:** `parentIdx` must be less than `boneIdx`.

### SetBonePose

```csharp
public void SetBonePose(int boneIdx, Transform3D pose)
```

引擎名：`set_bone_pose`

Sets the pose transform, `pose`, for the bone at `boneIdx`.

### SetBonePosePosition

```csharp
public void SetBonePosePosition(int boneIdx, Vector3 position)
```

引擎名：`set_bone_pose_position`

Sets the pose position of the bone at `boneIdx` to `position`. `position` is a [`Vector3`](Vector3.md) describing a position local to the `Skeleton3D` node.

### SetBonePoseRotation

```csharp
public void SetBonePoseRotation(int boneIdx, Quaternion rotation)
```

引擎名：`set_bone_pose_rotation`

Sets the pose rotation of the bone at `boneIdx` to `rotation`. `rotation` is a [`Quaternion`](Quaternion.md) describing a rotation in the bone's local coordinate space with respect to the rotation of any parent bones.

### SetBonePoseScale

```csharp
public void SetBonePoseScale(int boneIdx, Vector3 scale)
```

引擎名：`set_bone_pose_scale`

Sets the pose scale of the bone at `boneIdx` to `scale`.

### SetBoneRest

```csharp
public void SetBoneRest(int boneIdx, Transform3D rest)
```

引擎名：`set_bone_rest`

Sets the rest transform for bone `boneIdx`.

### SetModifierCallbackModeProcess

```csharp
public void SetModifierCallbackModeProcess(Skeleton3D.ModifierCallbackModeProcessEnum mode)
```

引擎名：`set_modifier_callback_mode_process`

### SetMotionScale

```csharp
public void SetMotionScale(float motionScale)
```

引擎名：`set_motion_scale`

### SetShowRestOnly

```csharp
public void SetShowRestOnly(bool enabled)
```

引擎名：`set_show_rest_only`

### UnparentBoneAndRest

```csharp
public void UnparentBoneAndRest(int boneIdx)
```

引擎名：`unparent_bone_and_rest`

Unparents the bone at `boneIdx` and sets its rest position to that of its parent prior to being reset.

## 事件（信号）

### BoneEnabledChanged

```csharp
public event Skeleton3D.BoneEnabledChangedEventHandler BoneEnabledChanged
```

引擎名：`bone_enabled_changed`

Emitted when the bone at `boneIdx` is toggled with `SetBoneEnabled()`. Use `IsBoneEnabled()` to check the new value.

### BoneListChanged

```csharp
public event Action BoneListChanged
```

引擎名：`bone_list_changed`

Emitted when the list of bones changes, such as when calling `AddBone()`, `SetBoneParent()`, `UnparentBoneAndRest()`, or `ClearBones()`.

### PoseUpdated

```csharp
public event Action PoseUpdated
```

引擎名：`pose_updated`

Emitted when the pose is updated.

**Note:** During the update process, this signal is not fired, so modification by [`SkeletonModifier3D`](SkeletonModifier3D.md) is not detected.

### RestUpdated

```csharp
public event Action RestUpdated
```

引擎名：`rest_updated`

Emitted when the rest is updated.

### ShowRestOnlyChanged

```csharp
public event Action ShowRestOnlyChanged
```

引擎名：`show_rest_only_changed`

Emitted when the value of `ShowRestOnly` changes.

### SkeletonUpdated

```csharp
public event Action SkeletonUpdated
```

引擎名：`skeleton_updated`

Emitted when the final pose has been calculated will be applied to the skin in the update process.

This means that all [`SkeletonModifier3D`](SkeletonModifier3D.md) processing is complete. In order to detect the completion of the processing of each [`SkeletonModifier3D`](SkeletonModifier3D.md), use [`SkeletonModifier3D.ModificationProcessed`](SkeletonModifier3D.md).

## 常量

### NotificationUpdateSkeleton

```csharp
public const long NotificationUpdateSkeleton = 50
```

Notification received when this skeleton's pose needs to be updated. In that case, this is called only once per frame in a deferred process.

## BoneEnabledChangedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Skeleton3D.BoneEnabledChanged`](Skeleton3D.md) event of a [`Skeleton3D`](Skeleton3D.md) class.

### 签名

#### Invoke

```csharp
delegate void BoneEnabledChangedEventHandler(long boneIdx)
```

## ModifierCallbackModeProcessEnum

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`：Set a flag to process modification during physics frames (see [`Node.NotificationInternalPhysicsProcess`](Node.md)).
- `Idle = 1`：Set a flag to process modification during process frames (see [`Node.NotificationInternalProcess`](Node.md)).
- `Manual = 2`：Do not process modification.
