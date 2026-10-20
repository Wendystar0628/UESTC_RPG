<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# IAwaitable

命名空间 `Godot`；interface

An interface that requires a GetAwaiter() method to get a reference to the Awaiter.

## 方法

### GetAwaiter

```csharp
public IAwaiter GetAwaiter()
```

Gets an Awaiter for this `IAwaitable`.

返回：An Awaiter.
