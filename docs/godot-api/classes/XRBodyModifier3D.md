<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRBodyModifier3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node uses body tracking data from an [`XRBodyTracker`](XRBodyTracker.md) to pose the skeleton of a body mesh.

Positioning of the body is performed by creating an [`XRNode3D`](XRNode3D.md) ancestor of the body mesh driven by the same [`XRBodyTracker`](XRBodyTracker.md).

The body tracking position-data is scaled by [`Skeleton3D.MotionScale`](Skeleton3D.md) when applied to the skeleton, which can be used to adjust the tracked body to match the scale of the body model.

## 构造函数

### XRBodyModifier3D

```csharp
public XRBodyModifier3D()
```

## 属性

### BodyTracker

```csharp
public StringName BodyTracker { get; set; }
```

引擎名：`body_tracker`

The name of the [`XRBodyTracker`](XRBodyTracker.md) registered with [`XRServer`](XRServer.md) to obtain the body tracking data from.

### BodyUpdate

```csharp
public XRBodyModifier3D.BodyUpdateEnum BodyUpdate { get; set; }
```

引擎名：`body_update`

Specifies the body parts to update.

### BoneUpdate

```csharp
public XRBodyModifier3D.BoneUpdateEnum BoneUpdate { get; set; }
```

引擎名：`bone_update`

Specifies the type of updates to perform on the bones.

## 方法

### GetBodyTracker

```csharp
public StringName GetBodyTracker()
```

引擎名：`get_body_tracker`

### GetBodyUpdate

```csharp
public XRBodyModifier3D.BodyUpdateEnum GetBodyUpdate()
```

引擎名：`get_body_update`

### GetBoneUpdate

```csharp
public XRBodyModifier3D.BoneUpdateEnum GetBoneUpdate()
```

引擎名：`get_bone_update`

### SetBodyTracker

```csharp
public void SetBodyTracker(StringName trackerName)
```

引擎名：`set_body_tracker`

### SetBodyUpdate

```csharp
public void SetBodyUpdate(XRBodyModifier3D.BodyUpdateEnum bodyUpdate)
```

引擎名：`set_body_update`

### SetBoneUpdate

```csharp
public void SetBoneUpdate(XRBodyModifier3D.BoneUpdateEnum boneUpdate)
```

引擎名：`set_bone_update`

## BodyUpdateEnum

命名空间 `Godot`；enum

### 枚举值

- `UpperBody = 1`：The skeleton's upper body joints are updated.
- `LowerBody = 2`：The skeleton's lower body joints are updated.
- `Hands = 4`：The skeleton's hand joints are updated.

## BoneUpdateEnum

命名空间 `Godot`；enum

### 枚举值

- `Full = 0`：The skeleton's bones are fully updated (both position and rotation) to match the tracked bones.
- `RotationOnly = 1`：The skeleton's bones are only rotated to align with the tracked bones, preserving bone length.
- `Max = 2`：Represents the size of the `XRBodyModifier3D.BoneUpdateEnum` enum.
