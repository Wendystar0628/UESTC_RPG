<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeStateMachineTransition

命名空间 `Godot`；class；继承 [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

The path generated when using [`AnimationNodeStateMachinePlayback.Travel`](AnimationNodeStateMachinePlayback.md) is limited to the nodes connected by `AnimationNodeStateMachineTransition`.

You can set the timing and conditions of the transition in detail.

## 构造函数

### AnimationNodeStateMachineTransition

```csharp
public AnimationNodeStateMachineTransition()
```

## 属性

### AdvanceCondition

```csharp
public StringName AdvanceCondition { get; set; }
```

引擎名：`advance_condition`

Turn on auto advance when this condition is set. The provided name will become a boolean parameter on the [`AnimationTree`](AnimationTree.md) that can be controlled from code (see [Using AnimationTree]($DOCS_URL/tutorials/animation/animation_tree.html#controlling-from-code)). For example, if [`AnimationTree.TreeRoot`](AnimationTree.md) is an [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md) and `AdvanceCondition` is set to `"idle"`:

```csharp
GetNode<AnimationTree>("animation_tree").Set("parameters/conditions/idle", IsOnFloor && (LinearVelocity.X == 0));

```

### AdvanceExpression

```csharp
public string AdvanceExpression { get; set; }
```

引擎名：`advance_expression`

Use an expression as a condition for state machine transitions. It is possible to create complex animation advance conditions for switching between states and gives much greater flexibility for creating complex state machines by directly interfacing with the script code.

### AdvanceMode

```csharp
public AnimationNodeStateMachineTransition.AdvanceModeEnum AdvanceMode { get; set; }
```

引擎名：`advance_mode`

Determines whether the transition should be disabled, enabled when using [`AnimationNodeStateMachinePlayback.Travel`](AnimationNodeStateMachinePlayback.md), or traversed automatically if the `AdvanceCondition` and `AdvanceExpression` checks are `true` (if assigned).

### BreakLoopAtEnd

```csharp
public bool BreakLoopAtEnd { get; set; }
```

引擎名：`break_loop_at_end`

If `true`, breaks the loop at the end of the loop cycle for transition, even if the animation is looping.

### Priority

```csharp
public int Priority { get; set; }
```

引擎名：`priority`

Lower priority transitions are preferred when travelling through the tree via [`AnimationNodeStateMachinePlayback.Travel`](AnimationNodeStateMachinePlayback.md) or `AdvanceMode` is set to [`AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto`](AnimationNodeStateMachineTransition.md).

### Reset

```csharp
public bool Reset { get; set; }
```

引擎名：`reset`

If `true`, the destination animation is played back from the beginning when switched.

### SwitchMode

```csharp
public AnimationNodeStateMachineTransition.SwitchModeEnum SwitchMode { get; set; }
```

引擎名：`switch_mode`

The transition type.

### XfadeCurve

```csharp
public Curve XfadeCurve { get; set; }
```

引擎名：`xfade_curve`

Ease curve for better control over cross-fade between this state and the next. Should be a unit [`Curve`](Curve.md).

### XfadeTime

```csharp
public float XfadeTime { get; set; }
```

引擎名：`xfade_time`

The time to cross-fade between this state and the next.

**Note:** [`AnimationNodeStateMachine`](AnimationNodeStateMachine.md) transitions the current state immediately after the start of the fading. The precise remaining time can only be inferred from the main animation. When [`AnimationNodeOutput`](AnimationNodeOutput.md) is considered as the most upstream, so the `XfadeTime` is not scaled depending on the downstream delta. See also [`AnimationNodeOneShot.FadeOutTime`](AnimationNodeOneShot.md).

## 方法

### GetAdvanceCondition

```csharp
public StringName GetAdvanceCondition()
```

引擎名：`get_advance_condition`

### GetAdvanceExpression

```csharp
public string GetAdvanceExpression()
```

引擎名：`get_advance_expression`

### GetAdvanceMode

```csharp
public AnimationNodeStateMachineTransition.AdvanceModeEnum GetAdvanceMode()
```

引擎名：`get_advance_mode`

### GetPriority

```csharp
public int GetPriority()
```

引擎名：`get_priority`

### GetSwitchMode

```csharp
public AnimationNodeStateMachineTransition.SwitchModeEnum GetSwitchMode()
```

引擎名：`get_switch_mode`

### GetXfadeCurve

```csharp
public Curve GetXfadeCurve()
```

引擎名：`get_xfade_curve`

### GetXfadeTime

```csharp
public float GetXfadeTime()
```

引擎名：`get_xfade_time`

### IsLoopBrokenAtEnd

```csharp
public bool IsLoopBrokenAtEnd()
```

引擎名：`is_loop_broken_at_end`

### IsReset

```csharp
public bool IsReset()
```

引擎名：`is_reset`

### SetAdvanceCondition

```csharp
public void SetAdvanceCondition(StringName name)
```

引擎名：`set_advance_condition`

### SetAdvanceExpression

```csharp
public void SetAdvanceExpression(string text)
```

引擎名：`set_advance_expression`

### SetAdvanceMode

```csharp
public void SetAdvanceMode(AnimationNodeStateMachineTransition.AdvanceModeEnum mode)
```

引擎名：`set_advance_mode`

### SetBreakLoopAtEnd

```csharp
public void SetBreakLoopAtEnd(bool enable)
```

引擎名：`set_break_loop_at_end`

### SetPriority

```csharp
public void SetPriority(int priority)
```

引擎名：`set_priority`

### SetReset

```csharp
public void SetReset(bool reset)
```

引擎名：`set_reset`

### SetSwitchMode

```csharp
public void SetSwitchMode(AnimationNodeStateMachineTransition.SwitchModeEnum mode)
```

引擎名：`set_switch_mode`

### SetXfadeCurve

```csharp
public void SetXfadeCurve(Curve curve)
```

引擎名：`set_xfade_curve`

### SetXfadeTime

```csharp
public void SetXfadeTime(float secs)
```

引擎名：`set_xfade_time`

## 事件（信号）

### AdvanceConditionChanged

```csharp
public event Action AdvanceConditionChanged
```

引擎名：`advance_condition_changed`

Emitted when `AdvanceCondition` is changed.

## AdvanceModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Disabled = 0`：Don't use this transition.
- `Enabled = 1`：Only use this transition during [`AnimationNodeStateMachinePlayback.Travel`](AnimationNodeStateMachinePlayback.md).
- `Auto = 2`：Automatically use this transition if the [`AnimationNodeStateMachineTransition.AdvanceCondition`](AnimationNodeStateMachineTransition.md) and [`AnimationNodeStateMachineTransition.AdvanceExpression`](AnimationNodeStateMa…

## SwitchModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Immediate = 0`：Switch to the next state immediately.
- `Sync = 1`：Switch to the next state immediately, but will seek the new state to the playback position of the old state.
- `AtEnd = 2`：Wait for the current state playback to end, then switch to the beginning of the next state animation.
