<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# TwoBoneIK3D

命名空间 `Godot`；class；继承 [`IKModifier3D`](IKModifier3D.md) → [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This [`IKModifier3D`](IKModifier3D.md) requires a pole target. It provides deterministic results by constructing a plane from each joint and pole target and finding the intersection of two circles (disks in 3D).

This IK can handle twist by setting the pole direction. If there are more than one bone between each set bone, their rotations are ignored, and the straight line connecting the root-middle and middle-end joints are treated as virtual bones.

**Note:** All the methods in this class take an `index` parameter. This parameter specifies which setting list entry to return if the IK has multiple entries (e.g. `settings/<index>/root_bone_name`).

## 构造函数

### TwoBoneIK3D

```csharp
public TwoBoneIK3D()
```

## 属性

### SettingCount

```csharp
public int SettingCount { get; set; }
```

引擎名：`setting_count`

The number of settings.

## 方法

### GetEndBone

```csharp
public int GetEndBone(int index)
```

引擎名：`get_end_bone`

Returns the end bone index.

### GetEndBoneDirection

```csharp
public SkeletonModifier3D.BoneDirection GetEndBoneDirection(int index)
```

引擎名：`get_end_bone_direction`

Returns the end bone's tail direction when `IsEndBoneExtended()` is `true`.

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

Returns the end bone name.

### GetMiddleBone

```csharp
public int GetMiddleBone(int index)
```

引擎名：`get_middle_bone`

Returns the middle bone index.

### GetMiddleBoneName

```csharp
public string GetMiddleBoneName(int index)
```

引擎名：`get_middle_bone_name`

Returns the middle bone name.

### GetPoleDirection

```csharp
public SkeletonModifier3D.SecondaryDirection GetPoleDirection(int index)
```

引擎名：`get_pole_direction`

Returns the pole direction.

### GetPoleDirectionVector

```csharp
public Vector3 GetPoleDirectionVector(int index)
```

引擎名：`get_pole_direction_vector`

Returns the pole direction vector.

If `GetPoleDirection()` is [`SkeletonModifier3D.SecondaryDirection.None`](SkeletonModifier3D.md), this method returns `Vector3(0, 0, 0)`.

### GetPoleNode

```csharp
public NodePath GetPoleNode(int index)
```

引擎名：`get_pole_node`

Returns the pole target node that constructs a plane which the joints are all on and the pole is trying to direct.

### GetRootBone

```csharp
public int GetRootBone(int index)
```

引擎名：`get_root_bone`

Returns the root bone index.

### GetRootBoneName

```csharp
public string GetRootBoneName(int index)
```

引擎名：`get_root_bone_name`

Returns the root bone name.

### GetTargetNode

```csharp
public NodePath GetTargetNode(int index)
```

引擎名：`get_target_node`

Returns the target node that the end bone is trying to reach.

### IsEndBoneExtended

```csharp
public bool IsEndBoneExtended(int index)
```

引擎名：`is_end_bone_extended`

Returns `true` if the end bone is extended to have a tail.

### IsUsingVirtualEnd

```csharp
public bool IsUsingVirtualEnd(int index)
```

引擎名：`is_using_virtual_end`

Returns `true` if the end bone is extended from the middle bone as a virtual bone.

### SetEndBone

```csharp
public void SetEndBone(int index, int bone)
```

引擎名：`set_end_bone`

Sets the end bone index.

### SetEndBoneDirection

```csharp
public void SetEndBoneDirection(int index, SkeletonModifier3D.BoneDirection boneDirection)
```

引擎名：`set_end_bone_direction`

Sets the end bone tail direction when `IsEndBoneExtended()` is `true`.

### SetEndBoneLength

```csharp
public void SetEndBoneLength(int index, float length)
```

引擎名：`set_end_bone_length`

Sets the end bone tail length when `IsEndBoneExtended()` is `true`.

### SetEndBoneName

```csharp
public void SetEndBoneName(int index, string boneName)
```

引擎名：`set_end_bone_name`

Sets the end bone name.

**Note:** The end bone must be a child of the middle bone.

### SetExtendEndBone

```csharp
public void SetExtendEndBone(int index, bool enabled)
```

引擎名：`set_extend_end_bone`

If `enabled` is `true`, the end bone is extended to have a tail.

### SetMiddleBone

```csharp
public void SetMiddleBone(int index, int bone)
```

引擎名：`set_middle_bone`

Sets the middle bone index.

### SetMiddleBoneName

```csharp
public void SetMiddleBoneName(int index, string boneName)
```

引擎名：`set_middle_bone_name`

Sets the middle bone name.

**Note:** The middle bone must be a child of the root bone.

### SetPoleDirection

```csharp
public void SetPoleDirection(int index, SkeletonModifier3D.SecondaryDirection direction)
```

引擎名：`set_pole_direction`

Sets the pole direction.

The pole is on the middle bone and will direct to the pole target.

The rotation axis is a vector that is orthogonal to this and the forward vector.

**Note:** The pole direction and the forward vector shouldn't be colinear to avoid unintended rotation.

### SetPoleDirectionVector

```csharp
public void SetPoleDirectionVector(int index, Vector3 vector)
```

引擎名：`set_pole_direction_vector`

Sets the pole direction vector.

This vector is normalized by an internal process.

If the vector length is `0`, it is considered synonymous with [`SkeletonModifier3D.SecondaryDirection.None`](SkeletonModifier3D.md).

### SetPoleNode

```csharp
public void SetPoleNode(int index, NodePath poleNode)
```

引擎名：`set_pole_node`

Sets the pole target node that constructs a plane which the joints are all on and the pole is trying to direct.

### SetRootBone

```csharp
public void SetRootBone(int index, int bone)
```

引擎名：`set_root_bone`

Sets the root bone index.

### SetRootBoneName

```csharp
public void SetRootBoneName(int index, string boneName)
```

引擎名：`set_root_bone_name`

Sets the root bone name.

### SetTargetNode

```csharp
public void SetTargetNode(int index, NodePath targetNode)
```

引擎名：`set_target_node`

Sets the target node that the end bone is trying to reach.

### SetUseVirtualEnd

```csharp
public void SetUseVirtualEnd(int index, bool enabled)
```

引擎名：`set_use_virtual_end`

If `enabled` is `true`, the end bone is extended from the middle bone as a virtual bone.
