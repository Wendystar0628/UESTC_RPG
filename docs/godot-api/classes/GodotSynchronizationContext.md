<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# GodotSynchronizationContext

命名空间 `Godot`；class

## 构造函数

### GodotSynchronizationContext

```csharp
public GodotSynchronizationContext()
```

## 方法

### Dispose

```csharp
public void Dispose()
```

### ExecutePendingContinuations

```csharp
public void ExecutePendingContinuations()
```

Calls the Key method on each workItem object in the _queue to activate their callbacks.

### Post

```csharp
public virtual void Post(SendOrPostCallback d, object state)
```

### Send

```csharp
public virtual void Send(SendOrPostCallback d, object state)
```
