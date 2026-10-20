<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonProfile

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This resource is used in `EditorScenePostImport`. Some parameters are referring to bones in [`Skeleton3D`](Skeleton3D.md), [`Skin`](Skin.md), [`Animation`](Animation.md), and some other nodes are rewritten based on the parameters of `SkeletonProfile`.

**Note:** These parameters need to be set only when creating a custom profile. In [`SkeletonProfileHumanoid`](SkeletonProfileHumanoid.md), they are defined internally as read-only values.

## 构造函数

### SkeletonProfile

```csharp
public SkeletonProfile()
```

## 属性

### BoneSize

```csharp
public int BoneSize { get; set; }
```

引擎名：`bone_size`

The amount of bones in retargeting section's [`BoneMap`](BoneMap.md) editor. For example, [`SkeletonProfileHumanoid`](SkeletonProfileHumanoid.md) has 56 bones.

The size of elements in [`BoneMap`](BoneMap.md) updates when changing this property in it's assigned `SkeletonProfile`.

### GroupSize

```csharp
public int GroupSize { get; set; }
```

引擎名：`group_size`

The amount of groups of bones in retargeting section's [`BoneMap`](BoneMap.md) editor. For example, [`SkeletonProfileHumanoid`](SkeletonProfileHumanoid.md) has 4 groups.

This property exists to separate the bone list into several sections in the editor.

### RootBone

```csharp
public StringName RootBone { get; set; }
```

引擎名：`root_bone`

A bone name that will be used as the root bone in [`AnimationTree`](AnimationTree.md). This should be the bone of the parent of hips that exists at the world origin.

### ScaleBaseBone

```csharp
public StringName ScaleBaseBone { get; set; }
```

引擎名：`scale_base_bone`

A bone name which will use model's height as the coefficient for normalization. For example, [`SkeletonProfileHumanoid`](SkeletonProfileHumanoid.md) defines it as `Hips`.

## 方法

### FindBone

```csharp
public int FindBone(StringName boneName)
```

引擎名：`find_bone`

Returns the bone index that matches `boneName` as its name.

### GetBoneName

```csharp
public StringName GetBoneName(int boneIdx)
```

引擎名：`get_bone_name`

Returns the name of the bone at `boneIdx` that will be the key name in the [`BoneMap`](BoneMap.md).

In the retargeting process, the returned bone name is the bone name of the target skeleton.

### GetBoneParent

```csharp
public StringName GetBoneParent(int boneIdx)
```

引擎名：`get_bone_parent`

Returns the name of the bone which is the parent to the bone at `boneIdx`. The result is empty if the bone has no parent.

### GetBoneSize

```csharp
public int GetBoneSize()
```

引擎名：`get_bone_size`

### GetBoneTail

```csharp
public StringName GetBoneTail(int boneIdx)
```

引擎名：`get_bone_tail`

Returns the name of the bone which is the tail of the bone at `boneIdx`.

### GetGroup

```csharp
public StringName GetGroup(int boneIdx)
```

引擎名：`get_group`

Returns the group of the bone at `boneIdx`.

### GetGroupName

```csharp
public StringName GetGroupName(int groupIdx)
```

引擎名：`get_group_name`

Returns the name of the group at `groupIdx` that will be the drawing group in the [`BoneMap`](BoneMap.md) editor.

### GetGroupSize

```csharp
public int GetGroupSize()
```

引擎名：`get_group_size`

### GetHandleOffset

```csharp
public Vector2 GetHandleOffset(int boneIdx)
```

引擎名：`get_handle_offset`

Returns the offset of the bone at `boneIdx` that will be the button position in the [`BoneMap`](BoneMap.md) editor.

This is the offset with origin at the top left corner of the square.

### GetReferencePose

```csharp
public Transform3D GetReferencePose(int boneIdx)
```

引擎名：`get_reference_pose`

Returns the reference pose transform for bone `boneIdx`.

### GetRootBone

```csharp
public StringName GetRootBone()
```

引擎名：`get_root_bone`

### GetScaleBaseBone

```csharp
public StringName GetScaleBaseBone()
```

引擎名：`get_scale_base_bone`

### GetTailDirection

```csharp
public SkeletonProfile.TailDirection GetTailDirection(int boneIdx)
```

引擎名：`get_tail_direction`

