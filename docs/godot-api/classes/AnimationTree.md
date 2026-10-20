<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationTree

命名空间 `Godot`；class；继承 [`AnimationMixer`](AnimationMixer.md) → [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

A node used for advanced animation transitions in an [`AnimationPlayer`](AnimationPlayer.md).

**Note:** When linked with an [`AnimationPlayer`](AnimationPlayer.md), several properties and methods of the corresponding [`AnimationPlayer`](AnimationPlayer.md) will not function as expected. Playback and transitions should be handled using only the `AnimationTree` and its constituent [`AnimationNode`](AnimationNode.md)(s). The [`AnimationPlayer`](AnimationPlayer.md) node should be used solely for adding, deleting, and editing animations.

## 构造函数

### AnimationTree

```csharp
public AnimationTree()
```

## 属性

### AdvanceExpressionBaseNode

```csharp
public NodePath AdvanceExpressionBaseNode { get; set; }
```

引擎名：`advance_expression_base_node`

The path to the [`Node`](Node.md) used to evaluate the [`AnimationNode`](AnimationNode.md) [`Expression`](Expression.md) if one is not explicitly specified internally.

### AnimPlayer

```csharp
public NodePath AnimPlayer { get; set; }
```

引擎名：`anim_player`

The path to the [`AnimationPlayer`](AnimationPlayer.md) used for animating.

### ProcessCallback

```csharp
public AnimationTree.AnimationProcessCallback ProcessCallback { get; set; }
```

The process notification in which to update animations.

### TreeRoot

```csharp
public AnimationRootNode TreeRoot { get; set; }
```

引擎名：`tree_root`

The root animation node of this `AnimationTree`. See [`AnimationRootNode`](AnimationRootNode.md).

## 方法

### GetAdvanceExpressionBaseNode

```csharp
public NodePath GetAdvanceExpressionBaseNode()
```

引擎名：`get_advance_expression_base_node`

### GetAnimationPlayer

```csharp
public NodePath GetAnimationPlayer()
```

引擎名：`get_animation_player`

### GetProcessCallback

```csharp
public AnimationTree.AnimationProcessCallback GetProcessCallback()
```

引擎名：`get_process_callback`

Returns the process notification in which to update animations.

### GetTreeRoot

```csharp
public AnimationRootNode GetTreeRoot()
```

引擎名：`get_tree_root`

### SetAdvanceExpressionBaseNode

```csharp
public void SetAdvanceExpressionBaseNode(NodePath path)
```

引擎名：`set_advance_expression_base_node`

### SetAnimationPlayer

```csharp
public void SetAnimationPlayer(NodePath path)
```

引擎名：`set_animation_player`

### SetProcessCallback

```csharp
public void SetProcessCallback(AnimationTree.AnimationProcessCallback mode)
```

引擎名：`set_process_callback`

Sets the process notification in which to update animations.

### SetTreeRoot

```csharp
public void SetTreeRoot(AnimationNode root)
```

引擎名：`set_tree_root`

### SetTreeRoot

```csharp
public void SetTreeRoot(AnimationRootNode animationNode)
```

引擎名：`set_tree_root`

## 事件（信号）

### AnimationPlayerChanged

```csharp
public event Action AnimationPlayerChanged
```

引擎名：`animation_player_changed`

Emitted when the `AnimPlayer` is changed.

## AnimationProcessCallback

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`
- `Idle = 1`
- `Manual = 2`
