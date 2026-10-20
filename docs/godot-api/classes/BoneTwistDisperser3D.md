<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoneTwistDisperser3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This `BoneTwistDisperser3D` allows for smooth twist interpolation between multiple bones by dispersing the end bone's twist to the parents. This only changes the twist without changing the global position of each joint.

This is useful for smoothly twisting bones in combination with [`CopyTransformModifier3D`](CopyTransformModifier3D.md) and IK.

**Note:** If an extracted twist is greater than 180 degrees, flipping occurs. This is similar to [`ConvertTransformModifier3D`](ConvertTransformModifier3D.md).

**Note:** Most methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/root_bone_name`).

## 构造函数

### BoneTwistDisperser3D

```csharp
public BoneTwistDisperser3D()
```

## 属性

### MutableBoneAxes

```csharp
public bool MutableBoneAxes { get; set; }
```

引擎名：`mutable_bone_axes`

If `true`, the solver retrieves the bone axis from the bone pose every frame.

If `false`, the solver retrieves the bone axis from the bone rest and caches it.

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings.

## 方法

### AreBoneAxesMutable

```csharp
public bool AreBoneAxesMutable()
```

引擎名：`are_bone_axes_mutable`

### ClearSettings

```csharp
public void ClearSettings()
```

引擎名：`clear_settings`

Clears all settings.

### GetDampingCurve

```csharp
public Curve GetDampingCurve(int index)
```

引擎名：`get_damping_curve`

Returns the damping curve when `GetDisperseMode()` is [`BoneTwistDisperser3D.DisperseMode.Custom`](BoneTwistDisperser3D.md).

### GetDisperseMode

```csharp
public BoneTwistDisperser3D.DisperseMode GetDisperseMode(int index)
```

引擎名：`get_disperse_mode`

Returns whether to use automatic amount assignment or to allow manual assignment.

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

### GetEndBoneName

```csharp
public string GetEndBoneName(int index)
```

引擎名：`get_end_bone_name`

Returns the end bone name of the bone chain.

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

### GetJointTwistAmount

```csharp
public float GetJointTwistAmount(int index, int joint)
```

引擎名：`get_joint_twist_amount`

Returns the twist amount at `joint` in the bone chain's joint list when `GetDisperseMode()` is [`BoneTwistDisperser3D.DisperseMode.Custom`](BoneTwistDisperser3D.md).

### GetReferenceBone

```csharp
public int GetReferenceBone(int index)
```

引擎名：`get_reference_bone`

Returns the reference bone to extract twist of the setting at `index`.

This bone is either the end of the chain or its parent, depending on `IsEndBoneExtended()`.

### GetReferenceBoneName

```csharp
public string GetReferenceBoneName(int index)
```

引擎名：`get_reference_bone_name`

Returns the reference bone name to extract twist of the setting at `index`.

This bone is either the end of the chain or its parent, depending on `IsEndBoneExtended()`.

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

### GetSettingCount

```csharp
public int GetSettingCount()
```

引擎名：`get_setting_count`

### GetTwistFrom

```csharp
public Quaternion GetTwistFrom(int index)
```

引擎名：`get_twist_from`

Returns the rotation to an arbitrary state before twisting for the current bone pose to extract the twist when `IsTwistFromRest()` is `false`.

### GetWeightPosition

```csharp
public float GetWeightPosition(int index)
```

引擎名：`get_weight_position`

Returns the position at which to divide the segment between joints for weight assignment when `GetDisperseMode()` is [`BoneTwistDisperser3D.DisperseMode.Weighted`](BoneTwistDisperser3D.md).

### IsEndBoneExtended

```csharp
public bool IsEndBoneExtended(int index)
```

引擎名：`is_end_bone_extended`

Returns `true` if the end bone is extended to have a tail.

### IsTwistFromRest

```csharp
public bool IsTwistFromRest(int index)
```

引擎名：`is_twist_from_rest`

Returns `true` if extracting the twist amount from the difference between the bone rest and the current bone pose.

### SetDampingCurve

```csharp
public void SetDampingCurve(int index, Curve curve)
```

引擎名：`set_damping_curve`

Sets the damping curve when `GetDisperseMode()` is [`BoneTwistDisperser3D.DisperseMode.Custom`](BoneTwistDisperser3D.md).

### SetDisperseMode

```csharp
public void SetDisperseMode(int index, BoneTwistDisperser3D.DisperseMode disperseMode)
```

引擎名：`set_disperse_mode`

Sets whether to use automatic amount assignment or to allow manual assignment.

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

### SetEndBoneName

```csharp
public void SetEndBoneName(int index, string boneName)
```

引擎名：`set_end_bone_name`

Sets the end bone name of the bone chain.

**Note:** The end bone must be a child of the root bone.

### SetExtendEndBone

```csharp
public void SetExtendEndBone(int index, bool enabled)
```

引擎名：`set_extend_end_bone`

If `enabled` is `true`, the end bone is extended to have a tail.

If `enabled` is `false`, `GetReferenceBone()` becomes a parent of the end bone and it uses the vector to the end bone as a twist axis.

### SetJointTwistAmount

```csharp
public void SetJointTwistAmount(int index, int joint, float twistAmount)
```

引擎名：`set_joint_twist_amount`

Sets the twist amount at `joint` in the bone chain's joint list when `GetDisperseMode()` is [`BoneTwistDisperser3D.DisperseMode.Custom`](BoneTwistDisperser3D.md).

### SetMutableBoneAxes

```csharp
public void SetMutableBoneAxes(bool enabled)
```

引擎名：`set_mutable_bone_axes`

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

### SetSettingCount

```csharp
public void SetSettingCount(int count)
```

引擎名：`set_setting_count`

### SetTwistFrom

```csharp
public void SetTwistFrom(int index, Quaternion from)
```

引擎名：`set_twist_from`

Sets the rotation to an arbitrary state before twisting for the current bone pose to extract the twist when `IsTwistFromRest()` is `false`.

In other words, by calling `SetTwistFrom()` by [`SkeletonModifier3D.ModificationProcessed`](SkeletonModifier3D.md) of a specific [`SkeletonModifier3D`](SkeletonModifier3D.md), you can extract only the twists generated by modifiers processed after that but before this `BoneTwistDisperser3D`.

### SetTwistFromRest

```csharp
public void SetTwistFromRest(int index, bool enabled)
```

引擎名：`set_twist_from_rest`

If `enabled` is `true`, it extracts the twist amount from the difference between the bone rest and the current bone pose.

If `enabled` is `false`, it extracts the twist amount from the difference between `GetTwistFrom()` and the current bone pose. See also `SetTwistFrom()`.

### SetWeightPosition

```csharp
public void SetWeightPosition(int index, float weightPosition)
```

引擎名：`set_weight_position`

Sets the position at which to divide the segment between joints for weight assignment when `GetDisperseMode()` is [`BoneTwistDisperser3D.DisperseMode.Weighted`](BoneTwistDisperser3D.md).

For example, when `weightPosition` is `0.5`, if two bone segments with a length of `1.0` exist between three joints, weights are assigned to each joint from root to end at ratios of `0.5`, `1.0`, and `0.5`. Then amounts become `0.25`, `0.75`, and `1.0` respectively.

## DisperseMode

命名空间 `Godot`；enum

### 枚举值

- `Even = 0`：Assign amounts so that they monotonically increase from `0.0` to `1.0`, ensuring all weights are equal.
- `Weighted = 1`：Assign amounts so that they monotonically increase from `0.0` to `1.0`, based on the length of the bones between joint segments.
- `Custom = 2`：You can assign arbitrary amounts to the joint list.
