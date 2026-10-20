<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GodotTaskScheduler

命名空间 `Godot`；class

GodotTaskScheduler contains a linked list of tasks to perform as a queue. Methods
within the class are used to control the queue and perform the contained tasks.

## 构造函数

### GodotTaskScheduler

```csharp
public GodotTaskScheduler()
```

Constructs a new GodotTaskScheduler instance.

## 方法

### Activate

```csharp
public void Activate()
```

Executes all queued tasks and pending tasks from the current context.

### Dispose

```csharp
public void Dispose()
```
