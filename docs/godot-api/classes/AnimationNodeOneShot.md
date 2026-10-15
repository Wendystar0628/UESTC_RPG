<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AnimationNodeOneShot

命名空间 `Godot`；class；继承 [`AnimationNodeSync`](AnimationNodeSync.md) → [`AnimationNode`](AnimationNode.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A resource to add to an [`AnimationNodeBlendTree`](AnimationNodeBlendTree.md). This animation node will execute a sub-animation and return once it finishes. Blend times for fading in and out can be customized, as well as filters.

After setting the request and changing the animation playback, the one-shot node automatically clears the request on the next process frame by setting its `request` value to [`AnimationNodeOneShot.OneShotRequest.None`](AnimationNodeOneShot.md).

```csharp
// Play child animation connected to "shot" port.
animationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

// Abort child animation connected to "shot" port.
animationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.Abort);

// Abort child animation with fading out connected to "shot" port.
animationTree.Set("parameters/OneShot/request", (int)AnimationNodeOneShot.OneShotRequest.FadeOut);

// Get current state (read-only).
animationTree.Get("parameters/OneShot/active");

// Get current internal state (read-only).
animationTree.Get("parameters/OneShot/internal_active");

```

## 构造函数

### AnimationNodeOneShot

```csharp
public AnimationNodeOneShot()
```

## 属性

### AbortOnReset

```csharp
public bool AbortOnReset { get; set; }
```

引擎名：`abort_on_reset`

If `true`, the sub-animation will abort if resumed with a reset after a prior interruption.

### Autorestart

```csharp
public bool Autorestart { get; set; }
```

引擎名：`autorestart`

If `true`, the sub-animation will restart automatically after finishing.

In other words, to start auto restarting, the animation must be played once with the [`AnimationNodeOneShot.OneShotRequest.Fire`](AnimationNodeOneShot.md) request. The [`AnimationNodeOneShot.OneShotRequest.Abort`](AnimationNodeOneShot.md) request stops the auto restarting, but it does not disable the `Autorestart` itself. So, the [`AnimationNodeOneShot.OneShotRequest.Fire`](AnimationNodeOneShot.md) request will start auto restarting again.

### AutorestartDelay

```csharp
public double AutorestartDelay { get; set; }
```

引擎名：`autorestart_delay`

The delay after which the automatic restart is triggered, in seconds.

### AutorestartRandomDelay

```csharp
public double AutorestartRandomDelay { get; set; }
```

引擎名：`autorestart_random_delay`

If `Autorestart` is `true`, a random additional delay (in seconds) between 0 and this value will be added to `AutorestartDelay`.

### BreakLoopAtEnd

```csharp
public bool BreakLoopAtEnd { get; set; }
```

引擎名：`break_loop_at_end`

If `true`, breaks the loop at the end of the loop cycle for transition, even if the animation is looping.

### FadeInCurve

```csharp
public Curve FadeInCurve { get; set; }
```

Determines how cross-fading between animations is eased. If empty, the transition will be linear. Should be a unit [`Curve`](Curve.md).

### FadeInTime

```csharp
public double FadeInTime { get; set; }
```

The fade-in duration. For example, setting this to `1.0` for a 5 second length animation will produce a cross-fade that starts at 0 second and ends at 1 second during the animation.

**Note:** `AnimationNodeOneShot` transitions the current state after the fading has finished.

### FadeOutCurve

```csharp
public Curve FadeOutCurve { get; set; }
```

Determines how cross-fading between animations is eased. If empty, the transition will be linear. Should be a unit [`Curve`](Curve.md).

### FadeOutTime

```csharp
public double FadeOutTime { get; set; }
```

The fade-out duration. For example, setting this to `1.0` for a 5 second length animation will produce a cross-fade that starts at 4 second and ends at 5 second during the animation.

**Note:** `AnimationNodeOneShot` transitions the current state after the fading has finished.

### MixMode

```csharp
public AnimationNodeOneShot.MixModeEnum MixMode { get; set; }
```

引擎名：`mix_mode`

The blend type.

## 方法

### GetAutorestartDelay

```csharp
public double GetAutorestartDelay()
```

引擎名：`get_autorestart_delay`

### GetAutorestartRandomDelay

```csharp
public double GetAutorestartRandomDelay()
```

引擎名：`get_autorestart_random_delay`

### GetFadeInCurve

```csharp
public Curve GetFadeInCurve()
```

### GetFadeInTime

```csharp
public double GetFadeInTime()
```

### GetFadeOutCurve

```csharp
public Curve GetFadeOutCurve()
```

### GetFadeOutTime

```csharp
public double GetFadeOutTime()
```

### GetMixMode

```csharp
public AnimationNodeOneShot.MixModeEnum GetMixMode()
```

引擎名：`get_mix_mode`

### HasAutorestart

```csharp
public bool HasAutorestart()
```

引擎名：`has_autorestart`

### IsAbortedOnReset

```csharp
public bool IsAbortedOnReset()
```

引擎名：`is_aborted_on_reset`

### IsLoopBrokenAtEnd

```csharp
public bool IsLoopBrokenAtEnd()
```

引擎名：`is_loop_broken_at_end`

### SetAbortOnReset

```csharp
public void SetAbortOnReset(bool enable)
```

引擎名：`set_abort_on_reset`

### SetAutorestart

```csharp
public void SetAutorestart(bool active)
```

引擎名：`set_autorestart`

### SetAutorestartDelay

```csharp
public void SetAutorestartDelay(double time)
```

引擎名：`set_autorestart_delay`

### SetAutorestartRandomDelay

```csharp
public void SetAutorestartRandomDelay(double time)
```

引擎名：`set_autorestart_random_delay`

### SetBreakLoopAtEnd

```csharp
public void SetBreakLoopAtEnd(bool enable)
```

引擎名：`set_break_loop_at_end`

### SetFadeInCurve

```csharp
public void SetFadeInCurve(Curve curve)
```

### SetFadeInTime

```csharp
public void SetFadeInTime(double time)
```

### SetFadeOutCurve

```csharp
public void SetFadeOutCurve(Curve curve)
```

### SetFadeOutTime

```csharp
public void SetFadeOutTime(double time)
```

### SetMixMode

```csharp
public void SetMixMode(AnimationNodeOneShot.MixModeEnum mode)
```

引擎名：`set_mix_mode`

## MixModeEnum

命名空间 `Godot`；enum

### 枚举值

- `Blend = 0`：Blends two animations.
- `Add = 1`：Blends two animations additively.

## OneShotRequest

命名空间 `Godot`；enum

### 枚举值

- `None = 0`：The default state of the request.
- `Fire = 1`：The request to play the animation connected to "shot" port.
- `Abort = 2`：The request to stop the animation connected to "shot" port.
- `FadeOut = 3`：The request to fade out the animation connected to "shot" port.
