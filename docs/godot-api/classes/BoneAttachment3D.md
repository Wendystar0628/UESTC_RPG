<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# BoneAttachment3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node selects a bone in a [`Skeleton3D`](Skeleton3D.md) and attaches to it. This means that the `BoneAttachment3D` node will either dynamically copy or override the 3D transform of the selected bone.

## 构造函数

### BoneAttachment3D

```csharp
public BoneAttachment3D()
```

## 属性

### BoneIdx

```csharp
public int BoneIdx { get; set; }
```

引擎名：`bone_idx`

The index of the attached bone.

### BoneName

```csharp
public string BoneName { get; set; }
```

引擎名：`bone_name`

The name of the attached bone.

### ExternalSkeleton

```csharp
public NodePath ExternalSkeleton { get; set; }
```

引擎名：`external_skeleton`

The [`NodePath`](NodePath.md) to the external [`Skeleton3D`](Skeleton3D.md) node.

### OverridePose

```csharp
public bool OverridePose { get; set; }
```

引擎名：`override_pose`

Whether the `BoneAttachment3D` node will override the bone pose of the bone it is attached to. When set to `true`, the `BoneAttachment3D` node can change the pose of the bone. When set to `false`, the `BoneAttachment3D` will always be set to the bone's transform.

**Note:** This override performs interruptively in the skeleton update process using signals due to the old design. It may cause unintended behavior when used at the same time with [`SkeletonModifier3D`](SkeletonModifier3D.md).

### UseExternalSkeleton

```csharp
public bool UseExternalSkeleton { get; set; }
```

引擎名：`use_external_skeleton`

Whether the `BoneAttachment3D` node will use an external [`Skeleton3D`](Skeleton3D.md) node rather than attempting to use its parent node as the [`Skeleton3D`](Skeleton3D.md). When set to `true`, the `BoneAttachment3D` node will use the external [`Skeleton3D`](Skeleton3D.md) node set in `ExternalSkeleton`.

## 方法

### GetBoneIdx

```csharp
public int GetBoneIdx()
```

引擎名：`get_bone_idx`

### GetBoneName

```csharp
public string GetBoneName()
```

引擎名：`get_bone_name`

### GetExternalSkeleton

```csharp
public NodePath GetExternalSkeleton()
```

引擎名：`get_external_skeleton`

### GetOverridePose

```csharp
public bool GetOverridePose()
```

引擎名：`get_override_pose`

### GetSkeleton

```csharp
public Skeleton3D GetSkeleton()
```

引擎名：`get_skeleton`

Returns the parent or external [`Skeleton3D`](Skeleton3D.md) node if it exists, otherwise returns `null`.

### GetUseExternalSkeleton

```csharp
public bool GetUseExternalSkeleton()
```

引擎名：`get_use_external_skeleton`

### OnBonePoseUpdate

```csharp
public void OnBonePoseUpdate(int boneIndex)
```

### OnSkeletonUpdate

```csharp
public void OnSkeletonUpdate()
```

引擎名：`on_skeleton_update`

A function that is called automatically when the [`Skeleton3D`](Skeleton3D.md) is updated. This function is where the `BoneAttachment3D` node updates its position so it is correctly bound when it is *not* set to override the bone pose.

### SetBoneIdx

```csharp
public void SetBoneIdx(int boneIdx)
```

引擎名：`set_bone_idx`

### SetBoneName

```csharp
public void SetBoneName(string boneName)
```

引擎名：`set_bone_name`

### SetExternalSkeleton

```csharp
public void SetExternalSkeleton(NodePath externalSkeleton)
```

引擎名：`set_external_skeleton`

### SetOverridePose

```csharp
public void SetOverridePose(bool overridePose)
```

引擎名：`set_override_pose`

### SetUseExternalSkeleton

```csharp
public void SetUseExternalSkeleton(bool useExternalSkeleton)
```

引擎名：`set_use_external_skeleton`
