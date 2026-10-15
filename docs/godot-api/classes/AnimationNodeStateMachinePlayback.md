<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeStateMachinePlayback

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Allows control of [`AnimationTree`](AnimationTree.md) state machines created with [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md). Retrieve with `$AnimationTree.get("parameters/playback")`.

```csharp
var stateMachine = GetNode<AnimationTree>("AnimationTree").Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
stateMachine.Travel("some_state");

```

## 构造函数

### AnimationNodeStateMachinePlayback

```csharp
public AnimationNodeStateMachinePlayback()
```

## 方法

### GetCurrentLength

```csharp
public float GetCurrentLength()
```

引擎名：`get_current_length`

Returns the current state length.

**Note:** It is possible that any [`AnimationRootNode`](AnimationRootNode.md) can be nodes as well as animations. This means that there can be multiple animations within a single state. Which animation length has priority depends on the nodes connected inside it. Also, if a transition does not reset, the remaining length at that point will be returned.

### GetCurrentNode

```csharp
public StringName GetCurrentNode()
```

引擎名：`get_current_node`

Returns the currently playing animation state.

**Note:** When using a cross-fade, the current state changes to the next state immediately after the cross-fade begins.

### GetCurrentPlayPosition

```csharp
public float GetCurrentPlayPosition()
```

引擎名：`get_current_play_position`

Returns the playback position within the current animation state.

### GetFadingFromLength

```csharp
public float GetFadingFromLength()
```

引擎名：`get_fading_from_length`

Returns the playback state length of the node from `GetFadingFromNode()`. Returns `0` if no animation fade is occurring.

### GetFadingFromNode

```csharp
public StringName GetFadingFromNode()
```

引擎名：`get_fading_from_node`

Returns the starting state of currently fading animation.

### GetFadingFromPlayPosition

```csharp
public float GetFadingFromPlayPosition()
```

引擎名：`get_fading_from_play_position`

Returns the playback position of the node from `GetFadingFromNode()`. Returns `0` if no animation fade is occurring.

### GetFadingLength

```csharp
public float GetFadingLength()
```

引擎名：`get_fading_length`

Returns the length of the current fade animation. Returns `0` if no animation fade is occurring.

### GetFadingPosition

```csharp
public float GetFadingPosition()
```

引擎名：`get_fading_position`

Returns the playback position of the current fade animation. Returns `0` if no animation fade is occurring.

### GetTravelPath

```csharp
public Array<StringName> GetTravelPath()
```

引擎名：`get_travel_path`

Returns the current travel path as computed internally by the A* algorithm.

### IsPlaying

```csharp
public bool IsPlaying()
```

引擎名：`is_playing`

Returns `true` if an animation is playing.

### Next

```csharp
public void Next()
```

引擎名：`next`

If there is a next path by travel or auto advance, immediately transitions from the current state to the next state.

### Start

```csharp
public void Start(StringName node, bool reset = true)
```

引擎名：`start`

Starts playing the given animation.

If `reset` is `true`, the animation is played from the beginning.

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the currently playing animation.

### Travel

```csharp
public void Travel(StringName toNode, bool resetOnTeleport = true)
```

引擎名：`travel`

Transitions from the current state to another one, following the shortest path.

If the path does not connect from the current state, the animation will play after the state teleports.

If `resetOnTeleport` is `true`, the animation is played from the beginning when the travel cause a teleportation.

## 事件（信号）

### StateFinished

```csharp
public event AnimationNodeStateMachinePlayback.StateFinishedEventHandler StateFinished
```

引擎名：`state_finished`

Emitted when the `state` finishes playback. If `state` is a state machine set to grouped mode, its signals are passed through with its name prefixed.

If there is a crossfade, this will be fired when the influence of the `GetFadingFromNode()` animation is no longer present.

### StateStarted

```csharp
public event AnimationNodeStateMachinePlayback.StateStartedEventHandler StateStarted
```

引擎名：`state_started`

Emitted when the `state` starts playback. If `state` is a state machine set to grouped mode, its signals are passed through with its name prefixed.

## StateFinishedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationNodeStateMachinePlayback.StateFinished`](AnimationNodeStateMachinePlayback.md) event of a [`AnimationNodeStateMachinePlayback`](AnimationNodeStateMachinePlayback.md) class.

### 签名

#### Invoke

```csharp
delegate void StateFinishedEventHandler(StringName state)
```

## StateStartedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`AnimationNodeStateMachinePlayback.StateStarted`](AnimationNodeStateMachinePlayback.md) event of a [`AnimationNodeStateMachinePlayback`](AnimationNodeStateMachinePlayback.md) class.

### 签名

#### Invoke

```csharp
delegate void StateStartedEventHandler(StringName state)
```
