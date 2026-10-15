<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SignalAwaiter

命名空间 `Godot`；class

## 构造函数

### SignalAwaiter

```csharp
public SignalAwaiter(GodotObject source, StringName signal, GodotObject target)
```

## 属性

### IsCompleted

```csharp
public bool IsCompleted { get; }
```

## 方法

### GetAwaiter

```csharp
public IAwaiter<Variant[]> GetAwaiter()
```

### GetResult

```csharp
public Variant[] GetResult()
```

### OnCompleted

```csharp
public void OnCompleted(Action continuation)
```
