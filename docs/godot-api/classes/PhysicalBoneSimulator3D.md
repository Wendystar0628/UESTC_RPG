<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# PhysicalBoneSimulator3D

命名空间 `Godot`；class；继承 [`SkeletonModifier3D`](SkeletonModifier3D.md) → [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Node that can be the parent of [`PhysicalBone3D`](PhysicalBone3D.md) and can apply the simulation results to [`Skeleton3D`](Skeleton3D.md).

## 构造函数

### PhysicalBoneSimulator3D

```csharp
public PhysicalBoneSimulator3D()
```

## 方法

### IsSimulatingPhysics

```csharp
public bool IsSimulatingPhysics()
```

引擎名：`is_simulating_physics`

Returns a boolean that indicates whether the `PhysicalBoneSimulator3D` is running and simulating.

### PhysicalBonesAddCollisionException

```csharp
public void PhysicalBonesAddCollisionException(Rid exception)
```

引擎名：`physical_bones_add_collision_exception`

Adds a collision exception to the physical bone.

Works just like the [`RigidBody3D`](RigidBody3D.md) node.

### PhysicalBonesRemoveCollisionException

```csharp
public void PhysicalBonesRemoveCollisionException(Rid exception)
```

引擎名：`physical_bones_remove_collision_exception`

Removes a collision exception to the physical bone.

Works just like the [`RigidBody3D`](RigidBody3D.md) node.

### PhysicalBonesStartSimulation

```csharp
public void PhysicalBonesStartSimulation(Array<StringName> bones = null)
```

引擎名：`physical_bones_start_simulation`

Tells the [`PhysicalBone3D`](PhysicalBone3D.md) nodes in the Skeleton to start simulating and reacting to the physics world.

Optionally, a list of bone names can be passed-in, allowing only the passed-in bones to be simulated.

### PhysicalBonesStopSimulation

```csharp
public void PhysicalBonesStopSimulation()
```

引擎名：`physical_bones_stop_simulation`

Tells the [`PhysicalBone3D`](PhysicalBone3D.md) nodes in the Skeleton to stop simulating.
