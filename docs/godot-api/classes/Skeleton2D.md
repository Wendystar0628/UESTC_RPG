<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Skeleton2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

`Skeleton2D` parents a hierarchy of [`Bone2D`](Bone2D.md) nodes. It holds a reference to each [`Bone2D`](Bone2D.md)'s rest pose and acts as a single point of access to its bones.

To set up different types of inverse kinematics for the given Skeleton2D, a [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) should be created. The inverse kinematics be applied by increasing [`SkeletonModificationStack2D.ModificationCount`](SkeletonModificationStack2D.md) and creating the desired number of modifications.

## 构造函数

### Skeleton2D

```csharp
public Skeleton2D()
```

## 方法

### ExecuteModifications

```csharp
public void ExecuteModifications(float delta, int executionMode)
```

引擎名：`execute_modifications`

Executes all the modifications on the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md), if the Skeleton2D has one assigned.

### GetBone

```csharp
public Bone2D GetBone(int idx)
```

引擎名：`get_bone`

Returns a [`Bone2D`](Bone2D.md) from the node hierarchy parented by Skeleton2D. The object to return is identified by the parameter `idx`. Bones are indexed by descending the node hierarchy from top to bottom, adding the children of each branch before moving to the next sibling.

### GetBoneCount

```csharp
public int GetBoneCount()
```

引擎名：`get_bone_count`

Returns the number of [`Bone2D`](Bone2D.md) nodes in the node hierarchy parented by Skeleton2D.

### GetBoneLocalPoseOverride

```csharp
public Transform2D GetBoneLocalPoseOverride(int boneIdx)
```

引擎名：`get_bone_local_pose_override`

Returns the local pose override transform for `boneIdx`.

### GetModificationStack

```csharp
public SkeletonModificationStack2D GetModificationStack()
```

引擎名：`get_modification_stack`

Returns the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) attached to this skeleton, if one exists.

### GetSkeleton

```csharp
public Rid GetSkeleton()
```

引擎名：`get_skeleton`

Returns the [`Rid`](Rid.md) of a Skeleton2D instance.

### SetBoneLocalPoseOverride

```csharp
public void SetBoneLocalPoseOverride(int boneIdx, Transform2D overridePose, float strength, bool persistent)
```

引擎名：`set_bone_local_pose_override`

Sets the local pose transform, `overridePose`, for the bone at `boneIdx`.

`strength` is the interpolation strength that will be used when applying the pose, and `persistent` determines if the applied pose will remain.

**Note:** The pose transform needs to be a local transform relative to the [`Bone2D`](Bone2D.md) node at `boneIdx`!

### SetModificationStack

```csharp
public void SetModificationStack(SkeletonModificationStack2D modificationStack)
```

引擎名：`set_modification_stack`

Sets the [`SkeletonModificationStack2D`](SkeletonModificationStack2D.md) attached to this skeleton.

## 事件（信号）

### BoneSetupChanged

```csharp
public event Action BoneSetupChanged
```

引擎名：`bone_setup_changed`

Emitted when the [`Bone2D`](Bone2D.md) setup attached to this skeletons changes. This is primarily used internally within the skeleton.
