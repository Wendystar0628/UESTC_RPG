<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# OpenXRHand

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

This node enables OpenXR's hand tracking functionality. The node should be a child node of an [`XROrigin3D`](XROrigin3D.md) node, tracking will update its position to the player's tracked hand Palm joint location (the center of the middle finger's metacarpal bone). This node also updates the skeleton of a properly skinned hand or avatar model.

If the skeleton is a hand (one of the hand bones is the root node of the skeleton), then the skeleton will be placed relative to the hand palm location and the hand mesh and skeleton should be children of the OpenXRHand node.

If the hand bones are part of a full skeleton, then the root of the hand will keep its location with the assumption that IK is used to position the hand and arm.

By default the skeleton hand bones are repositioned to match the size of the tracked hand. To preserve the modeled bone sizes change `BoneUpdate` to apply rotation only.

## 构造函数

### OpenXRHand

```csharp
public OpenXRHand()
```

## 属性

### BoneUpdate

```csharp
public OpenXRHand.BoneUpdateEnum BoneUpdate { get; set; }
```

引擎名：`bone_update`

Specify the type of updates to perform on the bone.

### Hand

```csharp
public OpenXRHand.Hands Hand { get; set; }
```

引擎名：`hand`

Specifies whether this node tracks the left or right hand of the player.

### HandSkeleton

```csharp
public NodePath HandSkeleton { get; set; }
```

引擎名：`hand_skeleton`

Set a [`Skeleton3D`](Skeleton3D.md) node for which the pose positions will be updated.

### MotionRange

```csharp
public OpenXRHand.MotionRangeEnum MotionRange { get; set; }
```

引擎名：`motion_range`

Set the motion range (if supported) limiting the hand motion.

### SkeletonRig

```csharp
public OpenXRHand.SkeletonRigEnum SkeletonRig { get; set; }
```

引擎名：`skeleton_rig`

Set the type of skeleton rig the `HandSkeleton` is compliant with.

## 方法

### GetBoneUpdate

```csharp
public OpenXRHand.BoneUpdateEnum GetBoneUpdate()
```

引擎名：`get_bone_update`

### GetHand

```csharp
public OpenXRHand.Hands GetHand()
```

引擎名：`get_hand`

### GetHandSkeleton

```csharp
public NodePath GetHandSkeleton()
```

引擎名：`get_hand_skeleton`

### GetMotionRange

```csharp
public OpenXRHand.MotionRangeEnum GetMotionRange()
```

引擎名：`get_motion_range`

### GetSkeletonRig

```csharp
public OpenXRHand.SkeletonRigEnum GetSkeletonRig()
```

引擎名：`get_skeleton_rig`

### SetBoneUpdate

```csharp
public void SetBoneUpdate(OpenXRHand.BoneUpdateEnum boneUpdate)
```

引擎名：`set_bone_update`

### SetHand

```csharp
public void SetHand(OpenXRHand.Hands hand)
```

引擎名：`set_hand`

### SetHandSkeleton

```csharp
public void SetHandSkeleton(NodePath handSkeleton)
```

引擎名：`set_hand_skeleton`

### SetMotionRange

```csharp
public void SetMotionRange(OpenXRHand.MotionRangeEnum motionRange)
```

引擎名：`set_motion_range`

### SetSkeletonRig

```csharp
public void SetSkeletonRig(OpenXRHand.SkeletonRigEnum skeletonRig)
```

引擎名：`set_skeleton_rig`

## BoneUpdateEnum

命名空间 `Godot`；enum

### 枚举值

- `Full = 0`：The skeletons bones are fully updated (both position and rotation) to match the tracked bones.
- `RotationOnly = 1`：The skeletons bones are only rotated to align with the tracked bones, preserving bone length.
- `Max = 2`：Maximum supported bone update mode.

## Hands

命名空间 `Godot`；enum

### 枚举值

- `Left = 0`：Tracking the player's left hand.
- `Right = 1`：Tracking the player's right hand.
- `Max = 2`：Maximum supported hands.

## MotionRangeEnum

命名空间 `Godot`；enum

### 枚举值

- `Unobstructed = 0`：When player grips, hand skeleton will form a full fist.
- `ConformToController = 1`：When player grips, hand skeleton conforms to the controller the player is holding.
- `Max = 2`：Maximum supported motion ranges.

## SkeletonRigEnum

命名空间 `Godot`；enum

### 枚举值

- `Openxr = 0`：An OpenXR compliant skeleton.
- `Humanoid = 1`：A [`SkeletonProfileHumanoid`](SkeletonProfileHumanoid.md) compliant skeleton.
- `Max = 2`：Maximum supported hands.
