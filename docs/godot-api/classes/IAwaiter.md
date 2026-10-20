<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IAwaiter

命名空间 `Godot`；interface

An interface that requires a boolean for completion status and a method that gets the result of completion.

## 属性

### IsCompleted

```csharp
public bool IsCompleted { get; }
```

The completion status of this `IAwaiter`.

## 方法

### GetResult

```csharp
public void GetResult()
```

Gets the result of completion for this `IAwaiter`.
