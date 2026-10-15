<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Mutex

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A synchronization mutex (mutual exclusion). This is used to synchronize multiple [`GodotThread`](GodotThread.md)s, and is equivalent to a binary [`Semaphore`](Semaphore.md). It guarantees that only one thread can access a critical section at a time.

This is a reentrant mutex, meaning that it can be locked multiple times by one thread, provided it also unlocks it as many times.

**Warning:** To ensure proper cleanup without crashes or deadlocks, the following conditions must be met:

- When a `Mutex`'s reference count reaches zero and it is therefore destroyed, no threads (including the one on which the destruction will happen) must have it locked.

- When a [`GodotThread`](GodotThread.md)'s reference count reaches zero and it is therefore destroyed, it must not have any mutex locked.

## 构造函数

### Mutex

```csharp
public Mutex()
```

## 方法

### Lock

```csharp
public void Lock()
```

引擎名：`lock`

Locks this `Mutex`, blocks until it is unlocked by the current owner.

**Note:** This function returns without blocking if the thread already has ownership of the mutex.

### TryLock

```csharp
public bool TryLock()
```

引擎名：`try_lock`

Tries locking this `Mutex`, but does not block. Returns `true` on success, `false` otherwise.

**Note:** This function returns `true` if the thread already has ownership of the mutex.

### Unlock

```csharp
public void Unlock()
```

引擎名：`unlock`

Unlocks this `Mutex`, leaving it to other threads.

**Note:** If a thread called `Lock()` or `TryLock()` multiple times while already having ownership of the mutex, it must also call `Unlock()` the same number of times in order to unlock it correctly.

**Warning:** Calling `Unlock()` more times than `Lock()` on a given thread, thus ending up trying to unlock a non-locked mutex, is wrong and may causes crashes or deadlocks.
