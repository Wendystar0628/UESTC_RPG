<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DLookAt

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This [`SkeletonModification2D`](SkeletonModification2D.md) rotates a bone to look a target. This is extremely helpful for moving character's head to look at the player, rotating a turret to look at a target, or any other case where you want to make a bone rotate towards something quickly and easily.

## 构造函数

### SkeletonModification2DLookAt

```csharp
public SkeletonModification2DLookAt()
```

## 属性

### Bone2DNode

```csharp
public NodePath Bone2DNode { get; set; }
```

The [`Bone2D`](Bone2D.md) node that the modification will operate on.

### BoneIndex

```csharp
public int BoneIndex { get; set; }
```

引擎名：`bone_index`

The index of the [`Bone2D`](Bone2D.md) node that the modification will operate on.

### TargetNodePath

```csharp
public NodePath TargetNodePath { get; set; }
```

The NodePath to the node that is the target for the LookAt modification. This node is what the modification will rotate the [`Bone2D`](Bone2D.md) to.

## 方法

### GetAdditionalRotation

```csharp
public float GetAdditionalRotation()
```

引擎名：`get_additional_rotation`

Returns the amount of additional rotation that is applied after the LookAt modification executes.

### GetBone2DNode

```csharp
public NodePath GetBone2DNode()
```

### GetBoneIndex

```csharp
public int GetBoneIndex()
```

引擎名：`get_bone_index`

### GetConstraintAngleInvert

```csharp
public bool GetConstraintAngleInvert()
```

引擎名：`get_constraint_angle_invert`

Returns whether the constraints to this modification are inverted or not.

### GetConstraintAngleMax

```csharp
public float GetConstraintAngleMax()
```

引擎名：`get_constraint_angle_max`

Returns the constraint's maximum allowed angle.

### GetConstraintAngleMin

```csharp
public float GetConstraintAngleMin()
```

引擎名：`get_constraint_angle_min`

Returns the constraint's minimum allowed angle.

### GetEnableConstraint

```csharp
public bool GetEnableConstraint()
```

引擎名：`get_enable_constraint`

Returns `true` if the LookAt modification is using constraints.

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

引擎名：`get_target_node`

### SetAdditionalRotation

```csharp
public void SetAdditionalRotation(float rotation)
```

引擎名：`set_additional_rotation`

Sets the amount of additional rotation that is to be applied after executing the modification. This allows for offsetting the results by the inputted rotation amount.

### SetBone2DNode

```csharp
public void SetBone2DNode(NodePath bone2DNodePath)
```

### SetBoneIndex

```csharp
public void SetBoneIndex(int boneIdx)
```

引擎名：`set_bone_index`

### SetConstraintAngleInvert

```csharp
public void SetConstraintAngleInvert(bool invert)
```

引擎名：`set_constraint_angle_invert`

When `true`, the modification will use an inverted joint constraint.

An inverted joint constraint only constraints the [`Bone2D`](Bone2D.md) to the angles *outside of* the inputted minimum and maximum angles. For this reason, it is referred to as an inverted joint constraint, as it constraints the joint to the outside of the inputted values.

### SetConstraintAngleMax

```csharp
public void SetConstraintAngleMax(float angleMax)
```

引擎名：`set_constraint_angle_max`

Sets the constraint's maximum allowed angle.

### SetConstraintAngleMin

```csharp
public void SetConstraintAngleMin(float angleMin)
```

引擎名：`set_constraint_angle_min`

Sets the constraint's minimum allowed angle.

### SetEnableConstraint

```csharp
public void SetEnableConstraint(bool enableConstraint)
```

引擎名：`set_enable_constraint`

Sets whether this modification will use constraints or not. When `true`, constraints will be applied when solving the LookAt modification.

### SetTargetNode

```csharp
public void SetTargetNode(NodePath targetNodePath)
```

引擎名：`set_target_node`
