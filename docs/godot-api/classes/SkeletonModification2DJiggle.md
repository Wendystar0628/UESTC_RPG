<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DJiggle

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This modification moves a series of bones, typically called a bone chain, towards a target. What makes this modification special is that it calculates the velocity and acceleration for each bone in the bone chain, and runs a very light physics-like calculation using the inputted values. This allows the bones to overshoot the target and "jiggle" around. It can be configured to act more like a spring, or sway around like cloth might.

This modification is useful for adding additional motion to things like hair, the edges of clothing, and more. It has several settings to that allow control over how the joint moves when the target moves.

**Note:** The Jiggle modifier has `jiggle_joints`, which are the data objects that hold the data for each joint in the Jiggle chain. This is different from than [`Bone2D`](Bone2D.md) nodes! Jiggle joints hold the data needed for each [`Bone2D`](Bone2D.md) in the bone chain used by the Jiggle modification.

## 构造函数

### SkeletonModification2DJiggle

```csharp
public SkeletonModification2DJiggle()
```

## 属性

### Damping

```csharp
public float Damping { get; set; }
```

引擎名：`damping`

The default amount of damping applied to the Jiggle joints, if they are not overridden. Higher values lead to more of the calculated velocity being applied.

### Gravity

```csharp
public Vector2 Gravity { get; set; }
```

引擎名：`gravity`

The default amount of gravity applied to the Jiggle joints, if they are not overridden.

### JiggleDataChainLength

```csharp
public int JiggleDataChainLength { get; set; }
```

引擎名：`jiggle_data_chain_length`

The amount of Jiggle joints in the Jiggle modification.

### Mass

```csharp
public float Mass { get; set; }
```

引擎名：`mass`

The default amount of mass assigned to the Jiggle joints, if they are not overridden. Higher values lead to faster movements and more overshooting.

### Stiffness

```csharp
public float Stiffness { get; set; }
```

引擎名：`stiffness`

The default amount of stiffness assigned to the Jiggle joints, if they are not overridden. Higher values act more like springs, quickly moving into the correct position.

### TargetNodePath

```csharp
public NodePath TargetNodePath { get; set; }
```

The NodePath to the node that is the target for the Jiggle modification. This node is what the Jiggle chain will attempt to rotate the bone chain to.

### UseGravity

```csharp
public bool UseGravity { get; set; }
```

引擎名：`use_gravity`

Whether the gravity vector, `Gravity`, should be applied to the Jiggle joints, assuming they are not overriding the default settings.

## 方法

### GetCollisionMask

```csharp
public int GetCollisionMask()
```

引擎名：`get_collision_mask`

Returns the collision mask used by the Jiggle modifier when collisions are enabled.

### GetDamping

```csharp
public float GetDamping()
```

引擎名：`get_damping`

### GetGravity

```csharp
public Vector2 GetGravity()
```

引擎名：`get_gravity`

### GetJiggleDataChainLength

```csharp
public int GetJiggleDataChainLength()
```

引擎名：`get_jiggle_data_chain_length`

### GetJiggleJointBone2DNode

```csharp
public NodePath GetJiggleJointBone2DNode(int jointIdx)
```

Returns the [`Bone2D`](Bone2D.md) node assigned to the Jiggle joint at `jointIdx`.

### GetJiggleJointBoneIndex

```csharp
public int GetJiggleJointBoneIndex(int jointIdx)
```

引擎名：`get_jiggle_joint_bone_index`

Returns the index of the [`Bone2D`](Bone2D.md) node assigned to the Jiggle joint at `jointIdx`.

### GetJiggleJointDamping

```csharp
public float GetJiggleJointDamping(int jointIdx)
```

引擎名：`get_jiggle_joint_damping`

Returns the amount of damping of the Jiggle joint at `jointIdx`.

### GetJiggleJointGravity

```csharp
public Vector2 GetJiggleJointGravity(int jointIdx)
```

引擎名：`get_jiggle_joint_gravity`

Returns a [`Vector2`](Vector2.md) representing the amount of gravity the Jiggle joint at `jointIdx` is influenced by.

### GetJiggleJointMass

```csharp
public float GetJiggleJointMass(int jointIdx)
```

引擎名：`get_jiggle_joint_mass`

Returns the amount of mass of the jiggle joint at `jointIdx`.

### GetJiggleJointOverride

```csharp
public bool GetJiggleJointOverride(int jointIdx)
```

引擎名：`get_jiggle_joint_override`

Returns a boolean that indicates whether the joint at `jointIdx` is overriding the default Jiggle joint data defined in the modification.

### GetJiggleJointStiffness

```csharp
public float GetJiggleJointStiffness(int jointIdx)
```

