<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRHandModifier3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node uses hand tracking data from an [`XRHandTracker`](XRHandTracker.md) to pose the skeleton of a hand mesh.

Positioning of hands is performed by creating an [`XRNode3D`](XRNode3D.md) ancestor of the hand mesh driven by the same [`XRHandTracker`](XRHandTracker.md).

The hand tracking position-data is scaled by [`Skeleton3D.MotionScale`](Skeleton3D.md) when applied to the skeleton, which can be used to adjust the tracked hand to match the scale of the hand model.

## 构造函数

### XRHandModifier3D

```csharp
public XRHandModifier3D()
```

## 属性

### BoneUpdate

```csharp
public XRHandModifier3D.BoneUpdateEnum BoneUpdate { get; set; }
```

引擎名：`bone_update`

Specifies the type of updates to perform on the bones.

### HandTracker

```csharp
public StringName HandTracker { get; set; }
```

引擎名：`hand_tracker`

The name of the [`XRHandTracker`](XRHandTracker.md) registered with [`XRServer`](XRServer.md) to obtain the hand tracking data from.

## 方法

### GetBoneUpdate

```csharp
public XRHandModifier3D.BoneUpdateEnum GetBoneUpdate()
```

引擎名：`get_bone_update`

### GetHandTracker

```csharp
public StringName GetHandTracker()
```

引擎名：`get_hand_tracker`

### SetBoneUpdate

```csharp
public void SetBoneUpdate(XRHandModifier3D.BoneUpdateEnum boneUpdate)
```

引擎名：`set_bone_update`

### SetHandTracker

```csharp
public void SetHandTracker(StringName trackerName)
```

引擎名：`set_hand_tracker`

## BoneUpdateEnum

命名空间 `Godot`；enum

### 枚举值

- `Full = 0`：The skeleton's bones are fully updated (both position and rotation) to match the tracked bones.
- `RotationOnly = 1`：The skeleton's bones are only rotated to align with the tracked bones, preserving bone length.
- `Max = 2`：Represents the size of the `XRHandModifier3D.BoneUpdateEnum` enum.
