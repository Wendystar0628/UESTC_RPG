<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SubtweenTweener

命名空间 `Godot`；class；继承 [`Tweener`](Tweener.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

`SubtweenTweener` is used to execute a [`Tween`](Tween.md) as one step in a sequence defined by another [`Tween`](Tween.md). See [`Tween.TweenSubtween`](Tween.md) for more usage information.

**Note:** [`Tween.TweenSubtween`](Tween.md) is the only correct way to create `SubtweenTweener`. Any `SubtweenTweener` created manually will not function correctly.

## 构造函数

### SubtweenTweener

```csharp
public SubtweenTweener()
```

## 方法

### SetDelay

```csharp
public SubtweenTweener SetDelay(double delay)
```

引擎名：`set_delay`

Sets the time in seconds after which the `SubtweenTweener` will start running the subtween. By default there's no delay.
