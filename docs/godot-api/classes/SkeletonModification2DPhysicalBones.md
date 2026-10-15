<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonModification2DPhysicalBones

命名空间 `Godot`；class；继承 [`SkeletonModification2D`](SkeletonModification2D.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

This modification takes the transforms of [`PhysicalBone2D`](PhysicalBone2D.md) nodes and applies them to [`Bone2D`](Bone2D.md) nodes. This allows the [`Bone2D`](Bone2D.md) nodes to react to physics thanks to the linked [`PhysicalBone2D`](PhysicalBone2D.md) nodes.

## 构造函数

### SkeletonModification2DPhysicalBones

```csharp
public SkeletonModification2DPhysicalBones()
```

## 属性

### PhysicalBoneChainLength

```csharp
public int PhysicalBoneChainLength { get; set; }
```

引擎名：`physical_bone_chain_length`

The number of [`PhysicalBone2D`](PhysicalBone2D.md) nodes linked in this modification.

## 方法

### FetchPhysicalBones

```csharp
public void FetchPhysicalBones()
```

引擎名：`fetch_physical_bones`

Empties the list of [`PhysicalBone2D`](PhysicalBone2D.md) nodes and populates it with all [`PhysicalBone2D`](PhysicalBone2D.md) nodes that are children of the [`Skeleton2D`](Skeleton2D.md).

### GetPhysicalBoneChainLength

```csharp
public int GetPhysicalBoneChainLength()
```

引擎名：`get_physical_bone_chain_length`

### GetPhysicalBoneNode

```csharp
public NodePath GetPhysicalBoneNode(int jointIdx)
```

引擎名：`get_physical_bone_node`

Returns the [`PhysicalBone2D`](PhysicalBone2D.md) node at `jointIdx`.

### SetPhysicalBoneChainLength

```csharp
public void SetPhysicalBoneChainLength(int length)
```

引擎名：`set_physical_bone_chain_length`

### SetPhysicalBoneNode

```csharp
public void SetPhysicalBoneNode(int jointIdx, NodePath physicalbone2DNode)
```

引擎名：`set_physical_bone_node`

Sets the [`PhysicalBone2D`](PhysicalBone2D.md) node at `jointIdx`.

**Note:** This is just the index used for this modification, not the bone index used in the [`Skeleton2D`](Skeleton2D.md).

### StartSimulation

```csharp
public void StartSimulation(Array<StringName> bones = null)
```

引擎名：`start_simulation`

Tell the [`PhysicalBone2D`](PhysicalBone2D.md) nodes to start simulating and interacting with the physics world.

Optionally, an array of bone names can be passed to this function, and that will cause only [`PhysicalBone2D`](PhysicalBone2D.md) nodes with those names to start simulating.

### StopSimulation

```csharp
public void StopSimulation(Array<StringName> bones = null)
```

引擎名：`stop_simulation`

Tell the [`PhysicalBone2D`](PhysicalBone2D.md) nodes to stop simulating and interacting with the physics world.

Optionally, an array of bone names can be passed to this function, and that will cause only [`PhysicalBone2D`](PhysicalBone2D.md) nodes with those names to stop simulating.
