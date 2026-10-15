<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# AwaitTweener

命名空间 `Godot`；class；继承 [`Tweener`](Tweener.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`AwaitTweener` is used to await a specified signal, allowing asynchronous steps in [`Tween`](Tween.md) animation. See [`Tween.TweenAwait`](Tween.md) for more usage information.

The [`Tweener.Finished`](Tweener.md) signal is emitted when either the awaited signal is received, when timeout is reached, or when the target object is freed.

## 构造函数

### AwaitTweener

```csharp
public AwaitTweener()
```

## 方法

### SetTimeout

```csharp
public AwaitTweener SetTimeout(double timeout)
```

引擎名：`set_timeout`

Sets the maximum time an `AwaitTweener` can wait for the signal. Can be used as a safeguard for signals that may never be emitted. If not specified, the tweener will wait indefinitely.
