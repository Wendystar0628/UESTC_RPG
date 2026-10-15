<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Tween

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

Tweens are mostly useful for animations requiring a numerical property to be interpolated over a range of values. The name *tween* comes from *in-betweening*, an animation technique where you specify *keyframes* and the computer interpolates the frames that appear between them. Animating something with a `Tween` is called tweening.

`Tween` is more suited than [`AnimationPlayer`](AnimationPlayer.md) for animations where you don't know the final values in advance. For example, interpolating a dynamically-chosen camera zoom value is best done with a `Tween`; it would be difficult to do the same thing with an [`AnimationPlayer`](AnimationPlayer.md) node. Tweens are also more light-weight than [`AnimationPlayer`](AnimationPlayer.md), so they are very much suited for simple animations or general tasks that don't require visual tweaking provided by the editor. They can be used in a "fire-and-forget" manner for some logic that normally would be done by code. You can e.g. make something shoot periodically by using a looped [`CallbackTweener`](CallbackTweener.md) with a delay.

A `Tween` can be created by using either [`SceneTree.CreateTween`](SceneTree.md) or [`Node.CreateTween`](Node.md). `Tween`s created manually (i.e. by using `Tween.new()`) are invalid and can't be used for tweening values.

A tween animation is created by adding [`Tweener`](Tweener.md)s to the `Tween` object, using `TweenProperty()`, `TweenInterval()`, `TweenCallback()`, `TweenMethod()`, `TweenSubtween()`, or `TweenAwait()`:

```csharp
Tween tween = GetTree().CreateTween();
tween.TweenProperty(GetNode("Sprite"), "modulate", Colors.Red, 1.0f);
tween.TweenProperty(GetNode("Sprite"), "scale", Vector2.Zero, 1.0f);
tween.TweenCallback(Callable.From(GetNode("Sprite").QueueFree));

```

This sequence will make the `$Sprite` node turn red, then shrink, before finally calling [`Node.QueueFree`](Node.md) to free the sprite. [`Tweener`](Tweener.md)s are executed one after another by default. This behavior can be changed using `Parallel()` and `SetParallel()`.

When a [`Tweener`](Tweener.md) is created with one of the `tween_*` methods, a chained method call can be used to tweak the properties of this [`Tweener`](Tweener.md). For example, if you want to set a different transition type in the above example, you can use `SetTrans()`:

```csharp
Tween tween = GetTree().CreateTween();
tween.TweenProperty(GetNode("Sprite"), "modulate", Colors.Red, 1.0f).SetTrans(Tween.TransitionType.Sine);
tween.TweenProperty(GetNode("Sprite"), "scale", Vector2.Zero, 1.0f).SetTrans(Tween.TransitionType.Bounce);
tween.TweenCallback(Callable.From(GetNode("Sprite").QueueFree));

```

Most of the `Tween` methods can be chained this way too. In the following example the `Tween` is bound to the running script's node and a default transition is set for its [`Tweener`](Tweener.md)s:

```csharp
var tween = GetTree().CreateTween().BindNode(this).SetTrans(Tween.TransitionType.Elastic);
tween.TweenProperty(GetNode("Sprite"), "modulate", Colors.Red, 1.0f);
tween.TweenProperty(GetNode("Sprite"), "scale", Vector2.Zero, 1.0f);
tween.TweenCallback(Callable.From(GetNode("Sprite").QueueFree));

```

Another interesting use for `Tween`s is animating arbitrary sets of objects:

```csharp
Tween tween = CreateTween();
foreach (Node sprite in GetChildren())
tween.TweenProperty(sprite, "position", Vector2.Zero, 1.0f);

```

In the example above, all children of a node are moved one after another to position `(0, 0)`.

You should avoid using more than one `Tween` per object's property. If two or more tweens animate one property at the same time, the last one created will take priority and assign the final value. If you want to interrupt and restart an animation, consider assigning the `Tween` to a variable:

```csharp
private Tween _tween;

public void Animate()
{
if (_tween != null)
_tween.Kill(); // Abort the previous animation
_tween = CreateTween();
}

```