引擎名：`get_jiggle_joint_stiffness`

Returns the stiffness of the Jiggle joint at `jointIdx`.

### GetJiggleJointUseGravity

```csharp
public bool GetJiggleJointUseGravity(int jointIdx)
```

引擎名：`get_jiggle_joint_use_gravity`

Returns a boolean that indicates whether the joint at `jointIdx` is using gravity or not.

### GetMass

```csharp
public float GetMass()
```

引擎名：`get_mass`

### GetStiffness

```csharp
public float GetStiffness()
```

引擎名：`get_stiffness`

### GetTargetNode

```csharp
public NodePath GetTargetNode()
```

引擎名：`get_target_node`

### GetUseColliders

```csharp
public bool GetUseColliders()
```

引擎名：`get_use_colliders`

Returns whether the jiggle modifier is taking physics colliders into account when solving.

### GetUseGravity

```csharp
public bool GetUseGravity()
```

引擎名：`get_use_gravity`

### Reset

```csharp
public void Reset()
```

引擎名：`reset`

Resets the internal jiggle simulation state to the current bone positions, clearing velocity, acceleration, and accumulated forces.

### SetCollisionMask

```csharp
public void SetCollisionMask(int collisionMask)
```

引擎名：`set_collision_mask`

Sets the collision mask that the Jiggle modifier will use when reacting to colliders, if the Jiggle modifier is set to take colliders into account.

### SetDamping

```csharp
public void SetDamping(float damping)
```

引擎名：`set_damping`

### SetGravity

```csharp
public void SetGravity(Vector2 gravity)
```

引擎名：`set_gravity`

### SetJiggleDataChainLength

```csharp
public void SetJiggleDataChainLength(int length)
```

引擎名：`set_jiggle_data_chain_length`

### SetJiggleJointBone2DNode

```csharp
public void SetJiggleJointBone2DNode(int jointIdx, NodePath bone2DNode)
```

Sets the [`Bone2D`](Bone2D.md) node assigned to the Jiggle joint at `jointIdx`.

### SetJiggleJointBoneIndex

```csharp
public void SetJiggleJointBoneIndex(int jointIdx, int boneIdx)
```

引擎名：`set_jiggle_joint_bone_index`

Sets the bone index, `boneIdx`, of the Jiggle joint at `jointIdx`. When possible, this will also update the `bone2d_node` of the Jiggle joint based on data provided by the linked skeleton.

### SetJiggleJointDamping

```csharp
public void SetJiggleJointDamping(int jointIdx, float damping)
```

引擎名：`set_jiggle_joint_damping`

Sets the amount of damping of the Jiggle joint at `jointIdx`.

### SetJiggleJointGravity

```csharp
public void SetJiggleJointGravity(int jointIdx, Vector2 gravity)
```

引擎名：`set_jiggle_joint_gravity`

Sets the gravity vector of the Jiggle joint at `jointIdx`.

### SetJiggleJointMass

```csharp
public void SetJiggleJointMass(int jointIdx, float mass)
```

引擎名：`set_jiggle_joint_mass`

Sets the of mass of the Jiggle joint at `jointIdx`.

### SetJiggleJointOverride

```csharp
public void SetJiggleJointOverride(int jointIdx, bool override)
```

引擎名：`set_jiggle_joint_override`

Sets whether the Jiggle joint at `jointIdx` should override the default Jiggle joint settings. Setting this to `true` will make the joint use its own settings rather than the default ones attached to the modification.

### SetJiggleJointStiffness

```csharp
public void SetJiggleJointStiffness(int jointIdx, float stiffness)
```

引擎名：`set_jiggle_joint_stiffness`

Sets the of stiffness of the Jiggle joint at `jointIdx`.

### SetJiggleJointUseGravity

```csharp
public void SetJiggleJointUseGravity(int jointIdx, bool useGravity)
```

引擎名：`set_jiggle_joint_use_gravity`

Sets whether the Jiggle joint at `jointIdx` should use gravity.

### SetMass

```csharp
public void SetMass(float mass)
```

引擎名：`set_mass`

### SetStiffness

```csharp
public void SetStiffness(float stiffness)
```

引擎名：`set_stiffness`

### SetTargetNode

```csharp
public void SetTargetNode(NodePath targetNodePath)
```

引擎名：`set_target_node`

### SetUseColliders

```csharp
public void SetUseColliders(bool useColliders)
```

引擎名：`set_use_colliders`

If `true`, the Jiggle modifier will take colliders into account, keeping them from entering into these collision objects.

### SetUseGravity

```csharp
public void SetUseGravity(bool useGravity)
```

引擎名：`set_use_gravity`