Returns the tail direction of the bone at `boneIdx`.

### GetTexture

```csharp
public Texture2D GetTexture(int groupIdx)
```

引擎名：`get_texture`

Returns the texture of the group at `groupIdx` that will be the drawing group background image in the [`BoneMap`](BoneMap.md) editor.

### IsRequired

```csharp
public bool IsRequired(int boneIdx)
```

引擎名：`is_required`

Returns whether the bone at `boneIdx` is required for retargeting.

This value is used by the bone map editor. If this method returns `true`, and no bone is assigned, the handle color will be red on the bone map editor.

### SetBoneName

```csharp
public void SetBoneName(int boneIdx, StringName boneName)
```

引擎名：`set_bone_name`

Sets the name of the bone at `boneIdx` that will be the key name in the [`BoneMap`](BoneMap.md).

In the retargeting process, the setting bone name is the bone name of the target skeleton.

### SetBoneParent

```csharp
public void SetBoneParent(int boneIdx, StringName boneParent)
```

引擎名：`set_bone_parent`

Sets the bone with name `boneParent` as the parent of the bone at `boneIdx`. If an empty string is passed, then the bone has no parent.

### SetBoneSize

```csharp
public void SetBoneSize(int size)
```

引擎名：`set_bone_size`

### SetBoneTail

```csharp
public void SetBoneTail(int boneIdx, StringName boneTail)
```

引擎名：`set_bone_tail`

Sets the bone with name `boneTail` as the tail of the bone at `boneIdx`.

### SetGroup

```csharp
public void SetGroup(int boneIdx, StringName group)
```

引擎名：`set_group`

Sets the group of the bone at `boneIdx`.

### SetGroupName

```csharp
public void SetGroupName(int groupIdx, StringName groupName)
```

引擎名：`set_group_name`

Sets the name of the group at `groupIdx` that will be the drawing group in the [`BoneMap`](BoneMap.md) editor.

### SetGroupSize

```csharp
public void SetGroupSize(int size)
```

引擎名：`set_group_size`

### SetHandleOffset

```csharp
public void SetHandleOffset(int boneIdx, Vector2 handleOffset)
```

引擎名：`set_handle_offset`

Sets the offset of the bone at `boneIdx` that will be the button position in the [`BoneMap`](BoneMap.md) editor.

This is the offset with origin at the top left corner of the square.

### SetReferencePose

```csharp
public void SetReferencePose(int boneIdx, Transform3D boneName)
```

引擎名：`set_reference_pose`

Sets the reference pose transform for bone `boneIdx`.

### SetRequired

```csharp
public void SetRequired(int boneIdx, bool required)
```

引擎名：`set_required`

Sets the required status for bone `boneIdx` to `required`.

### SetRootBone

```csharp
public void SetRootBone(StringName boneName)
```

引擎名：`set_root_bone`

### SetScaleBaseBone

```csharp
public void SetScaleBaseBone(StringName boneName)
```

引擎名：`set_scale_base_bone`

### SetTailDirection

```csharp
public void SetTailDirection(int boneIdx, SkeletonProfile.TailDirection tailDirection)
```

引擎名：`set_tail_direction`

Sets the tail direction of the bone at `boneIdx`.

**Note:** This only specifies the method of calculation. The actual coordinates required should be stored in an external skeleton, so the calculation itself needs to be done externally.

### SetTexture

```csharp
public void SetTexture(int groupIdx, Texture2D texture)
```

引擎名：`set_texture`

Sets the texture of the group at `groupIdx` that will be the drawing group background image in the [`BoneMap`](BoneMap.md) editor.

## 事件（信号）

### ProfileUpdated

```csharp
public event Action ProfileUpdated
```

引擎名：`profile_updated`

This signal is emitted when change the value in profile. This is used to update key name in the [`BoneMap`](BoneMap.md) and to redraw the [`BoneMap`](BoneMap.md) editor.

**Note:** This signal is not connected directly to editor to simplify the reference, instead it is passed on to editor through the [`BoneMap`](BoneMap.md).

## TailDirection

命名空间 `Godot`；enum

### 枚举值

- `AverageChildren = 0`：Direction to the average coordinates of bone children.
- `SpecificChild = 1`：Direction to the coordinates of specified bone child.
- `End = 2`：Direction is not calculated.
