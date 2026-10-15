<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeStateMachine

命名空间 `Godot`；class；继承 [`AnimationRootNode`](AnimationRootNode.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Contains multiple [`AnimationRootNode`](AnimationRootNode.md)s representing animation states, connected in a graph. State transitions can be configured to happen automatically or via code, using a shortest-path algorithm. Retrieve the [`AnimationNodeStateMachinePlayback`](AnimationNodeStateMachinePlayback.md) object from the [`AnimationTree`](AnimationTree.md) node to control it programmatically.

```csharp
var stateMachine = GetNode<AnimationTree>("AnimationTree").Get("parameters/playback") as AnimationNodeStateMachinePlayback;
stateMachine.Travel("some_state");

```

## 构造函数

### AnimationNodeStateMachine

```csharp
public AnimationNodeStateMachine()
```

## 属性

### AllowTransitionToSelf

```csharp
public bool AllowTransitionToSelf { get; set; }
```

引擎名：`allow_transition_to_self`

If `true`, allows teleport to the self state with [`AnimationNodeStateMachinePlayback.Travel`](AnimationNodeStateMachinePlayback.md). When the reset option is enabled in [`AnimationNodeStateMachinePlayback.Travel`](AnimationNodeStateMachinePlayback.md), the animation is restarted. If `false`, nothing happens on the teleportation to the self state.

### ResetEnds

```csharp
public bool ResetEnds { get; set; }
```

引擎名：`reset_ends`

If `true`, treat the cross-fade to the start and end nodes as a blend with the RESET animation.

In most cases, when additional cross-fades are performed in the parent [`AnimationNode`](AnimationNode.md) of the state machine, setting this property to `false` and matching the cross-fade time of the parent [`AnimationNode`](AnimationNode.md) and the state machine's start node and end node gives good results.

### StateMachineType

```csharp
public AnimationNodeStateMachine.StateMachineTypeEnum StateMachineType { get; set; }
```

引擎名：`state_machine_type`

This property can define the process of transitions for different use cases. See also [`AnimationNodeStateMachine.StateMachineTypeEnum`](AnimationNodeStateMachine.md).

## 方法

### AddNode

```csharp
public void AddNode(StringName name, AnimationNode node, Vector2? position = null)
```

引擎名：`add_node`

Adds a new animation node to the graph. The `position` is used for display in the editor.

参数：
- `position`：If the parameter is null, then the default value is `new Vector2(0.0f, 0.0f)`.

### AddTransition

```csharp
public void AddTransition(StringName from, StringName to, AnimationNodeStateMachineTransition transition)
```

引擎名：`add_transition`

Adds a transition between the given animation nodes.

### AreEndsReset

```csharp
public bool AreEndsReset()
```

引擎名：`are_ends_reset`

### GetGraphOffset

```csharp
public Vector2 GetGraphOffset()
```

引擎名：`get_graph_offset`

Returns the draw offset of the graph. Used for display in the editor.

### GetNode

```csharp
public AnimationNode GetNode(StringName name)
```

引擎名：`get_node`

Returns the animation node with the given name.

### GetNodeList

```csharp
public Array<StringName> GetNodeList()
```

引擎名：`get_node_list`

Returns a list containing the names of all animation nodes in this state machine.

### GetNodeName

```csharp
public StringName GetNodeName(AnimationNode node)
```

引擎名：`get_node_name`

Returns the given animation node's name.

### GetNodePosition

```csharp
public Vector2 GetNodePosition(StringName name)
```

引擎名：`get_node_position`

Returns the given animation node's coordinates. Used for display in the editor.

### GetStateMachineType

```csharp
public AnimationNodeStateMachine.StateMachineTypeEnum GetStateMachineType()
```

引擎名：`get_state_machine_type`

### GetTransition

```csharp
public AnimationNodeStateMachineTransition GetTransition(int idx)
```

引擎名：`get_transition`

Returns the given transition.

### GetTransitionCount

```csharp
public int GetTransitionCount()
```

引擎名：`get_transition_count`

Returns the number of connections in the graph.

### GetTransitionFrom

```csharp
public StringName GetTransitionFrom(int idx)
```

引擎名：`get_transition_from`

Returns the given transition's start node.

### GetTransitionTo

```csharp
public StringName GetTransitionTo(int idx)
```

引擎名：`get_transition_to`

Returns the given transition's end node.

### HasNode

```csharp
public bool HasNode(StringName name)
```

引擎名：`has_node`

Returns `true` if the graph contains the given animation node.

### HasTransition

```csharp
public bool HasTransition(StringName from, StringName to)
```

引擎名：`has_transition`

Returns `true` if there is a transition between the given animation nodes.

### IsAllowTransitionToSelf

```csharp
public bool IsAllowTransitionToSelf()
```

引擎名：`is_allow_transition_to_self`

### RemoveNode

```csharp
public void RemoveNode(StringName name)
```

引擎名：`remove_node`

Deletes the given animation node from the graph.

### RemoveTransition

```csharp
public void RemoveTransition(StringName from, StringName to)
```

引擎名：`remove_transition`

Deletes the transition between the two specified animation nodes.

### RemoveTransitionByIndex

```csharp
public void RemoveTransitionByIndex(int idx)
```

引擎名：`remove_transition_by_index`

Deletes the given transition by index.

### RenameNode

```csharp
public void RenameNode(StringName name, StringName newName)
```

引擎名：`rename_node`

Renames the given animation node.

### ReplaceNode

```csharp
public void ReplaceNode(StringName name, AnimationNode node)
```

引擎名：`replace_node`

Replaces the given animation node with a new animation node.

### SetAllowTransitionToSelf

```csharp
public void SetAllowTransitionToSelf(bool enable)
```

引擎名：`set_allow_transition_to_self`

### SetGraphOffset

```csharp
public void SetGraphOffset(Vector2 offset)
```

引擎名：`set_graph_offset`

Sets the draw offset of the graph. Used for display in the editor.

### SetNodePosition

```csharp
public void SetNodePosition(StringName name, Vector2 position)
```

引擎名：`set_node_position`

Sets the animation node's coordinates. Used for display in the editor.

### SetResetEnds

```csharp
public void SetResetEnds(bool enable)
```

引擎名：`set_reset_ends`

### SetStateMachineType

```csharp
public void SetStateMachineType(AnimationNodeStateMachine.StateMachineTypeEnum stateMachineType)
```

引擎名：`set_state_machine_type`

## StateMachineTypeEnum

命名空间 `Godot`；enum

### 枚举值

- `Root = 0`：Seeking to the beginning is treated as playing from the start state.
- `Nested = 1`：Seeking to the beginning is treated as seeking to the beginning of the animation in the current state.
- `Grouped = 2`：This is a grouped state machine that can be controlled from a parent state machine.
