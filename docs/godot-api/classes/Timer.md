<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Timer

命名空间 `Godot`；class；继承 [`Node`](Node.md) → [`GodotObject`](GodotObject.md)

The `Timer` node is a countdown timer and is the simplest way to handle time-based logic in the engine. When a timer reaches the end of its `WaitTime`, it will emit the `Timeout` signal.

After a timer enters the scene tree, it can be manually started with `Start()`. A timer node is also started automatically if `Autostart` is `true`.

Without requiring much code, a timer node can be added and configured in the editor. The `Timeout` signal it emits can also be connected through the Signals dock in the editor:

```csharp
func _on_timer_timeout():
print("Time to attack!")

```

**Note:** To create a one-shot timer without instantiating a node, use [`SceneTree.CreateTimer`](SceneTree.md).

**Note:** Timers are affected by [`Engine.TimeScale`](Engine.md) unless `IgnoreTimeScale` is `true`. The higher the time scale, the sooner timers will end. How often a timer processes may depend on the framerate or [`Engine.PhysicsTicksPerSecond`](Engine.md).

## 构造函数

### Timer

```csharp
public Timer()
```

## 属性

### Autostart

```csharp
public bool Autostart { get; set; }
```

引擎名：`autostart`

If `true`, the timer will start immediately when it enters the scene tree.

**Note:** After the timer enters the tree, this property is automatically set to `false`.

**Note:** This property does nothing when the timer is running in the editor.

### IgnoreTimeScale

```csharp
public bool IgnoreTimeScale { get; set; }
```

引擎名：`ignore_time_scale`

If `true`, the timer will ignore [`Engine.TimeScale`](Engine.md) and update with the real, elapsed time.

### OneShot

```csharp
public bool OneShot { get; set; }
```

引擎名：`one_shot`

If `true`, the timer will stop after reaching the end. Otherwise, as by default, the timer will automatically restart.

### Paused

```csharp
public bool Paused { get; set; }
```

引擎名：`paused`

If `true`, the timer is paused. A paused timer does not process until this property is set back to `false`, even when `Start()` is called. See also `Stop()`.

### ProcessCallback

```csharp
public Timer.TimerProcessCallback ProcessCallback { get; set; }
```

引擎名：`process_callback`

Specifies when the timer is updated during the main loop.

### TimeLeft

```csharp
public double TimeLeft { get; }
```

引擎名：`time_left`

The timer's remaining time in seconds. This is always `0` if the timer is stopped.

**Note:** This property is read-only and cannot be modified. It is based on `WaitTime`.

### WaitTime

```csharp
public double WaitTime { get; set; }
```

引擎名：`wait_time`

The time required for the timer to end, in seconds. This property can also be set every time `Start()` is called.

**Note:** Timers can only process once per physics or process frame (depending on the `ProcessCallback`). An unstable framerate may cause the timer to end inconsistently, which is especially noticeable if the wait time is lower than roughly `0.05` seconds. For very short timers, it is recommended to write your own code instead of using a `Timer` node. Timers are also affected by [`Engine.TimeScale`](Engine.md).

## 方法

### GetTimeLeft

```csharp
public double GetTimeLeft()
```

引擎名：`get_time_left`

### GetTimerProcessCallback

```csharp
public Timer.TimerProcessCallback GetTimerProcessCallback()
```

引擎名：`get_timer_process_callback`

### GetWaitTime

```csharp
public double GetWaitTime()
```

引擎名：`get_wait_time`

### HasAutostart

```csharp
public bool HasAutostart()
```

引擎名：`has_autostart`

### IsIgnoringTimeScale

```csharp
public bool IsIgnoringTimeScale()
```

引擎名：`is_ignoring_time_scale`

### IsOneShot

```csharp
public bool IsOneShot()
```

引擎名：`is_one_shot`

### IsPaused

```csharp
public bool IsPaused()
```

引擎名：`is_paused`

### IsStopped

```csharp
public bool IsStopped()
```

引擎名：`is_stopped`

Returns `true` if the timer is stopped or has not started.

### SetAutostart

```csharp
public void SetAutostart(bool enable)
```

引擎名：`set_autostart`

### SetIgnoreTimeScale

```csharp
public void SetIgnoreTimeScale(bool ignore)
```

引擎名：`set_ignore_time_scale`

### SetOneShot

```csharp
public void SetOneShot(bool enable)
```

引擎名：`set_one_shot`

### SetPaused

```csharp
public void SetPaused(bool paused)
```

引擎名：`set_paused`

### SetTimerProcessCallback

```csharp
public void SetTimerProcessCallback(Timer.TimerProcessCallback callback)
```

引擎名：`set_timer_process_callback`

### SetWaitTime

```csharp
public void SetWaitTime(double timeSec)
```

引擎名：`set_wait_time`

### Start

```csharp
public void Start(double timeSec = -1)
```

引擎名：`start`

Starts the timer, or resets the timer if it was started already. Fails if the timer is not inside the scene tree. If `timeSec` is greater than `0`, this value is used for the `WaitTime`.

**Note:** This method does not resume a paused timer. See `Paused`.

### Stop

```csharp
public void Stop()
```

引擎名：`stop`

Stops the timer. See also `Paused`. Unlike `Start()`, this can safely be called if the timer is not inside the scene tree.

**Note:** Calling `Stop()` does not emit the `Timeout` signal, as the timer is not considered to have timed out. If this is desired, use `$Timer.timeout.emit()` after calling `Stop()` to manually emit the signal.

## 事件（信号）

### Timeout

```csharp
public event Action Timeout
```

引擎名：`timeout`

Emitted when the timer reaches the end.

## TimerProcessCallback

命名空间 `Godot`；enum

### 枚举值

- `Physics = 0`：Update the timer every physics process frame (see [`Node.NotificationInternalPhysicsProcess`](Node.md)).
- `Idle = 1`：Update the timer every process (rendered) frame (see [`Node.NotificationInternalProcess`](Node.md)).
