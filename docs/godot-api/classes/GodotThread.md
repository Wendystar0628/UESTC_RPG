<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GodotThread

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A unit of execution in a process. Can run methods on [`GodotObject`](GodotObject.md)s simultaneously. The use of synchronization via [`Mutex`](Mutex.md) or [`Semaphore`](Semaphore.md) is advised if working with shared objects.

**Warning:** To ensure proper cleanup without crashes or deadlocks, when a `GodotThread`'s reference count reaches zero and it is therefore destroyed, the following conditions must be met:

- It must not have any [`Mutex`](Mutex.md) objects locked.

- It must not be waiting on any [`Semaphore`](Semaphore.md) objects.

- `WaitToFinish()` should have been called on it.

## 构造函数

### GodotThread

```csharp
public GodotThread()
```

## 方法

### GetId

```csharp
public string GetId()
```

Returns the current `GodotThread`'s ID, uniquely identifying it among all threads. If the `GodotThread` has not started running or if `WaitToFinish()` has been called, this returns an empty string.

### IsAlive

```csharp
public bool IsAlive()
```

Returns `true` if this `GodotThread` is currently running the provided function. This is useful for determining if `WaitToFinish()` can be called without blocking the calling thread.

To check if a `GodotThread` is joinable, use `IsStarted()`.

### IsMainThread

```csharp
public static bool IsMainThread()
```

Returns `true` if the thread this method was called from is the main thread.

**Note:** This is a static method and isn't associated with a specific `GodotThread` object.

### IsStarted

```csharp
public bool IsStarted()
```

Returns `true` if this `GodotThread` has been started. Once started, this will return `true` until it is joined using `WaitToFinish()`. For checking if a `GodotThread` is still executing its task, use `IsAlive()`.

### SetThreadSafetyChecksEnabled

```csharp
public static void SetThreadSafetyChecksEnabled(bool enabled)
```

Sets whether the thread safety checks the engine normally performs in methods of certain classes (e.g., [`Node`](Node.md)) should happen **on the current thread**.

The default, for every thread, is that they are enabled (as if called with `enabled` being `true`).

Those checks are conservative. That means that they will only succeed in considering a call thread-safe (and therefore allow it to happen) if the engine can guarantee such safety.

Because of that, there may be cases where the user may want to disable them (`enabled` being `false`) to make certain operations allowed again. By doing so, it becomes the user's responsibility to ensure thread safety (e.g., by using [`Mutex`](Mutex.md)) for those objects that are otherwise protected by the engine.

**Note:** This is an advanced usage of the engine. You are advised to use it only if you know what you are doing and there is no safer way.

**Note:** This is useful for scripts running on either arbitrary `GodotThread` objects or tasks submitted to the [`WorkerThreadPool`](WorkerThreadPool.md). It doesn't apply to code running during [`Node`](Node.md) group processing, where the checks will be always performed.

**Note:** Even in the case of having disabled the checks in a [`WorkerThreadPool`](WorkerThreadPool.md) task, there's no need to re-enable them at the end. The engine will do so.

### Start

```csharp
public Error Start(Callable callable, GodotThread.Priority priority = GodotThread.Priority.Normal)
```

Starts a new `GodotThread` that calls `callable`.

If the method takes some arguments, you can pass them using `Callable.bind`.

The `priority` of the `GodotThread` can be changed by passing a value from the [`GodotThread.Priority`](GodotThread.md) enum.

Returns [`Error.Ok`](Error.md) on success, or [`Error.CantCreate`](Error.md) on failure.

### WaitToFinish

```csharp
public Variant WaitToFinish()
```

Joins the `GodotThread` and waits for it to finish. Returns the output of the [`Callable`](Callable.md) passed to `Start()`.

Should either be used when you want to retrieve the value returned from the method called by the `GodotThread` or before freeing the instance that contains the `GodotThread`.

To determine if this can be called without blocking the calling thread, check if `IsAlive()` is `false`.

## Priority

命名空间 `Godot`；enum

### 枚举值

- `Low = 0`：A thread running with lower priority than normally.
- `Normal = 1`：A thread with a standard priority.
- `High = 2`：A thread running with higher priority than normally.
