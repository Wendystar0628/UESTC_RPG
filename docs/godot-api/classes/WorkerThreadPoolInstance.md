<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# WorkerThreadPoolInstance

命名空间 `Godot`；class；继承 [`GodotObject`](GodotObject.md)

The [`WorkerThreadPool`](WorkerThreadPool.md) singleton allocates a set of [`GodotThread`](GodotThread.md)s (called worker threads) on project startup and provides methods for offloading tasks to them. This can be used for simple multithreading without having to create [`GodotThread`](GodotThread.md)s.

Tasks hold the [`Callable`](Callable.md) to be run by the threads. [`WorkerThreadPool`](WorkerThreadPool.md) can be used to create regular tasks, which will be taken by one worker thread, or group tasks, which can be distributed between multiple worker threads. Group tasks execute the [`Callable`](Callable.md) multiple times, which makes them useful for iterating over a lot of elements, such as the enemies in an arena.

Here's a sample on how to offload an expensive function to worker threads:

```csharp
private List<Node> _enemies = new List<Node>(); // A list to be filled with enemies.

private void ProcessEnemyAI(int enemyIndex)
{
Node processedEnemy = _enemies[enemyIndex];
// Expensive logic here.
}

public override void _Process(double delta)
{
long taskId = WorkerThreadPool.AddGroupTask(Callable.From<int>(ProcessEnemyAI), _enemies.Count);
// Other code...
WorkerThreadPool.WaitForGroupTaskCompletion(taskId);
// Other code that depends on the enemy AI already being processed.
}

```

The above code relies on the number of elements in the `enemies` array remaining constant during the multithreaded part.

**Note:** Using this singleton could affect performance negatively if the task being distributed between threads is not computationally expensive.

## 方法

### AddGroupTask

```csharp
public long AddGroupTask(Callable action, int elements, int tasksNeeded = -1, bool highPriority = false, string description = "")
```

Adds `action` as a group task to be executed by the worker threads. The [`Callable`](Callable.md) will be called a number of times based on `elements`, with the first thread calling it with the value `0` as a parameter, and each consecutive execution incrementing this value by 1 until it reaches `element - 1`.

The number of threads the task is distributed to is defined by `tasksNeeded`, where the default value `-1` means it is distributed to all worker threads. `highPriority` determines if the task has a high priority or a low priority (default). You can optionally provide a `description` to help with debugging.

Returns a group task ID that can be used by other methods.

**Warning:** Every task must be waited for completion using `WaitForTaskCompletion()` or `WaitForGroupTaskCompletion()` at some point so that any allocated resources inside the task can be cleaned up.

### AddTask

```csharp
public long AddTask(Callable action, bool highPriority = false, string description = "")
```

Adds `action` as a task to be executed by a worker thread. `highPriority` determines if the task has a high priority or a low priority (default). You can optionally provide a `description` to help with debugging.

Returns a task ID that can be used by other methods.

**Warning:** Every task must be waited for completion using `WaitForTaskCompletion()` or `WaitForGroupTaskCompletion()` at some point so that any allocated resources inside the task can be cleaned up.

### GetCallerGroupId

```csharp
public long GetCallerGroupId()
```

Returns the task group ID of the current thread calling this method, or `-1` if invalid or the current thread is not part of a task group.

### GetCallerTaskId

```csharp
public long GetCallerTaskId()
```

Returns the task ID of the current thread calling this method, or `-1` if the task is a group task, invalid or the current thread is not part of the thread pool (e.g. the main thread).

Can be used by a task to get its own task ID, or to determine whether the current code is running inside the worker thread pool.

**Note:** Group tasks have their own IDs, so this method will return `-1` for group tasks.

### GetGroupProcessedElementCount

```csharp
public uint GetGroupProcessedElementCount(long groupId)
```

Returns how many times the [`Callable`](Callable.md) of the group task with the given ID has already been executed by the worker threads.

**Note:** If a thread has started executing the [`Callable`](Callable.md) but is yet to finish, it won't be counted.

### IsGroupTaskCompleted

```csharp
public bool IsGroupTaskCompleted(long groupId)
```

Returns `true` if the group task with the given ID is completed.

**Note:** You should only call this method between adding the group task and awaiting its completion.

### IsTaskCompleted

```csharp
public bool IsTaskCompleted(long taskId)
```

Returns `true` if the task with the given ID is completed.

**Note:** You should only call this method between adding the task and awaiting its completion.

### WaitForGroupTaskCompletion

```csharp
public void WaitForGroupTaskCompletion(long groupId)
```

Pauses the thread that calls this method until the group task with the given ID is completed.

### WaitForTaskCompletion

```csharp
public Error WaitForTaskCompletion(long taskId)
```

Pauses the thread that calls this method until the task with the given ID is completed.

Returns [`Error.Ok`](Error.md) if the task could be successfully awaited.

Returns [`Error.InvalidParameter`](Error.md) if a task with the passed ID does not exist (maybe because it was already awaited and disposed of).

Returns [`Error.Busy`](Error.md) if the call is made from another running task and, due to task scheduling, there's potential for deadlocking (e.g., the task to await may be at a lower level in the call stack and therefore can't progress). This is an advanced situation that should only matter when some tasks depend on others (in the current implementation, the tricky case is a task trying to wait on an older one).
