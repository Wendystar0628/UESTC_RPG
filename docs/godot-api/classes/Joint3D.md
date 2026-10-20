<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Joint3D

命名空间 `Godot`；class；继承 [`Node3D`](Node3D.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for all joints in 3D physics. 3D joints bind together two physics bodies (`NodeA` and `NodeB`) and apply a constraint. If only one body is defined, it is attached to a fixed [`StaticBody3D`](StaticBody3D.md) without collision shapes.

## 属性

### ExcludeNodesFromCollision

```csharp
public bool ExcludeNodesFromCollision { get; set; }
```

引擎名：`exclude_nodes_from_collision`

If `true`, the two bodies bound together do not collide with each other.

### NodeA

```csharp
public NodePath NodeA { get; set; }
```

引擎名：`node_a`

Path to the first node (A) attached to the joint. The node must inherit [`PhysicsBody3D`](PhysicsBody3D.md).

If left empty and `NodeB` is set, the body is attached to a fixed [`StaticBody3D`](StaticBody3D.md) without collision shapes.

### NodeB

```csharp
public NodePath NodeB { get; set; }
```

引擎名：`node_b`

Path to the second node (B) attached to the joint. The node must inherit [`PhysicsBody3D`](PhysicsBody3D.md).

If left empty and `NodeA` is set, the body is attached to a fixed [`StaticBody3D`](StaticBody3D.md) without collision shapes.

### SolverPriority

```csharp
public int SolverPriority { get; set; }
```

引擎名：`solver_priority`

The priority used to define which solver is executed first for multiple joints. The lower the value, the higher the priority.

**Note:** Only supported when using GodotPhysics3D. This property is ignored when using Jolt Physics.

## 方法

### GetExcludeNodesFromCollision

```csharp
public bool GetExcludeNodesFromCollision()
```

引擎名：`get_exclude_nodes_from_collision`

### GetNodeA

```csharp
public NodePath GetNodeA()
```

引擎名：`get_node_a`

### GetNodeB

```csharp
public NodePath GetNodeB()
```

引擎名：`get_node_b`

### GetRid

```csharp
public Rid GetRid()
```

引擎名：`get_rid`

Returns the joint's internal [`Rid`](Rid.md) from the [`PhysicsServer3D`](PhysicsServer3D.md).

### GetSolverPriority

```csharp
public int GetSolverPriority()
```

引擎名：`get_solver_priority`

### SetExcludeNodesFromCollision

```csharp
public void SetExcludeNodesFromCollision(bool enable)
```

引擎名：`set_exclude_nodes_from_collision`

### SetNodeA

```csharp
public void SetNodeA(NodePath node)
```

引擎名：`set_node_a`

### SetNodeB

```csharp
public void SetNodeB(NodePath node)
```

引擎名：`set_node_b`

### SetSolverPriority

```csharp
public void SetSolverPriority(int priority)
```

引擎名：`set_solver_priority`
