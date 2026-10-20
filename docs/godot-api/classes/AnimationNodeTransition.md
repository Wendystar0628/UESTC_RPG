<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeTransition

命名空间 `Godot`；class；继承 [`AnimationNodeSync`](AnimationNodeSync.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Simple state machine for cases which don't require a more advanced [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md). Animations can be connected to the inputs and transition times can be specified.

After setting the request and changing the animation playback, the transition node automatically clears the request on the next process frame by setting its `transition_request` value to empty.

**Note:** When using a cross-fade, `current_state` and `current_index` change to the next state immediately after the cross-fade begins.

```csharp
// Play child animation connected to "state_2" port.
animationTree.Set("parameters/Transition/transition_request", "state_2");

// Get current state name (read-only).
animationTree.Get("parameters/Transition/current_state");

// Get current state index (read-only).
animationTree.Get("parameters/Transition/current_index");

```

## 构造函数

### AnimationNodeTransition

```csharp
public AnimationNodeTransition()
```

## 属性

### AllowTransitionToSelf

```csharp
public bool AllowTransitionToSelf { get; set; }
```

引擎名：`allow_transition_to_self`

If `true`, allows transition to the self state. When the reset option is enabled in input, the animation is restarted. If `false`, nothing happens on the transition to the self state.

### InputCount

```csharp
public int InputCount { get; set; }
```

引擎名：`input_count`

The number of enabled input ports for this animation node.

### XfadeCurve

```csharp
public Curve XfadeCurve { get; set; }
```

引擎名：`xfade_curve`

Determines how cross-fading between animations is eased. If empty, the transition will be linear. Should be a unit [`Curve`](Curve.md).

### XfadeTime

```csharp
public double XfadeTime { get; set; }
```

引擎名：`xfade_time`

Cross-fading time (in seconds) between each animation connected to the inputs.

**Note:** `AnimationNodeTransition` transitions the current state immediately after the start of the fading. The precise remaining time can only be inferred from the main animation. When [`AnimationNodeOutput`](AnimationNodeOutput.md) is considered as the most upstream, so the `XfadeTime` is not scaled depending on the downstream delta. See also [`AnimationNodeOneShot.FadeOutTime`](AnimationNodeOneShot.md).

## 方法

### GetXfadeCurve

```csharp
public Curve GetXfadeCurve()
```

引擎名：`get_xfade_curve`

### GetXfadeTime

```csharp
public double GetXfadeTime()
```

引擎名：`get_xfade_time`

### IsAllowTransitionToSelf

```csharp
public bool IsAllowTransitionToSelf()
```

引擎名：`is_allow_transition_to_self`

### IsInputLoopBrokenAtEnd

```csharp
public bool IsInputLoopBrokenAtEnd(int input)
```

引擎名：`is_input_loop_broken_at_end`

Returns whether the animation breaks the loop at the end of the loop cycle for transition.

### IsInputReset

```csharp
public bool IsInputReset(int input)
```

引擎名：`is_input_reset`

Returns whether the animation restarts when the animation transitions from the other animation.

### IsInputSetAsAutoAdvance

```csharp
public bool IsInputSetAsAutoAdvance(int input)
```

引擎名：`is_input_set_as_auto_advance`

Returns `true` if auto-advance is enabled for the given `input` index.

### SetAllowTransitionToSelf

```csharp
public void SetAllowTransitionToSelf(bool enable)
```

引擎名：`set_allow_transition_to_self`

### SetInputAsAutoAdvance

```csharp
public void SetInputAsAutoAdvance(int input, bool enable)
```

引擎名：`set_input_as_auto_advance`

Enables or disables auto-advance for the given `input` index. If enabled, state changes to the next input after playing the animation once. If enabled for the last input state, it loops to the first.

### SetInputBreakLoopAtEnd

```csharp
public void SetInputBreakLoopAtEnd(int input, bool enable)
```

引擎名：`set_input_break_loop_at_end`

If `true`, breaks the loop at the end of the loop cycle for transition, even if the animation is looping.

### SetInputCount

```csharp
public void SetInputCount(int inputCount)
```

引擎名：`set_input_count`

### SetInputReset

```csharp
public void SetInputReset(int input, bool enable)
```

引擎名：`set_input_reset`

If `true`, the destination animation is restarted when the animation transitions.

### SetXfadeCurve

```csharp
public void SetXfadeCurve(Curve curve)
```

引擎名：`set_xfade_curve`

### SetXfadeTime

```csharp
public void SetXfadeTime(double time)
```

引擎名：`set_xfade_time`
