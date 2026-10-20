<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Joint2D

命名空间 `Godot`；class；继承 [`Node2D`](Node2D.md) → [`CanvasItem`](CanvasItem.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

Abstract base class for all joints in 2D physics. 2D joints bind together two physics bodies (`NodeA` and `NodeB`) and apply a constraint.

## 属性

### Bias

```csharp
public float Bias { get; set; }
```

引擎名：`bias`

When `NodeA` and `NodeB` move in different directions the `Bias` controls how fast the joint pulls them back to their original position. The lower the `Bias` the more the two bodies can pull on the joint.

When set to `0`, the default value from `ProjectSettings.physics/2d/solver/default_constraint_bias` is used.

### DisableCollision

```csharp
public bool DisableCollision { get; set; }
```

引擎名：`disable_collision`

If `true`, the two bodies bound together do not collide with each other.

### NodeA

```csharp
public NodePath NodeA { get; set; }
```

引擎名：`node_a`

Path to the first body (A) attached to the joint. The node must inherit [`PhysicsBody2D`](PhysicsBody2D.md).

### NodeB

```csharp
public NodePath NodeB { get; set; }
```

引擎名：`node_b`

Path to the second body (B) attached to the joint. The node must inherit [`PhysicsBody2D`](PhysicsBody2D.md).

## 方法

### GetBias

```csharp
public float GetBias()
```

引擎名：`get_bias`

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

Returns the joint's internal [`Rid`](Rid.md) from the [`PhysicsServer2D`](PhysicsServer2D.md).

### SetBias

```csharp
public void SetBias(float bias)
```

引擎名：`set_bias`

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