Some [`Tweener`](Tweener.md)s use transitions and eases. The first accepts a [`Tween.TransitionType`](Tween.md) constant, and refers to the way the timing of the animation is handled (see [easings.net](https://easings.net/) for some examples). The second accepts an [`Tween.EaseType`](Tween.md) constant, and controls where the `trans_type` is applied to the interpolation (in the beginning, the end, or both). If you don't know which transition and easing to pick, you can try different [`Tween.TransitionType`](Tween.md) constants with [`Tween.EaseType.InOut`](Tween.md), and use the one that looks best.

[Tween easing and transition types cheatsheet](https://raw.githubusercontent.com/godotengine/godot-docs/master/img/tween_cheatsheet.webp)

**Note:** Tweens are not designed to be reused and trying to do so results in an undefined behavior. Create a new Tween for each animation and every time you replay an animation from start. Keep in mind that Tweens start immediately, so only create a Tween when you want to start animating.

**Note:** The tween is processed after all of the nodes in the current frame, i.e. node's [`Node._Process()`](Node.md) method would be called before the tween (or [`Node._PhysicsProcess()`](Node.md) depending on the value passed to `SetProcessMode()`).

## 构造函数

### Tween

```csharp
public Tween()
```

## 方法

### BindNode

```csharp
public Tween BindNode(Node node)
```

引擎名：`bind_node`

Binds this `Tween` with the given `node`. `Tween`s are processed directly by the [`SceneTree`](SceneTree.md), so they run independently of the animated nodes. When you bind a [`Node`](Node.md) with the `Tween`, the `Tween` will halt the animation when the object is not inside tree and the `Tween` will be automatically killed when the bound object is freed. Also [`Tween.TweenPauseMode.Bound`](Tween.md) will make the pausing behavior dependent on the bound node.

For a shorter way to create and bind a `Tween`, you can use [`Node.CreateTween`](Node.md).

### Chain

```csharp
public Tween Chain()
```

引擎名：`chain`

Used to chain two [`Tweener`](Tweener.md)s after `SetParallel()` is called with `true`.

```csharp
Tween tween = CreateTween().SetParallel(true);
tween.TweenProperty(...);
tween.TweenProperty(...); // Will run parallelly with above.
tween.Chain().TweenProperty(...); // Will run after two above are finished.

```

### CustomStep

```csharp
public bool CustomStep(double delta)
```

引擎名：`custom_step`

Processes the `Tween` by the given `delta` value, in seconds. This is mostly useful for manual control when the `Tween` is paused. It can also be used to end the `Tween` animation immediately, by setting `delta` longer than the whole duration of the `Tween` animation.

Returns `true` if the `Tween` still has [`Tweener`](Tweener.md)s that haven't finished.

### GetLoopsLeft

```csharp
public int GetLoopsLeft()
```

引擎名：`get_loops_left`

Returns the number of remaining loops for this `Tween` (see `SetLoops()`). A return value of `-1` indicates an infinitely looping `Tween`, and a return value of `0` indicates that the `Tween` has already finished.

### GetTotalElapsedTime

```csharp
public double GetTotalElapsedTime()
```

引擎名：`get_total_elapsed_time`

Returns the total time in seconds the `Tween` has been animating (i.e. the time since it started, not counting pauses etc.). The time is affected by `SetSpeedScale()`, and `Stop()` will reset it to `0`.

**Note:** As it results from accumulating frame deltas, the time returned after the `Tween` has finished animating will be slightly greater than the actual `Tween` duration.

### HasTweeners

```csharp
public bool HasTweeners()
```

引擎名：`has_tweeners`

Returns `true` if any [`Tweener`](Tweener.md) has been added to the `Tween` and the `Tween` is valid. Useful when tweeners are added dynamically and the tween can end up empty. Killing an empty tween before it starts will prevent errors.

### InterpolateValue

```csharp
public static Variant InterpolateValue(Variant initialValue, Variant deltaValue, double elapsedTime, double duration, Tween.TransitionType transType, Tween.EaseType easeType)
```

引擎名：`interpolate_value`

This method can be used for manual interpolation of a value, when you don't want `Tween` to do animating for you. It's similar to `Mathf.Lerp`, but with support for custom transition and easing.

`initialValue` is the starting value of the interpolation.

`deltaValue` is the change of the value in the interpolation, i.e. it's equal to `final_value - initial_value`.

`elapsedTime` is the time in seconds that passed after the interpolation started and it's used to control the position of the interpolation. E.g. when it's equal to half of the `duration`, the interpolated value will be halfway between initial and final values. This value can also be greater than `duration` or lower than 0, which will extrapolate the value.

`duration` is the total time of the interpolation.

**Note:** If `duration` is equal to `0`, the method will always return the final value, regardless of `elapsedTime` provided.

### IsRunning

```csharp
public bool IsRunning()
```

引擎名：`is_running`

Returns whether the `Tween` is currently running, i.e. it wasn't paused and it's not finished.

### IsValid

```csharp
public bool IsValid()
```

引擎名：`is_valid`

Returns whether the `Tween` is valid. A valid `Tween` is a `Tween` contained by the scene tree (i.e. the array from [`SceneTree.GetProcessedTweens`](SceneTree.md) will contain this `Tween`). A `Tween` might become invalid when it has finished tweening, is killed, or when created with `Tween.new()`. Invalid `Tween`s can't have [`Tweener`](Tweener.md)s appended.

### Kill

```csharp
public void Kill()
```

引擎名：`kill`

Aborts all tweening operations and invalidates the `Tween`.

### Parallel

```csharp
public Tween Parallel()
```

引擎名：`parallel`

Makes the next [`Tweener`](Tweener.md) run parallelly to the previous one.

```csharp
Tween tween = CreateTween();
tween.TweenProperty(...);
tween.Parallel().TweenProperty(...);
tween.Parallel().TweenProperty(...);

```

All [`Tweener`](Tweener.md)s in the example will run at the same time.

You can make the `Tween` parallel by default by using `SetParallel()`.

### Pause

```csharp
public void Pause()
```

引擎名：`pause`

Pauses the tweening. The animation can be resumed by using `Play()`.

**Note:** If a Tween is paused and not bound to any node, it will exist indefinitely until manually started or invalidated. If you lose a reference to such Tween, you can retrieve it using [`SceneTree.GetProcessedTweens`](SceneTree.md).

### Play

```csharp
public void Play()
```

引擎名：`play`

Resumes a paused or stopped `Tween`.

### SetEase

```csharp
public Tween SetEase(Tween.EaseType ease)
```

引擎名：`set_ease`

Sets the default ease type for [`PropertyTweener`](PropertyTweener.md)s and [`MethodTweener`](MethodTweener.md)s appended after this method.

Before this method is called, the default ease type is [`Tween.EaseType.InOut`](Tween.md).

```csharp
Tween tween = CreateTween();
tween.TweenProperty(this, "position", new Vector2(300, 0), 0.5); // Uses EaseType.InOut.
tween.SetEase(Tween.EaseType.In);
tween.TweenProperty(this, "rotation_degrees", 45.0, 0.5); // Uses EaseType.In.

```

### SetIgnoreTimeScale

```csharp
public Tween SetIgnoreTimeScale(bool ignore = true)
```

引擎名：`set_ignore_time_scale`

If `ignore` is `true`, the tween will ignore [`Engine.TimeScale`](Engine.md) and update with the real, elapsed time. This affects all [`Tweener`](Tweener.md)s and their delays. Default value is `false`.

### SetLoops

```csharp
public Tween SetLoops(int loops = 0)
```

引擎名：`set_loops`

Sets the number of times the tweening sequence will be repeated, i.e. `set_loops(2)` will run the animation twice.

Calling this method without arguments will make the `Tween` run infinitely, until either it is killed with `Kill()`, the `Tween`'s bound node is freed, or all the animated objects have been freed (which makes further animation impossible).

**Warning:** Make sure to always add some duration/delay when using infinite loops. To prevent the game freezing, 0-duration looped animations (e.g. a single [`CallbackTweener`](CallbackTweener.md) with no delay) are stopped after a small number of loops, which may produce unexpected results. If a `Tween`'s lifetime depends on some node, always use `BindNode()`.

### SetParallel

```csharp
public Tween SetParallel(bool parallel = true)
```

引擎名：`set_parallel`

If `parallel` is `true`, the [`Tweener`](Tweener.md)s appended after this method will by default run simultaneously, as opposed to sequentially.

**Note:** Just like with `Parallel()`, the tweener added right before this method will also be part of the parallel step.

```csharp
tween.tween_property(self, "position", Vector2(300, 0), 0.5)
tween.set_parallel()
tween.tween_property(self, "modulate", Color.GREEN, 0.5) # Runs together with the position tweener.

```

### SetPauseMode

```csharp
public Tween SetPauseMode(Tween.TweenPauseMode mode)
```

引擎名：`set_pause_mode`

Determines the behavior of the `Tween` when the [`SceneTree`](SceneTree.md) is paused.

Default value is [`Tween.TweenPauseMode.Bound`](Tween.md).

### SetProcessMode

```csharp
public Tween SetProcessMode(Tween.TweenProcessMode mode)
```

引擎名：`set_process_mode`

Determines whether the `Tween` should run after process frames (see [`Node._Process()`](Node.md)) or physics frames (see [`Node._PhysicsProcess()`](Node.md)).

Default value is [`Tween.TweenProcessMode.Idle`](Tween.md).

### SetSpeedScale

```csharp
public Tween SetSpeedScale(float speed)
```

引擎名：`set_speed_scale`

Scales the speed of tweening. This affects all [`Tweener`](Tweener.md)s and their delays.

### SetTrans

```csharp
public Tween SetTrans(Tween.TransitionType trans)
```

引擎名：`set_trans`

Sets the default transition type for [`PropertyTweener`](PropertyTweener.md)s and [`MethodTweener`](MethodTweener.md)s appended after this method.

Before this method is called, the default transition type is [`Tween.TransitionType.Linear`](Tween.md).

```csharp
var tween = create_tween()
tween.tween_property(self, "position", Vector2(300, 0), 0.5) # Uses TRANS_LINEAR.
tween.set_trans(Tween.TRANS_SINE)
tween.tween_property(self, "rotation_degrees", 45.0, 0.5) # Uses TRANS_SINE.

```

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the tweening and resets the `Tween` to its initial state. This will not remove any appended [`Tweener`](Tweener.md)s.

**Note:** This does *not* reset targets of [`PropertyTweener`](PropertyTweener.md)s to their values when the `Tween` first started.

```csharp
var tween = create_tween()

# Will move from 0 to 500 over 1 second.
position.x = 0.0
tween.tween_property(self, "position:x", 500, 1.0)

# Will be at (about) 250 when the timer finishes.
await get_tree().create_timer(0.5).timeout

# Will now move from (about) 250 to 500 over 1 second,
# thus at half the speed as before.
tween.stop()
tween.play()

```

**Note:** If a Tween is stopped and not bound to any node, it will exist indefinitely until manually started or invalidated. If you lose a reference to such Tween, you can retrieve it using [`SceneTree.GetProcessedTweens`](SceneTree.md).

### TweenAwait

```csharp
public AwaitTweener TweenAwait(Signal signal)
```

引擎名：`tween_await`

Creates and appends an [`AwaitTweener`](AwaitTweener.md). This method can be used to await a signal to be emitted and create asynchronous animations or cutscenes.

The animation will not progress to the next step until the awaited signal is emitted or the connection becomes invalid (e.g. as a result of freeing the target object). If you know that the emission may not happen, use [`AwaitTweener.SetTimeout`](AwaitTweener.md).

**Note:** The awaited signal should be emitted during the step when [`AwaitTweener`](AwaitTweener.md) is active.

**Example:** An object launches itself and explodes upon collision or after 4 seconds.

```csharp
var tween = create_tween()
tween.tween_callback(launch)
tween.tween_await(collided).set_timeout(4.0)
tween.tween_callback(explode)

```

**Example:** A character walks to a specific point, says some lines and walks back when the player closes the message box.

```csharp
var tween = create_tween()
tween.tween_callback(walk_to.bind(600.0))
tween.tween_await(destination_reached)
tween.tween_callback(say_dialogue.bind("Good day, sir!"))
tween.tween_await(dialogue_closed)
tween.tween_callback(walk_to.bind(0.0))

```

**Note:** If you are awaiting a signal from a callback called in the same `Tween`, make sure the signal is emitted *after* the await starts. If it can't be reasonably guaranteed, you can await and emit in the same step:

```csharp
var tween = create_tween()
tween.tween_await(signal)
tween.parallel().tween_callback(method_that_emits_signal)

```

### TweenCallback

```csharp
public CallbackTweener TweenCallback(Callable callback)
```

引擎名：`tween_callback`

Creates and appends a [`CallbackTweener`](CallbackTweener.md). This method can be used to call an arbitrary method in any object. Use `Callable.bind` to bind additional arguments for the call.

**Example:** Object that keeps shooting every 1 second:

```csharp
Tween tween = GetTree().CreateTween().SetLoops();
tween.TweenCallback(Callable.From(Shoot)).SetDelay(1.0f);

```

**Example:** Turning a sprite red and then blue, with 2 second delay:

```csharp
Tween tween = GetTree().CreateTween();
Sprite2D sprite = GetNode<Sprite2D>("Sprite");
tween.TweenCallback(Callable.From(() => sprite.Modulate = Colors.Red)).SetDelay(2.0f);
tween.TweenCallback(Callable.From(() => sprite.Modulate = Colors.Blue)).SetDelay(2.0f);

```

### TweenInterval

```csharp
public IntervalTweener TweenInterval(double time)
```

引擎名：`tween_interval`

Creates and appends an [`IntervalTweener`](IntervalTweener.md). This method can be used to create delays in the tween animation, as an alternative to using the delay in other [`Tweener`](Tweener.md)s, or when there's no animation (in which case the `Tween` acts as a timer). `time` is the length of the interval, in seconds.

**Example:** Creating an interval in code execution:

```csharp
// ... some code
await ToSignal(CreateTween().TweenInterval(2.0f), Tween.SignalName.Finished);
// ... more code

```

**Example:** Creating an object that moves back and forth and jumps every few seconds:

```csharp
Tween tween = CreateTween().SetLoops();
tween.TweenProperty(GetNode("Sprite"), "position:x", 200.0f, 1.0f).AsRelative();
tween.TweenCallback(Callable.From(Jump));
tween.TweenInterval(2.0f);
tween.TweenProperty(GetNode("Sprite"), "position:x", -200.0f, 1.0f).AsRelative();
tween.TweenCallback(Callable.From(Jump));
tween.TweenInterval(2.0f);

```

### TweenMethod

```csharp
public MethodTweener TweenMethod(Callable method, Variant from, Variant to, double duration)
```

引擎名：`tween_method`

Creates and appends a [`MethodTweener`](MethodTweener.md). This method is similar to a combination of `TweenCallback()` and `TweenProperty()`. It calls a method over time with a tweened value provided as an argument. The value is tweened between `from` and `to` over the time specified by `duration`, in seconds. Use `Callable.bind` to bind additional arguments for the call. You can use [`MethodTweener.SetEase`](MethodTweener.md) and [`MethodTweener.SetTrans`](MethodTweener.md) to tweak the easing and transition of the value or [`MethodTweener.SetDelay`](MethodTweener.md) to delay the tweening.

**Example:** Making a 3D object look from one point to another point:

```csharp
Tween tween = CreateTween();
tween.TweenMethod(Callable.From((Vector3 target) => LookAt(target, Vector3.Up)), new Vector3(-1.0f, 0.0f, -1.0f), new Vector3(1.0f, 0.0f, -1.0f), 1.0f); // Use lambdas to bind additional arguments for the call.

```

**Example:** Setting the text of a [`Label`](Label.md), using an intermediate method and after a delay:

```csharp
public override void _Ready()
{
base._Ready();

Tween tween = CreateTween();
tween.TweenMethod(Callable.From<int>(SetLabelText), 0.0f, 10.0f, 1.0f).SetDelay(1.0f);
}

private void SetLabelText(int value)
{
GetNode<Label>("Label").Text = $"Counting {value}";
}

```

### TweenProperty

```csharp
public PropertyTweener TweenProperty(GodotObject object, NodePath property, Variant finalVal, double duration)
```

引擎名：`tween_property`

Creates and appends a [`PropertyTweener`](PropertyTweener.md). This method tweens a `property` of an `object` between an initial value and `finalVal` in a span of time equal to `duration`, in seconds. The initial value by default is the property's value at the time the tweening of the [`PropertyTweener`](PropertyTweener.md) starts.

```csharp
Tween tween = CreateTween();
tween.TweenProperty(GetNode("Sprite"), "position", new Vector2(100.0f, 200.0f), 1.0f);
tween.TweenProperty(GetNode("Sprite"), "position", new Vector2(200.0f, 300.0f), 1.0f);

```

will move the sprite to position (100, 200) and then to (200, 300). If you use [`PropertyTweener.From`](PropertyTweener.md) or [`PropertyTweener.FromCurrent`](PropertyTweener.md), the starting position will be overwritten by the given value instead. See other methods in [`PropertyTweener`](PropertyTweener.md) to see how the tweening can be tweaked further.

**Note:** You can find the correct property name by hovering over the property in the Inspector. You can also provide the components of a property directly by using `"property:component"` (eg. `position:x`), where it would only apply to that particular component.

**Example:** Moving an object twice from the same position, with different transition types:

```csharp
Tween tween = CreateTween();
tween.TweenProperty(GetNode("Sprite"), "position", Vector2.Right * 300.0f, 1.0f).AsRelative().SetTrans(Tween.TransitionType.Sine);
tween.TweenProperty(GetNode("Sprite"), "position", Vector2.Right * 300.0f, 1.0f).AsRelative().FromCurrent().SetTrans(Tween.TransitionType.Expo);

```

### TweenSubtween

```csharp
public SubtweenTweener TweenSubtween(Tween subtween)
```

引擎名：`tween_subtween`

Creates and appends a [`SubtweenTweener`](SubtweenTweener.md). This method can be used to nest `subtween` within this `Tween`, allowing for the creation of more complex and composable sequences.

```csharp
# Subtween will rotate the object.
var subtween = create_tween()
subtween.tween_property(self, "rotation_degrees", 45.0, 1.0)
subtween.tween_property(self, "rotation_degrees", 0.0, 1.0)

# Parent tween will execute the subtween as one of its steps.
var tween = create_tween()
tween.tween_property(self, "position:x", 500, 3.0)
tween.tween_subtween(subtween)
tween.tween_property(self, "position:x", 300, 2.0)

```

**Note:** The methods `Pause()`, `Stop()`, and `SetLoops()` can cause the parent `Tween` to get stuck on the subtween step; see the documentation for those methods for more information.

**Note:** The pause and process modes set by `SetPauseMode()` and `SetProcessMode()` on `subtween` will be overridden by the parent `Tween`'s settings.

## 事件（信号）

### Finished

```csharp
public event Action Finished
```

引擎名：`finished`

Emitted when the `Tween` has finished all tweening. Never emitted when the `Tween` is set to infinite looping (see `SetLoops()`).

### LoopFinished

```csharp
public event Tween.LoopFinishedEventHandler LoopFinished
```

引擎名：`loop_finished`

Emitted when a full loop is complete (see `SetLoops()`), providing the loop index. This signal is not emitted after the final loop, use `Finished` instead for this case.

### StepFinished

```csharp
public event Tween.StepFinishedEventHandler StepFinished
```

引擎名：`step_finished`

Emitted when one step of the `Tween` is complete, providing the step index. One step is either a single [`Tweener`](Tweener.md) or a group of [`Tweener`](Tweener.md)s running in parallel.

## EaseType

命名空间 `Godot`；enum

### 枚举值

- `In = 0`：The interpolation starts slowly and speeds up towards the end.
- `Out = 1`：The interpolation starts quickly and slows down towards the end.
- `InOut = 2`：A combination of `In` and `Out`.
- `OutIn = 3`：A combination of `In` and `Out`.

## LoopFinishedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tween.LoopFinished`](Tween.md) event of a [`Tween`](Tween.md) class.

### 签名

#### Invoke

```csharp
delegate void LoopFinishedEventHandler(long loopCount)
```

## StepFinishedEventHandler

命名空间 `Godot`；delegate

Represents the method that handles the [`Tween.StepFinished`](Tween.md) event of a [`Tween`](Tween.md) class.

### 签名

#### Invoke

```csharp
delegate void StepFinishedEventHandler(long idx)
```

## TransitionType

命名空间 `Godot`；enum

### 枚举值

- `Linear = 0`：The animation is interpolated linearly.
- `Sine = 1`：The animation is interpolated using a sine function.
- `Quint = 2`：The animation is interpolated with a quintic (to the power of 5) function.
- `Quart = 3`：The animation is interpolated with a quartic (to the power of 4) function.
- `Quad = 4`：The animation is interpolated with a quadratic (to the power of 2) function.
- `Expo = 5`：The animation is interpolated with an exponential (to the power of x) function.
- `Elastic = 6`：The animation is interpolated with elasticity, wiggling around the edges.
- `Cubic = 7`：The animation is interpolated with a cubic (to the power of 3) function.
- `Circ = 8`：The animation is interpolated with a function using square roots.
- `Bounce = 9`：The animation is interpolated by bouncing at the end.
- `Back = 10`：The animation is interpolated backing out at ends.
- `Spring = 11`：The animation is interpolated like a spring towards the end.

## TweenPauseMode

命名空间 `Godot`；enum

### 枚举值

- `Bound = 0`：If the [`Tween`](Tween.md) has a bound node, it will process when that node can process (see [`Node.ProcessMode`](Node.md)).
- `Stop = 1`：If [`SceneTree`](SceneTree.md) is paused, the [`Tween`](Tween.md) will also pause.
- `Process = 2`：The [`Tween`](Tween.md) will process regardless of whether [`SceneTree`](SceneTree.md) is paused.

## TweenProcessMode

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`：The [`Tween`](Tween.md) updates after each physics frame (see [`Node._PhysicsProcess()`](Node.md)).
- `Idle = 1`：The [`Tween`](Tween.md) updates after each process frame (see [`Node._Process()`](Node.md)).
