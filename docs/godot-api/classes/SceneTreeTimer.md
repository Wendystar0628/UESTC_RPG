<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SceneTreeTimer

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A one-shot timer managed by the scene tree, which emits `Timeout` on completion. See also [`SceneTree.CreateTimer`](SceneTree.md).

As opposed to [`Timer`](Timer.md), it does not require the instantiation of a node. Commonly used to create a one-shot delay timer as in the following example:

```csharp
public async Task SomeFunction()
{
GD.Print("Timer started.");
await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
GD.Print("Timer ended.");
}

```

The timer will be dereferenced after its time elapses. To preserve the timer, you can keep a reference to it. See [`RefCounted`](RefCounted.md).

**Note:** The timer is processed after all of the nodes in the current frame, i.e. node's [`Node._Process()`](Node.md) method would be called before the timer (or [`Node._PhysicsProcess()`](Node.md) if `process_in_physics` in [`SceneTree.CreateTimer`](SceneTree.md) has been set to `true`).

## 属性

### TimeLeft

```csharp
public double TimeLeft { get; set; }
```

引擎名：`time_left`

The time remaining (in seconds).

## 方法

### GetTimeLeft

```csharp
public double GetTimeLeft()
```

引擎名：`get_time_left`

### SetTimeLeft

```csharp
public void SetTimeLeft(double time)
```

引擎名：`set_time_left`

## 事件（信号）

### Timeout

```csharp
public event Action Timeout
```

引擎名：`timeout`

Emitted when the timer reaches 0.
