<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IAwaiter<TResult>

命名空间 `Godot`；interface

A templated interface that requires a boolean for completion status and a method that gets the result of completion and returns it.

## 属性

### IsCompleted

```csharp
public bool IsCompleted { get; }
```

The completion status of this [`IAwaiter`](IAwaiter.md).

## 方法

### GetResult

```csharp
public TResult GetResult()
```

Gets the result of completion for this [`IAwaiter`](IAwaiter.md).
