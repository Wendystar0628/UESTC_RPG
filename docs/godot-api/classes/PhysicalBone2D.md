<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicalBone2D

命名空间 `Godot`；class；继承 [`RigidBody2D`](RigidBody2D.md) → [`PhysicsBody2D`](PhysicsBody2D.md) → [`CollisionObject2D`](CollisionObject2D.md) → [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `PhysicalBone2D` node is a [`RigidBody2D`](RigidBody2D.md)-based node that can be used to make [`Bone2D`](Bone2D.md)s in a [`Skeleton2D`](Skeleton2D.md) react to physics.

**Note:** To make the [`Bone2D`](Bone2D.md)s visually follow the `PhysicalBone2D` node, use a [`SkeletonModification2DPhysicalBones`](SkeletonModification2DPhysicalBones.md) modification on the [`Skeleton2D`](Skeleton2D.md) parent.

**Note:** The `PhysicalBone2D` node does not automatically create a [`Joint2D`](Joint2D.md) node to keep `PhysicalBone2D` nodes together. They must be created manually. For most cases, you want to use a [`PinJoint2D`](PinJoint2D.md) node. The `PhysicalBone2D` node will automatically configure the [`Joint2D`](Joint2D.md) node once it's been added as a child node.

## 构造函数

### PhysicalBone2D

```csharp
public PhysicalBone2D()
```

## 属性

### AutoConfigureJoint

```csharp
public bool AutoConfigureJoint { get; set; }
```

引擎名：`auto_configure_joint`

If `true`, the `PhysicalBone2D` will automatically configure the first [`Joint2D`](Joint2D.md) child node. The automatic configuration is limited to setting up the node properties and positioning the [`Joint2D`](Joint2D.md).

### Bone2DIndex

```csharp
public int Bone2DIndex { get; set; }
```

The index of the [`Bone2D`](Bone2D.md) that this `PhysicalBone2D` should simulate.

### Bone2DNodePath

```csharp
public NodePath Bone2DNodePath { get; set; }
```

The [`NodePath`](NodePath.md) to the [`Bone2D`](Bone2D.md) that this `PhysicalBone2D` should simulate.

### FollowBoneWhenSimulating

```csharp
public bool FollowBoneWhenSimulating { get; set; }
```

引擎名：`follow_bone_when_simulating`

If `true`, the `PhysicalBone2D` will keep the transform of the bone it is bound to when simulating physics.

### SimulatePhysics

```csharp
public bool SimulatePhysics { get; set; }
```

引擎名：`simulate_physics`

If `true`, the `PhysicalBone2D` will start simulating using physics. If `false`, the `PhysicalBone2D` will follow the transform of the [`Bone2D`](Bone2D.md) node.

**Note:** To have the [`Bone2D`](Bone2D.md)s visually follow the `PhysicalBone2D`, use a [`SkeletonModification2DPhysicalBones`](SkeletonModification2DPhysicalBones.md) modification on the [`Skeleton2D`](Skeleton2D.md) node with the [`Bone2D`](Bone2D.md) nodes.

## 方法

### GetAutoConfigureJoint

```csharp
public bool GetAutoConfigureJoint()
```

引擎名：`get_auto_configure_joint`

### GetBone2DIndex

```csharp
public int GetBone2DIndex()
```

### GetBone2DNodePath

```csharp
public NodePath GetBone2DNodePath()
```

### GetFollowBoneWhenSimulating

```csharp
public bool GetFollowBoneWhenSimulating()
```

引擎名：`get_follow_bone_when_simulating`

### GetJoint

```csharp
public Joint2D GetJoint()
```

引擎名：`get_joint`

Returns the first [`Joint2D`](Joint2D.md) child node, if one exists. This is mainly a helper function to make it easier to get the [`Joint2D`](Joint2D.md) that the `PhysicalBone2D` is autoconfiguring.

### GetSimulatePhysics

```csharp
public bool GetSimulatePhysics()
```

引擎名：`get_simulate_physics`

### IsSimulatingPhysics

```csharp
public bool IsSimulatingPhysics()
```

引擎名：`is_simulating_physics`

Returns a boolean that indicates whether the `PhysicalBone2D` is running and simulating using the Godot 2D physics engine. When `true`, the PhysicalBone2D node is using physics.

### SetAutoConfigureJoint

```csharp
public void SetAutoConfigureJoint(bool autoConfigureJoint)
```

引擎名：`set_auto_configure_joint`

### SetBone2DIndex

```csharp
public void SetBone2DIndex(int boneIndex)
```

### SetBone2DNodePath

```csharp
public void SetBone2DNodePath(NodePath nodePath)
```

### SetFollowBoneWhenSimulating

```csharp
public void SetFollowBoneWhenSimulating(bool followBone)
```

引擎名：`set_follow_bone_when_simulating`

### SetSimulatePhysics

```csharp
public void SetSimulatePhysics(bool simulatePhysics)
```

引擎名：`set_simulate_physics`
