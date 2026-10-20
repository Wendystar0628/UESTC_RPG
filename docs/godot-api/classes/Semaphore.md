<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# Semaphore

命名空间 `Godot`；class；继承 [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A synchronization semaphore that can be used to synchronize multiple [`GodotThread`](GodotThread.md)s. Initialized to zero on creation. For a binary version, see [`Mutex`](Mutex.md).

**Warning:** Semaphores must be used carefully to avoid deadlocks.

**Warning:** To guarantee that the operating system is able to perform proper cleanup (no crashes, no deadlocks), these conditions must be met:

- When a `Semaphore`'s reference count reaches zero and it is therefore destroyed, no threads must be waiting on it.

- When a [`GodotThread`](GodotThread.md)'s reference count reaches zero and it is therefore destroyed, it must not be waiting on any semaphore.

## 构造函数

### Semaphore

```csharp
public Semaphore()
```

## 方法

### Post

```csharp
public void Post()
```

引擎名：`post`

Lowers the `Semaphore`, allowing one thread in, or more if `count` is specified.

### Post

```csharp
public void Post(int count = 1)
```

引擎名：`post`

Lowers the `Semaphore`, allowing one thread in, or more if `count` is specified.

### TryWait

```csharp
public bool TryWait()
```

引擎名：`try_wait`

Like `Wait()`, but won't block, so if the value is zero, fails immediately and returns `false`. If non-zero, it returns `true` to report success.

### Wait

```csharp
public void Wait()
```

引擎名：`wait`

Waits for the `Semaphore`, if its value is zero, blocks until non-zero.
