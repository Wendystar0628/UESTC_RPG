<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# ChainIK3D

命名空间 `Godot`；class；继承 [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Base class of [`SkeletonModifier3D`](SkeletonModifier3D.md) that automatically generates a joint list from the bones between the root bone and the end bone.

**Note:** All the methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/root_bone_name`).

## 方法

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

### IsEndBoneExtended

```csharp
public bool IsEndBoneExtended(int index)
```

引擎名：`is_end_bone_extended`

Returns `true` if the end bone is extended to have a tail.

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

**Note:** The end bone must be the root bone or a child of the root bone. If they are the same, the tail must be extended by `SetExtendEndBone()` to modify the bone.

### SetExtendEndBone

```csharp
public void SetExtendEndBone(int index, bool enabled)
```

引擎名：`set_extend_end_bone`

If `enabled` is `true`, the end bone is extended to have a tail.

The extended tail config is allocated to the last element in the joint list. In other words, if you set `enabled` to `false`, the config of the last element in the joint list has no effect in the simulated result.

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
