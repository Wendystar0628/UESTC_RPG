<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DCcdik

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModification2D`](SkeletonModification2D.md) uses an algorithm called Cyclic Coordinate Descent Inverse Kinematics, or CCDIK, to manipulate a chain of bones in a [`Skeleton2D`](Skeleton2D.md) so it reaches a defined target.

CCDIK works by rotating a set of bones, typically called a "bone chain", on a single axis. Each bone is rotated to face the target from the tip (by default), which over a chain of bones allow it to rotate properly to reach the target. Because the bones only rotate on a single axis, CCDIK *can* look more robotic than other IK solvers.

**Note:** The CCDIK modifier has `ccdik_joints`, which are the data objects that hold the data for each joint in the CCDIK chain. This is different from a bone! CCDIK joints hold the data needed for each bone in the bone chain used by CCDIK.

CCDIK also fully supports angle constraints, allowing for more control over how a solution is met.

## 构造函数

### SkeletonModification2DCcdik

```csharp
public SkeletonModification2DCcdik()
```

## 属性

### CcdikDataChainLength

```csharp
public int CcdikDataChainLength { get; set; }
```

The number of CCDIK joints in the CCDIK modification.

### TargetNodePath

```csharp
public NodePath TargetNodePath { get; set; }
```

The NodePath to the node that is the target for the CCDIK modification. This node is what the CCDIK chain will attempt to rotate the bone chain to.

### TipNodePath

```csharp
public NodePath TipNodePath { get; set; }
```

The end position of the CCDIK chain. Typically, this should be a child of a [`Bone2D`](Bone2D.md) node attached to the final [`Bone2D`](Bone2D.md) in the CCDIK chain.

## 方法

### GetCcdikDataChainLength

```csharp
public int GetCcdikDataChainLength()
```

### GetCcdikJointBone2DNode

```csharp
public NodePath GetCcdikJointBone2DNode(int jointIdx)
```

Returns the [`Bone2D`](Bone2D.md) node assigned to the CCDIK joint at `jointIdx`.

### GetCcdikJointBoneIndex

```csharp
public int GetCcdikJointBoneIndex(int jointIdx)
```

Returns the index of the [`Bone2D`](Bone2D.md) node assigned to the CCDIK joint at `jointIdx`.

### GetCcdikJointConstraintAngleInvert

```csharp
public bool GetCcdikJointConstraintAngleInvert(int jointIdx)
```

Returns whether the CCDIK joint at `jointIdx` uses an inverted joint constraint. See `SetCcdikJointConstraintAngleInvert()` for details.

### GetCcdikJointConstraintAngleMax

```csharp
public float GetCcdikJointConstraintAngleMax(int jointIdx)
```

Returns the maximum angle constraint for the joint at `jointIdx`.

### GetCcdikJointConstraintAngleMin

```csharp
public float GetCcdikJointConstraintAngleMin(int jointIdx)
```

Returns the minimum angle constraint for the joint at `jointIdx`.

### GetCcdikJointEnableConstraint

```csharp
public bool GetCcdikJointEnableConstraint(int jointIdx)
```

Returns whether angle constraints on the CCDIK joint at `jointIdx` are enabled.

### GetCcdikJointRotateFromJoint

```csharp
public bool GetCcdikJointRotateFromJoint(int jointIdx)
```

Returns whether the joint at `jointIdx` is set to rotate from the joint, `true`, or to rotate from the tip, `false`. The default is to rotate from the tip.

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

### GetTipNode

```csharp
public NodePath GetTipNode()
```

### SetCcdikDataChainLength

```csharp
public void SetCcdikDataChainLength(int length)
```

### SetCcdikJointBone2DNode

```csharp
public void SetCcdikJointBone2DNode(int jointIdx, NodePath bone2DNodePath)
```

Sets the [`Bone2D`](Bone2D.md) node assigned to the CCDIK joint at `jointIdx`.

### SetCcdikJointBoneIndex

```csharp
public void SetCcdikJointBoneIndex(int jointIdx, int boneIdx)
```

Sets the bone index, `boneIdx`, of the CCDIK joint at `jointIdx`. When possible, this will also update the `bone2d_node` of the CCDIK joint based on data provided by the linked skeleton.

### SetCcdikJointConstraintAngleInvert

```csharp
public void SetCcdikJointConstraintAngleInvert(int jointIdx, bool invert)
```

Sets whether the CCDIK joint at `jointIdx` uses an inverted joint constraint.

An inverted joint constraint only constraints the CCDIK joint to the angles *outside of* the inputted minimum and maximum angles. For this reason, it is referred to as an inverted joint constraint, as it constraints the joint to the outside of the inputted values.

### SetCcdikJointConstraintAngleMax

```csharp
public void SetCcdikJointConstraintAngleMax(int jointIdx, float angleMax)
```

Sets the maximum angle constraint for the joint at `jointIdx`.

### SetCcdikJointConstraintAngleMin

```csharp
public void SetCcdikJointConstraintAngleMin(int jointIdx, float angleMin)
```

Sets the minimum angle constraint for the joint at `jointIdx`.

### SetCcdikJointEnableConstraint

```csharp
public void SetCcdikJointEnableConstraint(int jointIdx, bool enableConstraint)
```

Determines whether angle constraints on the CCDIK joint at `jointIdx` are enabled. When `true`, constraints will be enabled and taken into account when solving.

### SetCcdikJointRotateFromJoint

```csharp
public void SetCcdikJointRotateFromJoint(int jointIdx, bool rotateFromJoint)
```

Sets whether the joint at `jointIdx` is set to rotate from the joint, `true`, or to rotate from the tip, `false`.

### SetTargetNode

```csharp
public void SetTargetNode(NodePath targetNodePath)
```

### SetTipNode

```csharp
public void SetTipNode(NodePath tipNodePath)
```
