<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IAwaitable<TResult>

命名空间 `Godot`；interface

A templated interface that requires a GetAwaiter() method to get a reference to the Awaiter.

## 方法

### GetAwaiter

```csharp
public IAwaiter<TResult> GetAwaiter()
```

Gets an Awaiter for this [`IAwaitable`](IAwaitable.md).

返回：An Awaiter.
