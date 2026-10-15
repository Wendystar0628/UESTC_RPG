<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DTwoBoneIK

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModification2D`](SkeletonModification2D.md) uses an algorithm typically called TwoBoneIK. This algorithm works by leveraging the law of cosines and the lengths of the bones to figure out what rotation the bones currently have, and what rotation they need to make a complete triangle, where the first bone, the second bone, and the target form the three vertices of the triangle. Because the algorithm works by making a triangle, it can only operate on two bones.

TwoBoneIK is great for arms, legs, and really any joints that can be represented by just two bones that bend to reach a target. This solver is more lightweight than [`SkeletonModification2DFabrik`](SkeletonModification2DFabrik.md), but gives similar, natural looking results.

## 构造函数

### SkeletonModification2DTwoBoneIK

```csharp
public SkeletonModification2DTwoBoneIK()
```

## 属性

### FlipBendDirection

```csharp
public bool FlipBendDirection { get; set; }
```

引擎名：`flip_bend_direction`

If `true`, the bones in the modification will bend outward as opposed to inwards when contracting. If `false`, the bones will bend inwards when contracting.

### TargetMaximumDistance

```csharp
public float TargetMaximumDistance { get; set; }
```

引擎名：`target_maximum_distance`

The maximum distance the target can be at. If the target is farther than this distance, the modification will solve as if it's at this maximum distance. When set to `0`, the modification will solve without distance constraints.

### TargetMinimumDistance

```csharp
public float TargetMinimumDistance { get; set; }
```

引擎名：`target_minimum_distance`

The minimum distance the target can be at. If the target is closer than this distance, the modification will solve as if it's at this minimum distance. When set to `0`, the modification will solve without distance constraints.

### TargetNodePath

```csharp
public NodePath TargetNodePath { get; set; }
```

The NodePath to the node that is the target for the TwoBoneIK modification. This node is what the modification will use when bending the [`Bone2D`](Bone2D.md) nodes.

## 方法

### GetFlipBendDirection

```csharp
public bool GetFlipBendDirection()
```

引擎名：`get_flip_bend_direction`

### GetJointOneBone2DNode

```csharp
public NodePath GetJointOneBone2DNode()
```

Returns the [`Bone2D`](Bone2D.md) node that is being used as the first bone in the TwoBoneIK modification.

### GetJointOneBoneIdx

```csharp
public int GetJointOneBoneIdx()
```

引擎名：`get_joint_one_bone_idx`

Returns the index of the [`Bone2D`](Bone2D.md) node that is being used as the first bone in the TwoBoneIK modification.

### GetJointTwoBone2DNode

```csharp
public NodePath GetJointTwoBone2DNode()
```

Returns the [`Bone2D`](Bone2D.md) node that is being used as the second bone in the TwoBoneIK modification.

### GetJointTwoBoneIdx

```csharp
public int GetJointTwoBoneIdx()
```

引擎名：`get_joint_two_bone_idx`

Returns the index of the [`Bone2D`](Bone2D.md) node that is being used as the second bone in the TwoBoneIK modification.

### GetTargetMaximumDistance

```csharp
public float GetTargetMaximumDistance()
```

引擎名：`get_target_maximum_distance`

### GetTargetMinimumDistance

```csharp
public float GetTargetMinimumDistance()
```

引擎名：`get_target_minimum_distance`

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

引擎名：`get_target_node`

### SetFlipBendDirection

```csharp
public void SetFlipBendDirection(bool flipDirection)
```

引擎名：`set_flip_bend_direction`

### SetJointOneBone2DNode

```csharp
public void SetJointOneBone2DNode(NodePath bone2DNode)
```

Sets the [`Bone2D`](Bone2D.md) node that is being used as the first bone in the TwoBoneIK modification.

### SetJointOneBoneIdx

```csharp
public void SetJointOneBoneIdx(int boneIdx)
```

引擎名：`set_joint_one_bone_idx`

Sets the index of the [`Bone2D`](Bone2D.md) node that is being used as the first bone in the TwoBoneIK modification.

### SetJointTwoBone2DNode

```csharp
public void SetJointTwoBone2DNode(NodePath bone2DNode)
```

Sets the [`Bone2D`](Bone2D.md) node that is being used as the second bone in the TwoBoneIK modification.

### SetJointTwoBoneIdx

```csharp
public void SetJointTwoBoneIdx(int boneIdx)
```

引擎名：`set_joint_two_bone_idx`

Sets the index of the [`Bone2D`](Bone2D.md) node that is being used as the second bone in the TwoBoneIK modification.

### SetTargetMaximumDistance

```csharp
public void SetTargetMaximumDistance(float maximumDistance)
```

引擎名：`set_target_maximum_distance`

### SetTargetMinimumDistance

```csharp
public void SetTargetMinimumDistance(float minimumDistance)
```

引擎名：`set_target_minimum_distance`

### SetTargetNode

```csharp
public void SetTargetNode(NodePath targetNodePath)
```

引擎名：`set_target_node`
