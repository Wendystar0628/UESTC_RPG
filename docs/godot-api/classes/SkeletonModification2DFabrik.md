<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DFabrik

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModification2D`](SkeletonModification2D.md) uses an algorithm called Forward And Backward Reaching Inverse Kinematics, or FABRIK, to rotate a bone chain so that it reaches a target.

FABRIK works by knowing the positions and lengths of a series of bones, typically called a "bone chain". It first starts by running a forward pass, which places the final bone at the target's position. Then all other bones are moved towards the tip bone, so they stay at the defined bone length away. Then a backwards pass is performed, where the root/first bone in the FABRIK chain is placed back at the origin. Then all other bones are moved so they stay at the defined bone length away. This positions the bone chain so that it reaches the target when possible, but all of the bones stay the correct length away from each other.

Because of how FABRIK works, it often gives more natural results than those seen in [`SkeletonModification2DCcdik`](SkeletonModification2DCcdik.md).

**Note:** The FABRIK modifier has `fabrik_joints`, which are the data objects that hold the data for each joint in the FABRIK chain. This is different from [`Bone2D`](Bone2D.md) nodes! FABRIK joints hold the data needed for each [`Bone2D`](Bone2D.md) in the bone chain used by FABRIK.

To help control how the FABRIK joints move, a magnet vector can be passed, which can nudge the bones in a certain direction prior to solving, giving a level of control over the final result.

## 构造函数

### SkeletonModification2DFabrik

```csharp
public SkeletonModification2DFabrik()
```

## 属性

### FabrikDataChainLength

```csharp
public int FabrikDataChainLength { get; set; }
```

The number of FABRIK joints in the FABRIK modification.

### TargetNodePath

```csharp
public NodePath TargetNodePath { get; set; }
```

The NodePath to the node that is the target for the FABRIK modification. This node is what the FABRIK chain will attempt to rotate the bone chain to.

## 方法

### GetFabrikDataChainLength

```csharp
public int GetFabrikDataChainLength()
```

### GetFabrikJointBone2DNode

```csharp
public NodePath GetFabrikJointBone2DNode(int jointIdx)
```

Returns the [`Bone2D`](Bone2D.md) node assigned to the FABRIK joint at `jointIdx`.

### GetFabrikJointBoneIndex

```csharp
public int GetFabrikJointBoneIndex(int jointIdx)
```

Returns the index of the [`Bone2D`](Bone2D.md) node assigned to the FABRIK joint at `jointIdx`.

### GetFabrikJointMagnetPosition

```csharp
public Vector2 GetFabrikJointMagnetPosition(int jointIdx)
```

Returns the magnet position vector for the joint at `jointIdx`.

### GetFabrikJointUseTargetRotation

```csharp
public bool GetFabrikJointUseTargetRotation(int jointIdx)
```

Returns whether the joint is using the target's rotation rather than allowing FABRIK to rotate the joint. This option only applies to the tip/final joint in the chain.

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

### SetFabrikDataChainLength

```csharp
public void SetFabrikDataChainLength(int length)
```

### SetFabrikJointBone2DNode

```csharp
public void SetFabrikJointBone2DNode(int jointIdx, NodePath bone2DNodePath)
```

Sets the [`Bone2D`](Bone2D.md) node assigned to the FABRIK joint at `jointIdx`.

### SetFabrikJointBoneIndex

```csharp
public void SetFabrikJointBoneIndex(int jointIdx, int boneIdx)
```

Sets the bone index, `boneIdx`, of the FABRIK joint at `jointIdx`. When possible, this will also update the `bone2d_node` of the FABRIK joint based on data provided by the linked skeleton.

### SetFabrikJointMagnetPosition

```csharp
public void SetFabrikJointMagnetPosition(int jointIdx, Vector2 magnetPosition)
```

Sets the magnet position vector for the joint at `jointIdx`.

### SetFabrikJointUseTargetRotation

```csharp
public void SetFabrikJointUseTargetRotation(int jointIdx, bool useTargetRotation)
```

Sets whether the joint at `jointIdx` will use the target node's rotation rather than letting FABRIK rotate the node.

**Note:** This option only works for the tip/final joint in the chain. For all other nodes, this option will be ignored.

### SetTargetNode

```csharp
public void SetTargetNode(NodePath targetNodePath)
```
